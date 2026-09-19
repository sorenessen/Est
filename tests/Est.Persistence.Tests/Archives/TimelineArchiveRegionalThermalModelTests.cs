using System.Text.Json;
using System.Text.Json.Nodes;
using Est.Persistence.Archives;
using Est.Simulation.Definitions;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Thermal;
using Est.Simulation.Time;
using Est.Simulation.Timelines;
using Est.Simulation.Worlds;

namespace Est.Persistence.Tests.Archives;

public sealed class TimelineArchiveRegionalThermalModelTests
{
    [Fact]
    public void RoundTrip_PreservesRegionalThermalModelDefinition()
    {
        var setup =
            CreateTimeline();

        var parameters =
            CreateParameters();

        var definition =
            new SimulationDefinition(
                regionalThermalModels:
                [
                    new RegionalThermalModelDefinition(
                        setup.Planet.Id,
                        parameters)
                ]);

        var restored =
            TimelineArchiveSerializer.Deserialize(
                TimelineArchiveSerializer.Serialize(
                    setup.Timeline,
                    definition,
                    CreateProvenance()));

        var model =
            Assert.Single(
                restored.Definition
                    .RegionalThermalModels);

        Assert.Equal(
            setup.Planet.Id,
            model.PlanetId);

        Assert.Equal(
            parameters,
            model.Parameters);
    }

    [Fact]
    public void Deserialize_VersionTwentyThreeDefaultsRegionalThermalModelsToEmpty()
    {
        var setup =
            CreateTimeline();

        var node =
            JsonNode.Parse(
                TimelineArchiveSerializer.Serialize(
                    setup.Timeline,
                    SimulationDefinition.Empty,
                    CreateProvenance()))!
                .AsObject();

        node["schemaVersion"] = 23;

        node["definition"]!
            .AsObject()
            .Remove(
                "regionalThermalModels");

        var restored =
            TimelineArchiveSerializer.Deserialize(
                node.ToJsonString());

        Assert.Empty(
            restored.Definition
                .RegionalThermalModels);
    }

    [Fact]
    public void Deserialize_VersionTwentyFourRequiresRegionalThermalModels()
    {
        var setup =
            CreateTimeline();

        var node =
            JsonNode.Parse(
                TimelineArchiveSerializer.Serialize(
                    setup.Timeline,
                    SimulationDefinition.Empty,
                    CreateProvenance()))!
                .AsObject();

        node["definition"]!
            .AsObject()
            .Remove(
                "regionalThermalModels");

        Assert.Throws<JsonException>(
            () =>
                TimelineArchiveSerializer.Deserialize(
                    node.ToJsonString()));
    }

    [Fact]
    public void Deserialize_RegionalThermalModelRequiresParameters()
    {
        var setup =
            CreateTimeline();

        var definition =
            new SimulationDefinition(
                regionalThermalModels:
                [
                    new RegionalThermalModelDefinition(
                        setup.Planet.Id,
                        CreateParameters())
                ]);

        var node =
            JsonNode.Parse(
                TimelineArchiveSerializer.Serialize(
                    setup.Timeline,
                    definition,
                    CreateProvenance()))!
                .AsObject();

        node["definition"]!
            ["regionalThermalModels"]!
            .AsArray()[0]!
            .AsObject()
            .Remove(
                "parameters");

        Assert.Throws<JsonException>(
            () =>
                TimelineArchiveSerializer.Deserialize(
                    node.ToJsonString()));
    }

    private static RegionalThermalModelParameters
        CreateParameters()
    {
        return new RegionalThermalModelParameters(
            stellarFluxWattsPerSquareMeter:
                1361.25,
            atmosphericShortwaveVerticalOpticalDepth:
                0.22,
            atmosphericShortwaveSingleScatteringAlbedo:
                0.37,
            atmosphericShortwaveDownwardScatteringFraction:
                0.58,
            surfaceShortwaveAlbedo:
                0.29,
            surfaceLongwaveEmissivity:
                0.96,
            atmosphericLongwaveEmissivity:
                0.74,
            surfaceEffectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                8.5e7,
            atmosphericEffectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                1.4e7,
            maximumIntegrationStepSeconds:
                1_800);
    }

    private static TestTimeline CreateTimeline()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Regional Thermal Archive World",
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
                            index * 100)));

        var thermal =
            PlanetRegionalThermalInitializer
                .FromPlanetaryMeanSurfaceTemperature(
                    planet,
                    terrain);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [],
                terrain: [terrain],
                regionalThermal: [thermal]);

        return new TestTimeline(
            planet,
            SimulationTimeline.Create(
                world));
    }

    private static TimelineArchiveProvenance
        CreateProvenance()
    {
        return new TimelineArchiveProvenance(
            "Est.Tests",
            "1.0",
            "regional-thermal-model-tests");
    }

    private sealed record TestTimeline(
        PlanetState Planet,
        SimulationTimeline Timeline);
}
