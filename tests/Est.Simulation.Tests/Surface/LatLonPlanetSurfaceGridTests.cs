using Est.Simulation.Planets;
using Est.Simulation.Surface;

namespace Est.Simulation.Tests.Surface;

public sealed class LatLonPlanetSurfaceGridTests
{
    private const double EarthRadiusMeters =
        6_371_000;

    [Fact]
    public void Constructor_CreatesExpectedCellCount()
    {
        var grid =
            CreateGrid();

        Assert.Equal(
            648,
            grid.CellCount);

        Assert.Equal(
            grid.CellCount,
            grid.Cells.Count);
    }

    [Fact]
    public void Areas_SumToSphericalSurfaceArea()
    {
        var grid =
            CreateGrid();

        var expected =
            4 *
            Math.PI *
            EarthRadiusMeters *
            EarthRadiusMeters;

        var relativeError =
            Math.Abs(
                grid.TotalSurfaceAreaSquareMeters -
                expected) /
            expected;

        Assert.InRange(
            relativeError,
            0,
            1e-12);
    }

    [Fact]
    public void LocateCell_ReturnsContainingAngularCell()
    {
        var grid =
            CreateGrid();

        var cell =
            grid.LocateCell(
                12.3,
                45.6);

        Assert.Equal(
            15,
            cell.CenterLatitudeDegrees,
            precision: 10);

        Assert.Equal(
            45,
            cell.CenterLongitudeDegrees,
            precision: 10);
    }

    [Fact]
    public void LocateCell_NormalizesLongitude()
    {
        var grid =
            CreateGrid();

        var first =
            grid.LocateCell(
                0,
                179.9);

        var wrapped =
            grid.LocateCell(
                0,
                -180.1);

        Assert.Equal(
            first.Id,
            wrapped.Id);
    }

    [Fact]
    public void NeighborTopology_WrapsLongitude()
    {
        var grid =
            CreateGrid();

        var westernEdge =
            grid.LocateCell(
                0,
                -179.9);

        var wrappedWest =
            grid.LocateCell(
                0,
                179.9);

        var neighbors =
            grid.GetNeighbors(
                westernEdge.Id);

        Assert.Equal(
            4,
            neighbors.Count);

        Assert.Contains(
            wrappedWest.Id,
            neighbors);
    }

    [Fact]
    public void PolarBand_HasNoNeighborBeyondPole()
    {
        var grid =
            CreateGrid();

        var polarCell =
            grid.LocateCell(
                89.9,
                0);

        var neighbors =
            grid.GetNeighbors(
                polarCell.Id);

        Assert.Equal(
            3,
            neighbors.Count);
    }

    [Fact]
    public void StableIdentity_IsDeterministicForSameGrid()
    {
        var planetId =
            new PlanetId(
                Guid.Parse(
                    "11111111-2222-3333-4444-555555555555"));

        var first =
            new LatLonPlanetSurfaceGrid(
                planetId,
                EarthRadiusMeters,
                latitudeBandCount: 18,
                longitudeBandCount: 36);

        var second =
            new LatLonPlanetSurfaceGrid(
                planetId,
                EarthRadiusMeters,
                latitudeBandCount: 18,
                longitudeBandCount: 36);

        Assert.Equal(
            first.Cells
                .Select(cell => cell.Id),
            second.Cells
                .Select(cell => cell.Id));
    }

    [Fact]
    public void StableIdentity_DiffersAcrossPlanets()
    {
        var first =
            new LatLonPlanetSurfaceGrid(
                new PlanetId(
                    Guid.Parse(
                        "11111111-2222-3333-4444-555555555555")),
                EarthRadiusMeters,
                latitudeBandCount: 18,
                longitudeBandCount: 36);

        var second =
            new LatLonPlanetSurfaceGrid(
                new PlanetId(
                    Guid.Parse(
                        "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
                EarthRadiusMeters,
                latitudeBandCount: 18,
                longitudeBandCount: 36);

        Assert.NotEqual(
            first.Cells[0].Id,
            second.Cells[0].Id);
    }

    [Fact]
    public void StableIdentity_DiffersAcrossGridResolution()
    {
        var planetId =
            new PlanetId(
                Guid.Parse(
                    "11111111-2222-3333-4444-555555555555"));

        var coarse =
            new LatLonPlanetSurfaceGrid(
                planetId,
                EarthRadiusMeters,
                latitudeBandCount: 18,
                longitudeBandCount: 36);

        var fine =
            new LatLonPlanetSurfaceGrid(
                planetId,
                EarthRadiusMeters,
                latitudeBandCount: 36,
                longitudeBandCount: 72);

        Assert.NotEqual(
            coarse.Cells[0].Id,
            fine.Cells[0].Id);
    }

    private static LatLonPlanetSurfaceGrid
        CreateGrid()
    {
        return new LatLonPlanetSurfaceGrid(
            new PlanetId(
                Guid.Parse(
                    "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            EarthRadiusMeters,
            latitudeBandCount: 18,
            longitudeBandCount: 36);
    }
}
