using Est.Simulation.Organisms;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Social;

namespace Est.Simulation.Tests.Population;

public sealed class PersonStateTests
{
    [Fact]
    public void Constructor_DefaultsToHealthyIdleState()
    {
        var person = CreatePerson();

        Assert.Equal(
            1,
            person.Needs.EnergyReserve);

        Assert.Equal(
            1,
            person.Needs.Health);

        Assert.Equal(
            PersonActivity.Idle,
            person.Activity);
    }

    [Fact]
    public void MoveTo_PreservesNeedsAndActivity()
    {
        var needs =
            new PersonNeedsState(
                energyReserve: 0.35,
                health: 0.8);

        var person =
            new PersonState(
                PersonId.New(),
                PlanetId.New(),
                PersonSex.Male,
                0,
                10,
                20,
                needs: needs,
                activity: PersonActivity.Foraging);

        var moved = person.MoveTo(11, 21);

        Assert.Equal(11, moved.LatitudeDegrees);
        Assert.Equal(21, moved.LongitudeDegrees);
        Assert.Equal(needs, moved.Needs);
        Assert.Equal(
            PersonActivity.Foraging,
            moved.Activity);
    }

    [Theory]
    [InlineData(-0.01, 1)]
    [InlineData(1.01, 1)]
    [InlineData(1, -0.01)]
    [InlineData(1, 1.01)]
    public void Needs_RejectsValuesOutsideNormalizedRange(
        double energyReserve,
        double health)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new PersonNeedsState(
                    energyReserve,
                    health));
    }

    [Fact]
    public void Needs_ConsumeEnergyAsTimePasses()
    {
        var needs = new PersonNeedsState();

        var advanced =
            needs.AdvanceWithoutFood(15);

        Assert.Equal(
            0.5,
            advanced.EnergyReserve,
            precision: 10);

        Assert.Equal(
            1,
            advanced.Health,
            precision: 10);
    }

    [Fact]
    public void Needs_LoseHealthAfterEnergyIsExhausted()
    {
        var needs = new PersonNeedsState();

        var advanced =
            needs.AdvanceWithoutFood(37);

        Assert.Equal(
            0,
            advanced.EnergyReserve,
            precision: 10);

        Assert.Equal(
            0.5,
            advanced.Health,
            precision: 10);
    }

    [Fact]
    public void Needs_EventuallyReachZeroHealthWithoutFood()
    {
        var needs = new PersonNeedsState();

        var advanced =
            needs.AdvanceWithoutFood(60);

        Assert.Equal(0, advanced.EnergyReserve);
        Assert.Equal(0, advanced.Health);
    }

    [Fact]
    public void Constructor_DefaultsToEmptySocialState()
    {
        var person = CreatePerson();
        var ester =
            SocialActorIdentity.ForEster(
                EsterId.New());

        Assert.Empty(person.SocialState.Contacts);
        Assert.False(
            person.SocialState.HasEncountered(
                ester));
    }

    [Fact]
    public void Constructor_RejectsSelfAsSocialContact()
    {
        var personId = PersonId.New();

        var socialState =
            new PersonSocialState()
                .RecordEncounter(
                    SocialActorIdentity.ForPerson(
                        personId),
                    encounterTimeSeconds: 10);

        Assert.Throws<ArgumentException>(
            () =>
                new PersonState(
                    personId,
                    PlanetId.New(),
                    PersonSex.Male,
                    birthTimeSeconds: 0,
                    latitudeDegrees: 0,
                    longitudeDegrees: 0,
                    socialState: socialState));
    }

    [Fact]
    public void StateTransitions_PreserveSocialState()
    {
        var socialState =
            new PersonSocialState()
                .RecordEncounter(
                    SocialActorIdentity.ForEster(
                        EsterId.New()),
                    encounterTimeSeconds: 100);

        var person =
            new PersonState(
                PersonId.New(),
                PlanetId.New(),
                PersonSex.Female,
                birthTimeSeconds: 0,
                latitudeDegrees: 10,
                longitudeDegrees: 20,
                socialState: socialState);

        var survival =
            person.WithSurvivalState(
                new PersonNeedsState(
                    energyReserve: 0.4,
                    health: 0.9),
                PersonActivity.Foraging);

        var moved =
            person.MoveTo(
                latitudeDegrees: 11,
                longitudeDegrees: 21);

        var pregnant =
            person.WithPregnancy(
                new PregnancyState(
                    conceptionTimeSeconds: 200,
                    fatherId: PersonId.New()));

        var withoutPregnancy =
            pregnant.WithoutPregnancy();

        var material =
            person.WithMaterial(
                new OrganismMaterialState(
                    liveBiomassKilograms: 70,
                    liveNitrogenKilograms: 1.75));

        Assert.Equal(
            socialState,
            survival.SocialState);
        Assert.Equal(
            socialState,
            moved.SocialState);
        Assert.Equal(
            socialState,
            pregnant.SocialState);
        Assert.Equal(
            socialState,
            withoutPregnancy.SocialState);
        Assert.Equal(
            socialState,
            material.SocialState);
    }

    private static PersonState CreatePerson()
    {
        return new PersonState(
            PersonId.New(),
            PlanetId.New(),
            PersonSex.Female,
            0,
            0,
            0);
    }
}
