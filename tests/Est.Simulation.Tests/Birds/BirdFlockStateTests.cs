using Est.Simulation.Birds;
using Est.Simulation.Planets;

namespace Est.Simulation.Tests.Birds;

public sealed class BirdFlockStateTests
{
    [Fact]
    public void Constructor_StoresDurableFlockState()
    {
        var id =
            BirdFlockId.New();

        var planetId =
            PlanetId.New();

        var flock =
            new BirdFlockState(
                id,
                planetId,
                125,
                38.9,
                -77.0);

        Assert.Equal(
            id,
            flock.Id);

        Assert.Equal(
            planetId,
            flock.PlanetId);

        Assert.Equal(
            125,
            flock.MemberCount);

        Assert.Equal(
            38.9,
            flock.LatitudeDegrees);

        Assert.Equal(
            -77.0,
            flock.LongitudeDegrees);

        Assert.Equal(
            0,
            flock.RecruitmentAccumulator);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonpositiveMemberCount(
        int memberCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new BirdFlockState(
                    BirdFlockId.New(),
                    PlanetId.New(),
                    memberCount,
                    0,
                    0));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    public void Constructor_RejectsInvalidRecruitmentAccumulator(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new BirdFlockState(
                    BirdFlockId.New(),
                    PlanetId.New(),
                    10,
                    0,
                    0,
                    recruitmentAccumulator:
                        value));
    }

    [Theory]
    [InlineData(-90.1, 0)]
    [InlineData(90.1, 0)]
    [InlineData(0, -180.1)]
    [InlineData(0, 180.1)]
    public void Constructor_RejectsInvalidCoordinates(
        double latitude,
        double longitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new BirdFlockState(
                    BirdFlockId.New(),
                    PlanetId.New(),
                    10,
                    latitude,
                    longitude));
    }
}
