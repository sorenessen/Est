using Est.Simulation.Planets;
using Est.Simulation.Terrain;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

public sealed record ReplacePlanetTerrainStateOperation
    : ISimulationOperation
{
    public ReplacePlanetTerrainStateOperation(
        PlanetTerrainState terrain)
    {
        ArgumentNullException.ThrowIfNull(terrain);

        Terrain = terrain;
    }

    public PlanetTerrainState Terrain { get; }

    public WorldState Apply(
        WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!world.Planets.Any(
                planet =>
                    planet.Id == Terrain.PlanetId))
        {
            throw new PlanetNotFoundException(
                Terrain.PlanetId);
        }

        var preserved =
            world.Terrain.Where(
                candidate =>
                    candidate.PlanetId !=
                    Terrain.PlanetId);

        return world.ReplaceTerrain(
            preserved.Append(
                Terrain));
    }
}
