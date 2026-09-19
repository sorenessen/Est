using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Seasons;
using Est.Simulation.Solar;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Thermal;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Thermal;

public sealed class RegionalThermalSystemTests
{
    [Fact]
    public void Evaluate_RejectsPersistedDerivedAstronomyWithoutConfiguredSeasonalModel()
    {
        var setup =
            CreateSetup(
                seasonalState:
                    CreateDerivedSeasonalState);

        var system =
            new RegionalThermalSystem(
                setup.Planet.Id,
                CreateParameters(),
                hasConfiguredSeasonalModel:
                    false);

        Assert.Throws<InvalidOperationException>(
            () =>
                system.Evaluate(
                    setup.World,
                    60));
    }

    [Fact]
    public void Evaluate_UsesExplicitOverrideAstronomyWithoutConfiguredSeasonalModel()
    {
        var setup =
            CreateSetup(
                seasonalState:
                    planetId =>
                        new PlanetSeasonalState(
                            planetId,
                            SeasonalControlMode.Override,
                            overrideContext:
                                new SeasonalContext(
                                    "fixed",
                                    subsolarLatitudeDegrees:
                                        12)));

        var system =
            new RegionalThermalSystem(
                setup.Planet.Id,
                CreateParameters(),
                hasConfiguredSeasonalModel:
                    false);

        var change =
            system.Evaluate(
                setup.World,
                0);

        Assert.Equal(
            12,
            change.Metrics[
                "subsolarLatitudeDegrees"]);

        var changedWorld =
            change.Operation.Apply(
                setup.World);

        Assert.Equal(
            setup.World.Id,
            changedWorld.Id);

        Assert.Equal(
            setup.World.CurrentTime,
            changedWorld.CurrentTime);
    }

    [Fact]
    public void Evaluate_UsesMaintainedDerivedAstronomyUnderOverride()
    {
        var setup =
            CreateSetup(
                seasonalState:
                    planetId =>
                        new PlanetSeasonalState(
                            planetId,
                            SeasonalControlMode.Override,
                            derivedContext:
                                new SeasonalContext(
                                    "derived",
                                    subsolarLatitudeDegrees:
                                        -18),
                            overrideContext:
                                new SeasonalContext(
                                    "label-only-override")));

        var system =
            new RegionalThermalSystem(
                setup.Planet.Id,
                CreateParameters(),
                hasConfiguredSeasonalModel:
                    true);

        var change =
            system.Evaluate(
                setup.World,
                0);

        Assert.Equal(
            -18,
            change.Metrics[
                "subsolarLatitudeDegrees"]);
    }

    [Fact]
    public void Evaluate_RejectsUnmaintainedDerivedFallbackUnderOverride()
    {
        var setup =
            CreateSetup(
                seasonalState:
                    planetId =>
                        new PlanetSeasonalState(
                            planetId,
                            SeasonalControlMode.Override,
                            derivedContext:
                                new SeasonalContext(
                                    "stale-derived",
                                    subsolarLatitudeDegrees:
                                        21),
                            overrideContext:
                                new SeasonalContext(
                                    "label-only-override")));

        var system =
            new RegionalThermalSystem(
                setup.Planet.Id,
                CreateParameters(),
                hasConfiguredSeasonalModel:
                    false);

        Assert.Throws<InvalidOperationException>(
            () =>
                system.Evaluate(
                    setup.World,
                    60));
    }

