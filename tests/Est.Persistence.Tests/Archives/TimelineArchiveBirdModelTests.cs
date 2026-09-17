using System.Text.Json;
using System.Text.Json.Nodes;
using Est.Persistence.Archives;
using Est.Simulation.Birds;
using Est.Simulation.Definitions;
using Est.Simulation.Hydrology;
using Est.Simulation.Invertebrates;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Timelines;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Persistence.Tests.Archives;

public sealed class TimelineArchiveBirdModelTests
{
    [Fact]
    public void RoundTrip_PreservesBirdModelDefinition()
    {
        var fixture =
            CreateFixture();

        var parameters =
            new BirdModelParameters(
                carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass:
                    0.000002,
                initialFractionOfLocalCarryingCapacity:
                    0.35,
                minimumInitialFlockMemberCount:
                    25,
                maximumInitialFlockCount:
                    12,
                maximumIntegrationStepSeconds:
                    3_600,
                maximumTravelMetersPerDay:
                    123_456,
                foodShortageMortalityRatePerDay:
                    0.07,
                waterAbsenceMortalityRatePerDay:
                    0.31,
                habitatAbsenceMortalityRatePerDay:
                    0.04,
                liveBiomassKilogramsPerBird:
                    0.75,
                liveNitrogenKilogramsPerBird:
                    0.01875,
                maximumPreyConsumptionKilogramsPerBirdPerDay:
                    0.42,
                maximumRecruitmentRatePerDay:
                    0.015);

        var definition =
            new SimulationDefinition(
                birdModels:
                [
                    new BirdModelDefinition(
                        fixture.Planet.Id,
                        parameters)
                ]);

        var restored =
            TimelineArchiveSerializer.Deserialize(
                TimelineArchiveSerializer.Serialize(
                    fixture.Timeline,
                    definition,
                    CreateProvenance()));

        var model =
            Assert.Single(
                restored.Definition.BirdModels);

        Assert.Equal(
            fixture.Planet.Id,
            model.PlanetId);

        Assert.Equal(
            parameters,
            model.Parameters);
    }

    [Fact]
    public void Deserialize_VersionTwentyOneDefaultsBirdRecruitmentPolicy()
    {
        var fixture =
            CreateFixture();

        var definition =
            new SimulationDefinition(
                birdModels:
                [
                    new BirdModelDefinition(
                        fixture.Planet.Id,
                        new BirdModelParameters(
                            maximumPreyConsumptionKilogramsPerBirdPerDay:
                                0.42,
                            maximumRecruitmentRatePerDay:
                                0.015))
                ]);

        var node =
            JsonNode.Parse(
                TimelineArchiveSerializer.Serialize(
                    fixture.Timeline,
                    definition,
                    CreateProvenance()))!
                .AsObject();

        node["schemaVersion"] =
            21;

        var parameters =
            node["definition"]!
                .AsObject()["birdModels"]!
                .AsArray()[0]!["parameters"]!
                .AsObject();

        parameters.Remove(
            "maximumPreyConsumptionKilogramsPerBirdPerDay");

        parameters.Remove(
            "maximumRecruitmentRatePerDay");

        var restored =
            TimelineArchiveSerializer.Deserialize(
                node.ToJsonString());

        var restoredParameters =
            Assert.Single(
                    restored.Definition.BirdModels)
                .Parameters;

        Assert.Equal(
            0,
            restoredParameters
                .MaximumPreyConsumptionKilogramsPerBirdPerDay);

        Assert.Equal(
            0,
            restoredParameters
                .MaximumRecruitmentRatePerDay);
    }

    [Fact]
    public void Deserialize_VersionFifteenDefaultsBirdMaterialComposition()
    {
        var fixture =
            CreateFixture();

        var definition =
            new SimulationDefinition(
                birdModels:
                [
                    new BirdModelDefinition(
                        fixture.Planet.Id,
                        new BirdModelParameters(
                            liveBiomassKilogramsPerBird:
                                0.75,
                            liveNitrogenKilogramsPerBird:
                                0.01875))
                ]);

        var node =
            JsonNode.Parse(
                TimelineArchiveSerializer.Serialize(
                    fixture.Timeline,
                    definition,
                    CreateProvenance()))!
                .AsObject();

        node["schemaVersion"] =
            15;

        var parameters =
            node["definition"]!
                .AsObject()["birdModels"]!
                .AsArray()[0]!["parameters"]!
                .AsObject();

        parameters.Remove(
            "liveBiomassKilogramsPerBird");

        parameters.Remove(
            "liveNitrogenKilogramsPerBird");

        var restored =
            TimelineArchiveSerializer.Deserialize(
                node.ToJsonString());

        var restoredParameters =
            Assert.Single(
                    restored.Definition.BirdModels)
                .Parameters;

        var defaults =
            new BirdModelParameters();

        Assert.Equal(
            defaults.MaterialPerBird,
            restoredParameters.MaterialPerBird);
    }

    [Fact]
    public void Deserialize_VersionEightDefaultsBirdModelsToEmpty()
    {
        var fixture =
            CreateFixture();

        var node =
            JsonNode.Parse(
                TimelineArchiveSerializer.Serialize(
                    fixture.Timeline,
                    SimulationDefinition.Empty,
                    CreateProvenance()))!
                .AsObject();

        node["schemaVersion"] = 8;

        node["definition"]!
            .AsObject()
            .Remove(
                "birdModels");

        var restored =
            TimelineArchiveSerializer.Deserialize(
                node.ToJsonString());

        Assert.Empty(
            restored.Definition.BirdModels);
    }

