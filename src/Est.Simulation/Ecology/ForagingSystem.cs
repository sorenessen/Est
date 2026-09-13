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
                .ToArray();

        var food =
            world.FoodResources
                .Where(
                    resource =>
                        resource.PlanetId == _planetId)
                .OrderBy(resource => resource.Id.Value)
                .ToArray();

        var updatedFood =
            food.ToDictionary(
                resource => resource.Id);

        var updatedPopulation =
            new List<PersonState>(
                population.Length);

        var foragers = 0;
        var fed = 0;
        var exhaustedResources = 0;
        var energyConsumed = 0d;

        foreach (var person in population)
        {
            if (person.Needs.EnergyReserve >=
                HungerThreshold)
            {
                updatedPopulation.Add(person);
                continue;
            }

            foragers++;

            var resource =
                FindNearestAvailableResource(
                    person,
                    updatedFood.Values);

            if (resource is null)
            {
                updatedPopulation.Add(
                    person.WithSurvivalState(
                        person.Needs,
                        PersonActivity.Foraging));

                continue;
            }

            var energyNeeded =
                1 - person.Needs.EnergyReserve;

            var consumed =
                Math.Min(
                    energyNeeded,
                    resource.AvailableEnergy);

            if (consumed <= 0)
            {
                updatedPopulation.Add(
                    person.WithSurvivalState(
                        person.Needs,
                        PersonActivity.Foraging));

                continue;
            }

            var remainingResource =
                resource.Consume(consumed);

            updatedFood[resource.Id] =
                remainingResource;

            if (resource.AvailableEnergy > 0 &&
                remainingResource.AvailableEnergy == 0)
            {
                exhaustedResources++;
            }

            var needs =
                person.Needs.WithEnergyReserve(
                    person.Needs.EnergyReserve +
                    consumed);

            updatedPopulation.Add(
                person.WithSurvivalState(
                    needs,
                    PersonActivity.Eating));

            fed++;
            energyConsumed += consumed;
        }

        var operation =
            new ReplacePlanetForagingStateOperation(
                _planetId,
                updatedPopulation,
                updatedFood.Values);

        return new SimulationChange(
            operation,
            "foraging",
            "Hungry people consumed nearby food resources.",
            _planetId,
            elapsedSeconds,
            new Dictionary<string, double>
            {
                ["foragers"] = foragers,
                ["fed"] = fed,
                ["energyConsumed"] = energyConsumed,
                ["exhaustedResources"] =
                    exhaustedResources
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
