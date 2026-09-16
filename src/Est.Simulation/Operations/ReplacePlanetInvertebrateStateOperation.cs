using Est.Simulation.Invertebrates;
using Est.Simulation.Planets;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

public sealed record ReplacePlanetInvertebrateStateOperation
    : ISimulationOperation
{
    public ReplacePlanetInvertebrateStateOperation(
        PlanetInvertebrateState invertebrates)
    {
        ArgumentNullException.ThrowIfNull(
            invertebrates);

        Invertebrates =
            invertebrates;
    }

    public PlanetInvertebrateState Invertebrates { get; }

    public WorldState Apply(
        WorldState world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        if (!world.Planets.Any(
                planet =>
                    planet.Id ==
                    Invertebrates.PlanetId))
        {
            throw new PlanetNotFoundException(
                Invertebrates.PlanetId);
        }

        var preserved =
            world.Invertebrates.Where(
                candidate =>
                    candidate.PlanetId !=
                    Invertebrates.PlanetId);

        return world.ReplaceInvertebrates(
            preserved.Append(
                Invertebrates));
    }
}
