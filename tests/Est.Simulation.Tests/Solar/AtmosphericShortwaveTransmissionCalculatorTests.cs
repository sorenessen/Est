using Est.Simulation.Solar;

namespace Est.Simulation.Tests.Solar;

public sealed class AtmosphericShortwaveTransmissionCalculatorTests
{
    [Fact]
    public void CalculateDirectBeam_IsDeterministicForSameInputs()
    {
        var first =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDirectBeam(
                    solarZenithCosine: 0.65,
                    verticalOpticalDepth: 0.2);

        var second =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDirectBeam(
                    solarZenithCosine: 0.65,
                    verticalOpticalDepth: 0.2);

        Assert.Equal(
            first,
            second);
    }

    [Fact]
    public void CalculateDirectBeam_AtZenithUsesBeerLambertRelation()
    {
        const double opticalDepth = 0.2;

        var result =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDirectBeam(
                    solarZenithCosine: 1,
                    verticalOpticalDepth: opticalDepth);

        var expected =
            Math.Exp(
                -opticalDepth);

        Assert.Equal(
            expected,
            result.DirectNormalTransmissionFraction,
            precision: 12);

        Assert.Equal(
            expected,
            result.DirectHorizontalInsolationFactor,
            precision: 12);
    }

    [Fact]
    public void CalculateDirectBeam_ZeroOpticalDepthHasUnitDirectNormalTransmission()
    {
        const double solarZenithCosine = 0.25;

        var result =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDirectBeam(
                    solarZenithCosine,
                    verticalOpticalDepth: 0);

        Assert.Equal(
            1,
            result.DirectNormalTransmissionFraction,
            precision: 12);

        Assert.Equal(
            solarZenithCosine,
            result.DirectHorizontalInsolationFactor,
            precision: 12);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.25)]
    [InlineData(-1)]
    public void CalculateDirectBeam_AtOrBelowHorizonHasNoSurfaceDirectBeam(
        double solarZenithCosine)
    {
        var result =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDirectBeam(
                    solarZenithCosine,
                    verticalOpticalDepth: 0.2);

        Assert.Equal(
            0,
            result.DirectNormalTransmissionFraction);

        Assert.Equal(
            0,
            result.DirectHorizontalInsolationFactor);
    }

    [Theory]
    [InlineData(0.05, 0)]
    [InlineData(0.05, 0.5)]
    [InlineData(0.25, 0.1)]
    [InlineData(0.50, 1)]
    [InlineData(1.00, 0.2)]
    [InlineData(1.00, 10)]
    public void CalculateDirectBeam_RemainsBounded(
        double solarZenithCosine,
        double verticalOpticalDepth)
    {
        var result =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDirectBeam(
                    solarZenithCosine,
                    verticalOpticalDepth);

        Assert.InRange(
            result.DirectNormalTransmissionFraction,
            0,
            1);

        Assert.InRange(
            result.DirectHorizontalInsolationFactor,
            0,
            1);
    }

    [Fact]
    public void CalculateDirectBeam_IncreasingOpticalDepthCannotIncreaseTransmission()
    {
        var clearer =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDirectBeam(
                    solarZenithCosine: 0.6,
                    verticalOpticalDepth: 0.1);

        var hazier =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDirectBeam(
                    solarZenithCosine: 0.6,
                    verticalOpticalDepth: 0.5);

        Assert.True(
            hazier.DirectNormalTransmissionFraction <
            clearer.DirectNormalTransmissionFraction);

        Assert.True(
            hazier.DirectHorizontalInsolationFactor <
            clearer.DirectHorizontalInsolationFactor);
    }

    [Fact]
    public void CalculateDirectBeam_LowerSolarElevationCannotIncreaseTransmission()
    {
        var higherSun =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDirectBeam(
                    solarZenithCosine: 0.8,
                    verticalOpticalDepth: 0.3);

        var lowerSun =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDirectBeam(
                    solarZenithCosine: 0.4,
                    verticalOpticalDepth: 0.3);

        Assert.True(
            lowerSun.DirectNormalTransmissionFraction <
            higherSun.DirectNormalTransmissionFraction);

        Assert.True(
            lowerSun.DirectHorizontalInsolationFactor <
            higherSun.DirectHorizontalInsolationFactor);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void CalculateDirectBeam_RejectsInvalidOpticalDepth(
        double verticalOpticalDepth)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                AtmosphericShortwaveTransmissionCalculator
                    .CalculateDirectBeam(
                        solarZenithCosine: 0.5,
                        verticalOpticalDepth));
    }

    [Theory]
    [InlineData(-1.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void CalculateDirectBeam_RejectsInvalidSolarZenithCosine(
        double solarZenithCosine)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                AtmosphericShortwaveTransmissionCalculator
                    .CalculateDirectBeam(
                        solarZenithCosine,
                        verticalOpticalDepth: 0.2));
    }

    [Fact]
    public void CalculateDailyMean_IsDeterministicForSameInputs()
    {
        var first =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDailyMeanDirectSurfaceInsolationFactor(
                    latitudeDegrees: 47,
                    subsolarLatitudeDegrees: 18,
                    verticalOpticalDepth: 0.2);

        var second =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDailyMeanDirectSurfaceInsolationFactor(
                    latitudeDegrees: 47,
                    subsolarLatitudeDegrees: 18,
                    verticalOpticalDepth: 0.2);

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
    public void CalculateDailyMean_ZeroOpticalDepthReproducesTopOfAtmosphereGeometry(
        double latitudeDegrees,
        double subsolarLatitudeDegrees)
    {
        var expected =
            SurfaceSolarGeometryCalculator
                .Calculate(
                    latitudeDegrees,
                    subsolarLatitudeDegrees)
                .DailyMeanInsolationFactor;

        var actual =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDailyMeanDirectSurfaceInsolationFactor(
                    latitudeDegrees,
                    subsolarLatitudeDegrees,
                    verticalOpticalDepth: 0);

        Assert.Equal(
            expected,
            actual);
    }

    [Theory]
    [InlineData(0, 0, 0.1)]
    [InlineData(45, 23.5, 0.2)]
    [InlineData(-45, -23.5, 0.5)]
    [InlineData(80, 23.5, 1.0)]
    public void CalculateDailyMean_PositiveOpticalDepthDoesNotExceedTopOfAtmosphere(
        double latitudeDegrees,
        double subsolarLatitudeDegrees,
        double verticalOpticalDepth)
    {
        var topOfAtmosphere =
            SurfaceSolarGeometryCalculator
                .Calculate(
                    latitudeDegrees,
                    subsolarLatitudeDegrees)
                .DailyMeanInsolationFactor;

        var surfaceDirect =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDailyMeanDirectSurfaceInsolationFactor(
                    latitudeDegrees,
                    subsolarLatitudeDegrees,
                    verticalOpticalDepth);

        Assert.InRange(
            surfaceDirect,
            0,
            topOfAtmosphere);
    }

    [Fact]
    public void CalculateDailyMean_IncreasingOpticalDepthReducesDirectSurfaceFactor()
    {
        var clearer =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDailyMeanDirectSurfaceInsolationFactor(
                    latitudeDegrees: 35,
                    subsolarLatitudeDegrees: 20,
                    verticalOpticalDepth: 0.1);

        var hazier =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDailyMeanDirectSurfaceInsolationFactor(
                    latitudeDegrees: 35,
                    subsolarLatitudeDegrees: 20,
                    verticalOpticalDepth: 0.6);

        Assert.True(
            hazier < clearer);
    }

    [Fact]
    public void CalculateDailyMean_PolarNightProducesZero()
    {
        var result =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDailyMeanDirectSurfaceInsolationFactor(
                    latitudeDegrees: -80,
                    subsolarLatitudeDegrees: 23.5,
                    verticalOpticalDepth: 0.2);

        Assert.Equal(
            0,
            result);
    }

    [Fact]
    public void CalculateDailyMean_RemainsFiniteAndNonNegativeAcrossRepresentativeInputs()
    {
        double[] opticalDepths =
        [
            0.01,
            0.2,
            1,
            5
        ];

        for (var latitude = -90;
             latitude <= 90;
             latitude += 15)
        {
            for (var subsolarLatitude = -60;
                 subsolarLatitude <= 60;
                 subsolarLatitude += 30)
            {
                foreach (var opticalDepth in opticalDepths)
                {
                    var result =
                        AtmosphericShortwaveTransmissionCalculator
                            .CalculateDailyMeanDirectSurfaceInsolationFactor(
                                latitude,
                                subsolarLatitude,
                                opticalDepth);

                    Assert.True(
                        double.IsFinite(result));

                    Assert.True(
                        result >= 0);

                    var topOfAtmosphere =
                        SurfaceSolarGeometryCalculator
                            .Calculate(
                                latitude,
                                subsolarLatitude)
                            .DailyMeanInsolationFactor;

                    Assert.True(
                        result <= topOfAtmosphere);
                }
            }
        }
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void CalculateDailyMean_RejectsInvalidOpticalDepth(
        double verticalOpticalDepth)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                AtmosphericShortwaveTransmissionCalculator
                    .CalculateDailyMeanDirectSurfaceInsolationFactor(
                        latitudeDegrees: 0,
                        subsolarLatitudeDegrees: 0,
                        verticalOpticalDepth));
    }
}
