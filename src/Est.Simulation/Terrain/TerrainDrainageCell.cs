using Est.Simulation.Surface;

namespace Est.Simulation.Terrain;

/// <summary>
/// Deterministic drainage geometry derived from authoritative terrain.
///
/// This is derived state, not persisted state. A null downhill neighbor
/// represents a local minimum or flat sink. Hydrology may later fill such
/// depressions rather than forcing artificial drainage through terrain.
/// </summary>
public sealed record TerrainDrainageCell
{
    public TerrainDrainageCell(
        SurfaceCellId cellId,
        SurfaceCellId? downhillNeighborCellId,
        double downhillSlope,
        double horizontalDistanceMeters)
    {
        if (!double.IsFinite(downhillSlope) ||
            downhillSlope < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(downhillSlope),
                "Downhill slope must be finite and nonnegative.");
        }

        if (!double.IsFinite(horizontalDistanceMeters) ||
            horizontalDistanceMeters < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(horizontalDistanceMeters),
                "Drainage distance must be finite and nonnegative.");
        }

        if (downhillNeighborCellId is null)
        {
            if (downhillSlope != 0 ||
                horizontalDistanceMeters != 0)
            {
                throw new ArgumentException(
                    "A drainage sink cannot have nonzero slope or distance.");
            }
        }
        else if (downhillSlope <= 0 ||
                 horizontalDistanceMeters <= 0)
        {
            throw new ArgumentException(
                "A downhill neighbor requires positive slope and distance.");
        }

        CellId = cellId;
        DownhillNeighborCellId =
            downhillNeighborCellId;
        DownhillSlope = downhillSlope;
        HorizontalDistanceMeters =
            horizontalDistanceMeters;
    }

    public SurfaceCellId CellId { get; }

    public SurfaceCellId? DownhillNeighborCellId { get; }

    /// <summary>
    /// Dimensionless elevation drop divided by horizontal distance.
    /// </summary>
    public double DownhillSlope { get; }

    public double HorizontalDistanceMeters { get; }

    public bool IsSink =>
        DownhillNeighborCellId is null;
}
