using Est.Simulation.Ecology;
using Est.Simulation.Planets;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Ecology;

public sealed class WorldFoodResourceTests
{
    [Fact]
    public void Constructor_StoresFoodResources()
    {
        var planet = CreatePlanet();

        var food =
            new FoodResourceState(
                FoodResourceId.New(),
                planet.Id,
                10,
                20,
                100);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [],
                [food]);

        Assert.Equal(
            food,
            Assert.Single(world.FoodResources));
    }

    [Fact]
    public void Constructor_RejectsFoodForMissingPlanet()
    {
        var planet = CreatePlanet();

        var food =
            new FoodResourceState(
                FoodResourceId.New(),
                PlanetId.New(),
                10,
                20,
                100);

        Assert.Throws<ArgumentException>(
            () =>
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet],
                    [],
                    [food]));
    }

    [Fact]
    public void Constructor_RejectsDuplicateFoodResourceIds()
    {
        var planet = CreatePlanet();
        var id = FoodResourceId.New();

        var first =
            new FoodResourceState(
                id,
                planet.Id,
                10,
                20,
                100);

        var second =
            new FoodResourceState(
                id,
                planet.Id,
                11,
                21,
                50);

        Assert.Throws<ArgumentException>(
            () =>
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet],
                    [],
                    [first, second]));
    }

    [Fact]
    public void ReplaceFoodResources_PreservesOtherWorldState()
    {
        var planet = CreatePlanet();

        var original =
            new FoodResourceState(
                FoodResourceId.New(),
                planet.Id,
                10,
                20,
                100);

        var replacement =
            new FoodResourceState(
                FoodResourceId.New(),
                planet.Id,
                12,
                22,
                75);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [],
                [original]);

        var changed =
            world.ReplaceFoodResources(
                [replacement]);

        Assert.Equal(world.Id, changed.Id);
        Assert.Equal(
            world.CurrentTime,
            changed.CurrentTime);
        Assert.Equal(
            world.Planets,
            changed.Planets);

        Assert.Equal(
            replacement,
            Assert.Single(changed.FoodResources));

        Assert.Equal(
            original,
            Assert.Single(world.FoodResources));
    }

    [Fact]
    public void CopyAndFork_PreserveFoodResources()
    {
        var planet = CreatePlanet();

        var food =
            new FoodResourceState(
                FoodResourceId.New(),
                planet.Id,
                10,
                20,
                100);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [],
                [food]);

        var copy = world.Copy();
        var fork = world.Fork();

        Assert.Equal(
            world.FoodResources,
            copy.FoodResources);

        Assert.Equal(
            world.FoodResources,
            fork.FoodResources);

        Assert.Equal(world.Id, copy.Id);
        Assert.NotEqual(world.Id, fork.Id);
    }

    private static PlanetState CreatePlanet()
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
                AtmosphereState.Vacuum));
    }
}
