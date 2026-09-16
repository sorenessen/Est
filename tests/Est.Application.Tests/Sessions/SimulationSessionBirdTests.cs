using Est.Application.Sessions;
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

namespace Est.Application.Tests.Sessions;

public sealed class SimulationSessionBirdTests
{
    [Fact]
    public void Advance_RunsBirdsAfterInvertebrates()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Bird Session World",
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

        var startCell =
            grid.Cells[4];

        var flock =
            new BirdFlockState(
                BirdFlockId.New(),
                planet.Id,
                100,
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
                invertebrates:
                [
                    invertebrates
                ],
                birdFlocks:
                [
                    flock
                ]);

        var definition =
            new SimulationDefinition(
                invertebrateModels:
                [
                    new InvertebrateModelDefinition(
                        planet.Id,
                        new InvertebrateModelParameters(
                            maximumRelativeGrowthRatePerDay:
                                0,
                            baselineMortalityRatePerDay:
                                0))
                ],
                birdModels:
                [
                    new BirdModelDefinition(
                        planet.Id,
                        new BirdModelParameters(
                            maximumTravelMetersPerDay:
                                0,
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

        var events =
            session.Timeline.Events
                .ToArray();

        var invertebrateIndex =
            Array.FindIndex(
                events,
                item =>
                    item.Cause ==
                    "planetary-invertebrates");

        var birdIndex =
            Array.FindIndex(
                events,
                item =>
                    item.Cause ==
                    "planetary-birds");

        Assert.True(
            invertebrateIndex >= 0);

        Assert.True(
            birdIndex >
            invertebrateIndex);

        var changedFlock =
            Assert.Single(
                session.CurrentWorld.BirdFlocks);

        Assert.Equal(
            flock.Id,
            changedFlock.Id);

        Assert.Equal(
            100,
            changedFlock.MemberCount);
    }
}
