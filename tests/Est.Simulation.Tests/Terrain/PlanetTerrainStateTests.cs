using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Terrain;

public sealed class PlanetTerrainStateTests
{
    [Fact]
    public void Terrain_ValidatesCompleteSurfaceCoverage()
    {
        var planet = CreatePlanet();

        var terrain =
            CreateTerrain(
                planet);

        terrain.ValidateFor(
            planet);

        Assert.Equal(
            32,
            terrain.Cells.Length);
    }

    [Fact]
    public void Terrain_RejectsIncompleteSurfaceCoverage()
    {
        var planet = CreatePlanet();

        var gridDefinition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                gridDefinition);

        var terrain =
            new PlanetTerrainState(
                planet.Id,
                gridDefinition,
                grid.Cells
                    .Skip(1)
                    .Select(
                        cell =>
                            new TerrainCellState(
                                cell.Id,
                                0)));

        Assert.Throws<ArgumentException>(
            () =>
                terrain.ValidateFor(
                    planet));
    }

    [Fact]
    public void World_StoresTerrainAndPreservesItAcrossCopyAndFork()
    {
        var planet = CreatePlanet();
        var terrain = CreateTerrain(planet);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [],
                [],
                [],
                [terrain]);

        Assert.Equal(
            terrain,
            Assert.Single(
                world.Terrain));

        var copy = world.Copy();
        var fork = world.Fork();

        Assert.Equal(
            world.Terrain,
            copy.Terrain);

        Assert.Equal(
            world.Terrain,
            fork.Terrain);

        Assert.Equal(
            world.Id,
            copy.Id);

        Assert.NotEqual(
            world.Id,
            fork.Id);
    }

    [Fact]
    public void World_RejectsTerrainForMissingPlanet()
    {
        var planet = CreatePlanet();
        var otherPlanet = CreatePlanet();
        var terrain = CreateTerrain(otherPlanet);

        Assert.Throws<ArgumentException>(
            () =>
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet],
                    [],
                    [],
                    [],
                    [terrain]));
    }

    [Fact]
    public void World_RejectsDuplicateTerrainForPlanet()
    {
        var planet = CreatePlanet();
        var first = CreateTerrain(planet);
        var second = CreateTerrain(planet);

        Assert.Throws<ArgumentException>(
            () =>
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet],
                    [],
                    [],
                    [],
                    [first, second]));
    }

    [Fact]
    public void ReplacePlanetTerrainStateOperation_ReplacesOnlyTargetPlanet()
    {
        var firstPlanet = CreatePlanet();
        var secondPlanet = CreatePlanet();

        var firstTerrain =
            CreateTerrain(
                firstPlanet,
                elevationOffset: 100);

        var secondTerrain =
            CreateTerrain(
                secondPlanet,
                elevationOffset: 200);

        var replacement =
            CreateTerrain(
                firstPlanet,
                elevationOffset: 900);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [firstPlanet, secondPlanet],
                [],
                [],
                [],
                [firstTerrain, secondTerrain]);

        var changed =
            new ReplacePlanetTerrainStateOperation(
                replacement)
            .Apply(world);

        Assert.Equal(
            2,
            changed.Terrain.Length);

        Assert.Equal(
            replacement,
            changed.Terrain.Single(
                item =>
                    item.PlanetId ==
                    firstPlanet.Id));

        Assert.Equal(
            secondTerrain,
            changed.Terrain.Single(
                item =>
                    item.PlanetId ==
                    secondPlanet.Id));

        Assert.Equal(
            firstTerrain,
            world.Terrain.Single(
                item =>
                    item.PlanetId ==
                    firstPlanet.Id));
    }

    [Fact]
    public void SurfaceGridDefinition_RecreatesStableGrid()
    {
        var planet = CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var first =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        var second =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        Assert.Equal(
            first.Cells.Select(cell => cell.Id),
            second.Cells.Select(cell => cell.Id));
    }

    private static PlanetTerrainState
        CreateTerrain(
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
                        index)));
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Generated World",
            5.0e24,
            6_000_000,
            new PlanetEnvironment(
                285,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
