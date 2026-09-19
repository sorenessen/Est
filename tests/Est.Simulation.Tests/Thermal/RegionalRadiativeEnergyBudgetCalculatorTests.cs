using Est.Simulation.Thermal;

namespace Est.Simulation.Tests.Thermal;

public sealed class RegionalRadiativeEnergyBudgetCalculatorTests
{
    [Fact]
    public void Calculate_IsDeterministicForIdenticalInputs()
    {
        var first =
            CalculateRepresentative();

        var second =
            CalculateRepresentative();

        Assert.Equal(
            first,
            second);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Calculate_RejectsInvalidSurfaceAbsorbedShortwave(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                RegionalRadiativeEnergyBudgetCalculator.Calculate(
                    value,
                    atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter: 70,
                    surfaceTemperatureKelvin: 288,
                    atmosphericTemperatureKelvin: 255,
                    surfaceEffectiveLongwaveEmissivity: 0.96,
                    atmosphericEffectiveLongwaveEmissivity: 0.78));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Calculate_RejectsInvalidAtmosphericAbsorbedShortwave(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                RegionalRadiativeEnergyBudgetCalculator.Calculate(
                    surfaceAbsorbedShortwaveFluxWattsPerSquareMeter: 170,
                    value,
                    surfaceTemperatureKelvin: 288,
                    atmosphericTemperatureKelvin: 255,
                    surfaceEffectiveLongwaveEmissivity: 0.96,
                    atmosphericEffectiveLongwaveEmissivity: 0.78));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Calculate_RejectsInvalidSurfaceTemperature(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                RegionalRadiativeEnergyBudgetCalculator.Calculate(
                    surfaceAbsorbedShortwaveFluxWattsPerSquareMeter: 170,
                    atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter: 70,
                    value,
                    atmosphericTemperatureKelvin: 255,
                    surfaceEffectiveLongwaveEmissivity: 0.96,
                    atmosphericEffectiveLongwaveEmissivity: 0.78));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Calculate_RejectsInvalidAtmosphericTemperature(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                RegionalRadiativeEnergyBudgetCalculator.Calculate(
                    surfaceAbsorbedShortwaveFluxWattsPerSquareMeter: 170,
                    atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter: 70,
                    surfaceTemperatureKelvin: 288,
                    value,
                    surfaceEffectiveLongwaveEmissivity: 0.96,
                    atmosphericEffectiveLongwaveEmissivity: 0.78));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Calculate_RejectsInvalidSurfaceEmissivity(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                RegionalRadiativeEnergyBudgetCalculator.Calculate(
                    surfaceAbsorbedShortwaveFluxWattsPerSquareMeter: 170,
                    atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter: 70,
                    surfaceTemperatureKelvin: 288,
                    atmosphericTemperatureKelvin: 255,
                    value,
                    atmosphericEffectiveLongwaveEmissivity: 0.78));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Calculate_RejectsInvalidAtmosphericEmissivity(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                RegionalRadiativeEnergyBudgetCalculator.Calculate(
                    surfaceAbsorbedShortwaveFluxWattsPerSquareMeter: 170,
                    atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter: 70,
                    surfaceTemperatureKelvin: 288,
                    atmosphericTemperatureKelvin: 255,
                    surfaceEffectiveLongwaveEmissivity: 0.96,
                    value));
    }

    [Fact]
    public void Calculate_ReusesGraybodyEmissionFoundation()
    {
        const double surfaceTemperature = 288;
        const double atmosphericTemperature = 255;
        const double surfaceEmissivity = 0.96;
        const double atmosphericEmissivity = 0.78;

        var budget =
            RegionalRadiativeEnergyBudgetCalculator.Calculate(
                surfaceAbsorbedShortwaveFluxWattsPerSquareMeter: 170,
                atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter: 70,
                surfaceTemperature,
                atmosphericTemperature,
                surfaceEmissivity,
                atmosphericEmissivity);

        Assert.Equal(
            GraybodyLongwaveEmissionCalculator
                .CalculateEmittedFluxWattsPerSquareMeter(
                    surfaceTemperature,
                    surfaceEmissivity),
            budget.SurfaceLongwaveEmissionWattsPerSquareMeter);

        Assert.Equal(
            GraybodyLongwaveEmissionCalculator
                .CalculateEmittedFluxWattsPerSquareMeter(
                    atmosphericTemperature,
                    atmosphericEmissivity),
            budget
                .AtmosphericLongwaveEmissionPerFaceWattsPerSquareMeter);
    }

