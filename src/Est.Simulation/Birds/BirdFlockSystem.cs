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
/// First-pass causal flock movement and survival.
///
/// Flocks use authoritative surface topology to seek locally better ecological
/// conditions. Surface liquid water is a water-availability constraint, live
/// vegetation is a coarse habitat-presence signal, and aggregate invertebrate
/// biomass establishes local population support.
///
/// This system does not consume invertebrate biomass, reproduce birds, split
/// or merge flocks, or materialize individual birds.
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
                        invertebrates);

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

            foreach (var flock in flocks)
            {
                var memberCountBeforeMortality =
                    flock.MemberCount;

                var occupiedCell =
                    occupiedCellsByFlockId[
                        flock.Id];

                var occupiedAssessment =
                    AssessCell(
                        occupiedCell,
                        hydrology,
                        vegetation,
                        invertebrates);

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

                if (removedMembers > 0)
                {
                    mortalityDeposits.Add(
                        OrganismMortalityDeposit
                            .FromRemovedFraction(
                                flock.LatitudeDegrees,
                                flock.LongitudeDegrees,
                                flock.Source.Material,
                                Math.Clamp(
                                    removedMembers /
                                    flock.Source.MemberCount,
                                    0,
                                    1)));
                }
            }

            foreach (var extinct in
                     flocks.Where(
                         flock =>
                             flock.MemberCount < 1))
            {
                mortalityDeposits.Add(
                    OrganismMortalityDeposit
                        .FromRemovedFraction(
                            extinct.LatitudeDegrees,
                            extinct.LongitudeDegrees,
                            extinct.Source.Material,
                            Math.Clamp(
                                extinct.MemberCount /
                                extinct.Source.MemberCount,
                                0,
                                1)));
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

                        if (roundingLoss > 0)
                        {
                            mortalityDeposits.Add(
                                OrganismMortalityDeposit
                                    .FromRemovedFraction(
                                        flock.LatitudeDegrees,
                                        flock.LongitudeDegrees,
                                        flock.Source.Material,
                                        Math.Clamp(
                                            roundingLoss /
                                            flock.Source.MemberCount,
                                            0,
                                            1)));
                        }

                        return flock.Source.WithSurvivalState(
                            survivingMembers,
                            flock.LatitudeDegrees,
                            flock.LongitudeDegrees);
                    })
                .ToArray();

        var finalMemberCount =
            finalFlocks.Sum(
                flock =>
                    (long)flock.MemberCount);

        PlanetBiogeochemistryState?
            nextBiogeochemistry = null;

        if (mortalityDeposits.Any(
                deposit =>
                    !deposit.Material.IsEmpty))
        {
            if (biogeochemistry is null)
            {
                throw new InvalidOperationException(
                    "Material-bearing bird mortality requires authoritative biogeochemistry state for the target planet.");
            }

            nextBiogeochemistry =
                OrganismMortalityDetritusTransfer.Apply(
                    planet,
                    biogeochemistry,
                    mortalityDeposits);
        }

        return new SimulationChange(
            new ReplacePlanetBirdFlocksOperation(
                _planetId,
                finalFlocks,
                nextBiogeochemistry),
            "planetary-birds",
            "Bird flock movement and survival changed.",
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
                ["movementSteps"] =
                    movementSteps,
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
        double flockMemberCount,
        IPlanetSurfaceGrid grid,
        PlanetHydrologyState hydrology,
        PlanetVegetationState vegetation,
        PlanetInvertebrateState invertebrates)
    {
        var current =
            AssessCell(
                currentCell,
                hydrology,
                vegetation,
                invertebrates);

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
                AssessCell(
                    grid.GetCell(
                        neighborId),
                    hydrology,
                    vegetation,
                    invertebrates);

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

    private CellAssessment AssessCell(
        SurfaceCell cell,
        PlanetHydrologyState hydrology,
        PlanetVegetationState vegetation,
        PlanetInvertebrateState invertebrates)
    {
        var hydrologyCell =
            hydrology.GetCell(
                cell.Id);

        var vegetationCell =
            vegetation.GetCell(
                cell.Id);

        var invertebrateCell =
            invertebrates.GetCell(
                cell.Id);

        var totalInvertebrateBiomassKilograms =
            invertebrateCell
                .LiveBiomassKilogramsPerSquareMeter *
            cell.AreaSquareMeters;

        var supportCapacityBirds =
            totalInvertebrateBiomassKilograms *
            _parameters
                .CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass;

        if (!double.IsFinite(
                supportCapacityBirds))
        {
            throw new InvalidOperationException(
                "Bird ecological support produced a non-finite carrying capacity.");
        }

        return new CellAssessment(
            cell,
            hydrologyCell
                .SurfaceLiquidWaterKilogramsPerSquareMeter >
                0,
            vegetationCell
                .LiveBiomassKilogramsPerSquareMeter >
                0,
            supportCapacityBirds);
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
        }

        public BirdFlockState Source { get; }

        public BirdFlockId Id { get; }

        public double LatitudeDegrees { get; set; }

        public double LongitudeDegrees { get; set; }

        public double MemberCount { get; set; }
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
