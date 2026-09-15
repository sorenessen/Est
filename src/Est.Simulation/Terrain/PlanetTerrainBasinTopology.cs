using System.Collections.Immutable;
using Est.Simulation.Planets;
using Est.Simulation.Surface;

namespace Est.Simulation.Terrain;

/// <summary>
/// Derived local-depression topology for authoritative terrain.
///
/// Cells are first assigned to the local sink reached by strict downhill
/// drainage. The lowest boundary connection between that catchment and
/// another catchment defines the basin's first spill elevation.
/// </summary>
public sealed class PlanetTerrainBasinTopology
{
    private PlanetTerrainBasinTopology(
        PlanetId planetId,
        SurfaceGridDefinition gridDefinition,
        IEnumerable<TerrainDrainageBasin> basins)
    {
        PlanetId =
            planetId;

        GridDefinition =
            gridDefinition;

        Basins =
            basins
                .OrderBy(
                    basin =>
                        basin.SinkCellId.Value)
                .ToImmutableArray();
    }

    public PlanetId PlanetId { get; }

    public SurfaceGridDefinition GridDefinition { get; }

    public ImmutableArray<TerrainDrainageBasin> Basins { get; }

    public static PlanetTerrainBasinTopology Derive(
        PlanetState planet,
        PlanetTerrainState terrain,
        PlanetTerrainDrainageTopology drainage)
    {
        ArgumentNullException.ThrowIfNull(
            planet);

        ArgumentNullException.ThrowIfNull(
            terrain);

        ArgumentNullException.ThrowIfNull(
            drainage);

        terrain.ValidateFor(
            planet);

        if (drainage.PlanetId != planet.Id ||
            drainage.GridDefinition !=
            terrain.GridDefinition)
        {
            throw new ArgumentException(
                "Drainage topology must describe the same planet and surface grid as terrain.",
                nameof(drainage));
        }

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                terrain.GridDefinition);

        var terrainByCellId =
            terrain.Cells.ToDictionary(
                cell =>
                    cell.CellId);

        var sinkByCellId =
            new Dictionary<
                SurfaceCellId,
                SurfaceCellId>();

        foreach (var cell in
                 grid.Cells)
        {
            if (sinkByCellId.ContainsKey(
                    cell.Id))
            {
                continue;
            }

            var path =
                new List<SurfaceCellId>();

            var pathSet =
                new HashSet<SurfaceCellId>();

            var current =
                cell.Id;

            SurfaceCellId sink;

            while (true)
            {
                if (sinkByCellId.TryGetValue(
                        current,
                        out sink))
                {
                    break;
                }

                if (!pathSet.Add(
                        current))
                {
                    throw new InvalidOperationException(
                        "Drainage topology contains a cycle.");
                }

                path.Add(
                    current);

                var drainageCell =
                    drainage.GetCell(
                        current);

                if (!drainageCell
                    .DownhillNeighborCellId
                    .HasValue)
                {
                    sink =
                        current;

                    break;
                }

                current =
                    drainageCell
                        .DownhillNeighborCellId
                        .Value;
            }

            foreach (var pathCellId in
                     path)
            {
                sinkByCellId[
                    pathCellId] =
                    sink;
            }
        }

        var catchmentsBySink =
            sinkByCellId
                .GroupBy(
                    pair =>
                        pair.Value,
                    pair =>
                        pair.Key)
                .ToDictionary(
                    group =>
                        group.Key,
                    group =>
                        group.ToHashSet());

        var basins =
            new List<TerrainDrainageBasin>(
                catchmentsBySink.Count);

        foreach (var pair in
                 catchmentsBySink.OrderBy(
                     pair =>
                         pair.Key.Value))
        {
            var sinkCellId =
                pair.Key;

            var catchment =
                pair.Value;

            SpillConnection? bestSpill =
                null;

            foreach (var sourceId in
                     catchment.OrderBy(
                         cellId =>
                             cellId.Value))
            {
                var sourceElevation =
                    terrainByCellId[
                        sourceId]
                    .ElevationMeters;

                foreach (var destinationId in
                         grid.GetNeighbors(
                                 sourceId)
                             .OrderBy(
                                 cellId =>
                                     cellId.Value))
                {
                    if (catchment.Contains(
                            destinationId))
                    {
                        continue;
                    }

                    var destinationElevation =
                        terrainByCellId[
                            destinationId]
                        .ElevationMeters;

                    var candidate =
                        new SpillConnection(
                            Math.Max(
                                sourceElevation,
                                destinationElevation),
                            sourceId,
                            destinationId);

                    if (bestSpill is null ||
                        candidate.IsPreferredTo(
                            bestSpill.Value))
                    {
                        bestSpill =
                            candidate;
                    }
                }
            }

            SurfaceCellId[] retainedCellIds;

            if (bestSpill.HasValue)
            {
                var spillElevation =
                    bestSpill.Value
                        .ElevationMeters;

                var sinkElevation =
                    terrainByCellId[
                        sinkCellId]
                    .ElevationMeters;

                /*
                 * A true retained basin requires a boundary strictly above
                 * its sink. Flat terrain and level outlets are not
                 * depressions and must not trigger standing-water
                 * redistribution.
                 */
                if (spillElevation <=
                    sinkElevation)
                {
                    continue;
                }

                retainedCellIds =
                    catchment
                        .Where(
                            cellId =>
                                cellId ==
                                sinkCellId ||
                                terrainByCellId[
                                    cellId]
                                .ElevationMeters <
                                spillElevation)
                        .OrderBy(
                            cellId =>
                                cellId.Value)
                        .ToArray();

                basins.Add(
                    new TerrainDrainageBasin(
                        sinkCellId,
                        retainedCellIds,
                        spillElevation,
                        bestSpill.Value
                            .SourceCellId,
                        bestSpill.Value
                            .DestinationCellId));
            }
            else
            {
                retainedCellIds =
                    catchment
                        .OrderBy(
                            cellId =>
                                cellId.Value)
                        .ToArray();

                basins.Add(
                    new TerrainDrainageBasin(
                        sinkCellId,
                        retainedCellIds,
                        null,
                        null,
                        null));
            }
        }

        return new PlanetTerrainBasinTopology(
            planet.Id,
            terrain.GridDefinition,
            basins);
    }

    private readonly record struct SpillConnection(
        double ElevationMeters,
        SurfaceCellId SourceCellId,
        SurfaceCellId DestinationCellId)
    {
        public bool IsPreferredTo(
            SpillConnection other)
        {
            var elevationComparison =
                ElevationMeters.CompareTo(
                    other.ElevationMeters);

            if (elevationComparison != 0)
            {
                return elevationComparison <
                       0;
            }

            var sourceComparison =
                SourceCellId.Value.CompareTo(
                    other.SourceCellId.Value);

            if (sourceComparison != 0)
            {
                return sourceComparison <
                       0;
            }

            return DestinationCellId.Value
                       .CompareTo(
                           other
                               .DestinationCellId
                               .Value) <
                   0;
        }
    }
}
