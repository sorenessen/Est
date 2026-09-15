using System.Collections.Immutable;
using Est.Simulation.Surface;

namespace Est.Simulation.Terrain;

/// <summary>
/// Derived terrain depression associated with one drainage sink.
///
/// Retained cells are the sink-side terrain cells that can participate in
/// standing water before the basin reaches its lowest spill connection.
/// </summary>
public sealed class TerrainDrainageBasin
{
    public TerrainDrainageBasin(
        SurfaceCellId sinkCellId,
        IEnumerable<SurfaceCellId> retainedCellIds,
        double? spillElevationMeters,
        SurfaceCellId? spillSourceCellId,
        SurfaceCellId? spillDestinationCellId)
    {
        var retained =
            retainedCellIds
                .Distinct()
                .OrderBy(
                    cellId =>
                        cellId.Value)
                .ToImmutableArray();

        if (retained.Length == 0 ||
            !retained.Contains(
                sinkCellId))
        {
            throw new ArgumentException(
                "A drainage basin must retain its sink cell.",
                nameof(retainedCellIds));
        }

        if (spillElevationMeters.HasValue)
        {
            if (!double.IsFinite(
                    spillElevationMeters.Value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(spillElevationMeters));
            }

            if (!spillSourceCellId.HasValue ||
                !spillDestinationCellId.HasValue)
            {
                throw new ArgumentException(
                    "A finite basin spill elevation requires source and destination cells.");
            }
        }
        else if (spillSourceCellId.HasValue ||
                 spillDestinationCellId.HasValue)
        {
            throw new ArgumentException(
                "A basin without a spill elevation cannot define a spill connection.");
        }

        SinkCellId =
            sinkCellId;

        RetainedCellIds =
            retained;

        SpillElevationMeters =
            spillElevationMeters;

        SpillSourceCellId =
            spillSourceCellId;

        SpillDestinationCellId =
            spillDestinationCellId;
    }

    public SurfaceCellId SinkCellId { get; }

    public ImmutableArray<SurfaceCellId>
        RetainedCellIds { get; }

    public double? SpillElevationMeters { get; }

    public SurfaceCellId? SpillSourceCellId { get; }

    public SurfaceCellId? SpillDestinationCellId { get; }
}
