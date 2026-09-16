using Est.Simulation.Animals;
using Est.Simulation.Grazers;
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
        IEnumerable<AnimalState> animals,
        IEnumerable<GrazerCohortState> grazerCohorts)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(population);
        ArgumentNullException.ThrowIfNull(animals);
        ArgumentNullException.ThrowIfNull(grazerCohorts);

        PlanetId = planetId;
        Population = population.ToArray();
        Animals = animals.ToArray();
        GrazerCohorts = grazerCohorts.ToArray();

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

        if (GrazerCohorts.Any(
                cohort =>
                    cohort.PlanetId != planetId))
        {
            throw new ArgumentException(
                "Grazer cohorts contain a cohort assigned to another planet.",
                nameof(grazerCohorts));
        }
    }

    public PlanetId PlanetId { get; }

    public IReadOnlyList<PersonState> Population { get; }

    public IReadOnlyList<AnimalState> Animals { get; }

    public IReadOnlyList<GrazerCohortState>
        GrazerCohorts { get; }

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

        var preservedGrazerCohorts =
            world.GrazerCohorts.Where(
                cohort =>
                    cohort.PlanetId != PlanetId);

        return world
            .ReplacePopulation(
                preservedPopulation.Concat(Population))
            .ReplaceAnimals(
                preservedAnimals.Concat(Animals))
            .ReplaceGrazerCohorts(
                preservedGrazerCohorts.Concat(
                    GrazerCohorts));
    }
}
