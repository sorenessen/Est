using Est.Application.Worlds;
using Est.Simulation.Hydrology;

namespace Est.Application.Tests.Worlds;

public sealed class WorldFactoryHydrologyTests
{
    private const double RadiusMeters =
        6_000_000;

    [Fact]
    public void Create_WithoutHydrologyConfiguration_HasNoHydrology()
    {
        var world =
            WorldFactory.Create(
                new WorldCreationSpecification(
                [
                    CreatePlanet(
                        hydrology: null)
                ]));

        Assert.Empty(
            world.Hydrology);
    }

    [Fact]
    public void Create_HydrologyWithoutTerrainIsRejected()
    {
        var planet =
            new PlanetCreationSpecification(
                "Generated World",
                5.0e24,
                RadiusMeters,
                CreateEnvironment(),
                GeneratedHydrology:
                    new GeneratedHydrologyCreationSpecification(
                        1.0e18));

        Assert.Throws<ArgumentException>(
            () =>
                WorldFactory.Create(
                    new WorldCreationSpecification(
                    [
                        planet
                    ])));
    }

    [Fact]
    public void Create_WithGeneratedHydrologyPreservesRequestedWaterInventory()
    {
        var requestedInventory =
            GlobalMeanDepthToMass(
                1_000);

        var world =
            WorldFactory.Create(
                new WorldCreationSpecification(
                [
                    CreatePlanet(
                        new GeneratedHydrologyCreationSpecification(
                            requestedInventory))
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

        Assert.Equal(
            terrain.GridDefinition,
            hydrology.GridDefinition);

        hydrology.ValidateFor(
            planet);

        var actualInventory =
            hydrology.TotalWaterMassKilograms(
                planet);

        var relativeError =
            Math.Abs(
                actualInventory -
                requestedInventory) /
            requestedInventory;

        Assert.InRange(
            relativeError,
            0,
            1e-12);
    }

    [Fact]
    public void Create_SameTerrainAndInventoryProduceSameWaterField()
    {
        var specification =
            new WorldCreationSpecification(
            [
                CreatePlanet(
                    new GeneratedHydrologyCreationSpecification(
                        GlobalMeanDepthToMass(
                            750)))
            ]);

        var first =
            WorldFactory.Create(
                specification);

        var second =
            WorldFactory.Create(
                specification);

        var firstWater =
            Assert.Single(
                    first.Hydrology)
                .Cells
                .Select(
                    cell =>
                        cell
                            .SurfaceLiquidWaterKilogramsPerSquareMeter);

        var secondWater =
            Assert.Single(
                    second.Hydrology)
                .Cells
                .Select(
                    cell =>
                        cell
                            .SurfaceLiquidWaterKilogramsPerSquareMeter);

        Assert.True(
            firstWater.SequenceEqual(
                secondWater));
    }

    [Fact]
    public void Create_GeneratedHydrologyProducesDerivedStandingWater()
    {
        var world =
            WorldFactory.Create(
                new WorldCreationSpecification(
                [
                    CreatePlanet(
                        new GeneratedHydrologyCreationSpecification(
                            GlobalMeanDepthToMass(
                                1_000)))
                ]));

        var planet =
            Assert.Single(
                world.Planets);

        var standingWater =
            PlanetStandingWaterState.Derive(
                planet,
                Assert.Single(
                    world.Terrain),
                Assert.Single(
                    world.Hydrology));

        Assert.NotEmpty(
            standingWater.WaterBodies);

        Assert.NotNull(
            standingWater.Ocean);

        Assert.Contains(
            standingWater.Cells,
            cell =>
                cell.IsFlooded);

        Assert.Contains(
            standingWater.Cells,
            cell =>
                !cell.IsFlooded);
    }

    private static PlanetCreationSpecification CreatePlanet(
        GeneratedHydrologyCreationSpecification? hydrology)
    {
        return new PlanetCreationSpecification(
            "Generated World",
            5.0e24,
            RadiusMeters,
            CreateEnvironment(),
            GeneratedTerrain:
                new GeneratedTerrainCreationSpecification(
                    Seed: 42,
                    LatitudeBandCount: 12,
                    LongitudeBandCount: 24,
                    PlateCount: 12,
                    ContinentalPlateFraction: 0.45),
            GeneratedHydrology:
                hydrology);
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
