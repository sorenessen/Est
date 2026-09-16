using Est.Simulation.Animals;
using Est.Simulation.Biogeochemistry;
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

public sealed class PredationMaterialAccountingTests
{
    private const long OneDaySeconds =
        86_400;

    [Fact]
    public void GrazerPredation_ReturnsConsumedPreyNitrogenAndPreservesRemainingMaterial()
    {
        var fixture =
            CreateFixture();

        var wolf =
            CreateWolf(
                fixture.Planet.Id,
                latitude:
                    fixture.TargetCell.CenterLatitudeDegrees,
                longitude:
                    fixture.TargetCell.CenterLongitudeDegrees,
                energyReserve: 0.20);

        var cohort =
            new GrazerCohortState(
                GrazerCohortId.New(),
                fixture.Planet.Id,
                memberCount: 5,
                latitudeDegrees:
                    fixture.TargetCell.CenterLatitudeDegrees,
                longitudeDegrees:
                    fixture.TargetCell.CenterLongitudeDegrees,
                material:
                    new OrganismMaterialState(
                        liveBiomassKilograms: 1_250,
                        liveNitrogenKilograms: 31.25));

        var world =
            CreateWorld(
                fixture,
                animals:
                [
                    wolf
                ],
                grazerCohorts:
                [
                    cohort
                ]);

        var change =
            new WolfPredatorSystem(
                    fixture.Planet.Id)
                .Evaluate(
                    world,
                    OneDaySeconds);

        var changed =
            change.Operation.Apply(
                world);

        var remaining =
            Assert.Single(
                changed.GrazerCohorts);

        Assert.Equal(
            4,
            remaining.MemberCount);

        Assert.Equal(
            1_000,
            remaining.Material.LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            25,
            remaining.Material.LiveNitrogenKilograms,
            precision: 10);

        Assert.Equal(
            250,
            change.Metrics[
                "preyBiomassRespiredKilograms"],
            precision: 10);

        Assert.Equal(
            6.25,
            change.Metrics[
                "preyNitrogenReturnedKilograms"],
            precision: 10);

        AssertBiogeochemistryMass(
            changed,
            fixture.Grid,
            fixture.TargetCell,
            expectedDetritalBiomassKilograms: 0,
            expectedDetritalNitrogenKilograms: 0,
            expectedPlantAvailableNitrogenKilograms:
                6.25);

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
    public void GrazerPredation_AssimilatesNearbyJuvenileGrowthBeforeReturningRemainder()
    {
        var fixture =
            CreateFixture();

        var parameters =
            new WolfLifecycleParameters();

        var adult =
            new AnimalState(
                new AnimalId(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000301")),
                fixture.Planet.Id,
                AnimalSpecies.Wolf,
                fixture.TargetCell
                    .CenterLatitudeDegrees,
                fixture.TargetCell
                    .CenterLongitudeDegrees,
                energyReserve: 0.20,
                health: 1,
                activity:
                    AnimalActivity.Hunting,
                material:
                    parameters.MatureMaterial
                        .ForUnits(1),
                birthTimeSeconds:
                    -4 *
                    WolfLifecycleParameters
                        .SecondsPerYear);

        var juvenileId =
            new AnimalId(
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000302"));

        var juvenileAgeSeconds =
            30 *
            WolfLifecycleParameters
                .SecondsPerDay;

        var juvenile =
            new AnimalState(
                juvenileId,
                fixture.Planet.Id,
                AnimalSpecies.Wolf,
                fixture.TargetCell
                    .CenterLatitudeDegrees,
                fixture.TargetCell
                    .CenterLongitudeDegrees,
                energyReserve: 0.40,
                health: 1,
                activity:
                    AnimalActivity.Idle,
                material:
                    parameters
                        .MaterialTargetAtAgeSeconds(
                            juvenileAgeSeconds),
                birthTimeSeconds:
                    -juvenileAgeSeconds,
                parentId:
                    adult.Id);

        var cohort =
            new GrazerCohortState(
                GrazerCohortId.New(),
                fixture.Planet.Id,
                memberCount: 1,
                latitudeDegrees:
                    fixture.TargetCell
                        .CenterLatitudeDegrees,
                longitudeDegrees:
                    fixture.TargetCell
                        .CenterLongitudeDegrees,
                material:
                    new OrganismMaterialState(
                        liveBiomassKilograms: 250,
                        liveNitrogenKilograms: 6.25));

        var world =
            CreateWorld(
                fixture,
                animals:
                [
                    adult,
                    juvenile
                ],
                grazerCohorts:
                [
                    cohort
                ]);

        var change =
            new WolfPredatorSystem(
                    fixture.Planet.Id,
                    parameters)
                .Evaluate(
                    world,
                    OneDaySeconds);

        var changed =
            change.Operation.Apply(
                world);

        Assert.Empty(
            changed.GrazerCohorts);

        var grownJuvenile =
            Assert.Single(
                changed.Animals.Where(
                    animal =>
                        animal.Id ==
                        juvenileId));

        var expectedTarget =
            parameters
                .MaterialTargetAtAgeSeconds(
                    juvenileAgeSeconds +
                    OneDaySeconds);

        Assert.Equal(
            expectedTarget
                .LiveBiomassKilograms,
            grownJuvenile.Material
                .LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            expectedTarget
                .LiveNitrogenKilograms,
            grownJuvenile.Material
                .LiveNitrogenKilograms,
            precision: 10);

        var initialTarget =
            parameters
                .MaterialTargetAtAgeSeconds(
                    juvenileAgeSeconds);

        var expectedAssimilatedBiomass =
            expectedTarget
                .LiveBiomassKilograms -
            initialTarget
                .LiveBiomassKilograms;

        var expectedAssimilatedNitrogen =
            expectedTarget
                .LiveNitrogenKilograms -
            initialTarget
                .LiveNitrogenKilograms;

        Assert.Equal(
            expectedAssimilatedBiomass,
            change.Metrics[
                "preyBiomassAssimilatedKilograms"],
            precision: 10);

        Assert.Equal(
            expectedAssimilatedNitrogen,
            change.Metrics[
                "preyNitrogenAssimilatedKilograms"],
            precision: 10);

        Assert.Equal(
            250 -
            expectedAssimilatedBiomass,
            change.Metrics[
                "preyBiomassRespiredKilograms"],
            precision: 10);

        Assert.Equal(
            6.25 -
            expectedAssimilatedNitrogen,
            change.Metrics[
                "preyNitrogenReturnedKilograms"],
            precision: 10);

        AssertBiogeochemistryMass(
            changed,
            fixture.Grid,
            fixture.TargetCell,
            expectedDetritalBiomassKilograms: 0,
            expectedDetritalNitrogenKilograms: 0,
            expectedPlantAvailableNitrogenKilograms:
                6.25 -
                expectedAssimilatedNitrogen);
    }

    [Fact]
    public void GrazerPredation_AllowsAdultWolfToRecoverMaterialUpToMatureTarget()
    {
        var fixture =
            CreateFixture();

        var parameters =
            new WolfLifecycleParameters();

        var wolf =
            new AnimalState(
                new AnimalId(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000401")),
                fixture.Planet.Id,
                AnimalSpecies.Wolf,
                fixture.TargetCell
                    .CenterLatitudeDegrees,
                fixture.TargetCell
                    .CenterLongitudeDegrees,
                energyReserve: 0.20,
                health: 1,
                activity:
                    AnimalActivity.Hunting,
                material:
                    new OrganismMaterialState(
                        liveBiomassKilograms: 38,
                        liveNitrogenKilograms: 0.95),
                birthTimeSeconds:
                    -4 *
                    WolfLifecycleParameters
                        .SecondsPerYear,
                wolfLifecycle:
                    new WolfLifecycleState(
                        WolfSex.Female));

        var cohort =
            new GrazerCohortState(
                GrazerCohortId.New(),
                fixture.Planet.Id,
                memberCount: 1,
                latitudeDegrees:
                    fixture.TargetCell
                        .CenterLatitudeDegrees,
                longitudeDegrees:
                    fixture.TargetCell
                        .CenterLongitudeDegrees,
                material:
                    new OrganismMaterialState(
                        liveBiomassKilograms: 250,
                        liveNitrogenKilograms: 6.25));

        var world =
            CreateWorld(
                fixture,
                animals:
                [
                    wolf
                ],
                grazerCohorts:
                [
                    cohort
                ]);

        var change =
            new WolfPredatorSystem(
                    fixture.Planet.Id,
                    parameters)
                .Evaluate(
                    world,
                    OneDaySeconds);

        var changed =
            change.Operation.Apply(
                world);

        var recoveredWolf =
            Assert.Single(
                changed.Animals);

        Assert.Equal(
            40,
            recoveredWolf.Material
                .LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            1,
            recoveredWolf.Material
                .LiveNitrogenKilograms,
            precision: 10);

        Assert.Equal(
            2,
            change.Metrics[
                "preyBiomassAssimilatedKilograms"],
            precision: 10);

        Assert.Equal(
            0.05,
            change.Metrics[
                "preyNitrogenAssimilatedKilograms"],
            precision: 10);

        Assert.Equal(
            248,
            change.Metrics[
                "preyBiomassRespiredKilograms"],
            precision: 10);

        Assert.Equal(
            6.20,
            change.Metrics[
                "preyNitrogenReturnedKilograms"],
            precision: 10);

        AssertBiogeochemistryMass(
            changed,
            fixture.Grid,
            fixture.TargetCell,
            expectedDetritalBiomassKilograms: 0,
            expectedDetritalNitrogenKilograms: 0,
            expectedPlantAvailableNitrogenKilograms:
                6.20);
    }

    [Fact]
    public void HumanPredation_ReturnsConsumedPreyNitrogenWithoutChangingWolfMaterial()
    {
        var fixture =
            CreateFixture();

        var person =
            new PersonState(
                new PersonId(
                    Guid.Parse(
                        "ce8847d3-dbf8-428b-ad2d-7a98901789fd")),
                fixture.Planet.Id,
                PersonSex.Female,
                birthTimeSeconds: -800_000_000,
                fixture.TargetCell.CenterLatitudeDegrees,
                fixture.TargetCell.CenterLongitudeDegrees,
                material:
                    new OrganismMaterialState(
                        liveBiomassKilograms: 70,
                        liveNitrogenKilograms: 1.75));

        var wolf =
            new AnimalState(
                new AnimalId(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000301")),
                fixture.Planet.Id,
                AnimalSpecies.Wolf,
                fixture.TargetCell.CenterLatitudeDegrees,
                fixture.TargetCell.CenterLongitudeDegrees,
                energyReserve: 0,
                health: 1,
                activity: AnimalActivity.Hunting,
                material:
                    new OrganismMaterialState(
                        liveBiomassKilograms: 40,
                        liveNitrogenKilograms: 1),
                birthTimeSeconds:
                    -4 *
                    WolfLifecycleParameters
                        .SecondsPerYear);

        var world =
            CreateWorld(
                fixture,
                population:
                [
                    person
                ],
                animals:
                [
                    wolf
                ]);

        var change =
            new WolfPredatorSystem(
                    fixture.Planet.Id)
                .Evaluate(
                    world,
                    6 * OneDaySeconds);

        var changed =
            change.Operation.Apply(
                world);

        Assert.Empty(
            changed.Population);

        var changedWolf =
            Assert.Single(
                changed.Animals);

        Assert.Equal(
            40,
            changedWolf.Material.LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            1,
            changedWolf.Material.LiveNitrogenKilograms,
            precision: 10);

        Assert.Equal(
            1,
            change.Metrics[
                "predationDeaths"]);

        Assert.Equal(
            70,
            change.Metrics[
                "preyBiomassRespiredKilograms"],
            precision: 10);

        Assert.Equal(
            1.75,
            change.Metrics[
                "preyNitrogenReturnedKilograms"],
            precision: 10);

        AssertBiogeochemistryMass(
            changed,
            fixture.Grid,
            fixture.TargetCell,
            expectedDetritalBiomassKilograms: 0,
            expectedDetritalNitrogenKilograms: 0,
            expectedPlantAvailableNitrogenKilograms:
                1.75);

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
                "Predation Material World",
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
        PersonState[]? population = null,
        AnimalState[]? animals = null,
        GrazerCohortState[]? grazerCohorts = null)
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
                                100,
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
                            liveBiomassKilogramsPerSquareMeter:
                                0)));

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

        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [
                fixture.Planet
            ],
            population ??
            [],
            animals ??
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
            grazerCohorts:
                grazerCohorts ??
                [],
            biogeochemistry:
            [
                biogeochemistry
            ]);
    }

    private static AnimalState CreateWolf(
        PlanetId planetId,
        double latitude,
        double longitude,
        double energyReserve)
    {
        return new AnimalState(
            new AnimalId(
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000301")),
            planetId,
            AnimalSpecies.Wolf,
            latitude,
            longitude,
            energyReserve,
            health: 1,
            activity: AnimalActivity.Hunting,
            material:
                new OrganismMaterialState(
                    liveBiomassKilograms: 40,
                    liveNitrogenKilograms: 1),
            birthTimeSeconds:
                -4 * 31_536_000L);
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
