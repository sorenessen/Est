using Est.Simulation.Hydrology;
using Est.Simulation.Planets;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

public sealed record ReplacePlanetHydrologyStateOperation
    : ISimulationOperation
{
    public ReplacePlanetHydrologyStateOperation(
        PlanetHydrologyState hydrology)
    {
        ArgumentNullException.ThrowIfNull(
            hydrology);

        Hydrology = hydrology;
    }

    public PlanetHydrologyState Hydrology { get; }

    public WorldState Apply(
        WorldState world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        if (!world.Planets.Any(
                planet =>
                    planet.Id ==
                    Hydrology.PlanetId))
        {
            throw new PlanetNotFoundException(
                Hydrology.PlanetId);
        }

        var preserved =
            world.Hydrology.Where(
                candidate =>
                    candidate.PlanetId !=
                    Hydrology.PlanetId);

        return world.ReplaceHydrology(
            preserved.Append(
                Hydrology));
    }
}
