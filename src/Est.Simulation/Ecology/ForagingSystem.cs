using Est.Simulation.Causality;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Surface;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Ecology;

public sealed class ForagingSystem : ICausalSystem
{
    private const double HungerThreshold = 0.6;
    private const double DefaultSearchRadiusDegrees = 1;
    private const double FoodSeekingRadiusDegrees = 5;
    private const double ScarcityMigrationRadiusDegrees = 30;
    private const double TravelDegreesPerDay = 0.25;
    private const long MaximumIntegrationStepSeconds = 86_400;

    private readonly PlanetId _planetId;
    private readonly double _searchRadiusDegrees;
    private readonly VegetationForagingParameters _vegetationForaging;

    public ForagingSystem(
        PlanetId planetId,
        VegetationForagingParameters vegetationForaging,
        double searchRadiusDegrees =
            DefaultSearchRadiusDegrees)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(
            vegetationForaging);

        if (!double.IsFinite(searchRadiusDegrees) ||
            searchRadiusDegrees < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(searchRadiusDegrees));
        }

        _planetId = planetId;
        _searchRadiusDegrees =
            searchRadiusDegrees;
        _vegetationForaging =
            vegetationForaging;
    }

    public SimulationChange Evaluate(
        WorldState world,
        long elapsedSeconds)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (elapsedSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elapsedSeconds));
        }

        if (!world.Planets.Any(
                planet => planet.Id == _planetId))
        {
            throw new PlanetNotFoundException(
                _planetId);
        }

        return EvaluateVegetation(
            world,
            elapsedSeconds,
            _vegetationForaging);
    }

    private SimulationChange EvaluateVegetation(
        WorldState world,
        long elapsedSeconds,
        VegetationForagingParameters parameters)
    {
        var planet =
            world.Planets.Single(
                candidate =>
                    candidate.Id == _planetId);

        var vegetation =
            world.Vegetation.SingleOrDefault(
                state =>
                    state.PlanetId == _planetId)
            ?? throw new InvalidOperationException(
                "Vegetation foraging requires authoritative vegetation state for the target planet.");

        vegetation.ValidateFor(planet);

        var surfaceGrid =
            PlanetSurfaceGridFactory.Create(
                planet,
                vegetation.GridDefinition);

        var vegetationByCell =
            vegetation.Cells.ToDictionary(
                cell => cell.CellId);

        var population =
            world.Population
                .Where(
                    person =>
                        person.PlanetId == _planetId)
                .OrderBy(person => person.Id.Value)
                .ToList();

        var foragingAttempts = 0;
        var feedingEvents = 0;
        var travelFeedingEvents = 0;
        var continuedFoodTravel = 0;
        var starvationDeaths = 0;
        var depletedVegetationCells = 0;
        var foodSeekingTravel = 0;
        var scarcityMigrations = 0;
        var noViableFoodFound = 0;
        var biomassHarvestedKilograms = 0d;
        var reserveEnergyGained = 0d;

        var remainingSeconds = elapsedSeconds;

        while (remainingSeconds > 0)
        {
            var stepSeconds =
                Math.Min(
                    MaximumIntegrationStepSeconds,
                    remainingSeconds);

            var elapsedDays =
                stepSeconds / 86_400d;

            var survivors =
                new List<PersonState>(
                    population.Count);

            foreach (var person in population)
            {
                var current = person;
                var fedThisStep = false;
                var traveledThisStep = false;

                if (current.Needs.EnergyReserve <
                    HungerThreshold)
                {
                    foragingAttempts++;

                    var minimumLocalBiomass =
                        PersonNeedsState
                            .EnergyConsumedPerDay *
                        parameters
                            .KilogramsLiveBiomassPerEnergyReserveUnit;

                    var occupiedCell =
                        surfaceGrid.LocateCell(
                            current.LatitudeDegrees,
                            current.LongitudeDegrees);

                    var occupiedVegetation =
                        vegetationByCell[
                            occupiedCell.Id];

                    var occupiedBiomass =
                        occupiedVegetation
                            .LiveBiomassKilogramsPerSquareMeter *
                        occupiedCell.AreaSquareMeters;

                    SurfaceCell? source =
                        occupiedBiomass >= minimumLocalBiomass
                            ? occupiedCell
                            : FindNearestHarvestableCell(
                                current,
                                surfaceGrid.Cells,
                                vegetationByCell,
                                _searchRadiusDegrees,
                                minimumLocalBiomass);

                    if (source is not null)
                    {
                        var vegetationCell =
                            vegetationByCell[
                                source.Id];

                        var availableBiomass =
                            vegetationCell
                                .LiveBiomassKilogramsPerSquareMeter *
                            source.AreaSquareMeters;

                        var reserveNeeded =
                            1 -
                            current.Needs.EnergyReserve;

                        var biomassNeeded =
                            reserveNeeded *
                            parameters
                                .KilogramsLiveBiomassPerEnergyReserveUnit;

                        var maximumHarvest =
                            parameters
                                .MaximumHarvestKilogramsPerPersonPerDay *
                            elapsedDays;

                        var harvestedBiomass =
                            Math.Min(
                                availableBiomass,
                                Math.Min(
                                    biomassNeeded,
                                    maximumHarvest));

                        if (harvestedBiomass > 0)
                        {
                            var wasTraveling =
                                current.Activity ==
                                PersonActivity.Traveling;

                            var remainingBiomass =
                                Math.Max(
                                    0,
                                    availableBiomass -
                                    harvestedBiomass);

                            vegetationByCell[
                                source.Id] =
                                new VegetationCellState(
                                    source.Id,
                                    remainingBiomass /
                                    source.AreaSquareMeters);

                            if (availableBiomass > 0 &&
                                remainingBiomass == 0)
                            {
                                depletedVegetationCells++;
                            }

                            var reserveGained =
                                harvestedBiomass /
                                parameters
                                    .KilogramsLiveBiomassPerEnergyReserveUnit;

                            current =
                                current.WithSurvivalState(
                                    current.Needs
                                        .WithEnergyReserve(
                                            Math.Min(
                                                1,
                                                current.Needs
                                                    .EnergyReserve +
                                                reserveGained)),
                                    PersonActivity.Eating);

                            feedingEvents++;

                            if (wasTraveling)
                            {
                                travelFeedingEvents++;
                            }

                            biomassHarvestedKilograms +=
                                harvestedBiomass;

                            reserveEnergyGained +=
                                reserveGained;

                            fedThisStep = true;
                        }
                    }

                    if (!fedThisStep)
                    {
                        var minimumDestinationBiomass =
                            PersonNeedsState
                                .EnergyConsumedPerDay *
                            parameters
                                .KilogramsLiveBiomassPerEnergyReserveUnit;

                        var destination =
                            FindNearestHarvestableCell(
                                current,
                                surfaceGrid.Cells,
                                vegetationByCell,
                                FoodSeekingRadiusDegrees,
                                minimumDestinationBiomass);

                        if (destination is not null)
                        {
                            foodSeekingTravel++;

                            if (current.Activity ==
                                PersonActivity.Traveling)
                            {
                                continuedFoodTravel++;
                            }
                        }
                        else
                        {
                            destination =
                                FindNearestHarvestableCell(
                                    current,
                                    surfaceGrid.Cells,
                                    vegetationByCell,
                                    ScarcityMigrationRadiusDegrees,
                                    minimumDestinationBiomass);

                            if (destination is not null)
                            {
                                scarcityMigrations++;
                            }
                            else
                            {
                                noViableFoodFound++;
                            }
                        }

                        if (destination is not null)
                        {
                            current =
                                MoveTowardCell(
                                    current,
                                    destination,
                                    elapsedDays);

                            traveledThisStep = true;
                        }
                        else
                        {
                            current =
                                current.WithSurvivalState(
                                    current.Needs,
                                    PersonActivity.Foraging);
                        }
                    }
                }

                var needs =
                    current.Needs.AdvanceWithoutFood(
                        elapsedDays);

                if (needs.Health <= 0)
                {
                    starvationDeaths++;
                    continue;
                }

                var activity =
                    fedThisStep
                        ? PersonActivity.Eating
                        : traveledThisStep
                            ? PersonActivity.Traveling
                            : needs.EnergyReserve <
                                HungerThreshold
                                ? PersonActivity.Foraging
                                : PersonActivity.Idle;

                survivors.Add(
                    current.WithSurvivalState(
                        needs,
                        activity));
            }

            population = survivors;
            remainingSeconds -= stepSeconds;
        }

        var updatedVegetation =
            new PlanetVegetationState(
                _planetId,
                vegetation.GridDefinition,
                vegetation.Cells.Select(
                    original =>
                        vegetationByCell[
                            original.CellId]));

        var operation =
            new ReplacePlanetVegetationForagingStateOperation(
                _planetId,
                population,
                updatedVegetation);

        return new SimulationChange(
            operation,
            "vegetation-foraging",
            "Survival needs and authoritative plant biomass changed.",
            _planetId,
            elapsedSeconds,
            new Dictionary<string, double>
            {
                ["foragingAttempts"] = foragingAttempts,
                ["feedingEvents"] = feedingEvents,
                ["travelFeedingEvents"] =
                    travelFeedingEvents,
                ["continuedFoodTravel"] =
                    continuedFoodTravel,
                ["energyConsumed"] =
                    reserveEnergyGained,
                ["biomassHarvestedKilograms"] =
                    biomassHarvestedKilograms,
                ["depletedVegetationCells"] =
                    depletedVegetationCells,
                ["foodSeekingTravel"] =
                    foodSeekingTravel,
                ["scarcityMigrations"] =
                    scarcityMigrations,
                ["noViableFoodFound"] =
                    noViableFoodFound,
                ["starvationDeaths"] =
                    starvationDeaths,
                ["survivors"] = population.Count
            });
    }

    private static SurfaceCell?
        FindNearestHarvestableCell(
            PersonState person,
            IEnumerable<SurfaceCell> surfaceCells,
            IReadOnlyDictionary<
                SurfaceCellId,
                VegetationCellState> vegetationByCell,
            double searchRadiusDegrees,
            double minimumAvailableBiomassKilograms)
    {
        if (!double.IsFinite(
                minimumAvailableBiomassKilograms) ||
            minimumAvailableBiomassKilograms < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    minimumAvailableBiomassKilograms));
        }

        SurfaceCell? nearest = null;
        var nearestDistanceSquared =
            double.PositiveInfinity;

        foreach (var surfaceCell in surfaceCells)
        {
            var vegetationCell =
                vegetationByCell[
                    surfaceCell.Id];

            var availableBiomass =
                vegetationCell
                    .LiveBiomassKilogramsPerSquareMeter *
                surfaceCell.AreaSquareMeters;

            if (availableBiomass <= 0 ||
                availableBiomass <
                    minimumAvailableBiomassKilograms)
            {
                continue;
            }

            var latitudeDelta =
                surfaceCell.CenterLatitudeDegrees -
                person.LatitudeDegrees;

            var longitudeDelta =
                SignedLongitudeDelta(
                    surfaceCell.CenterLongitudeDegrees,
                    person.LongitudeDegrees);

            var distanceSquared =
                latitudeDelta * latitudeDelta +
                longitudeDelta * longitudeDelta;

            if (distanceSquared >
                searchRadiusDegrees *
                searchRadiusDegrees)
            {
                continue;
            }

            if (distanceSquared <
                nearestDistanceSquared)
            {
                nearest = surfaceCell;
                nearestDistanceSquared =
                    distanceSquared;
            }
        }

        return nearest;
    }

    private static PersonState MoveTowardCell(
        PersonState person,
        SurfaceCell cell,
        double elapsedDays)
    {
        return MoveToward(
            person,
            cell.CenterLatitudeDegrees,
            cell.CenterLongitudeDegrees,
            elapsedDays);
    }

    private static PersonState MoveToward(
        PersonState person,
        double targetLatitudeDegrees,
        double targetLongitudeDegrees,
        double elapsedDays)
    {
        var latitudeDelta =
            targetLatitudeDegrees -
            person.LatitudeDegrees;

        var longitudeDelta =
            SignedLongitudeDelta(
                targetLongitudeDegrees,
                person.LongitudeDegrees);

        var distance =
            Math.Sqrt(
                latitudeDelta * latitudeDelta +
                longitudeDelta * longitudeDelta);

        if (distance <= 0)
        {
            return person;
        }

        var travelDistance =
            Math.Min(
                distance,
                TravelDegreesPerDay * elapsedDays);

        var fraction =
            travelDistance / distance;

        var latitude =
            Math.Clamp(
                person.LatitudeDegrees +
                    latitudeDelta * fraction,
                -89.999,
                89.999);

        var longitude =
            WrapLongitude(
                person.LongitudeDegrees +
                    longitudeDelta * fraction);

        return person.MoveTo(
            latitude,
            longitude);
    }

    private static double SignedLongitudeDelta(
        double target,
        double source)
    {
        return
            ((target - source + 540) % 360)
            - 180;
    }

    private static double WrapLongitude(
        double longitude)
    {
        return
            ((longitude + 540) % 360)
            - 180;
    }
}
