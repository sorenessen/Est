using System.Text.Json;
using System.Text.Json.Nodes;
using Est.Persistence.Archives;
using Est.Simulation.Definitions;
using Est.Simulation.Grazers;
using Est.Simulation.Hydrology;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Timelines;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Persistence.Tests.Archives;

public sealed class TimelineArchiveGrazerModelTests
{
    [Fact]
    public void RoundTrip_PreservesGrazerModelDefinition()
    {
        var fixture =
            CreateFixture();

        var parameters =
            new GrazerModelParameters(
                carryingCapacityGrazersPerKilogramLiveVegetationBiomass:
                    0.000002,
                initialFractionOfLocalCarryingCapacity:
                    0.35,
                minimumInitialCohortMemberCount:
                    25,
                maximumInitialCohortCount:
                    12,
                maximumIntegrationStepSeconds:
                    7_200,
                maximumTravelMetersPerDay:
                    80_000,
                maximumGrazeKilogramsPerGrazerPerDay:
                    14,
                foodShortageMortalityRatePerDay:
                    0.08,
                waterAbsenceMortalityRatePerDay:
                    0.25,
                habitatAbsenceMortalityRatePerDay:
                    0.04,
                liveBiomassKilogramsPerGrazer:
                    320,
                liveNitrogenKilogramsPerGrazer:
                    8);

        var restored =
            RoundTrip(
                fixture,
                parameters);

        var model =
            Assert.Single(
                restored.Definition.GrazerModels);

        Assert.Equal(
            fixture.Planet.Id,
            model.PlanetId);

        Assert.Equal(
            parameters,
            model.Parameters);
    }

    [Fact]
    public void Deserialize_VersionFifteenDefaultsGrazerMaterialComposition()
    {
        var fixture =
            CreateFixture();

        var parameters =
            new GrazerModelParameters(
                liveBiomassKilogramsPerGrazer:
                    320,
                liveNitrogenKilogramsPerGrazer:
                    8);

        var node =
            JsonNode.Parse(
                TimelineArchiveSerializer.Serialize(
                    fixture.Timeline,
                    new SimulationDefinition(
                        grazerModels:
                        [
                            new GrazerModelDefinition(
                                fixture.Planet.Id,
                                parameters)
                        ]),
                    CreateProvenance()))!
                .AsObject();

        node["schemaVersion"] =
            15;

        var parametersNode =
            node["definition"]!
                .AsObject()["grazerModels"]!
                .AsArray()[0]!["parameters"]!
                .AsObject();

        parametersNode.Remove(
            "liveBiomassKilogramsPerGrazer");

        parametersNode.Remove(
            "liveNitrogenKilogramsPerGrazer");

        var restored =
            TimelineArchiveSerializer.Deserialize(
                node.ToJsonString());

        var restoredParameters =
            Assert.Single(
                    restored.Definition.GrazerModels)
                .Parameters;

        var defaults =
            new GrazerModelParameters();

        Assert.Equal(
            defaults.MaterialPerGrazer,
            restoredParameters.MaterialPerGrazer);
    }

    [Fact]
    public void Deserialize_VersionTenDefaultsGrazerModelsToEmpty()
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

        node["schemaVersion"] = 10;

        node["definition"]!
            .AsObject()
            .Remove(
                "grazerModels");

        var restored =
            TimelineArchiveSerializer.Deserialize(
                node.ToJsonString());

