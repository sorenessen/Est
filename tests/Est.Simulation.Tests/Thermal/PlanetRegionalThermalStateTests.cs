using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Thermal;

namespace Est.Simulation.Tests.Thermal;

public sealed class PlanetRegionalThermalStateTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Cell_RejectsInvalidSurfaceTemperature(
        double temperatureKelvin)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new RegionalThermalCellState(
                    new SurfaceCellId(
                        Guid.NewGuid()),
                    temperatureKelvin,
                    250));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Cell_RejectsInvalidAtmosphericTemperature(
        double temperatureKelvin)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new RegionalThermalCellState(
                    new SurfaceCellId(
                        Guid.NewGuid()),
                    285,
                    temperatureKelvin));
    }

    [Fact]
    public void Cell_AllowsAbsoluteZero()
    {
        var cell =
            new RegionalThermalCellState(
                new SurfaceCellId(
                    Guid.NewGuid()),
                0,
                0);

        Assert.Equal(
            0,
            cell.SurfaceTemperatureKelvin);

        Assert.Equal(
            0,
            cell.AtmosphericTemperatureKelvin);
    }

    [Fact]
    public void State_RejectsDuplicateSurfaceCellIdentity()
    {
        var planet =
            CreatePlanet();

        var terrain =
            CreateTerrain(
                planet);

        var cellId =
            terrain.Cells[0].CellId;

        Assert.Throws<ArgumentException>(
            () =>
                new PlanetRegionalThermalState(
                    planet.Id,
                    terrain.GridDefinition,
                    [
                        new RegionalThermalCellState(
                            cellId,
                            285,
                            250),
                        new RegionalThermalCellState(
                            cellId,
                            286,
                            251)
                    ]));
    }

    [Fact]
    public void State_ValidatesCompleteSurfaceCoverage()
    {
        var planet =
            CreatePlanet();

        var terrain =
            CreateTerrain(
                planet);

        var state =
            CreateThermalState(
                planet,
                terrain.GridDefinition);

        state.ValidateFor(
            planet);

        Assert.Equal(
            terrain.Cells.Length,
            state.Cells.Length);
    }

    [Fact]
    public void State_RejectsIncompleteSurfaceCoverage()
    {
        var planet =
            CreatePlanet();

        var terrain =
            CreateTerrain(
                planet);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                terrain.GridDefinition);

        var state =
            new PlanetRegionalThermalState(
                planet.Id,
                terrain.GridDefinition,
                grid.Cells
                    .Skip(1)
                    .Select(
                        cell =>
                            new RegionalThermalCellState(
                                cell.Id,
                                285,
                                250)));

        Assert.Throws<ArgumentException>(
            () =>
                state.ValidateFor(
                    planet));
    }

    [Fact]
    public void State_RejectsDifferentPlanet()
    {
        var planet =
            CreatePlanet();

        var otherPlanet =
            CreatePlanet();

        var terrain =
            CreateTerrain(
                planet);

        var state =
            CreateThermalState(
                planet,
                terrain.GridDefinition);

        Assert.Throws<ArgumentException>(
            () =>
                state.ValidateFor(
                    otherPlanet));
    }

    [Fact]
    public void State_GetCellReturnsRequestedCell()
    {
        var planet =
            CreatePlanet();

        var terrain =
            CreateTerrain(
                planet);

        var state =
            CreateThermalState(
                planet,
                terrain.GridDefinition);

        var expected =
            state.Cells[7];

        var actual =
            state.GetCell(
                expected.CellId);

        Assert.Equal(
            expected,
            actual);
    }

    [Fact]
    public void Initializer_SeedsEveryCellFromPlanetaryMean()
    {
        var planet =
            CreatePlanet(
                temperatureKelvin: 287.25);

        var terrain =
            CreateTerrain(
                planet);

        var state =
            PlanetRegionalThermalInitializer
                .FromPlanetaryMeanSurfaceTemperature(
                    planet,
                    terrain);

        Assert.Equal(
            planet.Id,
            state.PlanetId);

        Assert.Equal(
            terrain.GridDefinition,
            state.GridDefinition);

        Assert.Equal(
            terrain.Cells.Length,
            state.Cells.Length);

        Assert.All(
            state.Cells,
            cell =>
            {
                Assert.Equal(
                    287.25,
                    cell.SurfaceTemperatureKelvin);

                Assert.Equal(
                    287.25,
                    cell.AtmosphericTemperatureKelvin);
            });

        state.ValidateFor(
            planet);
    }

    [Fact]
    public void Initializer_DoesNotApplyTerrainTemperatureAdjustment()
    {
        var planet =
            CreatePlanet(
                temperatureKelvin: 281.5);

        var terrain =
            CreateTerrain(
                planet,
                elevationOffset: -5_000);

        var state =
            PlanetRegionalThermalInitializer
                .FromPlanetaryMeanSurfaceTemperature(
                    planet,
                    terrain);

        Assert.Single(
            state.Cells
                .Select(
                    cell =>
                        cell.SurfaceTemperatureKelvin)
                .Distinct());

        Assert.Single(
            state.Cells
                .Select(
                    cell =>
                        cell.AtmosphericTemperatureKelvin)
                .Distinct());

        Assert.All(
            state.Cells,
            cell =>
            {
                Assert.Equal(
                    281.5,
                    cell.SurfaceTemperatureKelvin);

                Assert.Equal(
                    281.5,
                    cell.AtmosphericTemperatureKelvin);
            });
    }

    private static PlanetRegionalThermalState
        CreateThermalState(
            PlanetState planet,
            SurfaceGridDefinition definition)
    {
        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        return new PlanetRegionalThermalState(
            planet.Id,
            definition,
            grid.Cells.Select(
                (cell, index) =>
                    new RegionalThermalCellState(
                        cell.Id,
                        280 + index * 0.1,
                        240 + index * 0.05)));
    }

    private static PlanetTerrainState CreateTerrain(
        PlanetState planet,
        double elevationOffset = 0)
    {
        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        return new PlanetTerrainState(
            planet.Id,
            definition,
            grid.Cells.Select(
                (cell, index) =>
                    new TerrainCellState(
                        cell.Id,
                        elevationOffset +
                        index * 250)));
    }

    private static PlanetState CreatePlanet(
        double temperatureKelvin = 285)
    {
        return new PlanetState(
            PlanetId.New(),
            "Thermal World",
            5.0e24,
            6_000_000,
            new PlanetEnvironment(
                temperatureKelvin,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
