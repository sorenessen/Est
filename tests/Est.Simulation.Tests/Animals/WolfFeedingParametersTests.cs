using Est.Simulation.Animals;

namespace Est.Simulation.Tests.Animals;

public sealed class WolfFeedingParametersTests
{
    [Fact]
    public void DefaultConversion_MatchesCurrentWolfMaintenanceScale()
    {
        var parameters =
            new WolfFeedingParameters();

        Assert.Equal(
            40,
            parameters
                .KilogramsMetabolizedBiomassPerEnergyReserveUnit,
            precision: 10);
    }

    [Fact]
    public void EnergyReserveFromMetabolizedBiomass_UsesConfiguredConversion()
    {
        var parameters =
            new WolfFeedingParameters(
                kilogramsMetabolizedBiomassPerEnergyReserveUnit:
                    40);

        Assert.Equal(
            0.08,
            parameters
                .EnergyReserveFromMetabolizedBiomass(
                    3.2),
            precision: 10);

        Assert.Equal(
            6.25,
            parameters
                .EnergyReserveFromMetabolizedBiomass(
                    250),
            precision: 10);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Constructor_RejectsInvalidBiomassConversion(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                _ = new WolfFeedingParameters(
                    value);
            });
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void EnergyReserveFromMetabolizedBiomass_RejectsInvalidInput(
        double value)
    {
        var parameters =
            new WolfFeedingParameters();

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                _ = parameters
                    .EnergyReserveFromMetabolizedBiomass(
                        value);
            });
    }
}
