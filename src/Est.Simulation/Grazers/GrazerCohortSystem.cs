using Est.Simulation.Organisms;
using Est.Simulation.Biogeochemistry;
using Est.Simulation.Causality;
using Est.Simulation.Hydrology;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Grazers;

/// <summary>
/// First-pass causal terrestrial-grazer movement, grazing, and survival.
///
/// Cohorts move across authoritative surface-grid topology. Live vegetation is
/// both coarse terrestrial habitat and directly consumable plant biomass.
/// Surface liquid water is used only when explicitly enabled as a coarse
/// movement signal or water-absence mortality signal.
///
/// Cohorts sharing a cell share available plant biomass proportionally rather
/// than consuming in iteration order. This system does not reproduce, split,
/// merge, or materialize individual grazers.
/// </summary>
public sealed class GrazerCohortSystem
    : ICausalSystem
{
    private const double SecondsPerDay =
        86_400d;

    private readonly PlanetId _planetId;
    private readonly GrazerModelParameters _parameters;
    private readonly double?
        _plantNitrogenKilogramsPerKilogramLiveBiomass;

    public GrazerCohortSystem(
        PlanetId planetId,
        GrazerModelParameters parameters,
        double?
            plantNitrogenKilogramsPerKilogramLiveBiomass =
                null)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(
            parameters);

        if (plantNitrogenKilogramsPerKilogramLiveBiomass
                is not null &&
            (!double.IsFinite(
                plantNitrogenKilogramsPerKilogramLiveBiomass.Value) ||
             plantNitrogenKilogramsPerKilogramLiveBiomass.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    plantNitrogenKilogramsPerKilogramLiveBiomass),
                "Plant-tissue nitrogen ratio must be finite and greater than zero.");
        }

        _planetId =
            planetId;

        _parameters =
            parameters;

        _plantNitrogenKilogramsPerKilogramLiveBiomass =
            plantNitrogenKilogramsPerKilogramLiveBiomass;
    }

    public SimulationChange Evaluate(
        WorldState world,
        long elapsedSeconds)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        if (elapsedSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elapsedSeconds));
        }

        var planet =
            world.Planets.FirstOrDefault(
                candidate =>
                    candidate.Id ==
                    _planetId)
            ?? throw new InvalidOperationException(
                "The target planet does not exist in this world.");

        var hydrology =
            world.Hydrology.FirstOrDefault(
                candidate =>
                    candidate.PlanetId ==
                    _planetId)
            ?? throw new InvalidOperationException(
                "Grazer integration requires hydrology for the target planet.");

        var vegetation =
            world.Vegetation.FirstOrDefault(
                candidate =>
                    candidate.PlanetId ==
                    _planetId)
            ?? throw new InvalidOperationException(
                "Grazer integration requires vegetation for the target planet.");

        var biogeochemistry =
            world.Biogeochemistry.FirstOrDefault(
                candidate =>
                    candidate.PlanetId ==
                    _planetId);

        if (biogeochemistry is not null &&
            biogeochemistry.GridDefinition !=
                vegetation.GridDefinition)
        {
            throw new InvalidOperationException(
                "Grazer mortality and biogeochemistry must use the same surface grid.");
        }

        if (hydrology.GridDefinition !=
            vegetation.GridDefinition)
        {
            throw new InvalidOperationException(
                "Grazer ecological inputs must use the same surface grid.");
        }

        hydrology.ValidateFor(
            planet);

        vegetation.ValidateFor(
            planet);

        biogeochemistry?.ValidateFor(
            planet);

        if (_plantNitrogenKilogramsPerKilogramLiveBiomass
                is not null &&
            biogeochemistry is null)
        {
            throw new InvalidOperationException(
                "Nitrogen-coupled grazer grazing requires authoritative biogeochemistry state for the target planet.");
        }

        if (_parameters.MaximumRecruitmentRatePerDay > 0 &&
            _parameters.MaterialPerGrazer
                .LiveNitrogenKilogramsPerUnit > 0 &&
            _plantNitrogenKilogramsPerKilogramLiveBiomass
                is null)
        {
            throw new InvalidOperationException(
                "Nitrogen-bearing grazer recruitment requires an authoritative plant-tissue nitrogen policy.");
        }

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                vegetation.GridDefinition);

        var surfaceCellsById =
            grid.Cells.ToDictionary(
                cell =>
                    cell.Id);

        var vegetationMassByCellId =
            vegetation.Cells.ToDictionary(
                cell =>
                    cell.CellId,
                cell =>
                    cell.LiveBiomassKilogramsPerSquareMeter *
                    surfaceCellsById[
                        cell.CellId]
                    .AreaSquareMeters);

        var cohorts =
            world.GrazerCohorts
                .Where(
                    cohort =>
                        cohort.PlanetId ==
                        _planetId)
                .OrderBy(
                    cohort =>
                        cohort.Id.Value)
                .Select(
                    cohort =>
                        new RuntimeCohort(
                            cohort))
                .ToList();

        var feedingMaterialTransfers =
            new List<VegetationFeedingEvent>();

        var mortalityDeposits =
            new List<OrganismMortalityDeposit>();

        var nitrogenReturnedKilograms =
            0d;

        var initialCohortCount =
            cohorts.Count;

        var initialMemberCount =
            cohorts.Sum(
                cohort =>
                    (long)cohort.MemberCount);

        var initialVegetationMass =
            vegetationMassByCellId.Values.Sum();

        var movementSteps =
            0;

        var grazingSteps =
            0;

        var foodStressSteps =
            0;

        var waterStressSteps =
            0;

        var habitatStressSteps =
            0;

        var integrationSubsteps =
            0;

        var biomassGrazedKilograms =
            0d;

        long recruitedMembers =
            0;

        var biomassAssimilatedKilograms =
            0d;

        var nitrogenAssimilatedKilograms =
            0d;

        var remainingSeconds =
            elapsedSeconds;

        while (remainingSeconds > 0 &&
               cohorts.Count > 0)
        {
            var stepSeconds =
                Math.Min(
                    remainingSeconds,
                    _parameters
                        .MaximumIntegrationStepSeconds);

            var elapsedDays =
                stepSeconds /
                SecondsPerDay;

            foreach (var cohort in cohorts)
            {
                var currentCell =
                    grid.LocateCell(
                        cohort.LatitudeDegrees,
                        cohort.LongitudeDegrees);

                var target =
                    SelectTargetCell(
                        currentCell,
                        cohort.MemberCount,
                        grid,
                        hydrology,
                        vegetationMassByCellId);

                if (target.Cell.Id ==
                    currentCell.Id)
                {
                    continue;
                }

                var moved =
                    MoveToward(
                        planet,
                        cohort.LatitudeDegrees,
                        cohort.LongitudeDegrees,
                        target.Cell.CenterLatitudeDegrees,
                        target.Cell.CenterLongitudeDegrees,
                        _parameters
                            .MaximumTravelMetersPerDay *
                        elapsedDays);

                if (moved.LatitudeDegrees ==
                        cohort.LatitudeDegrees &&
                    moved.LongitudeDegrees ==
                        cohort.LongitudeDegrees)
                {
                    continue;
                }

                cohort.LatitudeDegrees =
                    moved.LatitudeDegrees;

                cohort.LongitudeDegrees =
                    moved.LongitudeDegrees;

                movementSteps++;
            }

            var occupiedCellsByCohortId =
                cohorts.ToDictionary(
                    cohort =>
                        cohort.Id,
                    cohort =>
                        grid.LocateCell(
                            cohort.LatitudeDegrees,
                            cohort.LongitudeDegrees));

            var assessmentsByCohortId =
                cohorts.ToDictionary(
                    cohort =>
                        cohort.Id,
                    cohort =>
                        AssessCell(
                            occupiedCellsByCohortId[
                                cohort.Id],
                            hydrology,
                            vegetationMassByCellId));

            var foodSupportFractionByCohortId =
                cohorts.ToDictionary(
                    cohort =>
                        cohort.Id,
                    _ =>
                        1d);

            foreach (var group in
                     cohorts.GroupBy(
                         cohort =>
                             occupiedCellsByCohortId[
                                 cohort.Id]
                             .Id))
            {
                var cellId =
                    group.Key;

                var totalDemandKilograms =
                    group.Sum(
                        cohort =>
                            GrazingDemandKilograms(
                                cohort.MemberCount,
                                elapsedDays));

                if (!double.IsFinite(
                        totalDemandKilograms))
                {
                    throw new InvalidOperationException(
                        "Grazer integration produced a non-finite grazing demand.");
                }

                if (totalDemandKilograms <= 0)
                {
                    continue;
                }

                var availableKilograms =
                    vegetationMassByCellId[
                        cellId];

                var grazedKilograms =
                    Math.Min(
                        availableKilograms,
                        totalDemandKilograms);

                if (grazedKilograms > 0)
                {
                    vegetationMassByCellId[
                        cellId] =
                        Math.Max(
                            0,
                            availableKilograms -
                            grazedKilograms);

                    biomassGrazedKilograms +=
                        grazedKilograms;

                    grazingSteps++;
                }

                var supportFraction =
                    Math.Clamp(
                        grazedKilograms /
                        totalDemandKilograms,
                        0,
                        1);

                var groupAssimilatedBiomassKilograms =
                    0d;

                var groupAssimilatedNitrogenKilograms =
                    0d;

                foreach (var cohort in group)
                {
                    foodSupportFractionByCohortId[
                        cohort.Id] =
                        supportFraction;

                    if (_parameters
                            .MaximumRecruitmentRatePerDay <= 0 ||
                        supportFraction <= 0)
                    {
                        continue;
                    }

                    cohort.RecruitmentAccumulator +=
                        cohort.MemberCount *
                        _parameters
                            .MaximumRecruitmentRatePerDay *
                        elapsedDays *
                        supportFraction;

                    var consumedBiomassKilograms =
                        GrazingDemandKilograms(
                            cohort.MemberCount,
                            elapsedDays) *
                        supportFraction;

                    var recruitmentCapacity =
                        cohort.RecruitmentAccumulator;

                    var biomassPerGrazer =
                        _parameters.MaterialPerGrazer
                            .LiveBiomassKilogramsPerUnit;

                    if (biomassPerGrazer > 0)
                    {
                        recruitmentCapacity =
                            Math.Min(
                                recruitmentCapacity,
                                consumedBiomassKilograms /
                                biomassPerGrazer);
                    }

                    var nitrogenPerGrazer =
                        _parameters.MaterialPerGrazer
                            .LiveNitrogenKilogramsPerUnit;

                    if (nitrogenPerGrazer > 0)
                    {
                        var recruitmentPlantNitrogenRatio =
                            _plantNitrogenKilogramsPerKilogramLiveBiomass
                            ?? throw new InvalidOperationException(
                                "Nitrogen-bearing grazer recruitment requires an authoritative plant-tissue nitrogen policy.");

                        recruitmentCapacity =
                            Math.Min(
                                recruitmentCapacity,
                                consumedBiomassKilograms *
                                recruitmentPlantNitrogenRatio /
                                nitrogenPerGrazer);
                    }

                    var integerCapacity =
                        Math.Max(
                            0,
                            int.MaxValue -
                            (int)Math.Min(
                                int.MaxValue,
                                Math.Floor(
                                    cohort.MemberCount)));

                    var newMembers =
                        (int)Math.Floor(
                            Math.Min(
                                recruitmentCapacity,
                                integerCapacity));

                    if (newMembers <= 0)
                    {
                        continue;
                    }

                    var recruitedMaterial =
                        _parameters.MaterialPerGrazer
                            .ForUnits(
                                newMembers);

                    cohort.MemberCount +=
                        newMembers;

                    cohort.RecruitmentAccumulator -=
                        newMembers;

                    cohort.Material =
                        new OrganismMaterialState(
                            cohort.Material
                                .LiveBiomassKilograms +
                            recruitedMaterial
                                .LiveBiomassKilograms,
                            cohort.Material
                                .LiveNitrogenKilograms +
                            recruitedMaterial
                                .LiveNitrogenKilograms);

                    recruitedMembers +=
                        newMembers;

                    biomassAssimilatedKilograms +=
                        recruitedMaterial
                            .LiveBiomassKilograms;

                    nitrogenAssimilatedKilograms +=
                        recruitedMaterial
                            .LiveNitrogenKilograms;

                    groupAssimilatedBiomassKilograms +=
                        recruitedMaterial
                            .LiveBiomassKilograms;

                    groupAssimilatedNitrogenKilograms +=
                        recruitedMaterial
                            .LiveNitrogenKilograms;
                }

                if (_plantNitrogenKilogramsPerKilogramLiveBiomass
                        is double plantNitrogenRatio)
                {
                    feedingMaterialTransfers.Add(
                        new VegetationFeedingEvent(
                            cellId,
                            grazedKilograms,
                            groupAssimilatedBiomassKilograms,
                            groupAssimilatedNitrogenKilograms));

                    nitrogenReturnedKilograms +=
                        grazedKilograms *
                        plantNitrogenRatio -
                        groupAssimilatedNitrogenKilograms;
                }
            }

            foreach (var cohort in cohorts)
            {
                var memberCountBeforeMortality =
                    cohort.MemberCount;

                var assessment =
                    assessmentsByCohortId[
                        cohort.Id];

                var foodSupportFraction =
                    foodSupportFractionByCohortId[
                        cohort.Id];

                if (foodSupportFraction < 1)
                {
                    var supportedMembers =
                        cohort.MemberCount *
                        foodSupportFraction;

                    var unsupportedMembers =
                        cohort.MemberCount -
                        supportedMembers;

                    cohort.MemberCount =
                        supportedMembers +
                        unsupportedMembers *
                        SurvivalFraction(
                            _parameters
                                .FoodShortageMortalityRatePerDay,
                            elapsedDays);

                    foodStressSteps++;
                }

                if (_parameters
                        .WaterAbsenceMortalityRatePerDay > 0 &&
                    !assessment.HasSurfaceWater)
                {
                    cohort.MemberCount *=
                        SurvivalFraction(
                            _parameters
                                .WaterAbsenceMortalityRatePerDay,
                            elapsedDays);

                    waterStressSteps++;
                }

                if (!assessment.HasVegetatedHabitat)
                {
                    cohort.MemberCount *=
                        SurvivalFraction(
                            _parameters
                                .HabitatAbsenceMortalityRatePerDay,
                            elapsedDays);

                    habitatStressSteps++;
                }

                var removedMembers =
                    Math.Max(
                        0,
                        memberCountBeforeMortality -
                        cohort.MemberCount);

                if (removedMembers > 0 &&
                    memberCountBeforeMortality > 0)
                {
                    var removedFraction =
                        Math.Clamp(
                            removedMembers /
                            memberCountBeforeMortality,
                            0,
                            1);

                    mortalityDeposits.Add(
                        OrganismMortalityDeposit
                            .FromRemovedFraction(
                                cohort.LatitudeDegrees,
                                cohort.LongitudeDegrees,
                                cohort.Material,
                                removedFraction));

                    var retainedFraction =
                        1 -
                        removedFraction;

                    cohort.Material =
                        cohort.Material
                            .RetainFraction(
                                retainedFraction);

                    cohort.RecruitmentAccumulator *=
                        retainedFraction;
                }
            }

            foreach (var extinct in
                     cohorts.Where(
                         cohort =>
                             cohort.MemberCount < 1))
            {
                if (!extinct.Material.IsEmpty)
                {
                    mortalityDeposits.Add(
                        OrganismMortalityDeposit
                            .FromRemovedFraction(
                                extinct.LatitudeDegrees,
                                extinct.LongitudeDegrees,
                                extinct.Material,
                                1));
                }
            }

            cohorts.RemoveAll(
                cohort =>
                    cohort.MemberCount <
                    1);

            integrationSubsteps++;
            remainingSeconds -=
                stepSeconds;
        }

        var finalCohorts =
            cohorts
                .Select(
                    cohort =>
                    {
                        var survivingMembers =
                            checked(
                                (int)Math.Floor(
                                    cohort.MemberCount));

                        var roundingLoss =
                            Math.Max(
                                0,
                                cohort.MemberCount -
                                survivingMembers);

                        if (roundingLoss > 0 &&
                            cohort.MemberCount > 0)
                        {
                            var removedFraction =
                                Math.Clamp(
                                    roundingLoss /
                                    cohort.MemberCount,
                                    0,
                                    1);

                            mortalityDeposits.Add(
                                OrganismMortalityDeposit
                                    .FromRemovedFraction(
                                        cohort.LatitudeDegrees,
                                        cohort.LongitudeDegrees,
                                        cohort.Material,
                                        removedFraction));

                            var retainedFraction =
                                1 -
                                removedFraction;

                            cohort.Material =
                                cohort.Material
                                    .RetainFraction(
                                        retainedFraction);

                            cohort.RecruitmentAccumulator *=
                                retainedFraction;
                        }

                        return new GrazerCohortState(
                            cohort.Id,
                            cohort.Source.PlanetId,
                            survivingMembers,
                            cohort.LatitudeDegrees,
                            cohort.LongitudeDegrees,
                            cohort.Material,
                            cohort.RecruitmentAccumulator);
                    })
                .ToArray();

        var finalVegetation =
            new PlanetVegetationState(
                vegetation.PlanetId,
                vegetation.GridDefinition,
                vegetation.Cells.Select(
                    original =>
                        new VegetationCellState(
                            original.CellId,
                            vegetationMassByCellId[
                                original.CellId] /
                            surfaceCellsById[
                                original.CellId]
                            .AreaSquareMeters)));

        var finalMemberCount =
            finalCohorts.Sum(
                cohort =>
                    (long)cohort.MemberCount);

        var finalVegetationMass =
            vegetationMassByCellId.Values.Sum();

        PlanetBiogeochemistryState?
            nextBiogeochemistry = null;

        if (feedingMaterialTransfers.Count > 0)
        {
            if (biogeochemistry is null ||
                _plantNitrogenKilogramsPerKilogramLiveBiomass
                    is not double plantNitrogenRatio)
            {
                throw new InvalidOperationException(
                    "Nitrogen-coupled grazer grazing requires authoritative biogeochemistry and plant-tissue nitrogen policy.");
            }

            nextBiogeochemistry =
                VegetationFeedingMaterialTransfer
                    .ReturnConsumedNitrogen(
                        planet,
                        biogeochemistry,
                        feedingMaterialTransfers,
                        plantNitrogenRatio);
        }

        if (mortalityDeposits.Any(
                deposit =>
                    !deposit.Material.IsEmpty))
        {
            var mortalityBiogeochemistry =
                nextBiogeochemistry ??
                biogeochemistry ??
                throw new InvalidOperationException(
                    "Material-bearing grazer mortality requires authoritative biogeochemistry state for the target planet.");

            nextBiogeochemistry =
                OrganismMortalityDetritusTransfer.Apply(
                    planet,
                    mortalityBiogeochemistry,
                    mortalityDeposits);
        }

        return new SimulationChange(
            new ReplacePlanetGrazerVegetationStateOperation(
                _planetId,
                finalCohorts,
                finalVegetation,
                nextBiogeochemistry),
            "planetary-grazers",
            "Grazer movement, grazing, and survival changed cohorts and live plant biomass.",
            _planetId,
            elapsedSeconds,
            new Dictionary<string, double>
            {
                ["initialCohorts"] =
                    initialCohortCount,
                ["finalCohorts"] =
                    finalCohorts.Length,
                ["extinctCohorts"] =
                    initialCohortCount -
                    finalCohorts.Length,
                ["initialMembers"] =
                    initialMemberCount,
                ["finalMembers"] =
                    finalMemberCount,
                ["memberChange"] =
                    finalMemberCount -
                    initialMemberCount,
                ["initialLiveVegetationKilograms"] =
                    initialVegetationMass,
                ["finalLiveVegetationKilograms"] =
                    finalVegetationMass,
                ["biomassGrazedKilograms"] =
                    biomassGrazedKilograms,
                ["recruitedMembers"] =
                    recruitedMembers,
                ["biomassAssimilatedKilograms"] =
                    biomassAssimilatedKilograms,
                ["biomassRespiredKilograms"] =
                    Math.Max(
                        0,
                        biomassGrazedKilograms -
                        biomassAssimilatedKilograms),
                ["nitrogenAssimilatedKilograms"] =
                    nitrogenAssimilatedKilograms,
                ["nitrogenReturnedKilograms"] =
                    nitrogenReturnedKilograms,
                ["movementSteps"] =
                    movementSteps,
                ["grazingSteps"] =
                    grazingSteps,
                ["foodStressSteps"] =
                    foodStressSteps,
                ["waterStressSteps"] =
                    waterStressSteps,
                ["habitatStressSteps"] =
                    habitatStressSteps,
                ["integrationSubsteps"] =
                    integrationSubsteps
            });
    }

    private CellAssessment SelectTargetCell(
        SurfaceCell currentCell,
        double cohortMemberCount,
        IPlanetSurfaceGrid grid,
        PlanetHydrologyState hydrology,
        IReadOnlyDictionary<
            SurfaceCellId,
            double> vegetationMassByCellId)
    {
        var current =
            AssessCell(
                currentCell,
                hydrology,
                vegetationMassByCellId);

        if (current.HasVegetatedHabitat &&
            (!_parameters.UseSurfaceWaterForMovement ||
             current.HasSurfaceWater) &&
            current.SupportCapacityGrazers >=
                cohortMemberCount)
        {
            return current;
        }

        var best =
            current;

        foreach (var neighborId in
                 grid.GetNeighbors(
                     currentCell.Id))
        {
            var candidate =
                AssessCell(
                    grid.GetCell(
                        neighborId),
                    hydrology,
                    vegetationMassByCellId);

            if (IsBetter(
                    candidate,
                    best,
                    cohortMemberCount))
            {
                best =
                    candidate;
            }
        }

        return best;
    }

    private CellAssessment AssessCell(
        SurfaceCell cell,
        PlanetHydrologyState hydrology,
        IReadOnlyDictionary<
            SurfaceCellId,
            double> vegetationMassByCellId)
    {
        var hydrologyCell =
            hydrology.GetCell(
                cell.Id);

        var vegetationMassKilograms =
            vegetationMassByCellId[
                cell.Id];

        var supportCapacityGrazers =
            vegetationMassKilograms *
            _parameters
                .CarryingCapacityGrazersPerKilogramLiveVegetationBiomass;

        if (!double.IsFinite(
                supportCapacityGrazers))
        {
            throw new InvalidOperationException(
                "Grazer ecological support produced a non-finite carrying capacity.");
        }

        return new CellAssessment(
            cell,
            vegetationMassKilograms > 0,
            hydrologyCell
                .SurfaceLiquidWaterKilogramsPerSquareMeter >
                0,
            supportCapacityGrazers);
    }

    private bool IsBetter(
        CellAssessment candidate,
        CellAssessment currentBest,
        double cohortMemberCount)
    {
        if (candidate.HasVegetatedHabitat !=
            currentBest.HasVegetatedHabitat)
        {
            return candidate.HasVegetatedHabitat;
        }

        if (_parameters.UseSurfaceWaterForMovement &&
            candidate.HasSurfaceWater !=
                currentBest.HasSurfaceWater)
        {
            return candidate.HasSurfaceWater;
        }

        var candidateSupportedMembers =
            Math.Min(
                cohortMemberCount,
                candidate.SupportCapacityGrazers);

        var currentSupportedMembers =
            Math.Min(
                cohortMemberCount,
                currentBest.SupportCapacityGrazers);

        if (candidateSupportedMembers !=
            currentSupportedMembers)
        {
            return candidateSupportedMembers >
                   currentSupportedMembers;
        }

        if (candidate.SupportCapacityGrazers !=
            currentBest.SupportCapacityGrazers)
        {
            return candidate.SupportCapacityGrazers >
                   currentBest.SupportCapacityGrazers;
        }

        return candidate.Cell.Id.Value.CompareTo(
                   currentBest.Cell.Id.Value) <
               0;
    }

    private double GrazingDemandKilograms(
        double memberCount,
        double elapsedDays)
    {
        return memberCount *
               _parameters
                   .MaximumGrazeKilogramsPerGrazerPerDay *
               elapsedDays;
    }

    private static double SurvivalFraction(
        double mortalityRatePerDay,
        double elapsedDays)
    {
        return Math.Exp(
            -mortalityRatePerDay *
            elapsedDays);
    }

    private static Position MoveToward(
        PlanetState planet,
        double sourceLatitudeDegrees,
        double sourceLongitudeDegrees,
        double targetLatitudeDegrees,
        double targetLongitudeDegrees,
        double maximumTravelMeters)
    {
        if (maximumTravelMeters <= 0)
        {
            return new Position(
                sourceLatitudeDegrees,
                sourceLongitudeDegrees);
        }

        var sourceLatitude =
            DegreesToRadians(
                sourceLatitudeDegrees);

        var sourceLongitude =
            DegreesToRadians(
                sourceLongitudeDegrees);

        var targetLatitude =
            DegreesToRadians(
                targetLatitudeDegrees);

        var targetLongitude =
            DegreesToRadians(
                targetLongitudeDegrees);

        var longitudeDelta =
            targetLongitude -
            sourceLongitude;

        var haversine =
            Math.Pow(
                Math.Sin(
                    (targetLatitude -
                     sourceLatitude) /
                    2),
                2) +
            Math.Cos(
                sourceLatitude) *
            Math.Cos(
                targetLatitude) *
            Math.Pow(
                Math.Sin(
                    longitudeDelta /
                    2),
                2);

        var angularDistance =
            2 *
            Math.Asin(
                Math.Min(
                    1,
                    Math.Sqrt(
                        Math.Max(
                            0,
                            haversine))));

        if (angularDistance <= 0)
        {
            return new Position(
                targetLatitudeDegrees,
                targetLongitudeDegrees);
        }

        var targetDistanceMeters =
            angularDistance *
            planet.MeanRadiusMeters;

        if (maximumTravelMeters >=
            targetDistanceMeters)
        {
            return new Position(
                targetLatitudeDegrees,
                targetLongitudeDegrees);
        }

        var travelAngle =
            maximumTravelMeters /
            planet.MeanRadiusMeters;

        var bearing =
            Math.Atan2(
                Math.Sin(
                    longitudeDelta) *
                Math.Cos(
                    targetLatitude),
                Math.Cos(
                    sourceLatitude) *
                Math.Sin(
                    targetLatitude) -
                Math.Sin(
                    sourceLatitude) *
                Math.Cos(
                    targetLatitude) *
                Math.Cos(
                    longitudeDelta));

        var destinationLatitude =
            Math.Asin(
                Math.Sin(
                    sourceLatitude) *
                Math.Cos(
                    travelAngle) +
                Math.Cos(
                    sourceLatitude) *
                Math.Sin(
                    travelAngle) *
                Math.Cos(
                    bearing));

        var destinationLongitude =
            sourceLongitude +
            Math.Atan2(
                Math.Sin(
                    bearing) *
                Math.Sin(
                    travelAngle) *
                Math.Cos(
                    sourceLatitude),
                Math.Cos(
                    travelAngle) -
                Math.Sin(
                    sourceLatitude) *
                Math.Sin(
                    destinationLatitude));

        return new Position(
            RadiansToDegrees(
                destinationLatitude),
            WrapLongitude(
                RadiansToDegrees(
                    destinationLongitude)));
    }

    private static double DegreesToRadians(
        double degrees)
    {
        return degrees *
               Math.PI /
               180d;
    }

    private static double RadiansToDegrees(
        double radians)
    {
        return radians *
               180d /
               Math.PI;
    }

    private static double WrapLongitude(
        double longitudeDegrees)
    {
        return
            ((longitudeDegrees + 540) % 360) -
            180;
    }

    private sealed class RuntimeCohort
    {
        public RuntimeCohort(
            GrazerCohortState source)
        {
            ArgumentNullException.ThrowIfNull(
                source);

            Source =
                source;

            Id =
                source.Id;

            LatitudeDegrees =
                source.LatitudeDegrees;

            LongitudeDegrees =
                source.LongitudeDegrees;

            MemberCount =
                source.MemberCount;

            Material =
                source.Material;

            RecruitmentAccumulator =
                source.RecruitmentAccumulator;
        }

        public GrazerCohortState Source { get; }

        public GrazerCohortId Id { get; }

        public double LatitudeDegrees { get; set; }

        public double LongitudeDegrees { get; set; }

        public double MemberCount { get; set; }

        public OrganismMaterialState Material { get; set; }

        public double RecruitmentAccumulator { get; set; }
    }

    private sealed record CellAssessment(
        SurfaceCell Cell,
        bool HasVegetatedHabitat,
        bool HasSurfaceWater,
        double SupportCapacityGrazers);

    private readonly record struct Position(
        double LatitudeDegrees,
        double LongitudeDegrees);
}
