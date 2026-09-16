using Est.Application.Worlds;

namespace Est.Application.Tests.Worlds;

public sealed class WorldFactoryBirdTests
{
    [Fact]
    public void Create_WithGeneratedBirdsSeedsFlocksFromInvertebrateSupport()
    {
        var world =
            WorldFactory.Create(
                new WorldCreationSpecification(
                [
                    new PlanetCreationSpecification(
                        "Generated Bird World",
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
                        GeneratedInvertebrates:
                            new GeneratedInvertebrateCreationSpecification(
                                CarryingCapacityKilogramsPerKilogramLiveVegetation:
                                    0.04,
                                InitialFractionOfLocalCarryingCapacity:
                                    0.25),
                        GeneratedBirds:
                            new GeneratedBirdCreationSpecification(
                                CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass:
                                    0.000001,
                                InitialFractionOfLocalCarryingCapacity:
                                    0.25,
                                MinimumInitialFlockMemberCount:
                                    1,
                                MaximumInitialFlockCount:
                                    3))
                ]));

        Assert.Equal(
            3,
            world.BirdFlocks.Length);

        Assert.All(
            world.BirdFlocks,
            flock =>
            {
                Assert.Equal(
                    world.Planets[0].Id,
                    flock.PlanetId);

                Assert.True(
                    flock.MemberCount > 0);
            });
    }

    [Fact]
    public void Create_BirdsWithoutInvertebratesIsRejected()
    {
        var specification =
            new WorldCreationSpecification(
            [
                new PlanetCreationSpecification(
                    "Generated Bird World",
                    5.0e24,
                    6_000_000,
                    CreateEnvironment(),
                    GeneratedBirds:
                        new GeneratedBirdCreationSpecification())
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
