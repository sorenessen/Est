using Est.Simulation.Solar;

namespace Est.Simulation.Tests.Solar;

public sealed class AtmosphericShortwaveEnergyPartitionCalculatorTests
{
    [Fact]
    public void CalculateInstantaneous_IsDeterministicForSameInputs()
    {
        var first =
            AtmosphericShortwaveEnergyPartitionCalculator
                .CalculateInstantaneous(
                    solarZenithCosine: 0.65,
                    verticalOpticalDepth: 0.2,
                    singleScatteringAlbedo: 0.7,
                    downwardScatteringFraction: 0.55);

        var second =
            AtmosphericShortwaveEnergyPartitionCalculator
                .CalculateInstantaneous(
                    solarZenithCosine: 0.65,
                    verticalOpticalDepth: 0.2,
                    singleScatteringAlbedo: 0.7,
                    downwardScatteringFraction: 0.55);

        Assert.Equal(
            first,
            second);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.25)]
    [InlineData(-1)]
    public void CalculateInstantaneous_AtOrBelowHorizonProducesZeroPartition(
        double solarZenithCosine)
    {
        var result =
            AtmosphericShortwaveEnergyPartitionCalculator
                .CalculateInstantaneous(
                    solarZenithCosine,
                    verticalOpticalDepth: 0.2,
                    singleScatteringAlbedo: 0.7,
                    downwardScatteringFraction: 0.5);

        Assert.Equal(
            0,
            result.TopOfAtmosphereInsolationFactor);

        Assert.Equal(
            0,
            result.DirectSurfaceInsolationFactor);

        Assert.Equal(
            0,
            result.DownwardDiffuseSurfaceInsolationFactor);

        Assert.Equal(
            0,
            result.TotalSurfaceDownwellingInsolationFactor);

        Assert.Equal(
            0,
            result.AtmosphericAbsorbedInsolationFactor);

        Assert.Equal(
            0,
            result.UpwardScatteredInsolationFactor);
    }

    [Fact]
    public void CalculateInstantaneous_ZeroOpticalDepthProducesOnlyDirectSurfaceRadiation()
    {
        const double solarZenithCosine = 0.6;

        var result =
            AtmosphericShortwaveEnergyPartitionCalculator
                .CalculateInstantaneous(
                    solarZenithCosine,
                    verticalOpticalDepth: 0,
                    singleScatteringAlbedo: 0.8,
                    downwardScatteringFraction: 0.4);

        Assert.Equal(
            solarZenithCosine,
            result.TopOfAtmosphereInsolationFactor,
            precision: 12);

        Assert.Equal(
            solarZenithCosine,
            result.DirectSurfaceInsolationFactor,
            precision: 12);

        Assert.Equal(
            0,
            result.DownwardDiffuseSurfaceInsolationFactor,
            precision: 12);

        Assert.Equal(
            0,
            result.AtmosphericAbsorbedInsolationFactor,
            precision: 12);

        Assert.Equal(
            0,
            result.UpwardScatteredInsolationFactor,
            precision: 12);
    }

    [Fact]
    public void CalculateInstantaneous_PureAbsorptionSendsExtinctionToAtmosphere()
    {
        const double solarZenithCosine = 0.7;

        var direct =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDirectBeam(
                    solarZenithCosine,
                    verticalOpticalDepth: 0.4);

        var result =
            AtmosphericShortwaveEnergyPartitionCalculator
                .CalculateInstantaneous(
                    solarZenithCosine,
                    verticalOpticalDepth: 0.4,
                    singleScatteringAlbedo: 0,
                    downwardScatteringFraction: 0.5);

        var expectedAbsorption =
            solarZenithCosine
            -
            direct.DirectHorizontalInsolationFactor;

        Assert.Equal(
            expectedAbsorption,
            result.AtmosphericAbsorbedInsolationFactor,
            precision: 12);

        Assert.Equal(
            0,
            result.DownwardDiffuseSurfaceInsolationFactor,
            precision: 12);

        Assert.Equal(
            0,
            result.UpwardScatteredInsolationFactor,
            precision: 12);
    }

    [Fact]
    public void CalculateInstantaneous_PureScatteringProducesNoAtmosphericAbsorption()
    {
        var result =
            AtmosphericShortwaveEnergyPartitionCalculator
                .CalculateInstantaneous(
                    solarZenithCosine: 0.7,
                    verticalOpticalDepth: 0.4,
                    singleScatteringAlbedo: 1,
                    downwardScatteringFraction: 0.5);

        Assert.Equal(
            0,
            result.AtmosphericAbsorbedInsolationFactor,
            precision: 12);

        Assert.True(
            result.DownwardDiffuseSurfaceInsolationFactor > 0);

        Assert.True(
            result.UpwardScatteredInsolationFactor > 0);
    }

    [Fact]
    public void CalculateInstantaneous_NoDownwardScatteringSendsNoDiffuseToSurface()
    {
        var result =
            AtmosphericShortwaveEnergyPartitionCalculator
                .CalculateInstantaneous(
                    solarZenithCosine: 0.7,
                    verticalOpticalDepth: 0.4,
                    singleScatteringAlbedo: 1,
                    downwardScatteringFraction: 0);

        Assert.Equal(
            0,
            result.DownwardDiffuseSurfaceInsolationFactor,
            precision: 12);

        Assert.True(
            result.UpwardScatteredInsolationFactor > 0);
    }

    [Fact]
    public void CalculateInstantaneous_AllDownwardScatteringSendsNoScatteringUpward()
    {
        const double solarZenithCosine = 0.7;

        var result =
            AtmosphericShortwaveEnergyPartitionCalculator
                .CalculateInstantaneous(
                    solarZenithCosine,
                    verticalOpticalDepth: 0.4,
                    singleScatteringAlbedo: 1,
                    downwardScatteringFraction: 1);

        Assert.Equal(
            0,
            result.UpwardScatteredInsolationFactor,
            precision: 12);

        Assert.Equal(
            solarZenithCosine,
            result.TotalSurfaceDownwellingInsolationFactor,
            precision: 12);
    }

    [Theory]
    [InlineData(0.1, 0.01, 0.1, 0.2)]
    [InlineData(0.25, 0.2, 0.6, 0.4)]
    [InlineData(0.5, 1.0, 0.9, 0.75)]
    [InlineData(0.8, 0.4, 1.0, 0.0)]
    [InlineData(1.0, 5.0, 1.0, 1.0)]
    public void CalculateInstantaneous_ConservesIncomingEnergy(
        double solarZenithCosine,
        double verticalOpticalDepth,
        double singleScatteringAlbedo,
        double downwardScatteringFraction)
    {
        var result =
            AtmosphericShortwaveEnergyPartitionCalculator
                .CalculateInstantaneous(
                    solarZenithCosine,
                    verticalOpticalDepth,
                    singleScatteringAlbedo,
                    downwardScatteringFraction);

        var accounted =
            result.DirectSurfaceInsolationFactor
            +
            result.DownwardDiffuseSurfaceInsolationFactor
            +
            result.AtmosphericAbsorbedInsolationFactor
            +
            result.UpwardScatteredInsolationFactor;

        Assert.Equal(
            result.TopOfAtmosphereInsolationFactor,
            accounted,
            precision: 12);
    }

    [Theory]
    [InlineData(0.2, 0.1)]
    [InlineData(0.6, 0.4)]
    [InlineData(1.0, 2.0)]
    public void CalculateInstantaneous_DirectSurfaceMatchesTransmissionFoundation(
        double solarZenithCosine,
        double verticalOpticalDepth)
    {
        var expected =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDirectBeam(
                    solarZenithCosine,
                    verticalOpticalDepth)
                .DirectHorizontalInsolationFactor;

        var actual =
            AtmosphericShortwaveEnergyPartitionCalculator
                .CalculateInstantaneous(
                    solarZenithCosine,
                    verticalOpticalDepth,
                    singleScatteringAlbedo: 0.7,
                    downwardScatteringFraction: 0.5)
                .DirectSurfaceInsolationFactor;

        Assert.Equal(
            expected,
            actual);
    }

    [Fact]
    public void CalculateInstantaneous_SurfaceDownwellingEqualsDirectPlusDiffuse()
    {
        var result =
            AtmosphericShortwaveEnergyPartitionCalculator
                .CalculateInstantaneous(
                    solarZenithCosine: 0.65,
                    verticalOpticalDepth: 0.3,
                    singleScatteringAlbedo: 0.8,
                    downwardScatteringFraction: 0.6);

        Assert.Equal(
            result.DirectSurfaceInsolationFactor
            +
            result.DownwardDiffuseSurfaceInsolationFactor,
            result.TotalSurfaceDownwellingInsolationFactor,
            precision: 12);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void CalculateInstantaneous_RejectsInvalidSingleScatteringAlbedo(
        double singleScatteringAlbedo)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                AtmosphericShortwaveEnergyPartitionCalculator
                    .CalculateInstantaneous(
                        solarZenithCosine: 0.5,
                        verticalOpticalDepth: 0.2,
                        singleScatteringAlbedo,
                        downwardScatteringFraction: 0.5));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void CalculateInstantaneous_RejectsInvalidDownwardScatteringFraction(
        double downwardScatteringFraction)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                AtmosphericShortwaveEnergyPartitionCalculator
                    .CalculateInstantaneous(
                        solarZenithCosine: 0.5,
                        verticalOpticalDepth: 0.2,
                        singleScatteringAlbedo: 0.7,
                        downwardScatteringFraction));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void CalculateInstantaneous_RejectsInvalidOpticalDepth(
        double verticalOpticalDepth)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                AtmosphericShortwaveEnergyPartitionCalculator
                    .CalculateInstantaneous(
                        solarZenithCosine: 0.5,
                        verticalOpticalDepth,
                        singleScatteringAlbedo: 0.7,
                        downwardScatteringFraction: 0.5));
    }

    [Fact]
    public void CalculateDailyMean_IsDeterministicForSameInputs()
    {
        var first =
            AtmosphericShortwaveEnergyPartitionCalculator
                .CalculateDailyMean(
                    latitudeDegrees: 47,
                    subsolarLatitudeDegrees: 18,
                    verticalOpticalDepth: 0.2,
                    singleScatteringAlbedo: 0.7,
                    downwardScatteringFraction: 0.55);

        var second =
            AtmosphericShortwaveEnergyPartitionCalculator
                .CalculateDailyMean(
                    latitudeDegrees: 47,
                    subsolarLatitudeDegrees: 18,
                    verticalOpticalDepth: 0.2,
                    singleScatteringAlbedo: 0.7,
                    downwardScatteringFraction: 0.55);

        Assert.Equal(
            first,
            second);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(45, 23.5)]
    [InlineData(-45, -23.5)]
    [InlineData(80, 23.5)]
    [InlineData(-80, 23.5)]
    public void CalculateDailyMean_ZeroOpticalDepthProducesOnlyDirectRadiation(
        double latitudeDegrees,
        double subsolarLatitudeDegrees)
    {
        var result =
            AtmosphericShortwaveEnergyPartitionCalculator
                .CalculateDailyMean(
                    latitudeDegrees,
                    subsolarLatitudeDegrees,
                    verticalOpticalDepth: 0,
                    singleScatteringAlbedo: 0.8,
                    downwardScatteringFraction: 0.6);

        Assert.Equal(
            result.TopOfAtmosphereInsolationFactor,
            result.DirectSurfaceInsolationFactor);

        Assert.Equal(
            0,
            result.DownwardDiffuseSurfaceInsolationFactor);

        Assert.Equal(
            0,
            result.AtmosphericAbsorbedInsolationFactor);

        Assert.Equal(
            0,
            result.UpwardScatteredInsolationFactor);
    }

    [Theory]
    [InlineData(0, 0, 0.1)]
    [InlineData(45, 23.5, 0.2)]
    [InlineData(-45, -23.5, 0.5)]
    [InlineData(80, 23.5, 1.0)]
    public void CalculateDailyMean_DirectSurfaceMatchesTransmissionFoundation(
        double latitudeDegrees,
        double subsolarLatitudeDegrees,
        double verticalOpticalDepth)
    {
        var expected =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDailyMeanDirectSurfaceInsolationFactor(
                    latitudeDegrees,
                    subsolarLatitudeDegrees,
                    verticalOpticalDepth);

        var actual =
            AtmosphericShortwaveEnergyPartitionCalculator
                .CalculateDailyMean(
                    latitudeDegrees,
                    subsolarLatitudeDegrees,
                    verticalOpticalDepth,
                    singleScatteringAlbedo: 0.7,
                    downwardScatteringFraction: 0.5)
                .DirectSurfaceInsolationFactor;

        Assert.Equal(
            expected,
            actual);
    }

    [Theory]
    [InlineData(0, 0, 0.1, 0.5, 0.5)]
    [InlineData(45, 23.5, 0.2, 0.8, 0.6)]
    [InlineData(-45, -23.5, 0.5, 0.3, 0.2)]
    [InlineData(80, 23.5, 1.0, 1.0, 1.0)]
    [InlineData(-80, 23.5, 0.4, 0.9, 0.7)]
    public void CalculateDailyMean_ConservesIncomingEnergy(
        double latitudeDegrees,
        double subsolarLatitudeDegrees,
        double verticalOpticalDepth,
        double singleScatteringAlbedo,
        double downwardScatteringFraction)
    {
        var result =
            AtmosphericShortwaveEnergyPartitionCalculator
                .CalculateDailyMean(
                    latitudeDegrees,
                    subsolarLatitudeDegrees,
                    verticalOpticalDepth,
                    singleScatteringAlbedo,
                    downwardScatteringFraction);

        var accounted =
            result.DirectSurfaceInsolationFactor
            +
            result.DownwardDiffuseSurfaceInsolationFactor
            +
            result.AtmosphericAbsorbedInsolationFactor
            +
            result.UpwardScatteredInsolationFactor;

        Assert.Equal(
            result.TopOfAtmosphereInsolationFactor,
            accounted,
            precision: 12);
    }

    [Fact]
    public void CalculateDailyMean_PureAbsorptionProducesNoScatteredEnergy()
    {
        var result =
            AtmosphericShortwaveEnergyPartitionCalculator
                .CalculateDailyMean(
                    latitudeDegrees: 35,
                    subsolarLatitudeDegrees: 20,
                    verticalOpticalDepth: 0.4,
                    singleScatteringAlbedo: 0,
                    downwardScatteringFraction: 0.5);

        Assert.Equal(
            0,
            result.DownwardDiffuseSurfaceInsolationFactor,
            precision: 12);

        Assert.Equal(
            0,
            result.UpwardScatteredInsolationFactor,
            precision: 12);

        Assert.True(
            result.AtmosphericAbsorbedInsolationFactor > 0);
    }

    [Fact]
    public void CalculateDailyMean_PureDownwardScatteringPreservesSurfaceDirectedBudget()
    {
        var result =
            AtmosphericShortwaveEnergyPartitionCalculator
                .CalculateDailyMean(
                    latitudeDegrees: 35,
                    subsolarLatitudeDegrees: 20,
                    verticalOpticalDepth: 0.4,
                    singleScatteringAlbedo: 1,
                    downwardScatteringFraction: 1);

        Assert.Equal(
            result.TopOfAtmosphereInsolationFactor,
            result.TotalSurfaceDownwellingInsolationFactor,
            precision: 12);

        Assert.Equal(
            0,
            result.AtmosphericAbsorbedInsolationFactor,
            precision: 12);

        Assert.Equal(
            0,
            result.UpwardScatteredInsolationFactor,
            precision: 12);
    }

    [Fact]
    public void CalculateDailyMean_RemainsFiniteAndNonNegativeAcrossRepresentativeInputs()
    {
        double[] opticalDepths =
        [
            0,
            0.1,
            0.5,
            2
        ];

        double[] scatteringAlbedos =
        [
            0,
            0.5,
            1
        ];

        double[] downwardFractions =
        [
            0,
            0.5,
            1
        ];

        for (var latitude = -90;
             latitude <= 90;
             latitude += 30)
        {
            for (var subsolarLatitude = -60;
                 subsolarLatitude <= 60;
                 subsolarLatitude += 30)
            {
                foreach (var opticalDepth in opticalDepths)
                {
                    foreach (var scatteringAlbedo in scatteringAlbedos)
                    {
                        foreach (var downwardFraction in downwardFractions)
                        {
                            var result =
                                AtmosphericShortwaveEnergyPartitionCalculator
                                    .CalculateDailyMean(
                                        latitude,
                                        subsolarLatitude,
                                        opticalDepth,
                                        scatteringAlbedo,
                                        downwardFraction);

                            double[] factors =
                            [
                                result.TopOfAtmosphereInsolationFactor,
                                result.DirectSurfaceInsolationFactor,
                                result.DownwardDiffuseSurfaceInsolationFactor,
                                result.TotalSurfaceDownwellingInsolationFactor,
                                result.AtmosphericAbsorbedInsolationFactor,
                                result.UpwardScatteredInsolationFactor
                            ];

                            foreach (var factor in factors)
                            {
                                Assert.True(
                                    double.IsFinite(factor));

                                Assert.InRange(
                                    factor,
                                    0,
                                    1);
                            }
                        }
                    }
                }
            }
        }
    }
}
