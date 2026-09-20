using System.Text.Json;
using System.Text.Json.Nodes;
using Est.Persistence.Snapshots;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Social;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Persistence.Tests.Snapshots;

public sealed class WorldSnapshotSocialRecognitionTests
{
    [Fact]
    public void RoundTrip_PreservesPersonSocialRecognition()
    {
        var actor =
            SocialActorIdentity.ForEster(
                EsterId.New());

        var socialState =
            new PersonSocialState()
                .RecordEncounter(
                    actor,
                    encounterTimeSeconds: 100)
                .RecordEncounter(
                    actor,
                    encounterTimeSeconds: 360);

        var person =
            CreatePerson(
                socialState);

        var world =
            CreateWorld(
                person,
                currentTimeSeconds: 500);

        var json =
            WorldSnapshotSerializer.Serialize(
                world);

        Assert.Contains(
            $"\"schemaVersion\": {WorldSnapshotSerializer.CurrentSchemaVersion}",
            json,
            StringComparison.Ordinal);

        var restored =
            WorldSnapshotSerializer.Deserialize(
                json);

        var restoredPerson =
            Assert.Single(
                restored.Population);

        Assert.Equal(
            socialState,
            restoredPerson.SocialState);

        var contact =
            restoredPerson.SocialState
                .GetContact(actor);

        Assert.NotNull(contact);
        Assert.Equal(
            100,
            contact.FirstEncounterTimeSeconds);
        Assert.Equal(
            360,
            contact.LastEncounterTimeSeconds);
        Assert.Equal(
            2,
            contact.EncounterCount);
    }

    [Fact]
    public void Deserialize_VersionTwentyFourDefaultsSocialStateToEmpty()
    {
        var actor =
            SocialActorIdentity.ForEster(
                EsterId.New());

        var person =
            CreatePerson(
                new PersonSocialState()
                    .RecordEncounter(
                        actor,
                        encounterTimeSeconds: 100));

        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    CreateWorld(
                        person,
                        currentTimeSeconds: 500)))!
            .AsObject();

        node["schemaVersion"] = 24;

        node["population"]!
            .AsArray()[0]!
            .AsObject()
            .Remove(
                "socialContacts");

        var restored =
            WorldSnapshotSerializer.Deserialize(
                node.ToJsonString());

        Assert.Empty(
            Assert.Single(
                    restored.Population)
                .SocialState
                .Contacts);
    }

    [Fact]
    public void Deserialize_VersionTwentyFiveRequiresSocialContacts()
    {
        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    CreateWorld(
                        CreatePerson(),
                        currentTimeSeconds: 500)))!
            .AsObject();

        node["population"]!
            .AsArray()[0]!
            .AsObject()
            .Remove(
                "socialContacts");

        Assert.Throws<JsonException>(
            () =>
                WorldSnapshotSerializer.Deserialize(
                    node.ToJsonString()));
    }

    [Fact]
    public void Deserialize_RejectsInvalidSocialActorIdentity()
    {
        var node =
            CreateSnapshotWithContact();

        var contact =
            node["population"]!
                .AsArray()[0]!
                .AsObject()["socialContacts"]!
                .AsArray()[0]!
                .AsObject();

        contact["actorId"] =
            Guid.Empty.ToString();

        Assert.Throws<JsonException>(
            () =>
                WorldSnapshotSerializer.Deserialize(
                    node.ToJsonString()));
    }

    [Fact]
    public void Deserialize_RejectsDuplicateSocialActors()
    {
        var node =
            CreateSnapshotWithContact();

        var contacts =
            node["population"]!
                .AsArray()[0]!
                .AsObject()["socialContacts"]!
                .AsArray();

        contacts.Add(
            JsonNode.Parse(
                contacts[0]!
                    .ToJsonString()));

        Assert.Throws<JsonException>(
            () =>
                WorldSnapshotSerializer.Deserialize(
                    node.ToJsonString()));
    }

    [Fact]
    public void Deserialize_RejectsNullSocialContact()
    {
        var node =
            CreateSnapshotWithContact();

        var contacts =
            node["population"]!
                .AsArray()[0]!
                .AsObject()["socialContacts"]!
                .AsArray();

        contacts.Add(null);

        Assert.Throws<JsonException>(
            () =>
                WorldSnapshotSerializer.Deserialize(
                    node.ToJsonString()));
    }

    [Fact]
    public void Deserialize_RejectsInvalidSocialActorKind()
    {
        var node =
            CreateSnapshotWithContact();

        var contact =
            node["population"]!
                .AsArray()[0]!
                .AsObject()["socialContacts"]!
                .AsArray()[0]!
                .AsObject();

        contact["actorKind"] = 999;

        Assert.Throws<JsonException>(
            () =>
                WorldSnapshotSerializer.Deserialize(
                    node.ToJsonString()));
    }

    [Fact]
    public void Deserialize_RejectsSelfSocialContact()
    {
        var node =
            CreateSnapshotWithContact();

        var person =
            node["population"]!
                .AsArray()[0]!
                .AsObject();

        var contact =
            person["socialContacts"]!
                .AsArray()[0]!
                .AsObject();

        contact["actorKind"] =
            (int)SocialActorKind.Person;

        contact["actorId"] =
            person["personId"]!
                .GetValue<string>();

        Assert.Throws<JsonException>(
            () =>
                WorldSnapshotSerializer.Deserialize(
                    node.ToJsonString()));
    }

    [Fact]
    public void Deserialize_RejectsInvalidSocialChronology()
    {
        var node =
            CreateSnapshotWithContact();

        var contact =
            node["population"]!
                .AsArray()[0]!
                .AsObject()["socialContacts"]!
                .AsArray()[0]!
                .AsObject();

        contact["firstEncounterTimeSeconds"] =
            500;

        contact["lastEncounterTimeSeconds"] =
            100;

        Assert.Throws<JsonException>(
            () =>
                WorldSnapshotSerializer.Deserialize(
                    node.ToJsonString()));
    }

    [Fact]
    public void Deserialize_RejectsInvalidEncounterCount()
    {
        var node =
            CreateSnapshotWithContact();

        var contact =
            node["population"]!
                .AsArray()[0]!
                .AsObject()["socialContacts"]!
                .AsArray()[0]!
                .AsObject();

        contact["encounterCount"] = 0;

        Assert.Throws<JsonException>(
            () =>
                WorldSnapshotSerializer.Deserialize(
                    node.ToJsonString()));
    }

    private static JsonObject CreateSnapshotWithContact()
    {
        var actor =
            SocialActorIdentity.ForEster(
                EsterId.New());

        var person =
            CreatePerson(
                new PersonSocialState()
                    .RecordEncounter(
                        actor,
                        encounterTimeSeconds: 100));

        return JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    CreateWorld(
                        person,
                        currentTimeSeconds: 500)))!
            .AsObject();
    }

    private static PersonState CreatePerson(
        PersonSocialState? socialState = null)
    {
        return new PersonState(
            PersonId.New(),
            PlanetId.New(),
            PersonSex.Female,
            birthTimeSeconds: 0,
            latitudeDegrees: 10,
            longitudeDegrees: 20,
            socialState: socialState);
    }

    private static WorldState CreateWorld(
        PersonState person,
        long currentTimeSeconds)
    {
        var planet =
            new PlanetState(
                person.PlanetId,
                "Social World",
                5.9722e24,
                6_371_000,
                new PlanetEnvironment(
                    288.15,
                    0.71,
                    0.03,
                    AtmosphereState.Vacuum));

        return new WorldState(
            WorldId.New(),
            new SimulationTime(
                currentTimeSeconds),
            [planet],
            [person]);
    }
}