    [Fact]
    public void Deserialize_VersionNineRequiresBirdModels()
    {
        var fixture =
            CreateFixture();

        var node =
            JsonNode.Parse(
                TimelineArchiveSerializer.Serialize(
                    fixture.Timeline,
                    SimulationDefinition.Empty,
                    CreateProvenance()))!
                .AsObject();

        node["schemaVersion"] = 9;

        node["definition"]!
            .AsObject()
            .Remove(
                "birdModels");

        Assert.Throws<JsonException>(
            () =>
                TimelineArchiveSerializer.Deserialize(
                    node.ToJsonString()));
    }

    [Fact]
    public void Deserialize_VersionNineDefaultsBirdBehaviorPolicy()
    {
        var fixture =
            CreateFixture();

        var definition =
            new SimulationDefinition(
                birdModels:
                [
                    new BirdModelDefinition(
                        fixture.Planet.Id,
                        new BirdModelParameters(
                            carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass:
                                0.000003,
                            initialFractionOfLocalCarryingCapacity:
                                0.40,
                            minimumInitialFlockMemberCount:
                                15,
                            maximumInitialFlockCount:
                                20,
                            maximumIntegrationStepSeconds:
                                1_800,
                            maximumTravelMetersPerDay:
                                999_999,
                            foodShortageMortalityRatePerDay:
                                0.11,
                            waterAbsenceMortalityRatePerDay:
                                0.22,
                            habitatAbsenceMortalityRatePerDay:
                                0.33))
                ]);

        var node =
            JsonNode.Parse(
                TimelineArchiveSerializer.Serialize(
                    fixture.Timeline,
                    definition,
                    CreateProvenance()))!
                .AsObject();

        node["schemaVersion"] = 9;

        var parametersNode =
            node["definition"]!
                .AsObject()["birdModels"]!
                .AsArray()[0]!["parameters"]!
                .AsObject();

        parametersNode.Remove(
            "maximumIntegrationStepSeconds");

        parametersNode.Remove(
            "maximumTravelMetersPerDay");

        parametersNode.Remove(
            "foodShortageMortalityRatePerDay");

        parametersNode.Remove(
            "waterAbsenceMortalityRatePerDay");

        parametersNode.Remove(
            "habitatAbsenceMortalityRatePerDay");

        var restored =
            TimelineArchiveSerializer.Deserialize(
                node.ToJsonString());

        var restoredParameters =
            Assert.Single(
                    restored.Definition.BirdModels)
                .Parameters;

        var defaults =
            new BirdModelParameters();

        Assert.Equal(
            0.000003,
            restoredParameters
                .CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass);

        Assert.Equal(
            0.40,
            restoredParameters
                .InitialFractionOfLocalCarryingCapacity);

        Assert.Equal(
            15,
            restoredParameters
                .MinimumInitialFlockMemberCount);

        Assert.Equal(
            20,
            restoredParameters
                .MaximumInitialFlockCount);

        Assert.Equal(
            defaults.MaximumIntegrationStepSeconds,
            restoredParameters.MaximumIntegrationStepSeconds);

        Assert.Equal(
            defaults.MaximumTravelMetersPerDay,
            restoredParameters.MaximumTravelMetersPerDay);

        Assert.Equal(
            defaults.FoodShortageMortalityRatePerDay,
            restoredParameters.FoodShortageMortalityRatePerDay);

        Assert.Equal(
            defaults.WaterAbsenceMortalityRatePerDay,
            restoredParameters.WaterAbsenceMortalityRatePerDay);

        Assert.Equal(
            defaults.HabitatAbsenceMortalityRatePerDay,
            restoredParameters.HabitatAbsenceMortalityRatePerDay);
    }

    [Fact]
    public void Deserialize_VersionTenRequiresBirdBehaviorPolicy()
    {
        var fixture =
            CreateFixture();

        var definition =
            new SimulationDefinition(
                birdModels:
                [
                    new BirdModelDefinition(
                        fixture.Planet.Id,
                        new BirdModelParameters())
                ]);

        var node =
            JsonNode.Parse(
                TimelineArchiveSerializer.Serialize(
                    fixture.Timeline,
                    definition,
                    CreateProvenance()))!
                .AsObject();

        node["definition"]!
            .AsObject()["birdModels"]!
            .AsArray()[0]!["parameters"]!
            .AsObject()
            .Remove(
                "maximumTravelMetersPerDay");

        Assert.Throws<JsonException>(
            () =>
                TimelineArchiveSerializer.Deserialize(
                    node.ToJsonString()));
    }

    private static Fixture CreateFixture()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Bird Model Archive World",
                5.0e24,
                6_000_000,
                new PlanetEnvironment(
                    285,
                    0.60,
                    0.05,
                    AtmosphereState.Vacuum));

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                2,
                4);

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
                            0)));

        var hydrology =
            new PlanetHydrologyState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new HydrologyCellState(
                            cell.Id,
                            0,
                            0,
                            100,
                            0)));

        var vegetation =
            new PlanetVegetationState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new VegetationCellState(
                            cell.Id,
                            1)));

        var invertebrates =
            new PlanetInvertebrateState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new InvertebrateCellState(
                            cell.Id,
                            0.005)));

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
                vegetation:
                [
                    vegetation
                ],
                invertebrates:
                [
                    invertebrates
                ]);

        return new Fixture(
            planet,
            SimulationTimeline.Create(
                world));
    }

    private static TimelineArchiveProvenance CreateProvenance()
    {
        return new TimelineArchiveProvenance(
            "Est.Tests",
            "1.0",
            "test");
    }

    private sealed record Fixture(
        PlanetState Planet,
        SimulationTimeline Timeline);
}
