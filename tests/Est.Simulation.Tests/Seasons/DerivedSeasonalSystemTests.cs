using Est.Simulation.Causality;
using Est.Simulation.Planets;
using Est.Simulation.Seasons;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Seasons;

public sealed class DerivedSeasonalSystemTests
{
    [Fact]
    public void Step_DerivesAgainstResultingSimulationTime()
    {
        var planet =
            CreatePlanet();

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet]);

        var system =
            CreateSystem(
                planet.Id);

        var result =
            SimulationStepRunner.Step(
                world,
                100,
                system);

        Assert.Equal(
            100,
            result.World.CurrentTime.TotalSeconds);

        var state =
            Assert.Single(
                result.World.SeasonalStates);

        Assert.Equal(
            SeasonalControlMode.Derived,
            state.ControlMode);

        Assert.Equal(
            0.25,
            state.DerivedContext!
                .CycleFraction!.Value,
            precision: 12);

        Assert.Equal(
            23.5,
            state.DerivedContext
                .SubsolarLatitudeDegrees!.Value,
            precision: 10);
    }

    [Fact]
    public void ZeroDurationStep_DerivesWithoutAdvancingTime()
    {
        var planet =
            CreatePlanet();

        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(200),
                [planet]);

        var result =
            SimulationStepRunner.Step(
                world,
                0,
                CreateSystem(
                    planet.Id));

        Assert.Equal(
            200,
            result.World.CurrentTime.TotalSeconds);

        var state =
            Assert.Single(
                result.World.SeasonalStates);

        Assert.Equal(
            0.50,
            state.DerivedContext!
                .CycleFraction!.Value,
            precision: 12);
    }

    [Fact]
    public void Step_UpdatesDerivedContextUnderOverride()
    {
        var planet =
            CreatePlanet();

        var overridden =
            new SeasonalContext(
                "forced-cold",
                0.90);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                population: [],
                seasonalStates:
                [
                    new PlanetSeasonalState(
                        planet.Id,
                        SeasonalControlMode.Override,
                        derivedContext:
                            new SeasonalContext(
                                "old-derived",
                                0),
                        overrideContext:
                            overridden)
                ]);

        var result =
            SimulationStepRunner.Step(
                world,
                100,
                CreateSystem(
                    planet.Id));

        var state =
            Assert.Single(
                result.World.SeasonalStates);

        Assert.Equal(
            SeasonalControlMode.Override,
            state.ControlMode);

        Assert.Equal(
            0.25,
            state.DerivedContext!
                .CycleFraction!.Value,
            precision: 12);

        Assert.Equal(
            23.5,
            state.DerivedContext
                .SubsolarLatitudeDegrees!.Value,
            precision: 10);

        Assert.Same(
            overridden,
            state.OverrideContext);

        Assert.Same(
            overridden,
            state.EffectiveContext);
    }

    private static DerivedSeasonalSystem CreateSystem(
        PlanetId planetId)
    {
        return new DerivedSeasonalSystem(
            planetId,
            new CircularOrbitSeasonalProvider(
                new CircularOrbitSeasonalParameters(
                    orbitalPeriodSeconds: 400,
                    axialTiltDegrees: 23.5)));
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Seasonal Test",
            5.9722e24,
            6_371_000,
            new PlanetEnvironment(
                288.15,
                0.71,
                0.03,
                AtmosphereState.Vacuum));
    }
}
