using Est.Simulation.Organisms;
using Est.Simulation.Biogeochemistry;
using Est.Simulation.Causality;
using Est.Simulation.Hydrology;
using Est.Simulation.Invertebrates;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Birds;

/// <summary>
/// Causal flock movement, prey consumption, recruitment, and survival.
///
/// Flocks use authoritative surface topology to seek locally better ecological
/// conditions. Surface liquid water is a water-availability constraint, live
/// vegetation is a coarse habitat-presence signal, and aggregate invertebrate
/// biomass provides both local population support and consumable prey.
///
/// Recruitment is material-backed and consumes authoritative prey biomass and
/// nitrogen. The system does not split or merge flocks or materialize
/// individual birds.
/// </summary>
public sealed class BirdFlockSystem
    : ICausalSystem
{
    private const double SecondsPerDay =
        86_400d;

    private readonly PlanetId _planetId;
    private readonly BirdModelParameters _parameters;

    public BirdFlockSystem(
        PlanetId planetId,
        BirdModelParameters parameters)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(
            parameters);

        _planetId =
            planetId;

        _parameters =
            parameters;
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
                "Bird integration requires hydrology for the target planet.");

        var vegetation =
            world.Vegetation.FirstOrDefault(
                candidate =>
                    candidate.PlanetId ==
                    _planetId)
            ?? throw new InvalidOperationException(
                "Bird integration requires vegetation for the target planet.");

        var invertebrates =
            world.Invertebrates.FirstOrDefault(
                candidate =>
                    candidate.PlanetId ==
                    _planetId)
            ?? throw new InvalidOperationException(
                "Bird integration requires invertebrate state for the target planet.");

        var biogeochemistry =
            world.Biogeochemistry.FirstOrDefault(
                candidate =>
                    candidate.PlanetId ==
                    _planetId);

        if (biogeochemistry is not null &&
            biogeochemistry.GridDefinition !=
                invertebrates.GridDefinition)
        {
            throw new InvalidOperationException(
                "Bird mortality and biogeochemistry must use the same surface grid.");
        }

        if (hydrology.GridDefinition !=
                vegetation.GridDefinition ||
            hydrology.GridDefinition !=
                invertebrates.GridDefinition)
        {
            throw new InvalidOperationException(
                "Bird ecological inputs must use the same surface grid.");
        }

        hydrology.ValidateFor(
            planet);

        vegetation.ValidateFor(
            planet);

        invertebrates.ValidateFor(
            planet);

        biogeochemistry?.ValidateFor(
            planet);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                invertebrates.GridDefinition);

        var surfaceCellsById =
            grid.Cells.ToDictionary(
                cell =>
                    cell.Id);

        var invertebrateBiomassByCellId =
            invertebrates.Cells.ToDictionary(
                cell =>
                    cell.CellId,
                cell =>
                    cell.LiveBiomassKilogramsPerSquareMeter *
                    surfaceCellsById[
                        cell.CellId]
                        .AreaSquareMeters);

        var invertebrateNitrogenByCellId =
            invertebrates.Cells.ToDictionary(
                cell =>
                    cell.CellId,
                cell =>
                    cell.LiveNitrogenKilogramsPerSquareMeter *
                    surfaceCellsById[
                        cell.CellId]
                        .AreaSquareMeters);

        var flocks =
            world.BirdFlocks
                .Where(
                    flock =>
                        flock.PlanetId ==
                        _planetId)
                .OrderBy(
                    flock =>
                        flock.Id.Value)
                .Select(
                    flock =>
                        new RuntimeFlock(
                            flock))
                .ToList();

        var mortalityDeposits =
            new List<OrganismMortalityDeposit>();

        var initialFlockCount =
            flocks.Count;

        var initialMemberCount =
            flocks.Sum(
                flock =>
                    flock.MemberCount);

        var movementSteps =
            0;

        var foodStressSteps =
            0;

        var waterStressSteps =
            0;

        var habitatStressSteps =
            0;

        var preyConsumptionSteps =
            0;

        var recruitedMembers =
            0L;

        var preyBiomassConsumedKilograms =
            0d;

        var preyNitrogenConsumedKilograms =
            0d;

        var preyBiomassAssimilatedKilograms =
            0d;

        var preyNitrogenAssimilatedKilograms =
            0d;

        var preyConsumptionEvents =
            new List<OrganismConsumptionEvent>();

        var integrationSubsteps =
            0;

        var remainingSeconds =
            elapsedSeconds;

        while (remainingSeconds > 0 &&
               flocks.Count > 0)
        {
            var stepSeconds =
                Math.Min(
                    remainingSeconds,
                    _parameters
                        .MaximumIntegrationStepSeconds);

            var elapsedDays =
                stepSeconds /
                SecondsPerDay;

            var regionalCellBudget =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        (double)grid.CellCount /
                        _parameters.MaximumInitialFlockCount));

            foreach (var flock in flocks)
            {
                var currentCell =
                    grid.LocateCell(
                        flock.LatitudeDegrees,
                        flock.LongitudeDegrees);

                var target =
                    SelectTargetCell(
                        currentCell,
                        flock.MemberCount,
                        grid,
                        hydrology,
                        vegetation,
                        invertebrateBiomassByCellId,
                        regionalCellBudget);

                if (target.Cell.Id ==
                    currentCell.Id)
                {
                    continue;
                }

                var moved =
                    MoveToward(
                        planet,
                        flock.LatitudeDegrees,
                        flock.LongitudeDegrees,
                        target.Cell.CenterLatitudeDegrees,
                        target.Cell.CenterLongitudeDegrees,
                        _parameters
                            .MaximumTravelMetersPerDay *
                        elapsedDays);

                if (moved.LatitudeDegrees ==
                        flock.LatitudeDegrees &&
                    moved.LongitudeDegrees ==
                        flock.LongitudeDegrees)
                {
                    continue;
                }

                flock.LatitudeDegrees =
                    moved.LatitudeDegrees;

                flock.LongitudeDegrees =
                    moved.LongitudeDegrees;

                movementSteps++;
            }

            var occupiedCellsByFlockId =
                flocks.ToDictionary(
                    flock =>
                        flock.Id,
                    flock =>
                        grid.LocateCell(
                            flock.LatitudeDegrees,
                            flock.LongitudeDegrees));

            var totalMembersByCellId =
                flocks
                    .GroupBy(
                        flock =>
                            occupiedCellsByFlockId[
                                flock.Id]
                            .Id)
                    .ToDictionary(
                        group =>
                            group.Key,
                        group =>
                            group.Sum(
                                flock =>
                                    flock.MemberCount));

            if (_parameters
                    .MaximumPreyConsumptionKilogramsPerBirdPerDay >
                0)
            {
                foreach (var group in
                         flocks
                             .GroupBy(
                                 flock =>
                                     occupiedCellsByFlockId[
                                         flock.Id]
                                         .Id)
                             .OrderBy(
                                 group =>
                                     group.Key.Value))
                {
                    var cellId =
                        group.Key;

                    var regionalCellIds =
                        GetRegionalCellIds(
                            grid.GetCell(
                                cellId),
                            grid,
                            regionalCellBudget);

                    var totalDemandKilograms =
                        group.Sum(
                            flock =>
                                PreyConsumptionDemandKilograms(
                                    flock.MemberCount,
                                    elapsedDays));

                    if (!double.IsFinite(
                            totalDemandKilograms))
                    {
                        throw new InvalidOperationException(
                            "Bird integration produced a non-finite prey-consumption demand.");
                    }

                    if (totalDemandKilograms <= 0)
                    {
                        continue;
                    }

                    var availableBiomassKilograms =
                        regionalCellIds.Sum(
                            regionalCellId =>
                                invertebrateBiomassByCellId[
                                    regionalCellId]);

                    if (!double.IsFinite(
                            availableBiomassKilograms))
                    {
                        throw new InvalidOperationException(
                            "Bird regional prey support produced non-finite biomass.");
                    }

                    var consumptionFraction =
                        availableBiomassKilograms <= 0
                            ? 0
                            : Math.Min(
                                1,
                                totalDemandKilograms /
                                availableBiomassKilograms);

                    var consumedBiomassKilograms =
                        0d;

                    var consumedNitrogenKilograms =
                        0d;

                    foreach (var regionalCellId in
                             regionalCellIds)
                    {
                        var cellBiomassKilograms =
                            invertebrateBiomassByCellId[
                                regionalCellId];

                        var cellNitrogenKilograms =
                            invertebrateNitrogenByCellId[
                                regionalCellId];

                        var cellConsumedBiomassKilograms =
                            cellBiomassKilograms *
                            consumptionFraction;

                        var cellConsumedNitrogenKilograms =
                            cellNitrogenKilograms *
                            consumptionFraction;

                        invertebrateBiomassByCellId[
                            regionalCellId] =
                            Math.Max(
                                0,
                                cellBiomassKilograms -
                                cellConsumedBiomassKilograms);

                        invertebrateNitrogenByCellId[
                            regionalCellId] =
                            Math.Max(
                                0,
                                cellNitrogenKilograms -
                                cellConsumedNitrogenKilograms);

                        consumedBiomassKilograms +=
                            cellConsumedBiomassKilograms;

                        consumedNitrogenKilograms +=
                            cellConsumedNitrogenKilograms;
                    }

                    preyBiomassConsumedKilograms +=
                        consumedBiomassKilograms;

                    preyNitrogenConsumedKilograms +=
                        consumedNitrogenKilograms;

                    if (consumedBiomassKilograms > 0)
                    {
                        preyConsumptionSteps++;
                    }

                    var supportFraction =
                        Math.Clamp(
                            consumedBiomassKilograms /
                            totalDemandKilograms,
                            0,
                            1);

                    foreach (var flock in group)
                    {
                        var flockDemandKilograms =
                            PreyConsumptionDemandKilograms(
                                flock.MemberCount,
                                elapsedDays);

                        var flockConsumedBiomassKilograms =
                            flockDemandKilograms *
                            supportFraction;

                        var flockConsumedNitrogenKilograms =
                            consumedBiomassKilograms <= 0
                                ? 0
                                : consumedNitrogenKilograms *
                                  flockConsumedBiomassKilograms /
                                  consumedBiomassKilograms;

                        if (_parameters
                                .MaximumRecruitmentRatePerDay >
                            0 &&
                            supportFraction >
                            0)
                        {
                            flock.RecruitmentAccumulator +=
                                flock.MemberCount *
                                _parameters
                                    .MaximumRecruitmentRatePerDay *
                                elapsedDays *
                                supportFraction;
                        }

                        var recruitmentCapacity =
                            flock.RecruitmentAccumulator;

                        var biomassPerBird =
                            _parameters.MaterialPerBird
                                .LiveBiomassKilogramsPerUnit;

                        if (biomassPerBird > 0)
                        {
                            recruitmentCapacity =
                                Math.Min(
                                    recruitmentCapacity,
                                    flockConsumedBiomassKilograms /
                                    biomassPerBird);
                        }

                        var nitrogenPerBird =
                            _parameters.MaterialPerBird
                                .LiveNitrogenKilogramsPerUnit;

                        if (nitrogenPerBird > 0)
                        {
                            recruitmentCapacity =
                                Math.Min(
                                    recruitmentCapacity,
                                    flockConsumedNitrogenKilograms /
                                    nitrogenPerBird);
                        }

                        var integerCapacity =
                            Math.Max(
                                0,
                                int.MaxValue -
                                (int)Math.Min(
                                    int.MaxValue,
                                    Math.Floor(
                                        flock.MemberCount)));

                        var newMembers =
                            (int)Math.Floor(
                                Math.Min(
                                    recruitmentCapacity,
                                    integerCapacity));

                        var recruitedMaterial =
                            _parameters.MaterialPerBird
                                .ForUnits(
                                    newMembers);

                        if (newMembers > 0)
                        {
                            flock.MemberCount +=
                                newMembers;

                            flock.RecruitmentAccumulator -=
                                newMembers;

                            flock.Material =
                                new OrganismMaterialState(
                                    flock.Material
                                        .LiveBiomassKilograms +
                                    recruitedMaterial
                                        .LiveBiomassKilograms,
                                    flock.Material
                                        .LiveNitrogenKilograms +
                                    recruitedMaterial
                                        .LiveNitrogenKilograms);

                            recruitedMembers +=
                                newMembers;

                            preyBiomassAssimilatedKilograms +=
                                recruitedMaterial
                                    .LiveBiomassKilograms;

                            preyNitrogenAssimilatedKilograms +=
                                recruitedMaterial
                                    .LiveNitrogenKilograms;
                        }

                        if (flockConsumedBiomassKilograms > 0)
                        {
                            preyConsumptionEvents.Add(
                                new OrganismConsumptionEvent(
                                    flock.LatitudeDegrees,
                                    flock.LongitudeDegrees,
                                    new OrganismMaterialState(
                                        flockConsumedBiomassKilograms,
                                        flockConsumedNitrogenKilograms),
                                    recruitedMaterial
                                        .LiveBiomassKilograms,
                                    recruitedMaterial
                                        .LiveNitrogenKilograms));
                        }
                    }
                }

                totalMembersByCellId =
                    flocks
                        .GroupBy(
                            flock =>
                                occupiedCellsByFlockId[
                                    flock.Id]
                                    .Id)
                        .ToDictionary(
                            group =>
                                group.Key,
                            group =>
                                group.Sum(
                                    flock =>
                                        flock.MemberCount));
            }

            foreach (var flock in flocks)
            {
                var memberCountBeforeMortality =
                    flock.MemberCount;

                var occupiedCell =
                    occupiedCellsByFlockId[
                        flock.Id];

                var occupiedAssessment =
                    AssessRegion(
                        occupiedCell,
                        grid,
                        hydrology,
                        vegetation,
                        invertebrateBiomassByCellId,
                        regionalCellBudget);

                var totalMembersInCell =
                    totalMembersByCellId[
                        occupiedCell.Id];

                if (totalMembersInCell >
                    occupiedAssessment.SupportCapacityBirds)
                {
                    var supportedFraction =
                        occupiedAssessment
                            .SupportCapacityBirds <= 0
                            ? 0
                            : Math.Min(
                                1,
                                occupiedAssessment
                                    .SupportCapacityBirds /
                                totalMembersInCell);

                    var supportedMembers =
                        flock.MemberCount *
                        supportedFraction;

                    var unsupportedMembers =
                        flock.MemberCount -
                        supportedMembers;

                    flock.MemberCount =
                        supportedMembers +
                        unsupportedMembers *
                        SurvivalFraction(
                            _parameters
                                .FoodShortageMortalityRatePerDay,
                            elapsedDays);

                    foodStressSteps++;
                }

                if (!occupiedAssessment.HasSurfaceWater)
                {
                    flock.MemberCount *=
                        SurvivalFraction(
                            _parameters
                                .WaterAbsenceMortalityRatePerDay,
                            elapsedDays);

                    waterStressSteps++;
                }

                if (!occupiedAssessment.HasVegetationHabitat)
                {
                    flock.MemberCount *=
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
                        flock.MemberCount);

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
                                flock.LatitudeDegrees,
                                flock.LongitudeDegrees,
                                flock.Material,
                                removedFraction));

                    var retainedFraction =
                        1 -
                        removedFraction;

                    flock.Material =
                        flock.Material
                            .RetainFraction(
                                retainedFraction);

                    flock.RecruitmentAccumulator *=
                        retainedFraction;
                }
            }

            foreach (var extinct in
                     flocks.Where(
                         flock =>
                             flock.MemberCount < 1))
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

            flocks.RemoveAll(
                flock =>
                    flock.MemberCount <
                    1);

            integrationSubsteps++;
            remainingSeconds -=
                stepSeconds;
        }

        var finalFlocks =
            flocks
                .Select(
                    flock =>
                    {
                        var survivingMembers =
                            checked(
                                (int)Math.Floor(
                                    flock.MemberCount));

                        var roundingLoss =
                            Math.Max(
                                0,
                                flock.MemberCount -
                                survivingMembers);

                        if (roundingLoss > 0 &&
                            flock.MemberCount > 0)
                        {
                            var removedFraction =
                                Math.Clamp(
                                    roundingLoss /
                                    flock.MemberCount,
                                    0,
                                    1);

                            mortalityDeposits.Add(
                                OrganismMortalityDeposit
                                    .FromRemovedFraction(
                                        flock.LatitudeDegrees,
                                        flock.LongitudeDegrees,
                                        flock.Material,
                                        removedFraction));

                            var retainedFraction =
                                1 -
                                removedFraction;

                            flock.Material =
                                flock.Material
                                    .RetainFraction(
                                        retainedFraction);

                            flock.RecruitmentAccumulator *=
                                retainedFraction;
                        }

                        return new BirdFlockState(
                            flock.Id,
                            flock.Source.PlanetId,
                            survivingMembers,
                            flock.LatitudeDegrees,
                            flock.LongitudeDegrees,
                            flock.Material,
                            flock.RecruitmentAccumulator);
                    })
                .ToArray();

        var finalInvertebrates =
            new PlanetInvertebrateState(
                invertebrates.PlanetId,
                invertebrates.GridDefinition,
                invertebrates.Cells.Select(
                    original =>
                        new InvertebrateCellState(
                            original.CellId,
                            invertebrateBiomassByCellId[
                                original.CellId] /
                            surfaceCellsById[
                                original.CellId]
                                .AreaSquareMeters,
                            invertebrateNitrogenByCellId[
                                original.CellId] /
                            surfaceCellsById[
                                original.CellId]
                                .AreaSquareMeters)));

        var finalMemberCount =
            finalFlocks.Sum(
                flock =>
                    (long)flock.MemberCount);

        PlanetBiogeochemistryState?
            nextBiogeochemistry = null;

        var returnedPreyNitrogenKilograms =
            preyConsumptionEvents.Sum(
                consumption =>
                    consumption.ReturnedNitrogenKilograms);

        if (returnedPreyNitrogenKilograms > 0)
        {
            if (biogeochemistry is null)
            {
                throw new InvalidOperationException(
                    "Nitrogen-bearing bird prey consumption requires authoritative biogeochemistry state for the target planet.");
            }

            nextBiogeochemistry =
                OrganismConsumptionMaterialTransfer
                    .ReturnConsumedNitrogen(
                        planet,
                        biogeochemistry,
                        preyConsumptionEvents);
        }

        if (mortalityDeposits.Any(
                deposit =>
                    !deposit.Material.IsEmpty))
        {
            if (biogeochemistry is null)
            {
                throw new InvalidOperationException(
                    "Material-bearing bird mortality requires authoritative biogeochemistry state for the target planet.");
            }

            var mortalityBiogeochemistry =
                nextBiogeochemistry ??
                biogeochemistry;

            nextBiogeochemistry =
                OrganismMortalityDetritusTransfer.Apply(
                    planet,
                    mortalityBiogeochemistry,
                    mortalityDeposits);
        }

        ISimulationOperation operation =
            preyBiomassConsumedKilograms > 0
                ? new ReplacePlanetBirdInvertebrateStateOperation(
                    _planetId,
                    finalFlocks,
                    finalInvertebrates,
                    nextBiogeochemistry)
                : new ReplacePlanetBirdFlocksOperation(
                    _planetId,
                    finalFlocks,
                    nextBiogeochemistry);

        return new SimulationChange(
            operation,
            "planetary-birds",
            "Bird flock movement, prey consumption, recruitment, and survival changed.",
            _planetId,
            elapsedSeconds,
            new Dictionary<string, double>
            {
                ["initialFlocks"] =
                    initialFlockCount,
                ["finalFlocks"] =
                    finalFlocks.Length,
                ["extinctFlocks"] =
                    initialFlockCount -
                    finalFlocks.Length,
                ["initialMembers"] =
                    initialMemberCount,
                ["finalMembers"] =
                    finalMemberCount,
                ["memberChange"] =
                    finalMemberCount -
                    initialMemberCount,
                ["recruitedMembers"] =
                    recruitedMembers,
                ["preyBiomassConsumedKilograms"] =
                    preyBiomassConsumedKilograms,
                ["preyNitrogenConsumedKilograms"] =
                    preyNitrogenConsumedKilograms,
                ["preyBiomassAssimilatedKilograms"] =
                    preyBiomassAssimilatedKilograms,
                ["preyNitrogenAssimilatedKilograms"] =
                    preyNitrogenAssimilatedKilograms,
                ["preyBiomassRespiredKilograms"] =
                    Math.Max(
                        0,
                        preyBiomassConsumedKilograms -
                        preyBiomassAssimilatedKilograms),
                ["preyNitrogenReturnedKilograms"] =
                    returnedPreyNitrogenKilograms,
                ["movementSteps"] =
                    movementSteps,
                ["foodStressSteps"] =
                    foodStressSteps,
                ["waterStressSteps"] =
                    waterStressSteps,
                ["habitatStressSteps"] =
                    habitatStressSteps,
                ["preyConsumptionSteps"] =
                    preyConsumptionSteps,
                ["integrationSubsteps"] =
                    integrationSubsteps
            });
    }

    private CellAssessment SelectTargetCell(
        SurfaceCell currentCell,
        double flockMemberCount,
        IPlanetSurfaceGrid grid,
        PlanetHydrologyState hydrology,
        PlanetVegetationState vegetation,
        IReadOnlyDictionary<SurfaceCellId, double>
            invertebrateBiomassByCellId,
        int regionalCellBudget)
    {
        var current =
            AssessRegion(
                currentCell,
                grid,
                hydrology,
                vegetation,
                invertebrateBiomassByCellId,
                regionalCellBudget);

        if (current.HasSurfaceWater &&
            current.HasVegetationHabitat &&
            current.SupportCapacityBirds >=
                flockMemberCount)
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
                AssessRegion(
                    grid.GetCell(
                        neighborId),
                    grid,
                    hydrology,
                    vegetation,
                    invertebrateBiomassByCellId,
                    regionalCellBudget);

            if (IsBetter(
                    candidate,
                    best,
                    flockMemberCount))
            {
                best =
                    candidate;
            }
        }

        return best;
    }

    private CellAssessment AssessRegion(
        SurfaceCell centerCell,
        IPlanetSurfaceGrid grid,
        PlanetHydrologyState hydrology,
        PlanetVegetationState vegetation,
        IReadOnlyDictionary<SurfaceCellId, double>
            invertebrateBiomassByCellId,
        int regionalCellBudget)
    {
        var hasSurfaceWater =
            false;

        var hasVegetationHabitat =
            false;

        var supportCapacityBirds =
            0d;

        foreach (var cellId in
                 GetRegionalCellIds(
                     centerCell,
                     grid,
                     regionalCellBudget))
        {
            var hydrologyCell =
                hydrology.GetCell(
                    cellId);

            var vegetationCell =
                vegetation.GetCell(
                    cellId);

            hasSurfaceWater |=
                hydrologyCell
                    .SurfaceLiquidWaterKilogramsPerSquareMeter >
                0;

            hasVegetationHabitat |=
                vegetationCell
                    .LiveBiomassKilogramsPerSquareMeter >
                0;

            supportCapacityBirds +=
                invertebrateBiomassByCellId[
                    cellId] *
                _parameters
                    .CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass;
        }

        if (!double.IsFinite(
                supportCapacityBirds))
        {
            throw new InvalidOperationException(
                "Bird regional ecological support produced a non-finite carrying capacity.");
        }

        return new CellAssessment(
            centerCell,
            hasSurfaceWater,
            hasVegetationHabitat,
            supportCapacityBirds);
    }

    private static IReadOnlyList<SurfaceCellId>
        GetRegionalCellIds(
            SurfaceCell centerCell,
            IPlanetSurfaceGrid grid,
            int regionalCellBudget)
    {
        if (regionalCellBudget <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(regionalCellBudget),
                "Bird regional cell budget must be positive.");
        }

        var visited =
            new HashSet<SurfaceCellId>
            {
                centerCell.Id
            };

        var queue =
            new Queue<SurfaceCellId>();

        var regionalCellIds =
            new List<SurfaceCellId>(
                Math.Min(
                    regionalCellBudget,
                    grid.CellCount));

        queue.Enqueue(
            centerCell.Id);

        while (queue.Count > 0 &&
               regionalCellIds.Count <
                   regionalCellBudget)
        {
            var cellId =
                queue.Dequeue();

            regionalCellIds.Add(
                cellId);

            foreach (var neighborId in
                     grid.GetNeighbors(
                         cellId))
            {
                if (visited.Add(
                        neighborId))
                {
                    queue.Enqueue(
                        neighborId);
                }
            }
        }

        return regionalCellIds;
    }

    private static bool IsBetter(
        CellAssessment candidate,
        CellAssessment currentBest,
        double flockMemberCount)
    {
        if (candidate.HasSurfaceWater !=
            currentBest.HasSurfaceWater)
        {
            return candidate.HasSurfaceWater;
        }

        if (candidate.HasVegetationHabitat !=
            currentBest.HasVegetationHabitat)
        {
            return candidate.HasVegetationHabitat;
        }

        var candidateSupportedMembers =
            Math.Min(
                flockMemberCount,
                candidate.SupportCapacityBirds);

        var currentSupportedMembers =
            Math.Min(
                flockMemberCount,
                currentBest.SupportCapacityBirds);

        if (candidateSupportedMembers !=
            currentSupportedMembers)
        {
            return candidateSupportedMembers >
                   currentSupportedMembers;
        }

        if (candidate.SupportCapacityBirds !=
            currentBest.SupportCapacityBirds)
        {
            return candidate.SupportCapacityBirds >
                   currentBest.SupportCapacityBirds;
        }

        return candidate.Cell.Id.Value.CompareTo(
                   currentBest.Cell.Id.Value) <
               0;
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

    private sealed class RuntimeFlock
    {
        public RuntimeFlock(
            BirdFlockState source)
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

        public BirdFlockState Source { get; }

        public BirdFlockId Id { get; }

        public double LatitudeDegrees { get; set; }

        public double LongitudeDegrees { get; set; }

        public double MemberCount { get; set; }

        public OrganismMaterialState Material { get; set; }

        public double RecruitmentAccumulator { get; set; }
    }

    private double PreyConsumptionDemandKilograms(
        double memberCount,
        double elapsedDays)
    {
        return
            memberCount *
            _parameters
                .MaximumPreyConsumptionKilogramsPerBirdPerDay *
            elapsedDays;
    }

    private sealed record CellAssessment(
        SurfaceCell Cell,
        bool HasSurfaceWater,
        bool HasVegetationHabitat,
        double SupportCapacityBirds);

    private readonly record struct Position(
        double LatitudeDegrees,
        double LongitudeDegrees);
}
