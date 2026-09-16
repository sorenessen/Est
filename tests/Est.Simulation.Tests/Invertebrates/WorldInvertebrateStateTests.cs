using Est.Simulation.Hydrology;
using Est.Simulation.Invertebrates;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Invertebrates;

public sealed class WorldInvertebrateStateTests
{
    [Fact]
    public void World_StoresInvertebratesAndPreservesThemAcrossCopyAndFork()
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
                definition);

        var invertebrates =
            CreateInvertebrates(
                planet,
                definition,
                offset: 1);

        var world =
            CreateWorld(
                planet,
                terrain,
                hydrology,
                vegetation,
                invertebrates);

        Assert.Equal(
            invertebrates,
            Assert.Single(
                world.Invertebrates));

        Assert.Equal(
            world.Invertebrates,
            world.Copy().Invertebrates);

        Assert.Equal(
            world.Invertebrates,
            world.Fork().Invertebrates);
    }

    [Fact]
    public void World_RejectsInvertebratesWithoutVegetation()
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

        var invertebrates =
            CreateInvertebrates(
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
                    [terrain],
                    [hydrology],
                    [],
                    [invertebrates]));
    }

    [Fact]
    public void World_RejectsInvertebratesOnDifferentGrid()
    {
        var planet =
            CreatePlanet();

        var vegetationDefinition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var invertebrateDefinition =
            SurfaceGridDefinition.LatitudeLongitude(
                6,
                12);

        var terrain =
            CreateTerrain(
                planet,
                vegetationDefinition);

        var hydrology =
            CreateHydrology(
                planet,
                vegetationDefinition);

        var vegetation =
            CreateVegetation(
                planet,
                vegetationDefinition);

        var invertebrates =
            CreateInvertebrates(
                planet,
                invertebrateDefinition);

        Assert.Throws<ArgumentException>(
            () =>
                CreateWorld(
                    planet,
                    terrain,
                    hydrology,
                    vegetation,
                    invertebrates));
    }

    [Fact]
    public void World_RejectsDuplicateInvertebratesForPlanet()
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
                definition);

        var first =
            CreateInvertebrates(
                planet,
                definition,
                offset: 1);

        var second =
            CreateInvertebrates(
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
                    [terrain],
                    [hydrology],
                    [vegetation],
                    [first, second]));
    }

    [Fact]
    public void ReplacePlanetInvertebrateStateOperation_ReplacesOnlyTargetPlanet()
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
                definition);

        var secondVegetation =
            CreateVegetation(
                secondPlanet,
                definition);

        var firstInvertebrates =
            CreateInvertebrates(
                firstPlanet,
                definition,
                offset: 1);

        var secondInvertebrates =
            CreateInvertebrates(
                secondPlanet,
                definition,
                offset: 2);

        var replacement =
            CreateInvertebrates(
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
                [firstTerrain, secondTerrain],
                [firstHydrology, secondHydrology],
                [firstVegetation, secondVegetation],
                [firstInvertebrates, secondInvertebrates]);

        var changed =
            new ReplacePlanetInvertebrateStateOperation(
                replacement)
            .Apply(
                world);

        Assert.Equal(
            replacement,
            changed.Invertebrates.Single(
                item =>
                    item.PlanetId ==
                    firstPlanet.Id));

        Assert.Equal(
            secondInvertebrates,
            changed.Invertebrates.Single(
                item =>
                    item.PlanetId ==
                    secondPlanet.Id));

        Assert.Equal(
            firstInvertebrates,
            world.Invertebrates.Single(
                item =>
                    item.PlanetId ==
                    firstPlanet.Id));
    }

    private static WorldState CreateWorld(
        PlanetState planet,
        PlanetTerrainState terrain,
        PlanetHydrologyState hydrology,
        PlanetVegetationState vegetation,
        PlanetInvertebrateState invertebrates)
    {
        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [planet],
            [],
            [],
            [terrain],
            [hydrology],
            [vegetation],
            [invertebrates]);
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
        SurfaceGridDefinition definition)
    {
        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        return new PlanetVegetationState(
            planet.Id,
            definition,
            grid.Cells.Select(
                cell =>
                    new VegetationCellState(
                        cell.Id,
                        1)));
    }

    private static PlanetInvertebrateState CreateInvertebrates(
        PlanetState planet,
        SurfaceGridDefinition definition,
        double offset = 0)
    {
        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        return new PlanetInvertebrateState(
            planet.Id,
            definition,
            grid.Cells.Select(
                (cell, index) =>
                    new InvertebrateCellState(
                        cell.Id,
                        offset +
                        index * 0.01)));
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Invertebrate World",
            5.0e24,
            6_000_000,
            new PlanetEnvironment(
                285,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