    [Fact]
    public void Calculate_ZeroTemperaturesProduceNoLongwaveTransfer()
    {
        var budget =
            RegionalRadiativeEnergyBudgetCalculator.Calculate(
                surfaceAbsorbedShortwaveFluxWattsPerSquareMeter: 12,
                atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter: 8,
                surfaceTemperatureKelvin: 0,
                atmosphericTemperatureKelvin: 0,
                surfaceEffectiveLongwaveEmissivity: 0.9,
                atmosphericEffectiveLongwaveEmissivity: 0.7);

        Assert.Equal(
            0,
            budget.SurfaceLongwaveEmissionWattsPerSquareMeter);

        Assert.Equal(
            0,
            budget.AtmosphericLongwaveEmissionPerFaceWattsPerSquareMeter);

        Assert.Equal(
            12,
            budget.SurfaceNetRadiativeFluxWattsPerSquareMeter);

        Assert.Equal(
            8,
            budget.AtmosphericNetRadiativeFluxWattsPerSquareMeter);

        Assert.Equal(
            0,
            budget.OutgoingLongwaveFluxToSpaceWattsPerSquareMeter);
    }

    [Fact]
    public void Calculate_TransparentAtmosphereDoesNotAbsorbOrEmitLongwave()
    {
        var budget =
            RegionalRadiativeEnergyBudgetCalculator.Calculate(
                surfaceAbsorbedShortwaveFluxWattsPerSquareMeter: 0,
                atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter: 0,
                surfaceTemperatureKelvin: 300,
                atmosphericTemperatureKelvin: 300,
                surfaceEffectiveLongwaveEmissivity: 1,
                atmosphericEffectiveLongwaveEmissivity: 0);

        Assert.Equal(
            0,
            budget.AtmosphericLongwaveEmissionPerFaceWattsPerSquareMeter);

        Assert.Equal(
            0,
            budget.SurfaceLongwaveAbsorbedByAtmosphereWattsPerSquareMeter);

        Assert.Equal(
            budget.SurfaceLongwaveEmissionWattsPerSquareMeter,
            budget.SurfaceLongwaveTransmittedToSpaceWattsPerSquareMeter);

        Assert.Equal(
            budget.SurfaceLongwaveEmissionWattsPerSquareMeter,
            budget.OutgoingLongwaveFluxToSpaceWattsPerSquareMeter);
    }

    [Fact]
    public void Calculate_OpaqueAtmosphereEliminatesDirectSurfaceLongwaveEscape()
    {
        var budget =
            RegionalRadiativeEnergyBudgetCalculator.Calculate(
                surfaceAbsorbedShortwaveFluxWattsPerSquareMeter: 0,
                atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter: 0,
                surfaceTemperatureKelvin: 300,
                atmosphericTemperatureKelvin: 250,
                surfaceEffectiveLongwaveEmissivity: 0.9,
                atmosphericEffectiveLongwaveEmissivity: 1);

        Assert.Equal(
            0,
            budget.SurfaceLongwaveTransmittedToSpaceWattsPerSquareMeter);

        Assert.Equal(
            0,
            budget.ReflectedLongwaveTransmittedToSpaceWattsPerSquareMeter);
    }

    [Fact]
    public void Calculate_ZeroSurfaceEmissivityReflectsAllDownwardAtmosphericLongwave()
    {
        var budget =
            RegionalRadiativeEnergyBudgetCalculator.Calculate(
                surfaceAbsorbedShortwaveFluxWattsPerSquareMeter: 0,
                atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter: 0,
                surfaceTemperatureKelvin: 300,
                atmosphericTemperatureKelvin: 250,
                surfaceEffectiveLongwaveEmissivity: 0,
                atmosphericEffectiveLongwaveEmissivity: 0.8);

        Assert.Equal(
            0,
            budget.SurfaceLongwaveEmissionWattsPerSquareMeter);

        Assert.Equal(
            0,
            budget
                .AtmosphericDownwardLongwaveAbsorbedBySurfaceWattsPerSquareMeter);

        Assert.Equal(
            budget.AtmosphericLongwaveEmissionPerFaceWattsPerSquareMeter,
            budget
                .AtmosphericDownwardLongwaveReflectedBySurfaceWattsPerSquareMeter);
    }

