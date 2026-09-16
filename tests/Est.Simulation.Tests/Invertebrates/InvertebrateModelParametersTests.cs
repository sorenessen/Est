using Est.Simulation.Invertebrates;

namespace Est.Simulation.Tests.Invertebrates;

public sealed class InvertebrateModelParametersTests
{
    [Fact]
    public void Constructor_UsesExplicitDefaults()
    {
        var parameters =
            new InvertebrateModelParameters();

        Assert.Equal(
            21_600,
            parameters.MaximumIntegrationStepSeconds);

        Assert.Equal(
            0.02,
            parameters
                .CarryingCapacityKilogramsPerKilogramLiveVegetation);

        Assert.Equal(
            0.25,
            parameters
                .InitialFractionOfLocalCarryingCapacity);

        Assert.Equal(
            0.10,
            parameters.MaximumRelativeGrowthRatePerDay);

        Assert.Equal(
            0.02,
            parameters.BaselineMortalityRatePerDay);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void Constructor_RejectsInvalidInitialFraction(
        double fraction)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new InvertebrateModelParameters(
                    initialFractionOfLocalCarryingCapacity:
                        fraction));
    }
}
