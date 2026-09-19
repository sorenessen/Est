using Est.Application.Sessions;
using Est.Simulation.Climate;
using Est.Simulation.Definitions;
using Est.Simulation.Planets;
using Est.Simulation.Seasons;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Thermal;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Application.Tests.Sessions;

public sealed class SimulationSessionRegionalThermalTests
{
    [Fact]
    public void Advance_UpdatesSeasonalAstronomyBeforeRegionalThermalEvaluation()
    {
        var setup =
            CreateRegionalWorld();

        var seasonalParameters =
            new CircularOrbitSeasonalParameters(
                orbitalPeriodSeconds:
                    400,
                axialTiltDegrees:
                    23.5);

        var definition =
            new SimulationDefinition(
                seasonalModels:
                [
                    new CircularOrbitSeasonalModelDefinition(
                        setup.Planet.Id,
                        seasonalParameters)
                ],
                regionalThermalModels:
                [
                    new RegionalThermalModelDefinition(
                        setup.Planet.Id,
                        CreateThermalParameters())
                ]);

        var session =
            new SimulationSession(
                setup.World,
                definition);

        session.Advance(
            100);

        Assert.Equal(
            100,
            session.CurrentWorld
                .CurrentTime.TotalSeconds);

        var seasonalState =
            Assert.Single(
                session.CurrentWorld
                    .SeasonalStates);

        Assert.Equal(
            SeasonalControlMode.Derived,
            seasonalState.ControlMode);

        Assert.Equal(
            0.25,
            seasonalState.DerivedContext!
                .CycleFraction!.Value,
            precision:
                12);

        Assert.Equal(
            23.5,
            seasonalState.DerivedContext
                .SubsolarLatitudeDegrees!
                .Value,
            precision:
                10);

        Assert.Equal(
            2,
            session.Timeline.Events.Length);

        Assert.Equal(
            "derived-seasonality",
            session.Timeline.Events[0]
                .Cause);

        Assert.Equal(
            "regional-thermal",
            session.Timeline.Events[1]
                .Cause);

        Assert.Equal(
            23.5,
            session.Timeline.Events[1]
                .Metrics[
                    "subsolarLatitudeDegrees"],
            precision:
                10);

        Assert.NotEqual(
            setup.Planet.Environment
                .MeanSurfaceTemperatureKelvin,
            Assert.Single(
                    session.CurrentWorld.Planets)
                .Environment
                .MeanSurfaceTemperatureKelvin);
    }

    [Fact]
    public void Advance_UsesExplicitFixedAstronomyWithoutSeasonalModel()
    {
        var setup =
            CreateRegionalWorld(
                seasonalState:
                    planetId =>
                        new PlanetSeasonalState(
                            planetId,
                            SeasonalControlMode.Override,
                            overrideContext:
                                new SeasonalContext(
                                    "fixed",
                                    subsolarLatitudeDegrees:
                                        -11)));

        var definition =
            new SimulationDefinition(
                regionalThermalModels:
                [
                    new RegionalThermalModelDefinition(
                        setup.Planet.Id,
                        CreateThermalParameters())
                ]);

        var session =
            new SimulationSession(
                setup.World,
                definition);

        session.Advance(
            60);

        var regionalEvent =
            Assert.Single(
                session.Timeline.Events);

        Assert.Equal(
            "regional-thermal",
            regionalEvent.Cause);

        Assert.Equal(
            -11,
            regionalEvent.Metrics[
                "subsolarLatitudeDegrees"]);
    }

    [Fact]
    public void Advance_DifferentPlanetsCanUseDifferentThermalAuthorities()
    {
        var regional =
            CreateRegionalWorld();

        var ebmPlanet =
            new PlanetState(
                PlanetId.New(),
                "EBM Planet",
                4.5e24,
                5_500_000,
                new PlanetEnvironment(
                    260,
                    0.2,
                    0.1,
                    AtmosphereState.Vacuum));

        var regionalSeasonalState =
            new PlanetSeasonalState(
                regional.Planet.Id,
                SeasonalControlMode.Override,
                overrideContext:
                    new SeasonalContext(
                        "fixed",
                        subsolarLatitudeDegrees:
                            0));

        var world =
            new WorldState(
                regional.World.Id,
                regional.World.CurrentTime,
                [
                    regional.Planet,
                    ebmPlanet
                ],
                [],
                terrain:
                    [regional.Terrain],
                seasonalStates:
                    [regionalSeasonalState],
                regionalThermal:
                    [regional.Thermal]);

        var definition =
            new SimulationDefinition(
                planetaryEnergyBalanceModels:
                [
                    new PlanetaryEnergyBalanceModelDefinition(
                        ebmPlanet.Id,
                        new PlanetaryEnergyBalanceParameters(
                            900,
                            0.6,
                            1.0e8,
                            0.3,
                            0.6,
                            240,
                            270,
                            31_536_000))
                ],
                regionalThermalModels:
                [
                    new RegionalThermalModelDefinition(
                        regional.Planet.Id,
                        CreateThermalParameters())
                ]);

        var session =
            new SimulationSession(
                world,
                definition);

        session.Advance(
            600);

        Assert.Equal(
            2,
            session.Timeline.Events.Length);

        Assert.Equal(
            "planetary-energy-balance",
            session.Timeline.Events[0]
                .Cause);

        Assert.Equal(
            ebmPlanet.Id,
            session.Timeline.Events[0]
                .AffectedPlanetId);

        Assert.Equal(
            "regional-thermal",
            session.Timeline.Events[1]
                .Cause);

        Assert.Equal(
            regional.Planet.Id,
            session.Timeline.Events[1]
                .AffectedPlanetId);
    }

    private static RegionalThermalModelParameters
        CreateThermalParameters()
    {
        return new RegionalThermalModelParameters(
            stellarFluxWattsPerSquareMeter:
                1361,
            atmosphericShortwaveVerticalOpticalDepth:
                0.2,
            atmosphericShortwaveSingleScatteringAlbedo:
                0.4,
            atmosphericShortwaveDownwardScatteringFraction:
                0.5,
            surfaceShortwaveAlbedo:
                0.3,
            surfaceLongwaveEmissivity:
                0.95,
            atmosphericLongwaveEmissivity:
                0.75,
            surfaceEffectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                1.0e8,
            atmosphericEffectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                1.0e7,
            maximumIntegrationStepSeconds:
                300);
    }

    private static RegionalWorldSetup
        CreateRegionalWorld(
            Func<PlanetId, PlanetSeasonalState>?
                seasonalState = null)
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Regional Planet",
                5.0e24,
                6_000_000,
                new PlanetEnvironment(
                    285,
                    0.6,
                    0.05,
                    AtmosphereState.Vacuum));

        var gridDefinition =
            SurfaceGridDefinition
                .LatitudeLongitude(
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

        var thermal =
            PlanetRegionalThermalInitializer
                .FromPlanetaryMeanSurfaceTemperature(
                    planet,
                    terrain);

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

        return new RegionalWorldSetup(
            planet,
            terrain,
            thermal,
            world);
    }

    private sealed record RegionalWorldSetup(
        PlanetState Planet,
        PlanetTerrainState Terrain,
        PlanetRegionalThermalState Thermal,
        WorldState World);
}
