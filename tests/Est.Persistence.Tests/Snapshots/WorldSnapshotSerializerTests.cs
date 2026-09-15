using System.Text.Json;
using System.Text.Json.Nodes;
using Est.Persistence.Snapshots;
using Est.Simulation.Animals;
using Est.Simulation.Ecology;
using Est.Simulation.Hydrology;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Persistence.Tests.Snapshots;

public class WorldSnapshotSerializerTests
{
    [Fact]
    public void RoundTrip_PreservesWorldAndPlanetState()
    {
        var worldId = WorldId.New();
        var planetId = PlanetId.New();

        var atmosphere = new AtmosphereState(
            101_325,
            new Dictionary<string, double>
            {
                ["N2"] = 0.78,
                ["O2"] = 0.21,
                ["Ar"] = 0.01
            });

        var planet = new PlanetState(
            planetId,
            "Earth",
            5.9722e24,
            6_371_000,
            new PlanetEnvironment(
                288.15,
                0.71,
                0.03,
                atmosphere));

        var world = new WorldState(
            worldId,
            new SimulationTime(12_345),
            [planet]);

        var json = WorldSnapshotSerializer.Serialize(world);
        var restored = WorldSnapshotSerializer.Deserialize(json);

        Assert.Equal(worldId, restored.Id);
        Assert.Equal(12_345, restored.CurrentTime.TotalSeconds);

        var restoredPlanet = Assert.Single(restored.Planets);

        Assert.Equal(planetId, restoredPlanet.Id);
        Assert.Equal("Earth", restoredPlanet.Name);
        Assert.Equal(5.9722e24, restoredPlanet.MassKilograms);
        Assert.Equal(6_371_000, restoredPlanet.MeanRadiusMeters);
        Assert.Equal(
            288.15,
            restoredPlanet.Environment.MeanSurfaceTemperatureKelvin);
        Assert.Equal(
            0.71,
            restoredPlanet.Environment.SurfaceWaterFraction);
        Assert.Equal(
            0.03,
            restoredPlanet.Environment.IceCoverageFraction);
        Assert.Equal(
            101_325,
            restoredPlanet.Environment.Atmosphere.SurfacePressurePascals);
        Assert.Equal(
            0.78,
            restoredPlanet.Environment.Atmosphere
                .CompositionByMoleFraction["N2"]);
        Assert.Equal(
            0.21,
            restoredPlanet.Environment.Atmosphere
                .CompositionByMoleFraction["O2"]);
        Assert.Equal(
            0.01,
            restoredPlanet.Environment.Atmosphere
                .CompositionByMoleFraction["Ar"]);
    }

    [Fact]
    public void RoundTrip_PreservesPopulationState()
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

