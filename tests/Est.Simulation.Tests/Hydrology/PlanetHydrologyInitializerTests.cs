using Est.Simulation.Hydrology;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;

namespace Est.Simulation.Tests.Hydrology;

public sealed class PlanetHydrologyInitializerTests
{
    [Fact]
    public void Initialize_ZeroInventoryProducesCompleteDryHydrology()
    {
        var setup =
            CreateTerrain();

        var hydrology =
            PlanetHydrologyInitializer
                .FromSurfaceLiquidWaterInventory(
                    setup.Planet,
                    setup.Terrain,
                    0);

        Assert.Equal(
            setup.Grid.CellCount,
            hydrology.Cells.Length);

        Assert.All(
            hydrology.Cells,
            cell =>
                Assert.Equal(
                    0,
                    cell.SurfaceLiquidWaterKilogramsPerSquareMeter));

        Assert.Equal(
            0,
            hydrology.TotalWaterMassKilograms(
                setup.Planet));
    }

    [Fact]
    public void Initialize_SolvesAreaWeightedEquilibriumWaterLevel()
    {
        var setup =
            CreateTerrain();

        const double targetWaterSurfaceElevationMeters =
            100;

        var expectedInventory =
            setup.Grid.Cells.Sum(
                cell =>
                {
                    var elevation =
                        setup.Terrain
                            .GetCell(
                                cell.Id)
                            .ElevationMeters;

                    var depth =
                        Math.Max(
                            0,
                            targetWaterSurfaceElevationMeters -
                            elevation);

                    return depth *
                           cell.AreaSquareMeters *
                           PlanetStandingWaterState
                               .LiquidWaterDensityKilogramsPerCubicMeter;
                });

        var hydrology =
            PlanetHydrologyInitializer
                .FromSurfaceLiquidWaterInventory(
                    setup.Planet,
                    setup.Terrain,
                    expectedInventory);

        foreach (var surfaceCell in
                 setup.Grid.Cells)
        {
            var elevation =
                setup.Terrain
                    .GetCell(
                        surfaceCell.Id)
                    .ElevationMeters;

            var expectedDepth =
                Math.Max(
                    0,
                    targetWaterSurfaceElevationMeters -
                    elevation);

            var actualDepth =
                hydrology
                    .GetCell(
                        surfaceCell.Id)
                    .SurfaceLiquidWaterKilogramsPerSquareMeter /
                PlanetStandingWaterState
                    .LiquidWaterDensityKilogramsPerCubicMeter;

            Assert.Equal(
                expectedDepth,
                actualDepth,
                precision: 8);
        }

        var actualInventory =
            hydrology.TotalWaterMassKilograms(
                setup.Planet);

        var relativeError =
            Math.Abs(
                actualInventory -
                expectedInventory) /
            expectedInventory;

        Assert.InRange(
            relativeError,
            0,
            1e-12);
    }

    [Fact]
    public void Initialize_DoesNotTreatZeroElevationAsSeaLevel()
    {
        var setup =
            CreateTerrain();

        var lowestCell =
            setup.Terrain.Cells
                .OrderBy(
                    cell =>
                        cell.ElevationMeters)
                .First();

        var lowestSurfaceCell =
            setup.Grid.GetCell(
                lowestCell.CellId);

        const double desiredDepthMeters =
            25;

        var inventory =
            desiredDepthMeters *
            lowestSurfaceCell.AreaSquareMeters *
            PlanetStandingWaterState
                .LiquidWaterDensityKilogramsPerCubicMeter;

        var hydrology =
            PlanetHydrologyInitializer
                .FromSurfaceLiquidWaterInventory(
                    setup.Planet,
                    setup.Terrain,
                    inventory);

        var flooded =
            hydrology.Cells
                .Where(
                    cell =>
                        cell
                            .SurfaceLiquidWaterKilogramsPerSquareMeter >
                        0)
                .ToArray();

        Assert.Single(
            flooded);

        Assert.Equal(
            lowestCell.CellId,
            flooded[0].CellId);

        Assert.True(
            lowestCell.ElevationMeters <
            0);

        var waterSurfaceElevation =
            lowestCell.ElevationMeters +
            flooded[0]
                .SurfaceLiquidWaterKilogramsPerSquareMeter /
            PlanetStandingWaterState
                .LiquidWaterDensityKilogramsPerCubicMeter;

        Assert.NotEqual(
            0,
            waterSurfaceElevation);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Initialize_RejectsInvalidInventory(
        double inventory)
    {
        var setup =
            CreateTerrain();

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                PlanetHydrologyInitializer
                    .FromSurfaceLiquidWaterInventory(
                        setup.Planet,
                        setup.Terrain,
                        inventory));
    }

    private static TestTerrain CreateTerrain()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Inventory World",
                5.0e24,
                6_000_000,
                new PlanetEnvironment(
                    285,
                    0.60,
                    0.05,
                    AtmosphereState.Vacuum));

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                2,
                4);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        var elevations =
            new[]
            {
                -300d,
                -100d,
                50d,
                250d,
                400d,
                600d,
                800d,
                1_000d
            };

        var terrain =
            new PlanetTerrainState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    (cell, index) =>
                        new TerrainCellState(
                            cell.Id,
                            elevations[index])));

        return new TestTerrain(
            planet,
            grid,
            terrain);
    }

    private sealed record TestTerrain(
        PlanetState Planet,
        IPlanetSurfaceGrid Grid,
        PlanetTerrainState Terrain);
}
