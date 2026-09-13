using Est.Simulation.Ecology;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

public sealed record ReplacePlanetForagingStateOperation
    : ISimulationOperation
{
    public ReplacePlanetForagingStateOperation(
        PlanetId planetId,
        IEnumerable<PersonState> population,
        IEnumerable<FoodResourceState> foodResources)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(population);
        ArgumentNullException.ThrowIfNull(foodResources);

        Population = population.ToArray();
        FoodResources = foodResources.ToArray();

        if (Population.Any(person => person is null))
        {
            throw new ArgumentException(
                "Population cannot contain null entries.",
                nameof(population));
        }

        if (FoodResources.Any(resource => resource is null))
        {
            throw new ArgumentException(
                "Food resources cannot contain null entries.",
                nameof(foodResources));
        }

        if (Population.Any(
                person =>
                    person.PlanetId != planetId))
        {
            throw new ArgumentException(
                "Population contains a person assigned to another planet.",
                nameof(population));
        }

        if (FoodResources.Any(
                resource =>
                    resource.PlanetId != planetId))
        {
            throw new ArgumentException(
                "Food resources contain a resource assigned to another planet.",
                nameof(foodResources));
        }

        PlanetId = planetId;
    }

    public PlanetId PlanetId { get; }

    public IReadOnlyList<PersonState> Population { get; }

    public IReadOnlyList<FoodResourceState> FoodResources { get; }

    public WorldState Apply(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!world.Planets.Any(
                planet => planet.Id == PlanetId))
        {
            throw new PlanetNotFoundException(
                PlanetId);
        }

        var preservedPopulation =
            world.Population.Where(
                person =>
                    person.PlanetId != PlanetId);

        var preservedFood =
            world.FoodResources.Where(
                resource =>
                    resource.PlanetId != PlanetId);

        return world
            .ReplacePopulation(
                preservedPopulation.Concat(
                    Population))
            .ReplaceFoodResources(
                preservedFood.Concat(
                    FoodResources));
    }
}
