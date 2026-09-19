using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Thermal;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Thermal;

public sealed class WorldRegionalThermalStateTests
{
    [Fact]
    public void World_StoresRegionalThermalStateAndPreservesItAcrossCopyAndFork()
    {
        var planet =
            CreatePlanet();

        var terrain =
            CreateTerrain(
                planet);

        var thermal =
            CreateThermalState(
                planet,
                terrain.GridDefinition);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [],
                terrain: [terrain],
                regionalThermal: [thermal]);

        Assert.Equal(
            thermal,
            Assert.Single(
                world.RegionalThermal));

        Assert.Equal(
            world.RegionalThermal,
            world.Copy().RegionalThermal);

        Assert.Equal(
            world.RegionalThermal,
            world.Fork().RegionalThermal);
    }

    [Fact]
    public void World_RejectsRegionalThermalStateWithoutTerrain()
    {
        var planet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var thermal =
            CreateThermalState(
                planet,
                definition);

        Assert.Throws<ArgumentException>(
            () =>
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet],
                    [],
                    regionalThermal: [thermal]));
    }

    [Fact]
    public void World_RejectsRegionalThermalStateOnDifferentGridFromTerrain()
    {
        var planet =
            CreatePlanet();

        var terrain =
            CreateTerrain(
                planet,
                SurfaceGridDefinition.LatitudeLongitude(
                    4,
                    8));

        var thermal =
            CreateThermalState(
                planet,
                SurfaceGridDefinition.LatitudeLongitude(
                    6,
                    12));

        Assert.Throws<ArgumentException>(
            () =>
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet],
                    [],
                    terrain: [terrain],
                    regionalThermal: [thermal]));
    }

    [Fact]
    public void World_RejectsDuplicateRegionalThermalStateForPlanet()
    {
        var planet =
            CreatePlanet();

        var terrain =
            CreateTerrain(
                planet);

        var first =
            CreateThermalState(
                planet,
                terrain.GridDefinition,
                offset: 0);

        var second =
            CreateThermalState(
                planet,
                terrain.GridDefinition,
                offset: 10);

        Assert.Throws<ArgumentException>(
            () =>
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet],
                    [],
                    terrain: [terrain],
                    regionalThermal:
                        [first, second]));
    }

    [Fact]
    public void World_RejectsRegionalThermalStateForUnknownPlanet()
    {
        var planet =
            CreatePlanet();

        var otherPlanet =
            CreatePlanet();

        var terrain =
            CreateTerrain(
                planet);

        var otherTerrain =
            CreateTerrain(
                otherPlanet);

        var thermal =
            CreateThermalState(
                otherPlanet,
                otherTerrain.GridDefinition);

        Assert.Throws<ArgumentException>(
            () =>
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet],
                    [],
                    terrain: [terrain],
                    regionalThermal: [thermal]));
    }

    [Fact]
    public void ReplacePlanetRegionalThermalStateOperation_ReplacesOnlyTargetPlanet()
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

        var firstThermal =
            CreateThermalState(
                firstPlanet,
                firstTerrain.GridDefinition,
                offset: 1);

        var secondThermal =
            CreateThermalState(
                secondPlanet,
                secondTerrain.GridDefinition,
                offset: 2);

        var replacement =
            CreateThermalState(
                firstPlanet,
                firstTerrain.GridDefinition,
                offset: 20);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [firstPlanet, secondPlanet],
                [],
                terrain:
                    [firstTerrain, secondTerrain],
                regionalThermal:
                    [firstThermal, secondThermal]);

        var changed =
            new ReplacePlanetRegionalThermalStateOperation(
                replacement)
            .Apply(
                world);

        Assert.Equal(
            replacement,
            changed.RegionalThermal.Single(
                item =>
                    item.PlanetId ==
                    firstPlanet.Id));

        Assert.Equal(
            secondThermal,
            changed.RegionalThermal.Single(
                item =>
                    item.PlanetId ==
                    secondPlanet.Id));

        Assert.Equal(
            firstThermal,
            world.RegionalThermal.Single(
                item =>
                    item.PlanetId ==
                    firstPlanet.Id));

        Assert.Equal(
            world.Id,
            changed.Id);

        Assert.Equal(
            world.CurrentTime,
            changed.CurrentTime);
    }

    [Fact]
    public void ReplacePlanetRegionalThermalStateOperation_RejectsUnknownPlanet()
    {
        var planet =
            CreatePlanet();

        var terrain =
            CreateTerrain(
                planet);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [],
                terrain: [terrain]);

        var otherPlanet =
            CreatePlanet();

        var otherTerrain =
            CreateTerrain(
                otherPlanet);

        var replacement =
            CreateThermalState(
                otherPlanet,
                otherTerrain.GridDefinition);

        Assert.Throws<PlanetNotFoundException>(
            () =>
                new ReplacePlanetRegionalThermalStateOperation(
                    replacement)
                .Apply(
                    world));
    }

    private static PlanetRegionalThermalState
        CreateThermalState(
            PlanetState planet,
            SurfaceGridDefinition definition,
            double offset = 0)
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
                        280 + offset +
                        index * 0.1,
                        240 + offset +
                        index * 0.05)));
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
            "Regional Thermal World",
            5.0e24,
            6_000_000,
            new PlanetEnvironment(
                285,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
