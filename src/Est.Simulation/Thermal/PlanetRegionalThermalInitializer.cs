using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;

namespace Est.Simulation.Thermal;

/// <summary>
/// Creates deterministic compatibility regional thermal state from the current
/// planetary mean-surface-temperature baseline.
///
/// This initializer intentionally applies no lapse rate, terrain correction,
/// water correction, or hidden radiative-equilibrium adjustment.
/// </summary>
public static class PlanetRegionalThermalInitializer
{
    public static PlanetRegionalThermalState
        FromPlanetaryMeanSurfaceTemperature(
            PlanetState planet,
            PlanetTerrainState terrain)
    {
        ArgumentNullException.ThrowIfNull(
            planet);

        ArgumentNullException.ThrowIfNull(
            terrain);

        terrain.ValidateFor(
            planet);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                terrain.GridDefinition);

        var initialTemperatureKelvin =
            planet.Environment
                .MeanSurfaceTemperatureKelvin;

        return new PlanetRegionalThermalState(
            planet.Id,
            terrain.GridDefinition,
            grid.Cells.Select(
                cell =>
                    new RegionalThermalCellState(
                        cell.Id,
                        initialTemperatureKelvin,
                        initialTemperatureKelvin)));
    }
}
