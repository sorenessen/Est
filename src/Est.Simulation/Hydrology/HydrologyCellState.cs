using Est.Simulation.Surface;

namespace Est.Simulation.Hydrology;

/// <summary>
/// Conserved water stores for one planet-surface cell.
///
/// Values are kilograms of water per square meter of surface area.
/// Groundwater is intentionally not represented yet rather than being
/// approximated as another store.
/// </summary>
public sealed record HydrologyCellState
{
    public HydrologyCellState(
        SurfaceCellId cellId,
        double atmosphericWaterKilogramsPerSquareMeter,
        double surfaceLiquidWaterKilogramsPerSquareMeter,
        double soilWaterKilogramsPerSquareMeter,
        double snowIceWaterEquivalentKilogramsPerSquareMeter)
    {
        ValidateStore(
            atmosphericWaterKilogramsPerSquareMeter,
            nameof(atmosphericWaterKilogramsPerSquareMeter));

        ValidateStore(
            surfaceLiquidWaterKilogramsPerSquareMeter,
            nameof(surfaceLiquidWaterKilogramsPerSquareMeter));

        ValidateStore(
            soilWaterKilogramsPerSquareMeter,
            nameof(soilWaterKilogramsPerSquareMeter));

        ValidateStore(
            snowIceWaterEquivalentKilogramsPerSquareMeter,
            nameof(snowIceWaterEquivalentKilogramsPerSquareMeter));

        CellId = cellId;

        AtmosphericWaterKilogramsPerSquareMeter =
            atmosphericWaterKilogramsPerSquareMeter;

        SurfaceLiquidWaterKilogramsPerSquareMeter =
            surfaceLiquidWaterKilogramsPerSquareMeter;

        SoilWaterKilogramsPerSquareMeter =
            soilWaterKilogramsPerSquareMeter;

        SnowIceWaterEquivalentKilogramsPerSquareMeter =
            snowIceWaterEquivalentKilogramsPerSquareMeter;
    }

    public SurfaceCellId CellId { get; }

    public double AtmosphericWaterKilogramsPerSquareMeter
    {
        get;
    }

    public double SurfaceLiquidWaterKilogramsPerSquareMeter
    {
        get;
    }

    public double SoilWaterKilogramsPerSquareMeter
    {
        get;
    }

    public double SnowIceWaterEquivalentKilogramsPerSquareMeter
    {
        get;
    }

    public double TotalWaterKilogramsPerSquareMeter =>
        AtmosphericWaterKilogramsPerSquareMeter +
        SurfaceLiquidWaterKilogramsPerSquareMeter +
        SoilWaterKilogramsPerSquareMeter +
        SnowIceWaterEquivalentKilogramsPerSquareMeter;

    private static void ValidateStore(
        double value,
        string parameterName)
    {
        if (!double.IsFinite(value) ||
            value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Hydrology water stores must be finite and nonnegative.");
        }
    }
}
