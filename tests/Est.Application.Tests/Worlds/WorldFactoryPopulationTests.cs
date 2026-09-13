using Est.Application.Worlds;
using Est.Simulation.Population;

namespace Est.Application.Tests.Worlds;

public sealed class WorldFactoryPopulationTests
{
    [Fact]
    public void Create_WithoutPopulationConfiguration_HasNoPeople()
    {
        var world =
            WorldFactory.Create(
                new WorldCreationSpecification(
                [
                    CreateEarth()
                ]));

        Assert.Empty(world.Population);
    }

    [Fact]
    public void Create_WithSyntheticPopulation_CreatesFoundersOnPlanet()
    {
        const int founderCount = 100;
        const double centerLatitude = 0;
        const double centerLongitude = 25;
        const double spreadDegrees = 3;

        var world =
            WorldFactory.Create(
                new WorldCreationSpecification(
                [
                    CreateEarth(
                        new SyntheticPopulationCreationSpecification(
                            founderCount,
                            Seed: 42,
                            centerLatitude,
                            centerLongitude,
                            spreadDegrees))
                ]));

        Assert.Equal(
            founderCount,
            world.Population.Length);

        var planet =
            Assert.Single(world.Planets);

        Assert.All(
            world.Population,
            person =>
            {
                Assert.Equal(
                    planet.Id,
                    person.PlanetId);

                Assert.InRange(
                    person.LatitudeDegrees,
                    centerLatitude - spreadDegrees,
                    centerLatitude + spreadDegrees);

                Assert.InRange(
                    person.LongitudeDegrees,
                    centerLongitude - spreadDegrees,
                    centerLongitude + spreadDegrees);

                Assert.InRange(
                    person.AgeYears(
                        world.CurrentTime.TotalSeconds),
                    18,
                    35);

                Assert.Null(person.ParentId);
            });

        Assert.Equal(
            founderCount / 2,
            world.Population.Count(
                person =>
                    person.Sex ==
                    PersonSex.Female));

        Assert.Equal(
            founderCount / 2,
            world.Population.Count(
                person =>
                    person.Sex ==
                    PersonSex.Male));
    }

    private static PlanetCreationSpecification CreateEarth(
        SyntheticPopulationCreationSpecification?
            population = null)
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
            population);
    }
}
