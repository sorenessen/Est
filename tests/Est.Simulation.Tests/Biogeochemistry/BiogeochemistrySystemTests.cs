using Est.Simulation.Biogeochemistry;
using Est.Simulation.Hydrology;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Biogeochemistry;

public sealed class BiogeochemistrySystemTests
{
    private const long OneDaySeconds =
        86_400;

    [Fact]
    public void Evaluate_DecomposesDetritusAndTransfersNitrogenUnderSuitableConditions()
    {
        var setup =
            CreateWorld(
                temperatureKelvin: 293.15,
                elevationMeters: 0,
                soilWater: 100,
                detritalBiomass: 2,
                detritalNitrogen: 0.04,
                availableNitrogen: 0.01);

        var change =
            new BiogeochemistrySystem(
                setup.Planet.Id,
                CreateParameters())
            .Evaluate(
                setup.World,
                OneDaySeconds);

        var state =
            Assert.IsType<
                ReplacePlanetBiogeochemistryStateOperation>(
                    change.Operation)
            .Biogeochemistry;

        var changed =
            state.GetCell(
                setup.TargetCellId);

        Assert.True(
            changed
                .DetritalBiomassKilogramsPerSquareMeter <
            2);

        Assert.True(
            changed
                .DetritalNitrogenKilogramsPerSquareMeter <
            0.04);

        Assert.True(
            changed
                .PlantAvailableNitrogenKilogramsPerSquareMeter >
            0.01);

        Assert.Equal(
            "planetary-biogeochemistry",
            change.Cause);

        Assert.True(
            change.Metrics[
                "decomposedBiomassMassKilograms"] >
            0);

        Assert.True(
            change.Metrics[
                "transferredNitrogenMassKilograms"] >
            0);

        Assert.Equal(
            4,
            change.Metrics[
                "integrationSubsteps"]);
    }

    [Fact]
    public void Evaluate_DrySoilPreventsDecomposition()
    {
        var setup =
            CreateWorld(
                temperatureKelvin: 293.15,
                elevationMeters: 0,
                soilWater: 0,
                detritalBiomass: 2,
                detritalNitrogen: 0.04,
                availableNitrogen: 0.01);

        var state =
            EvaluateState(
                setup,
                CreateParameters());

        var changed =
            state.GetCell(
                setup.TargetCellId);

        Assert.Equal(
            2,
            changed
                .DetritalBiomassKilogramsPerSquareMeter);

        Assert.Equal(
            0.04,
            changed
                .DetritalNitrogenKilogramsPerSquareMeter);

        Assert.Equal(
            0.01,
            changed
                .PlantAvailableNitrogenKilogramsPerSquareMeter);
    }

    [Fact]
    public void Evaluate_UnsuitableTemperaturePreventsDecomposition()
    {
        var setup =
            CreateWorld(
                temperatureKelvin: 250,
                elevationMeters: 0,
                soilWater: 100,
                detritalBiomass: 2,
                detritalNitrogen: 0.04,
                availableNitrogen: 0.01);

        var state =
            EvaluateState(
                setup,
                CreateParameters());

        var changed =
            state.GetCell(
                setup.TargetCellId);

        Assert.Equal(
            2,
            changed
                .DetritalBiomassKilogramsPerSquareMeter);

        Assert.Equal(
            0.04,
            changed
                .DetritalNitrogenKilogramsPerSquareMeter);

        Assert.Equal(
            0.01,
            changed
                .PlantAvailableNitrogenKilogramsPerSquareMeter);
    }

    [Fact]
    public void Evaluate_HigherTerrainCanReduceDecompositionThroughTemperature()
    {
        var low =
            CreateWorld(
                temperatureKelvin: 293.15,
                elevationMeters: 0,
                soilWater: 100,
                detritalBiomass: 2,
                detritalNitrogen: 0.04,
                availableNitrogen: 0.01);

        var high =
            CreateWorld(
                temperatureKelvin: 293.15,
                elevationMeters: 4_000,
                soilWater: 100,
                detritalBiomass: 2,
                detritalNitrogen: 0.04,
                availableNitrogen: 0.01);

        var parameters =
            CreateParameters();

        var lowState =
            EvaluateState(
                low,
                parameters);

        var highState =
            EvaluateState(
                high,
                parameters);

        Assert.True(
            lowState
                .GetCell(
                    low.TargetCellId)
                .DetritalBiomassKilogramsPerSquareMeter <
            highState
                .GetCell(
                    high.TargetCellId)
                .DetritalBiomassKilogramsPerSquareMeter);
    }

    [Fact]
    public void Evaluate_ConservesTrackedNitrogenMass()
    {
        var setup =
            CreateWorld(
                temperatureKelvin: 293.15,
                elevationMeters: 0,
                soilWater: 100,
                detritalBiomass: 2,
                detritalNitrogen: 0.04,
                availableNitrogen: 0.01);

        var change =
            new BiogeochemistrySystem(
                setup.Planet.Id,
                CreateParameters())
            .Evaluate(
                setup.World,
                OneDaySeconds);

        Assert.InRange(
            Math.Abs(
                change.Metrics[
                    "relativeNitrogenMassConservationError"]),
            0,
            1e-12);

        var initialTrackedNitrogen =
            change.Metrics[
                "initialTrackedNitrogenMassKilograms"];

        var finalTrackedNitrogen =
            change.Metrics[
                "finalTrackedNitrogenMassKilograms"];

        var directRelativeError =
            initialTrackedNitrogen == 0
                ? Math.Abs(
                    finalTrackedNitrogen)
                : Math.Abs(
                    finalTrackedNitrogen -
                    initialTrackedNitrogen) /
                  initialTrackedNitrogen;

        Assert.InRange(
            directRelativeError,
            0,
            1e-12);
    }

