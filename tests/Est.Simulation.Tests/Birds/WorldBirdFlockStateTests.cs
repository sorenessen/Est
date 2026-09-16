using Est.Simulation.Birds;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Birds;

public sealed class WorldBirdFlockStateTests
{
    [Fact]
    public void World_StoresBirdFlocksAndPreservesThemAcrossCopyAndFork()
    {
        var planet =
            CreatePlanet(
                "Bird World");

        var first =
            CreateFlock(
                planet,
                100,
                10,
                20);

        var second =
            CreateFlock(
                planet,
                250,
                -15,
                45);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [],
                birdFlocks:
                [
                    first,
                    second
                ]);

        Assert.Equal(
            2,
            world.BirdFlocks.Length);

        Assert.Equal(
            world.BirdFlocks,
            world.Copy().BirdFlocks);

        Assert.Equal(
            world.BirdFlocks,
            world.Fork().BirdFlocks);
    }

    [Fact]
    public void World_RejectsDuplicateBirdFlockIdentity()
    {
        var planet =
            CreatePlanet(
                "Duplicate World");

        var id =
            BirdFlockId.New();

        var first =
            new BirdFlockState(
                id,
                planet.Id,
                10,
                0,
                0);

        var second =
            new BirdFlockState(
                id,
                planet.Id,
                20,
                1,
                1);

        Assert.Throws<ArgumentException>(
            () =>
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet],
                    [],
                    birdFlocks:
                    [
                        first,
                        second
                    ]));
    }

    [Fact]
    public void World_RejectsBirdFlockWithoutPlanet()
    {
        var planet =
            CreatePlanet(
                "Known World");

        var otherPlanet =
            CreatePlanet(
                "Other World");

        var flock =
            CreateFlock(
                otherPlanet,
                10,
                0,
                0);

        Assert.Throws<ArgumentException>(
            () =>
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet],
                    [],
                    birdFlocks:
                    [
                        flock
                    ]));
    }

    [Fact]
    public void ReplacePlanetBirdFlocks_ReplacesOnlyTargetPlanet()
    {
        var firstPlanet =
            CreatePlanet(
                "First");

        var secondPlanet =
            CreatePlanet(
                "Second");

        var oldFirst =
            CreateFlock(
                firstPlanet,
                10,
                0,
                0);

        var preservedSecond =
            CreateFlock(
                secondPlanet,
                20,
                5,
                5);

        var replacement =
            CreateFlock(
                firstPlanet,
                30,
                10,
                10);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [
                    firstPlanet,
                    secondPlanet
                ],
                [],
                birdFlocks:
                [
                    oldFirst,
                    preservedSecond
                ]);

        var changed =
            new ReplacePlanetBirdFlocksOperation(
                firstPlanet.Id,
                [replacement])
            .Apply(
                world);

        Assert.DoesNotContain(
            oldFirst,
            changed.BirdFlocks);

        Assert.Contains(
            replacement,
            changed.BirdFlocks);

        Assert.Contains(
            preservedSecond,
            changed.BirdFlocks);

        Assert.Equal(
            2,
            changed.BirdFlocks.Length);
    }

    private static PlanetState CreatePlanet(
        string name)
    {
        return new PlanetState(
            PlanetId.New(),
            name,
            5.0e24,
            6_000_000,
            new PlanetEnvironment(
                288,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }

    private static BirdFlockState CreateFlock(
        PlanetState planet,
        int memberCount,
        double latitude,
        double longitude)
    {
        return new BirdFlockState(
            BirdFlockId.New(),
            planet.Id,
            memberCount,
            latitude,
            longitude);
    }
}
