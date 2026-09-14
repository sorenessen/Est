using System.Collections.Immutable;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;

namespace Est.Simulation.Hydrology;

/// <summary>
/// Derived flooded-surface view of authoritative terrain and hydrology.
///
/// Surface liquid is converted to physical depth using liquid-water density.
/// Neighbor-connected flooded cells form water bodies. The largest connected
/// flooded component is classified as the planet's ocean; any smaller
/// disconnected flooded components are classified as lakes.
///
/// This type is intentionally derived rather than persisted.
/// </summary>
public sealed class PlanetStandingWaterState
{
    public const double LiquidWaterDensityKilogramsPerCubicMeter =
        1_000;

    private readonly Dictionary<
        SurfaceCellId,
        StandingWaterCellState> _cellsById;

    private PlanetStandingWaterState(
        PlanetId planetId,
        SurfaceGridDefinition gridDefinition,
        IEnumerable<StandingWaterCellState> cells,
        IEnumerable<StandingWaterBody> waterBodies)
    {
        PlanetId = planetId;
        GridDefinition = gridDefinition;
        Cells = cells.ToImmutableArray();
        WaterBodies =
            waterBodies.ToImmutableArray();

        _cellsById =
            Cells.ToDictionary(
                cell =>
                    cell.CellId);
    }

    public PlanetId PlanetId { get; }

    public SurfaceGridDefinition GridDefinition { get; }

    public ImmutableArray<StandingWaterCellState> Cells { get; }

    public ImmutableArray<StandingWaterBody> WaterBodies { get; }

    public StandingWaterBody? Ocean =>
        WaterBodies.FirstOrDefault(
            body =>
                body.Kind ==
                StandingWaterKind.Ocean);

    public StandingWaterCellState GetCell(
        SurfaceCellId cellId)
    {
        if (_cellsById.TryGetValue(
                cellId,
                out var cell))
        {
            return cell;
        }

        throw new KeyNotFoundException(
            $"Standing-water state does not contain surface cell {cellId.Value}.");
    }

    public static PlanetStandingWaterState Derive(
        PlanetState planet,
        PlanetTerrainState terrain,
        PlanetHydrologyState hydrology)
    {
        ArgumentNullException.ThrowIfNull(
            planet);

        ArgumentNullException.ThrowIfNull(
            terrain);

        ArgumentNullException.ThrowIfNull(
            hydrology);

        terrain.ValidateFor(
            planet);

        hydrology.ValidateFor(
            planet);

        if (terrain.GridDefinition !=
            hydrology.GridDefinition)
        {
            throw new ArgumentException(
                "Terrain and hydrology must use the same surface grid.");
        }

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                terrain.GridDefinition);

        var terrainById =
            terrain.Cells.ToDictionary(
                cell =>
                    cell.CellId);

        var hydrologyById =
            hydrology.Cells.ToDictionary(
                cell =>
                    cell.CellId);

        var floodedCellIds =
            hydrology.Cells
                .Where(
                    cell =>
                        cell
                            .SurfaceLiquidWaterKilogramsPerSquareMeter >
                        0)
                .Select(
                    cell =>
                        cell.CellId)
                .ToHashSet();

        var components =
            FindConnectedFloodedComponents(
                grid,
                floodedCellIds);

        var componentDetails =
            components
                .Select(
                    component =>
                        CreateComponentDetails(
                            component,
                            grid,
                            hydrologyById))
                .OrderByDescending(
                    component =>
                        component
                            .SurfaceAreaSquareMeters)
                .ThenBy(
                    component =>
                        component
                            .AnchorCellId
                            .Value)
                .ToArray();

        var waterBodies =
            new StandingWaterBody[
                componentDetails.Length];

        var bodyKindByAnchor =
            new Dictionary<
                SurfaceCellId,
                StandingWaterKind>();

        for (var index = 0;
             index < componentDetails.Length;
             index++)
        {
            var component =
                componentDetails[index];

            var kind =
                index == 0
                    ? StandingWaterKind.Ocean
                    : StandingWaterKind.Lake;

            waterBodies[index] =
                new StandingWaterBody(
                    component.AnchorCellId,
                    kind,
                    component.CellIds,
                    component.SurfaceAreaSquareMeters,
                    component.WaterVolumeCubicMeters);

            bodyKindByAnchor.Add(
                component.AnchorCellId,
                kind);
        }

