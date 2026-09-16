using System.Text.Json;
using System.Text.Json.Nodes;
using Est.Persistence.Archives;
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

public sealed class TimelineArchiveInvertebrateModelTests
{
    [Fact]
    public void RoundTrip_PreservesInvertebrateModelDefinition()
    {
        var fixture =
            CreateFixture();

        var parameters =
            new InvertebrateModelParameters(
                maximumIntegrationStepSeconds: 3_600,
                carryingCapacityKilogramsPerKilogramLiveVegetation: 0.04,
                initialFractionOfLocalCarryingCapacity: 0.30,
                maximumRelativeGrowthRatePerDay: 0.25,
                baselineMortalityRatePerDay: 0.03);

        var definition =
            new SimulationDefinition(
                invertebrateModels:
                [
                    new InvertebrateModelDefinition(
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
                restored.Definition.InvertebrateModels);

        Assert.Equal(
            fixture.Planet.Id,
            model.PlanetId);

        Assert.Equal(
            parameters,
            model.Parameters);
    }

    [Fact]
    public void Deserialize_VersionSevenDefaultsInvertebrateModelsToEmpty()
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

        node["schemaVersion"] = 7;

        node["definition"]!
            .AsObject()
            .Remove(
                "invertebrateModels");

        var restored =
            TimelineArchiveSerializer.Deserialize(
                node.ToJsonString());

        Assert.Empty(
            restored.Definition.InvertebrateModels);
    }

    [Fact]
    public void Deserialize_VersionEightRequiresInvertebrateModels()
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
                "invertebrateModels");

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
                "Invertebrate World",
                5.0e24,
                6_000_000,
                new PlanetEnvironment(
                    285,
                    0.60,
                    0.05,
                    AtmosphereState.Vacuum));

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
                [],
                [terrain],
                [hydrology],
                [vegetation],
                [invertebrates]);

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
