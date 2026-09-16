using Est.Simulation.Biogeochemistry;
using Est.Simulation.Hydrology;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Biogeochemistry;

public sealed class WorldBiogeochemistryStateTests
{
    [Fact]
    public void World_StoresBiogeochemistryAndPreservesItAcrossCopyAndFork()
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

        var biogeochemistry =
            CreateBiogeochemistry(
                planet,
                definition,
                offset: 1);

        var world =
            CreateWorld(
                planet,
                terrain,
                hydrology,
                biogeochemistry);

        Assert.Equal(
            biogeochemistry,
            Assert.Single(
                world.Biogeochemistry));

        Assert.Equal(
            world.Biogeochemistry,
            world.Copy().Biogeochemistry);

        Assert.Equal(
            world.Biogeochemistry,
            world.Fork().Biogeochemistry);
    }

    [Fact]
    public void World_RejectsBiogeochemistryWithoutHydrology()
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

        var biogeochemistry =
            CreateBiogeochemistry(
                planet,
                definition);

        Assert.Throws<ArgumentException>(
            () =>
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet],
                    [],
                    terrain:
                    [
                        terrain
                    ],
                    biogeochemistry:
                    [
                        biogeochemistry
                    ]));
    }

    [Fact]
    public void World_RejectsBiogeochemistryOnDifferentGrid()
    {
        var planet =
            CreatePlanet();

        var environmentDefinition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var biogeochemistryDefinition =
            SurfaceGridDefinition.LatitudeLongitude(
                6,
                12);

        var terrain =
            CreateTerrain(
                planet,
                environmentDefinition);

        var hydrology =
            CreateHydrology(
                planet,
                environmentDefinition);

        var biogeochemistry =
            CreateBiogeochemistry(
                planet,
                biogeochemistryDefinition);

        Assert.Throws<ArgumentException>(
            () =>
                CreateWorld(
                    planet,
                    terrain,
                    hydrology,
                    biogeochemistry));
    }

    [Fact]
    public void World_RejectsDuplicateBiogeochemistryForPlanet()
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
            CreateBiogeochemistry(
                planet,
                definition,
                offset: 1);

        var second =
            CreateBiogeochemistry(
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
                    terrain:
                    [
                        terrain
                    ],
                    hydrology:
                    [
                        hydrology
                    ],
                    biogeochemistry:
                    [
                        first,
                        second
                    ]));
    }

    [Fact]
    public void ReplacePlanetBiogeochemistryStateOperation_ReplacesOnlyTargetPlanet()
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

        var first =
            CreateBiogeochemistry(
                firstPlanet,
                definition,
                offset: 1);

        var second =
            CreateBiogeochemistry(
                secondPlanet,
                definition,
                offset: 2);

        var replacement =
            CreateBiogeochemistry(
                firstPlanet,
                definition,
                offset: 20);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [
                    firstPlanet,
                    secondPlanet
                ],
                [],
                terrain:
                [
                    firstTerrain,
                    secondTerrain
                ],
                hydrology:
                [
                    firstHydrology,
                    secondHydrology
                ],
                biogeochemistry:
                [
                    first,
                    second
                ]);

        var changed =
            new ReplacePlanetBiogeochemistryStateOperation(
                replacement)
            .Apply(
                world);

        Assert.Equal(
            replacement,
            changed.Biogeochemistry.Single(
                item =>
                    item.PlanetId ==
                    firstPlanet.Id));

        Assert.Equal(
            second,
            changed.Biogeochemistry.Single(
                item =>
                    item.PlanetId ==
                    secondPlanet.Id));

        Assert.Equal(
            first,
            world.Biogeochemistry.Single(
                item =>
                    item.PlanetId ==
                    firstPlanet.Id));
    }

    private static WorldState CreateWorld(
        PlanetState planet,
        PlanetTerrainState terrain,
        PlanetHydrologyState hydrology,
        PlanetBiogeochemistryState biogeochemistry)
    {
        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [planet],
            [],
            terrain:
            [
                terrain
            ],
            hydrology:
            [
                hydrology
            ],
            biogeochemistry:
            [
                biogeochemistry
            ]);
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

    private static PlanetBiogeochemistryState CreateBiogeochemistry(
        PlanetState planet,
        SurfaceGridDefinition definition,
        double offset = 0)
    {
        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        return new PlanetBiogeochemistryState(
            planet.Id,
            definition,
            grid.Cells.Select(
                (cell, index) =>
                    new BiogeochemistryCellState(
                        cell.Id,
                        offset +
                        index * 0.01,
                        0.03 +
                        index * 0.001,
                        0.01 +
                        index * 0.0005)));
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Biogeochemistry World",
            5.0e24,
            6_000_000,
            new PlanetEnvironment(
                285,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
