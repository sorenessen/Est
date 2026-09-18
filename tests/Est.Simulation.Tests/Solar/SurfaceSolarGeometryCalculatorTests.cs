using Est.Simulation.Planets;
using Est.Simulation.Solar;
using Est.Simulation.Surface;

namespace Est.Simulation.Tests.Solar;

public sealed class SurfaceSolarGeometryCalculatorTests
{
    [Fact]
    public void Calculate_IsDeterministicForSameInputs()
    {
        var first =
            SurfaceSolarGeometryCalculator.Calculate(
                42.5,
                17.25);

        var second =
            SurfaceSolarGeometryCalculator.Calculate(
                42.5,
                17.25);

        Assert.Equal(
            first,
            second);
    }

    [Fact]
    public void Calculate_EquatorialEquinoxHasExpectedGeometry()
    {
        var result =
            SurfaceSolarGeometryCalculator.Calculate(
                latitudeDegrees: 0,
                subsolarLatitudeDegrees: 0);

        Assert.Equal(
            0.5,
            result.DaylightFraction,
            precision: 12);

        Assert.Equal(
            1 / Math.PI,
            result.DailyMeanInsolationFactor,
            precision: 12);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(45)]
    [InlineData(75)]
    public void Calculate_EquinoxIsSymmetricAcrossHemispheres(
        double latitudeDegrees)
    {
        var north =
            SurfaceSolarGeometryCalculator.Calculate(
                latitudeDegrees,
                subsolarLatitudeDegrees: 0);

        var south =
            SurfaceSolarGeometryCalculator.Calculate(
                -latitudeDegrees,
                subsolarLatitudeDegrees: 0);

        Assert.Equal(
            north.DaylightFraction,
            south.DaylightFraction,
            precision: 12);

        Assert.Equal(
            north.DailyMeanInsolationFactor,
            south.DailyMeanInsolationFactor,
            precision: 12);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(20)]
    [InlineData(45)]
    [InlineData(70)]
    public void Calculate_OppositeSolsticesMirrorAcrossHemispheres(
        double latitudeDegrees)
    {
        var northernSummer =
            SurfaceSolarGeometryCalculator.Calculate(
                latitudeDegrees,
                subsolarLatitudeDegrees: 23.5);

        var southernSummer =
            SurfaceSolarGeometryCalculator.Calculate(
                -latitudeDegrees,
                subsolarLatitudeDegrees: -23.5);

        Assert.Equal(
            northernSummer.DaylightFraction,
            southernSummer.DaylightFraction,
            precision: 12);

        Assert.Equal(
            northernSummer.DailyMeanInsolationFactor,
            southernSummer.DailyMeanInsolationFactor,
            precision: 12);
    }

    [Fact]
    public void Calculate_PolarDayAndNightHaveExpectedLimits()
    {
        var polarDay =
            SurfaceSolarGeometryCalculator.Calculate(
                latitudeDegrees: 80,
                subsolarLatitudeDegrees: 23.5);

        var polarNight =
            SurfaceSolarGeometryCalculator.Calculate(
                latitudeDegrees: -80,
                subsolarLatitudeDegrees: 23.5);

        Assert.Equal(
            1,
            polarDay.DaylightFraction,
            precision: 12);

        Assert.True(
            polarDay.DailyMeanInsolationFactor > 0);

        Assert.Equal(
            0,
            polarNight.DaylightFraction,
            precision: 12);

        Assert.Equal(
            0,
            polarNight.DailyMeanInsolationFactor,
            precision: 12);
    }

    [Fact]
    public void Calculate_RemainsBoundedAcrossRepresentativeGeometry()
    {
        for (var latitude = -90;
             latitude <= 90;
             latitude += 5)
        {
            for (var subsolarLatitude = -90;
                 subsolarLatitude <= 90;
                 subsolarLatitude += 5)
            {
                var result =
                    SurfaceSolarGeometryCalculator.Calculate(
                        latitude,
                        subsolarLatitude);

                Assert.InRange(
                    result.DaylightFraction,
                    0,
                    1);

                Assert.InRange(
                    result.DailyMeanInsolationFactor,
                    0,
                    1);
            }
        }
    }

    [Fact]
    public void Calculate_SameLatitudeIsIndependentOfLongitude()
    {
        var grid =
            CreateGrid(
                latitudeBandCount: 18,
                longitudeBandCount: 36);

        var western =
            grid.LocateCell(
                latitudeDegrees: 45,
                longitudeDegrees: -120);

        var eastern =
            grid.LocateCell(
                latitudeDegrees: 45,
                longitudeDegrees: 120);

        Assert.Equal(
            western.CenterLatitudeDegrees,
            eastern.CenterLatitudeDegrees);

        var westernGeometry =
            SurfaceSolarGeometryCalculator.Calculate(
                western.CenterLatitudeDegrees,
                23.5);

        var easternGeometry =
            SurfaceSolarGeometryCalculator.Calculate(
                eastern.CenterLatitudeDegrees,
                23.5);

        Assert.Equal(
            westernGeometry,
            easternGeometry);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(23.5)]
    [InlineData(-23.5)]
    public void AreaWeightedGlobalMean_OnAuthoritativeSurfaceGridApproachesOneQuarter(
        double subsolarLatitudeDegrees)
    {
        var grid =
            CreateGrid(
                latitudeBandCount: 36,
                longitudeBandCount: 72);

        var weightedTotal =
            grid.Cells.Sum(
                cell =>
                    SurfaceSolarGeometryCalculator
                        .Calculate(
                            cell.CenterLatitudeDegrees,
                            subsolarLatitudeDegrees)
                        .DailyMeanInsolationFactor
                    *
                    cell.AreaSquareMeters);

        var globalMean =
            weightedTotal /
            grid.TotalSurfaceAreaSquareMeters;

        Assert.InRange(
            globalMean,
            0.2498,
            0.2502);
    }

    [Theory]
    [InlineData(-90.01, 0)]
    [InlineData(90.01, 0)]
    [InlineData(double.NaN, 0)]
    [InlineData(0, -90.01)]
    [InlineData(0, 90.01)]
    [InlineData(0, double.PositiveInfinity)]
    public void Calculate_RejectsInvalidLatitudeInputs(
        double latitudeDegrees,
        double subsolarLatitudeDegrees)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                SurfaceSolarGeometryCalculator.Calculate(
                    latitudeDegrees,
                    subsolarLatitudeDegrees));
    }

    private static IPlanetSurfaceGrid CreateGrid(
        int latitudeBandCount,
        int longitudeBandCount)
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Solar Geometry Test",
                5.9722e24,
                6_371_000,
                new PlanetEnvironment(
                    288.15,
                    0.71,
                    0.03,
                    AtmosphereState.Vacuum));

        return PlanetSurfaceGridFactory.Create(
            planet,
            SurfaceGridDefinition.LatitudeLongitude(
                latitudeBandCount,
                longitudeBandCount));
    }
}
