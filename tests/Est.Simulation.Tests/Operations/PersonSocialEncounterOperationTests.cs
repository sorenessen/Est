using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Social;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Operations;

public sealed class PersonSocialEncounterOperationTests
{
    [Fact]
    public void Apply_FirstEncounterCreatesRecognitionAtWorldTime()
    {
        var person = CreatePerson();
        var actor =
            SocialActorIdentity.ForEster(
                EsterId.New());

        var world =
            CreateWorld(
                currentTimeSeconds: 120,
                [person]);

        Assert.False(
            person.SocialState.HasEncountered(
                actor));

        var changed =
            new RecordPersonSocialEncounterOperation(
                person.Id,
                actor)
            .Apply(world);

        var changedPerson =
            Assert.Single(
                changed.Population);

        var contact =
            changedPerson.SocialState.GetContact(
                actor);

        Assert.NotNull(contact);
        Assert.True(
            changedPerson.SocialState.HasEncountered(
                actor));
        Assert.Equal(
            1,
            contact.EncounterCount);
        Assert.Equal(
            120,
            contact.FirstEncounterTimeSeconds);
        Assert.Equal(
            120,
            contact.LastEncounterTimeSeconds);

        Assert.False(
            person.SocialState.HasEncountered(
                actor));
        Assert.Equal(
            world.Id,
            changed.Id);
        Assert.Equal(
            world.CurrentTime,
            changed.CurrentTime);
    }

    [Fact]
    public void Apply_RepeatedEncounterUpdatesExistingContact()
    {
        var actor =
            SocialActorIdentity.ForEster(
                EsterId.New());

        var person =
            CreatePerson()
                .WithSocialState(
                    new PersonSocialState()
                        .RecordEncounter(
                            actor,
                            encounterTimeSeconds: 100));

        var world =
            CreateWorld(
                currentTimeSeconds: 360,
                [person]);

        var changed =
            new RecordPersonSocialEncounterOperation(
                person.Id,
                actor)
            .Apply(world);

        var contact =
            Assert.Single(
                    changed.Population)
                .SocialState
                .GetContact(actor);

        Assert.NotNull(contact);
        Assert.Single(
            changed.Population[0]
                .SocialState
                .Contacts);
        Assert.Equal(
            2,
            contact.EncounterCount);
        Assert.Equal(
            100,
            contact.FirstEncounterTimeSeconds);
        Assert.Equal(
            360,
            contact.LastEncounterTimeSeconds);
    }

    [Fact]
    public void Apply_RecognitionRemainsActorAndPersonSpecific()
    {
        var firstPerson = CreatePerson();
        var secondPerson = CreatePerson();

        var recognizedActor =
            SocialActorIdentity.ForEster(
                EsterId.New());

        var unknownActor =
            SocialActorIdentity.ForEster(
                EsterId.New());

        var world =
            CreateWorld(
                currentTimeSeconds: 240,
                [firstPerson, secondPerson]);

        var changed =
            new RecordPersonSocialEncounterOperation(
                firstPerson.Id,
                recognizedActor)
            .Apply(world);

        var changedFirst =
            changed.Population.Single(
                person =>
                    person.Id == firstPerson.Id);

        var changedSecond =
            changed.Population.Single(
                person =>
                    person.Id == secondPerson.Id);

        Assert.True(
            changedFirst.SocialState.HasEncountered(
                recognizedActor));
        Assert.False(
            changedFirst.SocialState.HasEncountered(
                unknownActor));
        Assert.False(
            changedSecond.SocialState.HasEncountered(
                recognizedActor));
    }

    [Fact]
    public void Apply_RejectsUnknownPerson()
    {
        var world =
            CreateWorld(
                currentTimeSeconds: 0,
                []);

        var operation =
            new RecordPersonSocialEncounterOperation(
                PersonId.New(),
                SocialActorIdentity.ForEster(
                    EsterId.New()));

        Assert.Throws<InvalidOperationException>(
            () => operation.Apply(world));
    }

    [Fact]
    public void Constructor_RejectsDefaultActorIdentity()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new RecordPersonSocialEncounterOperation(
                    PersonId.New(),
                    default));
    }

    [Fact]
    public void Constructor_RejectsSelfEncounter()
    {
        var personId = PersonId.New();

        Assert.Throws<ArgumentException>(
            () =>
                new RecordPersonSocialEncounterOperation(
                    personId,
                    SocialActorIdentity.ForPerson(
                        personId)));
    }

    private static PersonState CreatePerson()
    {
        return new PersonState(
            PersonId.New(),
            PlanetId.New(),
            PersonSex.Female,
            birthTimeSeconds: 0,
            latitudeDegrees: 0,
            longitudeDegrees: 0);
    }

    private static WorldState CreateWorld(
        long currentTimeSeconds,
        IEnumerable<PersonState> population)
    {
        var people = population.ToArray();

        var planetIds =
            people
                .Select(person => person.PlanetId)
                .Distinct()
                .ToArray();

        var planets =
            planetIds
                .Select(
                    planetId =>
                        new PlanetState(
                            planetId,
                            "Test",
                            5.9722e24,
                            6_371_000,
                            new PlanetEnvironment(
                                288.15,
                                0.71,
                                0.03,
                                AtmosphereState.Vacuum)))
                .ToArray();

        return new WorldState(
            WorldId.New(),
            new SimulationTime(
                currentTimeSeconds),
            planets,
            people);
    }
}
