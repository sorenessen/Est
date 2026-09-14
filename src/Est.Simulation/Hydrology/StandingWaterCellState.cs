using Est.Simulation.Surface;

namespace Est.Simulation.Hydrology;

/// <summary>
/// Derived standing-water presentation of one authoritative surface cell.
///
/// This is not durable simulation state. It is reconstructed from terrain
/// elevation and current surface-liquid water inventory.
/// </summary>
public sealed record StandingWaterCellState
{
    public StandingWaterCellState(
        SurfaceCellId cellId,
        StandingWaterKind kind,
        SurfaceCellId? waterBodyAnchorCellId,
        double waterDepthMeters,
        double waterSurfaceElevationMeters)
    {
        if (!double.IsFinite(waterDepthMeters) ||
            waterDepthMeters < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(waterDepthMeters),
                "Water depth must be finite and nonnegative.");
        }

        if (!double.IsFinite(
                waterSurfaceElevationMeters))
        {
            throw new ArgumentOutOfRangeException(
                nameof(waterSurfaceElevationMeters),
                "Water-surface elevation must be finite.");
        }

        if (kind == StandingWaterKind.Dry)
        {
            if (waterBodyAnchorCellId.HasValue)
            {
                throw new ArgumentException(
                    "Dry cells cannot belong to a standing-water body.",
                    nameof(waterBodyAnchorCellId));
            }

            if (waterDepthMeters != 0)
            {
                throw new ArgumentException(
                    "Dry cells must have zero standing-water depth.",
                    nameof(waterDepthMeters));
            }
        }
        else
        {
            if (!waterBodyAnchorCellId.HasValue)
            {
                throw new ArgumentException(
                    "Flooded cells must belong to a standing-water body.",
                    nameof(waterBodyAnchorCellId));
            }

            if (waterDepthMeters <= 0)
            {
                throw new ArgumentException(
                    "Flooded cells must have positive standing-water depth.",
                    nameof(waterDepthMeters));
            }
        }

        CellId = cellId;
        Kind = kind;
        WaterBodyAnchorCellId =
            waterBodyAnchorCellId;
        WaterDepthMeters =
            waterDepthMeters;
        WaterSurfaceElevationMeters =
            waterSurfaceElevationMeters;
    }

    public SurfaceCellId CellId { get; }

    public StandingWaterKind Kind { get; }

    public SurfaceCellId? WaterBodyAnchorCellId { get; }

    public double WaterDepthMeters { get; }

    public double WaterSurfaceElevationMeters { get; }

    public bool IsFlooded =>
        Kind != StandingWaterKind.Dry;
}
