using Est.Simulation.Grazers;
using Est.Simulation.Hydrology;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Grazers;

public sealed class GrazerCohortSystemTests
{
    private const long OneDaySeconds =
        86_400;

    [Fact]
    public void Evaluate_DefaultWaterPolicyDoesNotMoveSupportedDryCohortTowardStandingWater()
    {
        var fixture =
            CreateFixture();

        var startCell =
            fixture.Grid.Cells[4];

        var waterNeighbor =
            fixture.Grid.GetNeighbors(
                    startCell.Id)
                .Select(
                    fixture.Grid.GetCell)
                .First();

        var world =
            CreateWorld(
                fixture,
                [
                    CreateCohort(
                        fixture,
                        startCell,
                        100)
                ],
                water:
                    cell =>
                        cell.Id ==
                        waterNeighbor.Id
                            ? 100
                            : 0,
                vegetation:
                    _ =>
                        1);

        var system =
            new GrazerCohortSystem(
                fixture.Planet.Id,
                new GrazerModelParameters(
                    carryingCapacityGrazersPerKilogramLiveVegetationBiomass:
                        1,
                    maximumIntegrationStepSeconds:
                        OneDaySeconds,
                    maximumTravelMetersPerDay:
                        100_000_000,
                    maximumGrazeKilogramsPerGrazerPerDay:
                        0,
                    foodShortageMortalityRatePerDay:
                        0,
                    habitatAbsenceMortalityRatePerDay:
                        0));

        var change =
            system.Evaluate(
                world,
                OneDaySeconds);

        var changed =
            change.Operation.Apply(
                world);

        var cohort =
            Assert.Single(
                changed.GrazerCohorts);

        Assert.Equal(
            startCell.CenterLatitudeDegrees,
            cohort.LatitudeDegrees);

        Assert.Equal(
            startCell.CenterLongitudeDegrees,
            cohort.LongitudeDegrees);

        Assert.Equal(
            0,
            change.Metrics[
                "movementSteps"]);

        Assert.Equal(
            0,
            change.Metrics[
                "waterStressSteps"]);
    }

    [Fact]
    public void Evaluate_MovesCohortTowardBetterAdjacentLandEcology()
    {
        var fixture =
            CreateFixture();

        var startCell =
            fixture.Grid.Cells[4];

        var neighbors =
            fixture.Grid.GetNeighbors(
                    startCell.Id)
                .Select(
                    fixture.Grid.GetCell)
                .ToArray();

        var waterOnlyCell =
            neighbors[0];

        var vegetatedTarget =
            neighbors[1];

        var world =
            CreateWorld(
                fixture,
                [
                    CreateCohort(
                        fixture,
                        startCell,
                        100)
                ],
                water:
                    cell =>
                        cell.Id ==
                        waterOnlyCell.Id
                            ? 100
                            : 0,
                vegetation:
                    cell =>
                    {
                        if (cell.Id ==
                            vegetatedTarget.Id)
                        {
                            return 2;
                        }

                        if (cell.Id ==
                            startCell.Id)
                        {
                            return 1;
                        }

                        return 0;
                    });

        var system =
            new GrazerCohortSystem(
                fixture.Planet.Id,
                new GrazerModelParameters(
                    carryingCapacityGrazersPerKilogramLiveVegetationBiomass:
                        1,
                    maximumIntegrationStepSeconds:
                        OneDaySeconds,
                    maximumTravelMetersPerDay:
                        100_000_000,
                    maximumGrazeKilogramsPerGrazerPerDay:
                        0,
                    foodShortageMortalityRatePerDay:
                        0,
                    waterAbsenceMortalityRatePerDay:
                        0,
                    useSurfaceWaterForMovement:
                        true,
                    habitatAbsenceMortalityRatePerDay:
                        0));

        var change =
            system.Evaluate(
                world,
                OneDaySeconds);

        var changed =
            change.Operation.Apply(
                world);

        var cohort =
            Assert.Single(
                changed.GrazerCohorts);

        Assert.Equal(
            vegetatedTarget.CenterLatitudeDegrees,
            cohort.LatitudeDegrees);

        Assert.Equal(
            vegetatedTarget.CenterLongitudeDegrees,
            cohort.LongitudeDegrees);

        Assert.NotEqual(
            waterOnlyCell.CenterLatitudeDegrees,
            cohort.LatitudeDegrees);

        Assert.Equal(
            1,
            change.Metrics[
                "movementSteps"]);
    }

