using Est.Simulation.Animals;
using Est.Simulation.Biogeochemistry;
using Est.Simulation.Causality;
using Est.Simulation.Grazers;
using Est.Simulation.Hydrology;
using Est.Simulation.Organisms;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Vegetation;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Animals;

public sealed class WolfPredatorSystemTests
{
    private const long OneDaySeconds = 86_400;

    [Fact]
    public void Step_NonDesperateWolfDoesNotAttackNearbyHuman()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 0.05);

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0,
                energyReserve: 0.35);

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [person],
                    [wolf]),
                OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.Single(
            result.World.Population);

        var changedWolf =
            Assert.Single(
                result.World.Animals);

        Assert.Equal(
            0.27,
            changedWolf.EnergyReserve,
            precision: 10);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "wolfAttacks"]);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "predationDeaths"]);
    }

    [Fact]
    public void Step_DesperateWolfDoesNotHomeTowardDistantHuman()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 2);

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0,
                energyReserve: 0.05);

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [person],
                    [wolf]),
                14 * OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.Single(
            result.World.Population);

        var changedWolf =
            Assert.Single(
                result.World.Animals);

        Assert.Equal(
            0,
            changedWolf.LongitudeDegrees,
            precision: 10);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "wolfAttacks"]);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "predationDeaths"]);
    }

    [Fact]
    public void Step_HumanGroupDetersLoneWolf()
    {
        var planet = CreatePlanet();

        var first =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 0.05);

        var second =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 0.10);

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0,
                energyReserve: 0.01);

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [first, second],
                    [wolf]),
                14 * OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.Equal(
            2,
            result.World.Population.Length);

        Assert.True(
            result.Change.Metrics[
                "avoidedEncounters"] >= 1);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "predationDeaths"]);

        var changedWolf =
            Assert.Single(
                result.World.Animals);

        Assert.True(
            changedWolf.LongitudeDegrees < 0);
    }

    [Fact]
    public void Step_StarvingWolfAtContactCanBeInjuredByHumanDefense()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 0.05,
                id: new PersonId(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000101")));

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0,
                energyReserve: 0,
                id: new AnimalId(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000301")));

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [person],
                    [wolf]),
                14 * OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.True(
            result.Change.Metrics[
                "wolfAttacks"] >= 1);

        Assert.True(
            result.Change.Metrics[
                "wolfInjuries"] >= 1);

        if (result.World.Animals.Length > 0)
        {
            var changedWolf =
                Assert.Single(
                    result.World.Animals);

            Assert.True(
                changedWolf.Health < 1);
        }
        else
        {
            Assert.True(
                result.Change.Metrics[
                    "wolfDeaths"] +
                result.Change.Metrics[
                    "wolfStarvationDeaths"] >= 1);
        }
    }

    [Fact]
    public void Step_DesperateWolfPackCanEscalateAgainstIsolatedHuman()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 0.05);

        var firstWolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0,
                energyReserve: 0);

        var secondWolf =
            CreateWolf(
                planet.Id,
                latitude: 0.01,
                longitude: 0,
                energyReserve: 0,
                id: new AnimalId(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000302")));

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [person],
                    [firstWolf, secondWolf]),
                14 * OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.True(
            result.Change.Metrics[
                "packEncounters"] >= 1);

        Assert.True(
            result.Change.Metrics[
                "wolfAttacks"] >= 1);
    }

    [Fact]
    public void Step_SatiatedWolfRestsInsteadOfTargetingHuman()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 0.05);

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0,
                energyReserve: 1);

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [person],
                    [wolf]),
                OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.Single(
            result.World.Population);

        var changedWolf =
            Assert.Single(
                result.World.Animals);

        Assert.Equal(
            AnimalActivity.Idle,
            changedWolf.Activity);

        Assert.Equal(
            0.92,
            changedWolf.EnergyReserve,
            precision: 10);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "wolfAttacks"]);
    }

    [Fact]
    public void Step_WolfEnergyUseRemainsBoundedForPartialDay()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 2);

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0,
                energyReserve: 1);

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [person],
                    [wolf]),
                OneDaySeconds / 2,
                new WolfPredatorSystem(
                    planet.Id));

        var changedWolf =
            Assert.Single(
                result.World.Animals);

        Assert.Equal(
            0.96,
            changedWolf.EnergyReserve,
            precision: 10);

        Assert.Equal(
            1,
            changedWolf.Health,
            precision: 10);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "wolfStarvationDeaths"]);
    }

    [Fact]
    public void Step_WolfWithoutFoodEventuallyDiesFromStarvation()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 10);

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0,
                energyReserve: 0.05);

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [person],
                    [wolf]),
                30 * OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.Empty(
            result.World.Animals);

        Assert.Single(
            result.World.Population);

        Assert.Equal(
            1,
            result.Change.Metrics[
                "wolfStarvationDeaths"]);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "predationDeaths"]);
    }

    [Fact]
    public void Step_HungryWolfConsumesNearbyGrazerBeforeConsideringHuman()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 0.05);

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0,
                energyReserve: 0.20);

        var grazer =
            CreateGrazer(
                planet.Id,
                memberCount: 5,
                latitude: 0,
                longitude: 0.05);

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [person],
                    [wolf],
                    [grazer]),
                OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.Single(
            result.World.Population);

        var remainingCohort =
            Assert.Single(
                result.World.GrazerCohorts);

        Assert.Equal(
            4,
            remainingCohort.MemberCount);

        var changedWolf =
            Assert.Single(
                result.World.Animals);

        Assert.True(
            changedWolf.EnergyReserve >
            0.12);

        Assert.Equal(
            AnimalActivity.Eating,
            changedWolf.Activity);

        Assert.Equal(
            1,
            result.Change.Metrics[
                "grazerKills"]);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "wolfAttacks"]);
    }


    [Fact]
    public void Step_LargeGrazerKillSustainsEightWolfPackFromPreyBiomass()
    {
        var planet =
            CreatePlanet();

        var wolves =
            Enumerable.Range(
                    0,
                    8)
                .Select(
                    _ =>
                        new AnimalState(
                            AnimalId.New(),
                            planet.Id,
                            AnimalSpecies.Wolf,
                            latitudeDegrees: 0,
                            longitudeDegrees: 0,
                            energyReserve: 0.20,
                            health: 1,
                            activity:
                                AnimalActivity.Hunting,
                            material:
                                new OrganismMaterialState(
                                    liveBiomassKilograms:
                                        40,
                                    liveNitrogenKilograms:
                                        1),
                            birthTimeSeconds:
                                -4 * 31_536_000L))
                .ToArray();

        var grazer =
            new GrazerCohortState(
                GrazerCohortId.New(),
                planet.Id,
                memberCount: 1,
                latitudeDegrees: 0,
                longitudeDegrees: 0.05,
                material:
                    new OrganismMaterialState(
                        liveBiomassKilograms:
                            320,
                        liveNitrogenKilograms:
                            8));

        var surfaceDefinition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var surfaceGrid =
            PlanetSurfaceGridFactory.Create(
                planet,
                surfaceDefinition);

        var terrain =
            new PlanetTerrainState(
                planet.Id,
                surfaceDefinition,
                surfaceGrid.Cells.Select(
                    cell =>
                        new TerrainCellState(
                            cell.Id,
                            elevationMeters: 0)));

        var hydrology =
            new PlanetHydrologyState(
                planet.Id,
                surfaceDefinition,
                surfaceGrid.Cells.Select(
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
                planet.Id,
                surfaceDefinition,
                surfaceGrid.Cells.Select(
                    cell =>
                        new VegetationCellState(
                            cell.Id,
                            liveBiomassKilogramsPerSquareMeter:
                                0)));

        var biogeochemistry =
            new PlanetBiogeochemistryState(
                planet.Id,
                surfaceDefinition,
                surfaceGrid.Cells.Select(
                    cell =>
                        new BiogeochemistryCellState(
                            cell.Id,
                            detritalBiomassKilogramsPerSquareMeter:
                                0,
                            detritalNitrogenKilogramsPerSquareMeter:
                                0,
                            plantAvailableNitrogenKilogramsPerSquareMeter:
                                0)));

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [],
                wolves,
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
                [
                    grazer
                ],
                biogeochemistry:
                [
                    biogeochemistry
                ]);

        var result =
            SimulationStepRunner.Step(
                world,
                OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.Equal(
            1,
            result.Change.Metrics[
                "grazerKills"]);

        Assert.Equal(
            8,
            result.World.Animals.Length);

        Assert.All(
            result.World.Animals,
            wolf =>
            {
                Assert.True(
                    wolf.EnergyReserve > 0.9,
                    $"Expected prey biomass to sustain the pack, but wolf {wolf.Id.Value} had reserve {wolf.EnergyReserve:F6}.");

                Assert.Equal(
                    1,
                    wolf.Health,
                    precision: 10);
            });
    }

    [Fact]
    public void Step_HungryWolfMovesTowardDetectedGrazer()
    {
        var planet = CreatePlanet();

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0,
                energyReserve: 0.30);

        var grazer =
            CreateGrazer(
                planet.Id,
                memberCount: 5,
                latitude: 0,
                longitude: 1);

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [],
                    [wolf],
                    [grazer]),
                OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        var changedWolf =
            Assert.Single(
                result.World.Animals);

        Assert.Equal(
            0.75,
            changedWolf.LongitudeDegrees,
            precision: 10);

        Assert.Equal(
            AnimalActivity.Traveling,
            changedWolf.Activity);

        Assert.Equal(
            5,
            Assert.Single(
                    result.World.GrazerCohorts)
                .MemberCount);

        Assert.Equal(
            1,
            result.Change.Metrics[
                "grazerChaseSteps"]);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "grazerKills"]);
    }

    [Fact]
    public void Step_GrazerPredationRemovesExtinctCohort()
    {
        var planet = CreatePlanet();

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0,
                energyReserve: 0.20);

        var grazer =
            CreateGrazer(
                planet.Id,
                memberCount: 1,
                latitude: 0,
                longitude: 0.05);

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [],
                    [wolf],
                    [grazer]),
                OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.Empty(
            result.World.GrazerCohorts);

        Assert.Equal(
            1,
            result.Change.Metrics[
                "grazerKills"]);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "grazerMembers"]);
    }

    [Fact]
    public void Step_GrazerPredationPreservesOtherPlanetCohort()
    {
        var huntedPlanet =
            CreatePlanet();

        var otherPlanet =
            CreatePlanet();

        var wolf =
            CreateWolf(
                huntedPlanet.Id,
                latitude: 0,
                longitude: 0,
                energyReserve: 0.20);

        var huntedGrazer =
            CreateGrazer(
                huntedPlanet.Id,
                memberCount: 1,
                latitude: 0,
                longitude: 0.05);

        var preservedGrazer =
            CreateGrazer(
                otherPlanet.Id,
                memberCount: 7,
                latitude: 10,
                longitude: 10);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [
                    huntedPlanet,
                    otherPlanet
                ],
                [],
                [wolf],
                grazerCohorts:
                [
                    huntedGrazer,
                    preservedGrazer
                ]);

        var result =
            SimulationStepRunner.Step(
                world,
                OneDaySeconds,
                new WolfPredatorSystem(
                    huntedPlanet.Id));

        var remaining =
            Assert.Single(
                result.World.GrazerCohorts);

        Assert.Equal(
            preservedGrazer.Id,
            remaining.Id);

        Assert.Equal(
            7,
            remaining.MemberCount);

        Assert.Equal(
            otherPlanet.Id,
            remaining.PlanetId);

        Assert.Equal(
            1,
            result.Change.Metrics[
                "grazerKills"]);
    }

    [Fact]
    public void Step_JuvenileWolfDoesNotHuntGrazer()
    {
        var planet =
            CreatePlanet();

        var juvenile =
            new AnimalState(
                AnimalId.New(),
                planet.Id,
                AnimalSpecies.Wolf,
                latitudeDegrees: 0,
                longitudeDegrees: 0,
                energyReserve: 0.20,
                health: 1,
                activity:
                    AnimalActivity.Hunting,
                birthTimeSeconds:
                    -30 * OneDaySeconds);

        var grazer =
            CreateGrazer(
                planet.Id,
                memberCount: 5,
                latitude: 0,
                longitude: 0.05);

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [],
                    [juvenile],
                    [grazer]),
                OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        var changedJuvenile =
            Assert.Single(
                result.World.Animals);

        Assert.Equal(
            AnimalActivity.Idle,
            changedJuvenile.Activity);

        Assert.Equal(
            5,
            Assert.Single(
                    result.World.GrazerCohorts)
                .MemberCount);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "grazerHunts"]);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "grazerKills"]);
    }

    [Fact]
    public void Step_NoWolfDoesNotCreatePredator()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 0);

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    [person],
                    []),
                OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.Single(
            result.World.Population);

        Assert.Empty(
            result.World.Animals);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "wolfAttacks"]);
    }

    [Fact]
    public void Step_PredationPreservesSourceWorld()
    {
        var planet = CreatePlanet();

        var person =
            CreatePerson(
                planet.Id,
                latitude: 0,
                longitude: 0.05);

        var wolf =
            CreateWolf(
                planet.Id,
                latitude: 0,
                longitude: 0,
                energyReserve: 0);

        var world =
            CreateWorld(
                planet,
                [person],
                [wolf]);

        var result =
            SimulationStepRunner.Step(
                world,
                14 * OneDaySeconds,
                new WolfPredatorSystem(
                    planet.Id));

        Assert.Single(
            world.Population);

        Assert.Single(
            world.Animals);

        Assert.NotSame(
            world,
            result.World);
    }

    private static WorldState CreateWorld(
        PlanetState planet,
        PersonState[] people,
        AnimalState[] animals,
        GrazerCohortState[]? grazerCohorts = null)
    {
        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [planet],
            people,
            animals,
            grazerCohorts:
                grazerCohorts ??
                []);
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Earth",
            5.9722e24,
            6_371_000,
            new PlanetEnvironment(
                288.15,
                0.71,
                0.03,
                AtmosphereState.Vacuum));
    }

    private static PersonState CreatePerson(
        PlanetId planetId,
        double latitude,
        double longitude,
        PersonId? id = null)
    {
        return new PersonState(
            id ??
                new PersonId(
                    Guid.NewGuid()),
            planetId,
            PersonSex.Female,
            -800_000_000,
            latitude,
            longitude);
    }

    private static GrazerCohortState CreateGrazer(
        PlanetId planetId,
        int memberCount,
        double latitude,
        double longitude)
    {
        return new GrazerCohortState(
            GrazerCohortId.New(),
            planetId,
            memberCount,
            latitude,
            longitude);
    }

    private static AnimalState CreateWolf(
        PlanetId planetId,
        double latitude,
        double longitude,
        double energyReserve,
        AnimalId? id = null)
    {
        return new AnimalState(
            id ??
                new AnimalId(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000301")),
            planetId,
            AnimalSpecies.Wolf,
            latitude,
            longitude,
            energyReserve,
            health: 1,
            activity:
                AnimalActivity.Hunting,
            birthTimeSeconds:
                -4 * 31_536_000L);
    }
}
