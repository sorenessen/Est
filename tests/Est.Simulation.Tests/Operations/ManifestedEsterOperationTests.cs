using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Social;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Operations;

public sealed class ManifestedEsterOperationTests
{
    [Fact]
    public void Manifest_AddsAuthoritativePhysicalPresence()
    {
        var planet = CreateEarth();
        var esterId = EsterId.New();

        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(120),
                [planet]);

        var changed =
            SimulationOperationExecutor.Apply(
                world,
                new ManifestEsterOperation(
                    esterId,
                    planet.Id,
                    47.0379,
                    -122.9007));

        var manifested =
            Assert.Single(
                changed.ManifestedEsters);

        Assert.Equal(
            esterId,
            manifested.EsterId);

        Assert.Equal(
            planet.Id,
            manifested.PlanetId);

        Assert.Equal(
            47.0379,
            manifested.LatitudeDegrees);

        Assert.Equal(
            -122.9007,
            manifested.LongitudeDegrees);

        Assert.Empty(
            world.ManifestedEsters);

        Assert.Equal(
            world.Id,
            changed.Id);

        Assert.Equal(
            world.CurrentTime,
            changed.CurrentTime);
    }

    [Fact]
    public void Manifest_RejectsUnknownPlanet()
    {
        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [CreateEarth()]);

        Assert.Throws<PlanetNotFoundException>(
            () =>
                SimulationOperationExecutor.Apply(
                    world,
                    new ManifestEsterOperation(
                        EsterId.New(),
                        PlanetId.New(),
                        0,
                        0)));
    }

    [Fact]
    public void Manifest_RejectsDuplicateManifestation()
    {
        var planet = CreateEarth();
        var esterId = EsterId.New();

        var world =
            new ManifestEsterOperation(
                esterId,
                planet.Id,
                10,
                20)
            .Apply(
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet]));

        Assert.Throws<InvalidOperationException>(
            () =>
                new ManifestEsterOperation(
                    esterId,
                    planet.Id,
                    11,
                    21)
                .Apply(world));
    }

    [Fact]
    public void Move_ChangesAuthoritativeLocationWithoutChangingIdentity()
    {
        var planet = CreateEarth();
        var esterId = EsterId.New();

        var manifested =
            new ManifestEsterOperation(
                esterId,
                planet.Id,
                47.0379,
                -122.9007)
            .Apply(
                new WorldState(
                    WorldId.New(),
                    new SimulationTime(300),
                    [planet]));

        var moved =
            new MoveManifestedEsterOperation(
                esterId,
                47.0381,
                -122.9004)
            .Apply(
                manifested);

        var result =
            Assert.Single(
                moved.ManifestedEsters);

        Assert.Equal(
            esterId,
            result.EsterId);

        Assert.Equal(
            planet.Id,
            result.PlanetId);

        Assert.Equal(
            47.0381,
            result.LatitudeDegrees);

        Assert.Equal(
            -122.9004,
            result.LongitudeDegrees);

        var source =
            Assert.Single(
                manifested.ManifestedEsters);

        Assert.Equal(
            47.0379,
            source.LatitudeDegrees);

        Assert.Equal(
            -122.9007,
            source.LongitudeDegrees);

        Assert.Equal(
            manifested.CurrentTime,
            moved.CurrentTime);
    }

    [Fact]
    public void Move_RejectsUnknownEster()
    {
        var planet = CreateEarth();

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet]);

        Assert.Throws<InvalidOperationException>(
            () =>
                new MoveManifestedEsterOperation(
                    EsterId.New(),
                    0,
                    0)
                .Apply(world));
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
