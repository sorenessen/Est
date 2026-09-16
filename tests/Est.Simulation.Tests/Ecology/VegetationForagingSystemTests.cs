using Est.Simulation.Causality;
using Est.Simulation.Ecology;
using Est.Simulation.Hydrology;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Ecology;

public sealed class VegetationForagingSystemTests
{
    private const long OneDaySeconds =
        86_400;

    [Fact]
    public void Parameters_RejectInvalidPolicy()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new VegetationForagingParameters(
                    kilogramsLiveBiomassPerEnergyReserveUnit:
                        0,
                    maximumHarvestKilogramsPerPersonPerDay:
                        1));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new VegetationForagingParameters(
                    kilogramsLiveBiomassPerEnergyReserveUnit:
                        1,
                    maximumHarvestKilogramsPerPersonPerDay:
                        double.NaN));
    }

    [Fact]
    public void Step_HungryPersonConsumesOccupiedVegetationCell()
    {
        var setup =
            CreateWorld(
                personEnergy: 0.25,
                targetBiomass: 1);

        var result =
            SimulationStepRunner.Step(
                setup.World,
                OneDaySeconds,
                new ForagingSystem(
                    setup.Planet.Id,
                    new VegetationForagingParameters(
                        kilogramsLiveBiomassPerEnergyReserveUnit:
                            1,
                        maximumHarvestKilogramsPerPersonPerDay:
                            1)));

        Assert.IsType<
            ReplacePlanetVegetationForagingStateOperation>(
                result.Change.Operation);

        var changedPerson =
            Assert.Single(
                result.World.Population);

        var changedVegetation =
            Assert.Single(
                result.World.Vegetation);

        var beforeDensity =
            setup.World.Vegetation
                .Single()
                .GetCell(
                    setup.TargetCell.Id)
                .LiveBiomassKilogramsPerSquareMeter;

        var afterDensity =
            changedVegetation
                .GetCell(
                    setup.TargetCell.Id)
                .LiveBiomassKilogramsPerSquareMeter;

        var biomassRemoved =
            (beforeDensity - afterDensity) *
            setup.TargetCell.AreaSquareMeters;

        Assert.Equal(
            0.75,
            biomassRemoved,
            8);

        Assert.Equal(
            1 - (1d / 30d),
            changedPerson.Needs.EnergyReserve,
            10);

        Assert.Equal(
            PersonActivity.Eating,
            changedPerson.Activity);

        Assert.Equal(
            0.25,
            setup.World.Population[0]
                .Needs.EnergyReserve,
            10);

        Assert.Equal(
            1,
            setup.World.Vegetation
                .Single()
                .GetCell(
                    setup.TargetCell.Id)
                .LiveBiomassKilogramsPerSquareMeter,
            12);

        Assert.Equal(
            0.75,
            result.Change.Metrics[
                "biomassHarvestedKilograms"],
            10);

        Assert.Equal(
            0.75,
            result.Change.Metrics[
                "energyConsumed"],
            10);

        Assert.Equal(
            "vegetation-foraging",
            result.Change.Cause);
    }

    [Fact]
    public void Step_VegetationForagingRespectsDailyHarvestLimit()
    {
        var setup =
            CreateWorld(
                personEnergy: 0.25,
                targetBiomass: 1);

        var result =
            SimulationStepRunner.Step(
                setup.World,
                OneDaySeconds,
                new ForagingSystem(
                    setup.Planet.Id,
                    new VegetationForagingParameters(
                        kilogramsLiveBiomassPerEnergyReserveUnit:
                            1,
                        maximumHarvestKilogramsPerPersonPerDay:
                            0.1)));

        var changedPerson =
            Assert.Single(
                result.World.Population);

        Assert.Equal(
            0.25 +
            0.1 -
            (1d / 30d),
            changedPerson.Needs.EnergyReserve,
            10);

        Assert.Equal(
            0.1,
            result.Change.Metrics[
                "biomassHarvestedKilograms"],
            10);

        Assert.Equal(
            0.1,
            result.Change.Metrics[
                "energyConsumed"],
            10);
    }

    [Fact]
    public void Step_RequiresAuthoritativeVegetation()
    {
        var setup =
            CreateWorld(
                personEnergy: 0.25,
                targetBiomass: 1,
                includeVegetation: false);

        Assert.Throws<InvalidOperationException>(
            () =>
                SimulationStepRunner.Step(
                    setup.World,
                    OneDaySeconds,
                    new ForagingSystem(
                        setup.Planet.Id,
                        new VegetationForagingParameters(
                            kilogramsLiveBiomassPerEnergyReserveUnit:
                                1,
                            maximumHarvestKilogramsPerPersonPerDay:
                                1))));
    }

    private static TestWorld CreateWorld(
        double personEnergy,
        double targetBiomass,
        bool includeVegetation = true)
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Vegetation Foraging World",
                5.0e24,
                1_000,
                new PlanetEnvironment(
                    293.15,
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

        var targetCell =
            grid.LocateCell(
                latitudeDegrees: 0,
                longitudeDegrees: 0);

        var terrain =
            new PlanetTerrainState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new TerrainCellState(
                            cell.Id,
                            elevationMeters: 0)));

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
                            cell.Id ==
                            targetCell.Id
                                ? targetBiomass
                                : 0)));

        var person =
            new PersonState(
                PersonId.New(),
                planet.Id,
                PersonSex.Male,
                birthTimeSeconds: 0,
                latitudeDegrees: 0,
                longitudeDegrees: 0,
                needs:
                    new PersonNeedsState(
                        energyReserve:
                            personEnergy));

        var vegetationStates =
            includeVegetation
                ? new[]
                {
                    vegetation
                }
                : Array.Empty<
                    PlanetVegetationState>();

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [person],
                [],
                [terrain],
                [hydrology],
                vegetationStates);

        return new TestWorld(
            planet,
            targetCell,
            world);
    }

    private sealed record TestWorld(
        PlanetState Planet,
        SurfaceCell TargetCell,
        WorldState World);
}
