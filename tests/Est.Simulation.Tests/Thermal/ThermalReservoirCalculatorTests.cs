using Est.Simulation.Thermal;

namespace Est.Simulation.Tests.Thermal;

public sealed class ThermalReservoirCalculatorTests
{
    [Fact]
    public void Calculate_IsDeterministicForSameInputs()
    {
        var first =
            ThermalReservoirCalculator
                .Calculate(
                    initialTemperatureKelvin: 288,
                    effectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                        1.0e8,
                    netHeatFluxWattsPerSquareMeter: 25,
                    elapsedSeconds: 3600);

        var second =
            ThermalReservoirCalculator
                .Calculate(
                    initialTemperatureKelvin: 288,
                    effectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                        1.0e8,
                    netHeatFluxWattsPerSquareMeter: 25,
                    elapsedSeconds: 3600);

        Assert.Equal(
            first,
            second);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Calculate_RejectsInvalidInitialTemperature(
        double initialTemperatureKelvin)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                ThermalReservoirCalculator
                    .Calculate(
                        initialTemperatureKelvin,
                        effectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                            1000,
                        netHeatFluxWattsPerSquareMeter: 10,
                        elapsedSeconds: 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Calculate_RejectsInvalidArealHeatCapacity(
        double effectiveArealHeatCapacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                ThermalReservoirCalculator
                    .Calculate(
                        initialTemperatureKelvin: 300,
                        effectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                            effectiveArealHeatCapacity,
                        netHeatFluxWattsPerSquareMeter: 10,
                        elapsedSeconds: 1));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Calculate_RejectsInvalidNetHeatFlux(
        double netHeatFluxWattsPerSquareMeter)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                ThermalReservoirCalculator
                    .Calculate(
                        initialTemperatureKelvin: 300,
                        effectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                            1000,
                        netHeatFluxWattsPerSquareMeter,
                        elapsedSeconds: 1));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Calculate_RejectsInvalidElapsedTime(
        double elapsedSeconds)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                ThermalReservoirCalculator
                    .Calculate(
                        initialTemperatureKelvin: 300,
                        effectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                            1000,
                        netHeatFluxWattsPerSquareMeter: 10,
                        elapsedSeconds));
    }

    [Fact]
    public void Calculate_ZeroElapsedTimeProducesNoChange()
    {
        var result =
            ThermalReservoirCalculator
                .Calculate(
                    initialTemperatureKelvin: 300,
                    effectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                        1000,
                    netHeatFluxWattsPerSquareMeter: 50,
                    elapsedSeconds: 0);

        Assert.Equal(
            0,
            result.ArealEnergyChangeJoulesPerSquareMeter);

        Assert.Equal(
            0,
            result.TemperatureChangeKelvin);

        Assert.Equal(
            300,
            result.FinalTemperatureKelvin);
    }

    [Fact]
    public void Calculate_ZeroNetFluxProducesNoChange()
    {
        var result =
            ThermalReservoirCalculator
                .Calculate(
                    initialTemperatureKelvin: 300,
                    effectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                        1000,
                    netHeatFluxWattsPerSquareMeter: 0,
                    elapsedSeconds: 3600);

        Assert.Equal(
            0,
            result.ArealEnergyChangeJoulesPerSquareMeter);

        Assert.Equal(
            0,
            result.TemperatureChangeKelvin);

        Assert.Equal(
            300,
            result.FinalTemperatureKelvin);
    }

    [Fact]
    public void Calculate_PositiveNetFluxWarmsReservoir()
    {
        var result =
            ThermalReservoirCalculator
                .Calculate(
                    initialTemperatureKelvin: 300,
                    effectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                        1000,
                    netHeatFluxWattsPerSquareMeter: 100,
                    elapsedSeconds: 10);

        Assert.Equal(
            1,
            result.TemperatureChangeKelvin);

        Assert.Equal(
            301,
            result.FinalTemperatureKelvin);
    }

    [Fact]
    public void Calculate_NegativeNetFluxCoolsReservoir()
    {
        var result =
            ThermalReservoirCalculator
                .Calculate(
                    initialTemperatureKelvin: 300,
                    effectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                        1000,
                    netHeatFluxWattsPerSquareMeter: -100,
                    elapsedSeconds: 10);

        Assert.Equal(
            -1,
            result.TemperatureChangeKelvin);

        Assert.Equal(
            299,
            result.FinalTemperatureKelvin);
    }

    [Fact]
    public void Calculate_ArealEnergyChangeEqualsFluxTimesTime()
    {
        var result =
            ThermalReservoirCalculator
                .Calculate(
                    initialTemperatureKelvin: 280,
                    effectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                        2000,
                    netHeatFluxWattsPerSquareMeter: 25,
                    elapsedSeconds: 40);

        Assert.Equal(
            1000,
            result.ArealEnergyChangeJoulesPerSquareMeter);
    }

    [Fact]
    public void Calculate_TemperatureChangeEqualsEnergyDividedByHeatCapacity()
    {
        var result =
            ThermalReservoirCalculator
                .Calculate(
                    initialTemperatureKelvin: 280,
                    effectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                        2000,
                    netHeatFluxWattsPerSquareMeter: 25,
                    elapsedSeconds: 40);

        Assert.Equal(
            result.ArealEnergyChangeJoulesPerSquareMeter
            /
            result
                .EffectiveArealHeatCapacityJoulesPerSquareMeterKelvin,
            result.TemperatureChangeKelvin);
    }

    [Fact]
    public void Calculate_EqualAndOppositeFluxesProduceOppositeTemperatureChanges()
    {
        var warming =
            ThermalReservoirCalculator
                .Calculate(
                    initialTemperatureKelvin: 300,
                    effectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                        5000,
                    netHeatFluxWattsPerSquareMeter: 50,
                    elapsedSeconds: 100);

        var cooling =
            ThermalReservoirCalculator
                .Calculate(
                    initialTemperatureKelvin: 300,
                    effectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                        5000,
                    netHeatFluxWattsPerSquareMeter: -50,
                    elapsedSeconds: 100);

        Assert.Equal(
            warming.TemperatureChangeKelvin,
            -cooling.TemperatureChangeKelvin,
            precision: 12);
    }

    [Fact]
    public void Calculate_AllowsResultExactlyAtAbsoluteZero()
    {
        var result =
            ThermalReservoirCalculator
                .Calculate(
                    initialTemperatureKelvin: 10,
                    effectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                        100,
                    netHeatFluxWattsPerSquareMeter: -10,
                    elapsedSeconds: 100);

        Assert.Equal(
            0,
            result.FinalTemperatureKelvin);
    }

    [Fact]
    public void Calculate_RejectsStepThatWouldGoBelowAbsoluteZero()
    {
        Assert.Throws<InvalidOperationException>(
            () =>
                ThermalReservoirCalculator
                    .Calculate(
                        initialTemperatureKelvin: 10,
                        effectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                            100,
                        netHeatFluxWattsPerSquareMeter: -11,
                        elapsedSeconds: 100));
    }

    [Fact]
    public void Calculate_RejectsFiniteInputsWhoseEnergyChangeOverflows()
    {
        Assert.Throws<InvalidOperationException>(
            () =>
                ThermalReservoirCalculator
                    .Calculate(
                        initialTemperatureKelvin: 300,
                        effectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                            1,
                        netHeatFluxWattsPerSquareMeter:
                            double.MaxValue,
                        elapsedSeconds: 2));
    }
}
