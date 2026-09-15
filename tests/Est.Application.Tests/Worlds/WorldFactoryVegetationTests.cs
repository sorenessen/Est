using Est.Application.Worlds;
using Est.Simulation.Hydrology;

namespace Est.Application.Tests.Worlds;

public sealed class WorldFactoryVegetationTests
{
    private const double RadiusMeters =
        6_000_000;

    private const double InitialBiomass =
        0.25;

    [Fact]
    public void Create_WithoutVegetationConfiguration_HasNoVegetation()
    {
        var world =
            WorldFactory.Create(
                new WorldCreationSpecification(
                [
                    CreatePlanet(
                        vegetation: null)
                ]));

        Assert.Empty(
            world.Vegetation);
    }

    [Fact]
    public void Create_VegetationWithoutHydrologyIsRejected()
    {
        var specification =
            new PlanetCreationSpecification(
                "Generated World",
                5.0e24,
                RadiusMeters,
                CreateEnvironment(),
                GeneratedTerrain:
                    CreateTerrain(),
                GeneratedVegetation:
                    new GeneratedVegetationCreationSpecification(
                        InitialBiomass));

        Assert.Throws<ArgumentException>(
            () =>
                WorldFactory.Create(
                    new WorldCreationSpecification(
                    [
                        specification
                    ])));
    }

    [Fact]
    public void Create_WithGeneratedVegetationSeedsOnlyDrySurfaceCells()
    {
        var world =
            WorldFactory.Create(
                new WorldCreationSpecification(
                [
                    CreatePlanet(
                        new GeneratedVegetationCreationSpecification(
                            InitialBiomass))
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

        var vegetation =
            Assert.Single(
                world.Vegetation);

        Assert.Equal(
            terrain.GridDefinition,
            vegetation.GridDefinition);

        Assert.Equal(
            hydrology.GridDefinition,
            vegetation.GridDefinition);

        vegetation.ValidateFor(
            planet);

        var standingWater =
            PlanetStandingWaterState.Derive(
                planet,
                terrain,
                hydrology);

        Assert.Contains(
            standingWater.Cells,
            cell =>
                cell.IsFlooded);

        Assert.Contains(
            standingWater.Cells,
            cell =>
                !cell.IsFlooded);

        foreach (var standingWaterCell in
                 standingWater.Cells)
        {
            var biomass =
                vegetation
                    .GetCell(
                        standingWaterCell.CellId)
                    .LiveBiomassKilogramsPerSquareMeter;

            Assert.Equal(
                standingWaterCell.IsFlooded
                    ? 0
                    : InitialBiomass,
                biomass,
                12);
        }
    }

    [Fact]
    public void Create_SameInputsProduceSameVegetationField()
    {
        var specification =
            new WorldCreationSpecification(
            [
                CreatePlanet(
                    new GeneratedVegetationCreationSpecification(
                        InitialBiomass))
            ]);

        var first =
            WorldFactory.Create(
                specification);

        var second =
            WorldFactory.Create(
                specification);

        var firstBiomass =
            Assert.Single(
                    first.Vegetation)
                .Cells
                .Select(
                    cell =>
                        cell
                            .LiveBiomassKilogramsPerSquareMeter)
                .ToArray();

        var secondBiomass =
            Assert.Single(
                    second.Vegetation)
                .Cells
                .Select(
                    cell =>
                        cell
                            .LiveBiomassKilogramsPerSquareMeter)
                .ToArray();

        Assert.True(
            firstBiomass.SequenceEqual(
                secondBiomass));
    }

    [Fact]
    public void Create_RejectsInvalidInitialVegetationBiomass()
    {
        var specification =
            new WorldCreationSpecification(
            [
                CreatePlanet(
                    new GeneratedVegetationCreationSpecification(
                        -0.01))
            ]);

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                WorldFactory.Create(
                    specification));
    }

    private static PlanetCreationSpecification CreatePlanet(
        GeneratedVegetationCreationSpecification? vegetation)
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
                    GlobalMeanDepthToMass(
                        1_000)),
            GeneratedVegetation:
                vegetation);
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

    private static double GlobalMeanDepthToMass(
        double depthMeters)
    {
        var surfaceArea =
            4 *
            Math.PI *
            RadiusMeters *
            RadiusMeters;

        return depthMeters *
               surfaceArea *
               PlanetStandingWaterState
                   .LiquidWaterDensityKilogramsPerCubicMeter;
    }
}
