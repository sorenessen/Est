using Est.Simulation.Biogeochemistry;
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
    public void Evaluate_GrowthConsumesVegetationBiomass()
    {
        var fixture =
            CreateWorld(
                vegetationBiomass: 1,
                invertebrateBiomass: 0.005);

        var system =
            new InvertebrateSystem(
                fixture.Planet.Id,
                new InvertebrateModelParameters(
                    maximumIntegrationStepSeconds:
                        86_400,
                    carryingCapacityKilogramsPerKilogramLiveVegetation:
                        0.02,
                    maximumRelativeGrowthRatePerDay:
                        0.20,
                    baselineMortalityRatePerDay:
                        0));

        var changed =
            system.Evaluate(
                    fixture.World,
                    86_400)
                .Operation
                .Apply(
                    fixture.World);

        var initialInvertebrates =
            Assert.Single(
                fixture.World.Invertebrates);

        var finalInvertebrates =
            Assert.Single(
                changed.Invertebrates);

        var finalVegetation =
            Assert.Single(
                changed.Vegetation);

        var initialInvertebrateCell =
            initialInvertebrates.Cells[0];

        var initialVegetationCell =
            fixture.Vegetation.GetCell(
                initialInvertebrateCell.CellId);

        var finalInvertebrateCell =
            finalInvertebrates.GetCell(
                initialInvertebrateCell.CellId);

        var finalVegetationCell =
            finalVegetation.GetCell(
                initialInvertebrateCell.CellId);

        Assert.True(
            finalInvertebrateCell
                .LiveBiomassKilogramsPerSquareMeter >
            initialInvertebrateCell
                .LiveBiomassKilogramsPerSquareMeter);

        Assert.True(
            finalVegetationCell
                .LiveBiomassKilogramsPerSquareMeter <
            initialVegetationCell
                .LiveBiomassKilogramsPerSquareMeter);

        Assert.Equal(
            initialVegetationCell
                .LiveBiomassKilogramsPerSquareMeter +
            initialInvertebrateCell
                .LiveBiomassKilogramsPerSquareMeter,
            finalVegetationCell
                .LiveBiomassKilogramsPerSquareMeter +
            finalInvertebrateCell
                .LiveBiomassKilogramsPerSquareMeter,
            12);

        Assert.Equal(
            "planetary-invertebrates",
            system.Evaluate(
                    fixture.World,
                    86_400)
                .Cause);
    }

    [Fact]
    public void Evaluate_NitrogenBearingGrowthAssimilatesPlantNitrogen()
    {
        var fixture =
            CreateWorld(
                vegetationBiomass: 1,
                invertebrateBiomass: 0.005,
                invertebrateNitrogen: 0.000125,
                includeBiogeochemistry: true);

        var system =
            new InvertebrateSystem(
                fixture.Planet.Id,
                new InvertebrateModelParameters(
                    maximumIntegrationStepSeconds:
                        86_400,
                    carryingCapacityKilogramsPerKilogramLiveVegetation:
                        0.02,
                    maximumRelativeGrowthRatePerDay:
                        0.20,
                    baselineMortalityRatePerDay:
                        0,
                    liveNitrogenKilogramsPerKilogramLiveBiomass:
                        0.025),
                plantNitrogenKilogramsPerKilogramLiveBiomass:
                    0.025);

        var changed =
            system.Evaluate(
                    fixture.World,
                    86_400)
                .Operation
                .Apply(
                    fixture.World);

        var initialInvertebrate =
            Assert.Single(
                    fixture.World.Invertebrates)
                .Cells[0];

        var finalInvertebrate =
            Assert.Single(
                    changed.Invertebrates)
                .GetCell(
                    initialInvertebrate.CellId);

        var initialVegetation =
            fixture.Vegetation.GetCell(
                initialInvertebrate.CellId);

        var finalVegetation =
            Assert.Single(
                    changed.Vegetation)
                .GetCell(
                    initialInvertebrate.CellId);

        var biomassGrowth =
            finalInvertebrate
                .LiveBiomassKilogramsPerSquareMeter -
            initialInvertebrate
                .LiveBiomassKilogramsPerSquareMeter;

        var nitrogenGrowth =
            finalInvertebrate
                .LiveNitrogenKilogramsPerSquareMeter -
            initialInvertebrate
                .LiveNitrogenKilogramsPerSquareMeter;

        Assert.True(
            biomassGrowth > 0);

        Assert.Equal(
            biomassGrowth,
            initialVegetation
                .LiveBiomassKilogramsPerSquareMeter -
            finalVegetation
                .LiveBiomassKilogramsPerSquareMeter,
            12);

        Assert.Equal(
            biomassGrowth * 0.025,
            nitrogenGrowth,
            12);

        Assert.Equal(
            finalInvertebrate
                .LiveBiomassKilogramsPerSquareMeter *
            0.025,
            finalInvertebrate
                .LiveNitrogenKilogramsPerSquareMeter,
            12);
    }

    [Fact]
    public void Evaluate_NitrogenBearingGrowthWithoutPlantNitrogenPolicyIsRejected()
    {
        var fixture =
            CreateWorld(
                vegetationBiomass: 1,
                invertebrateBiomass: 0.005,
                invertebrateNitrogen: 0.000125,
                includeBiogeochemistry: true);

        var system =
            new InvertebrateSystem(
                fixture.Planet.Id,
                new InvertebrateModelParameters(
                    maximumIntegrationStepSeconds:
                        86_400,
                    carryingCapacityKilogramsPerKilogramLiveVegetation:
                        0.02,
                    maximumRelativeGrowthRatePerDay:
                        0.20,
                    baselineMortalityRatePerDay:
                        0,
                    liveNitrogenKilogramsPerKilogramLiveBiomass:
                        0.025));

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    system.Evaluate(
                        fixture.World,
                        86_400));

        Assert.Equal(
            "Nitrogen-bearing invertebrate growth requires an authoritative plant-tissue nitrogen policy.",
            exception.Message);
    }

    [Fact]
    public void Evaluate_NitrogenBearingGrowthWithoutBiogeochemistryIsRejected()
    {
        var fixture =
            CreateWorld(
                vegetationBiomass: 1,
                invertebrateBiomass: 0.005,
                invertebrateNitrogen: 0.000125);

        var system =
            new InvertebrateSystem(
                fixture.Planet.Id,
                new InvertebrateModelParameters(
                    maximumIntegrationStepSeconds:
                        86_400,
                    carryingCapacityKilogramsPerKilogramLiveVegetation:
                        0.02,
                    maximumRelativeGrowthRatePerDay:
                        0.20,
                    baselineMortalityRatePerDay:
                        0,
                    liveNitrogenKilogramsPerKilogramLiveBiomass:
                        0.025),
                plantNitrogenKilogramsPerKilogramLiveBiomass:
                    0.025);

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    system.Evaluate(
                        fixture.World,
                        86_400));

        Assert.Equal(
            "Nitrogen-bearing invertebrate growth requires authoritative biogeochemistry state for the target planet.",
            exception.Message);
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
        double invertebrateBiomass,
        double invertebrateNitrogen = 0,
        bool includeBiogeochemistry = false)
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
                            invertebrateBiomass,
                            invertebrateNitrogen)));

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

        if (includeBiogeochemistry)
        {
            var biogeochemistry =
                new PlanetBiogeochemistryState(
                    planet.Id,
                    definition,
                    grid.Cells.Select(
                        cell =>
                            new BiogeochemistryCellState(
                                cell.Id,
                                detritalBiomassKilogramsPerSquareMeter:
                                    0,
                                detritalNitrogenKilogramsPerSquareMeter:
                                    0,
                                plantAvailableNitrogenKilogramsPerSquareMeter:
                                    0.10)));

            world =
                world.ReplaceBiogeochemistry(
                    [biogeochemistry]);
        }

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
