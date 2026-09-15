using Est.Simulation.Causality;
using Est.Simulation.Hydrology;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Hydrology;

public sealed class HydrologySystemTests
{
    private const long OneDaySeconds =
        86_400;

    [Fact]
    public void Step_EvaporationMovesSurfaceWaterToAtmosphere()
    {
        var setup =
            CreateUniformWorld(
                atmospheric: 0,
                surface: 10,
                soil: 0);

        var result =
            SimulationStepRunner.Step(
                setup.World,
                OneDaySeconds,
                new HydrologySystem(
                    setup.Planet.Id,
                    CreateParameters(
                        evaporation: 4)));

        var cell =
            result.World.Hydrology[0]
                .GetCell(
                    setup.Grid.Cells[0].Id);

        Assert.Equal(
            4,
            cell.AtmosphericWaterKilogramsPerSquareMeter,
            10);

        Assert.Equal(
            6,
            cell.SurfaceLiquidWaterKilogramsPerSquareMeter,
            10);

        AssertConserved(
            result);
    }

    [Fact]
    public void Step_PrecipitationOnlyRemovesAtmosphericExcess()
    {
        var setup =
            CreateUniformWorld(
                atmospheric: 30,
                surface: 0,
                soil: 0);

        var result =
            SimulationStepRunner.Step(
                setup.World,
                OneDaySeconds,
                new HydrologySystem(
                    setup.Planet.Id,
                    CreateParameters(
                        precipitationThreshold: 20,
                        precipitation: 12)));

        var cell =
            result.World.Hydrology[0]
                .GetCell(
                    setup.Grid.Cells[0].Id);

        Assert.Equal(
            20,
            cell.AtmosphericWaterKilogramsPerSquareMeter,
            10);

        Assert.Equal(
            10,
            cell.SurfaceLiquidWaterKilogramsPerSquareMeter,
            10);

        AssertConserved(
            result);
    }

    [Fact]
    public void Step_InfiltrationHonorsSoilCapacity()
    {
        var setup =
            CreateUniformWorld(
                atmospheric: 0,
                surface: 20,
                soil: 140);

        var result =
            SimulationStepRunner.Step(
                setup.World,
                OneDaySeconds,
                new HydrologySystem(
                    setup.Planet.Id,
                    CreateParameters(
                        soilCapacity: 150,
                        infiltration: 20)));

        var cell =
            result.World.Hydrology[0]
                .GetCell(
                    setup.Grid.Cells[0].Id);

        Assert.Equal(
            10,
            cell.SurfaceLiquidWaterKilogramsPerSquareMeter,
            10);

        Assert.Equal(
            150,
            cell.SoilWaterKilogramsPerSquareMeter,
            10);

        AssertConserved(
            result);
    }

