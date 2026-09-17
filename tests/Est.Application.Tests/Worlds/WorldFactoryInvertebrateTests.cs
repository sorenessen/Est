using Est.Application.Worlds;

namespace Est.Application.Tests.Worlds;

public sealed class WorldFactoryInvertebrateTests
{
    [Fact]
    public void Create_WithGeneratedInvertebratesSeedsFromVegetationSupport()
    {
        var world =
            WorldFactory.Create(
                new WorldCreationSpecification(
                [
                    new PlanetCreationSpecification(
                        "Generated World",
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
                                    0.25,
                                LiveNitrogenKilogramsPerKilogramLiveBiomass:
                                    0.025))
                ]));

        var vegetation =
            Assert.Single(
                world.Vegetation);

        var invertebrates =
            Assert.Single(
                world.Invertebrates);

        Assert.Equal(
            vegetation.GridDefinition,
            invertebrates.GridDefinition);

        foreach (var vegetationCell in
                 vegetation.Cells)
        {
            var invertebrateCell =
                invertebrates.GetCell(
                    vegetationCell.CellId);

            var expectedBiomass =
                vegetationCell.LiveBiomassKilogramsPerSquareMeter *
                0.04 *
                0.25;

            Assert.Equal(
                expectedBiomass,
                invertebrateCell
                    .LiveBiomassKilogramsPerSquareMeter,
                12);

            Assert.Equal(
                expectedBiomass *
                0.025,
                invertebrateCell
                    .LiveNitrogenKilogramsPerSquareMeter,
                12);
        }
    }

    [Fact]
    public void Create_InvertebratesWithoutVegetationIsRejected()
    {
        var specification =
            new WorldCreationSpecification(
            [
                new PlanetCreationSpecification(
                    "Generated World",
                    5.0e24,
                    6_000_000,
                    CreateEnvironment(),
                    GeneratedInvertebrates:
                        new GeneratedInvertebrateCreationSpecification())
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
