using Est.Simulation.Thermal;

namespace Est.Simulation.Tests.Thermal;

public sealed class GraybodyLongwaveEmissionCalculatorTests
{
    [Fact]
    public void Calculate_IsDeterministicForSameInputs()
    {
        var first =
            GraybodyLongwaveEmissionCalculator
                .CalculateEmittedFluxWattsPerSquareMeter(
                    temperatureKelvin: 288.15,
                    effectiveEmissivity: 0.72);

        var second =
            GraybodyLongwaveEmissionCalculator
                .CalculateEmittedFluxWattsPerSquareMeter(
                    temperatureKelvin: 288.15,
                    effectiveEmissivity: 0.72);

        Assert.Equal(
            first,
            second);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Calculate_RejectsInvalidTemperature(
        double temperatureKelvin)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                GraybodyLongwaveEmissionCalculator
                    .CalculateEmittedFluxWattsPerSquareMeter(
                        temperatureKelvin,
                        effectiveEmissivity: 0.8));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Calculate_RejectsInvalidEmissivity(
        double effectiveEmissivity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                GraybodyLongwaveEmissionCalculator
                    .CalculateEmittedFluxWattsPerSquareMeter(
                        temperatureKelvin: 300,
                        effectiveEmissivity));
    }

    [Fact]
    public void Calculate_ZeroTemperatureProducesZeroEmission()
    {
        var result =
            GraybodyLongwaveEmissionCalculator
                .CalculateEmittedFluxWattsPerSquareMeter(
                    temperatureKelvin: 0,
                    effectiveEmissivity: 1);

        Assert.Equal(
            0,
            result);
    }

    [Fact]
    public void Calculate_ZeroEmissivityProducesZeroEmission()
    {
        var result =
            GraybodyLongwaveEmissionCalculator
                .CalculateEmittedFluxWattsPerSquareMeter(
                    temperatureKelvin: 300,
                    effectiveEmissivity: 0);

        Assert.Equal(
            0,
            result);
    }

    [Fact]
    public void Calculate_UnitEmissivityMatchesBlackbodyReferenceAt300Kelvin()
    {
        var result =
            GraybodyLongwaveEmissionCalculator
                .CalculateEmittedFluxWattsPerSquareMeter(
                    temperatureKelvin: 300,
                    effectiveEmissivity: 1);

        Assert.Equal(
            459.300327939,
            result,
            precision: 9);
    }

    [Fact]
    public void Calculate_EmissionIncreasesWithTemperature()
    {
        var cooler =
            GraybodyLongwaveEmissionCalculator
                .CalculateEmittedFluxWattsPerSquareMeter(
                    temperatureKelvin: 250,
                    effectiveEmissivity: 0.8);

        var warmer =
            GraybodyLongwaveEmissionCalculator
                .CalculateEmittedFluxWattsPerSquareMeter(
                    temperatureKelvin: 300,
                    effectiveEmissivity: 0.8);

        Assert.True(
            warmer > cooler);
    }

    [Fact]
    public void Calculate_EmissionScalesLinearlyWithEmissivity()
    {
        var blackbody =
            GraybodyLongwaveEmissionCalculator
                .CalculateEmittedFluxWattsPerSquareMeter(
                    temperatureKelvin: 300,
                    effectiveEmissivity: 1);

        var halfEmissivity =
            GraybodyLongwaveEmissionCalculator
                .CalculateEmittedFluxWattsPerSquareMeter(
                    temperatureKelvin: 300,
                    effectiveEmissivity: 0.5);

        Assert.Equal(
            blackbody * 0.5,
            halfEmissivity,
            precision: 12);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 0.2)]
    [InlineData(288.15, 0.61)]
    [InlineData(300, 1)]
    [InlineData(1000, 0.95)]
    public void Calculate_RepresentativeOutputsAreFiniteAndNonNegative(
        double temperatureKelvin,
        double effectiveEmissivity)
    {
        var result =
            GraybodyLongwaveEmissionCalculator
                .CalculateEmittedFluxWattsPerSquareMeter(
                    temperatureKelvin,
                    effectiveEmissivity);

        Assert.True(
            double.IsFinite(result));

        Assert.True(
            result >= 0);
    }

    [Fact]
    public void Calculate_RejectsFiniteTemperatureWhoseEmissionOverflows()
    {
        Assert.Throws<InvalidOperationException>(
            () =>
                GraybodyLongwaveEmissionCalculator
                    .CalculateEmittedFluxWattsPerSquareMeter(
                        double.MaxValue,
                        effectiveEmissivity: 1));
    }
}
