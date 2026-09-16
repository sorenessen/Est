using Est.Simulation.Biogeochemistry;
using Est.Simulation.Hydrology;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Vegetation;

public sealed class VegetationMortalityDetritusTests
{
    private const long OneDaySeconds =
        86_400;

    [Theory]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    public void Parameters_RejectInvalidBaselineMortality(
        double mortalityRate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new VegetationModelParameters(
                    plantNitrogenKilogramsPerKilogramLiveBiomass:
                        0.02,
                    baselineMortalityRatePerDay:
                        mortalityRate));
    }

    [Fact]
    public void Parameters_MortalityRequiresPlantNitrogenRatio()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new VegetationModelParameters(
                    baselineMortalityRatePerDay:
                        0.02));
    }

    [Fact]
    public void Evaluate_MortalityTransfersPlantBiomassAndNitrogenToDetritus()
    {
        var setup =
            CreateWorld();

        const double nitrogenRatio =
            0.02;

        var system =
            new VegetationSystem(
                setup.Planet.Id,
                new VegetationModelParameters(
                    maximumIntegrationStepSeconds:
                        OneDaySeconds,
                    carryingCapacityKilogramsPerSquareMeter:
                        5,
                    maximumRelativeGrowthRatePerDay:
                        0,
                    soilWaterForFullProductivityKilogramsPerSquareMeter:
                        50,
                    minimumGrowthTemperatureKelvin:
                        273.15,
                    optimumGrowthTemperatureKelvin:
                        293.15,
                    maximumGrowthTemperatureKelvin:
                        313.15,
                    temperatureLapseRateKelvinPerMeter:
                        0.0065,
                    plantNitrogenKilogramsPerKilogramLiveBiomass:
                        nitrogenRatio,
                    baselineMortalityRatePerDay:
                        0.10));

        var change =
            system.Evaluate(
                setup.World,
                OneDaySeconds);

        var operation =
            Assert.IsType<
                ReplacePlanetVegetationBiogeochemistryStateOperation>(
                    change.Operation);

        var vegetation =
            operation.Vegetation.GetCell(
                setup.TargetCellId);

        var biogeochemistry =
            operation.Biogeochemistry.GetCell(
                setup.TargetCellId);

        Assert.Equal(
            0.9,
            vegetation
                .LiveBiomassKilogramsPerSquareMeter,
            12);

        Assert.Equal(
            0.6,
            biogeochemistry
                .DetritalBiomassKilogramsPerSquareMeter,
            12);

        Assert.Equal(
            0.012,
            biogeochemistry
                .DetritalNitrogenKilogramsPerSquareMeter,
            12);

        Assert.Equal(
            1,
            biogeochemistry
                .PlantAvailableNitrogenKilogramsPerSquareMeter,
            12);
    }

    [Fact]
    public void Evaluate_GrowthAndMortalityPreserveTrackedNitrogen()
    {
        var setup =
            CreateWorld();

        const double nitrogenRatio =
            0.02;

        var initialVegetation =
            setup.World.Vegetation
                .Single()
                .GetCell(
                    setup.TargetCellId);

        var initialBiogeochemistry =
            setup.World.Biogeochemistry
                .Single()
                .GetCell(
                    setup.TargetCellId);

        var initialTrackedNitrogen =
            initialVegetation
                .LiveBiomassKilogramsPerSquareMeter *
            nitrogenRatio +
            initialBiogeochemistry
                .DetritalNitrogenKilogramsPerSquareMeter +
            initialBiogeochemistry
                .PlantAvailableNitrogenKilogramsPerSquareMeter;

        var change =
            new VegetationSystem(
                setup.Planet.Id,
                new VegetationModelParameters(
                    maximumIntegrationStepSeconds:
                        OneDaySeconds,
                    carryingCapacityKilogramsPerSquareMeter:
                        5,
                    maximumRelativeGrowthRatePerDay:
                        0.20,
                    soilWaterForFullProductivityKilogramsPerSquareMeter:
                        50,
                    minimumGrowthTemperatureKelvin:
                        273.15,
                    optimumGrowthTemperatureKelvin:
                        293.15,
                    maximumGrowthTemperatureKelvin:
                        313.15,
                    temperatureLapseRateKelvinPerMeter:
                        0.0065,
                    plantNitrogenKilogramsPerKilogramLiveBiomass:
                        nitrogenRatio,
                    baselineMortalityRatePerDay:
                        0.10))
            .Evaluate(
                setup.World,
                OneDaySeconds);

        var operation =
            Assert.IsType<
                ReplacePlanetVegetationBiogeochemistryStateOperation>(
                    change.Operation);

        var finalVegetation =
            operation.Vegetation.GetCell(
                setup.TargetCellId);

        var finalBiogeochemistry =
            operation.Biogeochemistry.GetCell(
                setup.TargetCellId);

        var finalTrackedNitrogen =
            finalVegetation
                .LiveBiomassKilogramsPerSquareMeter *
            nitrogenRatio +
            finalBiogeochemistry
                .DetritalNitrogenKilogramsPerSquareMeter +
            finalBiogeochemistry
                .PlantAvailableNitrogenKilogramsPerSquareMeter;

        Assert.Equal(
            initialTrackedNitrogen,
            finalTrackedNitrogen,
            12);

        Assert.Equal(
            1.06,
            finalVegetation
                .LiveBiomassKilogramsPerSquareMeter,
            12);

        Assert.Equal(
            0.012,
            finalBiogeochemistry
                .DetritalNitrogenKilogramsPerSquareMeter,
            12);

        Assert.Equal(
            0.9968,
            finalBiogeochemistry
                .PlantAvailableNitrogenKilogramsPerSquareMeter,
            12);
    }

    private static TestWorld CreateWorld()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Plant Mortality World",
                5.0e24,
                6_000_000,
                new PlanetEnvironment(
                    293.15,
                    0.60,
                    0.05,
                    AtmosphereState.Vacuum));

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        var target =
            grid.Cells[0];

        var terrain =
            new PlanetTerrainState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new TerrainCellState(
                            cell.Id,
                            0)));

        var hydrology =
            new PlanetHydrologyState(
                planet.Id,
                definition,
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
                definition,
                grid.Cells.Select(
                    cell =>
                        new VegetationCellState(
                            cell.Id,
                            1)));

        var biogeochemistry =
            new PlanetBiogeochemistryState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new BiogeochemistryCellState(
                            cell.Id,
                            detritalBiomassKilogramsPerSquareMeter:
                                0.5,
                            detritalNitrogenKilogramsPerSquareMeter:
                                0.01,
                            plantAvailableNitrogenKilogramsPerSquareMeter:
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
                ],
                biogeochemistry:
                [
                    biogeochemistry
                ]);

        return new TestWorld(
            planet,
            target.Id,
            world);
    }

    private sealed record TestWorld(
        PlanetState Planet,
        SurfaceCellId TargetCellId,
        WorldState World);
}
