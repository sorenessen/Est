using Est.Simulation.Planets;
using Est.Simulation.Worlds;

namespace Est.Simulation.Seasons;

/// <summary>
/// Extension point for authoritative derived seasonality.
///
/// Providers may use simulation time, planetary state, astronomy, climate,
/// environmental signals, or configurable world rules. The simulation core
/// does not prescribe how seasonal context is derived.
/// </summary>
public interface ISeasonalProvider
{
    SeasonalContext Derive(
        WorldState world,
        PlanetId planetId);
}
