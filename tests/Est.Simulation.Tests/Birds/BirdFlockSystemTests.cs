using Est.Simulation.Biogeochemistry;
using Est.Simulation.Birds;
using Est.Simulation.Hydrology;
using Est.Simulation.Invertebrates;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Birds;

public sealed class BirdFlockSystemTests
{
    [Fact]
    public void Evaluate_MovesFlockTowardBetterAdjacentEcology()
    {
        var fixture =
            CreateFixture();

        var startCell =
            fixture.Grid.Cells[4];

        var targetCellId =
            fixture.Grid.GetNeighbors(
                    startCell.Id)
                .First();

        var targetCell =
            fixture.Grid.GetCell(
                targetCellId);

        var world =
            CreateWorld(
                fixture,
                startCell,
                memberCount: 100,
                water:
                    cell =>
                        cell.Id ==
                        targetCell.Id
                            ? 100
                            : 0,
                vegetation:
                    _ =>
                        1,
                invertebrates:
                    cell =>
                        cell.Id ==
                        targetCell.Id
                            ? 1
                            : 0);

        var system =
            new BirdFlockSystem(
                fixture.Planet.Id,
                new BirdModelParameters(
                    carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass:
                        1,
                    maximumIntegrationStepSeconds:
                        86_400,
                    maximumTravelMetersPerDay:
                        100_000_000,
                    foodShortageMortalityRatePerDay:
                        0,
                    waterAbsenceMortalityRatePerDay:
                        0,
                    habitatAbsenceMortalityRatePerDay:
                        0));

        var change =
            system.Evaluate(
                world,
                86_400);

        var changed =
            change.Operation.Apply(
                world);

        var flock =
            Assert.Single(
                changed.BirdFlocks);

        Assert.Equal(
            world.BirdFlocks[0].Id,
            flock.Id);

        Assert.Equal(
            targetCell.CenterLatitudeDegrees,
            flock.LatitudeDegrees);

        Assert.Equal(
            targetCell.CenterLongitudeDegrees,
            flock.LongitudeDegrees);

        Assert.Equal(
            1,
            change.Metrics[
                "movementSteps"]);
    }

    [Fact]
    public void Evaluate_SupportedFlockRemainsInPlace()
    {
        var fixture =
            CreateFixture();

        var startCell =
            fixture.Grid.Cells[4];

        var world =
            CreateWorld(
                fixture,
                startCell,
                memberCount: 100,
                water:
                    _ =>
                        100,
                vegetation:
                    _ =>
                        1,
                invertebrates:
                    _ =>
                        1);

        var system =
            new BirdFlockSystem(
                fixture.Planet.Id,
                new BirdModelParameters(
                    carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass:
                        1,
                    maximumIntegrationStepSeconds:
                        86_400,
                    maximumTravelMetersPerDay:
                        100_000_000));

        var changed =
            system.Evaluate(
                    world,
                    86_400)
                .Operation
                .Apply(
                    world);

        var flock =
            Assert.Single(
                changed.BirdFlocks);

        Assert.Equal(
            startCell.CenterLatitudeDegrees,
            flock.LatitudeDegrees);

        Assert.Equal(
            startCell.CenterLongitudeDegrees,
            flock.LongitudeDegrees);

        Assert.Equal(
            100,
            flock.MemberCount);
    }

