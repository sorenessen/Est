using Est.Simulation.Animals;
using Est.Simulation.Causality;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Animals;

public sealed class WolfPredatorSystemTests
{
    private const long OneDaySeconds = 86_400;

    [Fact]
    public void Step_NonDesperateWolfDoesNotAttackNearbyHuman()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 0.05);

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0,
                energyReserve: 0.35);

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [person],
                    [wolf]),
                OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.Single(
            result.World.Population);

        var changedWolf =
            Assert.Single(
                result.World.Animals);

        Assert.Equal(
            0.27,
            changedWolf.EnergyReserve,
            precision: 10);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "wolfAttacks"]);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "predationDeaths"]);
    }

    [Fact]
    public void Step_DesperateWolfDoesNotHomeTowardDistantHuman()
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
                longitude: 0,
                energyReserve: 0.05);

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [person],
                    [wolf]),
                14 * OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.Single(
            result.World.Population);

        var changedWolf =
            Assert.Single(
                result.World.Animals);

        Assert.Equal(
            0,
            changedWolf.LongitudeDegrees,
            precision: 10);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "wolfAttacks"]);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "predationDeaths"]);
    }

    [Fact]
    public void Step_HumanGroupDetersLoneWolf()
    {
        var planet = CreatePlanet();

        var first =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 0.05);

        var second =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 0.10);

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0,
                energyReserve: 0.01);

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [first, second],
                    [wolf]),
                14 * OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.Equal(
            2,
            result.World.Population.Length);

        Assert.True(
            result.Change.Metrics[
                "avoidedEncounters"] >= 1);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "predationDeaths"]);

        var changedWolf =
            Assert.Single(
                result.World.Animals);

        Assert.True(
            changedWolf.LongitudeDegrees < 0);
    }

    [Fact]
    public void Step_StarvingWolfAtContactCanBeInjuredByHumanDefense()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 0.05,
                id: new PersonId(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000101")));

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0,
                energyReserve: 0,
                id: new AnimalId(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000301")));

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [person],
                    [wolf]),
                14 * OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.True(
            result.Change.Metrics[
                "wolfAttacks"] >= 1);

        Assert.True(
            result.Change.Metrics[
                "wolfInjuries"] >= 1);

        if (result.World.Animals.Length > 0)
        {
            var changedWolf =
                Assert.Single(
                    result.World.Animals);

            Assert.True(
                changedWolf.Health < 1);
        }
        else
        {
            Assert.True(
                result.Change.Metrics[
                    "wolfDeaths"] +
                result.Change.Metrics[
                    "wolfStarvationDeaths"] >= 1);
        }
    }

    [Fact]
    public void Step_DesperateWolfPackCanEscalateAgainstIsolatedHuman()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 0.05);

        var firstWolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0,
                energyReserve: 0);

        var secondWolf =
            CreateWolf(
                planet.Id,
                latitude: 0.01,
                longitude: 0,
                energyReserve: 0,
                id: new AnimalId(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000302")));

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [person],
                    [firstWolf, secondWolf]),
                14 * OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.True(
            result.Change.Metrics[
                "packEncounters"] >= 1);

        Assert.True(
            result.Change.Metrics[
                "wolfAttacks"] >= 1);
    }

    [Fact]
    public void Step_SatiatedWolfRestsInsteadOfTargetingHuman()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 0.05);

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0,
                energyReserve: 1);

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [person],
                    [wolf]),
                OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.Single(
            result.World.Population);

        var changedWolf =
            Assert.Single(
                result.World.Animals);

        Assert.Equal(
            AnimalActivity.Idle,
            changedWolf.Activity);

        Assert.Equal(
            0.92,
            changedWolf.EnergyReserve,
            precision: 10);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "wolfAttacks"]);
    }

    [Fact]
    public void Step_WolfEnergyUseRemainsBoundedForPartialDay()
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
                longitude: 0,
                energyReserve: 1);

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [person],
                    [wolf]),
                OneDaySeconds / 2,
                new WolfPredatorSystem(
                    planet.Id));

        var changedWolf =
            Assert.Single(
                result.World.Animals);

        Assert.Equal(
            0.96,
            changedWolf.EnergyReserve,
            precision: 10);

        Assert.Equal(
            1,
            changedWolf.Health,
            precision: 10);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "wolfStarvationDeaths"]);
    }

    [Fact]
    public void Step_WolfWithoutFoodEventuallyDiesFromStarvation()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 10);

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0,
                energyReserve: 0.05);

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [person],
                    [wolf]),
                30 * OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.Empty(
            result.World.Animals);

        Assert.Single(
            result.World.Population);

        Assert.Equal(
            1,
            result.Change.Metrics[
                "wolfStarvationDeaths"]);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "predationDeaths"]);
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

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [person],
                    []),
                OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.Single(
            result.World.Population);

        Assert.Empty(
            result.World.Animals);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "wolfAttacks"]);
    }

    [Fact]
    public void Step_PredationPreservesSourceWorld()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 0.05);

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0,
                energyReserve: 0);

        var world =
            CreateWorld(
                planet,
                [person],
                [wolf]);

        var result =
            SimulationStepRunner.Step(
                world,
                14 * OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.Single(
            world.Population);

        Assert.Single(
            world.Animals);

        Assert.NotSame(
            world,
            result.World);
    }

    private static WorldState CreateWorld(
        PlanetState planet,
        PersonState[] people,
        AnimalState[] animals)
    {
        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [planet],
            people,
            animals);
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
            id ??
                new PersonId(
                    Guid.NewGuid()),
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
        double energyReserve,
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
            energyReserve,
            health: 1,
            activity:
                AnimalActivity.Hunting);
    }
}
