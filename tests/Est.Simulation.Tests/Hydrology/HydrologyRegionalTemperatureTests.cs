using Est.Simulation.Hydrology;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Thermal;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Hydrology;

public sealed class HydrologyRegionalTemperatureTests
{
    private const long OneDaySeconds =
        86_400;

    [Fact]
    public void Evaluate_RegionalSurfaceTemperatureAllowsDifferentPhaseDecisionsWithinOnePlanet()
    {
        var setup =
            CreateWorld(
                compatibilityTemperatureKelvin:
                    273.5,
                regionalSurfaceTemperatures:
                    (index, _) =>
                        index switch
                        {
                            0 => 270,
                            1 => 278,
                            _ => 273.5
                        },
                reverseThermalStorage:
                    true);

        var change =
            CreateRegionalSystem(
                    setup.Planet.Id)
                .Evaluate(
                    setup.World,
                    OneDaySeconds);

        var changedWorld =
            change.Operation.Apply(
                setup.World);

        var changedHydrology =
            Assert.Single(
                changedWorld.Hydrology);

        Assert.Equal(
            15,
            changedHydrology
                .GetCell(
                    setup.Grid.Cells[0].Id)
                .SnowIceWaterEquivalentKilogramsPerSquareMeter,
            10);

        Assert.Equal(
            6,
            changedHydrology
                .GetCell(
                    setup.Grid.Cells[1].Id)
                .SnowIceWaterEquivalentKilogramsPerSquareMeter,
            10);

        Assert.Equal(
            10,
            changedHydrology
                .GetCell(
                    setup.Grid.Cells[2].Id)
                .SnowIceWaterEquivalentKilogramsPerSquareMeter,
            10);

        Assert.True(
            change.Metrics[
                "freezingMassKilograms"] >
            0);

        Assert.True(
            change.Metrics[
                "meltingMassKilograms"] >
            0);

        Assert.InRange(
            Math.Abs(
                change.Metrics[
                    "relativeWaterMassConservationError"]),
            0,
            1e-12);

        Assert.Equal(
            setup.Thermal,
            Assert.Single(
                changedWorld.RegionalThermal));
    }

    [Fact]
    public void Evaluate_RegionalSurfaceTemperatureOverridesCompatibilityMeanForPhaseDecision()
    {
        var setup =
            CreateWorld(
                compatibilityTemperatureKelvin:
                    270,
                regionalSurfaceTemperatures:
                    (_, _) =>
                        278);

        var change =
            CreateRegionalSystem(
                    setup.Planet.Id)
                .Evaluate(
                    setup.World,
                    OneDaySeconds);

        var changedWorld =
            change.Operation.Apply(
                setup.World);

        var changedCell =
            Assert.Single(
                    changedWorld.Hydrology)
                .GetCell(
                    setup.Grid.Cells[0].Id);

        Assert.Equal(
            6,
            changedCell
                .SnowIceWaterEquivalentKilogramsPerSquareMeter,
            10);

        Assert.Equal(
            0,
            change.Metrics[
                "freezingMassKilograms"]);

        Assert.True(
            change.Metrics[
                "meltingMassKilograms"] >
            0);
    }

    [Fact]
    public void Evaluate_CompatibilityModeIgnoresDormantRegionalThermalState()
    {
        var setup =
            CreateWorld(
                compatibilityTemperatureKelvin:
                    270,
                regionalSurfaceTemperatures:
                    (_, _) =>
                        278);

        var change =
            new HydrologySystem(
                setup.Planet.Id,
                CreateParameters())
                .Evaluate(
                    setup.World,
                    OneDaySeconds);

        var changedWorld =
            change.Operation.Apply(
                setup.World);

        Assert.Equal(
            15,
            Assert.Single(
                    changedWorld.Hydrology)
                .GetCell(
                    setup.Grid.Cells[0].Id)
                .SnowIceWaterEquivalentKilogramsPerSquareMeter,
            10);

        Assert.True(
            change.Metrics[
                "freezingMassKilograms"] >
            0);

        Assert.Equal(
            0,
            change.Metrics[
                "meltingMassKilograms"]);
    }