    [Fact]
    public void Evaluate_CannotDecomposeMoreDetritusThanExists()
    {
        var setup =
            CreateWorld(
                temperatureKelvin: 293.15,
                elevationMeters: 0,
                soilWater: 100,
                detritalBiomass: 0.1,
                detritalNitrogen: 0.02,
                availableNitrogen: 0.01);

        var parameters =
            new BiogeochemistryModelParameters(
                maximumIntegrationStepSeconds:
                    OneDaySeconds,
                maximumRelativeDecompositionRatePerDay:
                    20,
                soilWaterForFullDecompositionKilogramsPerSquareMeter:
                    50,
                minimumDecompositionTemperatureKelvin:
                    263.15,
                optimumDecompositionTemperatureKelvin:
                    293.15,
                maximumDecompositionTemperatureKelvin:
                    313.15,
                temperatureLapseRateKelvinPerMeter:
                    0.0065);

        var state =
            EvaluateState(
                setup,
                parameters);

        var changed =
            state.GetCell(
                setup.TargetCellId);

        Assert.Equal(
            0,
            changed
                .DetritalBiomassKilogramsPerSquareMeter,
            12);

        Assert.Equal(
            0,
            changed
                .DetritalNitrogenKilogramsPerSquareMeter,
            12);

        Assert.Equal(
            0.03,
            changed
                .PlantAvailableNitrogenKilogramsPerSquareMeter,
            12);
    }

    [Fact]
    public void Evaluate_ZeroDetritalBiomassDoesNotMoveNitrogen()
    {
        var setup =
            CreateWorld(
                temperatureKelvin: 293.15,
                elevationMeters: 0,
                soilWater: 100,
                detritalBiomass: 0,
                detritalNitrogen: 0.02,
                availableNitrogen: 0.01);

        var state =
            EvaluateState(
                setup,
                CreateParameters());

        var changed =
            state.GetCell(
                setup.TargetCellId);

        Assert.Equal(
            0,
            changed
                .DetritalBiomassKilogramsPerSquareMeter);

        Assert.Equal(
            0.02,
            changed
                .DetritalNitrogenKilogramsPerSquareMeter);

        Assert.Equal(
            0.01,
            changed
                .PlantAvailableNitrogenKilogramsPerSquareMeter);
    }

    [Fact]
    public void Evaluate_RequiresAuthoritativeBiogeochemistryState()
    {
        var setup =
            CreateWorld(
                temperatureKelvin: 293.15,
                elevationMeters: 0,
                soilWater: 100,
                detritalBiomass: 2,
                detritalNitrogen: 0.04,
                availableNitrogen: 0.01);

        var worldWithoutBiogeochemistry =
            new WorldState(
                setup.World.Id,
                SimulationTime.Zero,
                setup.World.Planets,
                [],
                terrain:
                    setup.World.Terrain,
                hydrology:
                    setup.World.Hydrology);

        var system =
            new BiogeochemistrySystem(
                setup.Planet.Id,
                CreateParameters());

        Assert.Throws<InvalidOperationException>(
            () =>
                system.Evaluate(
                    worldWithoutBiogeochemistry,
                    OneDaySeconds));
    }

    private static PlanetBiogeochemistryState EvaluateState(
        TestWorld setup,
        BiogeochemistryModelParameters parameters)
    {
        var change =
            new BiogeochemistrySystem(
                setup.Planet.Id,
                parameters)
            .Evaluate(
                setup.World,
                OneDaySeconds);

        return Assert.IsType<
            ReplacePlanetBiogeochemistryStateOperation>(
                change.Operation)
            .Biogeochemistry;
    }

    private static BiogeochemistryModelParameters CreateParameters()
    {
        return new BiogeochemistryModelParameters(
            maximumIntegrationStepSeconds:
                21_600,
            maximumRelativeDecompositionRatePerDay:
                0.05,
            soilWaterForFullDecompositionKilogramsPerSquareMeter:
                50,
            minimumDecompositionTemperatureKelvin:
                263.15,
            optimumDecompositionTemperatureKelvin:
                293.15,
            maximumDecompositionTemperatureKelvin:
                313.15,
            temperatureLapseRateKelvinPerMeter:
                0.0065);
    }

    private static TestWorld CreateWorld(
        double temperatureKelvin,
        double elevationMeters,
        double soilWater,
        double detritalBiomass,
        double detritalNitrogen,
        double availableNitrogen)
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Biogeochemistry World",
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

        var biogeochemistry =
            new PlanetBiogeochemistryState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new BiogeochemistryCellState(
                            cell.Id,
                            detritalBiomass,
                            detritalNitrogen,
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
