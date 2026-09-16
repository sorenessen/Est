using Est.Simulation.Animals;
using Est.Simulation.Biogeochemistry;
using Est.Simulation.Birds;
using Est.Simulation.Grazers;
using Est.Simulation.Hydrology;
using Est.Simulation.Invertebrates;
using Est.Simulation.Organisms;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Organisms;

public sealed class OrganismMortalityDetritusIntegrationTests
{
    private const long OneDaySeconds =
        86_400;

    private const long OneYearSeconds =
        31_536_000;

    [Fact]
    public void PopulationMortality_AtomicallyTransfersRemainingMaterialIntoDeathCellDetritus()
    {
        var fixture =
            CreateFixture();

        var person =
            new PersonState(
                new PersonId(
                    Guid.Parse(
                        "10000000-0000-0000-0000-000000000101")),
                fixture.Planet.Id,
                PersonSex.Female,
                -90 * OneYearSeconds,
                10,
                20,
                material:
                    new OrganismMaterialState(
                        liveBiomassKilograms: 70,
                        liveNitrogenKilograms: 1.75));

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [
                    fixture.Planet
                ],
                [
                    person
                ],
                terrain:
                [
                    CreateTerrain(
                        fixture)
                ],
                hydrology:
                [
                    CreateHydrology(
                        fixture,
                        surfaceWater: 0)
                ],
                biogeochemistry:
                [
                    CreateEmptyBiogeochemistry(
                        fixture)
                ]);

        var deathCell =
            fixture.Grid.LocateCell(
                person.LatitudeDegrees,
                person.LongitudeDegrees);

        var change =
            new PopulationSystem(
                    fixture.Planet.Id,
                    new PopulationModelParameters(
                        seed: 7,
                        annualBirthRatePerEligibleFemale: 0,
                        annualAdultMigrationRate: 0,
                        annualBaseMortalityRate: 0,
                        annualElderMortalityRate: 100))
                .Evaluate(
                    world,
                    OneYearSeconds);

        var changed =
            change.Operation.Apply(
                world);

        Assert.Single(
            world.Population);

        Assert.Empty(
            changed.Population);

        AssertDetritalMaterial(
            world,
            fixture.Grid,
            deathCell,
            expectedBiomassKilograms: 0,
            expectedNitrogenKilograms: 0);

