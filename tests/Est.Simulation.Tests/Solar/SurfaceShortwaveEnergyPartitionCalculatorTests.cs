using Est.Simulation.Solar;

namespace Est.Simulation.Tests.Solar;

public sealed class SurfaceShortwaveEnergyPartitionCalculatorTests
{
    [Fact]
    public void Calculate_IsDeterministicForSameInputs()
    {
        var first =
            SurfaceShortwaveEnergyPartitionCalculator
                .Calculate(
                    incomingSurfaceShortwaveFactor: 0.42,
                    surfaceAlbedo: 0.3);

        var second =
            SurfaceShortwaveEnergyPartitionCalculator
                .Calculate(
                    incomingSurfaceShortwaveFactor: 0.42,
                    surfaceAlbedo: 0.3);

        Assert.Equal(
            first,
            second);
    }

    [Fact]
    public void Calculate_ZeroIncomingShortwaveProducesZeroPartition()
    {
        var result =
            SurfaceShortwaveEnergyPartitionCalculator
                .Calculate(
                    incomingSurfaceShortwaveFactor: 0,
                    surfaceAlbedo: 0.65);

        Assert.Equal(
            0,
            result.IncomingSurfaceShortwaveFactor);

        Assert.Equal(
            0,
            result.ReflectedSurfaceShortwaveFactor);

        Assert.Equal(
            0,
            result.AbsorbedSurfaceShortwaveFactor);
    }

    [Fact]
    public void Calculate_ZeroAlbedoProducesCompleteAbsorption()
    {
        const double incoming = 0.7;

        var result =
            SurfaceShortwaveEnergyPartitionCalculator
                .Calculate(
                    incoming,
                    surfaceAlbedo: 0);

        Assert.Equal(
            0,
            result.ReflectedSurfaceShortwaveFactor);

        Assert.Equal(
            incoming,
            result.AbsorbedSurfaceShortwaveFactor);
    }

    [Fact]
    public void Calculate_UnitAlbedoProducesCompleteReflection()
    {
        const double incoming = 0.7;

        var result =
            SurfaceShortwaveEnergyPartitionCalculator
                .Calculate(
                    incoming,
                    surfaceAlbedo: 1);

        Assert.Equal(
            incoming,
            result.ReflectedSurfaceShortwaveFactor);

        Assert.Equal(
            0,
            result.AbsorbedSurfaceShortwaveFactor);
    }

