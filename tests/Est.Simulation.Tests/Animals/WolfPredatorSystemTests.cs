using Est.Simulation.Animals;
using Est.Simulation.Causality;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Animals;

public sealed class WolfPredatorSystemTests
{
    [Fact]
    public void Step_WolfWithinAttackRadiusKillsNearestPerson()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 10,
                longitude: 20,
                id: new PersonId(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000101")));

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 10.05,
                longitude: 20.05,
                id: new AnimalId(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000301")));

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [person],
                [],
                [wolf]);

        var result =
            SimulationStepRunner.Step(
                world,
                86_400,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.Empty(result.World.Population);

        var changedWolf =
            Assert.Single(result.World.Animals);

        Assert.Equal(
            AnimalActivity.Eating,
            changedWolf.Activity);

        Assert.Equal(
            person.LatitudeDegrees,
            changedWolf.LatitudeDegrees);

        Assert.Equal(
            person.LongitudeDegrees,
            changedWolf.LongitudeDegrees);

        Assert.Equal(
            1,
            result.Change.Metrics["wolfAttacks"]);

        Assert.Equal(
            1,
            result.Change.Metrics["successfulKills"]);

        Assert.Equal(
            1,
            result.Change.Metrics["predationDeaths"]);
    }

    [Fact]
    public void Step_FailedAttackLetsHumanFlee()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 0.05,
                id: new PersonId(
                    Guid.Parse(
                        "159dfc07-5c90-45d6-bf6b-be33bc2eb035")));

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0,
                id: new AnimalId(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000301")));

        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(86_400),
                [planet],
                [person],
                [],
                [wolf]);

        var result =
            SimulationStepRunner.Step(
                world,
                86_400,
                new WolfPredatorSystem(
                    planet.Id));

        var changedPerson =
            Assert.Single(
                result.World.Population);

        Assert.Equal(
            PersonActivity.Fleeing,
            changedPerson.Activity);

        Assert.True(
            changedPerson.LongitudeDegrees >
            person.LongitudeDegrees);

        var changedWolf =
            Assert.Single(
                result.World.Animals);

        Assert.Equal(
            AnimalActivity.Attacking,
            changedWolf.Activity);

        Assert.Equal(
            1,
            result.Change.Metrics["wolfAttacks"]);

        Assert.Equal(
            1,
            result.Change.Metrics["failedAttacks"]);

        Assert.Equal(
            0,
            result.Change.Metrics["successfulKills"]);

        Assert.Equal(
            0,
            result.Change.Metrics["predationDeaths"]);
    }

    [Fact]
    public void Step_DistantWolfTravelsTowardNearestPerson()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 2);

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [person],
                [],
                [wolf]);

        var result =
            SimulationStepRunner.Step(
                world,
                86_400,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.Single(result.World.Population);

        var changedWolf =
            Assert.Single(result.World.Animals);

        Assert.Equal(
            AnimalActivity.Traveling,
            changedWolf.Activity);

        Assert.Equal(
            0,
            changedWolf.LatitudeDegrees,
            10);

        Assert.Equal(
            0.75,
            changedWolf.LongitudeDegrees,
            10);

        Assert.Equal(
            0,
            result.Change.Metrics["predationDeaths"]);

        Assert.Equal(
            1,
            result.Change.Metrics["chaseSteps"]);
    }

    [Fact]
    public void Step_HumanWithinThreatRadiusFleesBeforeWolfPursues()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 0.8);

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [person],
                [],
                [wolf]);

        var result =
            SimulationStepRunner.Step(
                world,
                86_400,
                new WolfPredatorSystem(
                    planet.Id));

        var changedPerson =
            Assert.Single(
                result.World.Population);

        Assert.Equal(
            PersonActivity.Fleeing,
            changedPerson.Activity);

        Assert.Equal(
            1.05,
            changedPerson.LongitudeDegrees,
            10);

        var changedWolf =
            Assert.Single(
                result.World.Animals);

        Assert.Equal(
            AnimalActivity.Traveling,
            changedWolf.Activity);

        Assert.Equal(
            0.75,
            changedWolf.LongitudeDegrees,
            10);

        Assert.Equal(
            1,
            result.Change.Metrics["fleeSteps"]);

        Assert.Equal(
            1,
            result.Change.Metrics["chaseSteps"]);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "predationDeaths"]);
    }

    [Fact]
    public void Step_WolfTravelRemainsBoundedForPartialDay()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 2);

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [person],
                [],
                [wolf]);

        var result =
            SimulationStepRunner.Step(
                world,
                43_200,
                new WolfPredatorSystem(
                    planet.Id));

        var changedWolf =
            Assert.Single(result.World.Animals);

        Assert.Equal(
            0.375,
            changedWolf.LongitudeDegrees,
            10);

        Assert.Single(result.World.Population);
    }

    [Fact]
    public void Step_NoWolfDoesNotCreatePredator()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 0);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [person]);

        var result =
            SimulationStepRunner.Step(
                world,
                86_400,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.Single(result.World.Population);
        Assert.Empty(result.World.Animals);

        Assert.Equal(
            0,
            result.Change.Metrics["wolfAttacks"]);

        Assert.Equal(
            0,
            result.Change.Metrics["predationDeaths"]);
    }

    [Fact]
    public void Step_PredationPreservesSourceWorld()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 0,
                id: new PersonId(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000101")));

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0.05,
                id: new AnimalId(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000301")));

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [person],
                [],
                [wolf]);

        var result =
            SimulationStepRunner.Step(
                world,
                86_400,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.Single(world.Population);
        Assert.Single(world.Animals);

        Assert.Empty(result.World.Population);
        Assert.Single(result.World.Animals);
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

    private static PersonState CreatePerson(
        PlanetId planetId,
        double latitude,
        double longitude,
        PersonId? id = null)
    {
        return new PersonState(
            id ?? new PersonId(Guid.NewGuid()),
            planetId,
            PersonSex.Female,
            -800_000_000,
            latitude,
            longitude);
    }

    private static AnimalState CreateWolf(
        PlanetId planetId,
        double latitude,
        double longitude,
        AnimalId? id = null)
    {
        return new AnimalState(
            id ??
            new AnimalId(
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000301")),
            planetId,
            AnimalSpecies.Wolf,
            latitude,
            longitude,
            energyReserve: 0.35,
            health: 1,
            activity: AnimalActivity.Hunting);
    }
}
