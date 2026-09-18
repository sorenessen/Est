using Est.Simulation.Birds;
using Est.Simulation.Invertebrates;
using Est.Simulation.Planets;
using Est.Simulation.Surface;

namespace Est.Simulation.Tests.Birds;

public sealed class PlanetBirdFlockInitializerTests
{
    [Fact]
    public void FromInvertebrateSupport_DistributesCentersSpatiallyAndDeterministically()
    {
        var planet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                2,
                4);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        var invertebrates =
            new PlanetInvertebrateState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    (cell, index) =>
                        new InvertebrateCellState(
                            cell.Id,
                            index + 1)));

        var parameters =
            new BirdModelParameters(
                carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass:
                    0.001,
                initialFractionOfLocalCarryingCapacity:
                    0.5,
                minimumInitialFlockMemberCount:
                    1,
                maximumInitialFlockCount:
                    2);

        var first =
            PlanetBirdFlockInitializer
                .FromInvertebrateSupport(
                    planet,
                    invertebrates,
                    parameters);

        var second =
            PlanetBirdFlockInitializer
                .FromInvertebrateSupport(
                    planet,
                    invertebrates,
                    parameters);

        Assert.Equal(
            2,
            first.Count);

        Assert.Equal(
            first,
            second);

        var expectedTotalMemberCount =
            (long)Math.Floor(
                grid.Cells.Sum(
                    cell =>
                        invertebrates
                            .GetCell(
                                cell.Id)
                            .LiveBiomassKilogramsPerSquareMeter *
                        cell.AreaSquareMeters *
                        0.001 *
                        0.5));

        Assert.Equal(
            expectedTotalMemberCount,
            first.Sum(
                flock =>
                    (long)flock.MemberCount));

        var strongestCell =
            grid.Cells[^1];

        Assert.Equal(
            strongestCell.CenterLatitudeDegrees,
            first[0].LatitudeDegrees);

        Assert.Equal(
            strongestCell.CenterLongitudeDegrees,
            first[0].LongitudeDegrees);

        Assert.Equal(
            BirdFlockId.CreateDeterministic(
                planet.Id,
                strongestCell.Id),
            first[0].Id);

        Assert.True(
            first[0].LatitudeDegrees *
            first[1].LatitudeDegrees <
            0);

        var longitudeSeparation =
            Math.Abs(
                first[0].LongitudeDegrees -
                first[1].LongitudeDegrees);

        longitudeSeparation =
            Math.Min(
                longitudeSeparation,
                360 -
                longitudeSeparation);

        Assert.True(
            longitudeSeparation >=
            90);
    }

    [Fact]
    public void FromInvertebrateSupport_WithNoSupportCreatesNoFlocks()
    {
        var planet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                2,
                4);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        var invertebrates =
            new PlanetInvertebrateState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new InvertebrateCellState(
                            cell.Id,
                            0)));

        var flocks =
            PlanetBirdFlockInitializer
                .FromInvertebrateSupport(
                    planet,
                    invertebrates,
                    new BirdModelParameters());

        Assert.Empty(
            flocks);
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Bird Initializer World",
            5.0e20,
            1_000,
            new PlanetEnvironment(
                285,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