    [Fact]
    public void Evaluate_GrazingRemovesAuthoritativeVegetationBiomass()
    {
        var fixture =
            CreateFixture();

        var startCell =
            fixture.Grid.Cells[4];

        var initialMassKilograms =
            1_000d;

        var world =
            CreateWorld(
                fixture,
                [
                    CreateCohort(
                        fixture,
                        startCell,
                        10)
                ],
                water:
                    _ =>
                        100,
                vegetation:
                    cell =>
                        cell.Id ==
                        startCell.Id
                            ? initialMassKilograms /
                              cell.AreaSquareMeters
                            : 0);

        var system =
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
                        0));

        var change =
            system.Evaluate(
                world,
                OneDaySeconds);

        Assert.IsType<
            ReplacePlanetGrazerVegetationStateOperation>(
                change.Operation);

        var changed =
            change.Operation.Apply(
                world);

        var before =
            world.Vegetation
                .Single()
                .GetCell(
                    startCell.Id)
                .LiveBiomassKilogramsPerSquareMeter *
            startCell.AreaSquareMeters;

        var after =
            changed.Vegetation
                .Single()
                .GetCell(
                    startCell.Id)
                .LiveBiomassKilogramsPerSquareMeter *
            startCell.AreaSquareMeters;

        Assert.Equal(
            20,
            before - after,
            6);

        Assert.Equal(
            20,
            change.Metrics[
                "biomassGrazedKilograms"],
            6);

        Assert.Equal(
            10,
            Assert.Single(
                    changed.GrazerCohorts)
                .MemberCount);
    }

    [Fact]
    public void Evaluate_CohortsInSameCellShareAvailableVegetation()
    {
        var fixture =
            CreateFixture();

        var startCell =
            fixture.Grid.Cells[4];

        var availableMassKilograms =
            100d;

        var first =
            CreateCohort(
                fixture,
                startCell,
                100);

        var second =
            new GrazerCohortState(
                GrazerCohortId.New(),
                fixture.Planet.Id,
                100,
                startCell.CenterLatitudeDegrees,
                startCell.CenterLongitudeDegrees);

        var world =
            CreateWorld(
                fixture,
                [
                    first,
                    second
                ],
                water:
                    _ =>
                        100,
                vegetation:
                    cell =>
                        cell.Id ==
                        startCell.Id
                            ? availableMassKilograms /
                              cell.AreaSquareMeters
                            : 0);

        var system =
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
                        1,
                    foodShortageMortalityRatePerDay:
                        Math.Log(
                            2),
                    waterAbsenceMortalityRatePerDay:
                        0,
                    habitatAbsenceMortalityRatePerDay:
                        0));

        var changed =
            system.Evaluate(
                    world,
                    OneDaySeconds)
                .Operation
                .Apply(
                    world);

        Assert.Equal(
            2,
            changed.GrazerCohorts.Length);

        Assert.All(
            changed.GrazerCohorts,
            cohort =>
                Assert.Equal(
                    75,
                    cohort.MemberCount));

        Assert.Equal(
            150,
            changed.GrazerCohorts.Sum(
                cohort =>
                    cohort.MemberCount));

        Assert.Equal(
            0,
            changed.Vegetation
                .Single()
                .GetCell(
                    startCell.Id)
                .LiveBiomassKilogramsPerSquareMeter,
            12);
    }

    [Fact]
    public void Evaluate_WaterAbsenceCausesExplicitSurvivalLoss()
    {
        var fixture =
            CreateFixture();

        var startCell =
            fixture.Grid.Cells[4];

        var world =
            CreateWorld(
                fixture,
                [
                    CreateCohort(
                        fixture,
                        startCell,
                        100)
                ],
                water:
                    _ =>
                        0,
                vegetation:
                    _ =>
                        1);

        var system =
            new GrazerCohortSystem(
                fixture.Planet.Id,
                new GrazerModelParameters(
                    maximumIntegrationStepSeconds:
                        OneDaySeconds,
                    maximumTravelMetersPerDay:
                        0,
                    maximumGrazeKilogramsPerGrazerPerDay:
                        0,
                    foodShortageMortalityRatePerDay:
                        0,
                    waterAbsenceMortalityRatePerDay:
                        Math.Log(
                            2),
                    habitatAbsenceMortalityRatePerDay:
                        0));

        var changed =
            system.Evaluate(
                    world,
                    OneDaySeconds)
                .Operation
                .Apply(
                    world);

        Assert.Equal(
            50,
            Assert.Single(
                    changed.GrazerCohorts)
                .MemberCount);
    }

    [Fact]
    public void Evaluate_HabitatAbsenceCausesExplicitSurvivalLoss()
    {
        var fixture =
            CreateFixture();

        var startCell =
            fixture.Grid.Cells[4];

        var world =
            CreateWorld(
                fixture,
                [
                    CreateCohort(
                        fixture,
                        startCell,
                        100)
                ],
                water:
                    _ =>
                        100,
                vegetation:
                    _ =>
                        0);

        var system =
            new GrazerCohortSystem(
                fixture.Planet.Id,
                new GrazerModelParameters(
                    maximumIntegrationStepSeconds:
                        OneDaySeconds,
                    maximumTravelMetersPerDay:
                        0,
                    maximumGrazeKilogramsPerGrazerPerDay:
                        0,
                    foodShortageMortalityRatePerDay:
                        0,
                    waterAbsenceMortalityRatePerDay:
                        0,
                    habitatAbsenceMortalityRatePerDay:
                        Math.Log(
                            2)));

        var changed =
            system.Evaluate(
                    world,
                    OneDaySeconds)
                .Operation
                .Apply(
                    world);

        Assert.Equal(
            50,
            Assert.Single(
                    changed.GrazerCohorts)
                .MemberCount);
    }

    [Fact]
    public void Evaluate_ExtinctionRemovesCohort()
    {
        var fixture =
            CreateFixture();

        var startCell =
            fixture.Grid.Cells[4];

        var world =
            CreateWorld(
                fixture,
                [
                    CreateCohort(
                        fixture,
                        startCell,
                        1)
                ],
                water:
                    _ =>
                        100,
                vegetation:
                    _ =>
                        0);

        var system =
            new GrazerCohortSystem(
                fixture.Planet.Id,
                new GrazerModelParameters(
                    maximumIntegrationStepSeconds:
                        OneDaySeconds,
                    maximumTravelMetersPerDay:
                        0,
                    maximumGrazeKilogramsPerGrazerPerDay:
                        0,
                    foodShortageMortalityRatePerDay:
                        0,
                    waterAbsenceMortalityRatePerDay:
                        0,
                    habitatAbsenceMortalityRatePerDay:
                        Math.Log(
                            2)));

        var change =
            system.Evaluate(
                world,
                OneDaySeconds);

        var changed =
            change.Operation.Apply(
                world);

        Assert.Empty(
            changed.GrazerCohorts);

        Assert.Equal(
            1,
            change.Metrics[
                "extinctCohorts"]);
    }

    private static Fixture CreateFixture()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Grazer Behavior World",
                5.0e20,
                1_000,
                new PlanetEnvironment(
                    285,
                    0.60,
                    0.05,
                    AtmosphereState.Vacuum));

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                3,
                4);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        return new Fixture(
            planet,
            definition,
            grid);
    }

    private static GrazerCohortState CreateCohort(
        Fixture fixture,
        SurfaceCell cell,
        int memberCount)
    {
        return new GrazerCohortState(
            GrazerCohortId.New(),
            fixture.Planet.Id,
            memberCount,
            cell.CenterLatitudeDegrees,
            cell.CenterLongitudeDegrees);
    }

    private static WorldState CreateWorld(
        Fixture fixture,
        IEnumerable<GrazerCohortState> cohorts,
        Func<SurfaceCell, double> water,
        Func<SurfaceCell, double> vegetation)
    {
        var terrain =
            new PlanetTerrainState(
                fixture.Planet.Id,
                fixture.Definition,
                fixture.Grid.Cells.Select(
                    cell =>
                        new TerrainCellState(
                            cell.Id,
                            0)));

        var hydrology =
            new PlanetHydrologyState(
                fixture.Planet.Id,
                fixture.Definition,
                fixture.Grid.Cells.Select(
                    cell =>
                        new HydrologyCellState(
                            cell.Id,
                            0,
                            water(
                                cell),
                            100,
                            0)));

        var vegetationState =
            new PlanetVegetationState(
                fixture.Planet.Id,
                fixture.Definition,
                fixture.Grid.Cells.Select(
                    cell =>
                        new VegetationCellState(
                            cell.Id,
                            vegetation(
                                cell))));

        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [
                fixture.Planet
            ],
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
                vegetationState
            ],
            grazerCohorts:
                cohorts);
    }

    private sealed record Fixture(
        PlanetState Planet,
        SurfaceGridDefinition Definition,
        IPlanetSurfaceGrid Grid);
}
