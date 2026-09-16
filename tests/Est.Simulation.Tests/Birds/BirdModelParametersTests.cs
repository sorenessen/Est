using Est.Simulation.Birds;

namespace Est.Simulation.Tests.Birds;

public sealed class BirdModelParametersTests
{
    [Fact]
    public void Constructor_UsesExplicitDefaults()
    {
        var parameters =
            new BirdModelParameters();

        Assert.Equal(
            0.000001,
            parameters
                .CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass);

        Assert.Equal(
            0.25,
            parameters.InitialFractionOfLocalCarryingCapacity);

        Assert.Equal(
            10,
            parameters.MinimumInitialFlockMemberCount);

        Assert.Equal(
            64,
            parameters.MaximumInitialFlockCount);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    public void Constructor_RejectsInvalidSupportRatio(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new BirdModelParameters(
                    carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass:
                        value));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void Constructor_RejectsInvalidInitialFraction(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new BirdModelParameters(
                    initialFractionOfLocalCarryingCapacity:
                        value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonpositiveFlockLimits(
        int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new BirdModelParameters(
                    minimumInitialFlockMemberCount:
                        value));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new BirdModelParameters(
                    maximumInitialFlockCount:
                        value));
    }
}