    [Fact]
    public void Calculate_UnitSurfaceEmissivityAbsorbsAllDownwardAtmosphericLongwave()
    {
        var budget =
            RegionalRadiativeEnergyBudgetCalculator.Calculate(
                surfaceAbsorbedShortwaveFluxWattsPerSquareMeter: 0,
                atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter: 0,
                surfaceTemperatureKelvin: 300,
                atmosphericTemperatureKelvin: 250,
                surfaceEffectiveLongwaveEmissivity: 1,
                atmosphericEffectiveLongwaveEmissivity: 0.8);

        Assert.Equal(
            budget.AtmosphericLongwaveEmissionPerFaceWattsPerSquareMeter,
            budget
                .AtmosphericDownwardLongwaveAbsorbedBySurfaceWattsPerSquareMeter);

        Assert.Equal(
            0,
            budget
                .AtmosphericDownwardLongwaveReflectedBySurfaceWattsPerSquareMeter);

        Assert.Equal(
            0,
            budget.ReflectedLongwaveReabsorbedByAtmosphereWattsPerSquareMeter);

        Assert.Equal(
            0,
            budget.ReflectedLongwaveTransmittedToSpaceWattsPerSquareMeter);
    }

    [Fact]
    public void Calculate_SurfaceEmissionPathConservesEnergy()
    {
        var budget =
            CalculateRepresentative();

        Assert.Equal(
            budget.SurfaceLongwaveEmissionWattsPerSquareMeter,
            budget.SurfaceLongwaveAbsorbedByAtmosphereWattsPerSquareMeter
            +
            budget.SurfaceLongwaveTransmittedToSpaceWattsPerSquareMeter,
            precision: 10);
    }

    [Fact]
    public void Calculate_DownwardAtmosphericPathConservesEnergy()
    {
        var budget =
            CalculateRepresentative();

        Assert.Equal(
            budget.AtmosphericLongwaveEmissionPerFaceWattsPerSquareMeter,
            budget
                .AtmosphericDownwardLongwaveAbsorbedBySurfaceWattsPerSquareMeter
            +
            budget
                .AtmosphericDownwardLongwaveReflectedBySurfaceWattsPerSquareMeter,
            precision: 10);
    }

    [Fact]
    public void Calculate_ReflectedLongwavePathConservesEnergy()
    {
        var budget =
            CalculateRepresentative();

        Assert.Equal(
            budget
                .AtmosphericDownwardLongwaveReflectedBySurfaceWattsPerSquareMeter,
            budget.ReflectedLongwaveReabsorbedByAtmosphereWattsPerSquareMeter
            +
            budget.ReflectedLongwaveTransmittedToSpaceWattsPerSquareMeter,
            precision: 10);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(170, 70, 288, 255, 0.96, 0.78)]
    [InlineData(400, 0, 320, 220, 1, 0)]
    [InlineData(0, 120, 240, 290, 0, 1)]
    [InlineData(1000, 500, 450, 350, 0.35, 0.65)]
    public void Calculate_CombinedCellBudgetConservesRadiativeEnergy(
        double surfaceShortwave,
        double atmosphericShortwave,
        double surfaceTemperature,
        double atmosphericTemperature,
        double surfaceEmissivity,
        double atmosphericEmissivity)
    {
        var budget =
            RegionalRadiativeEnergyBudgetCalculator.Calculate(
                surfaceShortwave,
                atmosphericShortwave,
                surfaceTemperature,
                atmosphericTemperature,
                surfaceEmissivity,
                atmosphericEmissivity);

        var incoming =
            surfaceShortwave
            +
            atmosphericShortwave;

        var accounted =
            budget.SurfaceNetRadiativeFluxWattsPerSquareMeter
            +
            budget.AtmosphericNetRadiativeFluxWattsPerSquareMeter
            +
            budget.OutgoingLongwaveFluxToSpaceWattsPerSquareMeter;

        Assert.Equal(
            incoming,
            accounted,
            precision: 9);

        Assert.Equal(
            0,
            budget.CombinedRadiativeConservationErrorWattsPerSquareMeter,
            precision: 9);
    }