    [Fact]
    public void Evaluate_UsesDailyMeanShortwaveWithoutGlobalQuarterFactor()
    {
        const double initialTemperature = 280;
        const double stellarFlux = 1361;
        const double surfaceHeatCapacity = 1.0e8;
        const long elapsedSeconds = 3_600;

        var setup =
            CreateSetup(
                latitudeBandCount: 3,
                surfaceTemperatureKelvin:
                    initialTemperature,
                atmosphericTemperatureKelvin:
                    initialTemperature,
                seasonalState:
                    planetId =>
                        new PlanetSeasonalState(
                            planetId,
                            SeasonalControlMode.Override,
                            overrideContext:
                                new SeasonalContext(
                                    "equinox",
                                    subsolarLatitudeDegrees:
                                        0)));

        var parameters =
            CreateParameters(
                stellarFlux:
                    stellarFlux,
                opticalDepth:
                    0,
                singleScatteringAlbedo:
                    0,
                downwardScatteringFraction:
                    0,
                surfaceAlbedo:
                    0,
                surfaceLongwaveEmissivity:
                    0,
                atmosphericLongwaveEmissivity:
                    0,
                surfaceHeatCapacity:
                    surfaceHeatCapacity,
                atmosphericHeatCapacity:
                    1.0e8,
                maximumIntegrationStepSeconds:
                    elapsedSeconds);

        var system =
            new RegionalThermalSystem(
                setup.Planet.Id,
                parameters,
                hasConfiguredSeasonalModel:
                    false);

        var change =
            system.Evaluate(
                setup.World,
                elapsedSeconds);

        var changedWorld =
            change.Operation.Apply(
                setup.World);

        var changedThermal =
            Assert.Single(
                changedWorld.RegionalThermal);

        var grid =
            PlanetSurfaceGridFactory.Create(
                setup.Planet,
                changedThermal.GridDefinition);

        var equatorialCell =
            Assert.Single(
                grid.Cells,
                cell =>
                    cell.CenterLatitudeDegrees ==
                    0 &&
                    cell.CenterLongitudeDegrees <
                    -90);

        var changedCell =
            changedThermal.GetCell(
                equatorialCell.Id);

        var dailyMeanFactor =
            SurfaceSolarGeometryCalculator
                .Calculate(
                    0,
                    0)
                .DailyMeanInsolationFactor;

        var expectedTemperature =
            initialTemperature
            +
            stellarFlux
            *
            dailyMeanFactor
            *
            elapsedSeconds
            /
            surfaceHeatCapacity;

        Assert.Equal(
            expectedTemperature,
            changedCell.SurfaceTemperatureKelvin,
            precision:
                10);

        Assert.Equal(
            initialTemperature,
            changedCell.AtmosphericTemperatureKelvin,
            precision:
                12);
    }

    [Fact]
    public void Evaluate_UsesBoundedSubstepsAndProjectsAreaWeightedMeanAtomically()
    {
        var atmosphere =
            new AtmosphereState(
                80_000,
                new Dictionary<string, double>
                {
                    ["N2"] = 1
                });

        var setup =
            CreateSetup(
                environment:
                    new PlanetEnvironment(
                        310,
                        0.42,
                        0.17,
                        atmosphere),
                surfaceTemperatureKelvin:
                    275,
                atmosphericTemperatureKelvin:
                    265,
                seasonalState:
                    planetId =>
                        new PlanetSeasonalState(
                            planetId,
                            SeasonalControlMode.Override,
                            overrideContext:
                                new SeasonalContext(
                                    "fixed",
                                    subsolarLatitudeDegrees:
                                        0)));

        var system =
            new RegionalThermalSystem(
                setup.Planet.Id,
                CreateParameters(
                    maximumIntegrationStepSeconds:
                        600),
                hasConfiguredSeasonalModel:
                    false);

        var change =
            system.Evaluate(
                setup.World,
                1_800);

        Assert.Equal(
            3,
            change.Metrics[
                "integrationSubsteps"]);

        var changedWorld =
            change.Operation.Apply(
                setup.World);

        var changedPlanet =
            Assert.Single(
                changedWorld.Planets);

        var changedThermal =
            Assert.Single(
                changedWorld.RegionalThermal);

        var grid =
            PlanetSurfaceGridFactory.Create(
                changedPlanet,
                changedThermal.GridDefinition);

        var thermalByCellId =
            changedThermal.Cells.ToDictionary(
                cell =>
                    cell.CellId);

        var expectedMean =
            grid.Cells.Sum(
                cell =>
                    thermalByCellId[
                        cell.Id]
                    .SurfaceTemperatureKelvin
                    *
                    cell.AreaSquareMeters)
            /
            grid.TotalSurfaceAreaSquareMeters;

        Assert.Equal(
            expectedMean,
            changedPlanet.Environment
                .MeanSurfaceTemperatureKelvin,
            precision:
                10);

        Assert.Equal(
            0.42,
            changedPlanet.Environment
                .SurfaceWaterFraction);

        Assert.Equal(
            0.17,
            changedPlanet.Environment
                .IceCoverageFraction);

        Assert.Same(
            atmosphere,
            changedPlanet.Environment
                .Atmosphere);

        Assert.Equal(
            setup.World.Id,
            changedWorld.Id);

        Assert.Equal(
            setup.World.CurrentTime,
            changedWorld.CurrentTime);

        Assert.Equal(
            310,
            setup.Planet.Environment
                .MeanSurfaceTemperatureKelvin);
    }

