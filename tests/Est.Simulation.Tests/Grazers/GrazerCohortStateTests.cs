using Est.Simulation.Grazers;
using Est.Simulation.Planets;

namespace Est.Simulation.Tests.Grazers;

public sealed class GrazerCohortStateTests
{
    [Fact]
    public void Constructor_StoresValidState()
    {
        var id =
            GrazerCohortId.New();

        var planetId =
            PlanetId.New();

        var cohort =
            new GrazerCohortState(
                id,
                planetId,
                125,
                12.5,
                -45.25);

        Assert.Equal(
            id,
            cohort.Id);

        Assert.Equal(
            planetId,
            cohort.PlanetId);

        Assert.Equal(
            125,
            cohort.MemberCount);

        Assert.Equal(
            12.5,
            cohort.LatitudeDegrees);

        Assert.Equal(
            -45.25,
            cohort.LongitudeDegrees);
    }

    [Fact]
    public void Constructor_RejectsNonpositiveMembership()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new GrazerCohortState(
                    GrazerCohortId.New(),
                    PlanetId.New(),
                    0,
                    0,
                    0));
    }

    [Theory]
    [InlineData(-90.01)]
    [InlineData(90.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Constructor_RejectsInvalidLatitude(
        double latitudeDegrees)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new GrazerCohortState(
                    GrazerCohortId.New(),
                    PlanetId.New(),
                    10,
                    latitudeDegrees,
                    0));
    }

    [Theory]
    [InlineData(-180.01)]
    [InlineData(180.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Constructor_RejectsInvalidLongitude(
        double longitudeDegrees)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new GrazerCohortState(
                    GrazerCohortId.New(),
                    PlanetId.New(),
                    10,
                    0,
                    longitudeDegrees));
    }
}
