using Est.Simulation.Surface;

namespace Est.Simulation.Terrain;

public sealed record TerrainCellState
{
    public TerrainCellState(
        SurfaceCellId cellId,
        double elevationMeters)
    {
        if (!double.IsFinite(elevationMeters))
        {
            throw new ArgumentOutOfRangeException(
                nameof(elevationMeters),
                "Terrain elevation must be finite.");
        }

        CellId = cellId;
        ElevationMeters = elevationMeters;
    }

    public SurfaceCellId CellId { get; }

    /// <summary>
    /// Elevation relative to the planet mean-radius datum.
    /// Positive values are above the datum and negative values below it.
    /// This is not itself sea level.
    /// </summary>
    public double ElevationMeters { get; }
}
