using System.Text.Json;
using System.Text.Json.Nodes;
using Est.Persistence.Archives;
using Est.Simulation.Climate;
using Est.Simulation.Definitions;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Causality;
using Est.Simulation.Operations;
using Est.Simulation.Time;
using Est.Simulation.Timelines;
using Est.Simulation.Worlds;

namespace Est.Persistence.Tests.Archives;

public class TimelineArchiveSerializerTests
{
    [Fact]
    public void RoundTrip_PreservesTimelineIdentityAndCurrentWorld()
    {
        var timeline = CreateTimelineWithHistory();

        var restored = RoundTrip(timeline);

        Assert.Equal(
            timeline.Id,
            restored.Timeline.Id);

        Assert.Equal(
            timeline.CurrentWorld.Id,
            restored.Timeline.CurrentWorld.Id);

        Assert.Equal(
            timeline.CurrentWorld.CurrentTime,
            restored.Timeline.CurrentWorld.CurrentTime);
    }

    [Fact]
    public void RoundTrip_PreservesCheckpointIdentities()
    {
        var timeline = CreateTimelineWithHistory();

        var restored = RoundTrip(timeline);

        Assert.Equal(
            timeline.Checkpoints.Select(x => x.Id),
            restored.Timeline.Checkpoints.Select(x => x.Id));
    }

