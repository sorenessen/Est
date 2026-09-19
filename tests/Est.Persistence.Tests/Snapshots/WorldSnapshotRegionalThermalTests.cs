using System.Text.Json;
using System.Text.Json.Nodes;
using Est.Persistence.Snapshots;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Thermal;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Persistence.Tests.Snapshots;

public sealed class WorldSnapshotRegionalThermalTests
{
    [Fact]
    public void RoundTrip_PreservesRegionalThermalState()
    {
        var planet =
            CreatePlanet();

        var terrain =
            CreateTerrain(
                planet);

        var thermal =
            CreateThermalState(
                planet,
                terrain.GridDefinition);

        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(
                    123_456),
                [planet],
                [],
                terrain: [terrain],
                regionalThermal: [thermal]);

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

        var restoredThermal =
            Assert.Single(
                restored.RegionalThermal);

        Assert.Equal(
            thermal.PlanetId,
            restoredThermal.PlanetId);

        Assert.Equal(
            thermal.GridDefinition,
            restoredThermal.GridDefinition);

        Assert.True(
            thermal.Cells.SequenceEqual(
                restoredThermal.Cells));

        restoredThermal.ValidateFor(
            Assert.Single(
                restored.Planets));

        Assert.Equal(
            123_456,
            restored.CurrentTime.TotalSeconds);
    }

    [Fact]
    public void Deserialize_VersionTwentyThreeGetsEmptyRegionalThermalState()
    {
        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    CreateWorldWithoutRegionalThermal()))!
            .AsObject();

        node["schemaVersion"] = 23;

        node.Remove(
            "regionalThermal");

        var restored =
            WorldSnapshotSerializer.Deserialize(
                node.ToJsonString());

        Assert.Empty(
            restored.RegionalThermal);
    }

    [Fact]
    public void Deserialize_VersionTwentyFourRequiresRegionalThermalState()
    {
        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    CreateWorldWithoutRegionalThermal()))!
            .AsObject();

        node.Remove(
            "regionalThermal");

        Assert.Throws<JsonException>(
            () =>
                WorldSnapshotSerializer.Deserialize(
                    node.ToJsonString()));
    }

    [Fact]
    public void Deserialize_RegionalThermalRejectsMissingGridDefinition()
    {
        var world =
            CreateWorldWithRegionalThermal();

        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    world))!
            .AsObject();

        node["regionalThermal"]!
            .AsArray()[0]!
            .AsObject()
            .Remove(
                "gridDefinition");

        Assert.Throws<JsonException>(
            () =>
                WorldSnapshotSerializer.Deserialize(
                    node.ToJsonString()));
    }

    [Fact]
    public void Deserialize_RegionalThermalRejectsMissingCells()
    {
        var world =
            CreateWorldWithRegionalThermal();

        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    world))!
            .AsObject();

        node["regionalThermal"]!
            .AsArray()[0]!
            .AsObject()
            .Remove(
                "cells");

        Assert.Throws<JsonException>(
            () =>
                WorldSnapshotSerializer.Deserialize(
                    node.ToJsonString()));
    }

    private static WorldState
        CreateWorldWithoutRegionalThermal()
    {
        var planet =
            CreatePlanet();

        var terrain =
            CreateTerrain(
                planet);

        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [planet],
            [],
            terrain: [terrain]);
    }

    private static WorldState
        CreateWorldWithRegionalThermal()
    {
        var planet =
            CreatePlanet();

        var terrain =
            CreateTerrain(
                planet);

        var thermal =
            CreateThermalState(
                planet,
                terrain.GridDefinition);

        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [planet],
            [],
            terrain: [terrain],
            regionalThermal: [thermal]);
    }

    private static PlanetRegionalThermalState
        CreateThermalState(
            PlanetState planet,
            SurfaceGridDefinition definition)
    {
        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        return new PlanetRegionalThermalState(
            planet.Id,
            definition,
            grid.Cells.Select(
                (cell, index) =>
                    new RegionalThermalCellState(
                        cell.Id,
                        280.0 +
                        index * 0.25,
                        240.0 +
                        index * 0.125)));
    }

    private static PlanetTerrainState CreateTerrain(
        PlanetState planet)
    {
        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        return new PlanetTerrainState(
            planet.Id,
            definition,
            grid.Cells.Select(
                (cell, index) =>
                    new TerrainCellState(
                        cell.Id,
                        index * 100)));
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Regional Thermal Snapshot World",
            5.0e24,
            6_000_000,
            new PlanetEnvironment(
                285,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
