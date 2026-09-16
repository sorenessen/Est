using Est.Simulation.Definitions;
using Est.Simulation.Grazers;
using Est.Simulation.Hydrology;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Grazers;

public sealed class GrazerModelDefinitionTests
{
    [Fact]
    public void Constructor_RejectsDuplicatePlanetModels()
    {
        var planetId =
            PlanetId.New();

        Assert.Throws<ArgumentException>(
            () =>
                new SimulationDefinition(
                    grazerModels:
                    [
                        new GrazerModelDefinition(
                            planetId,
                            new GrazerModelParameters()),
                        new GrazerModelDefinition(
                            planetId,
                            new GrazerModelParameters())
                    ]));
    }

    [Fact]
    public void ValidateFor_RequiresAuthoritativeVegetationState()
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
                grazerModels:
                [
                    new GrazerModelDefinition(
                        planet.Id,
                        new GrazerModelParameters())
                ]);

        Assert.Throws<ArgumentException>(
            () =>
                definition.ValidateFor(
                    world));
    }

    [Fact]
    public void ValidateFor_AllowsGrazerModelWithVegetationState()
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
                ]);

        var definition =
            new SimulationDefinition(
                grazerModels:
                [
                    new GrazerModelDefinition(
                        planet.Id,
                        new GrazerModelParameters())
                ]);

        definition.ValidateFor(
            world);
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Grazer Model World",
            5.0e24,
            6_000_000,
            new PlanetEnvironment(
                285,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
