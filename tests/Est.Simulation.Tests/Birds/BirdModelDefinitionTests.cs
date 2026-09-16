using Est.Simulation.Birds;
using Est.Simulation.Definitions;
using Est.Simulation.Hydrology;
using Est.Simulation.Invertebrates;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Birds;

public sealed class BirdModelDefinitionTests
{
    [Fact]
    public void Constructor_RejectsDuplicatePlanetModels()
    {
        var planetId =
            PlanetId.New();

        Assert.Throws<ArgumentException>(
            () =>
                new SimulationDefinition(
                    birdModels:
                    [
                        new BirdModelDefinition(
                            planetId,
                            new BirdModelParameters()),
                        new BirdModelDefinition(
                            planetId,
                            new BirdModelParameters())
                    ]));
    }

    [Fact]
    public void ValidateFor_RequiresAuthoritativeInvertebrateState()
    {
        var planet =
            CreatePlanet();

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                []);

        var definition =
            new SimulationDefinition(
                birdModels:
                [
                    new BirdModelDefinition(
                        planet.Id,
                        new BirdModelParameters())
                ]);

        Assert.Throws<ArgumentException>(
            () =>
                definition.ValidateFor(
                    world));
    }

    [Fact]
    public void ValidateFor_AllowsBirdModelWithInvertebrateState()
    {
        var planet =
            CreatePlanet();

        var gridDefinition =
            SurfaceGridDefinition.LatitudeLongitude(
                2,
                4);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                gridDefinition);

        var terrain =
            new PlanetTerrainState(
                planet.Id,
                gridDefinition,
                grid.Cells.Select(
                    cell =>
                        new TerrainCellState(
                            cell.Id,
                            0)));

        var hydrology =
            new PlanetHydrologyState(
                planet.Id,
                gridDefinition,
                grid.Cells.Select(
                    cell =>
                        new HydrologyCellState(
                            cell.Id,
                            0,
                            0,
                            100,
                            0)));

        var vegetation =
            new PlanetVegetationState(
                planet.Id,
                gridDefinition,
                grid.Cells.Select(
                    cell =>
                        new VegetationCellState(
                            cell.Id,
                            1)));

        var invertebrates =
            new PlanetInvertebrateState(
                planet.Id,
                gridDefinition,
                grid.Cells.Select(
                    cell =>
                        new InvertebrateCellState(
                            cell.Id,
                            0)));

        var world =
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
                vegetation:
                [
                    vegetation
                ],
                invertebrates:
                [
                    invertebrates
                ]);

        var definition =
            new SimulationDefinition(
                birdModels:
                [
                    new BirdModelDefinition(
                        planet.Id,
                        new BirdModelParameters())
                ]);

        definition.ValidateFor(
            world);
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Bird Model World",
            5.0e24,
            6_000_000,
            new PlanetEnvironment(
                285,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
