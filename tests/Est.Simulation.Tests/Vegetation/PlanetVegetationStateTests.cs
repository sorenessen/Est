using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Vegetation;

namespace Est.Simulation.Tests.Vegetation;

public sealed class PlanetVegetationStateTests
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

        var vegetation =
            new PlanetVegetationState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new VegetationCellState(
                            cell.Id,
                            2)));

        vegetation.ValidateFor(
            planet);

        Assert.Equal(
            grid.CellCount,
            vegetation.Cells.Length);

        Assert.All(
            vegetation.Cells,
            cell =>
                Assert.Equal(
                    2,
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
                new PlanetVegetationState(
                    planet.Id,
                    definition,
                    [
                        new VegetationCellState(
                            cellId,
                            1),
                        new VegetationCellState(
                            cellId,
                            2)
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

        var vegetation =
            new PlanetVegetationState(
                planet.Id,
                definition,
                grid.Cells
                    .Take(
                        grid.CellCount - 1)
                    .Select(
                        cell =>
                            new VegetationCellState(
                                cell.Id,
                                1)));

        Assert.Throws<ArgumentException>(
            () =>
                vegetation.ValidateFor(
                    planet));
    }

    [Fact]
    public void GetCell_ReturnsMatchingVegetationState()
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

        var vegetation =
            new PlanetVegetationState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new VegetationCellState(
                            cell.Id,
                            cell.Id ==
                            target.Id
                                ? 7
                                : 0)));

        Assert.Equal(
            7,
            vegetation
                .GetCell(
                    target.Id)
                .LiveBiomassKilogramsPerSquareMeter);
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Vegetation World",
            5.0e24,
            RadiusMeters,
            new PlanetEnvironment(
                285,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
