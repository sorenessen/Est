using Est.Simulation.Causality;
using Est.Simulation.Ecology;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Ecology;

public sealed class ForagingSystemTests
{
    [Fact]
    public void Step_HungryPersonConsumesNearbyFood()
    {
        var planet = CreatePlanet();

        var person =
            CreateHungryPerson(
                planet.Id,
                latitude: 10,
                longitude: 20,
                energy: 0.25);

        var food =
            new FoodResourceState(
                FoodResourceId.New(),
                planet.Id,
                10.1,
                20.1,
                10);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [person],
                [food]);

        var result =
            SimulationStepRunner.Step(
                world,
                86_400,
                new ForagingSystem(
                    planet.Id));

        var changedPerson =
            Assert.Single(
                result.World.Population);

        var changedFood =
            Assert.Single(
                result.World.FoodResources);

        Assert.Equal(
            1,
            changedPerson.Needs.EnergyReserve);

        Assert.Equal(
            PersonActivity.Eating,
            changedPerson.Activity);

        Assert.Equal(
            9.25,
            changedFood.AvailableEnergy,
            10);

        Assert.Equal(
            1,
            result.Change.Metrics["fed"]);

        Assert.Equal(
            0.75,
            result.Change.Metrics[
                "energyConsumed"],
            10);
    }

    [Fact]
    public void Step_FoodConsumptionPreservesSourceWorld()
    {
        var planet = CreatePlanet();

        var person =
            CreateHungryPerson(
                planet.Id,
                0,
                0,
                0.5);

        var food =
            new FoodResourceState(
                FoodResourceId.New(),
                planet.Id,
                0,
                0,
                5);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [person],
                [food]);

        var result =
            SimulationStepRunner.Step(
                world,
                86_400,
                new ForagingSystem(
                    planet.Id));

        Assert.Equal(
            0.5,
            world.Population[0]
                .Needs.EnergyReserve);

        Assert.Equal(
            5,
            world.FoodResources[0]
                .AvailableEnergy);

        Assert.NotEqual(
            world.Population[0]
                .Needs.EnergyReserve,
            result.World.Population[0]
                .Needs.EnergyReserve);

        Assert.NotEqual(
            world.FoodResources[0]
                .AvailableEnergy,
            result.World.FoodResources[0]
                .AvailableEnergy);
    }

    [Fact]
    public void Step_NoNearbyFoodLeavesPersonForaging()
    {
        var planet = CreatePlanet();

        var person =
            CreateHungryPerson(
                planet.Id,
                0,
                0,
                0.25);

        var food =
            new FoodResourceState(
                FoodResourceId.New(),
                planet.Id,
                10,
                10,
                10);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [person],
                [food]);

        var result =
            SimulationStepRunner.Step(
                world,
                86_400,
                new ForagingSystem(
                    planet.Id,
                    searchRadiusDegrees: 1));

        var changedPerson =
            Assert.Single(
                result.World.Population);

        Assert.Equal(
            0.25,
            changedPerson.Needs.EnergyReserve);

        Assert.Equal(
            PersonActivity.Foraging,
            changedPerson.Activity);

        Assert.Equal(
            10,
            Assert.Single(
                result.World.FoodResources)
                .AvailableEnergy);

        Assert.Equal(
            0,
            result.Change.Metrics["fed"]);
    }

    [Fact]
    public void Step_LimitedFoodIsExhausted()
    {
        var planet = CreatePlanet();

        var person =
            CreateHungryPerson(
                planet.Id,
                0,
                0,
                0.25);

        var food =
            new FoodResourceState(
                FoodResourceId.New(),
                planet.Id,
                0,
                0,
                0.2);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [person],
                [food]);

        var result =
            SimulationStepRunner.Step(
                world,
                86_400,
                new ForagingSystem(
                    planet.Id));

        var changedPerson =
            Assert.Single(
                result.World.Population);

        Assert.Equal(
            0.45,
            changedPerson.Needs.EnergyReserve,
            10);

        Assert.Equal(
            0,
            Assert.Single(
                result.World.FoodResources)
                .AvailableEnergy);

        Assert.Equal(
            1,
            result.Change.Metrics[
                "exhaustedResources"]);
    }

    private static PersonState CreateHungryPerson(
        PlanetId planetId,
        double latitude,
        double longitude,
        double energy)
    {
        return new PersonState(
            PersonId.New(),
            planetId,
            PersonSex.Female,
            -25 * 31_536_000L,
            latitude,
            longitude,
            needs:
                new PersonNeedsState(
                    energyReserve: energy,
                    health: 1),
            activity:
                PersonActivity.Foraging);
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
