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
        var energyConsumed = 0d;

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
                        current =
                            current.WithSurvivalState(
                                current.Needs,
                                PersonActivity.Foraging);
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
                ["exhaustedResources"] =
                    exhaustedResources,
                ["starvationDeaths"] =
                    starvationDeaths,
                ["survivors"] = population.Count
            });
    }

    private FoodResourceState? FindNearestAvailableResource(
        PersonState person,
        IEnumerable<FoodResourceState> resources)
    {
        FoodResourceState? nearest = null;
        var nearestDistanceSquared =
            double.PositiveInfinity;

        foreach (var resource in resources)
        {
            if (resource.AvailableEnergy <= 0)
            {
                continue;
            }

            var latitudeDelta =
                resource.LatitudeDegrees -
                person.LatitudeDegrees;

            var longitudeDelta =
                LongitudeDelta(
                    resource.LongitudeDegrees,
                    person.LongitudeDegrees);

            var distanceSquared =
                latitudeDelta * latitudeDelta +
                longitudeDelta * longitudeDelta;

            if (distanceSquared >
                _searchRadiusDegrees *
                _searchRadiusDegrees)
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

    private static double LongitudeDelta(
        double first,
        double second)
    {
        var delta = Math.Abs(first - second);

        return delta <= 180
            ? delta
            : 360 - delta;
    }
}