    [Fact]
    public void Evaluate_FoodShortageReducesOnlyUnsupportedPopulation()
    {
        var fixture =
            CreateFixture();

        var startCell =
            fixture.Grid.Cells[4];

        var world =
            CreateWorld(
                fixture,
                startCell,
                memberCount: 100,
                water:
                    _ =>
                        100,
                vegetation:
                    _ =>
                        1,
                invertebrates:
                    _ =>
                        0);

        var system =
            new BirdFlockSystem(
                fixture.Planet.Id,
                new BirdModelParameters(
                    maximumIntegrationStepSeconds:
                        86_400,
                    maximumTravelMetersPerDay:
                        0,
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
                    86_400)
                .Operation
                .Apply(
                    world);

        var flock =
            Assert.Single(
                changed.BirdFlocks);

        Assert.Equal(
            50,
            flock.MemberCount);

        Assert.Equal(
            world.Invertebrates,
            changed.Invertebrates);
    }

    [Fact]
    public void Evaluate_FlocksInSameCellShareLocalFoodSupport()
    {
        var fixture =
            CreateFixture();

        var startCell =
            fixture.Grid.Cells[4];

        var world =
            CreateWorld(
                fixture,
                startCell,
                memberCount: 100,
                water:
                    _ =>
                        100,
                vegetation:
                    _ =>
                        1,
                invertebrates:
                    _ =>
                        1);

        var first =
            Assert.Single(
                world.BirdFlocks);

        var second =
            new BirdFlockState(
                BirdFlockId.New(),
                fixture.Planet.Id,
                100,
                startCell.CenterLatitudeDegrees,
                startCell.CenterLongitudeDegrees);

        world =
            world.ReplaceBirdFlocks(
            [
                first,
                second
            ]);

        var carryingCapacityRatio =
            100d /
            startCell.AreaSquareMeters;

        var system =
            new BirdFlockSystem(
                fixture.Planet.Id,
                new BirdModelParameters(
                    carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass:
                        carryingCapacityRatio,
                    maximumIntegrationStepSeconds:
                        86_400,
                    maximumTravelMetersPerDay:
                        0,
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
                    86_400)
                .Operation
                .Apply(
                    world);

        Assert.Equal(
            2,
            changed.BirdFlocks.Length);

        Assert.All(
            changed.BirdFlocks,
            flock =>
                Assert.Equal(
                    75,
                    flock.MemberCount));

        Assert.Equal(
            150,
            changed.BirdFlocks.Sum(
                flock =>
                    flock.MemberCount));
    }

    [Fact]
    public void Evaluate_AbundantInvertebratePreyCanRecruitMaterialBackedMember()
    {
        var fixture =
            CreateFixture();

        var startCell =
            fixture.Grid.Cells[4];

        var parameters =
            new BirdModelParameters(
                carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass:
                    10,
                maximumIntegrationStepSeconds:
                    86_400,
                maximumTravelMetersPerDay:
                    0,
                maximumPreyConsumptionKilogramsPerBirdPerDay:
                    2,
                foodShortageMortalityRatePerDay:
                    0,
                waterAbsenceMortalityRatePerDay:
                    0,
                habitatAbsenceMortalityRatePerDay:
                    0,
                liveBiomassKilogramsPerBird:
                    1,
                liveNitrogenKilogramsPerBird:
                    0,
                maximumRecruitmentRatePerDay:
                    0.1);

        var world =
            CreateWorld(
                fixture,
                startCell,
                memberCount:
                    10,
                water:
                    _ =>
                        100,
                vegetation:
                    _ =>
                        1,
                invertebrates:
                    cell =>
                        cell.Id ==
                        startCell.Id
                            ? 20 /
                              cell.AreaSquareMeters
                            : 0);

        var source =
            Assert.Single(
                world.BirdFlocks);

        world =
            world.ReplaceBirdFlocks(
            [
                new BirdFlockState(
                    source.Id,
                    source.PlanetId,
                    source.MemberCount,
                    source.LatitudeDegrees,
                    source.LongitudeDegrees,
                    parameters
                        .MaterialPerBird
                        .ForUnits(
                            source.MemberCount))
            ]);

        var initialPrey =
            Assert.Single(
                    world.Invertebrates)
                .GetCell(
                    startCell.Id)
                .LiveBiomassKilogramsPerSquareMeter *
            startCell.AreaSquareMeters;

        var system =
            new BirdFlockSystem(
                fixture.Planet.Id,
                parameters);

        var change =
            system.Evaluate(
                world,
                86_400);

        var changed =
            change.Operation.Apply(
                world);

        var flock =
            Assert.Single(
                changed.BirdFlocks);

        var finalPrey =
            Assert.Single(
                    changed.Invertebrates)
                .GetCell(
                    startCell.Id)
                .LiveBiomassKilogramsPerSquareMeter *
            startCell.AreaSquareMeters;

        Assert.Equal(
            20,
            initialPrey,
            6);

        Assert.Equal(
            11,
            flock.MemberCount);

        Assert.Equal(
            11,
            flock.Material.LiveBiomassKilograms,
            6);

        Assert.Equal(
            0,
            finalPrey,
            6);

        Assert.Equal(
            1,
            change.Metrics[
                "recruitedMembers"]);
    }

    [Fact]
    public void Evaluate_NitrogenBearingPreyRecruitmentConservesTrackedMaterial()
    {
        var fixture =
            CreateFixture();

        var startCell =
            fixture.Grid.Cells[4];

        var parameters =
            new BirdModelParameters(
                carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass:
                    10,
                maximumIntegrationStepSeconds:
                    86_400,
                maximumTravelMetersPerDay:
                    0,
                maximumPreyConsumptionKilogramsPerBirdPerDay:
                    2,
                foodShortageMortalityRatePerDay:
                    0,
                waterAbsenceMortalityRatePerDay:
                    0,
                habitatAbsenceMortalityRatePerDay:
                    0,
                liveBiomassKilogramsPerBird:
                    1,
                liveNitrogenKilogramsPerBird:
                    0.025,
                maximumRecruitmentRatePerDay:
                    0.1);

        var world =
            CreateWorld(
                fixture,
                startCell,
                memberCount:
                    10,
                water:
                    _ =>
                        100,
                vegetation:
                    _ =>
                        1,
                invertebrates:
                    cell =>
                        cell.Id ==
                        startCell.Id
                            ? 20 /
                              cell.AreaSquareMeters
                            : 0);

        world =
            world.ReplaceInvertebrates(
            [
                new PlanetInvertebrateState(
                    fixture.Planet.Id,
                    fixture.GridDefinition,
                    fixture.Grid.Cells.Select(
                        cell =>
                            new InvertebrateCellState(
                                cell.Id,
                                cell.Id ==
                                startCell.Id
                                    ? 20 /
                                      cell.AreaSquareMeters
                                    : 0,
                                cell.Id ==
                                startCell.Id
                                    ? 1 /
                                      cell.AreaSquareMeters
                                    : 0)))
            ]);

        world =
            world.ReplaceBiogeochemistry(
            [
                new PlanetBiogeochemistryState(
                    fixture.Planet.Id,
                    fixture.GridDefinition,
                    fixture.Grid.Cells.Select(
                        cell =>
                            new BiogeochemistryCellState(
                                cell.Id,
                                detritalBiomassKilogramsPerSquareMeter:
                                    0,
                                detritalNitrogenKilogramsPerSquareMeter:
                                    0,
                                plantAvailableNitrogenKilogramsPerSquareMeter:
                                    0)))
            ]);

        var source =
            Assert.Single(
                world.BirdFlocks);

        world =
            world.ReplaceBirdFlocks(
            [
                new BirdFlockState(
                    source.Id,
                    source.PlanetId,
                    source.MemberCount,
                    source.LatitudeDegrees,
                    source.LongitudeDegrees,
                    parameters
                        .MaterialPerBird
                        .ForUnits(
                            source.MemberCount))
            ]);

        var system =
            new BirdFlockSystem(
                fixture.Planet.Id,
                parameters);

        var change =
            system.Evaluate(
                world,
                86_400);

        var changed =
            change.Operation.Apply(
                world);

        var flock =
            Assert.Single(
                changed.BirdFlocks);

        var prey =
            Assert.Single(
                    changed.Invertebrates)
                .GetCell(
                    startCell.Id);

        var biogeochemistry =
            Assert.Single(
                    changed.Biogeochemistry)
                .GetCell(
                    startCell.Id);

        Assert.Equal(
            11,
            flock.MemberCount);

        Assert.Equal(
            11,
            flock.Material.LiveBiomassKilograms,
            8);

        Assert.Equal(
            0.275,
            flock.Material.LiveNitrogenKilograms,
            8);

        Assert.Equal(
            0,
            prey.LiveBiomassKilogramsPerSquareMeter *
            startCell.AreaSquareMeters,
            8);

        Assert.Equal(
            0,
            prey.LiveNitrogenKilogramsPerSquareMeter *
            startCell.AreaSquareMeters,
            8);

        Assert.Equal(
            0.975,
            biogeochemistry
                .PlantAvailableNitrogenKilogramsPerSquareMeter *
            startCell.AreaSquareMeters,
            8);

        Assert.Equal(
            20,
            change.Metrics[
                "preyBiomassConsumedKilograms"],
            8);

        Assert.Equal(
            1,
            change.Metrics[
                "preyNitrogenConsumedKilograms"],
            8);

        Assert.Equal(
            1,
            change.Metrics[
                "preyBiomassAssimilatedKilograms"],
            8);

        Assert.Equal(
            0.025,
            change.Metrics[
                "preyNitrogenAssimilatedKilograms"],
            8);

        Assert.Equal(
            19,
            change.Metrics[
                "preyBiomassRespiredKilograms"],
            8);

        Assert.Equal(
            0.975,
            change.Metrics[
                "preyNitrogenReturnedKilograms"],
            8);
    }

    [Fact]
    public void Evaluate_FractionalRecruitmentAccumulatesAcrossAdvances()
    {
        var fixture =
            CreateFixture();

        var startCell =
            fixture.Grid.Cells[4];

        var parameters =
            new BirdModelParameters(
                carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass:
                    100,
                maximumIntegrationStepSeconds:
                    86_400,
                maximumTravelMetersPerDay:
                    0,
                maximumPreyConsumptionKilogramsPerBirdPerDay:
                    2,
                foodShortageMortalityRatePerDay:
                    0,
                waterAbsenceMortalityRatePerDay:
                    0,
                habitatAbsenceMortalityRatePerDay:
                    0,
                liveBiomassKilogramsPerBird:
                    1,
                liveNitrogenKilogramsPerBird:
                    0,
                maximumRecruitmentRatePerDay:
                    0.05);

        var world =
            CreateWorld(
                fixture,
                startCell,
                memberCount:
                    10,
                water:
                    _ =>
                        100,
                vegetation:
                    _ =>
                        1,
                invertebrates:
                    cell =>
                        cell.Id ==
                        startCell.Id
                            ? 100 /
                              cell.AreaSquareMeters
                            : 0);

        var source =
            Assert.Single(
                world.BirdFlocks);

        world =
            world.ReplaceBirdFlocks(
            [
                new BirdFlockState(
                    source.Id,
                    source.PlanetId,
                    source.MemberCount,
                    source.LatitudeDegrees,
                    source.LongitudeDegrees,
                    parameters
                        .MaterialPerBird
                        .ForUnits(
                            source.MemberCount))
            ]);

        var system =
            new BirdFlockSystem(
                fixture.Planet.Id,
                parameters);

        var firstChange =
            system.Evaluate(
                world,
                86_400);

        var afterFirst =
            firstChange.Operation.Apply(
                world);

        var firstFlock =
            Assert.Single(
                afterFirst.BirdFlocks);

        Assert.Equal(
            10,
            firstFlock.MemberCount);

        Assert.Equal(
            0.5,
            firstFlock.RecruitmentAccumulator,
            6);

        Assert.Equal(
            0,
            firstChange.Metrics[
                "recruitedMembers"]);

        var secondChange =
            system.Evaluate(
                afterFirst,
                86_400);

        var afterSecond =
            secondChange.Operation.Apply(
                afterFirst);

        var secondFlock =
            Assert.Single(
                afterSecond.BirdFlocks);

        Assert.Equal(
            11,
            secondFlock.MemberCount);

        Assert.Equal(
            11,
            secondFlock.Material.LiveBiomassKilograms,
            6);

        Assert.Equal(
            0,
            secondFlock.RecruitmentAccumulator,
            6);

        Assert.Equal(
            1,
            secondChange.Metrics[
                "recruitedMembers"]);
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
                startCell,
                memberCount: 100,
                water:
                    _ =>
                        0,
                vegetation:
                    _ =>
                        1,
                invertebrates:
                    _ =>
                        1);

        var system =
            new BirdFlockSystem(
                fixture.Planet.Id,
                new BirdModelParameters(
                    carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass:
                        1,
                    maximumIntegrationStepSeconds:
                        86_400,
                    maximumTravelMetersPerDay:
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
                    86_400)
                .Operation
                .Apply(
                    world);

        Assert.Equal(
            50,
            Assert.Single(
                    changed.BirdFlocks)
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
                startCell,
                memberCount: 100,
                water:
                    _ =>
                        100,
                vegetation:
                    _ =>
                        0,
                invertebrates:
                    _ =>
                        1);

        var system =
            new BirdFlockSystem(
                fixture.Planet.Id,
                new BirdModelParameters(
                    carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass:
                        1,
                    maximumIntegrationStepSeconds:
                        86_400,
                    maximumTravelMetersPerDay:
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
                    86_400)
                .Operation
                .Apply(
                    world);

        Assert.Equal(
            50,
            Assert.Single(
                    changed.BirdFlocks)
                .MemberCount);
    }

    [Fact]
    public void Evaluate_ExtinctionRemovesFlock()
    {
        var fixture =
            CreateFixture();

        var startCell =
            fixture.Grid.Cells[4];

        var world =
            CreateWorld(
                fixture,
                startCell,
                memberCount: 1,
                water:
                    _ =>
                        100,
                vegetation:
                    _ =>
                        1,
                invertebrates:
                    _ =>
                        0);

        var system =
            new BirdFlockSystem(
                fixture.Planet.Id,
                new BirdModelParameters(
                    maximumIntegrationStepSeconds:
                        86_400,
                    maximumTravelMetersPerDay:
                        0,
                    foodShortageMortalityRatePerDay:
                        Math.Log(
                            2),
                    waterAbsenceMortalityRatePerDay:
                        0,
                    habitatAbsenceMortalityRatePerDay:
                        0));

        var change =
            system.Evaluate(
                world,
                86_400);

        var changed =
            change.Operation.Apply(
                world);

        Assert.Empty(
            changed.BirdFlocks);

        Assert.Equal(
            1,
            change.Metrics[
                "extinctFlocks"]);
    }

    private static Fixture CreateFixture()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Bird Behavior World",
                5.0e24,
                6_000_000,
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

    private static WorldState CreateWorld(
        Fixture fixture,
        SurfaceCell startCell,
        int memberCount,
        Func<SurfaceCell, double> water,
        Func<SurfaceCell, double> vegetation,
        Func<SurfaceCell, double> invertebrates)
    {
        var terrainState =
            new PlanetTerrainState(
                fixture.Planet.Id,
                fixture.GridDefinition,
                fixture.Grid.Cells.Select(
                    cell =>
                        new TerrainCellState(
                            cell.Id,
                            0)));

        var hydrologyState =
            new PlanetHydrologyState(
                fixture.Planet.Id,
                fixture.GridDefinition,
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
                fixture.GridDefinition,
                fixture.Grid.Cells.Select(
                    cell =>
                        new VegetationCellState(
                            cell.Id,
                            vegetation(
                                cell))));

        var invertebrateState =
            new PlanetInvertebrateState(
                fixture.Planet.Id,
                fixture.GridDefinition,
                fixture.Grid.Cells.Select(
                    cell =>
                        new InvertebrateCellState(
                            cell.Id,
                            invertebrates(
                                cell))));

        var flock =
            new BirdFlockState(
                BirdFlockId.New(),
                fixture.Planet.Id,
                memberCount,
                startCell.CenterLatitudeDegrees,
                startCell.CenterLongitudeDegrees);

        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [fixture.Planet],
            [],
            terrain:
            [
                terrainState
            ],
            hydrology:
            [
                hydrologyState
            ],
            vegetation:
            [
                vegetationState
            ],
            invertebrates:
            [
                invertebrateState
            ],
            birdFlocks:
            [
                flock
            ]);
    }

    private sealed record Fixture(
        PlanetState Planet,
        SurfaceGridDefinition GridDefinition,
        IPlanetSurfaceGrid Grid);
}
