using System.Collections.Immutable;
using Est.Simulation.Surface;

namespace Est.Simulation.Hydrology;

/// <summary>
/// Derived connected body of current surface-liquid water.
///
/// AnchorCellId is the lowest stable surface-cell identity in the component.
/// It acts as a deterministic identity for the current derived body.
/// </summary>
public sealed record StandingWaterBody
{
    public StandingWaterBody(
        SurfaceCellId anchorCellId,
        StandingWaterKind kind,
        IEnumerable<SurfaceCellId> cellIds,
        double surfaceAreaSquareMeters,
        double waterVolumeCubicMeters)
    {
        ArgumentNullException.ThrowIfNull(
            cellIds);

        if (kind == StandingWaterKind.Dry)
        {
            throw new ArgumentException(
                "A standing-water body cannot be dry.",
                nameof(kind));
        }

        var cells =
            cellIds.ToImmutableArray();

        if (cells.IsDefaultOrEmpty)
        {
            throw new ArgumentException(
                "A standing-water body must contain at least one cell.",
                nameof(cellIds));
        }

        if (!cells.Contains(
                anchorCellId))
        {
            throw new ArgumentException(
                "The standing-water anchor must belong to the body.",
                nameof(anchorCellId));
        }

        if (!double.IsFinite(
                surfaceAreaSquareMeters) ||
            surfaceAreaSquareMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(surfaceAreaSquareMeters));
        }

        if (!double.IsFinite(
                waterVolumeCubicMeters) ||
            waterVolumeCubicMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(waterVolumeCubicMeters));
        }

        AnchorCellId = anchorCellId;
        Kind = kind;
        CellIds = cells;
        SurfaceAreaSquareMeters =
            surfaceAreaSquareMeters;
        WaterVolumeCubicMeters =
            waterVolumeCubicMeters;
    }

    public SurfaceCellId AnchorCellId { get; }

    public StandingWaterKind Kind { get; }

    public ImmutableArray<SurfaceCellId> CellIds { get; }

    public double SurfaceAreaSquareMeters { get; }

    public double WaterVolumeCubicMeters { get; }
}
