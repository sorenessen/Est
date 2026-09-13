using Est.Application.Sessions;
using Est.Simulation.Definitions;
using Est.Simulation.Ecology;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Application.Tests.Sessions;

public sealed class SimulationSessionForagingTests
{
    [Fact]
    public void Advance_HungryPersonEatsBeforePopulationMetabolism()
    {
        var planet = CreatePlanet();

        var person =
            new PersonState(
                PersonId.New(),
                planet.Id,
                PersonSex.Female,
                -25 * 31_536_000L,
                0,
                0,
                needs:
                    new PersonNeedsState(
                        energyReserve: 0.5,
                        health: 1),
                activity:
                    PersonActivity.Foraging);

        var food =
            new FoodResourceState(
                FoodResourceId.New(),
                planet.Id,
                0,
                0,
                10);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [person],
                [food]);

        var definition =
            new SimulationDefinition(
                populationModels:
                [
                    new PopulationModelDefinition(
                        planet.Id,
                        new PopulationModelParameters(
                            annualBirthRatePerEligibleFemale: 0,
                            annualAdultMigrationRate: 0,
                            annualBaseMortalityRate: 0,
                            annualElderMortalityRate: 0))
                ]);

        var session =
            new SimulationSession(
                world,
                definition);

        session.Advance(86_400);

        var changed =
            Assert.Single(
                session.CurrentWorld.Population);

        Assert.True(
            changed.Needs.EnergyReserve > 0.9);

        Assert.Equal(
            9.5,
            Assert.Single(
                session.CurrentWorld.FoodResources)
                .AvailableEnergy,
            10);

        Assert.Contains(
            session.Timeline.Events,
            step =>
                step.Cause ==
                "foraging");

        Assert.Contains(
            session.Timeline.Events,
            step =>
                step.Cause ==
                "population-dynamics");
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