        var anchorByCellId =
            new Dictionary<
                SurfaceCellId,
                SurfaceCellId>();

        foreach (var component in
                 componentDetails)
        {
            foreach (var cellId in
                     component.CellIds)
            {
                anchorByCellId.Add(
                    cellId,
                    component.AnchorCellId);
            }
        }

        var cells =
            new StandingWaterCellState[
                grid.CellCount];

        for (var index = 0;
             index < grid.Cells.Count;
             index++)
        {
            var surfaceCell =
                grid.Cells[index];

            var terrainCell =
                terrainById[
                    surfaceCell.Id];

            var hydrologyCell =
                hydrologyById[
                    surfaceCell.Id];

            var waterDepthMeters =
                hydrologyCell
                    .SurfaceLiquidWaterKilogramsPerSquareMeter /
                LiquidWaterDensityKilogramsPerCubicMeter;

            if (waterDepthMeters <= 0)
            {
                cells[index] =
                    new StandingWaterCellState(
                        surfaceCell.Id,
                        StandingWaterKind.Dry,
                        null,
                        0,
                        terrainCell.ElevationMeters);

                continue;
            }

            var anchor =
                anchorByCellId[
                    surfaceCell.Id];

            cells[index] =
                new StandingWaterCellState(
                    surfaceCell.Id,
                    bodyKindByAnchor[
                        anchor],
                    anchor,
                    waterDepthMeters,
                    terrainCell.ElevationMeters +
                    waterDepthMeters);
        }

        return new PlanetStandingWaterState(
            planet.Id,
            terrain.GridDefinition,
            cells,
            waterBodies);
    }

    private static List<List<SurfaceCellId>>
        FindConnectedFloodedComponents(
            IPlanetSurfaceGrid grid,
            HashSet<SurfaceCellId> floodedCellIds)
    {
        var remaining =
            new HashSet<SurfaceCellId>(
                floodedCellIds);

        var components =
            new List<
                List<SurfaceCellId>>();

        while (remaining.Count > 0)
        {
            var start =
                remaining
                    .OrderBy(
                        cellId =>
                            cellId.Value)
                    .First();

            var queue =
                new Queue<SurfaceCellId>();

            var component =
                new List<SurfaceCellId>();

            remaining.Remove(
                start);

            queue.Enqueue(
                start);

            while (queue.Count > 0)
            {
                var current =
                    queue.Dequeue();

                component.Add(
                    current);

                foreach (var neighbor in
                         grid.GetNeighbors(
                             current))
                {
                    if (!remaining.Remove(
                            neighbor))
                    {
                        continue;
                    }

                    queue.Enqueue(
                        neighbor);
                }
            }

            component.Sort(
                (first, second) =>
                    first.Value.CompareTo(
                        second.Value));

            components.Add(
                component);
        }

        return components;
    }

    private static ComponentDetails
        CreateComponentDetails(
            IReadOnlyList<SurfaceCellId> cellIds,
            IPlanetSurfaceGrid grid,
            IReadOnlyDictionary<
                SurfaceCellId,
                HydrologyCellState> hydrologyById)
    {
        var anchor =
            cellIds
                .OrderBy(
                    cellId =>
                        cellId.Value)
                .First();

        var area = 0d;
        var volume = 0d;

        foreach (var cellId in
                 cellIds)
        {
            var surfaceCell =
                grid.GetCell(
                    cellId);

            var hydrologyCell =
                hydrologyById[
                    cellId];

            var depth =
                hydrologyCell
                    .SurfaceLiquidWaterKilogramsPerSquareMeter /
                LiquidWaterDensityKilogramsPerCubicMeter;

            area +=
                surfaceCell
                    .AreaSquareMeters;

            volume +=
                depth *
                surfaceCell
                    .AreaSquareMeters;
        }

        return new ComponentDetails(
            anchor,
            cellIds.ToImmutableArray(),
            area,
            volume);
    }

    private sealed record ComponentDetails(
        SurfaceCellId AnchorCellId,
        ImmutableArray<SurfaceCellId> CellIds,
        double SurfaceAreaSquareMeters,
        double WaterVolumeCubicMeters);
}
