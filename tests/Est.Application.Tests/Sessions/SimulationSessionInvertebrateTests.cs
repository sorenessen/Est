using Est.Application.Sessions;
using Est.Simulation.Definitions;
using Est.Simulation.Hydrology;
using Est.Simulation.Invertebrates;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Application.Tests.Sessions;

public sealed class SimulationSessionInvertebrateTests
{
    [Fact]
    public void Advance_RunsInvertebratesAfterVegetation()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Invertebrate World",
                5.0e24,
                6_000_000,
                new PlanetEnvironment(
                    293.15,
                    0.60,
                    0.05,
                    AtmosphereState.Vacuum));

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
                            0.005)));

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [],
                [],
                [terrain],
                [hydrology],
                [vegetation],
                [invertebrates]);

        var definition =
            new SimulationDefinition(
                vegetationModels:
                [
                    new VegetationModelDefinition(
                        planet.Id,
                        new VegetationModelParameters(
                            maximumRelativeGrowthRatePerDay:
                                0))
                ],
                invertebrateModels:
                [
                    new InvertebrateModelDefinition(
                        planet.Id,
                        new InvertebrateModelParameters(
                            carryingCapacityKilogramsPerKilogramLiveVegetation:
                                0.02,
                            maximumRelativeGrowthRatePerDay:
                                0.20,
                            baselineMortalityRatePerDay:
                                0))
                ]);

        var session =
            new SimulationSession(
                world,
                definition);

        session.Advance(
            86_400);

        var events =
            session.Timeline.Events
                .ToArray();

        var vegetationIndex =
            Array.FindIndex(
                events,
                item =>
                    item.Cause ==
                    "planetary-vegetation");

        var invertebrateIndex =
            Array.FindIndex(
                events,
                item =>
                    item.Cause ==
                    "planetary-invertebrates");

        Assert.True(
            vegetationIndex >= 0);

        Assert.True(
            invertebrateIndex >
            vegetationIndex);

        Assert.True(
            Assert.Single(
                    session.CurrentWorld.Invertebrates)
                .Cells[0]
                .LiveBiomassKilogramsPerSquareMeter >
            0.005);
    }

    [Fact]
    public void Constructor_RejectsInvertebrateModelWithoutState()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Empty World",
                5.0e24,
                6_000_000,
                new PlanetEnvironment(
                    285,
                    0.60,
                    0.05,
                    AtmosphereState.Vacuum));

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet]);

        var definition =
            new SimulationDefinition(
                invertebrateModels:
                [
                    new InvertebrateModelDefinition(
                        planet.Id,
                        new InvertebrateModelParameters())
                ]);

        Assert.Throws<ArgumentException>(
            () =>
                new SimulationSession(
                    world,
                    definition));
    }
}
