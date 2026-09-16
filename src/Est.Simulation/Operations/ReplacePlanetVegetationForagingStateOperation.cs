using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

/// <summary>
/// Atomically applies the population and vegetation consequences of
/// vegetation-backed foraging for one planet.
/// </summary>
public sealed record ReplacePlanetVegetationForagingStateOperation
    : ISimulationOperation
{
    public ReplacePlanetVegetationForagingStateOperation(
        PlanetId planetId,
        IEnumerable<PersonState> population,
        PlanetVegetationState vegetation)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(population);
        ArgumentNullException.ThrowIfNull(vegetation);

        Population = population.ToArray();

        if (Population.Any(person => person is null))
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

        if (vegetation.PlanetId != planetId)
        {
            throw new ArgumentException(
                "Vegetation belongs to another planet.",
                nameof(vegetation));
        }

        PlanetId = planetId;
        Vegetation = vegetation;
    }

    public PlanetId PlanetId { get; }

    public IReadOnlyList<PersonState> Population { get; }

    public PlanetVegetationState Vegetation { get; }

    public WorldState Apply(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!world.Planets.Any(
                planet =>
                    planet.Id == PlanetId))
        {
            throw new PlanetNotFoundException(
                PlanetId);
        }

        var preservedPopulation =
            world.Population.Where(
                person =>
                    person.PlanetId != PlanetId);

        var preservedVegetation =
            world.Vegetation.Where(
                vegetation =>
                    vegetation.PlanetId != PlanetId);

        return world
            .ReplacePopulation(
                preservedPopulation.Concat(
                    Population))
            .ReplaceVegetation(
                preservedVegetation.Append(
                    Vegetation));
    }
}
