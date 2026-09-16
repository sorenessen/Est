using Est.Application.Sessions;
using Est.Simulation.Definitions;
using Est.Simulation.Grazers;
using Est.Simulation.Hydrology;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Application.Tests.Sessions;

public sealed class SimulationSessionGrazerTests
{
    [Fact]
    public void Advance_RunsGrazerSystemAndConsumesVegetation()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Grazer Session World",
                5.0e24,
                6_000_000,
                new PlanetEnvironment(
                    285,
                    0.60,
                    0.05,
                    AtmosphereState.Vacuum));

        var gridDefinition =
            SurfaceGridDefinition.LatitudeLongitude(
                3,
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
                            100,
                            100,
                            0)));

        var startCell =
            grid.Cells[4];

        const double initialVegetationMassKilograms =
            1_000;

        var vegetation =
            new PlanetVegetationState(
                planet.Id,
                gridDefinition,
                grid.Cells.Select(
                    cell =>
                        new VegetationCellState(
                            cell.Id,
                            cell.Id ==
                            startCell.Id
                                ? initialVegetationMassKilograms /
                                  cell.AreaSquareMeters
                                : 0)));

        var cohort =
            new GrazerCohortState(
                GrazerCohortId.New(),
                planet.Id,
                10,
                startCell.CenterLatitudeDegrees,
                startCell.CenterLongitudeDegrees);

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
                grazerCohorts:
                [
                    cohort
                ]);

        var definition =
            new SimulationDefinition(
                grazerModels:
                [
                    new GrazerModelDefinition(
                        planet.Id,
                        new GrazerModelParameters(
                            maximumIntegrationStepSeconds:
                                3_600,
                            maximumTravelMetersPerDay:
                                0,
                            maximumGrazeKilogramsPerGrazerPerDay:
                                24,
                            foodShortageMortalityRatePerDay:
                                0,
                            waterAbsenceMortalityRatePerDay:
                                0,
                            habitatAbsenceMortalityRatePerDay:
                                0))
                ]);

        var session =
            new SimulationSession(
                world,
                definition);

        session.Advance(
            3_600);

        var grazerEvent =
            Assert.Single(
                session.Timeline.Events,
                timelineEvent =>
                    timelineEvent.Cause ==
                    "planetary-grazers");

        Assert.Equal(
            3_600,
            grazerEvent.ElapsedSeconds);

        var changedCohort =
            Assert.Single(
                session.CurrentWorld.GrazerCohorts);

        Assert.Equal(
            cohort.Id,
            changedCohort.Id);

        Assert.Equal(
            10,
            changedCohort.MemberCount);

        var remainingVegetationMassKilograms =
            session.CurrentWorld.Vegetation
                .Single()
                .GetCell(
                    startCell.Id)
                .LiveBiomassKilogramsPerSquareMeter *
            startCell.AreaSquareMeters;

        Assert.Equal(
            990,
            remainingVegetationMassKilograms,
            6);
    }
}
