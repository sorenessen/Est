using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Security.Cryptography;
using Est.Simulation.Planets;

namespace Est.Simulation.Surface;

/// <summary>
/// Initial deterministic spherical surface tessellation.
///
/// This is deliberately hidden behind IPlanetSurfaceGrid so that a
/// hierarchical equal-area or near-equal-area grid can replace it
/// without changing environmental or biological state contracts.
/// </summary>
public sealed class LatLonPlanetSurfaceGrid
    : IPlanetSurfaceGrid
{
    private const int GridIdentityVersion = 1;

    private readonly int _latitudeBandCount;
    private readonly int _longitudeBandCount;
    private readonly double _latitudeStepDegrees;
    private readonly double _longitudeStepDegrees;

    private readonly ImmutableArray<SurfaceCell> _cells;

    private readonly Dictionary<
        SurfaceCellId,
        (int Row, int Column)> _locations;

    public LatLonPlanetSurfaceGrid(
        PlanetId planetId,
        double meanRadiusMeters,
        int latitudeBandCount,
        int longitudeBandCount)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        if (!double.IsFinite(meanRadiusMeters) ||
            meanRadiusMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(meanRadiusMeters),
                "Planet radius must be finite and positive.");
        }

        if (latitudeBandCount < 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitudeBandCount),
                "A surface grid requires at least two latitude bands.");
        }

        if (longitudeBandCount < 4)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitudeBandCount),
                "A surface grid requires at least four longitude bands.");
        }

        PlanetId = planetId;
        MeanRadiusMeters = meanRadiusMeters;

        _latitudeBandCount = latitudeBandCount;
        _longitudeBandCount = longitudeBandCount;

        _latitudeStepDegrees =
            180d / latitudeBandCount;

        _longitudeStepDegrees =
            360d / longitudeBandCount;

        var cells =
            ImmutableArray.CreateBuilder<SurfaceCell>(
                checked(
                    latitudeBandCount *
                    longitudeBandCount));

        _locations = new Dictionary<
            SurfaceCellId,
            (int Row, int Column)>(
                checked(
                    latitudeBandCount *
                    longitudeBandCount));

        for (var row = 0;
             row < latitudeBandCount;
             row++)
        {
            var southLatitudeDegrees =
                -90 +
                row * _latitudeStepDegrees;

            var northLatitudeDegrees =
                southLatitudeDegrees +
                _latitudeStepDegrees;

            var centerLatitudeDegrees =
                (southLatitudeDegrees +
                 northLatitudeDegrees) /
                2;

            var southLatitudeRadians =
                DegreesToRadians(
                    southLatitudeDegrees);

            var northLatitudeRadians =
                DegreesToRadians(
                    northLatitudeDegrees);

            for (var column = 0;
                 column < longitudeBandCount;
                 column++)
            {
                var westLongitudeDegrees =
                    -180 +
                    column *
                    _longitudeStepDegrees;

                var centerLongitudeDegrees =
                    NormalizeLongitude(
                        westLongitudeDegrees +
                        _longitudeStepDegrees / 2);

                var areaSquareMeters =
                    CalculateCellAreaSquareMeters(
                        meanRadiusMeters,
                        southLatitudeRadians,
                        northLatitudeRadians,
                        _longitudeStepDegrees);

                var id =
                    CreateStableCellId(
                        planetId,
                        latitudeBandCount,
                        longitudeBandCount,
                        row,
                        column);

                cells.Add(
                    new SurfaceCell(
                        id,
                        centerLatitudeDegrees,
                        centerLongitudeDegrees,
                        areaSquareMeters));

                _locations.Add(
                    id,
                    (row, column));
            }
        }

        _cells = cells.MoveToImmutable();

        TotalSurfaceAreaSquareMeters =
            _cells.Sum(
                cell => cell.AreaSquareMeters);
    }

    public PlanetId PlanetId { get; }

    public double MeanRadiusMeters { get; }

    public int CellCount =>
        _cells.Length;

    public double TotalSurfaceAreaSquareMeters { get; }

    public IReadOnlyList<SurfaceCell> Cells =>
        _cells;

    public SurfaceCell GetCell(
        SurfaceCellId cellId)
    {
        if (!_locations.TryGetValue(
                cellId,
                out var location))
        {
            throw new KeyNotFoundException(
                $"Surface cell {cellId.Value} does not belong to this grid.");
        }

        return GetCell(
            location.Row,
            location.Column);
    }

    public SurfaceCell LocateCell(
        double latitudeDegrees,
        double longitudeDegrees)
    {
        if (!double.IsFinite(latitudeDegrees) ||
            latitudeDegrees < -90 ||
            latitudeDegrees > 90)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitudeDegrees),
                "Latitude must be finite and between -90 and 90 degrees.");
        }

        if (!double.IsFinite(longitudeDegrees))
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitudeDegrees),
                "Longitude must be finite.");
        }

        var normalizedLongitude =
            NormalizeLongitude(
                longitudeDegrees);

        var row =
            Math.Min(
                _latitudeBandCount - 1,
                (int)Math.Floor(
                    (latitudeDegrees + 90) /
                    _latitudeStepDegrees));

        var column =
            Math.Min(
                _longitudeBandCount - 1,
                (int)Math.Floor(
                    (normalizedLongitude + 180) /
                    _longitudeStepDegrees));

        return GetCell(
            row,
            column);
    }

    public IReadOnlyList<SurfaceCellId> GetNeighbors(
        SurfaceCellId cellId)
    {
        if (!_locations.TryGetValue(
                cellId,
                out var location))
        {
            throw new KeyNotFoundException(
                $"Surface cell {cellId.Value} does not belong to this grid.");
        }

        var neighbors =
            new List<SurfaceCellId>(4);

        if (location.Row > 0)
        {
            neighbors.Add(
                GetCell(
                    location.Row - 1,
                    location.Column)
                .Id);
        }

        if (location.Row <
            _latitudeBandCount - 1)
        {
            neighbors.Add(
                GetCell(
                    location.Row + 1,
                    location.Column)
                .Id);
        }

        neighbors.Add(
            GetCell(
                location.Row,
                WrapColumn(
                    location.Column - 1))
            .Id);

        neighbors.Add(
            GetCell(
                location.Row,
                WrapColumn(
                    location.Column + 1))
            .Id);

        return neighbors;
    }

    private SurfaceCell GetCell(
        int row,
        int column)
    {
        return _cells[
            checked(
                row *
                _longitudeBandCount +
                column)];
    }

    private int WrapColumn(
        int column)
    {
        var wrapped =
            column %
            _longitudeBandCount;

        return wrapped < 0
            ? wrapped +
              _longitudeBandCount
            : wrapped;
    }

    private static double
        CalculateCellAreaSquareMeters(
            double radiusMeters,
            double southLatitudeRadians,
            double northLatitudeRadians,
            double longitudeSpanDegrees)
    {
        var longitudeSpanRadians =
            DegreesToRadians(
                longitudeSpanDegrees);

        return radiusMeters *
               radiusMeters *
               longitudeSpanRadians *
               (
                   Math.Sin(
                       northLatitudeRadians) -
                   Math.Sin(
                       southLatitudeRadians)
               );
    }

    private static SurfaceCellId
        CreateStableCellId(
            PlanetId planetId,
            int latitudeBandCount,
            int longitudeBandCount,
            int row,
            int column)
    {
        Span<byte> identityBytes =
            stackalloc byte[36];

        if (!planetId.Value.TryWriteBytes(
                identityBytes[..16]))
        {
            throw new InvalidOperationException(
                "Planet identity could not be encoded.");
        }

        BinaryPrimitives.WriteInt32LittleEndian(
            identityBytes[16..20],
            GridIdentityVersion);

        BinaryPrimitives.WriteInt32LittleEndian(
            identityBytes[20..24],
            latitudeBandCount);

        BinaryPrimitives.WriteInt32LittleEndian(
            identityBytes[24..28],
            longitudeBandCount);

        BinaryPrimitives.WriteInt32LittleEndian(
            identityBytes[28..32],
            row);

        BinaryPrimitives.WriteInt32LittleEndian(
            identityBytes[32..36],
            column);

        Span<byte> hash =
            stackalloc byte[32];

        SHA256.HashData(
            identityBytes,
            hash);

        return new SurfaceCellId(
            new Guid(
                hash[..16]));
    }

    private static double DegreesToRadians(
        double degrees)
    {
        return degrees *
               Math.PI /
               180d;
    }

    private static double NormalizeLongitude(
        double longitudeDegrees)
    {
        var normalized =
            longitudeDegrees % 360d;

        if (normalized < -180)
        {
            normalized += 360;
        }

        if (normalized >= 180)
        {
            normalized -= 360;
        }

        return normalized;
    }
}