    [Fact]
    public void Evaluate_IsIndependentOfStoredRegionalCellOrder()
    {
        var first =
            CreateSetup(
                reverseThermalCellOrder:
                    false,
                seasonalState:
                    CreateDerivedSeasonalState);

        var secondWorld =
            new WorldState(
                first.World.Id,
                first.World.CurrentTime,
                first.World.Planets,
                first.World.Population,
                first.World.Animals,
                first.World.Terrain,
                first.World.Hydrology,
                first.World.Vegetation,
                first.World.Invertebrates,
                first.World.BirdFlocks,
                first.World.GrazerCohorts,
                first.World.Biogeochemistry,
                first.World.SeasonalStates,
                [
                    new PlanetRegionalThermalState(
                        first.Planet.Id,
                        first.Thermal.GridDefinition,
                        first.Thermal.Cells
                            .Reverse())
                ]);

        var system =
            new RegionalThermalSystem(
                first.Planet.Id,
                CreateParameters(
                    maximumIntegrationStepSeconds:
                        300),
                hasConfiguredSeasonalModel:
                    true);

        var firstResult =
            system.Evaluate(
                    first.World,
                    900)
                .Operation
                .Apply(
                    first.World);

        var secondResult =
            system.Evaluate(
                    secondWorld,
                    900)
                .Operation
                .Apply(
                    secondWorld);

        var firstThermal =
            Assert.Single(
                firstResult.RegionalThermal);

        var secondThermal =
            Assert.Single(
                secondResult.RegionalThermal);

        foreach (var cell in firstThermal.Cells)
        {
            var secondCell =
                secondThermal.GetCell(
                    cell.CellId);

            Assert.Equal(
                cell.SurfaceTemperatureKelvin,
                secondCell.SurfaceTemperatureKelvin);

            Assert.Equal(
                cell.AtmosphericTemperatureKelvin,
                secondCell.AtmosphericTemperatureKelvin);
        }
    }

    [Fact]
    public void Evaluate_RejectsInvalidThermalStepRatherThanClamping()
    {
        var setup =
            CreateSetup(
                surfaceTemperatureKelvin:
                    300,
                atmosphericTemperatureKelvin:
                    0,
                seasonalState:
                    planetId =>
                        new PlanetSeasonalState(
                            planetId,
                            SeasonalControlMode.Override,
                            overrideContext:
                                new SeasonalContext(
                                    "dark",
                                    subsolarLatitudeDegrees:
                                        90)));

        var system =
            new RegionalThermalSystem(
                setup.Planet.Id,
                CreateParameters(
                    stellarFlux:
                        0,
                    surfaceLongwaveEmissivity:
                        1,
                    atmosphericLongwaveEmissivity:
                        0,
                    surfaceHeatCapacity:
                        1,
                    atmosphericHeatCapacity:
                        1,
                    maximumIntegrationStepSeconds:
                        1_000),
                hasConfiguredSeasonalModel:
                    false);

        Assert.Throws<InvalidOperationException>(
            () =>
                system.Evaluate(
                    setup.World,
                    1_000));
    }

    [Fact]
    public void AtomicOperation_PreservesNonthermalEnvironmentAndOtherRegionalState()
    {
        var setup =
            CreateSetup(
                seasonalState:
                    CreateDerivedSeasonalState);

        var otherPlanet =
            new PlanetState(
                PlanetId.New(),
                "Other Planet",
                4.0e24,
                5_000_000,
                new PlanetEnvironment(
                    240,
                    0.1,
                    0.2,
                    AtmosphereState.Vacuum));

        var otherDefinition =
            SurfaceGridDefinition
                .LatitudeLongitude(
                    2,
                    4);

        var otherGrid =
            PlanetSurfaceGridFactory.Create(
                otherPlanet,
                otherDefinition);

        var otherTerrain =
            new PlanetTerrainState(
                otherPlanet.Id,
                otherDefinition,
                otherGrid.Cells.Select(
                    cell =>
                        new TerrainCellState(
                            cell.Id,
                            0)));

        var otherThermal =
            new PlanetRegionalThermalState(
                otherPlanet.Id,
                otherDefinition,
                otherGrid.Cells.Select(
                    cell =>
                        new RegionalThermalCellState(
                            cell.Id,
                            240,
                            230)));

        var world =
            new WorldState(
                setup.World.Id,
                setup.World.CurrentTime,
                [
                    setup.Planet,
                    otherPlanet
                ],
                [],
                terrain:
                [
                    setup.Terrain,
                    otherTerrain
                ],
                seasonalStates:
                    setup.World.SeasonalStates,
                regionalThermal:
                [
                    setup.Thermal,
                    otherThermal
                ]);

        var replacement =
            new PlanetRegionalThermalState(
                setup.Planet.Id,
                setup.Thermal.GridDefinition,
                setup.Thermal.Cells.Select(
                    cell =>
                        new RegionalThermalCellState(
                            cell.CellId,
                            301,
                            290)));

        var operation =
            new ReplacePlanetRegionalThermalStateAndEnvironmentOperation(
                replacement,
                301);

        var changed =
            operation.Apply(
                world);

        var changedPlanet =
            changed.Planets.Single(
                planet =>
                    planet.Id ==
                    setup.Planet.Id);

        Assert.Equal(
            301,
            changedPlanet.Environment
                .MeanSurfaceTemperatureKelvin);

        Assert.Equal(
            setup.Planet.Environment
                .SurfaceWaterFraction,
            changedPlanet.Environment
                .SurfaceWaterFraction);

        Assert.Equal(
            setup.Planet.Environment
                .IceCoverageFraction,
            changedPlanet.Environment
                .IceCoverageFraction);

        Assert.Same(
            setup.Planet.Environment
                .Atmosphere,
            changedPlanet.Environment
                .Atmosphere);

        Assert.Same(
            otherThermal,
            changed.RegionalThermal.Single(
                thermal =>
                    thermal.PlanetId ==
                    otherPlanet.Id));

        Assert.Same(
            replacement,
            changed.RegionalThermal.Single(
                thermal =>
                    thermal.PlanetId ==
                    setup.Planet.Id));

        Assert.Equal(
            world.Id,
            changed.Id);

        Assert.Equal(
            world.CurrentTime,
            changed.CurrentTime);
    }