        Assert.Empty(
            restored.Definition.GrazerModels);
    }

    [Fact]
    public void Deserialize_VersionElevenRequiresGrazerModels()
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

        node["schemaVersion"] = 11;

        node["definition"]!
            .AsObject()
            .Remove(
                "grazerModels");

        Assert.Throws<JsonException>(
            () =>
                TimelineArchiveSerializer.Deserialize(
                    node.ToJsonString()));
    }

    [Fact]
    public void Deserialize_VersionElevenDefaultsGrazerBehaviorPolicy()
    {
        var fixture =
            CreateFixture();

        var node =
            CreateCurrentGrazerArchiveNode(
                fixture);

        node["schemaVersion"] = 11;

        var parameters =
            node["definition"]!["grazerModels"]![0]!["parameters"]!
                .AsObject();

        parameters.Remove(
            "maximumIntegrationStepSeconds");

        parameters.Remove(
            "maximumTravelMetersPerDay");

        parameters.Remove(
            "maximumGrazeKilogramsPerGrazerPerDay");

        parameters.Remove(
            "foodShortageMortalityRatePerDay");

        parameters.Remove(
            "waterAbsenceMortalityRatePerDay");

        parameters.Remove(
            "habitatAbsenceMortalityRatePerDay");

        var restored =
            TimelineArchiveSerializer.Deserialize(
                node.ToJsonString());

        var behavior =
            Assert.Single(
                    restored.Definition.GrazerModels)
                .Parameters;

        var defaults =
            new GrazerModelParameters();

        Assert.Equal(
            defaults.MaximumIntegrationStepSeconds,
            behavior.MaximumIntegrationStepSeconds);

        Assert.Equal(
            defaults.MaximumTravelMetersPerDay,
            behavior.MaximumTravelMetersPerDay);

        Assert.Equal(
            defaults.MaximumGrazeKilogramsPerGrazerPerDay,
            behavior.MaximumGrazeKilogramsPerGrazerPerDay);

        Assert.Equal(
            defaults.FoodShortageMortalityRatePerDay,
            behavior.FoodShortageMortalityRatePerDay);

        Assert.Equal(
            defaults.WaterAbsenceMortalityRatePerDay,
            behavior.WaterAbsenceMortalityRatePerDay);

        Assert.Equal(
            defaults.HabitatAbsenceMortalityRatePerDay,
            behavior.HabitatAbsenceMortalityRatePerDay);
    }

    [Fact]
    public void Deserialize_VersionTwelveRequiresGrazerBehaviorPolicy()
    {
        var fixture =
            CreateFixture();

        var node =
            CreateCurrentGrazerArchiveNode(
                fixture);

        node["definition"]!["grazerModels"]![0]!["parameters"]!
            .AsObject()
            .Remove(
                "maximumGrazeKilogramsPerGrazerPerDay");

        Assert.Throws<JsonException>(
            () =>
                TimelineArchiveSerializer.Deserialize(
                    node.ToJsonString()));
    }

    [Fact]
    public void Deserialize_VersionTwelveRequiresGrazerModels()
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

        node["definition"]!
            .AsObject()
            .Remove(
                "grazerModels");

        Assert.Throws<JsonException>(
            () =>
                TimelineArchiveSerializer.Deserialize(
                    node.ToJsonString()));
    }

    private static TimelineArchive CreateRoundTripArchive(
        Fixture fixture,
        GrazerModelParameters parameters)
    {
        return TimelineArchiveSerializer.Deserialize(
            TimelineArchiveSerializer.Serialize(
                fixture.Timeline,
                new SimulationDefinition(
                    grazerModels:
                    [
                        new GrazerModelDefinition(
                            fixture.Planet.Id,
                            parameters)
                    ]),
                CreateProvenance()));
    }

    private static TimelineArchive RoundTrip(
        Fixture fixture,
        GrazerModelParameters parameters)
    {
        return CreateRoundTripArchive(
            fixture,
            parameters);
    }

    private static JsonObject CreateCurrentGrazerArchiveNode(
        Fixture fixture)
    {
        return JsonNode.Parse(
            TimelineArchiveSerializer.Serialize(
                fixture.Timeline,
                new SimulationDefinition(
                    grazerModels:
                    [
                        new GrazerModelDefinition(
                            fixture.Planet.Id,
                            new GrazerModelParameters())
                    ]),
                CreateProvenance()))!
            .AsObject();
    }

    private static Fixture CreateFixture()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Grazer Model Archive World",
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