    [Fact]
    public void Step_ColdConditionsFreezeSurfaceLiquidWater()
    {
        var setup =
            CreateUniformWorld(
                atmospheric: 0,
                surface: 12,
                soil: 0,
                snowIce: 3,
                temperatureKelvin: 270);

        var result =
            SimulationStepRunner.Step(
                setup.World,
                OneDaySeconds,
                new HydrologySystem(
                    setup.Planet.Id,
                    CreateParameters(
                        freezing: 5,
                        melting: 5)));

        var cell =
            result.World.Hydrology[0]
                .GetCell(
                    setup.Grid.Cells[0].Id);

        Assert.Equal(
            7,
            cell.SurfaceLiquidWaterKilogramsPerSquareMeter,
            10);

        Assert.Equal(
            8,
            cell.SnowIceWaterEquivalentKilogramsPerSquareMeter,
            10);

        Assert.True(
            result.Change.Metrics[
                "freezingMassKilograms"] >
            0);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "meltingMassKilograms"]);

        AssertConserved(
            result);
    }

    [Fact]
    public void Step_WarmConditionsMeltSnowAndIce()
    {
        var setup =
            CreateUniformWorld(
                atmospheric: 0,
                surface: 2,
                soil: 0,
                snowIce: 9,
                temperatureKelvin: 278);

        var result =
            SimulationStepRunner.Step(
                setup.World,
                OneDaySeconds,
                new HydrologySystem(
                    setup.Planet.Id,
                    CreateParameters(
                        freezing: 5,
                        melting: 4)));

        var cell =
            result.World.Hydrology[0]
                .GetCell(
                    setup.Grid.Cells[0].Id);

        Assert.Equal(
            6,
            cell.SurfaceLiquidWaterKilogramsPerSquareMeter,
            10);

        Assert.Equal(
            5,
            cell.SnowIceWaterEquivalentKilogramsPerSquareMeter,
            10);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "freezingMassKilograms"]);

        Assert.True(
            result.Change.Metrics[
                "meltingMassKilograms"] >
            0);

        AssertConserved(
            result);
    }

    [Fact]
    public void Step_BetweenPhaseThresholdsLeavesPhaseStoresUnchanged()
    {
        var setup =
            CreateUniformWorld(
                atmospheric: 0,
                surface: 7,
                soil: 0,
                snowIce: 6,
                temperatureKelvin: 273.5);

        var result =
            SimulationStepRunner.Step(
                setup.World,
                OneDaySeconds,
                new HydrologySystem(
                    setup.Planet.Id,
                    CreateParameters(
                        freezingTemperatureKelvin: 273,
                        meltingTemperatureKelvin: 274,
                        freezing: 10,
                        melting: 10)));

        var cell =
            result.World.Hydrology[0]
                .GetCell(
                    setup.Grid.Cells[0].Id);

        Assert.Equal(
            7,
            cell.SurfaceLiquidWaterKilogramsPerSquareMeter,
            10);

        Assert.Equal(
            6,
            cell.SnowIceWaterEquivalentKilogramsPerSquareMeter,
            10);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "freezingMassKilograms"]);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "meltingMassKilograms"]);

        AssertConserved(
            result);
    }

    [Fact]
    public void Step_RunoffTransfersPhysicalMassAcrossUnequalCellAreas()
    {
        var planet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        var source =
            grid.LocateCell(
                67.5,
                22.5);

        var destination =
            grid.LocateCell(
                22.5,
                22.5);

        Assert.NotEqual(
            source.AreaSquareMeters,
            destination.AreaSquareMeters);

        var terrain =
            new PlanetTerrainState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new TerrainCellState(
                            cell.Id,
                            cell.Id == source.Id
                                ? 1_000
                                : cell.Id ==
                                  destination.Id
                                    ? 0
                                    : 2_000)));

        var hydrology =
            new PlanetHydrologyState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new HydrologyCellState(
                            cell.Id,
                            0,
                            cell.Id == source.Id
                                ? 10
                                : 0,
                            0,
                            0)));

        var world =
            CreateWorld(
                planet,
                terrain,
                hydrology);

        var result =
            SimulationStepRunner.Step(
                world,
                OneDaySeconds,
                new HydrologySystem(
                    planet.Id,
                    CreateParameters(
                        runoff: 5)));

        var changed =
            Assert.Single(
                result.World.Hydrology);

        var changedSource =
            changed.GetCell(
                source.Id);

        var changedDestination =
            changed.GetCell(
                destination.Id);

        Assert.Equal(
            5,
            changedSource
                .SurfaceLiquidWaterKilogramsPerSquareMeter,
            10);

        var expectedDestinationWater =
            5 *
            source.AreaSquareMeters /
            destination.AreaSquareMeters;

        Assert.Equal(
            expectedDestinationWater,
            changedDestination
                .SurfaceLiquidWaterKilogramsPerSquareMeter,
            10);

        AssertConserved(
            result);
    }

    [Fact]
    public void Step_LocalSinkRetainsSurfaceWater()
    {
        var planet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        var sink =
            grid.LocateCell(
                22.5,
                22.5);

        var terrain =
            new PlanetTerrainState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new TerrainCellState(
                            cell.Id,
                            cell.Id == sink.Id
                                ? -1_000
                                : 1_000)));

        var hydrology =
            new PlanetHydrologyState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new HydrologyCellState(
                            cell.Id,
                            0,
                            cell.Id == sink.Id
                                ? 10
                                : 0,
                            0,
                            0)));

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    terrain,
                    hydrology),
                OneDaySeconds,
                new HydrologySystem(
                    planet.Id,
                    CreateParameters(
                        runoff: 5)));

        Assert.Equal(
            10,
            result.World.Hydrology[0]
                .GetCell(
                    sink.Id)
                .SurfaceLiquidWaterKilogramsPerSquareMeter,
            10);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "runoffMassKilograms"]);

        AssertConserved(
            result);
    }

    [Fact]
    public void Step_ClosedBasinEqualizesSurfaceBelowSpillElevation()
    {
        var planet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        var sink =
            grid.LocateCell(
                22.5,
                22.5);

        var sinkNeighbors =
            grid.GetNeighbors(
                    sink.Id)
                .ToHashSet();

        var shelfId =
            sinkNeighbors
                .OrderBy(
                    cellId =>
                        cellId.Value)
                .First();

        var shelf =
            grid.Cells.Single(
                cell =>
                    cell.Id ==
                    shelfId);

        var shelfNeighbors =
            grid.GetNeighbors(
                    shelf.Id)
                .ToHashSet();

        var saddleId =
            shelfNeighbors
                .Where(
                    cellId =>
                        cellId !=
                        sink.Id &&
                        !sinkNeighbors.Contains(
                            cellId))
                .OrderBy(
                    cellId =>
                        cellId.Value)
                .First();

        var saddle =
            grid.Cells.Single(
                cell =>
                    cell.Id ==
                    saddleId);

        var outletId =
            grid.GetNeighbors(
                    saddle.Id)
                .Where(
                    cellId =>
                        cellId !=
                        sink.Id &&
                        cellId !=
                        shelf.Id &&
                        !sinkNeighbors.Contains(
                            cellId) &&
                        !shelfNeighbors.Contains(
                            cellId))
                .OrderBy(
                    cellId =>
                        cellId.Value)
                .First();

        var outlet =
            grid.Cells.Single(
                cell =>
                    cell.Id ==
                    outletId);

        var terrain =
            new PlanetTerrainState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new TerrainCellState(
                            cell.Id,
                            cell.Id ==
                            sink.Id
                                ? 0
                                : cell.Id ==
                                  shelf.Id
                                    ? 2
                                    : cell.Id ==
                                      saddle.Id
                                        ? 10
                                        : cell.Id ==
                                          outlet.Id
                                            ? -10
                                            : 20)));

        const double targetWaterSurfaceElevation =
            6;

        var targetBasinVolumeCubicMeters =
            (targetWaterSurfaceElevation - 0) *
            sink.AreaSquareMeters +
            (targetWaterSurfaceElevation - 2) *
            shelf.AreaSquareMeters;

        var initialSinkSurfaceWater =
            targetBasinVolumeCubicMeters *
            1_000 /
            sink.AreaSquareMeters;

        var hydrology =
            new PlanetHydrologyState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new HydrologyCellState(
                            cell.Id,
                            0,
                            cell.Id ==
                            sink.Id
                                ? initialSinkSurfaceWater
                                : 0,
                            0,
                            0)));

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    terrain,
                    hydrology),
                OneDaySeconds,
                new HydrologySystem(
                    planet.Id,
                    CreateParameters()));

        var changed =
            Assert.Single(
                result.World.Hydrology);

        Assert.Equal(
            6_000,
            changed.GetCell(
                    sink.Id)
                .SurfaceLiquidWaterKilogramsPerSquareMeter,
            8);

        Assert.Equal(
            4_000,
            changed.GetCell(
                    shelf.Id)
                .SurfaceLiquidWaterKilogramsPerSquareMeter,
            8);

        Assert.Equal(
            0,
            changed.GetCell(
                    saddle.Id)
                .SurfaceLiquidWaterKilogramsPerSquareMeter,
            8);

        Assert.Equal(
            0,
            changed.GetCell(
                    outlet.Id)
                .SurfaceLiquidWaterKilogramsPerSquareMeter,
            8);

        AssertConserved(
            result);
    }

    [Fact]
    public void Step_ClosedBasinSpillsExcessAboveSaddle()
    {
        var planet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        var sink =
            grid.LocateCell(
                22.5,
                22.5);

        var sinkNeighbors =
            grid.GetNeighbors(
                    sink.Id)
                .ToHashSet();

        var shelfId =
            sinkNeighbors
                .OrderBy(
                    cellId =>
                        cellId.Value)
                .First();

        var shelf =
            grid.Cells.Single(
                cell =>
                    cell.Id ==
                    shelfId);

        var shelfNeighbors =
            grid.GetNeighbors(
                    shelf.Id)
                .ToHashSet();

        var saddleId =
            shelfNeighbors
                .Where(
                    cellId =>
                        cellId !=
                        sink.Id &&
                        !sinkNeighbors.Contains(
                            cellId))
                .OrderBy(
                    cellId =>
                        cellId.Value)
                .First();

        var saddle =
            grid.Cells.Single(
                cell =>
                    cell.Id ==
                    saddleId);

        var outletId =
            grid.GetNeighbors(
                    saddle.Id)
                .Where(
                    cellId =>
                        cellId !=
                        sink.Id &&
                        cellId !=
                        shelf.Id &&
                        !sinkNeighbors.Contains(
                            cellId) &&
                        !shelfNeighbors.Contains(
                            cellId))
                .OrderBy(
                    cellId =>
                        cellId.Value)
                .First();

        var outlet =
            grid.Cells.Single(
                cell =>
                    cell.Id ==
                    outletId);

        var terrain =
            new PlanetTerrainState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new TerrainCellState(
                            cell.Id,
                            cell.Id ==
                            sink.Id
                                ? 0
                                : cell.Id ==
                                  shelf.Id
                                    ? 2
                                    : cell.Id ==
                                      saddle.Id
                                        ? 10
                                        : cell.Id ==
                                          outlet.Id
                                            ? -10
                                            : 20)));

        const double spillElevationMeters =
            10;

        const double expectedSpillDepthMeters =
            2;

        var retainedBasinVolumeCubicMeters =
            (spillElevationMeters - 0) *
            sink.AreaSquareMeters +
            (spillElevationMeters - 2) *
            shelf.AreaSquareMeters;

        var expectedSpillMassKilograms =
            expectedSpillDepthMeters *
            saddle.AreaSquareMeters *
            1_000;

        var initialSinkSurfaceWater =
            (retainedBasinVolumeCubicMeters *
             1_000 +
             expectedSpillMassKilograms) /
            sink.AreaSquareMeters;

        var hydrology =
            new PlanetHydrologyState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new HydrologyCellState(
                            cell.Id,
                            0,
                            cell.Id ==
                            sink.Id
                                ? initialSinkSurfaceWater
                                : 0,
                            0,
                            0)));

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    terrain,
                    hydrology),
                OneDaySeconds,
                new HydrologySystem(
                    planet.Id,
                    CreateParameters()));

        var changed =
            Assert.Single(
                result.World.Hydrology);

        Assert.Equal(
            10_000,
            changed.GetCell(
                    sink.Id)
                .SurfaceLiquidWaterKilogramsPerSquareMeter,
            8);

        Assert.Equal(
            8_000,
            changed.GetCell(
                    shelf.Id)
                .SurfaceLiquidWaterKilogramsPerSquareMeter,
            8);

        Assert.Equal(
            2_000,
            changed.GetCell(
                    saddle.Id)
                .SurfaceLiquidWaterKilogramsPerSquareMeter,
            8);

        Assert.Equal(
            0,
            changed.GetCell(
                    outlet.Id)
                .SurfaceLiquidWaterKilogramsPerSquareMeter,
            8);

        var actualSpillMassKilograms =
            result.Change.Metrics[
                "basinSpillMassKilograms"];

        Assert.InRange(
            Math.Abs(
                actualSpillMassKilograms -
                expectedSpillMassKilograms) /
            expectedSpillMassKilograms,
            0,
            1e-12);

        AssertConserved(
            result);
    }

    [Fact]
    public void Evaluate_IsDeterministicForSameInputs()
    {
        var setup =
            CreateUniformWorld(
                atmospheric: 24,
                surface: 30,
                soil: 120);

        var system =
            new HydrologySystem(
                setup.Planet.Id,
                CreateParameters(
                    evaporation: 3,
                    precipitationThreshold: 20,
                    precipitation: 8,
                    soilCapacity: 150,
                    infiltration: 10,
                    runoff: 6,
                    maximumIntegrationStepSeconds:
                        21_600));

        var first =
            system.Evaluate(
                setup.World,
                OneDaySeconds);

        var second =
            system.Evaluate(
                setup.World,
                OneDaySeconds);

        var firstHydrology =
            Assert.IsType<
                Est.Simulation.Operations
                    .ReplacePlanetHydrologyStateOperation>(
                first.Operation)
                .Hydrology;

        var secondHydrology =
            Assert.IsType<
                Est.Simulation.Operations
                    .ReplacePlanetHydrologyStateOperation>(
                second.Operation)
                .Hydrology;

        Assert.True(
            firstHydrology.Cells.SequenceEqual(
                secondHydrology.Cells));

        Assert.Equal(
            first.Metrics,
            second.Metrics);
    }

    [Fact]
    public void Evaluate_RequiresAuthoritativeHydrologyState()
    {
        var planet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

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

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [],
                [],
                [],
                [terrain]);

        var system =
            new HydrologySystem(
                planet.Id,
                CreateParameters());

        Assert.Throws<InvalidOperationException>(
            () =>
                system.Evaluate(
                    world,
                    OneDaySeconds));
    }

    private static void AssertConserved(
        SimulationStepResult result)
    {
        Assert.InRange(
            Math.Abs(
                result.Change.Metrics[
                    "relativeWaterMassConservationError"]),
            0,
            1e-12);
    }

    private static TestWorld CreateUniformWorld(
        double atmospheric,
        double surface,
        double soil,
        double snowIce = 0,
        double temperatureKelvin = 285)
    {
        var planet =
            CreatePlanet(
                temperatureKelvin);

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

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
                            atmospheric,
                            surface,
                            soil,
                            snowIce)));

        return new TestWorld(
            planet,
            grid,
            CreateWorld(
                planet,
                terrain,
                hydrology));
    }

    private static WorldState CreateWorld(
        PlanetState planet,
        PlanetTerrainState terrain,
        PlanetHydrologyState hydrology)
    {
        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [planet],
            [],
            [],
            [],
            [terrain],
            [hydrology]);
    }

    private static HydrologyModelParameters CreateParameters(
        double evaporation = 0,
        double precipitationThreshold = 20,
        double precipitation = 0,
        double soilCapacity = 150,
        double infiltration = 0,
        double runoff = 0,
        double freezingTemperatureKelvin = 273.15,
        double meltingTemperatureKelvin = 273.15,
        double freezing = 0,
        double melting = 0,
        long maximumIntegrationStepSeconds =
            OneDaySeconds)
    {
        return new HydrologyModelParameters(
            maximumIntegrationStepSeconds:
                maximumIntegrationStepSeconds,
            maximumEvaporationRateKilogramsPerSquareMeterPerDay:
                evaporation,
            atmosphericPrecipitationThresholdKilogramsPerSquareMeter:
                precipitationThreshold,
            maximumPrecipitationRateKilogramsPerSquareMeterPerDay:
                precipitation,
            soilWaterCapacityKilogramsPerSquareMeter:
                soilCapacity,
            maximumInfiltrationRateKilogramsPerSquareMeterPerDay:
                infiltration,
            maximumRunoffRateKilogramsPerSquareMeterPerDay:
                runoff,
            freezingTemperatureKelvin:
                freezingTemperatureKelvin,
            meltingTemperatureKelvin:
                meltingTemperatureKelvin,
            maximumFreezingRateKilogramsPerSquareMeterPerDay:
                freezing,
            maximumMeltingRateKilogramsPerSquareMeterPerDay:
                melting);
    }

    private static PlanetState CreatePlanet(
        double temperatureKelvin = 285)
    {
        return new PlanetState(
            PlanetId.New(),
            "Hydrology World",
            5.0e24,
            6_000_000,
            new PlanetEnvironment(
                temperatureKelvin,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }

    private sealed record TestWorld(
        PlanetState Planet,
        IPlanetSurfaceGrid Grid,
        WorldState World);
}
