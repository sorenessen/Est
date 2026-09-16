using System.Text.Json;
using System.Text.Json.Nodes;
using Est.Persistence.Snapshots;
using Est.Simulation.Biogeochemistry;
using Est.Simulation.Hydrology;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Persistence.Tests.Snapshots;

public sealed class WorldSnapshotBiogeochemistryTests
{
    [Fact]
    public void RoundTrip_PreservesBiogeochemistry()
    {
        var planet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var terrain =
            CreateTerrain(
                planet,
                definition);

        var hydrology =
            CreateHydrology(
                planet,
                definition);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        var biogeochemistry =
            new PlanetBiogeochemistryState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    (cell, index) =>
                        new BiogeochemistryCellState(
                            cell.Id,
                            1 + index * 0.01,
                            0.03 + index * 0.001,
                            0.01 + index * 0.0005)));

        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(
                    123),
                [planet],
                [],
                terrain:
                [
                    terrain
                ],
                hydrology:
                [
                    hydrology
                ],
                biogeochemistry:
                [
                    biogeochemistry
                ]);

        var json =
            WorldSnapshotSerializer.Serialize(
                world);

        Assert.Contains(
            "\"schemaVersion\": 15",
            json,
            StringComparison.Ordinal);

        var restored =
            WorldSnapshotSerializer.Deserialize(
                json);

        var restoredBiogeochemistry =
            Assert.Single(
                restored.Biogeochemistry);

        Assert.Equal(
            biogeochemistry.PlanetId,
            restoredBiogeochemistry.PlanetId);

        Assert.Equal(
            biogeochemistry.GridDefinition,
            restoredBiogeochemistry.GridDefinition);

        Assert.True(
            biogeochemistry.Cells.SequenceEqual(
                restoredBiogeochemistry.Cells));
    }

    [Fact]
    public void Deserialize_VersionFourteenGetsEmptyBiogeochemistry()
    {
        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    CreateEmptyWorld()))!
                .AsObject();

        node["schemaVersion"] = 14;

        node.Remove(
            "biogeochemistry");

        var restored =
            WorldSnapshotSerializer.Deserialize(
                node.ToJsonString());

        Assert.Empty(
            restored.Biogeochemistry);
    }

    [Fact]
    public void Deserialize_VersionFifteenRequiresBiogeochemistry()
    {
        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    CreateEmptyWorld()))!
                .AsObject();

        node.Remove(
            "biogeochemistry");

        Assert.Throws<JsonException>(
            () =>
                WorldSnapshotSerializer.Deserialize(
                    node.ToJsonString()));
    }

    private static WorldState CreateEmptyWorld()
    {
        var planet =
            CreatePlanet();

        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [planet],
            []);
    }

    private static PlanetTerrainState CreateTerrain(
        PlanetState planet,
        SurfaceGridDefinition definition)
    {
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
                        index)));
    }

    private static PlanetHydrologyState CreateHydrology(
        PlanetState planet,
        SurfaceGridDefinition definition)
    {
        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        return new PlanetHydrologyState(
            planet.Id,
            definition,
            grid.Cells.Select(
                cell =>
                    new HydrologyCellState(
                        cell.Id,
                        1,
                        0,
                        100,
                        0)));
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Biogeochemistry Snapshot World",
            5.0e24,
            6_000_000,
            new PlanetEnvironment(
                288,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
