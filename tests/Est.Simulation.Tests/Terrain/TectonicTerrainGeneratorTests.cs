using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;

namespace Est.Simulation.Tests.Terrain;

public sealed class TectonicTerrainGeneratorTests
{
    [Fact]
    public void Generate_SameSeedProducesIdenticalTopography()
    {
        var planet =
            CreatePlanet();

        var parameters =
            CreateParameters(
                seed: 42);

        var first =
            TectonicTerrainGenerator.Generate(
                planet,
                parameters);

        var second =
            TectonicTerrainGenerator.Generate(
                planet,
                parameters);

        Assert.Equal(
            first.GridDefinition,
            second.GridDefinition);

        Assert.True(
            first.Cells.SequenceEqual(
                second.Cells));
    }

    [Fact]
    public void Generate_DifferentSeedsProduceDifferentTopography()
    {
        var planet =
            CreatePlanet();

        var first =
            TectonicTerrainGenerator.Generate(
                planet,
                CreateParameters(
                    seed: 42));

        var second =
            TectonicTerrainGenerator.Generate(
                planet,
                CreateParameters(
                    seed: 43));

        Assert.False(
            first.Cells
                .Select(cell => cell.ElevationMeters)
                .SequenceEqual(
                    second.Cells.Select(
                        cell =>
                            cell.ElevationMeters)));
    }

    [Fact]
    public void Generate_ProducesCompletePlanetScaleRelief()
    {
        var planet =
            CreatePlanet();

        var terrain =
            TectonicTerrainGenerator.Generate(
                planet,
                CreateParameters(
                    seed: 42));

        terrain.ValidateFor(
            planet);

        var elevations =
            terrain.Cells
                .Select(
                    cell =>
                        cell.ElevationMeters)
                .ToArray();

        Assert.Equal(
            12 * 24,
            elevations.Length);

        Assert.All(
            elevations,
            elevation =>
                Assert.True(
                    double.IsFinite(
                        elevation)));

        Assert.True(
            elevations.Min() <
            -2_000);

        Assert.True(
            elevations.Max() >
            1_000);

        Assert.True(
            elevations.Max() -
            elevations.Min() >
            4_000);
    }

    [Fact]
    public void Generate_PlateCountCannotExceedSurfaceCells()
    {
        var planet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition
                .LatitudeLongitude(
                    2,
                    4);

        var parameters =
            new TectonicTerrainGenerationParameters(
                definition,
                seed: 42,
                plateCount: 9);

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                TectonicTerrainGenerator.Generate(
                    planet,
                    parameters));
    }

    [Fact]
    public void Parameters_RejectInvalidContinentalFraction()
    {
        var definition =
            SurfaceGridDefinition
                .LatitudeLongitude(
                    4,
                    8);

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new TectonicTerrainGenerationParameters(
                    definition,
                    seed: 42,
                    continentalPlateFraction:
                        1.01));
    }

    private static
        TectonicTerrainGenerationParameters
        CreateParameters(
            int seed)
    {
        return new TectonicTerrainGenerationParameters(
            SurfaceGridDefinition
                .LatitudeLongitude(
                    12,
                    24),
            seed,
            plateCount: 12,
            continentalPlateFraction:
                0.45);
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Generated World",
            5.0e24,
            6_000_000,
            new PlanetEnvironment(
                285,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
