using Est.Application.Sessions;
using Est.Simulation.Biogeochemistry;
using Est.Simulation.Definitions;
using Est.Simulation.Hydrology;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Application.Tests.Sessions;

public sealed class SimulationSessionBiogeochemistryTests
{
    private const long OneDaySeconds =
        86_400;

    [Fact]
    public void Advance_ClosesPlantNitrogenLoopThroughDecompositionGrowthAndMortality()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Nutrient Cycle World",
                5.9722e24,
                6_371_000,
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

        var biogeochemistry =
            new PlanetBiogeochemistryState(
                planet.Id,
                gridDefinition,
                grid.Cells.Select(
                    cell =>
                        new BiogeochemistryCellState(
                            cell.Id,
                            detritalBiomassKilogramsPerSquareMeter:
                                1,
                            detritalNitrogenKilogramsPerSquareMeter:
                                0.02,
                            plantAvailableNitrogenKilogramsPerSquareMeter:
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
                biogeochemistry:
                [
                    biogeochemistry
                ]);

        var definition =
            new SimulationDefinition(
                vegetationModels:
                [
                    new VegetationModelDefinition(
                        planet.Id,
                        new VegetationModelParameters(
                            maximumIntegrationStepSeconds:
                                OneDaySeconds,
                            carryingCapacityKilogramsPerSquareMeter:
                                5,
                            maximumRelativeGrowthRatePerDay:
                                1,
                            soilWaterForFullProductivityKilogramsPerSquareMeter:
                                50,
                            minimumGrowthTemperatureKelvin:
                                263.15,
                            optimumGrowthTemperatureKelvin:
                                293.15,
                            maximumGrowthTemperatureKelvin:
                                313.15,
                            temperatureLapseRateKelvinPerMeter:
                                0.0065,
                            plantNitrogenKilogramsPerKilogramLiveBiomass:
                                0.02,
                            baselineMortalityRatePerDay:
                                0.10)),
                ],
                biogeochemistryModels:
                [
                    new BiogeochemistryModelDefinition(
                        planet.Id,
                        new BiogeochemistryModelParameters(
                            maximumIntegrationStepSeconds:
                                OneDaySeconds,
                            maximumRelativeDecompositionRatePerDay:
                                1,
                            soilWaterForFullDecompositionKilogramsPerSquareMeter:
                                50,
                            minimumDecompositionTemperatureKelvin:
                                263.15,
                            optimumDecompositionTemperatureKelvin:
                                293.15,
                            maximumDecompositionTemperatureKelvin:
                                313.15,
                            temperatureLapseRateKelvinPerMeter:
                                0.0065))
                ]);

        var session =
            new SimulationSession(
                world,
                definition);

        session.Advance(
            OneDaySeconds);

        var finalVegetation =
            Assert.Single(
                session.CurrentWorld.Vegetation);

        var finalBiogeochemistry =
            Assert.Single(
                session.CurrentWorld.Biogeochemistry);

        Assert.All(
            finalVegetation.Cells,
            cell =>
                Assert.Equal(
                    1.7,
                    cell.LiveBiomassKilogramsPerSquareMeter,
                    12));

        var finalVegetationByCellId =
            finalVegetation.Cells.ToDictionary(
                cell =>
                    cell.CellId);

        Assert.All(
            finalBiogeochemistry.Cells,
            cell =>
            {
                Assert.Equal(
                    0.1,
                    cell.DetritalBiomassKilogramsPerSquareMeter,
                    12);

                Assert.Equal(
                    0.002,
                    cell.DetritalNitrogenKilogramsPerSquareMeter,
                    12);

                Assert.Equal(
                    0.004,
                    cell.PlantAvailableNitrogenKilogramsPerSquareMeter,
                    12);

                var livePlantNitrogen =
                    finalVegetationByCellId[cell.CellId]
                        .LiveBiomassKilogramsPerSquareMeter *
                    0.02;

                var totalTrackedNitrogen =
                    livePlantNitrogen +
                    cell.DetritalNitrogenKilogramsPerSquareMeter +
                    cell.PlantAvailableNitrogenKilogramsPerSquareMeter;

                Assert.Equal(
                    0.04,
                    totalTrackedNitrogen,
                    12);
            });
    }
}
