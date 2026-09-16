using Est.Simulation.Invertebrates;
using Est.Simulation.Planets;
using Est.Simulation.Surface;

namespace Est.Simulation.Tests.Invertebrates;

public sealed class PlanetInvertebrateStateTests
{
    private const double RadiusMeters =
        6_000_000;

    [Fact]
    public void ValidateFor_AcceptsExactlyOneStatePerSurfaceCell()
    {
        var planet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        var invertebrates =
            new PlanetInvertebrateState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new InvertebrateCellState(
                            cell.Id,
                            0.25)));

        invertebrates.ValidateFor(
            planet);

        Assert.Equal(
            grid.CellCount,
            invertebrates.Cells.Length);

        Assert.All(
            invertebrates.Cells,
            cell =>
                Assert.Equal(
                    0.25,
                    cell.LiveBiomassKilogramsPerSquareMeter));
    }

    [Fact]
    public void Constructor_RejectsDuplicateSurfaceCells()
    {
        var planet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        var cellId =
            grid.Cells[0].Id;

        Assert.Throws<ArgumentException>(
            () =>
                new PlanetInvertebrateState(
                    planet.Id,
                    definition,
                    [
                        new InvertebrateCellState(
                            cellId,
                            0.1),
                        new InvertebrateCellState(
                            cellId,
                            0.2)
                    ]));
    }

    [Fact]
    public void ValidateFor_RejectsIncompleteSurfaceCoverage()
    {
        var planet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        var invertebrates =
            new PlanetInvertebrateState(
                planet.Id,
                definition,
                grid.Cells
                    .Take(
                        grid.CellCount - 1)
                    .Select(
                        cell =>
                            new InvertebrateCellState(
                                cell.Id,
                                0.1)));

        Assert.Throws<ArgumentException>(
            () =>
                invertebrates.ValidateFor(
                    planet));
    }

    [Fact]
    public void GetCell_ReturnsMatchingInvertebrateState()
    {
        var planet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        var target =
            grid.Cells[3];

        var invertebrates =
            new PlanetInvertebrateState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new InvertebrateCellState(
                            cell.Id,
                            cell.Id ==
                            target.Id
                                ? 0.75
                                : 0)));

        Assert.Equal(
            0.75,
            invertebrates
                .GetCell(
                    target.Id)
                .LiveBiomassKilogramsPerSquareMeter);
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Invertebrate World",
            5.0e24,
            RadiusMeters,
            new PlanetEnvironment(
                285,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
