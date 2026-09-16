using Est.Application.Sessions;
using Est.Simulation.Definitions;
using Est.Simulation.Hydrology;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Application.Tests.Sessions;

public sealed class SimulationSessionVegetationTests
{
    [Fact]
    public void Advance_RunsVegetationAfterHydrology()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Green World",
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
                            atmosphericWaterKilogramsPerSquareMeter:
                                0,
                            surfaceLiquidWaterKilogramsPerSquareMeter:
                                0,
                            soilWaterKilogramsPerSquareMeter:
                                100,
                            snowIceWaterEquivalentKilogramsPerSquareMeter:
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
                [],
                [terrain],
                [hydrology],
                [vegetation]);

        var definition =
            new SimulationDefinition(
                hydrologyModels:
                [
                    new HydrologyModelDefinition(
                        planet.Id,
                        new HydrologyModelParameters(
                            maximumEvaporationRateKilogramsPerSquareMeterPerDay:
                                0,
                            atmosphericPrecipitationThresholdKilogramsPerSquareMeter:
                                0,
                            maximumPrecipitationRateKilogramsPerSquareMeterPerDay:
                                0,
                            maximumInfiltrationRateKilogramsPerSquareMeterPerDay:
                                0,
                            maximumRunoffRateKilogramsPerSquareMeterPerDay:
                                0,
                            maximumFreezingRateKilogramsPerSquareMeterPerDay:
                                0,
                            maximumMeltingRateKilogramsPerSquareMeterPerDay:
                                0)),
                ],
                vegetationModels:
                [
                    new VegetationModelDefinition(
                        planet.Id,
                        new VegetationModelParameters())
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

        var hydrologyIndex =
            Array.FindIndex(
                events,
                item =>
                    item.Cause ==
                    "planetary-hydrology");

        var vegetationIndex =
            Array.FindIndex(
                events,
                item =>
                    item.Cause ==
                    "planetary-vegetation");

        Assert.True(
            hydrologyIndex >= 0);

        Assert.True(
            vegetationIndex >
            hydrologyIndex);

        Assert.True(
            Assert.Single(
                    session.CurrentWorld.Vegetation)
                .Cells[0]
                .LiveBiomassKilogramsPerSquareMeter >
            1);
    }
}
