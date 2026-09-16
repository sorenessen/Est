using System.Text.Json.Nodes;
using Est.Persistence.Snapshots;
using Est.Simulation.Animals;
using Est.Simulation.Birds;
using Est.Simulation.Grazers;
using Est.Simulation.Organisms;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Persistence.Tests.Snapshots;

public sealed class WorldSnapshotOrganismMaterialTests
{
    [Fact]
    public void RoundTrip_PreservesOrganismMaterialAcrossRepresentations()
    {
        var world =
            CreateWorld();

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
            world.Population[0].Material,
            restored.Population[0].Material);

        Assert.Equal(
            world.Animals[0].Material,
            restored.Animals[0].Material);

        Assert.Equal(
            world.BirdFlocks[0].Material,
            restored.BirdFlocks[0].Material);

        Assert.Equal(
            world.GrazerCohorts[0].Material,
            restored.GrazerCohorts[0].Material);
    }

    [Fact]
    public void Deserialize_VersionFifteenWithoutMaterialRestoresEmptyMaterial()
    {
        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    CreateWorld()))!
                .AsObject();

        node["schemaVersion"] =
            15;

        RemoveMaterial(
            node,
            "population");

        RemoveMaterial(
            node,
            "animals");

        RemoveMaterial(
            node,
            "birdFlocks");

        RemoveMaterial(
            node,
            "grazerCohorts");

        var restored =
            WorldSnapshotSerializer.Deserialize(
                node.ToJsonString());

        Assert.True(
            restored.Population[0].Material.IsEmpty);

        Assert.True(
            restored.Animals[0].Material.IsEmpty);

        Assert.True(
            restored.BirdFlocks[0].Material.IsEmpty);

        Assert.True(
            restored.GrazerCohorts[0].Material.IsEmpty);
    }

    [Fact]
    public void Deserialize_VersionSixteenRequiresMaterial()
    {
        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    CreateWorld()))!
                .AsObject();

        RemoveMaterial(
            node,
            "population");

        Assert.Throws<System.Text.Json.JsonException>(
            () =>
                WorldSnapshotSerializer.Deserialize(
                    node.ToJsonString()));
    }

    private static void RemoveMaterial(
        JsonObject world,
        string collectionName)
    {
        var collection =
            world[collectionName]!
                .AsArray();

        foreach (var item in collection)
        {
            item!
                .AsObject()
                .Remove(
                    "material");
        }
    }

    private static WorldState CreateWorld()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Material World",
                5.0e24,
                6_000_000,
                new PlanetEnvironment(
                    288,
                    0,
                    0,
                    AtmosphereState.Vacuum));

        var person =
            new PersonState(
                PersonId.New(),
                planet.Id,
                PersonSex.Female,
                birthTimeSeconds: -600_000_000,
                latitudeDegrees: 10,
                longitudeDegrees: 20,
                material:
                    new OrganismMaterialState(
                        liveBiomassKilograms: 82,
                        liveNitrogenKilograms: 2.05));

        var wolf =
            new AnimalState(
                AnimalId.New(),
                planet.Id,
                AnimalSpecies.Wolf,
                latitudeDegrees: 11,
                longitudeDegrees: 21,
                material:
                    new OrganismMaterialState(
                        liveBiomassKilograms: 45,
                        liveNitrogenKilograms: 1.125));

        var flock =
            new BirdFlockState(
                BirdFlockId.New(),
                planet.Id,
                memberCount: 25,
                latitudeDegrees: 12,
                longitudeDegrees: 22,
                material:
                    new OrganismMaterialState(
                        liveBiomassKilograms: 20,
                        liveNitrogenKilograms: 0.5));

        var cohort =
            new GrazerCohortState(
                GrazerCohortId.New(),
                planet.Id,
                memberCount: 20,
                latitudeDegrees: 13,
                longitudeDegrees: 23,
                material:
                    new OrganismMaterialState(
                        liveBiomassKilograms: 6_000,
                        liveNitrogenKilograms: 150));

        return new WorldState(
            WorldId.New(),
            new SimulationTime(
                123),
            [planet],
            [person],
            [wolf],
            birdFlocks:
            [
                flock
            ],
            grazerCohorts:
            [
                cohort
            ]);
    }
}
