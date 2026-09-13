using Est.Simulation.Causality;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Population;

public sealed class PopulationSystemTests
{
    private const long OneYearSeconds = 31_536_000;

    [Fact]
    public void Step_PreservesSourceWorldAndProducesPopulationChange()
    {
        var planet = CreateEarth();
        var founders = CreateFounders(
            planet.Id);

        var world = new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [planet],
            founders);

        var system = new PopulationSystem(
            planet.Id,
            new PopulationModelParameters(
                seed: 42,
                annualBirthRatePerEligibleFemale: 4,
                annualAdultMigrationRate: 1,
                annualBaseMortalityRate: 0,
                annualElderMortalityRate: 0));

        var result =
            SimulationStepRunner.Step(
                world,
                OneYearSeconds,
                system);

        Assert.Equal(
            founders.Length,
            world.Population.Length);

        Assert.True(
            result.World.Population.Length >
            world.Population.Length);

        Assert.Equal(
            OneYearSeconds,
            result.World.CurrentTime.TotalSeconds);

        Assert.Equal(
            "population-dynamics",
            result.Change.Cause);

        Assert.True(
            result.Change.Metrics["births"] > 0);

        Assert.True(
            result.Change.Metrics["migrations"] > 0);
    }

    [Fact]
    public void Evaluate_IsDeterministicForSameWorldAndStep()
    {
        var planet = CreateEarth();

        var world = new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [planet],
            CreateFounders(planet.Id));

        var system = new PopulationSystem(
            planet.Id,
            new PopulationModelParameters(
                seed: 12345,
                annualBirthRatePerEligibleFemale: 1,
                annualAdultMigrationRate: 0.5,
                annualBaseMortalityRate: 0.01,
                annualElderMortalityRate: 0.1));

        var first =
            SimulationStepRunner.Step(
                world,
                OneYearSeconds,
                system);

        var second =
            SimulationStepRunner.Step(
                world,
                OneYearSeconds,
                system);

        Assert.True(
            first.World.Population.SequenceEqual(
                second.World.Population));

        Assert.Equal(
            first.Change.Metrics["births"],
            second.Change.Metrics["births"]);

        Assert.Equal(
            first.Change.Metrics["deaths"],
            second.Change.Metrics["deaths"]);

        Assert.Equal(
            first.Change.Metrics["migrations"],
            second.Change.Metrics["migrations"]);
    }

    [Fact]
    public void Step_HighElderMortalityRemovesOldPeople()
    {
        var planet = CreateEarth();

        var oldPerson = new PersonState(
            new PersonId(
                Guid.Parse(
                    "10000000-0000-0000-0000-000000000001")),
            planet.Id,
            PersonSex.Female,
            -90 * OneYearSeconds,
            10,
            20);

        var world = new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [planet],
            [oldPerson]);

        var system = new PopulationSystem(
            planet.Id,
            new PopulationModelParameters(
                seed: 7,
                annualBirthRatePerEligibleFemale: 0,
                annualAdultMigrationRate: 0,
                annualBaseMortalityRate: 0,
                annualElderMortalityRate: 100));

        var result =
            SimulationStepRunner.Step(
                world,
                OneYearSeconds,
                system);

        Assert.Empty(result.World.Population);

        Assert.Equal(
            1,
            result.Change.Metrics["deaths"]);
    }

    [Fact]
    public void Step_PopulationLocationsRemainOnGlobe()
    {
        var planet = CreateEarth();

        var world = new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [planet],
            CreateFounders(planet.Id));

        var system = new PopulationSystem(
            planet.Id,
            new PopulationModelParameters(
                seed: 99,
                annualBirthRatePerEligibleFemale: 3,
                annualAdultMigrationRate: 1,
                annualBaseMortalityRate: 0,
                annualElderMortalityRate: 0,
                localMigrationDegrees: 20,
                longMigrationProbability: 1,
                longMigrationDegrees: 180));

        var result =
            SimulationStepRunner.Step(
                world,
                OneYearSeconds,
                system);

        Assert.All(
            result.World.Population,
            person =>
            {
                Assert.InRange(
                    person.LatitudeDegrees,
                    -90,
                    90);

                Assert.InRange(
                    person.LongitudeDegrees,
                    -180,
                    180);
            });
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
                AtmosphereState.Vacuum));
    }

    private static PersonState[] CreateFounders(
        PlanetId planetId)
    {
        return
        [
            new PersonState(
                new PersonId(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000001")),
                planetId,
                PersonSex.Female,
                -25 * OneYearSeconds,
                0,
                0),
            new PersonState(
                new PersonId(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000002")),
                planetId,
                PersonSex.Male,
                -27 * OneYearSeconds,
                0.1,
                0.1),
            new PersonState(
                new PersonId(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000003")),
                planetId,
                PersonSex.Female,
                -22 * OneYearSeconds,
                -0.1,
                0.05),
            new PersonState(
                new PersonId(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000004")),
                planetId,
                PersonSex.Male,
                -24 * OneYearSeconds,
                0.05,
                -0.1)
        ];
    }
}
