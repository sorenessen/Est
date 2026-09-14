using Est.Simulation.Hydrology;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;

namespace Est.Simulation.Tests.Hydrology;

public sealed class PlanetStandingWaterStateTests
{
    [Fact]
    public void Derive_NoSurfaceLiquidWaterProducesDryPlanet()
    {
        var setup =
            CreateSetup();

        var hydrology =
            CreateHydrology(
                setup,
                new Dictionary<
                    SurfaceCellId,
                    double>());

        var standingWater =
            PlanetStandingWaterState.Derive(
                setup.Planet,
                setup.Terrain,
                hydrology);

        Assert.Empty(
            standingWater.WaterBodies);

        Assert.Null(
            standingWater.Ocean);

        Assert.All(
            standingWater.Cells,
            cell =>
            {
                Assert.Equal(
                    StandingWaterKind.Dry,
                    cell.Kind);

                Assert.False(
                    cell.IsFlooded);

                Assert.Equal(
                    0,
                    cell.WaterDepthMeters);
            });
    }

    [Fact]
    public void Derive_ConvertsSurfaceMassDensityToPhysicalWaterDepth()
    {
        var setup =
            CreateSetup();

        var flooded =
            setup.Grid.LocateCell(
                22.5,
                22.5);

        var hydrology =
            CreateHydrology(
                setup,
                new Dictionary<
                    SurfaceCellId,
                    double>
                {
                    [flooded.Id] =
                        2_500
                });

        var standingWater =
            PlanetStandingWaterState.Derive(
                setup.Planet,
                setup.Terrain,
                hydrology);

        var cell =
            standingWater.GetCell(
                flooded.Id);

        var terrainElevation =
            setup.Terrain.GetCell(
                flooded.Id)
                .ElevationMeters;

        Assert.Equal(
            2.5,
            cell.WaterDepthMeters,
            10);

        Assert.Equal(
            terrainElevation + 2.5,
            cell.WaterSurfaceElevationMeters,
            10);

        Assert.Equal(
            StandingWaterKind.Ocean,
            cell.Kind);
    }

    [Fact]
    public void Derive_LargestConnectedFloodedBodyIsOcean()
    {
        var setup =
            CreateSetup();

        var firstOceanCell =
            setup.Grid.LocateCell(
                22.5,
                22.5);

        var secondOceanCell =
            setup.Grid.LocateCell(
                22.5,
                67.5);

        var lakeCell =
            setup.Grid.LocateCell(
                -67.5,
                -157.5);

        var hydrology =
            CreateHydrology(
                setup,
                new Dictionary<
                    SurfaceCellId,
                    double>
                {
                    [firstOceanCell.Id] = 1_000,
                    [secondOceanCell.Id] = 1_000,
                    [lakeCell.Id] = 1_000
                });

        var standingWater =
            PlanetStandingWaterState.Derive(
                setup.Planet,
                setup.Terrain,
                hydrology);

        Assert.Equal(
            2,
            standingWater.WaterBodies.Length);

        var ocean =
            Assert.Single(
                standingWater.WaterBodies.Where(
                    body =>
                        body.Kind ==
                        StandingWaterKind.Ocean));

        var lake =
            Assert.Single(
                standingWater.WaterBodies.Where(
                    body =>
                        body.Kind ==
                        StandingWaterKind.Lake));

        Assert.Equal(
            2,
            ocean.CellIds.Length);

        Assert.Single(
            lake.CellIds);

        Assert.Equal(
            StandingWaterKind.Ocean,
            standingWater.GetCell(
                firstOceanCell.Id)
                .Kind);

        Assert.Equal(
            StandingWaterKind.Ocean,
            standingWater.GetCell(
                secondOceanCell.Id)
                .Kind);

        Assert.Equal(
            StandingWaterKind.Lake,
            standingWater.GetCell(
                lakeCell.Id)
                .Kind);
    }

    [Fact]
    public void Derive_WaterBodyAnchorIsDeterministic()
    {
        var setup =
            CreateSetup();

        var first =
            setup.Grid.LocateCell(
                22.5,
                22.5);

        var second =
            setup.Grid.LocateCell(
                22.5,
                67.5);

        var hydrology =
            CreateHydrology(
                setup,
                new Dictionary<
                    SurfaceCellId,
                    double>
                {
                    [first.Id] = 1_000,
                    [second.Id] = 1_000
                });

        var firstResult =
            PlanetStandingWaterState.Derive(
                setup.Planet,
                setup.Terrain,
                hydrology);

        var secondResult =
            PlanetStandingWaterState.Derive(
                setup.Planet,
                setup.Terrain,
                hydrology);

        Assert.Equal(
            Assert.Single(
                firstResult.WaterBodies)
                .AnchorCellId,
            Assert.Single(
                secondResult.WaterBodies)
                .AnchorCellId);
    }

    [Fact]
    public void Derive_WaterBodyVolumeUsesPhysicalCellArea()
    {
        var setup =
            CreateSetup();

        var flooded =
            setup.Grid.LocateCell(
                67.5,
                22.5);

        const double waterKilogramsPerSquareMeter =
            3_000;

        var hydrology =
            CreateHydrology(
                setup,
                new Dictionary<
                    SurfaceCellId,
                    double>
                {
                    [flooded.Id] =
                        waterKilogramsPerSquareMeter
                });

        var standingWater =
            PlanetStandingWaterState.Derive(
                setup.Planet,
                setup.Terrain,
                hydrology);

        var body =
            Assert.Single(
                standingWater.WaterBodies);

        var expectedVolume =
            3 *
            flooded.AreaSquareMeters;

        Assert.Equal(
            expectedVolume,
            body.WaterVolumeCubicMeters,
            precision: 3);
    }

    private static PlanetHydrologyState CreateHydrology(
        TestSetup setup,
        IReadOnlyDictionary<
            SurfaceCellId,
            double> surfaceWaterByCell)
    {
        return new PlanetHydrologyState(
            setup.Planet.Id,
            setup.Definition,
            setup.Grid.Cells.Select(
                cell =>
                    new HydrologyCellState(
                        cell.Id,
                        atmosphericWaterKilogramsPerSquareMeter:
                            0,
                        surfaceLiquidWaterKilogramsPerSquareMeter:
                            surfaceWaterByCell.TryGetValue(
                                cell.Id,
                                out var surfaceWater)
                                ? surfaceWater
                                : 0,
                        soilWaterKilogramsPerSquareMeter:
                            0,
                        snowIceWaterEquivalentKilogramsPerSquareMeter:
                            0)));
    }

    private static TestSetup CreateSetup()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Standing Water World",
                5.0e24,
                6_000_000,
                new PlanetEnvironment(
                    285,
                    0.60,
                    0.05,
                    AtmosphereState.Vacuum));

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        var terrain =
            new PlanetTerrainState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    (cell, index) =>
                        new TerrainCellState(
                            cell.Id,
                            index * 10 - 100)));

        return new TestSetup(
            planet,
            definition,
            grid,
            terrain);
    }

    private sealed record TestSetup(
        PlanetState Planet,
        SurfaceGridDefinition Definition,
        IPlanetSurfaceGrid Grid,
        PlanetTerrainState Terrain);
}
