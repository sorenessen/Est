using Est.Simulation.Causality;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Population;

public sealed class ReproductionSystemTests
{
    private const long OneYearSeconds = 31_536_000;

    [Fact]
    public void Step_DistantEligiblePartnersSeekEachOtherBeforeBirth()
    {
        var planet = CreateEarth();

        var female =
            CreateAdult(
                planet.Id,
                PersonSex.Female,
                0,
                0);

        var male =
            CreateAdult(
                planet.Id,
                PersonSex.Male,
                0,
                1);

        var world =
            CreateWorld(
                planet,
                female,
                male);

        var result =
            SimulationStepRunner.Step(
                world,
                86_400,
                CreateSystem(
                    planet.Id,
                    birthRate: 1000));

        Assert.Equal(
            2,
            result.World.Population.Length);

        var movedFemale =
            result.World.Population.Single(
                person =>
                    person.Id == female.Id);

        Assert.Equal(
            PersonActivity.SeekingPartner,
            movedFemale.Activity);

        Assert.True(
            movedFemale.LongitudeDegrees > 0);

        Assert.Equal(
            1,
            result.Change.Metrics["partnerSeeking"]);

        Assert.Equal(
            0,
            result.Change.Metrics["births"]);
    }

    [Fact]
    public void Step_NearbyEligiblePartnersCanProduceChild()
    {
        var planet = CreateEarth();

        var female =
            CreateAdult(
                planet.Id,
                PersonSex.Female,
                0,
                0);

        var male =
            CreateAdult(
                planet.Id,
                PersonSex.Male,
                0,
                0.05);

        var world =
            CreateWorld(
                planet,
                female,
                male);

        var result =
            SimulationStepRunner.Step(
                world,
                OneYearSeconds,
                CreateSystem(
                    planet.Id,
                    birthRate: 1000));

        Assert.Equal(
            3,
            result.World.Population.Length);

        var mother =
            result.World.Population.Single(
                person =>
                    person.Id == female.Id);

        Assert.Equal(
            PersonActivity.Mating,
            mother.Activity);

        var child =
            result.World.Population.Single(
                person =>
                    person.ParentId ==
                    female.Id);

        Assert.Equal(
            OneYearSeconds,
            child.BirthTimeSeconds);

        Assert.Equal(
            1,
            result.Change.Metrics["matingEvents"]);

        Assert.Equal(
            1,
            result.Change.Metrics["births"]);
    }

    [Fact]
    public void Step_NoNearbyPartnerPreventsBirth()
    {
        var planet = CreateEarth();

        var female =
            CreateAdult(
                planet.Id,
                PersonSex.Female,
                0,
                0);

        var male =
            CreateAdult(
                planet.Id,
                PersonSex.Male,
                0,
                20);

        var world =
            CreateWorld(
                planet,
                female,
                male);

        var result =
            SimulationStepRunner.Step(
                world,
                OneYearSeconds,
                CreateSystem(
                    planet.Id,
                    birthRate: 1000));

        Assert.Equal(
            2,
            result.World.Population.Length);

        Assert.Equal(
            1,
            result.Change.Metrics["noPartnerFound"]);

        Assert.Equal(
            0,
            result.Change.Metrics["births"]);
    }

    [Fact]
    public void Step_StarvingPersonDoesNotSeekPartnerOrMate()
    {
        var planet = CreateEarth();

        var female =
            CreateAdult(
                planet.Id,
                PersonSex.Female,
                0,
                0)
            .WithSurvivalState(
                new PersonNeedsState(
                    energyReserve: 0.3,
                    health: 1),
                PersonActivity.Foraging);

        var male =
            CreateAdult(
                planet.Id,
                PersonSex.Male,
                0,
                0.05);

        var world =
            CreateWorld(
                planet,
                female,
                male);

        var result =
            SimulationStepRunner.Step(
                world,
                OneYearSeconds,
                CreateSystem(
                    planet.Id,
                    birthRate: 1000));

        Assert.Equal(
            2,
            result.World.Population.Length);

        var unchangedFemale =
            result.World.Population.Single(
                person =>
                    person.Id == female.Id);

        Assert.Equal(
            PersonActivity.Foraging,
            unchangedFemale.Activity);

        Assert.Equal(
            0,
            result.Change.Metrics["eligibleFemales"]);

        Assert.Equal(
            0,
            result.Change.Metrics["births"]);
    }

    [Fact]
    public void Step_FleeingPersonDoesNotSeekPartnerOrMate()
    {
        var planet = CreateEarth();

        var female =
            CreateAdult(
                planet.Id,
                PersonSex.Female,
                0,
                0)
            .WithSurvivalState(
                new PersonNeedsState(),
                PersonActivity.Fleeing);

        var male =
            CreateAdult(
                planet.Id,
                PersonSex.Male,
                0,
                0.05);

        var world =
            CreateWorld(
                planet,
                female,
                male);

        var result =
            SimulationStepRunner.Step(
                world,
                OneYearSeconds,
                CreateSystem(
                    planet.Id,
                    birthRate: 1000));

        Assert.Equal(
            2,
            result.World.Population.Length);

        Assert.Equal(
            0,
            result.Change.Metrics["eligibleFemales"]);

        Assert.Equal(
            0,
            result.Change.Metrics["births"]);
    }

    private static ReproductionSystem CreateSystem(
        PlanetId planetId,
        double birthRate)
    {
        return new ReproductionSystem(
            planetId,
            new PopulationModelParameters(
                seed: 42,
                annualBirthRatePerEligibleFemale:
                    birthRate,
                annualAdultMigrationRate: 0,
                annualBaseMortalityRate: 0,
                annualElderMortalityRate: 0));
    }

    private static WorldState CreateWorld(
        PlanetState planet,
        params PersonState[] population)
    {
        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [planet],
            population);
    }

    private static PersonState CreateAdult(
        PlanetId planetId,
        PersonSex sex,
        double latitude,
        double longitude)
    {
        return new PersonState(
            PersonId.New(),
            planetId,
            sex,
            -25 * OneYearSeconds,
            latitude,
            longitude);
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
}
