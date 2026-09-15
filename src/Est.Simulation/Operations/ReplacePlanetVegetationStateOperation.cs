using Est.Simulation.Planets;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

public sealed record ReplacePlanetVegetationStateOperation
    : ISimulationOperation
{
    public ReplacePlanetVegetationStateOperation(
        PlanetVegetationState vegetation)
    {
        ArgumentNullException.ThrowIfNull(
            vegetation);

        Vegetation =
            vegetation;
    }

    public PlanetVegetationState Vegetation { get; }

    public WorldState Apply(
        WorldState world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        if (!world.Planets.Any(
                planet =>
                    planet.Id ==
                    Vegetation.PlanetId))
        {
            throw new PlanetNotFoundException(
                Vegetation.PlanetId);
        }

        var preserved =
            world.Vegetation.Where(
                candidate =>
                    candidate.PlanetId !=
                    Vegetation.PlanetId);

        return world.ReplaceVegetation(
            preserved.Append(
                Vegetation));
    }
}
