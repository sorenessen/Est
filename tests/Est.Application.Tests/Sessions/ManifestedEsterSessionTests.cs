using Est.Application.Sessions;
using Est.Simulation.Planets;
using Est.Simulation.Social;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Application.Tests.Sessions;

public sealed class ManifestedEsterSessionTests
{
    [Fact]
    public void ManifestAndMove_AreAuthoritativeZeroTimeInterventions()
    {
        var planet = CreateEarth();

        var session =
            new SimulationSession(
                new WorldState(
                    WorldId.New(),
                    new SimulationTime(600),
                    [planet]));

        var esterId =
            EsterId.New();

        var originalTimelineId =
            session.Timeline.Id;

        session.ManifestEster(
            esterId,
            planet.Id,
            47.0379,
            -122.9007);

        var manifested =
            session.GetManifestedEster(
                esterId);

        Assert.NotNull(
            manifested);

        Assert.Equal(
            47.0379,
            manifested.LatitudeDegrees);

        Assert.Equal(
            -122.9007,
            manifested.LongitudeDegrees);

        Assert.Equal(
            600,
            session.CurrentWorld
                .CurrentTime
                .TotalSeconds);

        Assert.Equal(
            originalTimelineId,
            session.Timeline.Id);

        session.MoveManifestedEster(
            esterId,
            47.0381,
            -122.9004);

        var moved =
            session.GetManifestedEster(
                esterId);

        Assert.NotNull(
            moved);

        Assert.Equal(
            esterId,
            moved.EsterId);

        Assert.Equal(
            planet.Id,
            moved.PlanetId);

        Assert.Equal(
            47.0381,
            moved.LatitudeDegrees);

        Assert.Equal(
            -122.9004,
            moved.LongitudeDegrees);

        Assert.Equal(
            600,
            session.CurrentWorld
                .CurrentTime
                .TotalSeconds);
    }

    [Fact]
    public void Query_UnknownEsterIsNonMutating()
    {
        var planet = CreateEarth();

        var session =
            new SimulationSession(
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet]));

        var before =
            session.Timeline;

        Assert.Null(
            session.GetManifestedEster(
                EsterId.New()));

        Assert.Same(
            before,
            session.Timeline);
    }

    private static PlanetState CreateEarth()
    {
        return new PlanetState(
            PlanetId.New(),
            "Earth",
            5.9722e24,
            6_371_000,
            new PlanetEnvironment(
                288.15,
                0.71,
                0.03,
                new AtmosphereState(
                    101_325,
                    new Dictionary<string, double>
                    {
                        ["N2"] = 0.78,
                        ["O2"] = 0.21,
                        ["Ar"] = 0.01
                    })));
    }
}
