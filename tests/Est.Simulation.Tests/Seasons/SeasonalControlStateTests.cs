using Est.Simulation.Planets;
using Est.Simulation.Seasons;

namespace Est.Simulation.Tests.Seasons;

public class SeasonalControlStateTests
{
    [Fact]
    public void Context_PreservesGenericPhaseAndCycleFraction()
    {
        var context =
            new SeasonalContext(
                "cold-dry",
                0.75);

        Assert.Equal(
            "cold-dry",
            context.PhaseId);

        Assert.Equal(
            0.75,
            context.CycleFraction);

        Assert.Null(
            context.SubsolarLatitudeDegrees);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1)]
    [InlineData(double.PositiveInfinity)]
    public void Context_RejectsInvalidCycleFraction(
        double cycleFraction)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new SeasonalContext(
                    "phase",
                    cycleFraction));
    }

    [Theory]
    [InlineData(-90.01)]
    [InlineData(90.01)]
    [InlineData(double.PositiveInfinity)]
    public void Context_RejectsInvalidSubsolarLatitude(
        double subsolarLatitudeDegrees)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new SeasonalContext(
                    "phase",
                    0.25,
                    subsolarLatitudeDegrees));
    }

    [Fact]
    public void Disabled_HasNoEffectiveContext()
    {
        var state =
            new PlanetSeasonalState(
                PlanetId.New());

        Assert.Equal(
            SeasonalControlMode.Disabled,
            state.ControlMode);

        Assert.Null(
            state.EffectiveContext);
    }

    [Fact]
    public void Derived_UsesDerivedContext()
    {
        var derived =
            new SeasonalContext(
                "wet",
                0.20);

        var state =
            new PlanetSeasonalState(
                PlanetId.New(),
                SeasonalControlMode.Derived,
                derivedContext: derived);

        Assert.Same(
            derived,
            state.DerivedContext);

        Assert.Null(
            state.OverrideContext);

        Assert.Same(
            derived,
            state.EffectiveContext);
    }

    [Fact]
    public void Override_UsesOverrideWhilePreservingDerivedContext()
    {
        var derived =
            new SeasonalContext(
                "warm",
                0.40);

        var overridden =
            new SeasonalContext(
                "cold",
                0.90);

        var state =
            new PlanetSeasonalState(
                PlanetId.New(),
                SeasonalControlMode.Override,
                derivedContext: derived,
                overrideContext: overridden);

        Assert.Same(
            derived,
            state.DerivedContext);

        Assert.Same(
            overridden,
            state.OverrideContext);

        Assert.Same(
            overridden,
            state.EffectiveContext);
    }

    [Fact]
    public void Derived_RequiresDerivedContext()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new PlanetSeasonalState(
                    PlanetId.New(),
                    SeasonalControlMode.Derived));
    }

    [Fact]
    public void Override_RequiresOverrideContext()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new PlanetSeasonalState(
                    PlanetId.New(),
                    SeasonalControlMode.Override));
    }

    [Fact]
    public void Disabled_RejectsSeasonalContext()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new PlanetSeasonalState(
                    PlanetId.New(),
                    SeasonalControlMode.Disabled,
                    derivedContext:
                        new SeasonalContext(
                            "phase")));
    }
}
