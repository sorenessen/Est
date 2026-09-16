using Est.Simulation.Biogeochemistry;
using Est.Simulation.Definitions;
using Est.Simulation.Hydrology;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Vegetation;

public sealed class VegetationNitrogenCouplingTests
{
    private const long OneDaySeconds =
        86_400;

    [Fact]
    public void Parameters_RejectInvalidPlantNitrogenRatio()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new VegetationModelParameters(
                    plantNitrogenKilogramsPerKilogramLiveBiomass:
                        0));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new VegetationModelParameters(
                    plantNitrogenKilogramsPerKilogramLiveBiomass:
                        double.NaN));
    }

    [Fact]
    public void Evaluate_NitrogenCouplingConsumesAvailableNitrogenAtomically()
    {
        var setup =
            CreateWorld(
                availableNitrogen:
                    0.01);

        const double nitrogenRatio =
            0.02;

        var change =
            new VegetationSystem(
                setup.Planet.Id,
                CreateParameters(
                    nitrogenRatio))
            .Evaluate(
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

        var biomassGrowth =
            vegetation.LiveBiomassKilogramsPerSquareMeter -
            1;

        var nitrogenConsumed =
            0.01 -
            biogeochemistry
                .PlantAvailableNitrogenKilogramsPerSquareMeter;

        Assert.True(
            biomassGrowth >
            0);

        Assert.Equal(
            biomassGrowth *
            nitrogenRatio,
            nitrogenConsumed,
            12);
    }

    [Fact]
    public void Evaluate_NitrogenAvailabilityCapsGrowth()
    {
        var abundant =
            CreateWorld(
                availableNitrogen:
                    1);

        var limited =
            CreateWorld(
                availableNitrogen:
                    0.0001);

        const double nitrogenRatio =
            0.02;

        var abundantState =
            Evaluate(
                abundant,
                nitrogenRatio);

        var limitedState =
            Evaluate(
                limited,
                nitrogenRatio);

        Assert.True(
            abundantState.Vegetation
                .GetCell(
                    abundant.TargetCellId)
                .LiveBiomassKilogramsPerSquareMeter >
            limitedState.Vegetation
                .GetCell(
                    limited.TargetCellId)
                .LiveBiomassKilogramsPerSquareMeter);

        Assert.Equal(
            0,
            limitedState.Biogeochemistry
                .GetCell(
                    limited.TargetCellId)
                .PlantAvailableNitrogenKilogramsPerSquareMeter,
            12);
    }

    [Fact]
    public void Evaluate_ZeroAvailableNitrogenPreventsGrowth()
    {
        var setup =
            CreateWorld(
                availableNitrogen:
                    0);

        var state =
            Evaluate(
                setup,
                0.02);

        Assert.Equal(
            1,
            state.Vegetation
                .GetCell(
                    setup.TargetCellId)
                .LiveBiomassKilogramsPerSquareMeter,
            12);

        Assert.Equal(
            0,
            state.Biogeochemistry
                .GetCell(
                    setup.TargetCellId)
                .PlantAvailableNitrogenKilogramsPerSquareMeter,
            12);
    }

    [Fact]
    public void Evaluate_NitrogenUptakeUsesOnlyRealizedGrowthAtCarryingCapacity()
    {
        var setup =
            CreateWorld(
                availableNitrogen:
                    1);

        const double nitrogenRatio =
            0.02;

        var parameters =
            new VegetationModelParameters(
                maximumIntegrationStepSeconds:
                    OneDaySeconds,
                carryingCapacityKilogramsPerSquareMeter:
                    5,
                maximumRelativeGrowthRatePerDay:
                    20,
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
                    nitrogenRatio);

        var change =
            new VegetationSystem(
                setup.Planet.Id,
                parameters)
            .Evaluate(
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
            5,
            vegetation
                .LiveBiomassKilogramsPerSquareMeter,
            12);

        Assert.Equal(
            0.92,
            biogeochemistry
                .PlantAvailableNitrogenKilogramsPerSquareMeter,
            12);
    }

    [Fact]
    public void Definition_NitrogenCouplingRequiresBiogeochemistry()
    {
        var setup =
            CreateWorld(
                availableNitrogen:
                    0.01);

        var worldWithoutBiogeochemistry =
            new WorldState(
                setup.World.Id,
                SimulationTime.Zero,
                setup.World.Planets,
                [],
                terrain:
                    setup.World.Terrain,
                hydrology:
                    setup.World.Hydrology,
                vegetation:
                    setup.World.Vegetation);

        var definition =
            new SimulationDefinition(
                vegetationModels:
                [
                    new VegetationModelDefinition(
                        setup.Planet.Id,
                        CreateParameters(
                            0.02))
                ]);

        Assert.Throws<ArgumentException>(
            () =>
                definition.ValidateFor(
                    worldWithoutBiogeochemistry));

        definition.ValidateFor(
            setup.World);
    }

    [Fact]
    public void Evaluate_NitrogenCouplingRequiresBiogeochemistry()
    {
        var setup =
            CreateWorld(
                availableNitrogen:
                    0.01);

        var worldWithoutBiogeochemistry =
            new WorldState(
                setup.World.Id,
                SimulationTime.Zero,
                setup.World.Planets,
                [],
                terrain:
                    setup.World.Terrain,
                hydrology:
                    setup.World.Hydrology,
                vegetation:
                    setup.World.Vegetation);

        var system =
            new VegetationSystem(
                setup.Planet.Id,
                CreateParameters(
                    0.02));

        Assert.Throws<InvalidOperationException>(
            () =>
                system.Evaluate(
                    worldWithoutBiogeochemistry,
                    OneDaySeconds));
    }

    private static ReplacePlanetVegetationBiogeochemistryStateOperation
        Evaluate(
            TestWorld setup,
            double nitrogenRatio)
    {
        var change =
            new VegetationSystem(
                setup.Planet.Id,
                CreateParameters(
                    nitrogenRatio))
            .Evaluate(
                setup.World,
                OneDaySeconds);

        return Assert.IsType<
            ReplacePlanetVegetationBiogeochemistryStateOperation>(
                change.Operation);
    }

    private static VegetationModelParameters CreateParameters(
        double nitrogenRatio)
    {
        return new VegetationModelParameters(
            maximumIntegrationStepSeconds:
                21_600,
            carryingCapacityKilogramsPerSquareMeter:
                5,
            maximumRelativeGrowthRatePerDay:
                0.10,
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
                nitrogenRatio);
    }

    private static TestWorld CreateWorld(
        double availableNitrogen)
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Nitrogen Vegetation World",
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
                            0,
                            0,
                            availableNitrogen)));

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
