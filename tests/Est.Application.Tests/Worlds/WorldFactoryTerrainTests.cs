using Est.Application.Worlds;
using Est.Simulation.Surface;

namespace Est.Application.Tests.Worlds;

public sealed class WorldFactoryTerrainTests
{
    [Fact]
    public void Create_WithoutTerrainConfiguration_HasNoTerrain()
    {
        var world =
            WorldFactory.Create(
                new WorldCreationSpecification(
                [
                    CreatePlanet()
                ]));

        Assert.Empty(
            world.Terrain);
    }

    [Fact]
    public void Create_WithGeneratedTerrain_CreatesCompleteTerrain()
    {
        var world =
            WorldFactory.Create(
                new WorldCreationSpecification(
                [
                    CreatePlanet(
                        new GeneratedTerrainCreationSpecification(
                            Seed: 42,
                            LatitudeBandCount: 12,
                            LongitudeBandCount: 24,
                            PlateCount: 12,
                            ContinentalPlateFraction: 0.45))
                ]));

        var planet =
            Assert.Single(
                world.Planets);

        var terrain =
            Assert.Single(
                world.Terrain);

        Assert.Equal(
            planet.Id,
            terrain.PlanetId);

        Assert.Equal(
            SurfaceGridDefinition.LatitudeLongitude(
                12,
                24),
            terrain.GridDefinition);

        Assert.Equal(
            12 * 24,
            terrain.Cells.Length);

        terrain.ValidateFor(
            planet);

        var elevations =
            terrain.Cells
                .Select(
                    cell =>
                        cell.ElevationMeters)
                .ToArray();

        Assert.True(
            elevations.Min() <
            -2_000);

        Assert.True(
            elevations.Max() >
            1_000);
    }

    [Fact]
    public void Create_SameTerrainSeedProducesSameElevationField()
    {
        var specification =
            new WorldCreationSpecification(
            [
                CreatePlanet(
                    new GeneratedTerrainCreationSpecification(
                        Seed: 42,
                        LatitudeBandCount: 12,
                        LongitudeBandCount: 24,
                        PlateCount: 12,
                        ContinentalPlateFraction: 0.45))
            ]);

        var first =
            WorldFactory.Create(
                specification);

        var second =
            WorldFactory.Create(
                specification);

        var firstElevations =
            Assert.Single(
                    first.Terrain)
                .Cells
                .Select(
                    cell =>
                        cell.ElevationMeters);

        var secondElevations =
            Assert.Single(
                    second.Terrain)
                .Cells
                .Select(
                    cell =>
                        cell.ElevationMeters);

        Assert.True(
            firstElevations.SequenceEqual(
                secondElevations));
    }

    [Fact]
    public void Create_DifferentTerrainSeedsProduceDifferentElevationFields()
    {
        var first =
            WorldFactory.Create(
                new WorldCreationSpecification(
                [
                    CreatePlanet(
                        new GeneratedTerrainCreationSpecification(
                            Seed: 42,
                            LatitudeBandCount: 12,
                            LongitudeBandCount: 24,
                            PlateCount: 12,
                            ContinentalPlateFraction: 0.45))
                ]));

        var second =
            WorldFactory.Create(
                new WorldCreationSpecification(
                [
                    CreatePlanet(
                        new GeneratedTerrainCreationSpecification(
                            Seed: 43,
                            LatitudeBandCount: 12,
                            LongitudeBandCount: 24,
                            PlateCount: 12,
                            ContinentalPlateFraction: 0.45))
                ]));

        var firstElevations =
            Assert.Single(
                    first.Terrain)
                .Cells
                .Select(
                    cell =>
                        cell.ElevationMeters);

        var secondElevations =
            Assert.Single(
                    second.Terrain)
                .Cells
                .Select(
                    cell =>
                        cell.ElevationMeters);

        Assert.False(
            firstElevations.SequenceEqual(
                secondElevations));
    }

    private static PlanetCreationSpecification CreatePlanet(
        GeneratedTerrainCreationSpecification? terrain = null)
    {
        return new PlanetCreationSpecification(
            "Generated World",
            5.0e24,
            6_000_000,
            new PlanetEnvironmentCreationSpecification(
                285,
                0.60,
                0.05,
                new AtmosphereCreationSpecification(
                    0,
                    new Dictionary<string, double>())),
            GeneratedTerrain: terrain);
    }
}