        var parentId =
            new PersonId(
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000001"));

        var childId =
            new PersonId(
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000002"));

        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(123_456),
                [planet],
                [
                    new PersonState(
                        parentId,
                        planet.Id,
                        PersonSex.Female,
                        -800_000_000,
                        12.5,
                        -45.25,
                        needs:
                            new PersonNeedsState(
                                energyReserve: 0.42,
                                health: 0.73),
                        activity:
                            PersonActivity.Foraging),
                    new PersonState(
                        childId,
                        planet.Id,
                        PersonSex.Male,
                        100_000,
                        12.75,
                        -45.0,
                        parentId)
                ]);

        var restored =
            WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));

        Assert.True(
            world.Population.SequenceEqual(
                restored.Population));

        Assert.Equal(
            parentId,
            restored.Population[1].ParentId);
    }

    [Fact]
    public void RoundTrip_PreservesPregnancy()
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

        var motherId = PersonId.New();
        var fatherId = PersonId.New();

        var pregnancy =
            new PregnancyState(
                conceptionTimeSeconds: 123_456,
                fatherId);

        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(200_000),
                [planet],
                [
                    new PersonState(
                        motherId,
                        planet.Id,
                        PersonSex.Female,
                        -800_000_000,
                        10,
                        20,
                        pregnancy: pregnancy),
                    new PersonState(
                        fatherId,
                        planet.Id,
                        PersonSex.Male,
                        -800_000_000,
                        10.05,
                        20.05)
                ]);

        var restored =
            WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(
                    world));

        var mother =
            restored.Population.Single(
                person =>
                    person.Id == motherId);

        Assert.Equal(
            pregnancy,
            mother.Pregnancy);
    }

    [Fact]
    public void Deserialize_VersionSixDefaultsPregnancyToNull()
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

        var mother =
            new PersonState(
                PersonId.New(),
                planet.Id,
                PersonSex.Female,
                -800_000_000,
                10,
                20,
                pregnancy:
                    new PregnancyState(
                        123_456,
                        PersonId.New()));

        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(200_000),
                [planet],
                [mother]);

        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    world))!
                .AsObject();

        node["schemaVersion"] = 6;

        foreach (var entry in
                 node["population"]!.AsArray())
        {
            var person =
                entry!.AsObject();

            person.Remove(
                "pregnancyConceptionTimeSeconds");

            person.Remove(
                "pregnancyFatherId");
        }

        var restored =
            WorldSnapshotSerializer.Deserialize(
                node.ToJsonString());

        Assert.Null(
            Assert.Single(
                restored.Population)
                .Pregnancy);
    }

    [Fact]
    public void Deserialize_VersionSevenRejectsPartialPregnancy()
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

        var mother =
            new PersonState(
                PersonId.New(),
                planet.Id,
                PersonSex.Female,
                -800_000_000,
                10,
                20,
                pregnancy:
                    new PregnancyState(
                        123_456,
                        PersonId.New()));

        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(200_000),
                [planet],
                [mother]);

        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    world))!
                .AsObject();

        var person =
            node["population"]!
                .AsArray()[0]!
                .AsObject();

        person.Remove(
            "pregnancyFatherId");

        Assert.Throws<JsonException>(
            () =>
                WorldSnapshotSerializer.Deserialize(
                    node.ToJsonString()));
    }

    [Fact]
    public void RoundTrip_PreservesAnimals()
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

        var wolf =
            new AnimalState(
                new AnimalId(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000201")),
                planet.Id,
                AnimalSpecies.Wolf,
                12.5,
                -45.25,
                energyReserve: 0.42,
                health: 0.73,
                activity: AnimalActivity.Traveling);

        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(123_456),
                [planet],
                [],
                [],
                [wolf]);

        var restored =
            WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));

        Assert.Equal(
            wolf,
            Assert.Single(restored.Animals));
    }

    [Fact]
    public void Deserialize_VersionFiveGetsEmptyAnimals()
    {
        var worldId = WorldId.New().Value;

        var json =
            $$"""
            {
              "schemaVersion": 5,
              "worldId": "{{worldId}}",
              "currentTimeSeconds": 0,
              "planets": [],
              "population": [],
              "foodResources": []
            }
            """;

        var restored =
            WorldSnapshotSerializer.Deserialize(json);

        Assert.Empty(restored.Animals);
    }

    [Fact]
    public void Deserialize_VersionSixRequiresAnimals()
    {
        var worldId = WorldId.New().Value;

        var json =
            $$"""
            {
              "schemaVersion": 6,
              "worldId": "{{worldId}}",
              "currentTimeSeconds": 0,
              "planets": [],
              "population": [],
              "foodResources": []
            }
            """;

        Assert.Throws<JsonException>(
            () =>
                WorldSnapshotSerializer.Deserialize(
                    json));
    }

    [Fact]
    public void RoundTrip_PreservesFoodResources()
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

        var food =
            new FoodResourceState(
                new FoodResourceId(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000101")),
                planet.Id,
                12.5,
                -45.25,
                availableEnergy: 37.75,
                capacityEnergy: 50,
                recoveryEnergyPerDay: 2);

        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(123_456),
                [planet],
                [],
                [food]);

        var restored =
            WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));

        Assert.Equal(
            food,
            Assert.Single(
                restored.FoodResources));
    }

    [Fact]
    public void Deserialize_VersionThreeGetsEmptyFoodResources()
    {
        var worldId = WorldId.New().Value;

        var json =
            $$"""
            {
              "schemaVersion": 3,
              "worldId": "{{worldId}}",
              "currentTimeSeconds": 0,
              "planets": [],
              "population": []
            }
            """;

        var restored =
            WorldSnapshotSerializer.Deserialize(json);

        Assert.Empty(restored.FoodResources);
    }

    [Fact]
    public void Deserialize_VersionFourFoodDefaultsToFiniteNonRenewingState()
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

        var food =
            new FoodResourceState(
                FoodResourceId.New(),
                planet.Id,
                12.5,
                -45.25,
                availableEnergy: 37.75,
                capacityEnergy: 50,
                recoveryEnergyPerDay: 2);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [],
                [food]);

        var node =
            System.Text.Json.Nodes.JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    world))!
                .AsObject();

        node["schemaVersion"] = 4;

        foreach (var entry in
                 node["foodResources"]!.AsArray())
        {
            var resource =
                entry!.AsObject();

            resource.Remove(
                "capacityEnergy");

            resource.Remove(
                "recoveryEnergyPerDay");
        }

        var restored =
            WorldSnapshotSerializer.Deserialize(
                node.ToJsonString());

        var restoredFood =
            Assert.Single(
                restored.FoodResources);

        Assert.Equal(
            37.75,
            restoredFood.AvailableEnergy);

        Assert.Equal(
            37.75,
            restoredFood.CapacityEnergy);

        Assert.Equal(
            0,
            restoredFood.RecoveryEnergyPerDay);
    }

    [Fact]
    public void Deserialize_VersionFourRequiresFoodResources()
    {
        var worldId = WorldId.New().Value;

        var json =
            $$"""
            {
              "schemaVersion": 4,
              "worldId": "{{worldId}}",
              "currentTimeSeconds": 0,
              "planets": [],
              "population": []
            }
            """;

        Assert.Throws<JsonException>(
            () =>
                WorldSnapshotSerializer.Deserialize(
                    json));
    }

    [Fact]
    public void Deserialize_LegacyVersionOneHasEmptyPopulation()
    {
        var worldId = WorldId.New().Value;

        var json =
            $$"""
            {
              "schemaVersion": 1,
              "worldId": "{{worldId}}",
              "currentTimeSeconds": 42,
              "planets": []
            }
            """;

        var restored =
            WorldSnapshotSerializer.Deserialize(json);

        Assert.Equal(
            worldId,
            restored.Id.Value);

        Assert.Equal(
            42,
            restored.CurrentTime.TotalSeconds);

        Assert.Empty(restored.Population);
    }

    [Fact]
    public void Deserialize_VersionTwoPopulationGetsHealthyIdleDefaults()
    {
        var worldId = WorldId.New().Value;
        var planetId = PlanetId.New().Value;
        var personId = PersonId.New().Value;

        var json =
            $$"""
            {
              "schemaVersion": 2,
              "worldId": "{{worldId}}",
              "currentTimeSeconds": 0,
              "planets": [
                {
                  "planetId": "{{planetId}}",
                  "name": "Earth",
                  "massKilograms": 5.9722e24,
                  "meanRadiusMeters": 6371000,
                  "environment": {
                    "meanSurfaceTemperatureKelvin": 288.15,
                    "surfaceWaterFraction": 0.71,
                    "iceCoverageFraction": 0.03,
                    "atmosphere": {
                      "surfacePressurePascals": 0,
                      "compositionByMoleFraction": {}
                    }
                  }
                }
              ],
              "population": [
                {
                  "personId": "{{personId}}",
                  "planetId": "{{planetId}}",
                  "sex": 0,
                  "birthTimeSeconds": -1000,
                  "latitudeDegrees": 10,
                  "longitudeDegrees": 20,
                  "parentId": null
                }
              ]
            }
            """;

        var restored =
            WorldSnapshotSerializer.Deserialize(json);

        var person =
            Assert.Single(restored.Population);

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
    public void Deserialize_VersionThreeRequiresSurvivalState()
    {
        var worldId = WorldId.New().Value;
        var planetId = PlanetId.New().Value;
        var personId = PersonId.New().Value;

        var json =
            $$"""
            {
              "schemaVersion": 3,
              "worldId": "{{worldId}}",
              "currentTimeSeconds": 0,
              "planets": [
                {
                  "planetId": "{{planetId}}",
                  "name": "Earth",
                  "massKilograms": 5.9722e24,
                  "meanRadiusMeters": 6371000,
                  "environment": {
                    "meanSurfaceTemperatureKelvin": 288.15,
                    "surfaceWaterFraction": 0.71,
                    "iceCoverageFraction": 0.03,
                    "atmosphere": {
                      "surfacePressurePascals": 0,
                      "compositionByMoleFraction": {}
                    }
                  }
                }
              ],
              "population": [
                {
                  "personId": "{{personId}}",
                  "planetId": "{{planetId}}",
                  "sex": 0,
                  "birthTimeSeconds": -1000,
                  "latitudeDegrees": 10,
                  "longitudeDegrees": 20,
                  "parentId": null
                }
              ]
            }
            """;

        Assert.Throws<JsonException>(
            () =>
                WorldSnapshotSerializer.Deserialize(
                    json));
    }

    [Fact]
    public void Deserialize_VersionTwoRequiresPopulationCollection()
    {
        var worldId = WorldId.New().Value;

        var json =
            $$"""
            {
              "schemaVersion": 2,
              "worldId": "{{worldId}}",
              "currentTimeSeconds": 0,
              "planets": []
            }
            """;

        Assert.Throws<JsonException>(
            () => WorldSnapshotSerializer.Deserialize(json));
    }

    [Fact]
    public void Serialize_DoesNotStoreDerivedSurfaceGravity()
    {
        var world = CreateVacuumWorld();

        var json = WorldSnapshotSerializer.Serialize(world);

        Assert.DoesNotContain(
            "surfaceGravity",
            json,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deserialize_RejectsUnsupportedSchemaVersion()
    {
        var world = CreateVacuumWorld();
        var json = WorldSnapshotSerializer.Serialize(world);

        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;
        var modified =
            $"{{\"schemaVersion\":999,\"worldId\":\"{root.GetProperty("worldId").GetGuid()}\",\"currentTimeSeconds\":0,\"planets\":[]}}";

        Assert.Throws<NotSupportedException>(
            () => WorldSnapshotSerializer.Deserialize(modified));
    }

    [Fact]
    public void Deserialize_ReconstructsThroughDomainValidation()
    {
        var worldId = WorldId.New().Value;
        var planetId = PlanetId.New().Value;

        var json =
            $$"""
            {
              "schemaVersion": 1,
              "worldId": "{{worldId}}",
              "currentTimeSeconds": 0,
              "planets": [
                {
                  "planetId": "{{planetId}}",
                  "name": "Invalid Planet",
                  "massKilograms": -1,
                  "meanRadiusMeters": 6371000,
                  "environment": {
                    "meanSurfaceTemperatureKelvin": 288.15,
                    "surfaceWaterFraction": 0.71,
                    "iceCoverageFraction": 0.03,
                    "atmosphere": {
                      "surfacePressurePascals": 0,
                      "compositionByMoleFraction": {}
                    }
                  }
                }
              ]
            }
            """;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => WorldSnapshotSerializer.Deserialize(json));
    }

    [Fact]
    public void RoundTrip_PreservesTerrain()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Generated World",
                5.0e24,
                6_000_000,
                new PlanetEnvironment(
                    285,
                    0.60,
                    0.05,
                    AtmosphereState.Vacuum));

        var gridDefinition =
            SurfaceGridDefinition.LatitudeLongitude(
                latitudeBandCount: 4,
                longitudeBandCount: 8);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                gridDefinition);

        var terrain =
            new PlanetTerrainState(
                planet.Id,
                gridDefinition,
                grid.Cells.Select(
                    (cell, index) =>
                        new TerrainCellState(
                            cell.Id,
                            elevationMeters:
                                index * 125 - 2_000)));

        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(12_345),
                [planet],
                [],
                [],
                [],
                [terrain]);

        var json =
            WorldSnapshotSerializer.Serialize(
                world);

        Assert.Contains(
            "\"schemaVersion\": 10",
            json,
            StringComparison.Ordinal);

        var restored =
            WorldSnapshotSerializer.Deserialize(
                json);

        var restoredTerrain =
            Assert.Single(
                restored.Terrain);

        Assert.Equal(
            terrain.PlanetId,
            restoredTerrain.PlanetId);

        Assert.Equal(
            terrain.GridDefinition,
            restoredTerrain.GridDefinition);

        Assert.True(
            terrain.Cells.SequenceEqual(
                restoredTerrain.Cells));

        restoredTerrain.ValidateFor(
            Assert.Single(
                restored.Planets));
    }

    [Fact]
    public void RoundTrip_PreservesHydrology()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Water World",
                5.0e24,
                6_000_000,
                new PlanetEnvironment(
                    285,
                    0.60,
                    0.05,
                    AtmosphereState.Vacuum));

        var gridDefinition =
            SurfaceGridDefinition.LatitudeLongitude(
                latitudeBandCount: 4,
                longitudeBandCount: 8);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                gridDefinition);

        var terrain =
            new PlanetTerrainState(
                planet.Id,
                gridDefinition,
                grid.Cells.Select(
                    (cell, index) =>
                        new TerrainCellState(
                            cell.Id,
                            index * 100 - 1_000)));

        var hydrology =
            new PlanetHydrologyState(
                planet.Id,
                gridDefinition,
                grid.Cells.Select(
                    (cell, index) =>
                        new HydrologyCellState(
                            cell.Id,
                            atmosphericWaterKilogramsPerSquareMeter:
                                2 + index * 0.01,
                            surfaceLiquidWaterKilogramsPerSquareMeter:
                                100 + index,
                            soilWaterKilogramsPerSquareMeter:
                                25 + index * 0.5,
                            snowIceWaterEquivalentKilogramsPerSquareMeter:
                                index % 3)));

        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(12_345),
                [planet],
                [],
                [],
                [],
                [terrain],
                [hydrology]);

        var json =
            WorldSnapshotSerializer.Serialize(
                world);

        Assert.Contains(
            "\"schemaVersion\": 10",
            json,
            StringComparison.Ordinal);

        var restored =
            WorldSnapshotSerializer.Deserialize(
                json);

        var restoredHydrology =
            Assert.Single(
                restored.Hydrology);

        Assert.Equal(
            hydrology.PlanetId,
            restoredHydrology.PlanetId);

        Assert.Equal(
            hydrology.GridDefinition,
            restoredHydrology.GridDefinition);

        Assert.True(
            hydrology.Cells.SequenceEqual(
                restoredHydrology.Cells));

        restoredHydrology.ValidateFor(
            Assert.Single(
                restored.Planets));
    }

    [Fact]
    public void Deserialize_VersionEightGetsEmptyHydrology()
    {
        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    CreateVacuumWorld()))!
                .AsObject();

        node["schemaVersion"] = 8;
        node.Remove("hydrology");

        var restored =
            WorldSnapshotSerializer.Deserialize(
                node.ToJsonString());

        Assert.Empty(
            restored.Hydrology);
    }

    [Fact]
    public void Deserialize_VersionNineRequiresHydrology()
    {
        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    CreateVacuumWorld()))!
                .AsObject();

        node.Remove("hydrology");

        Assert.Throws<JsonException>(
            () =>
                WorldSnapshotSerializer.Deserialize(
                    node.ToJsonString()));
    }

    [Fact]
    public void RoundTrip_PreservesVegetation()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Green World",
                5.0e24,
                6_000_000,
                new PlanetEnvironment(
                    285,
                    0.60,
                    0.05,
                    AtmosphereState.Vacuum));

        var gridDefinition =
            SurfaceGridDefinition.LatitudeLongitude(
                latitudeBandCount: 4,
                longitudeBandCount: 8);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                gridDefinition);

        var terrain =
            new PlanetTerrainState(
                planet.Id,
                gridDefinition,
                grid.Cells.Select(
                    (cell, index) =>
                        new TerrainCellState(
                            cell.Id,
                            index * 100 - 1_000)));

        var hydrology =
            new PlanetHydrologyState(
                planet.Id,
                gridDefinition,
                grid.Cells.Select(
                    (cell, index) =>
                        new HydrologyCellState(
                            cell.Id,
                            atmosphericWaterKilogramsPerSquareMeter:
                                2 + index * 0.01,
                            surfaceLiquidWaterKilogramsPerSquareMeter:
                                0,
                            soilWaterKilogramsPerSquareMeter:
                                25 + index * 0.5,
                            snowIceWaterEquivalentKilogramsPerSquareMeter:
                                0)));

        var vegetation =
            new PlanetVegetationState(
                planet.Id,
                gridDefinition,
                grid.Cells.Select(
                    (cell, index) =>
                        new VegetationCellState(
                            cell.Id,
                            0.25 +
                            index * 0.1)));

        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(12_345),
                [planet],
                [],
                [],
                [],
                [terrain],
                [hydrology],
                [vegetation]);

        var json =
            WorldSnapshotSerializer.Serialize(
                world);

        Assert.Contains(
            "\"schemaVersion\": 10",
            json,
            StringComparison.Ordinal);

        var restored =
            WorldSnapshotSerializer.Deserialize(
                json);

        var restoredVegetation =
            Assert.Single(
                restored.Vegetation);

        Assert.Equal(
            vegetation.PlanetId,
            restoredVegetation.PlanetId);

        Assert.Equal(
            vegetation.GridDefinition,
            restoredVegetation.GridDefinition);

        Assert.True(
            vegetation.Cells.SequenceEqual(
                restoredVegetation.Cells));

        restoredVegetation.ValidateFor(
            Assert.Single(
                restored.Planets));
    }

    [Fact]
    public void Deserialize_VersionNineGetsEmptyVegetation()
    {
        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    CreateVacuumWorld()))!
                .AsObject();

        node["schemaVersion"] = 9;
        node.Remove("vegetation");

        var restored =
            WorldSnapshotSerializer.Deserialize(
                node.ToJsonString());

        Assert.Empty(
            restored.Vegetation);
    }

    [Fact]
    public void Deserialize_VersionTenRequiresVegetation()
    {
        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    CreateVacuumWorld()))!
                .AsObject();

        node.Remove("vegetation");

        Assert.Throws<JsonException>(
            () =>
                WorldSnapshotSerializer.Deserialize(
                    node.ToJsonString()));
    }

    [Fact]
    public void Deserialize_VersionSevenGetsEmptyTerrain()
    {
        var world =
            CreateVacuumWorld();

        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    world))!
                .AsObject();

        node["schemaVersion"] = 7;
        node.Remove("terrain");

        var restored =
            WorldSnapshotSerializer.Deserialize(
                node.ToJsonString());

        Assert.Empty(
            restored.Terrain);
    }

    [Fact]
    public void Deserialize_VersionEightRequiresTerrain()
    {
        var node =
            JsonNode.Parse(
                WorldSnapshotSerializer.Serialize(
                    CreateVacuumWorld()))!
                .AsObject();

        node.Remove("terrain");

        Assert.Throws<JsonException>(
            () =>
                WorldSnapshotSerializer.Deserialize(
                    node.ToJsonString()));
    }

    private static WorldState CreateVacuumWorld()
    {
        var planet = new PlanetState(
            PlanetId.New(),
            "Test",
            5.9722e24,
            6_371_000,
            new PlanetEnvironment(
                288.15,
                0.71,
                0.03,
                AtmosphereState.Vacuum));

        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [planet]);
    }

    [Fact]
    public void Deserialize_RejectsMissingRequiredField()
    {
        var worldId = WorldId.New().Value;

        var json =
            $$"""
            {
              "schemaVersion": 1,
              "worldId": "{{worldId}}",
              "planets": []
            }
            """;

        Assert.Throws<JsonException>(
            () => WorldSnapshotSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_RejectsDuplicatePlanetIdentities()
    {
        var worldId = WorldId.New().Value;
        var planetId = PlanetId.New().Value;

        var json =
            $$"""
            {
              "schemaVersion": 1,
              "worldId": "{{worldId}}",
              "currentTimeSeconds": 0,
              "planets": [
                {
                  "planetId": "{{planetId}}",
                  "name": "One",
                  "massKilograms": 1,
                  "meanRadiusMeters": 1,
                  "environment": {
                    "meanSurfaceTemperatureKelvin": 0,
                    "surfaceWaterFraction": 0,
                    "iceCoverageFraction": 0,
                    "atmosphere": {
                      "surfacePressurePascals": 0,
                      "compositionByMoleFraction": {}
                    }
                  }
                },
                {
                  "planetId": "{{planetId}}",
                  "name": "Two",
                  "massKilograms": 1,
                  "meanRadiusMeters": 1,
                  "environment": {
                    "meanSurfaceTemperatureKelvin": 0,
                    "surfaceWaterFraction": 0,
                    "iceCoverageFraction": 0,
                    "atmosphere": {
                      "surfacePressurePascals": 0,
                      "compositionByMoleFraction": {}
                    }
                  }
                }
              ]
            }
            """;

        Assert.Throws<ArgumentException>(
            () => WorldSnapshotSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_RejectsInvalidAtmosphericComposition()
    {
        var worldId = WorldId.New().Value;
        var planetId = PlanetId.New().Value;

        var json =
            $$"""
            {
              "schemaVersion": 1,
              "worldId": "{{worldId}}",
              "currentTimeSeconds": 0,
              "planets": [
                {
                  "planetId": "{{planetId}}",
                  "name": "Earth",
                  "massKilograms": 5.9722e24,
                  "meanRadiusMeters": 6371000,
                  "environment": {
                    "meanSurfaceTemperatureKelvin": 288.15,
                    "surfaceWaterFraction": 0.71,
                    "iceCoverageFraction": 0.03,
                    "atmosphere": {
                      "surfacePressurePascals": 101325,
                      "compositionByMoleFraction": {
                        "N2": 0.78,
                        "O2": 0.50
                      }
                    }
                  }
                }
              ]
            }
            """;

        Assert.Throws<ArgumentException>(
            () => WorldSnapshotSerializer.Deserialize(json));
    }

    [Fact]
    public void RoundTrip_PreservesEmptyWorld()
    {
        var world = new WorldState(
            WorldId.New(),
            new SimulationTime(42));

        var json = WorldSnapshotSerializer.Serialize(world);
        var restored = WorldSnapshotSerializer.Deserialize(json);

        Assert.Equal(world.Id, restored.Id);
        Assert.Equal(42, restored.CurrentTime.TotalSeconds);
        Assert.Empty(restored.Planets);
    }

    [Fact]
    public void RoundTrip_PreservesMultiplePlanets()
    {
        var first = new PlanetState(
            PlanetId.New(),
            "First",
            5.9722e24,
            6_371_000,
            new PlanetEnvironment(
                288.15,
                0.71,
                0.03,
                AtmosphereState.Vacuum));

        var second = new PlanetState(
            PlanetId.New(),
            "Second",
            6.39e23,
            3_389_500,
            new PlanetEnvironment(
                210,
                0,
                0,
                AtmosphereState.Vacuum));

        var world = new WorldState(
            WorldId.New(),
            new SimulationTime(99),
            [first, second]);

        var restored = WorldSnapshotSerializer.Deserialize(
            WorldSnapshotSerializer.Serialize(world));

        Assert.Equal(world.Id, restored.Id);
        Assert.Equal(99, restored.CurrentTime.TotalSeconds);
        Assert.Equal(2, restored.Planets.Length);

        Assert.Equal(first.Id, restored.Planets[0].Id);
        Assert.Equal(first.Name, restored.Planets[0].Name);
        Assert.Equal(first.MassKilograms, restored.Planets[0].MassKilograms);
        Assert.Equal(
            first.MeanRadiusMeters,
            restored.Planets[0].MeanRadiusMeters);

        Assert.Equal(second.Id, restored.Planets[1].Id);
        Assert.Equal(second.Name, restored.Planets[1].Name);
        Assert.Equal(second.MassKilograms, restored.Planets[1].MassKilograms);
        Assert.Equal(
            second.MeanRadiusMeters,
            restored.Planets[1].MeanRadiusMeters);
    }

    [Fact]
    public void Deserialize_RejectsExplicitNullPlanets()
    {
        var worldId = WorldId.New().Value;

        var json =
            $$"""
            {
              "schemaVersion": 1,
              "worldId": "{{worldId}}",
              "currentTimeSeconds": 0,
              "planets": null
            }
            """;

        Assert.Throws<JsonException>(
            () => WorldSnapshotSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_RejectsExplicitNullPlanetEnvironment()
    {
        var worldId = WorldId.New().Value;
        var planetId = PlanetId.New().Value;

        var json =
            $$"""
            {
              "schemaVersion": 1,
              "worldId": "{{worldId}}",
              "currentTimeSeconds": 0,
              "planets": [
                {
                  "planetId": "{{planetId}}",
                  "name": "Invalid",
                  "massKilograms": 1,
                  "meanRadiusMeters": 1,
                  "environment": null
                }
              ]
            }
            """;

        Assert.Throws<JsonException>(
            () => WorldSnapshotSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_RejectsDuplicateJsonProperties()
    {
        var firstWorldId = WorldId.New().Value;
        var secondWorldId = WorldId.New().Value;

        var json =
            $$"""
            {
              "schemaVersion": 1,
              "worldId": "{{firstWorldId}}",
              "worldId": "{{secondWorldId}}",
              "currentTimeSeconds": 0,
              "planets": []
            }
            """;

        Assert.Throws<JsonException>(
            () => WorldSnapshotSerializer.Deserialize(json));
    }

}
