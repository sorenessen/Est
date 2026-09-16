using Est.Simulation.Biogeochemistry;
using Est.Simulation.Ecology;
using Est.Simulation.Grazers;
using Est.Simulation.Hydrology;
using Est.Simulation.Organisms;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Organisms;

public sealed class VegetationFeedingMaterialAccountingTests
{
    private const long OneDaySeconds =
        86_400;

    private const double PlantNitrogenRatio =
        0.02;

    [Fact]
    public void HumanForaging_ReturnsConsumedPlantNitrogenAndRespiredBiomassIsExplicit()
    {
        var fixture =
            CreateFixture();

        var person =
            new PersonState(
                PersonId.New(),
                fixture.Planet.Id,
                PersonSex.Male,
                birthTimeSeconds: 0,
                fixture.TargetCell.CenterLatitudeDegrees,
                fixture.TargetCell.CenterLongitudeDegrees,
                needs:
                    new PersonNeedsState(
                        energyReserve: 0.25),
                material:
                    new OrganismMaterialState(
                        liveBiomassKilograms: 70,
                        liveNitrogenKilograms: 1.75));

        var world =
            CreateWorld(
                fixture,
                person: person,
                vegetationMassKilograms: 1);

        var change =
            new ForagingSystem(
                    fixture.Planet.Id,
                    new VegetationForagingParameters(
                        kilogramsLiveBiomassPerEnergyReserveUnit:
                            1,
                        maximumHarvestKilogramsPerPersonPerDay:
                            1),
                    plantNitrogenKilogramsPerKilogramLiveBiomass:
                        PlantNitrogenRatio)
                .Evaluate(
                    world,
                    OneDaySeconds);

        var changed =
            change.Operation.Apply(
                world);

        var changedPerson =
            Assert.Single(
                changed.Population);

        Assert.Equal(
            70,
            changedPerson.Material.LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            1.75,
            changedPerson.Material.LiveNitrogenKilograms,
            precision: 10);

        Assert.Equal(
            0.75,
            RemovedVegetationMassKilograms(
                world,
                changed,
                fixture.TargetCell),
            precision: 10);

        Assert.Equal(
            0.75,
            change.Metrics[
                "biomassRespiredKilograms"],
            precision: 10);

        Assert.Equal(
            0.015,
            change.Metrics[
                "nitrogenReturnedKilograms"],
            precision: 10);

        AssertBiogeochemistryMass(
            changed,
            fixture.Grid,
            fixture.TargetCell,
            expectedDetritalBiomassKilograms: 0,
            expectedDetritalNitrogenKilograms: 0,
            expectedPlantAvailableNitrogenKilograms:
                0.015);

        AssertBiogeochemistryMass(
            world,
            fixture.Grid,
            fixture.TargetCell,
            expectedDetritalBiomassKilograms: 0,
            expectedDetritalNitrogenKilograms: 0,
            expectedPlantAvailableNitrogenKilograms:
                0);
    }

    [Fact]
    public void GrazerGrazing_ReturnsConsumedPlantNitrogenAndPreservesCohortMaterial()
    {
        var fixture =
            CreateFixture();

        var cohort =
            new GrazerCohortState(
                GrazerCohortId.New(),
                fixture.Planet.Id,
                memberCount: 10,
                fixture.TargetCell.CenterLatitudeDegrees,
                fixture.TargetCell.CenterLongitudeDegrees,
                material:
                    new OrganismMaterialState(
                        liveBiomassKilograms: 2_500,
                        liveNitrogenKilograms: 62.5));

        var world =
            CreateWorld(
                fixture,
                cohort: cohort,
                vegetationMassKilograms: 1_000,
                surfaceWaterKilogramsPerSquareMeter:
                    100);

        var change =
            new GrazerCohortSystem(
                    fixture.Planet.Id,
                    new GrazerModelParameters(
                        carryingCapacityGrazersPerKilogramLiveVegetationBiomass:
                            1,
                        maximumIntegrationStepSeconds:
                            OneDaySeconds,
                        maximumTravelMetersPerDay:
                            0,
                        maximumGrazeKilogramsPerGrazerPerDay:
                            2,
                        foodShortageMortalityRatePerDay:
                            0,
                        waterAbsenceMortalityRatePerDay:
                            0,
                        habitatAbsenceMortalityRatePerDay:
                            0),
                    plantNitrogenKilogramsPerKilogramLiveBiomass:
                        PlantNitrogenRatio)
                .Evaluate(
                    world,
                    OneDaySeconds);

        var changed =
            change.Operation.Apply(
                world);

        var changedCohort =
            Assert.Single(
                changed.GrazerCohorts);

        Assert.Equal(
            10,
            changedCohort.MemberCount);

        Assert.Equal(
            2_500,
            changedCohort.Material.LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            62.5,
            changedCohort.Material.LiveNitrogenKilograms,
            precision: 10);

        Assert.Equal(
            20,
            RemovedVegetationMassKilograms(
                world,
                changed,
                fixture.TargetCell),
            precision: 8);

        Assert.Equal(
            20,
            change.Metrics[
                "biomassRespiredKilograms"],
            precision: 10);

        Assert.Equal(
            0.4,
            change.Metrics[
                "nitrogenReturnedKilograms"],
            precision: 10);

        AssertBiogeochemistryMass(
            changed,
            fixture.Grid,
            fixture.TargetCell,
            expectedDetritalBiomassKilograms: 0,
            expectedDetritalNitrogenKilograms: 0,
            expectedPlantAvailableNitrogenKilograms:
                0.4);
    }

