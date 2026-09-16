using Est.Simulation.Biogeochemistry;
using Est.Simulation.Planets;
using Est.Simulation.Surface;

namespace Est.Simulation.Tests.Biogeochemistry;

public sealed class PlanetBiogeochemistryStateTests
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

        var state =
            new PlanetBiogeochemistryState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new BiogeochemistryCellState(
                            cell.Id,
                            1,
                            0.03,
                            0.01)));

        state.ValidateFor(
            planet);

        Assert.Equal(
            grid.CellCount,
            state.Cells.Length);
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
                new PlanetBiogeochemistryState(
                    planet.Id,
                    definition,
                    [
                        new BiogeochemistryCellState(
                            cellId,
                            1,
                            0.03,
                            0.01),
                        new BiogeochemistryCellState(
                            cellId,
                            2,
                            0.06,
                            0.02)
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

        var state =
            new PlanetBiogeochemistryState(
                planet.Id,
                definition,
                grid.Cells
                    .Take(
                        grid.CellCount - 1)
                    .Select(
                        cell =>
                            new BiogeochemistryCellState(
                                cell.Id,
                                1,
                                0.03,
                                0.01)));

        Assert.Throws<ArgumentException>(
            () =>
                state.ValidateFor(
                    planet));
    }

    [Fact]
    public void GetCell_ReturnsMatchingState()
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

        var state =
            new PlanetBiogeochemistryState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new BiogeochemistryCellState(
                            cell.Id,
                            cell.Id == target.Id
                                ? 3
                                : 0,
                            0.03,
                            0.01)));

        Assert.Equal(
            3,
            state.GetCell(
                    target.Id)
                .DetritalBiomassKilogramsPerSquareMeter);
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Biogeochemistry World",
            5.0e24,
            RadiusMeters,
            new PlanetEnvironment(
                285,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
