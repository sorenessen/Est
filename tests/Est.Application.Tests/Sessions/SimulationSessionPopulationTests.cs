using Est.Application.Sessions;
using Est.Simulation.Definitions;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Application.Tests.Sessions;

public sealed class SimulationSessionPopulationTests
{
    private const long OneYearSeconds = 31_536_000;

    [Fact]
    public void Advance_RunsConfiguredPopulationSystem()
    {
        const long oneDaySeconds = 86_400;
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Earth",
                5.9722e24,
                6_371_000,
                new PlanetEnvironment(
                    288.15,
                    0.71,
                    0.03,
                    AtmosphereState.Vacuum));

        var founders =
            new[]
            {
                new PersonState(
                    new PersonId(
                        Guid.Parse(
                            "00000000-0000-0000-0000-000000000001")),
                    planet.Id,
                    PersonSex.Female,
                    -25 * OneYearSeconds,
                    0,
                    0),
                new PersonState(
                    new PersonId(
                        Guid.Parse(
                            "00000000-0000-0000-0000-000000000002")),
                    planet.Id,
                    PersonSex.Male,
                    -25 * OneYearSeconds,
                    0.1,
                    0.1)
            };

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                founders);

        var definition =
            new SimulationDefinition(
                populationModels:
                [
                    new PopulationModelDefinition(
                        planet.Id,
                        new PopulationModelParameters(
                            seed: 42,
                            annualBirthRatePerEligibleFemale: 0,
                            annualAdultMigrationRate: 0,
                            annualBaseMortalityRate: 0,
                            annualElderMortalityRate: 0))
                ]);

        var session =
            new SimulationSession(
                world,
                definition);

        session.Advance(oneDaySeconds);

        Assert.Equal(
            founders.Length,
            session.CurrentWorld.Population.Length);

        Assert.All(
            session.CurrentWorld.Population,
            person =>
                Assert.True(
                    person.Needs.EnergyReserve < 1));

        Assert.Equal(
            oneDaySeconds,
            session.CurrentWorld
                .CurrentTime.TotalSeconds);

        Assert.Contains(
            session.Timeline.Events,
            timelineEvent =>
                timelineEvent.Cause ==
                    "population-dynamics");
    }
}
