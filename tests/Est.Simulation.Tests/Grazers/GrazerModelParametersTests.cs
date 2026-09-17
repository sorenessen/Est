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

        Assert.Equal(
            21_600,
            parameters.MaximumIntegrationStepSeconds);

        Assert.Equal(
            50_000,
            parameters.MaximumTravelMetersPerDay);

        Assert.Equal(
            10,
            parameters.MaximumGrazeKilogramsPerGrazerPerDay);

        Assert.Equal(
            0.05,
            parameters.FoodShortageMortalityRatePerDay);

        Assert.Equal(
            0,
            parameters.WaterAbsenceMortalityRatePerDay);

        Assert.False(
            parameters.UseSurfaceWaterForMovement);

        Assert.Equal(
            0.02,
            parameters.HabitatAbsenceMortalityRatePerDay);

        Assert.Equal(
            0,
            parameters.MaximumRecruitmentRatePerDay);
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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonpositiveIntegrationStep(
        long value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new GrazerModelParameters(
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
                new GrazerModelParameters(
                    maximumTravelMetersPerDay:
                        value));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new GrazerModelParameters(
                    maximumGrazeKilogramsPerGrazerPerDay:
                        value));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new GrazerModelParameters(
                    foodShortageMortalityRatePerDay:
                        value));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new GrazerModelParameters(
                    waterAbsenceMortalityRatePerDay:
                        value));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new GrazerModelParameters(
                    habitatAbsenceMortalityRatePerDay:
                        value));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new GrazerModelParameters(
                    maximumRecruitmentRatePerDay:
                        value));
    }
}
