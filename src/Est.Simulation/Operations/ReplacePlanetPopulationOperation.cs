using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

public sealed record ReplacePlanetPopulationOperation
    : ISimulationOperation
{
    public ReplacePlanetPopulationOperation(
        PlanetId planetId,
        IEnumerable<PersonState> population)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(population);

        PlanetId = planetId;
        Population = population.ToArray();

        if (Population.Any(
                person => person is null))
        {
            throw new ArgumentException(
                "Population cannot contain null entries.",
                nameof(population));
        }

        if (Population.Any(
                person =>
                    person.PlanetId != planetId))
        {
            throw new ArgumentException(
                "Population contains a person assigned to another planet.",
                nameof(population));
        }
    }

    public PlanetId PlanetId { get; }

    public IReadOnlyList<PersonState> Population { get; }

    public WorldState Apply(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!world.Planets.Any(
                planet => planet.Id == PlanetId))
        {
            throw new PlanetNotFoundException(
                PlanetId);
        }

        var preserved = world.Population
            .Where(
                person =>
                    person.PlanetId != PlanetId);

        return world.ReplacePopulation(
            preserved.Concat(Population));
    }
}
