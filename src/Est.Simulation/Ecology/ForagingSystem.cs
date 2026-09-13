using Est.Simulation.Causality;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Population;
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

    public ForagingSystem(
        PlanetId planetId,
        double searchRadiusDegrees =
            DefaultSearchRadiusDegrees)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        if (!double.IsFinite(searchRadiusDegrees) ||
            searchRadiusDegrees < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(searchRadiusDegrees));
        }

        _planetId = planetId;
        _searchRadiusDegrees =
            searchRadiusDegrees;
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

        var population =
            world.Population
                .Where(
                    person =>
                        person.PlanetId == _planetId)
                .OrderBy(person => person.Id.Value)
                .ToList();

        var food =
            world.FoodResources
                .Where(
                    resource =>
                        resource.PlanetId == _planetId)
                .OrderBy(resource => resource.Id.Value)
                .ToDictionary(
                    resource => resource.Id);

        var foragingAttempts = 0;
        var feedingEvents = 0;
        var starvationDeaths = 0;
        var exhaustedResources = 0;
        var foodSeekingTravel = 0;
        var scarcityMigrations = 0;
        var noViableFoodFound = 0;
        var energyConsumed = 0d;
        var energyRecovered = 0d;

        var remainingSeconds = elapsedSeconds;

        while (remainingSeconds > 0)
        {
            var stepSeconds =
                Math.Min(
                    MaximumIntegrationStepSeconds,
                    remainingSeconds);

            var elapsedDays =
                stepSeconds / 86_400d;

            foreach (var resource in food.Values.ToArray())
            {
                var recovered =
                    resource.Recover(
                        elapsedDays);

                energyRecovered +=
                    recovered.AvailableEnergy -
                    resource.AvailableEnergy;

                food[resource.Id] =
                    recovered;
            }

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

                    var resource =
                        FindNearestAvailableResource(
                            current,
                            food.Values);

                    if (resource is not null)
                    {
                        var energyNeeded =
                            1 -
                            current.Needs.EnergyReserve;

                        var consumed =
                            Math.Min(
                                energyNeeded,
                                resource.AvailableEnergy);

                        if (consumed > 0)
                        {
                            var remainingResource =
                                resource.Consume(consumed);

                            food[resource.Id] =
                                remainingResource;

                            if (resource.AvailableEnergy > 0 &&
                                remainingResource
                                    .AvailableEnergy == 0)
                            {
                                exhaustedResources++;
                            }

                            current =
                                current.WithSurvivalState(
                                    current.Needs
                                        .WithEnergyReserve(
                                            current.Needs
                                                .EnergyReserve +
                                            consumed),
                                    PersonActivity.Eating);

                            feedingEvents++;
                            energyConsumed += consumed;
                            fedThisStep = true;
                        }
                    }

                    if (!fedThisStep)
                    {
                        var minimumDestinationEnergy =
                            PersonNeedsState
                                .EnergyConsumedPerDay;

                        var destination =
                            FindNearestAvailableResource(
                                current,
                                food.Values,
                                FoodSeekingRadiusDegrees,
                                minimumDestinationEnergy);

                        if (destination is not null)
                        {
                            foodSeekingTravel++;
                        }
                        else
                        {
                            destination =
                                FindNearestAvailableResource(
                                    current,
                                    food.Values,
                                    ScarcityMigrationRadiusDegrees,
                                    minimumDestinationEnergy);

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
                                MoveTowardResource(
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

        var operation =
            new ReplacePlanetForagingStateOperation(
                _planetId,
                population,
                food.Values);

        return new SimulationChange(
            operation,
            "foraging",
            "Survival needs and nearby food resources changed.",
            _planetId,
            elapsedSeconds,
            new Dictionary<string, double>
            {
                ["foragingAttempts"] = foragingAttempts,
                ["feedingEvents"] = feedingEvents,
                ["energyConsumed"] = energyConsumed,
                ["energyRecovered"] = energyRecovered,
                ["exhaustedResources"] =
                    exhaustedResources,
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

    private FoodResourceState? FindNearestAvailableResource(
        PersonState person,
        IEnumerable<FoodResourceState> resources)
    {
        return FindNearestAvailableResource(
            person,
            resources,
            _searchRadiusDegrees,
            PersonNeedsState.EnergyConsumedPerDay);
    }

    private static FoodResourceState?
        FindNearestAvailableResource(
            PersonState person,
            IEnumerable<FoodResourceState> resources,
            double searchRadiusDegrees,
            double minimumAvailableEnergy = 0)
    {
        if (!double.IsFinite(minimumAvailableEnergy) ||
            minimumAvailableEnergy < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumAvailableEnergy));
        }

        FoodResourceState? nearest = null;
        var nearestDistanceSquared =
            double.PositiveInfinity;

        foreach (var resource in resources)
        {
            if (resource.AvailableEnergy <= 0 ||
                resource.AvailableEnergy <
                    minimumAvailableEnergy)
            {
                continue;
            }

            var latitudeDelta =
                resource.LatitudeDegrees -
                person.LatitudeDegrees;

            var longitudeDelta =
                SignedLongitudeDelta(
                    resource.LongitudeDegrees,
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
                nearest = resource;
                nearestDistanceSquared =
                    distanceSquared;
            }
        }

        return nearest;
    }

    private static PersonState MoveTowardResource(
        PersonState person,
        FoodResourceState resource,
        double elapsedDays)
    {
        var latitudeDelta =
            resource.LatitudeDegrees -
            person.LatitudeDegrees;

        var longitudeDelta =
            SignedLongitudeDelta(
                resource.LongitudeDegrees,
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
