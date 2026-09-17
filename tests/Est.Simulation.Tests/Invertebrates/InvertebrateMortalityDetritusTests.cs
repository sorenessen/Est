using Est.Simulation.Biogeochemistry;
using Est.Simulation.Hydrology;
using Est.Simulation.Invertebrates;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Invertebrates;

public sealed class InvertebrateMortalityDetritusTests
{
    private const long OneDaySeconds =
        86_400;

    [Fact]
    public void Evaluate_MortalityAddsRealizedMaterialLossToDetritus()
    {
        var setup =
            CreateWorld(
                includeBiogeochemistry:
                    true,
                invertebrateBiomass:
                    0.01,
                invertebrateNitrogen:
                    0.00025,
                detritalBiomass:
                    0.20,
                detritalNitrogen:
                    0.005,
                availableNitrogen:
                    0.03);

        var change =
            new InvertebrateSystem(
                setup.Planet.Id,
                new InvertebrateModelParameters(
                    maximumIntegrationStepSeconds:
                        OneDaySeconds,
                    maximumRelativeGrowthRatePerDay:
                        0,
                    baselineMortalityRatePerDay:
                        0.10))
            .Evaluate(
                setup.World,
                OneDaySeconds);

        var operation =
            Assert.IsType<
                ReplacePlanetInvertebrateBiogeochemistryStateOperation>(
                    change.Operation);

        var invertebrateCell =
            operation.Invertebrates.GetCell(
                setup.TargetCellId);

        var biogeochemistryCell =
            operation.Biogeochemistry.GetCell(
                setup.TargetCellId);

        Assert.Equal(
            0.009,
            invertebrateCell
                .LiveBiomassKilogramsPerSquareMeter,
            12);

        Assert.Equal(
            0.000225,
            invertebrateCell
                .LiveNitrogenKilogramsPerSquareMeter,
            12);

        Assert.Equal(
            0.201,
            biogeochemistryCell
                .DetritalBiomassKilogramsPerSquareMeter,
            12);

        Assert.Equal(
            0.005025,
            biogeochemistryCell
                .DetritalNitrogenKilogramsPerSquareMeter,
            12);

        Assert.Equal(
            0.03,
            biogeochemistryCell
                .PlantAvailableNitrogenKilogramsPerSquareMeter,
            12);
    }

    [Fact]
    public void Evaluate_ExtremeMortalityDepositsOnlyBiomassActuallyRemoved()
    {
        var setup =
            CreateWorld(
                includeBiogeochemistry:
                    true,
                invertebrateBiomass:
                    0.01,
                detritalBiomass:
                    0.20);

        var operation =
            Assert.IsType<
                ReplacePlanetInvertebrateBiogeochemistryStateOperation>(
                    new InvertebrateSystem(
                        setup.Planet.Id,
                        new InvertebrateModelParameters(
                            maximumIntegrationStepSeconds:
                                OneDaySeconds,
                            maximumRelativeGrowthRatePerDay:
                                0,
                            baselineMortalityRatePerDay:
                                2))
                    .Evaluate(
                        setup.World,
                        OneDaySeconds)
                    .Operation);

        Assert.Equal(
            0,
            operation.Invertebrates
                .GetCell(
                    setup.TargetCellId)
                .LiveBiomassKilogramsPerSquareMeter,
            12);

        Assert.Equal(
            0.21,
            operation.Biogeochemistry
                .GetCell(
                    setup.TargetCellId)
                .DetritalBiomassKilogramsPerSquareMeter,
            12);
    }

    [Fact]
    public void Evaluate_WithoutBiogeochemistryPreservesExistingOperation()
    {
        var setup =
            CreateWorld(
                includeBiogeochemistry:
                    false,
                invertebrateBiomass:
                    0.01);

        var change =
            new InvertebrateSystem(
                setup.Planet.Id,
                new InvertebrateModelParameters(
                    maximumIntegrationStepSeconds:
                        OneDaySeconds,
                    maximumRelativeGrowthRatePerDay:
                        0,
                    baselineMortalityRatePerDay:
                        0.10))
            .Evaluate(
                setup.World,
                OneDaySeconds);

        Assert.IsType<
            ReplacePlanetInvertebrateStateOperation>(
                change.Operation);

        var changed =
            change.Operation.Apply(
                setup.World);

        Assert.Equal(
            0.009,
            Assert.Single(
                    changed.Invertebrates)
                .GetCell(
                    setup.TargetCellId)
                .LiveBiomassKilogramsPerSquareMeter,
            12);

        Assert.Empty(
            changed.Biogeochemistry);
    }

    private static TestWorld CreateWorld(
        bool includeBiogeochemistry,
        double invertebrateBiomass,
        double invertebrateNitrogen = 0,
        double detritalBiomass = 0,
        double detritalNitrogen = 0,
        double availableNitrogen = 0)
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Invertebrate Mortality World",
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

        var target =
            grid.Cells[0];

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
                            0)));

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

        var biogeochemistry =
            includeBiogeochemistry
                ? new[]
                {
                    new PlanetBiogeochemistryState(
                        planet.Id,
                        definition,
                        grid.Cells.Select(
                            cell =>
                                new BiogeochemistryCellState(
                                    cell.Id,
                                    detritalBiomass,
                                    detritalNitrogen,
                                    availableNitrogen)))
                }
                : [];

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
                ],
                biogeochemistry:
                    biogeochemistry);

        return new TestWorld(
            planet,
            target.Id,
            world);
    }

    private sealed record TestWorld(
        PlanetState Planet,
        SurfaceCellId TargetCellId,
        WorldState World);
}
