using Est.Simulation.Hydrology;
using Est.Simulation.Invertebrates;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Invertebrates;

public sealed class InvertebrateSystemTests
{
    [Fact]
    public void Evaluate_GrowsSupportedBiomassWithoutConsumingVegetation()
    {
        var fixture =
            CreateWorld(
                vegetationBiomass: 1,
                invertebrateBiomass: 0.005);

        var system =
            new InvertebrateSystem(
                fixture.Planet.Id,
                new InvertebrateModelParameters(
                    carryingCapacityKilogramsPerKilogramLiveVegetation:
                        0.02,
                    maximumRelativeGrowthRatePerDay:
                        0.20,
                    baselineMortalityRatePerDay:
                        0));

        var change =
            system.Evaluate(
                fixture.World,
                86_400);

        var changed =
            change.Operation.Apply(
                fixture.World);

        var final =
            Assert.Single(
                changed.Invertebrates);

        Assert.All(
            final.Cells,
            cell =>
            {
                Assert.True(
                    cell.LiveBiomassKilogramsPerSquareMeter >
                    0.005);

                Assert.True(
                    cell.LiveBiomassKilogramsPerSquareMeter <=
                    0.02);
            });

        Assert.True(
            fixture.Vegetation.Cells.SequenceEqual(
                Assert.Single(
                        changed.Vegetation)
                    .Cells));

        Assert.Equal(
            "planetary-invertebrates",
            change.Cause);
    }

    [Fact]
    public void Evaluate_UnsupportedBiomassDeclinesByBaselineMortality()
    {
        var fixture =
            CreateWorld(
                vegetationBiomass: 0,
                invertebrateBiomass: 0.01);

        var system =
            new InvertebrateSystem(
                fixture.Planet.Id,
                new InvertebrateModelParameters(
                    maximumRelativeGrowthRatePerDay:
                        0.20,
                    baselineMortalityRatePerDay:
                        0.10));

        var changed =
            system.Evaluate(
                    fixture.World,
                    86_400)
                .Operation
                .Apply(
                    fixture.World);

        Assert.All(
            Assert.Single(
                    changed.Invertebrates)
                .Cells,
            cell =>
                Assert.True(
                    cell.LiveBiomassKilogramsPerSquareMeter <
                    0.01));
    }

    private static Fixture CreateWorld(
        double vegetationBiomass,
        double invertebrateBiomass)
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
                            vegetationBiomass)));

        var invertebrates =
            new PlanetInvertebrateState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new InvertebrateCellState(
                            cell.Id,
                            invertebrateBiomass)));

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
            vegetation,
            world);
    }

    private sealed record Fixture(
        PlanetState Planet,
        PlanetVegetationState Vegetation,
        WorldState World);
}
