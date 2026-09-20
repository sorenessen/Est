using Est.Simulation.Planets;
using Est.Simulation.Players;
using Est.Simulation.Social;

namespace Est.Simulation.Tests.Players;

public sealed class ManifestedEsterStateTests
{
    [Fact]
    public void Constructor_PreservesAuthoritativeIdentityAndLocation()
    {
        var esterId = EsterId.New();
        var planetId = PlanetId.New();

        var state =
            new ManifestedEsterState(
                esterId,
                planetId,
                47.0379,
                -122.9007);

        Assert.Equal(esterId, state.EsterId);
        Assert.Equal(planetId, state.PlanetId);
        Assert.Equal(47.0379, state.LatitudeDegrees);
        Assert.Equal(-122.9007, state.LongitudeDegrees);
    }

    [Fact]
    public void MoveTo_ChangesLocationWithoutChangingIdentity()
    {
        var esterId = EsterId.New();
        var planetId = PlanetId.New();

        var original =
            new ManifestedEsterState(
                esterId,
                planetId,
                47.0379,
                -122.9007);

        var moved =
            original.MoveTo(
                47.0380,
                -122.9005);

        Assert.Equal(esterId, moved.EsterId);
        Assert.Equal(planetId, moved.PlanetId);
        Assert.Equal(47.0380, moved.LatitudeDegrees);
        Assert.Equal(-122.9005, moved.LongitudeDegrees);

        Assert.Equal(47.0379, original.LatitudeDegrees);
        Assert.Equal(-122.9007, original.LongitudeDegrees);
    }

    [Theory]
    [InlineData(-90.1, 0)]
    [InlineData(90.1, 0)]
    [InlineData(0, -180.1)]
    [InlineData(0, 180.1)]
    public void Constructor_RejectsOutOfRangePosition(
        double latitudeDegrees,
        double longitudeDegrees)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new ManifestedEsterState(
                    EsterId.New(),
                    PlanetId.New(),
                    latitudeDegrees,
                    longitudeDegrees));
    }
}
