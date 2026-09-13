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
            1 - (1d / 30d),
            changedPerson.Needs.EnergyReserve,
            10);

        Assert.Equal(
            PersonActivity.Eating,
            changedPerson.Activity);

        Assert.Equal(
            9.25,
            changedFood.AvailableEnergy,
            10);

        Assert.Equal(
            1,
            result.Change.Metrics["feedingEvents"]);

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
                0,
                40,
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
            0.25 - (1d / 30d),
            changedPerson.Needs.EnergyReserve,
            10);

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
            result.Change.Metrics["feedingEvents"]);
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
            0.45 - (1d / 30d),
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

    [Fact]
    public void Step_HungryPersonTravelsTowardDiscoverableFood()
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
                3,
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
            0,
            changedPerson.LatitudeDegrees,
            10);

        Assert.Equal(
            0.25,
            changedPerson.LongitudeDegrees,
            10);

        Assert.Equal(
            PersonActivity.Traveling,
            changedPerson.Activity);

        Assert.Equal(
            10,
            Assert.Single(
                result.World.FoodResources)
                .AvailableEnergy);

        Assert.Equal(
            0,
            result.Change.Metrics["feedingEvents"]);
    }

    [Fact]
    public void Step_HungryPersonDoesNotTeleportToFood()
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
                2,
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

        Assert.True(
            changedPerson.LongitudeDegrees > 0);

        Assert.True(
            changedPerson.LongitudeDegrees < 1);

        Assert.Equal(
            PersonActivity.Traveling,
            changedPerson.Activity);
    }

    [Fact]
    public void Step_HungryPersonEventuallyReachesAndConsumesFood()
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
                2,
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
                5 * 86_400L,
                new ForagingSystem(
                    planet.Id,
                    searchRadiusDegrees: 1));

        var changedPerson =
            Assert.Single(
                result.World.Population);

        Assert.Equal(
            PersonActivity.Eating,
            changedPerson.Activity);

        Assert.True(
            changedPerson.LongitudeDegrees > 0);

        Assert.True(
            Assert.Single(
                result.World.FoodResources)
                .AvailableEnergy < 10);

        Assert.True(
            result.Change.Metrics["feedingEvents"] > 0);
    }

    [Fact]
    public void Step_DepletedFoodRecoversAtConfiguredRate()
    {
        var planet = CreatePlanet();

        var food =
            new FoodResourceState(
                FoodResourceId.New(),
                planet.Id,
                0,
                0,
                availableEnergy: 10,
                capacityEnergy: 100,
                recoveryEnergyPerDay: 5);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [],
                [food]);

        var result =
            SimulationStepRunner.Step(
                world,
                2 * 86_400L,
                new ForagingSystem(
                    planet.Id));

        var changedFood =
            Assert.Single(
                result.World.FoodResources);

        Assert.Equal(
            20,
            changedFood.AvailableEnergy,
            10);

        Assert.Equal(
            10,
            result.Change.Metrics[
                "energyRecovered"],
            10);
    }

    [Fact]
    public void Step_RecoveryOccursBeforeForagingWithinInternalStep()
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
                availableEnergy: 0,
                capacityEnergy: 10,
                recoveryEnergyPerDay: 0.2);

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
            0.45 - (1d / 30d),
            changedPerson.Needs.EnergyReserve,
            10);

        Assert.Equal(
            PersonActivity.Eating,
            changedPerson.Activity);

        Assert.Equal(
            0,
            changedFood.AvailableEnergy,
            10);

        Assert.Equal(
            0.2,
            result.Change.Metrics[
                "energyRecovered"],
            10);

        Assert.Equal(
            0.2,
            result.Change.Metrics[
                "energyConsumed"],
            10);
    }

    [Fact]
    public void Step_LocalScarcityTriggersMigrationTowardDistantFood()
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
                10,
                100);

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
            0,
            changedPerson.LatitudeDegrees,
            10);

        Assert.Equal(
            0.25,
            changedPerson.LongitudeDegrees,
            10);

        Assert.Equal(
            PersonActivity.Traveling,
            changedPerson.Activity);

        Assert.Equal(
            1,
            result.Change.Metrics[
                "scarcityMigrations"]);
    }

    [Fact]
    public void Step_InsufficientLocalRecoveryDoesNotBlockScarcityMigration()
    {
        var planet = CreatePlanet();

        var person =
            CreateHungryPerson(
                planet.Id,
                0,
                0,
                0.5);

        var localFood =
            new FoodResourceState(
                FoodResourceId.New(),
                planet.Id,
                0,
                2,
                availableEnergy: 0,
                capacityEnergy: 20,
                recoveryEnergyPerDay: 0.03);

        var distantFood =
            new FoodResourceState(
                FoodResourceId.New(),
                planet.Id,
                0,
                10,
                availableEnergy: 20,
                capacityEnergy: 20);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [person],
                [localFood, distantFood]);

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
            0.25,
            changedPerson.LongitudeDegrees,
            10);

        Assert.Equal(
            PersonActivity.Traveling,
            changedPerson.Activity);

        Assert.Equal(
            1,
            result.Change.Metrics[
                "scarcityMigrations"]);

        var changedLocalFood =
            Assert.Single(
                result.World.FoodResources,
                resource =>
                    resource.Id == localFood.Id);

        Assert.Equal(
            0.03,
            changedLocalFood.AvailableEnergy,
            10);
    }

    [Fact]
    public void Step_ScarcityMigrationRemainsBounded()
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
                20,
                100);

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
            0.25,
            changedPerson.LongitudeDegrees,
            10);

        Assert.NotEqual(
            food.LongitudeDegrees,
            changedPerson.LongitudeDegrees);

        Assert.Equal(
            PersonActivity.Traveling,
            changedPerson.Activity);
    }

    [Fact]
    public void Step_FoodBeyondScarcityMigrationRadiusDoesNotCauseTravel()
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
                40,
                100);

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
            0,
            changedPerson.LongitudeDegrees,
            10);

        Assert.Equal(
            PersonActivity.Foraging,
            changedPerson.Activity);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "scarcityMigrations"]);
    }

    [Fact]
    public void Step_LongAdvanceWithFoodIntegratesSurvivalDaily()
    {
        var planet = CreatePlanet();

        var person =
            CreateHungryPerson(
                planet.Id,
                0,
                0,
                1);

        var food =
            new FoodResourceState(
                FoodResourceId.New(),
                planet.Id,
                0,
                0,
                100);

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
                365 * 86_400L,
                new ForagingSystem(
                    planet.Id));

        var survivor =
            Assert.Single(
                result.World.Population);

        Assert.True(
            survivor.Needs.Health > 0);

        Assert.True(
            survivor.Needs.EnergyReserve > 0);

        Assert.True(
            result.Change.Metrics[
                "feedingEvents"] > 1);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "starvationDeaths"]);

        Assert.Equal(
            365 * 86_400L,
            result.Change.ElapsedSeconds);
    }

    [Fact]
    public void Step_LongAdvanceWithoutFoodCausesStarvation()
    {
        var planet = CreatePlanet();

        var person =
            CreateHungryPerson(
                planet.Id,
                0,
                0,
                1);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [person],
                []);

        var result =
            SimulationStepRunner.Step(
                world,
                60 * 86_400L,
                new ForagingSystem(
                    planet.Id));

        Assert.Empty(
            result.World.Population);

        Assert.Equal(
            1,
            result.Change.Metrics[
                "starvationDeaths"]);

        Assert.Equal(
            60 * 86_400L,
            result.Change.ElapsedSeconds);
    }

    [Fact]
    public void Step_LongAdvanceStillProducesOneCausalChange()
    {
        var planet = CreatePlanet();

        var person =
            CreateHungryPerson(
                planet.Id,
                0,
                0,
                1);

        var food =
            new FoodResourceState(
                FoodResourceId.New(),
                planet.Id,
                0,
                0,
                100);

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
                365 * 86_400L,
                new ForagingSystem(
                    planet.Id));

        Assert.Single(
            result.Changes);

        Assert.Equal(
            365 * 86_400L,
            result.ElapsedSeconds);

        Assert.Equal(
            365 * 86_400L,
            result.World
                .CurrentTime
                .TotalSeconds);
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
