using Est.Simulation.Biogeochemistry;
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
        IEnumerable<GrazerCohortState> grazerCohorts,
        PlanetBiogeochemistryState? biogeochemistry = null)
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

        if (biogeochemistry is not null &&
            biogeochemistry.PlanetId != planetId)
        {
            throw new ArgumentException(
                "Biogeochemistry belongs to another planet.",
                nameof(biogeochemistry));
        }

        Biogeochemistry = biogeochemistry;

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

    public PlanetBiogeochemistryState? Biogeochemistry { get; }

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

        var next =
            world
                .ReplacePopulation(
                    preservedPopulation.Concat(Population))
                .ReplaceAnimals(
                    preservedAnimals.Concat(Animals))
                .ReplaceGrazerCohorts(
                    preservedGrazerCohorts.Concat(
                        GrazerCohorts));

        if (Biogeochemistry is null)
        {
            return next;
        }

        var preservedBiogeochemistry =
            world.Biogeochemistry.Where(
                state =>
                    state.PlanetId != PlanetId);

        return next.ReplaceBiogeochemistry(
            preservedBiogeochemistry.Append(
                Biogeochemistry));
    }
}
