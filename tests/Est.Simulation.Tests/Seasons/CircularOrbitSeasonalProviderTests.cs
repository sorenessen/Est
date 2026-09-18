using Est.Simulation.Planets;
using Est.Simulation.Seasons;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Seasons;

public sealed class CircularOrbitSeasonalProviderTests
{
    [Theory]
    [InlineData(0, 0.00, 0.0)]
    [InlineData(100, 0.25, 23.5)]
    [InlineData(200, 0.50, 0.0)]
    [InlineData(300, 0.75, -23.5)]
    [InlineData(400, 0.00, 0.0)]
    public void Derive_UsesCircularOrbitAndAxialTilt(
        long timeSeconds,
        double expectedCycleFraction,
        double expectedSubsolarLatitudeDegrees)
    {
        var planet =
            CreatePlanet();

        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(
                    timeSeconds),
                [planet]);

        var provider =
            new CircularOrbitSeasonalProvider(
                new CircularOrbitSeasonalParameters(
                    orbitalPeriodSeconds: 400,
                    axialTiltDegrees: 23.5));

        var context =
            provider.Derive(
                world,
                planet.Id);

        Assert.Equal(
            "circular-orbit",
            context.PhaseId);

        Assert.Equal(
            expectedCycleFraction,
            context.CycleFraction!.Value,
            precision: 12);

        Assert.Equal(
            expectedSubsolarLatitudeDegrees,
            context.SubsolarLatitudeDegrees!.Value,
            precision: 10);
    }

    [Fact]
    public void Derive_AppliesConfiguredEpochFraction()
    {
        var planet =
            CreatePlanet();

        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(100),
                [planet]);

        var provider =
            new CircularOrbitSeasonalProvider(
                new CircularOrbitSeasonalParameters(
                    orbitalPeriodSeconds: 400,
                    axialTiltDegrees: 0,
                    cycleFractionAtTimeZero: 0.625));

        var context =
            provider.Derive(
                world,
                planet.Id);

        Assert.Equal(
            0.875,
            context.CycleFraction!.Value,
            precision: 12);

        Assert.Equal(
            0,
            context.SubsolarLatitudeDegrees!.Value,
            precision: 12);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Parameters_RejectInvalidOrbitalPeriod(
        double orbitalPeriodSeconds)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new CircularOrbitSeasonalParameters(
                    orbitalPeriodSeconds,
                    axialTiltDegrees: 23.5));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(180.01)]
    [InlineData(double.NaN)]
    public void Parameters_RejectInvalidAxialTilt(
        double axialTiltDegrees)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new CircularOrbitSeasonalParameters(
                    orbitalPeriodSeconds: 400,
                    axialTiltDegrees));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1)]
    [InlineData(double.NaN)]
    public void Parameters_RejectInvalidEpochFraction(
        double cycleFractionAtTimeZero)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new CircularOrbitSeasonalParameters(
                    orbitalPeriodSeconds: 400,
                    axialTiltDegrees: 23.5,
                    cycleFractionAtTimeZero));
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
