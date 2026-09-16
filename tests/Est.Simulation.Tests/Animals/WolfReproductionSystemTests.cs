using Est.Simulation.Animals;
using Est.Simulation.Causality;
using Est.Simulation.Organisms;
using Est.Simulation.Planets;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Animals;

public sealed class WolfReproductionSystemTests
{
    private const long OneYearSeconds =
        WolfLifecycleParameters.SecondsPerYear;

    [Fact]
    public void Step_NearbyMaturePairConceivesWithinAnnualCycle()
    {
        var planet =
            CreatePlanet();

        var parameters =
            new WolfLifecycleParameters();

        var female =
            CreateAdult(
                planet.Id,
                WolfSex.Female,
                latitude: 0,
                longitude: 0,
                parameters);

        var male =
            CreateAdult(
                planet.Id,
                WolfSex.Male,
                latitude: 0,
                longitude: 0.05,
                parameters);

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    female,
                    male),
                OneYearSeconds,
                new WolfReproductionSystem(
                    planet.Id,
                    parameters));

        var mother =
            result.World.Animals.Single(
                animal =>
                    animal.Id ==
                    female.Id);

        var pregnancy =
            Assert.IsType<
                WolfPregnancyState>(
                mother
                    .WolfLifecycle!
                    .Pregnancy);

        Assert.Equal(
            male.Id,
            pregnancy.FatherId);

        Assert.InRange(
            pregnancy.ConceptionTimeSeconds,
            1,
            OneYearSeconds);

        Assert.Equal(
            1,
            result.Change.Metrics[
                "conceptions"]);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "births"]);
    }

    [Fact]
    public void Step_DuePregnancyCreatesMaterialConservingLitter()
    {
        var planet =
            CreatePlanet();

        var parameters =
            new WolfLifecycleParameters();

        var father =
            CreateAdult(
                planet.Id,
                WolfSex.Male,
                latitude: 0,
                longitude: 0.05,
                parameters);

        var mother =
            CreateAdult(
                planet.Id,
                WolfSex.Female,
                latitude: 0,
                longitude: 0,
                parameters)
            .WithWolfLifecycle(
                new WolfLifecycleState(
                    WolfSex.Female,
                    new WolfPregnancyState(
                        conceptionTimeSeconds: 0,
                        father.Id)));

        var initialBiomass =
            mother.Material
                .LiveBiomassKilograms +
            father.Material
                .LiveBiomassKilograms;

        var initialNitrogen =
            mother.Material
                .LiveNitrogenKilograms +
            father.Material
                .LiveNitrogenKilograms;

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    mother,
                    father),
                parameters.GestationSeconds,
                new WolfReproductionSystem(
                    planet.Id,
                    parameters));

        var births =
            (int)result.Change.Metrics[
                "births"];

        Assert.InRange(
            births,
            parameters.MinimumLitterSize,
            parameters.MaximumLitterSize);

        Assert.Equal(
            2 + births,
            result.World.Animals.Length);

        var restoredMother =
            result.World.Animals.Single(
                animal =>
                    animal.Id ==
                    mother.Id);

        Assert.Null(
            restoredMother
                .WolfLifecycle!
                .Pregnancy);

        var pups =
            result.World.Animals
                .Where(
                    animal =>
                        animal.ParentId ==
                        mother.Id)
                .ToArray();

        Assert.Equal(
            births,
            pups.Length);

        Assert.All(
            pups,
            pup =>
            {
                Assert.Equal(
                    parameters.GestationSeconds,
                    pup.BirthTimeSeconds);

                Assert.Equal(
                    parameters.NewbornMaterial
                        .LiveBiomassKilogramsPerUnit,
                    pup.Material
                        .LiveBiomassKilograms,
                    precision: 10);

                Assert.Equal(
                    parameters.NewbornMaterial
                        .LiveNitrogenKilogramsPerUnit,
                    pup.Material
                        .LiveNitrogenKilograms,
                    precision: 10);

                Assert.NotEqual(
                    WolfSex.Unknown,
                    pup.WolfLifecycle!.Sex);

                Assert.Null(
                    pup.WolfLifecycle
                        .Pregnancy);
            });

        Assert.Equal(
            initialBiomass,
            result.World.Animals.Sum(
                animal =>
                    animal.Material
                        .LiveBiomassKilograms),
            precision: 10);

        Assert.Equal(
            initialNitrogen,
            result.World.Animals.Sum(
                animal =>
                    animal.Material
                        .LiveNitrogenKilograms),
            precision: 10);

        Assert.Equal(
            births *
            parameters.NewbornMaterial
                .LiveBiomassKilogramsPerUnit,
            result.Change.Metrics[
                "newbornBiomassKilograms"],
            precision: 10);

        Assert.Equal(
            births *
            parameters.NewbornMaterial
                .LiveNitrogenKilogramsPerUnit,
            result.Change.Metrics[
                "newbornNitrogenKilograms"],
            precision: 10);
    }

    [Fact]
    public void Step_DuePregnancyWithoutEnoughMaterialCreatesNoPups()
    {
        var planet =
            CreatePlanet();

        var parameters =
            new WolfLifecycleParameters();

        var father =
            CreateAdult(
                planet.Id,
                WolfSex.Male,
                latitude: 0,
                longitude: 0.05,
                parameters);

        var mother =
            CreateAdult(
                planet.Id,
                WolfSex.Female,
                latitude: 0,
                longitude: 0,
                parameters)
            .WithMaterial(
                new OrganismMaterialState(
                    liveBiomassKilograms: 1,
                    liveNitrogenKilograms: 0.02))
            .WithWolfLifecycle(
                new WolfLifecycleState(
                    WolfSex.Female,
                    new WolfPregnancyState(
                        conceptionTimeSeconds: 0,
                        father.Id)));

        var result =
            SimulationStepRunner.Step(
                CreateWorld(
                    planet,
                    mother,
                    father),
                parameters.GestationSeconds,
                new WolfReproductionSystem(
                    planet.Id,
                    parameters));

        Assert.Equal(
            2,
            result.World.Animals.Length);

        var restoredMother =
            result.World.Animals.Single(
                animal =>
                    animal.Id ==
                    mother.Id);

        Assert.Null(
            restoredMother
                .WolfLifecycle!
                .Pregnancy);

        Assert.Equal(
            1,
            restoredMother.Material
                .LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            0.02,
            restoredMother.Material
                .LiveNitrogenKilograms,
            precision: 10);

        Assert.Equal(
            0,
            result.Change.Metrics[
                "births"]);

        Assert.Equal(
            1,
            result.Change.Metrics[
                "pregnancyLossesMaterialInsufficient"]);
    }

    private static AnimalState CreateAdult(
        PlanetId planetId,
        WolfSex sex,
        double latitude,
        double longitude,
        WolfLifecycleParameters parameters)
    {
        return new AnimalState(
            AnimalId.New(),
            planetId,
            AnimalSpecies.Wolf,
            latitude,
            longitude,
            energyReserve: 1,
            health: 1,
            activity:
                AnimalActivity.Idle,
            material:
                parameters.MatureMaterial
                    .ForUnits(1),
            birthTimeSeconds:
                -4 * OneYearSeconds,
            wolfLifecycle:
                new WolfLifecycleState(
                    sex));
    }

    private static WorldState CreateWorld(
        PlanetState planet,
        params AnimalState[] animals)
    {
        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [planet],
            [],
            animals);
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Wolf Reproduction World",
            5.9722e24,
            6_371_000,
            new PlanetEnvironment(
                288.15,
                0.71,
                0.03,
                AtmosphereState.Vacuum));
    }
}
