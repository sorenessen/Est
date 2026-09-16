using Est.Simulation.Hydrology;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Vegetation;

public sealed class VegetationSystemTests
{
    private const long OneDaySeconds =
        86_400;

    [Fact]
    public void Parameters_RejectInvalidPolicy()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new VegetationModelParameters(
                    maximumIntegrationStepSeconds: 0));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new VegetationModelParameters(
                    carryingCapacityKilogramsPerSquareMeter:
                        double.NaN));

        Assert.Throws<ArgumentException>(
            () =>
                new VegetationModelParameters(
                    minimumGrowthTemperatureKelvin: 295,
                    optimumGrowthTemperatureKelvin: 290,
                    maximumGrowthTemperatureKelvin: 310));

        Assert.Throws<ArgumentException>(
            () =>
                new VegetationModelParameters(
                    minimumGrowthTemperatureKelvin: 270,
                    optimumGrowthTemperatureKelvin: 310,
                    maximumGrowthTemperatureKelvin: 300));
    }

    [Fact]
    public void Evaluate_GrowsBiomassUnderSuitableConditions()
    {
        var setup =
            CreateWorld(
                temperatureKelvin: 293.15,
                elevationMeters: 0,
                soilWater: 100,
                biomass: 1);

        var system =
            new VegetationSystem(
                setup.Planet.Id,
                CreateParameters());

        var change =
            system.Evaluate(
                setup.World,
                OneDaySeconds);

        var vegetation =
            Assert.IsType<
                ReplacePlanetVegetationStateOperation>(
                change.Operation)
            .Vegetation;

        var changed =
            vegetation.GetCell(
                setup.TargetCellId);

        Assert.True(
            changed.LiveBiomassKilogramsPerSquareMeter >
            1);

        Assert.True(
            changed.LiveBiomassKilogramsPerSquareMeter <=
            5);

        Assert.Equal(
            "planetary-vegetation",
            change.Cause);

        Assert.True(
            change.Metrics[
                "biomassGrowthKilograms"] >
            0);
    }

    [Fact]
    public void Evaluate_DrySoilPreventsGrowth()
    {
        var setup =
            CreateWorld(
                temperatureKelvin: 293.15,
                elevationMeters: 0,
                soilWater: 0,
                biomass: 1);

        var change =
            new VegetationSystem(
                setup.Planet.Id,
                CreateParameters())
            .Evaluate(
                setup.World,
                OneDaySeconds);

        var vegetation =
            Assert.IsType<
                ReplacePlanetVegetationStateOperation>(
                change.Operation)
            .Vegetation;

        Assert.Equal(
            1,
            vegetation
                .GetCell(
                    setup.TargetCellId)
                .LiveBiomassKilogramsPerSquareMeter,
            12);
    }

    [Fact]
    public void Evaluate_ZeroBiomassDoesNotAppearSpontaneously()
    {
        var setup =
            CreateWorld(
                temperatureKelvin: 293.15,
                elevationMeters: 0,
                soilWater: 100,
                biomass: 0);

        var change =
            new VegetationSystem(
                setup.Planet.Id,
                CreateParameters())
            .Evaluate(
                setup.World,
                OneDaySeconds);

        var vegetation =
            Assert.IsType<
                ReplacePlanetVegetationStateOperation>(
                change.Operation)
            .Vegetation;

        Assert.Equal(
            0,
            vegetation
                .GetCell(
                    setup.TargetCellId)
                .LiveBiomassKilogramsPerSquareMeter);
    }

    [Fact]
    public void Evaluate_HigherTerrainCanReduceProductivityThroughTemperature()
    {
        var low =
            CreateWorld(
                temperatureKelvin: 293.15,
                elevationMeters: 0,
                soilWater: 100,
                biomass: 1);

        var high =
            CreateWorld(
                temperatureKelvin: 293.15,
                elevationMeters: 3_000,
                soilWater: 100,
                biomass: 1);

        var parameters =
            CreateParameters();

        var lowChange =
            new VegetationSystem(
                low.Planet.Id,
                parameters)
            .Evaluate(
                low.World,
                OneDaySeconds);

        var highChange =
            new VegetationSystem(
                high.Planet.Id,
                parameters)
            .Evaluate(
                high.World,
                OneDaySeconds);

        var lowVegetation =
            Assert.IsType<
                ReplacePlanetVegetationStateOperation>(
                lowChange.Operation)
            .Vegetation;

        var highVegetation =
            Assert.IsType<
                ReplacePlanetVegetationStateOperation>(
                highChange.Operation)
            .Vegetation;

        Assert.True(
            lowVegetation
                .GetCell(
                    low.TargetCellId)
                .LiveBiomassKilogramsPerSquareMeter >
            highVegetation
                .GetCell(
                    high.TargetCellId)
                .LiveBiomassKilogramsPerSquareMeter);
    }

    [Fact]
    public void Evaluate_DoesNotExceedCarryingCapacity()
    {
        var setup =
            CreateWorld(
                temperatureKelvin: 293.15,
                elevationMeters: 0,
                soilWater: 100,
                biomass: 4.99);

        var parameters =
            new VegetationModelParameters(
                maximumIntegrationStepSeconds:
                    21_600,
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
                    0.0065);

        var change =
            new VegetationSystem(
                setup.Planet.Id,
                parameters)
            .Evaluate(
                setup.World,
                OneDaySeconds);

        var vegetation =
            Assert.IsType<
                ReplacePlanetVegetationStateOperation>(
                change.Operation)
            .Vegetation;

        Assert.InRange(
            vegetation
                .GetCell(
                    setup.TargetCellId)
                .LiveBiomassKilogramsPerSquareMeter,
            0,
            5);
    }

    [Fact]
    public void Evaluate_RequiresAuthoritativeVegetationState()
    {
        var setup =
            CreateWorld(
                temperatureKelvin: 293.15,
                elevationMeters: 0,
                soilWater: 100,
                biomass: 1);

        var worldWithoutVegetation =
            new WorldState(
                setup.World.Id,
                SimulationTime.Zero,
                setup.World.Planets,
                [],
                [],
                setup.World.Terrain,
                setup.World.Hydrology);

        var system =
            new VegetationSystem(
                setup.Planet.Id,
                CreateParameters());

        Assert.Throws<InvalidOperationException>(
            () =>
                system.Evaluate(
                    worldWithoutVegetation,
                    OneDaySeconds));
    }

    private static VegetationModelParameters CreateParameters()
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
                0.0065);
    }

    private static TestWorld CreateWorld(
        double temperatureKelvin,
        double elevationMeters,
        double soilWater,
        double biomass)
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Vegetation World",
                5.0e24,
                6_000_000,
                new PlanetEnvironment(
                    temperatureKelvin,
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
                            elevationMeters)));

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
                                soilWater,
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
                            biomass)));

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
