using System.Text.Json;
using System.Text.Json.Nodes;
using Est.Persistence.Snapshots;
using Est.Simulation.Birds;
using Est.Simulation.Planets;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Persistence.Tests.Snapshots;

public sealed class WorldSnapshotBirdFlockTests
{
    [Fact]
    public void RoundTrip_PreservesBirdFlocks()
    {
        var planet =
            CreatePlanet();

        var flock =
            new BirdFlockState(
                BirdFlockId.New(),
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
                birdFlocks:
                [
                    flock
                ]);

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

        Assert.Equal(
            flock,
            Assert.Single(
                restored.BirdFlocks));
    }

    [Fact]
    public void Deserialize_VersionTwelveGetsEmptyBirdFlocks()
    {
        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    CreateWorld()))!
                .AsObject();

        node["schemaVersion"] = 12;

        node.Remove(
            "birdFlocks");

        var restored =
            WorldSnapshotSerializer.Deserialize(
                node.ToJsonString());

        Assert.Empty(
            restored.BirdFlocks);
    }

    [Fact]
    public void Deserialize_VersionThirteenRequiresBirdFlocks()
    {
        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    CreateWorld()))!
                .AsObject();

        node.Remove(
            "birdFlocks");

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
            "Bird Snapshot World",
            5.0e24,
            6_000_000,
            new PlanetEnvironment(
                288,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
