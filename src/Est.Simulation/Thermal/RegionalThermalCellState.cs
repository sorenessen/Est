using Est.Simulation.Surface;

namespace Est.Simulation.Thermal;

/// <summary>
/// Durable thermal state for one planet-surface cell.
///
/// Surface and atmospheric-column temperatures are state only. Radiative
/// properties, heat capacities, material composition, terrain, and water
/// remain owned by their respective model policy or authoritative subsystems.
/// </summary>
public sealed record RegionalThermalCellState
{
    public RegionalThermalCellState(
        SurfaceCellId cellId,
        double surfaceTemperatureKelvin,
        double atmosphericTemperatureKelvin)
    {
        ValidateTemperature(
            surfaceTemperatureKelvin,
            nameof(surfaceTemperatureKelvin));

        ValidateTemperature(
            atmosphericTemperatureKelvin,
            nameof(atmosphericTemperatureKelvin));

        CellId = cellId;
        SurfaceTemperatureKelvin =
            surfaceTemperatureKelvin;

        AtmosphericTemperatureKelvin =
            atmosphericTemperatureKelvin;
    }

    public SurfaceCellId CellId { get; }

    public double SurfaceTemperatureKelvin { get; }

    public double AtmosphericTemperatureKelvin { get; }

    private static void ValidateTemperature(
        double temperatureKelvin,
        string parameterName)
    {
        if (!double.IsFinite(temperatureKelvin) ||
            temperatureKelvin < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Regional thermal temperatures must be finite and at or above absolute zero.");
        }
    }
}
