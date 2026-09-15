using Est.Simulation.Hydrology;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Vegetation;

public sealed class WorldVegetationStateTests
{
    [Fact]
    public void World_StoresVegetationAndPreservesItAcrossCopyAndFork()
    {
        var planet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var terrain =
            CreateTerrain(
                planet,
                definition);

        var hydrology =
            CreateHydrology(
                planet,
                definition);

        var vegetation =
            CreateVegetation(
                planet,
                definition,
                offset: 1);

        var world =
            CreateWorld(
                planet,
                terrain,
                hydrology,
                vegetation);

        Assert.Equal(
            vegetation,
            Assert.Single(
                world.Vegetation));

        Assert.Equal(
            world.Vegetation,
            world.Copy().Vegetation);

        Assert.Equal(
            world.Vegetation,
            world.Fork().Vegetation);
    }

    [Fact]
    public void World_RejectsVegetationWithoutHydrology()
    {
        var planet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var terrain =
            CreateTerrain(
                planet,
                definition);

        var vegetation =
            CreateVegetation(
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
                    [terrain],
                    [],
                    [vegetation]));
    }

    [Fact]
    public void World_RejectsVegetationOnDifferentGrid()
    {
        var planet =
            CreatePlanet();

        var terrainDefinition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var vegetationDefinition =
            SurfaceGridDefinition.LatitudeLongitude(
                6,
                12);

        var terrain =
            CreateTerrain(
                planet,
                terrainDefinition);

        var hydrology =
            CreateHydrology(
                planet,
                terrainDefinition);

        var vegetation =
            CreateVegetation(
                planet,
                vegetationDefinition);

        Assert.Throws<ArgumentException>(
            () =>
                CreateWorld(
                    planet,
                    terrain,
                    hydrology,
                    vegetation));
    }

    [Fact]
    public void World_RejectsDuplicateVegetationForPlanet()
    {
        var planet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var terrain =
            CreateTerrain(
                planet,
                definition);

        var hydrology =
            CreateHydrology(
                planet,
                definition);

        var first =
            CreateVegetation(
                planet,
                definition,
                offset: 1);

        var second =
            CreateVegetation(
                planet,
                definition,
                offset: 2);

        Assert.Throws<ArgumentException>(
            () =>
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet],
                    [],
                    [],
                    [],
                    [terrain],
                    [hydrology],
                    [first, second]));
    }

    [Fact]
    public void ReplacePlanetVegetationStateOperation_ReplacesOnlyTargetPlanet()
    {
        var firstPlanet =
            CreatePlanet();

        var secondPlanet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var firstTerrain =
            CreateTerrain(
                firstPlanet,
                definition);

        var secondTerrain =
            CreateTerrain(
                secondPlanet,
                definition);

        var firstHydrology =
            CreateHydrology(
                firstPlanet,
                definition);

        var secondHydrology =
            CreateHydrology(
                secondPlanet,
                definition);

        var firstVegetation =
            CreateVegetation(
                firstPlanet,
                definition,
                offset: 1);

        var secondVegetation =
            CreateVegetation(
                secondPlanet,
                definition,
                offset: 2);

        var replacement =
            CreateVegetation(
                firstPlanet,
                definition,
                offset: 20);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [firstPlanet, secondPlanet],
                [],
                [],
                [],
                [firstTerrain, secondTerrain],
                [firstHydrology, secondHydrology],
                [firstVegetation, secondVegetation]);

        var changed =
            new ReplacePlanetVegetationStateOperation(
                replacement)
            .Apply(
                world);

        Assert.Equal(
            replacement,
            changed.Vegetation.Single(
                item =>
                    item.PlanetId ==
                    firstPlanet.Id));

        Assert.Equal(
            secondVegetation,
            changed.Vegetation.Single(
                item =>
                    item.PlanetId ==
                    secondPlanet.Id));

        Assert.Equal(
            firstVegetation,
            world.Vegetation.Single(
                item =>
                    item.PlanetId ==
                    firstPlanet.Id));
    }

    private static WorldState CreateWorld(
        PlanetState planet,
        PlanetTerrainState terrain,
        PlanetHydrologyState hydrology,
        PlanetVegetationState vegetation)
    {
        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [planet],
            [],
            [],
            [],
            [terrain],
            [hydrology],
            [vegetation]);
    }

    private static PlanetTerrainState CreateTerrain(
        PlanetState planet,
        SurfaceGridDefinition definition)
    {
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

    private static PlanetHydrologyState CreateHydrology(
        PlanetState planet,
        SurfaceGridDefinition definition)
    {
        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        return new PlanetHydrologyState(
            planet.Id,
            definition,
            grid.Cells.Select(
                cell =>
                    new HydrologyCellState(
                        cell.Id,
                        1,
                        0,
                        100,
                        0)));
    }

    private static PlanetVegetationState CreateVegetation(
        PlanetState planet,
        SurfaceGridDefinition definition,
        double offset = 0)
    {
        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        return new PlanetVegetationState(
            planet.Id,
            definition,
            grid.Cells.Select(
                (cell, index) =>
                    new VegetationCellState(
                        cell.Id,
                        offset +
                        index)));
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Vegetation World",
            5.0e24,
            6_000_000,
            new PlanetEnvironment(
                285,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
