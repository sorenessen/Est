using Est.Simulation.Animals;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

public sealed record ReplacePlanetPredatorStateOperation
    : ISimulationOperation
{
    public ReplacePlanetPredatorStateOperation(
        PlanetId planetId,
        IEnumerable<PersonState> population,
        IEnumerable<AnimalState> animals)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(population);
        ArgumentNullException.ThrowIfNull(animals);

        PlanetId = planetId;
        Population = population.ToArray();
        Animals = animals.ToArray();

        if (Population.Any(
                person => person.PlanetId != planetId))
        {
            throw new ArgumentException(
                "Population contains a person assigned to another planet.",
                nameof(population));
        }

        if (Animals.Any(
                animal => animal.PlanetId != planetId))
        {
            throw new ArgumentException(
                "Animals contain an animal assigned to another planet.",
                nameof(animals));
        }
    }

    public PlanetId PlanetId { get; }

    public IReadOnlyList<PersonState> Population { get; }

    public IReadOnlyList<AnimalState> Animals { get; }

    public WorldState Apply(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!world.Planets.Any(
                planet => planet.Id == PlanetId))
        {
            throw new PlanetNotFoundException(PlanetId);
        }

        var preservedPopulation =
            world.Population.Where(
                person => person.PlanetId != PlanetId);

        var preservedAnimals =
            world.Animals.Where(
                animal => animal.PlanetId != PlanetId);

        return world
            .ReplacePopulation(
                preservedPopulation.Concat(Population))
            .ReplaceAnimals(
                preservedAnimals.Concat(Animals));
    }
}