    [Fact]
    public void HumanForagingStarvation_TransfersRemainingMaterialIntoDetritus()
    {
        var fixture =
            CreateFixture();

        var person =
            new PersonState(
                PersonId.New(),
                fixture.Planet.Id,
                PersonSex.Female,
                birthTimeSeconds: 0,
                fixture.TargetCell.CenterLatitudeDegrees,
                fixture.TargetCell.CenterLongitudeDegrees,
                needs:
                    new PersonNeedsState(
                        energyReserve: 0,
                        health: 1),
                material:
                    new OrganismMaterialState(
                        liveBiomassKilograms: 70,
                        liveNitrogenKilograms: 1.75));

        var world =
            CreateWorld(
                fixture,
                person: person,
                vegetationMassKilograms: 0);

        var change =
            new ForagingSystem(
                    fixture.Planet.Id,
                    new VegetationForagingParameters(
                        kilogramsLiveBiomassPerEnergyReserveUnit:
                            1,
                        maximumHarvestKilogramsPerPersonPerDay:
                            1))
                .Evaluate(
                    world,
                    16 * OneDaySeconds);

        var changed =
            change.Operation.Apply(
                world);

        Assert.Single(
            world.Population);

        Assert.Empty(
            changed.Population);

        Assert.Equal(
            1,
            change.Metrics[
                "starvationDeaths"],
            precision: 10);

        AssertBiogeochemistryMass(
            changed,
            fixture.Grid,
            fixture.TargetCell,
            expectedDetritalBiomassKilograms: 70,
            expectedDetritalNitrogenKilograms: 1.75,
            expectedPlantAvailableNitrogenKilograms:
                0);

        AssertBiogeochemistryMass(
            world,
            fixture.Grid,
            fixture.TargetCell,
            expectedDetritalBiomassKilograms: 0,
            expectedDetritalNitrogenKilograms: 0,
            expectedPlantAvailableNitrogenKilograms:
                0);
    }

    private static Fixture CreateFixture()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Vegetation Feeding Material World",
                massKilograms: 5.0e24,
                meanRadiusMeters: 1_000,
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

