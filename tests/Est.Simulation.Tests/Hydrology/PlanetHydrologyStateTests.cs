using Est.Simulation.Hydrology;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Hydrology;

public sealed class PlanetHydrologyStateTests
{
    [Fact]
    public void Cell_RejectsNegativeOrNonFiniteWaterStores()
    {
        var cellId =
            new SurfaceCellId(
                Guid.NewGuid());

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new HydrologyCellState(
                    cellId,
                    -1,
                    0,
                    0,
                    0));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new HydrologyCellState(
                    cellId,
                    0,
                    double.NaN,
                    0,
                    0));
    }

    [Fact]
    public void Hydrology_ValidatesCompleteSurfaceCoverage()
    {
        var planet =
            CreatePlanet();

        var terrain =
            CreateTerrain(
                planet);

        var hydrology =
            CreateHydrology(
                planet,
                terrain.GridDefinition);

        hydrology.ValidateFor(
            planet);

        Assert.Equal(
            terrain.Cells.Length,
            hydrology.Cells.Length);
    }

    [Fact]
    public void Hydrology_TotalWaterMassUsesPhysicalCellArea()
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

        const double waterPerSquareMeter =
            12.5;

        var hydrology =
            new PlanetHydrologyState(
                planet.Id,
                terrain.GridDefinition,
                grid.Cells.Select(
                    cell =>
                        new HydrologyCellState(
                            cell.Id,
                            2.5,
                            4,
                            5,
                            1)));

        var expectedMass =
            waterPerSquareMeter *
            grid.TotalSurfaceAreaSquareMeters;

        var actualMass =
            hydrology.TotalWaterMassKilograms(
                planet);

        var relativeError =
            Math.Abs(
                actualMass -
                expectedMass) /
            expectedMass;

        Assert.InRange(
            relativeError,
            0,
            1e-12);
    }

    [Fact]
    public void World_StoresHydrologyAndPreservesItAcrossCopyAndFork()
    {
        var planet =
            CreatePlanet();

        var terrain =
            CreateTerrain(
                planet);

        var hydrology =
            CreateHydrology(
                planet,
                terrain.GridDefinition);

        var world =
            CreateWorld(
                planet,
                terrain,
                hydrology);

        Assert.Equal(
            hydrology,
            Assert.Single(
                world.Hydrology));

        Assert.Equal(
            world.Hydrology,
            world.Copy().Hydrology);

        Assert.Equal(
            world.Hydrology,
            world.Fork().Hydrology);
    }

    [Fact]
    public void World_RejectsHydrologyWithoutTerrain()
    {
        var planet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var hydrology =
            CreateHydrology(
                planet,
                definition);

        Assert.Throws<ArgumentException>(
            () =>
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet],
                    [],
                    [],
                    [],
                    [hydrology]));
    }

    [Fact]
    public void World_RejectsHydrologyOnDifferentGridFromTerrain()
    {
        var planet =
            CreatePlanet();

        var terrain =
            CreateTerrain(
                planet,
                SurfaceGridDefinition.LatitudeLongitude(
                    4,
                    8));

        var hydrology =
            CreateHydrology(
                planet,
                SurfaceGridDefinition.LatitudeLongitude(
                    6,
                    12));

        Assert.Throws<ArgumentException>(
            () =>
                CreateWorld(
                    planet,
                    terrain,
                    hydrology));
    }

    [Fact]
    public void World_RejectsDuplicateHydrologyForPlanet()
    {
        var planet =
            CreatePlanet();

        var terrain =
            CreateTerrain(
                planet);

        var first =
            CreateHydrology(
                planet,
                terrain.GridDefinition);

        var second =
            CreateHydrology(
                planet,
                terrain.GridDefinition);

        Assert.Throws<ArgumentException>(
            () =>
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet],
                    [],
                    [],
                    [terrain],
                    [first, second]));
    }

    [Fact]
    public void ReplacePlanetHydrologyStateOperation_ReplacesOnlyTargetPlanet()
    {
        var firstPlanet =
            CreatePlanet();

        var secondPlanet =
            CreatePlanet();

        var firstTerrain =
            CreateTerrain(
                firstPlanet);

        var secondTerrain =
            CreateTerrain(
                secondPlanet);

        var firstHydrology =
            CreateHydrology(
                firstPlanet,
                firstTerrain.GridDefinition,
                offset: 1);

        var secondHydrology =
            CreateHydrology(
                secondPlanet,
                secondTerrain.GridDefinition,
                offset: 2);

        var replacement =
            CreateHydrology(
                firstPlanet,
                firstTerrain.GridDefinition,
                offset: 20);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [firstPlanet, secondPlanet],
                [],
                [],
                [firstTerrain, secondTerrain],
                [firstHydrology, secondHydrology]);

        var changed =
            new ReplacePlanetHydrologyStateOperation(
                replacement)
            .Apply(
                world);

        Assert.Equal(
            replacement,
            changed.Hydrology.Single(
                item =>
                    item.PlanetId ==
                    firstPlanet.Id));

        Assert.Equal(
            secondHydrology,
            changed.Hydrology.Single(
                item =>
                    item.PlanetId ==
                    secondPlanet.Id));

        Assert.Equal(
            firstHydrology,
            world.Hydrology.Single(
                item =>
                    item.PlanetId ==
                    firstPlanet.Id));
    }

    private static WorldState CreateWorld(
        PlanetState planet,
        PlanetTerrainState terrain,
        PlanetHydrologyState hydrology)
    {
        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [planet],
            [],
            [],
            [terrain],
            [hydrology]);
    }

    private static PlanetHydrologyState CreateHydrology(
        PlanetState planet,
        SurfaceGridDefinition definition,
        double offset = 0)
    {
        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        return new PlanetHydrologyState(
            planet.Id,
            definition,
            grid.Cells.Select(
                (cell, index) =>
                    new HydrologyCellState(
                        cell.Id,
                        atmosphericWaterKilogramsPerSquareMeter:
                            offset + 1,
                        surfaceLiquidWaterKilogramsPerSquareMeter:
                            offset + index,
                        soilWaterKilogramsPerSquareMeter:
                            offset + 2,
                        snowIceWaterEquivalentKilogramsPerSquareMeter:
                            offset + 3)));
    }

    private static PlanetTerrainState CreateTerrain(
        PlanetState planet,
        SurfaceGridDefinition? definition = null)
    {
        definition ??=
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
                        index)));
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Hydrology World",
            5.0e24,
            6_000_000,
            new PlanetEnvironment(
                285,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