    [Fact]
    public void Calculate_AllExplicitTransferMagnitudesAreFiniteAndNonNegative()
    {
        var budget =
            CalculateRepresentative();

        var transfers =
            new[]
            {
                budget.SurfaceAbsorbedShortwaveFluxWattsPerSquareMeter,
                budget.AtmosphericAbsorbedShortwaveFluxWattsPerSquareMeter,
                budget.SurfaceLongwaveEmissionWattsPerSquareMeter,
                budget.AtmosphericLongwaveEmissionPerFaceWattsPerSquareMeter,
                budget.SurfaceLongwaveAbsorbedByAtmosphereWattsPerSquareMeter,
                budget.SurfaceLongwaveTransmittedToSpaceWattsPerSquareMeter,
                budget
                    .AtmosphericDownwardLongwaveAbsorbedBySurfaceWattsPerSquareMeter,
                budget
                    .AtmosphericDownwardLongwaveReflectedBySurfaceWattsPerSquareMeter,
                budget.ReflectedLongwaveReabsorbedByAtmosphereWattsPerSquareMeter,
                budget.ReflectedLongwaveTransmittedToSpaceWattsPerSquareMeter,
                budget.OutgoingLongwaveFluxToSpaceWattsPerSquareMeter
            };

        Assert.All(
            transfers,
            value =>
            {
                Assert.True(
                    double.IsFinite(value));

                Assert.True(
                    value >= 0);
            });
    }

    [Fact]
    public void Calculate_SurfaceNetFluxMayBeNegative()
    {
        var budget =
            RegionalRadiativeEnergyBudgetCalculator.Calculate(
                surfaceAbsorbedShortwaveFluxWattsPerSquareMeter: 0,
                atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter: 0,
                surfaceTemperatureKelvin: 350,
                atmosphericTemperatureKelvin: 100,
                surfaceEffectiveLongwaveEmissivity: 1,
                atmosphericEffectiveLongwaveEmissivity: 0.5);

        Assert.True(
            budget.SurfaceNetRadiativeFluxWattsPerSquareMeter < 0);
    }

    [Fact]
    public void Calculate_AtmosphericNetFluxMayBeNegative()
    {
        var budget =
            RegionalRadiativeEnergyBudgetCalculator.Calculate(
                surfaceAbsorbedShortwaveFluxWattsPerSquareMeter: 0,
                atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter: 0,
                surfaceTemperatureKelvin: 0,
                atmosphericTemperatureKelvin: 300,
                surfaceEffectiveLongwaveEmissivity: 1,
                atmosphericEffectiveLongwaveEmissivity: 1);

        Assert.True(
            budget.AtmosphericNetRadiativeFluxWattsPerSquareMeter < 0);
    }

    [Fact]
    public void Calculate_NetFluxesMayBePositive()
    {
        var budget =
            RegionalRadiativeEnergyBudgetCalculator.Calculate(
                surfaceAbsorbedShortwaveFluxWattsPerSquareMeter: 1000,
                atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter: 1000,
                surfaceTemperatureKelvin: 100,
                atmosphericTemperatureKelvin: 100,
                surfaceEffectiveLongwaveEmissivity: 1,
                atmosphericEffectiveLongwaveEmissivity: 1);

        Assert.True(
            budget.SurfaceNetRadiativeFluxWattsPerSquareMeter > 0);

        Assert.True(
            budget.AtmosphericNetRadiativeFluxWattsPerSquareMeter > 0);
    }

    [Fact]
    public void Calculate_RejectsFiniteInputsThatOverflowBudgetAccounting()
    {
        Assert.Throws<InvalidOperationException>(
            () =>
                RegionalRadiativeEnergyBudgetCalculator.Calculate(
                    surfaceAbsorbedShortwaveFluxWattsPerSquareMeter:
                        double.MaxValue,
                    atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter:
                        double.MaxValue,
                    surfaceTemperatureKelvin: 0,
                    atmosphericTemperatureKelvin: 0,
                    surfaceEffectiveLongwaveEmissivity: 0,
                    atmosphericEffectiveLongwaveEmissivity: 0));
    }

    private static RegionalRadiativeEnergyBudget
        CalculateRepresentative()
    {
        return RegionalRadiativeEnergyBudgetCalculator.Calculate(
            surfaceAbsorbedShortwaveFluxWattsPerSquareMeter: 170,
            atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter: 70,
            surfaceTemperatureKelvin: 288,
            atmosphericTemperatureKelvin: 255,
            surfaceEffectiveLongwaveEmissivity: 0.96,
            atmosphericEffectiveLongwaveEmissivity: 0.78);
    }
}