        return new Fixture(
            planet,
            definition,
            grid,
            targetCell);
    }

    private static WorldState CreateWorld(
        Fixture fixture,
        PersonState? person = null,
        GrazerCohortState? cohort = null,
        double vegetationMassKilograms = 0,
        double surfaceWaterKilogramsPerSquareMeter = 100)
    {
        var terrain =
            new PlanetTerrainState(
                fixture.Planet.Id,
                fixture.Definition,
                fixture.Grid.Cells.Select(
                    cell =>
                        new TerrainCellState(
                            cell.Id,
                            elevationMeters: 0)));

        var hydrology =
            new PlanetHydrologyState(
                fixture.Planet.Id,
                fixture.Definition,
                fixture.Grid.Cells.Select(
                    cell =>
                        new HydrologyCellState(
                            cell.Id,
                            atmosphericWaterKilogramsPerSquareMeter:
                                0,
                            surfaceLiquidWaterKilogramsPerSquareMeter:
                                surfaceWaterKilogramsPerSquareMeter,
                            soilWaterKilogramsPerSquareMeter:
                                100,
                            snowIceWaterEquivalentKilogramsPerSquareMeter:
                                0)));

        var vegetation =
            new PlanetVegetationState(
                fixture.Planet.Id,
                fixture.Definition,
                fixture.Grid.Cells.Select(
                    cell =>
                        new VegetationCellState(
                            cell.Id,
                            cell.Id ==
                            fixture.TargetCell.Id
                                ? vegetationMassKilograms /
                                  cell.AreaSquareMeters
                                : 0)));

        var biogeochemistry =
            new PlanetBiogeochemistryState(
                fixture.Planet.Id,
                fixture.Definition,
                fixture.Grid.Cells.Select(
                    cell =>
                        new BiogeochemistryCellState(
                            cell.Id,
                            detritalBiomassKilogramsPerSquareMeter:
                                0,
                            detritalNitrogenKilogramsPerSquareMeter:
                                0,
                            plantAvailableNitrogenKilogramsPerSquareMeter:
                                0)));

        var population =
            person is null
                ? Array.Empty<PersonState>()
                : new[]
                {
                    person
                };

        var cohorts =
            cohort is null
                ? Array.Empty<GrazerCohortState>()
                : new[]
                {
                    cohort
                };

        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [
                fixture.Planet
            ],
            population,
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
            grazerCohorts:
                cohorts,
            biogeochemistry:
            [
                biogeochemistry
            ]);
    }

    private static double RemovedVegetationMassKilograms(
        WorldState before,
        WorldState after,
        SurfaceCell cell)
    {
        var beforeDensity =
            before.Vegetation
                .Single()
                .GetCell(
                    cell.Id)
                .LiveBiomassKilogramsPerSquareMeter;

        var afterDensity =
            after.Vegetation
                .Single()
                .GetCell(
                    cell.Id)
                .LiveBiomassKilogramsPerSquareMeter;

        return
            (beforeDensity - afterDensity) *
            cell.AreaSquareMeters;
    }

    private static void AssertBiogeochemistryMass(
        WorldState world,
        IPlanetSurfaceGrid grid,
        SurfaceCell targetCell,
        double expectedDetritalBiomassKilograms,
        double expectedDetritalNitrogenKilograms,
        double expectedPlantAvailableNitrogenKilograms)
    {
        var biogeochemistry =
            Assert.Single(
                world.Biogeochemistry);

        var target =
            biogeochemistry.GetCell(
                targetCell.Id);

        Assert.Equal(
            expectedDetritalBiomassKilograms,
            target.DetritalBiomassKilogramsPerSquareMeter *
            targetCell.AreaSquareMeters,
            precision: 8);

        Assert.Equal(
            expectedDetritalNitrogenKilograms,
            target.DetritalNitrogenKilogramsPerSquareMeter *
            targetCell.AreaSquareMeters,
            precision: 8);

        Assert.Equal(
            expectedPlantAvailableNitrogenKilograms,
            target.PlantAvailableNitrogenKilogramsPerSquareMeter *
            targetCell.AreaSquareMeters,
            precision: 8);

        var totalDetritalBiomass =
            biogeochemistry.Cells.Sum(
                cell =>
                    cell.DetritalBiomassKilogramsPerSquareMeter *
                    grid.GetCell(
                        cell.CellId)
                    .AreaSquareMeters);

        var totalDetritalNitrogen =
            biogeochemistry.Cells.Sum(
                cell =>
                    cell.DetritalNitrogenKilogramsPerSquareMeter *
                    grid.GetCell(
                        cell.CellId)
                    .AreaSquareMeters);

        var totalAvailableNitrogen =
            biogeochemistry.Cells.Sum(
                cell =>
                    cell.PlantAvailableNitrogenKilogramsPerSquareMeter *
                    grid.GetCell(
                        cell.CellId)
                    .AreaSquareMeters);

        Assert.Equal(
            expectedDetritalBiomassKilograms,
            totalDetritalBiomass,
            precision: 8);

        Assert.Equal(
            expectedDetritalNitrogenKilograms,
            totalDetritalNitrogen,
            precision: 8);

        Assert.Equal(
            expectedPlantAvailableNitrogenKilograms,
            totalAvailableNitrogen,
            precision: 8);
    }

    private sealed record Fixture(
        PlanetState Planet,
        SurfaceGridDefinition Definition,
        IPlanetSurfaceGrid Grid,
        SurfaceCell TargetCell);
}