    [Fact]
    public void Calculate_IntermediateAlbedoPartitionsIncomingShortwave()
    {
        var result =
            SurfaceShortwaveEnergyPartitionCalculator
                .Calculate(
                    incomingSurfaceShortwaveFactor: 0.8,
                    surfaceAlbedo: 0.25);

        Assert.Equal(
            0.2,
            result.ReflectedSurfaceShortwaveFactor,
            precision: 12);

        Assert.Equal(
            0.6,
            result.AbsorbedSurfaceShortwaveFactor,
            precision: 12);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0.1, 0.15)]
    [InlineData(0.5, 0.5)]
    [InlineData(1, 0.8)]
    [InlineData(1.5, 0.25)]
    public void Calculate_ConservesIncomingShortwave(
        double incomingSurfaceShortwaveFactor,
        double surfaceAlbedo)
    {
        var result =
            SurfaceShortwaveEnergyPartitionCalculator
                .Calculate(
                    incomingSurfaceShortwaveFactor,
                    surfaceAlbedo);

        Assert.Equal(
            result.IncomingSurfaceShortwaveFactor,
            result.ReflectedSurfaceShortwaveFactor
            +
            result.AbsorbedSurfaceShortwaveFactor,
            precision: 12);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Calculate_RejectsInvalidIncomingShortwave(
        double incomingSurfaceShortwaveFactor)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                SurfaceShortwaveEnergyPartitionCalculator
                    .Calculate(
                        incomingSurfaceShortwaveFactor,
                        surfaceAlbedo: 0.3));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Calculate_RejectsInvalidSurfaceAlbedo(
        double surfaceAlbedo)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                SurfaceShortwaveEnergyPartitionCalculator
                    .Calculate(
                        incomingSurfaceShortwaveFactor: 0.5,
                        surfaceAlbedo));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0.2, 0.1)]
    [InlineData(0.5, 0.4)]
    [InlineData(1, 0.75)]
    [InlineData(5, 1)]
    public void Calculate_ProducesFiniteNonNegativeBoundedOutputs(
        double incomingSurfaceShortwaveFactor,
        double surfaceAlbedo)
    {
        var result =
            SurfaceShortwaveEnergyPartitionCalculator
                .Calculate(
                    incomingSurfaceShortwaveFactor,
                    surfaceAlbedo);

        Assert.True(
            double.IsFinite(
                result.ReflectedSurfaceShortwaveFactor));

        Assert.True(
            double.IsFinite(
                result.AbsorbedSurfaceShortwaveFactor));

        Assert.InRange(
            result.ReflectedSurfaceShortwaveFactor,
            0,
            incomingSurfaceShortwaveFactor);

        Assert.InRange(
            result.AbsorbedSurfaceShortwaveFactor,
            0,
            incomingSurfaceShortwaveFactor);
    }

    [Theory]
    [InlineData(0.2, 0.1, 0.5, 0.4)]
    [InlineData(0.6, 0.3, 0.8, 0.25)]
    [InlineData(1.0, 0.5, 1.0, 0.7)]
    public void Calculate_ComposesWithInstantaneousAtmosphericPartition(
        double solarZenithCosine,
        double verticalOpticalDepth,
        double singleScatteringAlbedo,
        double surfaceAlbedo)
    {
        var atmosphere =
            AtmosphericShortwaveEnergyPartitionCalculator
                .CalculateInstantaneous(
                    solarZenithCosine,
                    verticalOpticalDepth,
                    singleScatteringAlbedo,
                    downwardScatteringFraction: 0.55);

        var surface =
            SurfaceShortwaveEnergyPartitionCalculator
                .Calculate(
                    atmosphere
                        .TotalSurfaceDownwellingInsolationFactor,
                    surfaceAlbedo);

        var accounted =
            atmosphere
                .AtmosphericAbsorbedInsolationFactor
            +
            atmosphere
                .UpwardScatteredInsolationFactor
            +
            surface
                .ReflectedSurfaceShortwaveFactor
            +
            surface
                .AbsorbedSurfaceShortwaveFactor;

        Assert.Equal(
            atmosphere
                .TopOfAtmosphereInsolationFactor,
            accounted,
            precision: 12);
    }

    [Theory]
    [InlineData(0, 0, 0.1, 0.6, 0.2)]
    [InlineData(45, 23.5, 0.2, 0.8, 0.35)]
    [InlineData(-45, -23.5, 0.5, 0.3, 0.7)]
    [InlineData(80, 23.5, 1.0, 1.0, 0.9)]
    [InlineData(-80, 23.5, 0.4, 0.9, 0.15)]
    public void Calculate_ComposesWithDailyMeanAtmosphericPartition(
        double latitudeDegrees,
        double subsolarLatitudeDegrees,
        double verticalOpticalDepth,
        double singleScatteringAlbedo,
        double surfaceAlbedo)
    {
        var atmosphere =
            AtmosphericShortwaveEnergyPartitionCalculator
                .CalculateDailyMean(
                    latitudeDegrees,
                    subsolarLatitudeDegrees,
                    verticalOpticalDepth,
                    singleScatteringAlbedo,
                    downwardScatteringFraction: 0.55);

        var surface =
            SurfaceShortwaveEnergyPartitionCalculator
                .Calculate(
                    atmosphere
                        .TotalSurfaceDownwellingInsolationFactor,
                    surfaceAlbedo);

        var accounted =
            atmosphere
                .AtmosphericAbsorbedInsolationFactor
            +
            atmosphere
                .UpwardScatteredInsolationFactor
            +
            surface
                .ReflectedSurfaceShortwaveFactor
            +
            surface
                .AbsorbedSurfaceShortwaveFactor;

        Assert.Equal(
            atmosphere
                .TopOfAtmosphereInsolationFactor,
            accounted,
            precision: 12);
    }

    [Fact]
    public void Constructor_RejectsNonConservingPartition()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new SurfaceShortwaveEnergyPartition(
                    incomingSurfaceShortwaveFactor: 0.8,
                    reflectedSurfaceShortwaveFactor: 0.2,
                    absorbedSurfaceShortwaveFactor: 0.5));
    }

    [Fact]
    public void Constructor_RejectsReflectedShortwaveAboveIncoming()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new SurfaceShortwaveEnergyPartition(
                    incomingSurfaceShortwaveFactor: 0.5,
                    reflectedSurfaceShortwaveFactor: 0.6,
                    absorbedSurfaceShortwaveFactor: 0));
    }

    [Fact]
    public void Constructor_RejectsAbsorbedShortwaveAboveIncoming()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new SurfaceShortwaveEnergyPartition(
                    incomingSurfaceShortwaveFactor: 0.5,
                    reflectedSurfaceShortwaveFactor: 0,
                    absorbedSurfaceShortwaveFactor: 0.6));
    }
}
