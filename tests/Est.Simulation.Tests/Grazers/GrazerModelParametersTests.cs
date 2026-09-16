using Est.Simulation.Grazers;

namespace Est.Simulation.Tests.Grazers;

public sealed class GrazerModelParametersTests
{
    [Fact]
    public void Constructor_UsesExplicitDefaults()
    {
        var parameters =
            new GrazerModelParameters();

        Assert.Equal(
            0.000001,
            parameters
                .CarryingCapacityGrazersPerKilogramLiveVegetationBiomass);

        Assert.Equal(
            0.25,
            parameters.InitialFractionOfLocalCarryingCapacity);

        Assert.Equal(
            10,
            parameters.MinimumInitialCohortMemberCount);

        Assert.Equal(
            64,
            parameters.MaximumInitialCohortCount);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    public void Constructor_RejectsInvalidSupportRatio(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new GrazerModelParameters(
                    carryingCapacityGrazersPerKilogramLiveVegetationBiomass:
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
                new GrazerModelParameters(
                    initialFractionOfLocalCarryingCapacity:
                        value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonpositiveCohortLimits(
        int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new GrazerModelParameters(
                    minimumInitialCohortMemberCount:
                        value));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new GrazerModelParameters(
                    maximumInitialCohortCount:
                        value));
    }
}