    [Fact]
    public void Evaluate_RegionalModeRequiresRegionalThermalState()
    {
        var setup =
            CreateWorld(
                compatibilityTemperatureKelvin:
                    273.5,
                includeRegionalThermal:
                    false);

        var system =
            CreateRegionalSystem(
                setup.Planet.Id);

        Assert.Throws<
            InvalidOperationException>(
            () =>
                system.Evaluate(
                    setup.World,
                    OneDaySeconds));
    }

    [Fact]
    public void Evaluate_RegionalModeZeroDurationPreservesWaterAndThermalState()
    {
        var setup =
            CreateWorld(
                compatibilityTemperatureKelvin:
                    270,
                regionalSurfaceTemperatures:
                    (_, _) =>
                        278);

        var change =
            CreateRegionalSystem(
                    setup.Planet.Id)
                .Evaluate(
                    setup.World,
                    0);

        var changedWorld =
            change.Operation.Apply(
                setup.World);

        Assert.Equal(
            setup.Hydrology,
            Assert.Single(
                changedWorld.Hydrology));

        Assert.Equal(
            setup.Thermal,
            Assert.Single(
                changedWorld.RegionalThermal));

        Assert.Equal(
            0,
            change.Metrics[
                "freezingMassKilograms"]);

        Assert.Equal(
            0,
            change.Metrics[
                "meltingMassKilograms"]);
    }

    private static HydrologySystem
        CreateRegionalSystem(
            PlanetId planetId)
    {
        return new HydrologySystem(
            planetId,
            CreateParameters(),
            HydrologyTemperatureSource
                .RegionalSurface);
    }

    private static HydrologyModelParameters
        CreateParameters()
    {
        return new HydrologyModelParameters(
            maximumIntegrationStepSeconds:
                OneDaySeconds,
            maximumEvaporationRateKilogramsPerSquareMeterPerDay:
                0,
            atmosphericPrecipitationThresholdKilogramsPerSquareMeter:
                20,
            maximumPrecipitationRateKilogramsPerSquareMeterPerDay:
                0,
            soilWaterCapacityKilogramsPerSquareMeter:
                150,
            maximumInfiltrationRateKilogramsPerSquareMeterPerDay:
                0,
            maximumRunoffRateKilogramsPerSquareMeterPerDay:
                0,
            freezingTemperatureKelvin:
                273,
            meltingTemperatureKelvin:
                274,
            maximumFreezingRateKilogramsPerSquareMeterPerDay:
                5,
            maximumMeltingRateKilogramsPerSquareMeterPerDay:
                4);
    }

    private static TestWorld CreateWorld(
        double compatibilityTemperatureKelvin,
        Func<int, SurfaceCellId, double>?
            regionalSurfaceTemperatures = null,
        bool reverseThermalStorage = false,
        bool includeRegionalThermal = true)
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Regional Hydrology World",
                5.0e24,
                6_000_000,
                new PlanetEnvironment(
                    compatibilityTemperatureKelvin,
                    0.60,
                    0.05,
                    AtmosphereState.Vacuum));

        var definition =
            SurfaceGridDefinition
                .LatitudeLongitude(
                    2,
                    4);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

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
                                10,
                            soilWaterKilogramsPerSquareMeter:
                                0,
                            snowIceWaterEquivalentKilogramsPerSquareMeter:
                                10)));

        PlanetRegionalThermalState?
            thermal = null;

        if (includeRegionalThermal)
        {
            var thermalCells =
                grid.Cells
                    .Select(
                        (cell, index) =>
                            new RegionalThermalCellState(
                                cell.Id,
                                regionalSurfaceTemperatures?.Invoke(
                                    index,
                                    cell.Id)
                                ?? compatibilityTemperatureKelvin,
                                atmosphericTemperatureKelvin:
                                    200))
                    .ToArray();

            if (reverseThermalStorage)
            {
                Array.Reverse(
                    thermalCells);
            }

            thermal =
                new PlanetRegionalThermalState(
                    planet.Id,
                    definition,
                    thermalCells);
        }

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [],
                terrain:
                    [terrain],
                hydrology:
                    [hydrology],
                regionalThermal:
                    thermal is null
                        ? []
                        : [thermal]);

        return new TestWorld(
            planet,
            grid,
            hydrology,
            thermal,
            world);
    }

    private sealed record TestWorld(
        PlanetState Planet,
        IPlanetSurfaceGrid Grid,
        PlanetHydrologyState Hydrology,
        PlanetRegionalThermalState? Thermal,
        WorldState World);
}
