using Est.Simulation.Causality;
using Est.Simulation.Organisms;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Population;

public sealed class ReproductionSystemTests
{
    private const long OneDaySeconds = 86_400;
    private const long OneYearSeconds = 31_536_000;

    [Fact]
    public void Step_DistantEligiblePartnersSeekEachOtherBeforeConception()
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
                OneDaySeconds,
                CreateSystem(
                    planet.Id,
                    conceptionProbability: 1));

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

        Assert.Null(movedFemale.Pregnancy);

        Assert.Equal(
            1,
            result.Change.Metrics["partnerSeeking"]);

        Assert.Equal(
            0,
            result.Change.Metrics["conceptions"]);

        Assert.Equal(
            0,
            result.Change.Metrics["births"]);
    }

    [Fact]
    public void Step_NearbyEligiblePartnersCanConceiveWithinOneCycle()
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

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    female,
                    male),
                28 * OneDaySeconds,
                CreateSystem(
                    planet.Id,
                    conceptionProbability: 1));

        Assert.Equal(
            2,
            result.World.Population.Length);

        var mother =
            result.World.Population.Single(
                person =>
                    person.Id == female.Id);

        Assert.Equal(
            PersonActivity.Mating,
            mother.Activity);

        var pregnancy =
            Assert.IsType<PregnancyState>(
                mother.Pregnancy);

        Assert.InRange(
            pregnancy.ConceptionTimeSeconds,
            1,
            28 * OneDaySeconds);

        Assert.Equal(
            male.Id,
            pregnancy.FatherId);

        Assert.Equal(
            1,
            result.Change.Metrics["matingEvents"]);

        Assert.Equal(
            1,
            result.Change.Metrics["conceptions"]);

        Assert.Equal(
            0,
            result.Change.Metrics["births"]);
    }

    [Fact]
    public void Step_PregnancyPersistsBeforeGestationAndPreventsReconception()
    {
        var planet = CreateEarth();

        var father =
            CreateAdult(
                planet.Id,
                PersonSex.Male,
                0,
                0.05);

        var mother =
            CreateAdult(
                planet.Id,
                PersonSex.Female,
                0,
                0)
            .WithPregnancy(
                new PregnancyState(
                    conceptionTimeSeconds: 0,
                    father.Id));

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    mother,
                    father),
                279 * OneDaySeconds,
                CreateSystem(
                    planet.Id,
                    conceptionProbability: 1));

        Assert.Equal(
            2,
            result.World.Population.Length);

        var restoredMother =
            result.World.Population.Single(
                person =>
                    person.Id == mother.Id);

        var pregnancy =
            Assert.IsType<PregnancyState>(
                restoredMother.Pregnancy);

        Assert.Equal(
            0,
            pregnancy.ConceptionTimeSeconds);

        Assert.Equal(
            father.Id,
            pregnancy.FatherId);

        Assert.Equal(
            PersonActivity.Idle,
            restoredMother.Activity);

        Assert.Equal(
            1,
            result.Change.Metrics["pregnantFemales"]);

        Assert.Equal(
            0,
            result.Change.Metrics["matingEvents"]);

        Assert.Equal(
            0,
            result.Change.Metrics["conceptions"]);

        Assert.Equal(
            0,
            result.Change.Metrics["births"]);
    }

    [Fact]
    public void Step_GestationCompletionProducesChildAndClearsPregnancy()
    {
        var planet = CreateEarth();

        var father =
            CreateAdult(
                planet.Id,
                PersonSex.Male,
                0,
                0.05);

        var mother =
            CreateAdult(
                planet.Id,
                PersonSex.Female,
                0,
                0)
            .WithPregnancy(
                new PregnancyState(
                    conceptionTimeSeconds: 0,
                    father.Id));

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    mother,
                    father),
                280 * OneDaySeconds,
                CreateSystem(
                    planet.Id,
                    conceptionProbability: 1));

        Assert.Equal(
            3,
            result.World.Population.Length);

        var restoredMother =
            result.World.Population.Single(
                person =>
                    person.Id == mother.Id);

        Assert.Null(
            restoredMother.Pregnancy);

        Assert.Equal(
            PersonActivity.Idle,
            restoredMother.Activity);

        var child =
            result.World.Population.Single(
                person =>
                    person.ParentId ==
                    mother.Id);

        Assert.Equal(
            280 * OneDaySeconds,
            child.BirthTimeSeconds);

        Assert.Equal(
            3.5,
            child.Material.LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            0.0875,
            child.Material.LiveNitrogenKilograms,
            precision: 10);

        Assert.Equal(
            66.5,
            restoredMother.Material.LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            1.6625,
            restoredMother.Material.LiveNitrogenKilograms,
            precision: 10);

        Assert.Equal(
            70,
            restoredMother.Material.LiveBiomassKilograms +
            child.Material.LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            1.75,
            restoredMother.Material.LiveNitrogenKilograms +
            child.Material.LiveNitrogenKilograms,
            precision: 10);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "pregnancyLossesMaterialInsufficient"]);

        Assert.Equal(
            1,
            result.Change.Metrics["births"]);
    }

    [Fact]
    public void Step_DuePregnancyWithoutEnoughMaterialDoesNotCreateChild()
    {
        var planet = CreateEarth();

        var father =
            CreateAdult(
                planet.Id,
                PersonSex.Male,
                0,
                0.05);

        var mother =
            CreateAdult(
                planet.Id,
                PersonSex.Female,
                0,
                0)
            .WithMaterial(
                new OrganismMaterialState(
                    liveBiomassKilograms: 1,
                    liveNitrogenKilograms: 0.02))
            .WithPregnancy(
                new PregnancyState(
                    conceptionTimeSeconds: 0,
                    father.Id));

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    mother,
                    father),
                280 * OneDaySeconds,
                CreateSystem(
                    planet.Id,
                    conceptionProbability: 1));

        Assert.Equal(
            2,
            result.World.Population.Length);

        var restoredMother =
            result.World.Population.Single(
                person =>
                    person.Id == mother.Id);

        Assert.Null(
            restoredMother.Pregnancy);

        Assert.Equal(
            1,
            restoredMother.Material.LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            0.02,
            restoredMother.Material.LiveNitrogenKilograms,
            precision: 10);

        Assert.Equal(
            0,
            result.Change.Metrics["births"]);

        Assert.Equal(
            1,
            result.Change.Metrics[
                "pregnancyLossesMaterialInsufficient"]);
    }

    [Fact]
    public void Step_NoNearbyPartnerPreventsConception()
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
                    conceptionProbability: 1));

        Assert.Equal(
            2,
            result.World.Population.Length);

        Assert.Equal(
            1,
            result.Change.Metrics["noPartnerFound"]);

        Assert.Equal(
            0,
            result.Change.Metrics["conceptions"]);

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
                    conceptionProbability: 1));

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
            result.Change.Metrics["conceptions"]);

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
                    conceptionProbability: 1));

        Assert.Equal(
            2,
            result.World.Population.Length);

        Assert.Equal(
            0,
            result.Change.Metrics["eligibleFemales"]);

        Assert.Equal(
            0,
            result.Change.Metrics["conceptions"]);

        Assert.Equal(
            0,
            result.Change.Metrics["births"]);
    }

    private static ReproductionSystem CreateSystem(
        PlanetId planetId,
        double conceptionProbability)
    {
        return new ReproductionSystem(
            planetId,
            new PopulationModelParameters(
                seed: 42,
                annualBirthRatePerEligibleFemale: 0,
                annualAdultMigrationRate: 0,
                annualBaseMortalityRate: 0,
                annualElderMortalityRate: 0,
                conceptionProbabilityPerMatingOpportunity:
                    conceptionProbability,
                gestationDays: 280));
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
            longitude,
            material:
                new OrganismMaterialState(
                    liveBiomassKilograms: 70,
                    liveNitrogenKilograms: 1.75));
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
