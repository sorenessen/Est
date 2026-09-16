using System.Text.Json;
using System.Text.Json.Nodes;
using Est.Persistence.Snapshots;
using Est.Simulation.Grazers;
using Est.Simulation.Planets;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Persistence.Tests.Snapshots;

public sealed class WorldSnapshotGrazerCohortTests
{
    [Fact]
    public void RoundTrip_PreservesGrazerCohorts()
    {
        var planet =
            CreatePlanet();

        var cohort =
            new GrazerCohortState(
                GrazerCohortId.New(),
                planet.Id,
                275,
                41.25,
                -72.75);

        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(
                    123),
                [planet],
                [],
                grazerCohorts:
                [
                    cohort
                ]);

        var json =
            WorldSnapshotSerializer.Serialize(
                world);

        Assert.Contains(
            "\"schemaVersion\": 16",
            json,
            StringComparison.Ordinal);

        var restored =
            WorldSnapshotSerializer.Deserialize(
                json);

        Assert.Equal(
            cohort,
            Assert.Single(
                restored.GrazerCohorts));
    }

    [Fact]
    public void Deserialize_VersionThirteenGetsEmptyGrazerCohorts()
    {
        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    CreateWorld()))!
                .AsObject();

        node["schemaVersion"] = 13;

        node.Remove(
            "grazerCohorts");

        var restored =
            WorldSnapshotSerializer.Deserialize(
                node.ToJsonString());

        Assert.Empty(
            restored.GrazerCohorts);
    }

    [Fact]
    public void Deserialize_VersionFourteenRequiresGrazerCohorts()
    {
        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    CreateWorld()))!
                .AsObject();

        node["schemaVersion"] = 14;

        node.Remove(
            "grazerCohorts");

        Assert.Throws<JsonException>(
            () =>
                WorldSnapshotSerializer.Deserialize(
                    node.ToJsonString()));
    }

    private static WorldState CreateWorld()
    {
        var planet =
            CreatePlanet();

        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [planet],
            []);
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Grazer Snapshot World",
            5.0e24,
            6_000_000,
            new PlanetEnvironment(
                288,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
