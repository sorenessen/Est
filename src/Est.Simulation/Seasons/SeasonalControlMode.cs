namespace Est.Simulation.Seasons;

/// <summary>
/// Selects how effective seasonal context is supplied for a planet.
/// </summary>
public enum SeasonalControlMode
{
    Disabled = 0,
    Derived = 1,
    Override = 2
}
