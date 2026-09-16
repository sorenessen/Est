using Est.Application.Worlds;

namespace Est.Application.Tests.Worlds;

public sealed class WorldFactoryGrazerTests
{
    [Fact]
    public void Create_WithGeneratedGrazersSeedsCohortsFromVegetationSupport()
    {
        var world =
            WorldFactory.Create(
                new WorldCreationSpecification(
                [
                    new PlanetCreationSpecification(
                        "Generated Grazer World",
                        5.0e24,
                        6_000_000,
                        CreateEnvironment(),
                        GeneratedTerrain:
                            new GeneratedTerrainCreationSpecification(
                                Seed: 42,
                                LatitudeBandCount: 6,
                                LongitudeBandCount: 12,
                                PlateCount: 8),
                        GeneratedHydrology:
                            new GeneratedHydrologyCreationSpecification(
                                0),
                        GeneratedVegetation:
                            new GeneratedVegetationCreationSpecification(
                                2),
                        GeneratedGrazers:
                            new GeneratedGrazerCreationSpecification(
                                CarryingCapacityGrazersPerKilogramLiveVegetationBiomass:
                                    0.000001,
                                InitialFractionOfLocalCarryingCapacity:
                                    0.25,
                                MinimumInitialCohortMemberCount:
                                    1,
                                MaximumInitialCohortCount:
                                    3))
                ]));

        Assert.Equal(
            3,
            world.GrazerCohorts.Length);

        Assert.All(
            world.GrazerCohorts,
            cohort =>
            {
                Assert.Equal(
                    world.Planets[0].Id,
                    cohort.PlanetId);

                Assert.True(
                    cohort.MemberCount > 0);
            });
    }

    [Fact]
    public void Create_GrazersWithoutVegetationIsRejected()
    {
        var specification =
            new WorldCreationSpecification(
            [
                new PlanetCreationSpecification(
                    "Generated Grazer World",
                    5.0e24,
                    6_000_000,
                    CreateEnvironment(),
                    GeneratedGrazers:
                        new GeneratedGrazerCreationSpecification())
            ]);

        Assert.Throws<ArgumentException>(
            () =>
                WorldFactory.Create(
                    specification));
    }

    private static PlanetEnvironmentCreationSpecification
        CreateEnvironment()
    {
        return new PlanetEnvironmentCreationSpecification(
            285,
            0.60,
            0.05,
            new AtmosphereCreationSpecification(
                0,
                new Dictionary<string, double>()));
    }
}
