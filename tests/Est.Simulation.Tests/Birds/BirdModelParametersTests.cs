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

        Assert.Equal(
            21_600,
            parameters.MaximumIntegrationStepSeconds);

        Assert.Equal(
            250_000,
            parameters.MaximumTravelMetersPerDay);

        Assert.Equal(
            0.05,
            parameters.FoodShortageMortalityRatePerDay);

        Assert.Equal(
            0.20,
            parameters.WaterAbsenceMortalityRatePerDay);

        Assert.Equal(
            0.02,
            parameters.HabitatAbsenceMortalityRatePerDay);
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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonpositiveIntegrationStep(
        long value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new BirdModelParameters(
                    maximumIntegrationStepSeconds:
                        value));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    public void Constructor_RejectsInvalidBehaviorRates(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new BirdModelParameters(
                    maximumTravelMetersPerDay:
                        value));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new BirdModelParameters(
                    foodShortageMortalityRatePerDay:
                        value));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new BirdModelParameters(
                    waterAbsenceMortalityRatePerDay:
                        value));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new BirdModelParameters(
                    habitatAbsenceMortalityRatePerDay:
                        value));
    }
}