        AssertDetritalMaterial(
            changed,
            fixture.Grid,
            deathCell,
            expectedBiomassKilograms: 70,
            expectedNitrogenKilograms: 1.75);
    }

    [Fact]
    public void WolfStarvation_AtomicallyTransfersRemainingMaterialIntoDeathCellDetritus()
    {
        var fixture =
            CreateFixture();

        var person =
            new PersonState(
                new PersonId(
                    Guid.Parse(
                        "10000000-0000-0000-0000-000000000102")),
                fixture.Planet.Id,
                PersonSex.Female,
                -800_000_000,
                0,
                10);

        var wolf =
            new AnimalState(
                new AnimalId(
                    Guid.Parse(
                        "20000000-0000-0000-0000-000000000101")),
                fixture.Planet.Id,
                AnimalSpecies.Wolf,
                latitudeDegrees: 0,
                longitudeDegrees: 0,
                energyReserve: 0.05,
                health: 1,
                activity:
                    AnimalActivity.Hunting,
                material:
                    new OrganismMaterialState(
                        liveBiomassKilograms: 40,
                        liveNitrogenKilograms: 1));

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [
                    fixture.Planet
                ],
                [
                    person
                ],
                animals:
                [
                    wolf
                ],
                terrain:
                [
                    CreateTerrain(
                        fixture)
                ],
                hydrology:
                [
                    CreateHydrology(
                        fixture,
                        surfaceWater: 0)
                ],
                biogeochemistry:
                [
                    CreateEmptyBiogeochemistry(
                        fixture)
                ]);

        var deathCell =
            fixture.Grid.LocateCell(
                wolf.LatitudeDegrees,
                wolf.LongitudeDegrees);

        var change =
            new WolfPredatorSystem(
                    fixture.Planet.Id)
                .Evaluate(
                    world,
                    30 * OneDaySeconds);

        var changed =
            change.Operation.Apply(
                world);

        Assert.Single(
            world.Animals);

        Assert.Empty(
            changed.Animals);

        Assert.Single(
            changed.Population);

        AssertDetritalMaterial(
            changed,
            fixture.Grid,
            deathCell,
            expectedBiomassKilograms: 40,
            expectedNitrogenKilograms: 1);
    }

    [Fact]
    public void BirdMortality_AtomicallyTransfersRemovedAggregateMaterialIntoDeathCellDetritus()
    {
        var fixture =
            CreateFixture();

        var startCell =
            fixture.Grid.Cells[4];

        var flock =
            new BirdFlockState(
                BirdFlockId.New(),
                fixture.Planet.Id,
                memberCount: 100,
                startCell.CenterLatitudeDegrees,
                startCell.CenterLongitudeDegrees,
                material:
                    new OrganismMaterialState(
                        liveBiomassKilograms: 100,
                        liveNitrogenKilograms: 2.5));

        var world =
            CreateBirdWorld(
                fixture,
                flock);

        var change =
            new BirdFlockSystem(
                    fixture.Planet.Id,
                    new BirdModelParameters(
                        maximumIntegrationStepSeconds:
                            OneDaySeconds,
                        maximumTravelMetersPerDay:
                            0,
                        foodShortageMortalityRatePerDay:
                            Math.Log(
                                2),
                        waterAbsenceMortalityRatePerDay:
                            0,
                        habitatAbsenceMortalityRatePerDay:
                            0))
                .Evaluate(
                    world,
                    OneDaySeconds);

        var changed =
            change.Operation.Apply(
                world);

        var survivor =
            Assert.Single(
                changed.BirdFlocks);

        Assert.Equal(
            50,
            survivor.MemberCount);

        Assert.Equal(
            50,
            survivor.Material.LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            1.25,
            survivor.Material.LiveNitrogenKilograms,
            precision: 10);

        Assert.Equal(
            100,
            Assert.Single(
                    world.BirdFlocks)
                .Material
                .LiveBiomassKilograms,
            precision: 10);

        AssertDetritalMaterial(
            changed,
            fixture.Grid,
            startCell,
            expectedBiomassKilograms: 50,
            expectedNitrogenKilograms: 1.25);
    }

    [Fact]
    public void GrazerMortality_AtomicallyTransfersRemovedAggregateMaterialIntoDeathCellDetritus()
    {
        var fixture =
            CreateFixture();

        var startCell =
            fixture.Grid.Cells[4];

        var cohort =
            new GrazerCohortState(
                GrazerCohortId.New(),
                fixture.Planet.Id,
                memberCount: 100,
                startCell.CenterLatitudeDegrees,
                startCell.CenterLongitudeDegrees,
                material:
                    new OrganismMaterialState(
                        liveBiomassKilograms: 1_000,
                        liveNitrogenKilograms: 25));

        var world =
            CreateGrazerWorld(
                fixture,
                cohort);

        var change =
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
                                2)))
                .Evaluate(
                    world,
                    OneDaySeconds);

        var changed =
            change.Operation.Apply(
                world);

        var survivor =
            Assert.Single(
                changed.GrazerCohorts);

        Assert.Equal(
            50,
            survivor.MemberCount);

        Assert.Equal(
            500,
            survivor.Material.LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            12.5,
            survivor.Material.LiveNitrogenKilograms,
            precision: 10);

        Assert.Equal(
            1_000,
            Assert.Single(
                    world.GrazerCohorts)
                .Material
                .LiveBiomassKilograms,
            precision: 10);

        AssertDetritalMaterial(
            changed,
            fixture.Grid,
            startCell,
            expectedBiomassKilograms: 500,
            expectedNitrogenKilograms: 12.5);
    }

    private static WorldState CreateBirdWorld(
        Fixture fixture,
        BirdFlockState flock)
    {
        var hydrology =
            CreateHydrology(
                fixture,
                surfaceWater:
                    100);

        var vegetation =
            CreateVegetation(
                fixture,
                liveBiomass:
                    1);

        var invertebrates =
            new PlanetInvertebrateState(
                fixture.Planet.Id,
                fixture.Definition,
                fixture.Grid.Cells.Select(
                    cell =>
                        new InvertebrateCellState(
                            cell.Id,
                            0)));

        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [
                fixture.Planet
            ],
            [],
            terrain:
            [
                CreateTerrain(
                    fixture)
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
            birdFlocks:
            [
                flock
            ],
            biogeochemistry:
            [
                CreateEmptyBiogeochemistry(
                    fixture)
            ]);
    }

    private static WorldState CreateGrazerWorld(
        Fixture fixture,
        GrazerCohortState cohort)
    {
        var hydrology =
            CreateHydrology(
                fixture,
                surfaceWater:
                    100);

        var vegetation =
            CreateVegetation(
                fixture,
                liveBiomass:
                    0);

        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [
                fixture.Planet
            ],
            [],
            terrain:
            [
                CreateTerrain(
                    fixture)
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
            [
                cohort
            ],
            biogeochemistry:
            [
                CreateEmptyBiogeochemistry(
                    fixture)
            ]);
    }

    private static PlanetTerrainState CreateTerrain(
        Fixture fixture)
    {
        return new PlanetTerrainState(
            fixture.Planet.Id,
            fixture.Definition,
            fixture.Grid.Cells.Select(
                (cell, index) =>
                    new TerrainCellState(
                        cell.Id,
                        index)));
    }

    private static PlanetHydrologyState CreateHydrology(
        Fixture fixture,
        double surfaceWater)
    {
        return new PlanetHydrologyState(
            fixture.Planet.Id,
            fixture.Definition,
            fixture.Grid.Cells.Select(
                cell =>
                    new HydrologyCellState(
                        cell.Id,
                        0,
                        surfaceWater,
                        100,
                        0)));
    }

    private static PlanetVegetationState CreateVegetation(
        Fixture fixture,
        double liveBiomass)
    {
        return new PlanetVegetationState(
            fixture.Planet.Id,
            fixture.Definition,
            fixture.Grid.Cells.Select(
                cell =>
                    new VegetationCellState(
                        cell.Id,
                        liveBiomass)));
    }

    private static PlanetBiogeochemistryState
        CreateEmptyBiogeochemistry(
            Fixture fixture)
    {
        return new PlanetBiogeochemistryState(
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
    }

    private static void AssertDetritalMaterial(
        WorldState world,
        IPlanetSurfaceGrid grid,
        SurfaceCell expectedCell,
        double expectedBiomassKilograms,
        double expectedNitrogenKilograms)
    {
        var biogeochemistry =
            Assert.Single(
                world.Biogeochemistry);

        var target =
            biogeochemistry.GetCell(
                expectedCell.Id);

        Assert.Equal(
            expectedBiomassKilograms,
            target.DetritalBiomassKilogramsPerSquareMeter *
            expectedCell.AreaSquareMeters,
            precision: 8);

        Assert.Equal(
            expectedNitrogenKilograms,
            target.DetritalNitrogenKilogramsPerSquareMeter *
            expectedCell.AreaSquareMeters,
            precision: 8);

        var totalBiomassKilograms =
            biogeochemistry.Cells.Sum(
                cell =>
                    cell.DetritalBiomassKilogramsPerSquareMeter *
                    grid.GetCell(
                            cell.CellId)
                        .AreaSquareMeters);

        var totalNitrogenKilograms =
            biogeochemistry.Cells.Sum(
                cell =>
                    cell.DetritalNitrogenKilogramsPerSquareMeter *
                    grid.GetCell(
                            cell.CellId)
                        .AreaSquareMeters);

        Assert.Equal(
            expectedBiomassKilograms,
            totalBiomassKilograms,
            precision: 8);

        Assert.Equal(
            expectedNitrogenKilograms,
            totalNitrogenKilograms,
            precision: 8);
    }

    private static Fixture CreateFixture()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Mortality Material World",
                5.9722e24,
                6_371_000,
                new PlanetEnvironment(
                    288.15,
                    0.71,
                    0.03,
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

    private sealed record Fixture(
        PlanetState Planet,
        SurfaceGridDefinition Definition,
        IPlanetSurfaceGrid Grid);
}
