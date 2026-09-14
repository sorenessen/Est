using System.Collections.Immutable;
using Est.Simulation.Planets;
using Est.Simulation.Surface;

namespace Est.Simulation.Terrain;

/// <summary>
/// Derived steepest-descent topology for a planet's terrain.
///
/// The topology is intentionally reconstructed from terrain rather than
/// persisted. Terrain elevation and surface-grid identity remain the
/// authoritative data.
/// </summary>
public sealed class PlanetTerrainDrainageTopology
{
    private readonly Dictionary<
        SurfaceCellId,
        TerrainDrainageCell> _byCellId;

    private PlanetTerrainDrainageTopology(
        PlanetId planetId,
        SurfaceGridDefinition gridDefinition,
        IEnumerable<TerrainDrainageCell> cells)
    {
        PlanetId = planetId;
        GridDefinition = gridDefinition;
        Cells = cells.ToImmutableArray();

        _byCellId =
            Cells.ToDictionary(
                cell => cell.CellId);
    }

    public PlanetId PlanetId { get; }

    public SurfaceGridDefinition GridDefinition { get; }

    public ImmutableArray<TerrainDrainageCell> Cells { get; }

    public TerrainDrainageCell GetCell(
        SurfaceCellId cellId)
    {
        if (_byCellId.TryGetValue(
                cellId,
                out var cell))
        {
            return cell;
        }

        throw new KeyNotFoundException(
            $"Drainage topology does not contain surface cell {cellId.Value}.");
    }

    public static PlanetTerrainDrainageTopology Derive(
        PlanetState planet,
        PlanetTerrainState terrain)
    {
        ArgumentNullException.ThrowIfNull(planet);
        ArgumentNullException.ThrowIfNull(terrain);

        terrain.ValidateFor(
            planet);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                terrain.GridDefinition);

        var terrainByCellId =
            terrain.Cells.ToDictionary(
                cell => cell.CellId);

        var drainage =
            new TerrainDrainageCell[
                grid.CellCount];

        for (var index = 0;
             index < grid.Cells.Count;
             index++)
        {
            var cell =
                grid.Cells[index];

            var elevation =
                terrainByCellId[
                    cell.Id]
                .ElevationMeters;

            SurfaceCellId? bestNeighborId =
                null;

            var bestSlope = 0d;
            var bestDistance = 0d;

            foreach (var neighborId in
                     grid.GetNeighbors(
                         cell.Id))
            {
                var neighbor =
                    grid.GetCell(
                        neighborId);

                var neighborElevation =
                    terrainByCellId[
                        neighborId]
                    .ElevationMeters;

                var elevationDrop =
                    elevation -
                    neighborElevation;

                if (elevationDrop <= 0)
                {
                    continue;
                }

                var distance =
                    GreatCircleDistanceMeters(
                        planet.MeanRadiusMeters,
                        cell,
                        neighbor);

                var slope =
                    elevationDrop /
                    distance;

                if (slope > bestSlope ||
                    (slope == bestSlope &&
                     ShouldPreferForStableTieBreak(
                         neighborId,
                         bestNeighborId)))
                {
                    bestNeighborId =
                        neighborId;

                    bestSlope =
                        slope;

                    bestDistance =
                        distance;
                }
            }

            drainage[index] =
                bestNeighborId.HasValue
                    ? new TerrainDrainageCell(
                        cell.Id,
                        bestNeighborId.Value,
                        bestSlope,
                        bestDistance)
                    : new TerrainDrainageCell(
                        cell.Id,
                        null,
                        0,
                        0);
        }

        return new PlanetTerrainDrainageTopology(
            planet.Id,
            terrain.GridDefinition,
            drainage);
    }

    private static bool
        ShouldPreferForStableTieBreak(
            SurfaceCellId candidate,
            SurfaceCellId? current)
    {
        return current is null ||
               candidate.Value.CompareTo(
                   current.Value.Value) < 0;
    }

    private static double
        GreatCircleDistanceMeters(
            double radiusMeters,
            SurfaceCell first,
            SurfaceCell second)
    {
        var firstLatitude =
            DegreesToRadians(
                first.CenterLatitudeDegrees);

        var secondLatitude =
            DegreesToRadians(
                second.CenterLatitudeDegrees);

        var latitudeDelta =
            secondLatitude -
            firstLatitude;

        var longitudeDelta =
            DegreesToRadians(
                NormalizeLongitudeDelta(
                    second.CenterLongitudeDegrees -
                    first.CenterLongitudeDegrees));

        var sinHalfLatitude =
            Math.Sin(
                latitudeDelta / 2);

        var sinHalfLongitude =
            Math.Sin(
                longitudeDelta / 2);

        var haversine =
            sinHalfLatitude *
            sinHalfLatitude +
            Math.Cos(firstLatitude) *
            Math.Cos(secondLatitude) *
            sinHalfLongitude *
            sinHalfLongitude;

        var centralAngle =
            2 *
            Math.Asin(
                Math.Sqrt(
                    Math.Clamp(
                        haversine,
                        0,
                        1)));

        var distance =
            radiusMeters *
            centralAngle;

        if (!double.IsFinite(distance) ||
            distance <= 0)
        {
            throw new InvalidOperationException(
                "Adjacent surface cells must have a finite positive great-circle distance.");
        }

        return distance;
    }

    private static double
        NormalizeLongitudeDelta(
            double longitudeDegrees)
    {
        var normalized =
            longitudeDegrees %
            360;

        if (normalized < -180)
        {
            normalized += 360;
        }

        if (normalized > 180)
        {
            normalized -= 360;
        }

        return normalized;
    }

    private static double DegreesToRadians(
        double degrees)
    {
        return degrees *
               Math.PI /
               180;
    }
}
