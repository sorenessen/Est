using Est.Simulation.Planets;
using Est.Simulation.Seasons;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

public sealed record ReplacePlanetSeasonalStateOperation
    : ISimulationOperation
{
    public ReplacePlanetSeasonalStateOperation(
        PlanetSeasonalState seasonalState)
    {
        ArgumentNullException.ThrowIfNull(
            seasonalState);

        SeasonalState = seasonalState;
    }

    public PlanetSeasonalState SeasonalState { get; }

    public WorldState Apply(
        WorldState world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        if (!world.Planets.Any(
                planet =>
                    planet.Id ==
                    SeasonalState.PlanetId))
        {
            throw new PlanetNotFoundException(
                SeasonalState.PlanetId);
        }

        var preserved =
            world.SeasonalStates.Where(
                candidate =>
                    candidate.PlanetId !=
                    SeasonalState.PlanetId);

        return world.ReplaceSeasonalStates(
            preserved.Append(
                SeasonalState));
    }
}
