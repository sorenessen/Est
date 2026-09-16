using Est.Application.Worlds;

namespace Est.Application.Tests.Worlds;

public sealed class WorldFactoryBiogeochemistryTests
{
    private const double RadiusMeters =
        6_000_000;

    [Fact]
    public void Create_WithoutBiogeochemistryConfiguration_HasNoBiogeochemistry()
    {
        var world =
            WorldFactory.Create(
                new WorldCreationSpecification(
                [
                    CreatePlanet(
                        biogeochemistry:
                            null)
                ]));

        Assert.Empty(
            world.Biogeochemistry);
    }

    [Fact]
    public void Create_BiogeochemistryWithoutHydrologyIsRejected()
    {
        var specification =
            new PlanetCreationSpecification(
                "Generated World",
                5.0e24,
                RadiusMeters,
                CreateEnvironment(),
                GeneratedTerrain:
                    CreateTerrain(),
                GeneratedBiogeochemistry:
                    new GeneratedBiogeochemistryCreationSpecification(
                        InitialPlantAvailableNitrogenKilogramsPerSquareMeter:
                            0.02));

        Assert.Throws<ArgumentException>(
            () =>
                WorldFactory.Create(
                    new WorldCreationSpecification(
                    [
                        specification
                    ])));
    }

    [Fact]
    public void Create_WithGeneratedBiogeochemistrySeedsEverySurfaceCell()
    {
        const double detritalBiomass =
            0.40;

        const double detritalNitrogen =
            0.008;

        const double availableNitrogen =
            0.03;

        var world =
            WorldFactory.Create(
                new WorldCreationSpecification(
                [
                    CreatePlanet(
                        new GeneratedBiogeochemistryCreationSpecification(
                            detritalBiomass,
                            detritalNitrogen,
                            availableNitrogen))
                ]));

        var planet =
            Assert.Single(
                world.Planets);

        var terrain =
            Assert.Single(
                world.Terrain);

        var hydrology =
            Assert.Single(
                world.Hydrology);

        var biogeochemistry =
            Assert.Single(
                world.Biogeochemistry);

        Assert.Equal(
            terrain.GridDefinition,
            biogeochemistry.GridDefinition);

        Assert.Equal(
            hydrology.GridDefinition,
            biogeochemistry.GridDefinition);

        biogeochemistry.ValidateFor(
            planet);

        Assert.NotEmpty(
            biogeochemistry.Cells);

        Assert.All(
            biogeochemistry.Cells,
            cell =>
            {
                Assert.Equal(
                    detritalBiomass,
                    cell.DetritalBiomassKilogramsPerSquareMeter,
                    12);

                Assert.Equal(
                    detritalNitrogen,
                    cell.DetritalNitrogenKilogramsPerSquareMeter,
                    12);

                Assert.Equal(
                    availableNitrogen,
                    cell.PlantAvailableNitrogenKilogramsPerSquareMeter,
                    12);
            });
    }

    [Fact]
    public void Create_RejectsInvalidGeneratedBiogeochemistryPool()
    {
        var specification =
            new WorldCreationSpecification(
            [
                CreatePlanet(
                    new GeneratedBiogeochemistryCreationSpecification(
                        InitialDetritalNitrogenKilogramsPerSquareMeter:
                            -0.01))
            ]);

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                WorldFactory.Create(
                    specification));
    }

    private static PlanetCreationSpecification CreatePlanet(
        GeneratedBiogeochemistryCreationSpecification?
            biogeochemistry)
    {
        return new PlanetCreationSpecification(
            "Generated World",
            5.0e24,
            RadiusMeters,
            CreateEnvironment(),
            GeneratedTerrain:
                CreateTerrain(),
            GeneratedHydrology:
                new GeneratedHydrologyCreationSpecification(
                    0),
            GeneratedBiogeochemistry:
                biogeochemistry);
    }

    private static GeneratedTerrainCreationSpecification
        CreateTerrain()
    {
        return new GeneratedTerrainCreationSpecification(
            Seed: 42,
            LatitudeBandCount: 12,
            LongitudeBandCount: 24,
            PlateCount: 12,
            ContinentalPlateFraction: 0.45);
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