    [Fact]
    public void RoundTrip_PreservesEventHistory()
    {
        var timeline = CreateTimelineWithHistory();

        var restored = RoundTrip(timeline);

        var expected =
            Assert.Single(timeline.Events);

        var actual =
            Assert.Single(restored.Timeline.Events);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Cause, actual.Cause);
        Assert.Equal(expected.Summary, actual.Summary);
        Assert.Equal(expected.OccurredAt, actual.OccurredAt);
        Assert.Equal(
            expected.ElapsedSeconds,
            actual.ElapsedSeconds);
    }

    [Fact]
    public void RoundTrip_PreservesParentIdentity()
    {
        var parent = CreateTimelineWithHistory()
            .CreateCheckpoint();

        var child = parent.ForkFromCheckpoint(
            parent.Checkpoints[1].Id);

        var restored = RoundTrip(child);

        Assert.Equal(
            parent.Id,
            restored.Timeline.ParentTimelineId);

        Assert.Equal(
            parent.Checkpoints[1].Id,
            restored.Timeline.ParentCheckpointId);
    }

    [Fact]
    public void RoundTrip_PreservesProvenance()
    {
        var restored = RoundTrip(
            CreateTimelineWithHistory());

        Assert.Equal(
            "Est",
            restored.Provenance.Producer);

        Assert.Equal(
            "0.1.0-alpha",
            restored.Provenance.ProducerVersion);

        Assert.Equal(
            "simulation",
            restored.Provenance.Origin);
    }

    [Fact]
    public void RoundTrip_PreservesSimulationDefinition()
    {
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

        var timeline =
            SimulationTimeline.Create(
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet]));

        var parameters =
            new PlanetaryEnergyBalanceParameters(
                1361,
                0.61,
                1.0e8,
                0.30,
                0.60,
                263.15,
                273.15,
                31_536_000);

        var definition =
            new SimulationDefinition(
            [
                new PlanetaryEnergyBalanceModelDefinition(
                    planet.Id,
                    parameters)
            ]);

        var json =
            TimelineArchiveSerializer.Serialize(
                timeline,
                definition,
                CreateProvenance());

        var restored =
            TimelineArchiveSerializer.Deserialize(json);

        var model =
            Assert.Single(
                restored.Definition
                    .PlanetaryEnergyBalanceModels);

        Assert.Equal(planet.Id, model.PlanetId);
        Assert.Equal(
            parameters,
            model.Parameters);
    }

    [Fact]
    public void RoundTrip_PreservesPopulationModelDefinition()
    {
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

        var timeline =
            SimulationTimeline.Create(
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet]));

        var parameters =
            new PopulationModelParameters(
                seed: 42,
                annualBirthRatePerEligibleFemale: 0.2,
                annualAdultMigrationRate: 0.04,
                annualBaseMortalityRate: 0.005,
                annualElderMortalityRate: 0.09,
                reproductiveAgeMinimumYears: 17,
                reproductiveAgeMaximumYears: 42,
                elderAgeYears: 68,
                localMigrationDegrees: 2.25,
                longMigrationProbability: 0.11,
                longMigrationDegrees: 18,
                conceptionProbabilityPerMatingOpportunity: 0.37,
                gestationDays: 266);

        var definition =
            new SimulationDefinition(
                populationModels:
                [
                    new PopulationModelDefinition(
                        planet.Id,
                        parameters)
                ]);

        var json =
            TimelineArchiveSerializer.Serialize(
                timeline,
                definition,
                CreateProvenance());

        var restored =
            TimelineArchiveSerializer.Deserialize(json);

        var model =
            Assert.Single(
                restored.Definition.PopulationModels);

        Assert.Equal(planet.Id, model.PlanetId);
        Assert.Equal(parameters, model.Parameters);
    }

    [Fact]
    public void Deserialize_Version3ArchiveUsesDefaultReproductiveBehavior()
    {
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

        var timeline =
            SimulationTimeline.Create(
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet]));

        var definition =
            new SimulationDefinition(
                populationModels:
                [
                    new PopulationModelDefinition(
                        planet.Id,
                        new PopulationModelParameters(
                            seed: 42,
                            annualBirthRatePerEligibleFemale: 0.2,
                            annualAdultMigrationRate: 0.04,
                            annualBaseMortalityRate: 0.005,
                            annualElderMortalityRate: 0.09,
                            reproductiveAgeMinimumYears: 17,
                            reproductiveAgeMaximumYears: 42,
                            elderAgeYears: 68,
                            localMigrationDegrees: 2.25,
                            longMigrationProbability: 0.11,
                            longMigrationDegrees: 18,
                            conceptionProbabilityPerMatingOpportunity:
                                0.37,
                            gestationDays: 266))
                ]);

        var json =
            TimelineArchiveSerializer.Serialize(
                timeline,
                definition,
                CreateProvenance());

        var node =
            JsonNode.Parse(json)
            ?? throw new InvalidOperationException(
                "Archive JSON did not parse.");

        node["schemaVersion"] = 3;

        var parameters =
            node["definition"]!
                ["populationModels"]!
                .AsArray()[0]!
                ["parameters"]!
                .AsObject();

        parameters.Remove(
            "conceptionProbabilityPerMatingOpportunity");

        parameters.Remove(
            "gestationDays");

        var restored =
            TimelineArchiveSerializer.Deserialize(
                node.ToJsonString());

        var restoredParameters =
            Assert.Single(
                restored.Definition.PopulationModels)
                .Parameters;

        Assert.Equal(
            0.20,
            restoredParameters
                .ConceptionProbabilityPerMatingOpportunity);

        Assert.Equal(
            280,
            restoredParameters.GestationDays);
    }

    [Fact]
    public void Deserialize_Version2ArchiveUsesEmptyPopulationModels()
    {
        var timeline = CreateTimelineWithHistory();

        var json =
            TimelineArchiveSerializer.Serialize(
                timeline,
                CreateProvenance());

        var node = JsonNode.Parse(json)
            ?? throw new InvalidOperationException(
                "Archive JSON did not parse.");

        node["schemaVersion"] = 2;

        var definition =
            node["definition"]?.AsObject()
            ?? throw new InvalidOperationException(
                "Archive definition was missing.");

        definition.Remove("populationModels");

        var restored =
            TimelineArchiveSerializer.Deserialize(
                node.ToJsonString());

        Assert.Empty(
            restored.Definition.PopulationModels);
    }

    [Fact]
    public void Deserialize_Version1ArchiveUsesEmptySimulationDefinition()
    {
        var json =
            TimelineArchiveSerializer.Serialize(
                CreateTimelineWithHistory(),
                CreateProvenance());

        var node = JsonNode.Parse(json)
            ?? throw new InvalidOperationException(
                "Archive JSON did not parse.");

        node["schemaVersion"] = 1;
        node.AsObject().Remove("definition");

        var restored =
            TimelineArchiveSerializer.Deserialize(
                node.ToJsonString());

        Assert.Empty(
            restored.Definition
                .PlanetaryEnergyBalanceModels);
    }

    [Fact]
    public void Deserialize_RejectsUnsupportedSchemaVersion()
    {
        var timeline = CreateTimelineWithHistory();

        var json = TimelineArchiveSerializer.Serialize(
            timeline,
            CreateProvenance());

        using var document = JsonDocument.Parse(json);

        var node = JsonNode.Parse(json)
            ?? throw new InvalidOperationException(
                "Archive JSON did not parse.");

        node["schemaVersion"] = 999;

        Assert.Throws<NotSupportedException>(
            () => TimelineArchiveSerializer.Deserialize(
                node.ToJsonString()));
    }

    [Fact]
    public void Deserialize_RejectsCorruptCheckpointWorld()
    {
        var timeline = CreateTimelineWithHistory();

        var json = TimelineArchiveSerializer.Serialize(
            timeline,
            CreateProvenance());

        var node = JsonNode.Parse(json)
            ?? throw new InvalidOperationException(
                "Archive JSON did not parse.");

        var checkpoints =
            node["checkpoints"]?.AsArray()
            ?? throw new InvalidOperationException(
                "Archive checkpoints were missing.");

        var checkpointWorld =
            checkpoints[0]?["world"]
            ?? throw new InvalidOperationException(
                "Checkpoint world was missing.");

        checkpointWorld["worldId"] =
            WorldId.New().Value;

        Assert.Throws<ArgumentException>(
            () => TimelineArchiveSerializer.Deserialize(
                node.ToJsonString()));
    }

    [Fact]
    public void Deserialize_RejectsMissingProvenance()
    {
        var timeline = CreateTimelineWithHistory();

        var json = TimelineArchiveSerializer.Serialize(
            timeline,
            CreateProvenance());

        var node = JsonNode.Parse(json)
            ?? throw new InvalidOperationException(
                "Archive JSON did not parse.");

        node["provenance"] = null;

        Assert.Throws<JsonException>(
            () => TimelineArchiveSerializer.Deserialize(
                node.ToJsonString()));
    }

    [Fact]
    public void Deserialize_RejectsMissingEventsCollection()
    {
        var timeline = CreateTimelineWithHistory();

        var json = TimelineArchiveSerializer.Serialize(
            timeline,
            CreateProvenance());

        var node = JsonNode.Parse(json)
            ?? throw new InvalidOperationException(
                "Archive JSON did not parse.");

        node["events"] = null;

        Assert.Throws<JsonException>(
            () => TimelineArchiveSerializer.Deserialize(
                node.ToJsonString()));
    }

    [Fact]
    public void Deserialize_RejectsDuplicateArchiveProperties()
    {
        var timeline = CreateTimelineWithHistory();

        var json = TimelineArchiveSerializer.Serialize(
            timeline,
            CreateProvenance());

        var modified = json.Replace(
            "\"timelineId\":",
            "\"timelineId\":\"00000000-0000-0000-0000-000000000001\",\\n  \"timelineId\":",
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(
            () => TimelineArchiveSerializer.Deserialize(
                modified));
    }

    [Fact]
    public void Deserialize_RejectsUnknownArchiveProperties()
    {
        var timeline = CreateTimelineWithHistory();

        var json = TimelineArchiveSerializer.Serialize(
            timeline,
            CreateProvenance());

        var node = JsonNode.Parse(json)
            ?? throw new InvalidOperationException(
                "Archive JSON did not parse.");

        node["unexpected"] = true;

        Assert.Throws<JsonException>(
            () => TimelineArchiveSerializer.Deserialize(
                node.ToJsonString()));
    }

    [Fact]
    public void Deserialize_RejectsEventOutsideTimelineRange()
    {
        var timeline = CreateTimelineWithHistory();

        var json = TimelineArchiveSerializer.Serialize(
            timeline,
            CreateProvenance());

        var node = JsonNode.Parse(json)
            ?? throw new InvalidOperationException(
                "Archive JSON did not parse.");

        var events =
            node["events"]?.AsArray()
            ?? throw new InvalidOperationException(
                "Archive events were missing.");

        events[0]!["occurredAtSeconds"] = 999_999L;

        Assert.Throws<ArgumentException>(
            () => TimelineArchiveSerializer.Deserialize(
                node.ToJsonString()));
    }

    private static TimelineArchive RoundTrip(
        SimulationTimeline timeline)
    {
        var json =
            TimelineArchiveSerializer.Serialize(
                timeline,
                CreateProvenance());

        return TimelineArchiveSerializer.Deserialize(
            json);
    }

    private static TimelineArchiveProvenance
        CreateProvenance()
    {
        return new TimelineArchiveProvenance(
            "Est",
            "0.1.0-alpha",
            "simulation");
    }

    private static SimulationTimeline
        CreateTimelineWithHistory()
    {
        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(100));

        var timeline =
            SimulationTimeline.Create(world);

        var result =
            new SimulationStepResult(
                world.AdvanceBy(60),
                new SimulationChange(
                    new AdvanceTimeOperation(0),
                    "test-step",
                    "Test timeline event.",
                    null,
                    60,
                    new Dictionary<string, double>
                    {
                        ["value"] = 42
                    }));

        return timeline
            .RecordStep(result)
            .CreateCheckpoint();
    }
}
