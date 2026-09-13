using Est.Application.Worlds;

namespace Est.Application.Tests.Worlds;

public sealed class WorldFactoryFoodTests
{
    [Fact]
    public void Create_WithoutFoodConfiguration_HasNoFood()
    {
        var world =
            WorldFactory.Create(
                new WorldCreationSpecification(
                [
                    CreateEarth()
                ]));

        Assert.Empty(world.FoodResources);
    }

    [Fact]
    public void Create_WithSyntheticFood_CreatesPatchesOnPlanet()
    {
        const int patchCount = 40;
        const double centerLatitude = 0;
        const double centerLongitude = 25;
        const double spreadDegrees = 3;
        const double energyPerPatch = 20;
        const double recoveryEnergyPerDay = 0.5;

        var world =
            WorldFactory.Create(
                new WorldCreationSpecification(
                [
                    CreateEarth(
                        new SyntheticFoodCreationSpecification(
                            patchCount,
                            Seed: 84,
                            centerLatitude,
                            centerLongitude,
                            spreadDegrees,
                            energyPerPatch,
                            recoveryEnergyPerDay))
                ]));

        Assert.Equal(
            patchCount,
            world.FoodResources.Length);

        var planet =
            Assert.Single(world.Planets);

        Assert.All(
            world.FoodResources,
            resource =>
            {
                Assert.Equal(
                    planet.Id,
                    resource.PlanetId);

                Assert.InRange(
                    resource.LatitudeDegrees,
                    centerLatitude - spreadDegrees,
                    centerLatitude + spreadDegrees);

                Assert.InRange(
                    resource.LongitudeDegrees,
                    centerLongitude - spreadDegrees,
                    centerLongitude + spreadDegrees);

                Assert.Equal(
                    energyPerPatch,
                    resource.AvailableEnergy);

                Assert.Equal(
                    energyPerPatch,
                    resource.CapacityEnergy);

                Assert.Equal(
                    recoveryEnergyPerDay,
                    resource.RecoveryEnergyPerDay);
            });
    }

    private static PlanetCreationSpecification CreateEarth(
        SyntheticFoodCreationSpecification? food = null)
    {
        return new PlanetCreationSpecification(
            "Earth",
            5.9722e24,
            6_371_000,
            new PlanetEnvironmentCreationSpecification(
                288.15,
                0.71,
                0.10,
                new AtmosphereCreationSpecification(
                    101_325,
                    new Dictionary<string, double>
                    {
                        ["N2"] = 0.7808,
                        ["O2"] = 0.2095,
                        ["Ar"] = 0.0093,
                        ["CO2"] = 0.0004
                    })),
            SyntheticPopulation: null,
            SyntheticFood: food);
    }
}
