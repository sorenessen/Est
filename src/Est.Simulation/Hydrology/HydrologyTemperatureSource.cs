namespace Est.Simulation.Hydrology;

/// <summary>
/// Selects the authoritative temperature source used by hydrology phase-change
/// decisions.
///
/// PlanetaryCompatibility preserves the existing planetary mean-temperature
/// path. RegionalSurface consumes authoritative regional surface-cell
/// temperature.
/// </summary>
public enum HydrologyTemperatureSource
{
    PlanetaryCompatibility = 0,
    RegionalSurface = 1
}
