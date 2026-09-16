using System.Text.Json;
using System.Text.Json.Nodes;
using Est.Persistence.Archives;
using Est.Simulation.Climate;
using Est.Simulation.Definitions;
using Est.Simulation.Ecology;
using Est.Simulation.Hydrology;
using Est.Simulation.Biogeochemistry;
using Est.Simulation.Invertebrates;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Causality;
using Est.Simulation.Operations;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Timelines;
using Est.Simulation.Vegetation;
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
    public void RoundTrip_PreservesTerrainInCurrentWorldAndCheckpoints()
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
                                index * 100 - 1_500)));

        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(100),
                [planet],
                [],
                [],
                [terrain]);

        var timeline =
            SimulationTimeline.Create(
                world);

        var step =
            new SimulationStepResult(
                world.AdvanceBy(60),
                new SimulationChange(
                    new AdvanceTimeOperation(0),
                    "terrain-test-step",
                    "Terrain persistence test.",
                    planet.Id,
                    60));

        timeline =
            timeline
                .RecordStep(step)
                .CreateCheckpoint();

        var restored =
            RoundTrip(
                timeline);

        var restoredCurrentTerrain =
            Assert.Single(
                restored.Timeline
                    .CurrentWorld
                    .Terrain);

        Assert.Equal(
            terrain.PlanetId,
            restoredCurrentTerrain.PlanetId);

        Assert.Equal(
            terrain.GridDefinition,
            restoredCurrentTerrain.GridDefinition);

        Assert.True(
            terrain.Cells.SequenceEqual(
                restoredCurrentTerrain.Cells));

        Assert.All(
            restored.Timeline.Checkpoints,
            checkpoint =>
            {
                var restoredCheckpointTerrain =
                    Assert.Single(
                        checkpoint.World.Terrain);

                Assert.Equal(
                    terrain.PlanetId,
                    restoredCheckpointTerrain.PlanetId);

                Assert.Equal(
                    terrain.GridDefinition,
                    restoredCheckpointTerrain.GridDefinition);

                Assert.True(
                    terrain.Cells.SequenceEqual(
                        restoredCheckpointTerrain.Cells));
            });
    }

    [Fact]
    public void RoundTrip_PreservesHydrologyInCurrentWorldAndCheckpoints()
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
                            elevationMeters:
                                index * 100 - 1_500)));

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
                new SimulationTime(100),
                [planet],
                [],
                [],
                [terrain],
                [hydrology]);

        var timeline =
            SimulationTimeline.Create(
                world);

        var step =
            new SimulationStepResult(
                world.AdvanceBy(60),
                new SimulationChange(
                    new AdvanceTimeOperation(0),
                    "hydrology-test-step",
                    "Hydrology persistence test.",
                    planet.Id,
                    60));

        timeline =
            timeline
                .RecordStep(step)
                .CreateCheckpoint();

        var restored =
            RoundTrip(
                timeline);

        var restoredCurrentHydrology =
            Assert.Single(
                restored.Timeline
                    .CurrentWorld
                    .Hydrology);

        Assert.Equal(
            hydrology.PlanetId,
            restoredCurrentHydrology.PlanetId);

        Assert.Equal(
            hydrology.GridDefinition,
            restoredCurrentHydrology.GridDefinition);

        Assert.True(
            hydrology.Cells.SequenceEqual(
                restoredCurrentHydrology.Cells));

        Assert.All(
            restored.Timeline.Checkpoints,
            checkpoint =>
            {
                var restoredCheckpointHydrology =
                    Assert.Single(
                        checkpoint.World.Hydrology);

                Assert.Equal(
                    hydrology.PlanetId,
                    restoredCheckpointHydrology.PlanetId);

                Assert.Equal(
                    hydrology.GridDefinition,
                    restoredCheckpointHydrology.GridDefinition);

                Assert.True(
                    hydrology.Cells.SequenceEqual(
                        restoredCheckpointHydrology.Cells));
            });
    }

    [Fact]
    public void RoundTrip_PreservesInvertebratesInCurrentWorldAndCheckpoints()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Invertebrate World",
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
                                index * 100 - 1_500)));

        var hydrology =
            new PlanetHydrologyState(
                planet.Id,
                gridDefinition,
                grid.Cells.Select(
                    cell =>
                        new HydrologyCellState(
                            cell.Id,
                            atmosphericWaterKilogramsPerSquareMeter:
                                2,
                            surfaceLiquidWaterKilogramsPerSquareMeter:
                                0,
                            soilWaterKilogramsPerSquareMeter:
                                50,
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
                            1 +
                            index * 0.1)));

        var invertebrates =
            new PlanetInvertebrateState(
                planet.Id,
                gridDefinition,
                grid.Cells.Select(
                    (cell, index) =>
                        new InvertebrateCellState(
                            cell.Id,
                            0.05 +
                            index * 0.01)));

        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(100),
                [planet],
                [],
                [],
                [terrain],
                [hydrology],
                [vegetation],
                [invertebrates]);

        var timeline =
            SimulationTimeline.Create(
                world);

        var step =
            new SimulationStepResult(
                world.AdvanceBy(60),
                new SimulationChange(
                    new AdvanceTimeOperation(0),
                    "invertebrate-test-step",
                    "Invertebrate persistence test.",
                    planet.Id,
                    60));

        timeline =
            timeline
                .RecordStep(step)
                .CreateCheckpoint();

        var restored =
            RoundTrip(
                timeline);

        var restoredCurrent =
            Assert.Single(
                restored.Timeline
                    .CurrentWorld
                    .Invertebrates);

        Assert.Equal(
            invertebrates.PlanetId,
            restoredCurrent.PlanetId);

        Assert.Equal(
            invertebrates.GridDefinition,
            restoredCurrent.GridDefinition);

        Assert.True(
            invertebrates.Cells.SequenceEqual(
                restoredCurrent.Cells));

        Assert.All(
            restored.Timeline.Checkpoints,
            checkpoint =>
            {
                var restoredCheckpoint =
                    Assert.Single(
                        checkpoint.World.Invertebrates);

                Assert.Equal(
                    invertebrates.PlanetId,
                    restoredCheckpoint.PlanetId);

                Assert.Equal(
                    invertebrates.GridDefinition,
                    restoredCheckpoint.GridDefinition);

                Assert.True(
                    invertebrates.Cells.SequenceEqual(
                        restoredCheckpoint.Cells));
            });
    }

    [Fact]
    public void RoundTrip_PreservesBiogeochemistryInCurrentWorldAndCheckpoints()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Biogeochemistry World",
                5.0e24,
                6_000_000,
                new PlanetEnvironment(
                    285,
                    0.60,
                    0.05,
                    AtmosphereState.Vacuum));

        var gridDefinition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

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
                            index)));

        var hydrology =
            new PlanetHydrologyState(
                planet.Id,
                gridDefinition,
                grid.Cells.Select(
                    cell =>
                        new HydrologyCellState(
                            cell.Id,
                            1,
                            0,
                            100,
                            0)));

        var biogeochemistry =
            new PlanetBiogeochemistryState(
                planet.Id,
                gridDefinition,
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
                SimulationTime.Zero,
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

        var timeline =
            SimulationTimeline.Create(
                world);

        var step =
            new SimulationStepResult(
                world.AdvanceBy(
                    60),
                new SimulationChange(
                    new AdvanceTimeOperation(
                        0),
                    "biogeochemistry-test-step",
                    "Biogeochemistry persistence test.",
                    planet.Id,
                    60));

        timeline =
            timeline
                .RecordStep(
                    step)
                .CreateCheckpoint();

        var restored =
            RoundTrip(
                timeline);

        var restoredCurrent =
            Assert.Single(
                restored.Timeline
                    .CurrentWorld
                    .Biogeochemistry);

        Assert.Equal(
            biogeochemistry.PlanetId,
            restoredCurrent.PlanetId);

        Assert.Equal(
            biogeochemistry.GridDefinition,
            restoredCurrent.GridDefinition);

        Assert.True(
            biogeochemistry.Cells.SequenceEqual(
                restoredCurrent.Cells));

        Assert.All(
            restored.Timeline.Checkpoints,
            checkpoint =>
            {
                var restoredCheckpoint =
                    Assert.Single(
                        checkpoint.World.Biogeochemistry);

                Assert.Equal(
                    biogeochemistry.PlanetId,
                    restoredCheckpoint.PlanetId);

                Assert.Equal(
                    biogeochemistry.GridDefinition,
                    restoredCheckpoint.GridDefinition);

                Assert.True(
                    biogeochemistry.Cells.SequenceEqual(
                        restoredCheckpoint.Cells));
            });
    }

    [Fact]
    public void RoundTrip_PreservesBiogeochemistryModelDefinition()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Biogeochemistry Model World",
                5.0e24,
                6_000_000,
                new PlanetEnvironment(
                    285,
                    0.60,
                    0.05,
                    AtmosphereState.Vacuum));

        var gridDefinition =
            SurfaceGridDefinition.LatitudeLongitude(
                2,
                4);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                gridDefinition);

        var terrain =
            new PlanetTerrainState(
                planet.Id,
                gridDefinition,
                grid.Cells.Select(
                    cell =>
                        new TerrainCellState(
                            cell.Id,
                            0)));

        var hydrology =
            new PlanetHydrologyState(
                planet.Id,
                gridDefinition,
                grid.Cells.Select(
                    cell =>
                        new HydrologyCellState(
                            cell.Id,
                            0,
                            0,
                            100,
                            0)));

        var biogeochemistry =
            new PlanetBiogeochemistryState(
                planet.Id,
                gridDefinition,
                grid.Cells.Select(
                    cell =>
                        new BiogeochemistryCellState(
                            cell.Id,
                            1,
                            0.03,
                            0.01)));

        var timeline =
            SimulationTimeline.Create(
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
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
                    ]));

        var parameters =
            new BiogeochemistryModelParameters(
                maximumIntegrationStepSeconds: 3_600,
                maximumRelativeDecompositionRatePerDay: 0.08,
                soilWaterForFullDecompositionKilogramsPerSquareMeter: 65,
                minimumDecompositionTemperatureKelvin: 260,
                optimumDecompositionTemperatureKelvin: 295,
                maximumDecompositionTemperatureKelvin: 320,
                temperatureLapseRateKelvinPerMeter: 0.006);

        var definition =
            new SimulationDefinition(
                biogeochemistryModels:
                [
                    new BiogeochemistryModelDefinition(
                        planet.Id,
                        parameters)
                ]);

        var restored =
            TimelineArchiveSerializer.Deserialize(
                TimelineArchiveSerializer.Serialize(
                    timeline,
                    definition,
                    CreateProvenance()));

        var model =
            Assert.Single(
                restored.Definition
                    .BiogeochemistryModels);

        Assert.Equal(
            planet.Id,
            model.PlanetId);

        Assert.Equal(
            parameters,
            model.Parameters);
    }

    [Fact]
    public void Deserialize_VersionTwelveDefaultsBiogeochemistryModelsToEmpty()
    {
        var json =
            TimelineArchiveSerializer.Serialize(
                CreateTimelineWithHistory(),
                SimulationDefinition.Empty,
                CreateProvenance());

        var node =
            JsonNode.Parse(json)!
                .AsObject();

        node["schemaVersion"] = 12;

        node["definition"]!
            .AsObject()
            .Remove(
                "biogeochemistryModels");

        var restored =
            TimelineArchiveSerializer.Deserialize(
                node.ToJsonString());

        Assert.Empty(
            restored.Definition
                .BiogeochemistryModels);
    }

    [Fact]
    public void Deserialize_VersionThirteenRequiresBiogeochemistryModels()
    {
        var json =
            TimelineArchiveSerializer.Serialize(
                CreateTimelineWithHistory(),
                SimulationDefinition.Empty,
                CreateProvenance());

        var node =
            JsonNode.Parse(json)!
                .AsObject();

        node["definition"]!
            .AsObject()
            .Remove(
                "biogeochemistryModels");

        Assert.Throws<JsonException>(
            () =>
                TimelineArchiveSerializer.Deserialize(
                    node.ToJsonString()));
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
            CreateTimelineWithVegetation(
                planet);

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

        var vegetationForaging =
            new VegetationForagingParameters(
                kilogramsLiveBiomassPerEnergyReserveUnit:
                    0.8,
                maximumHarvestKilogramsPerPersonPerDay:
                    1.25);

        var definition =
            new SimulationDefinition(
                populationModels:
                [
                    new PopulationModelDefinition(
                        planet.Id,
                        parameters,
                        vegetationForaging)
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
        Assert.Equal(
            vegetationForaging,
            model.VegetationForaging);
    }

    [Fact]
    public void RoundTrip_PreservesHydrologyModelDefinition()
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
            new HydrologyModelParameters(
                maximumIntegrationStepSeconds: 3_600,
                maximumEvaporationRateKilogramsPerSquareMeterPerDay: 5,
                atmosphericPrecipitationThresholdKilogramsPerSquareMeter: 18,
                maximumPrecipitationRateKilogramsPerSquareMeterPerDay: 14,
                soilWaterCapacityKilogramsPerSquareMeter: 175,
                maximumInfiltrationRateKilogramsPerSquareMeterPerDay: 22,
                maximumRunoffRateKilogramsPerSquareMeterPerDay: 28,
                freezingTemperatureKelvin: 272.5,
                meltingTemperatureKelvin: 274,
                maximumFreezingRateKilogramsPerSquareMeterPerDay: 21,
                maximumMeltingRateKilogramsPerSquareMeterPerDay: 23);

        var definition =
            new SimulationDefinition(
                hydrologyModels:
                [
                    new HydrologyModelDefinition(
                        planet.Id,
                        parameters)
                ]);

        var restored =
            TimelineArchiveSerializer.Deserialize(
                TimelineArchiveSerializer.Serialize(
                    timeline,
                    definition,
                    CreateProvenance()));

        var model =
            Assert.Single(
                restored.Definition.HydrologyModels);

        Assert.Equal(
            planet.Id,
            model.PlanetId);

        Assert.Equal(
            parameters,
            model.Parameters);
    }

    [Fact]
    public void Deserialize_VersionFourDefaultsHydrologyModelsToEmpty()
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

        var json =
            TimelineArchiveSerializer.Serialize(
                timeline,
                SimulationDefinition.Empty,
                CreateProvenance());

        var node =
            JsonNode.Parse(json)!
                .AsObject();

        node["schemaVersion"] = 4;

        node["definition"]!
            .AsObject()
            .Remove("hydrologyModels");

        var restored =
            TimelineArchiveSerializer.Deserialize(
                node.ToJsonString());

        Assert.Empty(
            restored.Definition.HydrologyModels);
    }

    [Fact]
    public void Deserialize_VersionFiveRequiresHydrologyModels()
    {
        var json =
            TimelineArchiveSerializer.Serialize(
                CreateTimelineWithHistory(),
                SimulationDefinition.Empty,
                CreateProvenance());

        var node =
            JsonNode.Parse(json)!
                .AsObject();

        node["definition"]!
            .AsObject()
            .Remove("hydrologyModels");

        Assert.Throws<JsonException>(
            () =>
                TimelineArchiveSerializer.Deserialize(
                    node.ToJsonString()));
    }

    [Fact]
    public void RoundTrip_PreservesVegetationModelDefinition()
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
            new VegetationModelParameters(
                maximumIntegrationStepSeconds:
                    3_600,
                carryingCapacityKilogramsPerSquareMeter:
                    7,
                maximumRelativeGrowthRatePerDay:
                    0.2,
                soilWaterForFullProductivityKilogramsPerSquareMeter:
                    60,
                minimumGrowthTemperatureKelvin:
                    270,
                optimumGrowthTemperatureKelvin:
                    292,
                maximumGrowthTemperatureKelvin:
                    315,
                temperatureLapseRateKelvinPerMeter:
                    0.006);

        var definition =
            new SimulationDefinition(
                vegetationModels:
                [
                    new VegetationModelDefinition(
                        planet.Id,
                        parameters)
                ]);

        var restored =
            TimelineArchiveSerializer.Deserialize(
                TimelineArchiveSerializer.Serialize(
                    timeline,
                    definition,
                    CreateProvenance()));

        var model =
            Assert.Single(
                restored.Definition.VegetationModels);

        Assert.Equal(
            planet.Id,
            model.PlanetId);

        Assert.Equal(
            parameters,
            model.Parameters);
    }

    [Fact]
    public void Deserialize_VersionSixDefaultsVegetationForagingToNull()
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
            CreateTimelineWithVegetation(
                planet);

        var definition =
            new SimulationDefinition(
                populationModels:
                [
                    new PopulationModelDefinition(
                        planet.Id,
                        new PopulationModelParameters(
                            seed: 42),
                        new VegetationForagingParameters(
                            kilogramsLiveBiomassPerEnergyReserveUnit:
                                0.8,
                            maximumHarvestKilogramsPerPersonPerDay:
                                1.25))
                ]);

        var json =
            TimelineArchiveSerializer.Serialize(
                timeline,
                definition,
                CreateProvenance());

        var node =
            JsonNode.Parse(json)!
                .AsObject();

        node["schemaVersion"] = 6;

        node["definition"]!
            ["populationModels"]!
            .AsArray()[0]!
            .AsObject()
            .Remove(
                "vegetationForaging");

        var restored =
            TimelineArchiveSerializer.Deserialize(
                node.ToJsonString());

        var model =
            Assert.Single(
                restored.Definition.PopulationModels);

        Assert.Null(
            model.VegetationForaging);
    }

    [Fact]
    public void Deserialize_VersionFiveDefaultsVegetationModelsToEmpty()
    {
        var timeline =
            CreateTimelineWithHistory();

        var json =
            TimelineArchiveSerializer.Serialize(
                timeline,
                SimulationDefinition.Empty,
                CreateProvenance());

        var node =
            JsonNode.Parse(json)!
                .AsObject();

        node["schemaVersion"] = 5;

        node["definition"]!
            .AsObject()
            .Remove("vegetationModels");

        var restored =
            TimelineArchiveSerializer.Deserialize(
                node.ToJsonString());

        Assert.Empty(
            restored.Definition.VegetationModels);
    }

    [Fact]
    public void Deserialize_VersionSixRequiresVegetationModels()
    {
        var json =
            TimelineArchiveSerializer.Serialize(
                CreateTimelineWithHistory(),
                SimulationDefinition.Empty,
                CreateProvenance());

        var node =
            JsonNode.Parse(json)!
                .AsObject();

        node["definition"]!
            .AsObject()
            .Remove("vegetationModels");

        Assert.Throws<JsonException>(
            () =>
                TimelineArchiveSerializer.Deserialize(
                    node.ToJsonString()));
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

    private static SimulationTimeline
        CreateTimelineWithVegetation(
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

        var terrain =
            new PlanetTerrainState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new TerrainCellState(
                            cell.Id,
                            elevationMeters:
                                0)));

        var hydrology =
            new PlanetHydrologyState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new HydrologyCellState(
                            cell.Id,
                            atmosphericWaterKilogramsPerSquareMeter:
                                0,
                            surfaceLiquidWaterKilogramsPerSquareMeter:
                                0,
                            soilWaterKilogramsPerSquareMeter:
                                100,
                            snowIceWaterEquivalentKilogramsPerSquareMeter:
                                0)));

        var vegetation =
            new PlanetVegetationState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new VegetationCellState(
                            cell.Id,
                            liveBiomassKilogramsPerSquareMeter:
                                1)));

        return SimulationTimeline.Create(
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [],
                [],
                [terrain],
                [hydrology],
                [vegetation]));
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
