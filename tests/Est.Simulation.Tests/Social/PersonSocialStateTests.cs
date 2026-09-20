using Est.Simulation.Social;

namespace Est.Simulation.Tests.Social;

public sealed class PersonSocialStateTests
{
    [Fact]
    public void Constructor_DefaultsToNoKnownContacts()
    {
        var social = new PersonSocialState();
        var ester =
            SocialActorIdentity.ForEster(
                EsterId.New());

        Assert.Empty(social.Contacts);
        Assert.False(
            social.HasEncountered(ester));
        Assert.Null(
            social.GetContact(ester));
    }

    [Fact]
    public void RecordEncounter_CreatesFirstContact()
    {
        var ester =
            SocialActorIdentity.ForEster(
                EsterId.New());

        var social =
            new PersonSocialState()
                .RecordEncounter(
                    ester,
                    encounterTimeSeconds: 120);

        var contact =
            social.GetContact(ester);

        Assert.NotNull(contact);

        Assert.True(
            social.HasEncountered(ester));
        Assert.Equal(
            120,
            contact.FirstEncounterTimeSeconds);
        Assert.Equal(
            120,
            contact.LastEncounterTimeSeconds);
        Assert.Equal(
            1,
            contact.EncounterCount);
    }

    [Fact]
    public void RecordEncounter_UpdatesExistingContact()
    {
        var ester =
            SocialActorIdentity.ForEster(
                EsterId.New());

        var social =
            new PersonSocialState()
                .RecordEncounter(
                    ester,
                    encounterTimeSeconds: 120)
                .RecordEncounter(
                    ester,
                    encounterTimeSeconds: 360);

        var contact =
            social.GetContact(ester);

        Assert.NotNull(contact);

        Assert.Single(social.Contacts);
        Assert.Equal(
            120,
            contact.FirstEncounterTimeSeconds);
        Assert.Equal(
            360,
            contact.LastEncounterTimeSeconds);
        Assert.Equal(
            2,
            contact.EncounterCount);
    }

    [Fact]
    public void Recognition_IsActorSpecific()
    {
        var first =
            SocialActorIdentity.ForEster(
                EsterId.New());

        var second =
            SocialActorIdentity.ForEster(
                EsterId.New());

        var social =
            new PersonSocialState()
                .RecordEncounter(
                    first,
                    encounterTimeSeconds: 120);

        Assert.True(
            social.HasEncountered(first));
        Assert.False(
            social.HasEncountered(second));
    }

    [Fact]
    public void Constructor_RejectsDuplicateActorContacts()
    {
        var ester =
            SocialActorIdentity.ForEster(
                EsterId.New());

        var first =
            new PersonSocialContactState(
                ester,
                firstEncounterTimeSeconds: 10,
                lastEncounterTimeSeconds: 10);

        var second =
            new PersonSocialContactState(
                ester,
                firstEncounterTimeSeconds: 10,
                lastEncounterTimeSeconds: 20,
                encounterCount: 2);

        Assert.Throws<ArgumentException>(
            () =>
                new PersonSocialState(
                    [first, second]));
    }

    [Fact]
    public void Contact_RejectsDefaultActorIdentity()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new PersonSocialContactState(
                    default,
                    firstEncounterTimeSeconds: 10,
                    lastEncounterTimeSeconds: 10));
    }

    [Fact]
    public void Contact_RejectsInvalidChronology()
    {
        var ester =
            SocialActorIdentity.ForEster(
                EsterId.New());

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new PersonSocialContactState(
                    ester,
                    firstEncounterTimeSeconds: 20,
                    lastEncounterTimeSeconds: 10));
    }

    [Fact]
    public void Contact_RejectsEncounterCountBelowOne()
    {
        var ester =
            SocialActorIdentity.ForEster(
                EsterId.New());

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new PersonSocialContactState(
                    ester,
                    firstEncounterTimeSeconds: 10,
                    lastEncounterTimeSeconds: 10,
                    encounterCount: 0));
    }

    [Fact]
    public void RecordEncounter_RejectsTimeBeforeLastEncounter()
    {
        var ester =
            SocialActorIdentity.ForEster(
                EsterId.New());

        var social =
            new PersonSocialState()
                .RecordEncounter(
                    ester,
                    encounterTimeSeconds: 100);

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                social.RecordEncounter(
                    ester,
                    encounterTimeSeconds: 99));
    }

    [Fact]
    public void EqualSocialStates_HaveValueEquality()
    {
        var esterId = EsterId.New();
        var actor =
            SocialActorIdentity.ForEster(
                esterId);

        var first =
            new PersonSocialState(
                [
                    new PersonSocialContactState(
                        actor,
                        firstEncounterTimeSeconds: 10,
                        lastEncounterTimeSeconds: 20,
                        encounterCount: 2)
                ]);

        var second =
            new PersonSocialState(
                [
                    new PersonSocialContactState(
                        actor,
                        firstEncounterTimeSeconds: 10,
                        lastEncounterTimeSeconds: 20,
                        encounterCount: 2)
                ]);

        Assert.Equal(first, second);
        Assert.Equal(
            first.GetHashCode(),
            second.GetHashCode());
    }
}