    private static PlanetSeasonalState
        CreateDerivedSeasonalState(
            PlanetId planetId)
    {
        return new PlanetSeasonalState(
            planetId,
            SeasonalControlMode.Derived,
            derivedContext:
                new SeasonalContext(
                    "derived",
                    subsolarLatitudeDegrees:
                        0));
    }

    private static RegionalThermalModelParameters
        CreateParameters(
            double stellarFlux = 1361,
            double opticalDepth = 0.2,
            double singleScatteringAlbedo = 0.4,
            double downwardScatteringFraction = 0.5,
            double surfaceAlbedo = 0.3,
            double surfaceLongwaveEmissivity = 0.95,
            double atmosphericLongwaveEmissivity = 0.75,
            double surfaceHeatCapacity = 1.0e8,
            double atmosphericHeatCapacity = 1.0e7,
            long maximumIntegrationStepSeconds = 3_600)
    {
        return new RegionalThermalModelParameters(
            stellarFlux,
            opticalDepth,
            singleScatteringAlbedo,
            downwardScatteringFraction,
            surfaceAlbedo,
            surfaceLongwaveEmissivity,
            atmosphericLongwaveEmissivity,
            surfaceHeatCapacity,
            atmosphericHeatCapacity,
            maximumIntegrationStepSeconds);
    }

    private static TestSetup CreateSetup(
        int latitudeBandCount = 2,
        int longitudeBandCount = 4,
        PlanetEnvironment? environment = null,
        double surfaceTemperatureKelvin = 280,
        double atmosphericTemperatureKelvin = 270,
        Func<PlanetId, PlanetSeasonalState>?
            seasonalState = null,
        bool reverseThermalCellOrder = false)
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Regional Thermal Test World",
                5.0e24,
                6_000_000,
                environment ??
                    new PlanetEnvironment(
                        285,
                        0.60,
                        0.05,
                        AtmosphereState.Vacuum));

        var definition =
            SurfaceGridDefinition
                .LatitudeLongitude(
                    latitudeBandCount,
                    longitudeBandCount);

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

        IEnumerable<RegionalThermalCellState>
            thermalCells =
                grid.Cells.Select(
                    cell =>
                        new RegionalThermalCellState(
                            cell.Id,
                            surfaceTemperatureKelvin,
                            atmosphericTemperatureKelvin));

        if (reverseThermalCellOrder)
        {
            thermalCells =
                thermalCells.Reverse();
        }

        var thermal =
            new PlanetRegionalThermalState(
                planet.Id,
                definition,
                thermalCells);

        var seasonalStates =
            seasonalState is null
                ? Array.Empty<PlanetSeasonalState>()
                : new[]
                {
                    seasonalState(
                        planet.Id)
                };

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [],
                terrain:
                    [terrain],
                seasonalStates:
                    seasonalStates,
                regionalThermal:
                    [thermal]);

        return new TestSetup(
            planet,
            terrain,
            thermal,
            world);
    }

    private sealed record TestSetup(
        PlanetState Planet,
        PlanetTerrainState Terrain,
        PlanetRegionalThermalState Thermal,
        WorldState World);
}
