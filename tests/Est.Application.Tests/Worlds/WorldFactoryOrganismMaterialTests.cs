using Est.Application.Worlds;
using Est.Simulation.Animals;

namespace Est.Application.Tests.Worlds;

public sealed class WorldFactoryOrganismMaterialTests
{
    [Fact]
    public void Create_SeedsExplicitOrganismMaterialAcrossRepresentations()
    {
        const double personBiomassKilograms =
            82;

        const double personNitrogenKilograms =
            2.05;

        const double wolfBiomassKilograms =
            45;

        const double wolfNitrogenKilograms =
            1.125;

        const double birdBiomassKilograms =
            0.8;

        const double birdNitrogenKilograms =
            0.02;

        const double grazerBiomassKilograms =
            300;

        const double grazerNitrogenKilograms =
            7.5;

        var world =
            WorldFactory.Create(
                new WorldCreationSpecification(
                [
                    new PlanetCreationSpecification(
                        "Material World",
                        5.0e24,
                        6_000_000,
                        new PlanetEnvironmentCreationSpecification(
                            288,
                            0,
                            0,
                            new AtmosphereCreationSpecification(
                                0,
                                new Dictionary<string, double>())),
                        SyntheticPopulation:
                            new SyntheticPopulationCreationSpecification(
                                FounderCount: 4,
                                Seed: 11,
                                CenterLatitudeDegrees: 0,
                                CenterLongitudeDegrees: 0,
                                SpreadDegrees: 1,
                                LiveBiomassKilogramsPerPerson:
                                    personBiomassKilograms,
                                LiveNitrogenKilogramsPerPerson:
                                    personNitrogenKilograms),
                        SyntheticAnimals:
                            new SyntheticAnimalCreationSpecification(
                                WolfCount: 3,
                                Seed: 12,
                                CenterLatitudeDegrees: 0,
                                CenterLongitudeDegrees: 0,
                                SpreadDegrees: 1,
                                LiveBiomassKilogramsPerWolf:
                                    wolfBiomassKilograms,
                                LiveNitrogenKilogramsPerWolf:
                                    wolfNitrogenKilograms),
                        GeneratedTerrain:
                            new GeneratedTerrainCreationSpecification(
                                Seed: 13,
                                LatitudeBandCount: 6,
                                LongitudeBandCount: 12,
                                PlateCount: 6,
                                ContinentalPlateFraction: 0.6),
                        GeneratedHydrology:
                            new GeneratedHydrologyCreationSpecification(
                                SurfaceLiquidWaterInventoryKilograms: 0),
                        GeneratedVegetation:
                            new GeneratedVegetationCreationSpecification(
                                InitialLiveBiomassKilogramsPerSquareMeter:
                                    0.01),
                        GeneratedInvertebrates:
                            new GeneratedInvertebrateCreationSpecification(),
                        GeneratedBirds:
                            new GeneratedBirdCreationSpecification(
                                CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass:
                                    0.00001,
                                MaximumInitialFlockCount: 8,
                                LiveBiomassKilogramsPerBird:
                                    birdBiomassKilograms,
                                LiveNitrogenKilogramsPerBird:
                                    birdNitrogenKilograms),
                        GeneratedGrazers:
                            new GeneratedGrazerCreationSpecification(
                                MaximumInitialCohortCount: 8,
                                LiveBiomassKilogramsPerGrazer:
                                    grazerBiomassKilograms,
                                LiveNitrogenKilogramsPerGrazer:
                                    grazerNitrogenKilograms))
                ]));

        Assert.Equal(
            4,
            world.Population.Length);

        Assert.All(
            world.Population,
            person =>
            {
                Assert.Equal(
                    personBiomassKilograms,
                    person.Material.LiveBiomassKilograms);

                Assert.Equal(
                    personNitrogenKilograms,
                    person.Material.LiveNitrogenKilograms);
            });

        Assert.Equal(
            3,
            world.Animals.Length);

        Assert.Equal(
            2,
            world.Animals.Count(
                wolf =>
                    wolf.WolfLifecycle?.Sex ==
                    WolfSex.Female));

        Assert.Equal(
            1,
            world.Animals.Count(
                wolf =>
                    wolf.WolfLifecycle?.Sex ==
                    WolfSex.Male));

        Assert.All(
            world.Animals,
            wolf =>
            {
                Assert.Equal(
                    wolfBiomassKilograms,
                    wolf.Material.LiveBiomassKilograms);

                Assert.Equal(
                    wolfNitrogenKilograms,
                    wolf.Material.LiveNitrogenKilograms);

                Assert.InRange(
                    wolf.AgeYears(
                        world.CurrentTime.TotalSeconds),
                    2,
                    6);

                Assert.Null(
                    wolf.ParentId);

                Assert.NotNull(
                    wolf.WolfLifecycle);

                Assert.Null(
                    wolf.WolfLifecycle!
                        .Pregnancy);
            });

        Assert.NotEmpty(
            world.BirdFlocks);

        Assert.All(
            world.BirdFlocks,
            flock =>
            {
                Assert.Equal(
                    birdBiomassKilograms *
                    flock.MemberCount,
                    flock.Material.LiveBiomassKilograms);

                Assert.Equal(
                    birdNitrogenKilograms *
                    flock.MemberCount,
                    flock.Material.LiveNitrogenKilograms);
            });

        Assert.NotEmpty(
            world.GrazerCohorts);

        Assert.All(
            world.GrazerCohorts,
            cohort =>
            {
                Assert.Equal(
                    grazerBiomassKilograms *
                    cohort.MemberCount,
                    cohort.Material.LiveBiomassKilograms);

                Assert.Equal(
                    grazerNitrogenKilograms *
                    cohort.MemberCount,
                    cohort.Material.LiveNitrogenKilograms);
            });
    }
}
