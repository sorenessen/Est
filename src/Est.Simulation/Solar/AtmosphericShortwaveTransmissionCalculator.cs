namespace Est.Simulation.Solar;

/// <summary>
/// Calculates clear-sky direct-beam shortwave transmission using an explicit
/// broadband vertical extinction optical depth.
///
/// The model is plane-parallel and uses Beer-Lambert extinction. It does not
/// represent diffuse radiation or derive optical depth from atmospheric
/// composition.
/// </summary>
public static class AtmosphericShortwaveTransmissionCalculator
{
    private const int DailyMeanIntegrationIntervals = 512;

    public static AtmosphericDirectBeamTransmission CalculateDirectBeam(
        double solarZenithCosine,
        double verticalOpticalDepth)
    {
        ValidateSolarZenithCosine(
            solarZenithCosine);

        ValidateVerticalOpticalDepth(
            verticalOpticalDepth);

        if (solarZenithCosine <= 0)
        {
            return new AtmosphericDirectBeamTransmission(
                directNormalTransmissionFraction: 0,
                directHorizontalInsolationFactor: 0);
        }

        var directNormalTransmission =
            CalculateDirectNormalTransmission(
                solarZenithCosine,
                verticalOpticalDepth);

        return new AtmosphericDirectBeamTransmission(
            directNormalTransmission,
            solarZenithCosine *
            directNormalTransmission);
    }

    public static double
        CalculateDailyMeanDirectSurfaceInsolationFactor(
            double latitudeDegrees,
            double subsolarLatitudeDegrees,
            double verticalOpticalDepth)
    {
        ValidateVerticalOpticalDepth(
            verticalOpticalDepth);

        var topOfAtmosphereGeometry =
            SurfaceSolarGeometryCalculator.Calculate(
                latitudeDegrees,
                subsolarLatitudeDegrees);

        if (verticalOpticalDepth == 0)
        {
            return topOfAtmosphereGeometry
                .DailyMeanInsolationFactor;
        }

        var latitudeRadians =
            DegreesToRadians(
                latitudeDegrees);

        var subsolarLatitudeRadians =
            DegreesToRadians(
                subsolarLatitudeDegrees);

        var constantZenithComponent =
            Math.Sin(
                latitudeRadians)
            *
            Math.Sin(
                subsolarLatitudeRadians);

        var rotatingZenithComponent =
            Math.Cos(
                latitudeRadians)
            *
            Math.Cos(
                subsolarLatitudeRadians);

        var maximumZenithCosine =
            constantZenithComponent +
            rotatingZenithComponent;

        if (maximumZenithCosine <= 0)
        {
            return 0;
        }

        var minimumZenithCosine =
            constantZenithComponent -
            rotatingZenithComponent;

        double sunsetHourAngleRadians;

        if (minimumZenithCosine >= 0)
        {
            sunsetHourAngleRadians =
                Math.PI;
        }
        else
        {
            var sunsetHourAngleCosine =
                -constantZenithComponent /
                rotatingZenithComponent;

            sunsetHourAngleRadians =
                Math.Acos(
                    Math.Clamp(
                        sunsetHourAngleCosine,
                        -1,
                        1));
        }

        var integrationStep =
            sunsetHourAngleRadians /
            DailyMeanIntegrationIntervals;

        var weightedSum =
            DirectHorizontalFactor(
                hourAngleRadians: 0,
                constantZenithComponent,
                rotatingZenithComponent,
                verticalOpticalDepth)
            +
            DirectHorizontalFactor(
                sunsetHourAngleRadians,
                constantZenithComponent,
                rotatingZenithComponent,
                verticalOpticalDepth);

        for (var index = 1;
             index < DailyMeanIntegrationIntervals;
             index++)
        {
            var hourAngle =
                index *
                integrationStep;

            var weight =
                index % 2 == 0
                    ? 2
                    : 4;

            weightedSum +=
                weight *
                DirectHorizontalFactor(
                    hourAngle,
                    constantZenithComponent,
                    rotatingZenithComponent,
                    verticalOpticalDepth);
        }

        var daylightHalfIntegral =
            integrationStep *
            weightedSum /
            3;

        // The solar-zenith relation is symmetric about local noon:
        //
        // 1 / (2*pi) * integral[-H0,H0](...) dh
        //   == 1 / pi * integral[0,H0](...) dh
        var dailyMeanDirectSurfaceFactor =
            daylightHalfIntegral /
            Math.PI;

        if (!double.IsFinite(
                dailyMeanDirectSurfaceFactor) ||
            dailyMeanDirectSurfaceFactor < 0)
        {
            throw new InvalidOperationException(
                "Atmospheric shortwave integration produced an invalid result.");
        }

        return Math.Clamp(
            dailyMeanDirectSurfaceFactor,
            0,
            topOfAtmosphereGeometry
                .DailyMeanInsolationFactor);
    }

    private static double DirectHorizontalFactor(
        double hourAngleRadians,
        double constantZenithComponent,
        double rotatingZenithComponent,
        double verticalOpticalDepth)
    {
        var solarZenithCosine =
            constantZenithComponent
            +
            rotatingZenithComponent *
            Math.Cos(
                hourAngleRadians);

        if (solarZenithCosine <= 0)
        {
            return 0;
        }

        return solarZenithCosine
            *
            CalculateDirectNormalTransmission(
                solarZenithCosine,
                verticalOpticalDepth);
    }

    private static double CalculateDirectNormalTransmission(
        double solarZenithCosine,
        double verticalOpticalDepth)
    {
        if (verticalOpticalDepth == 0)
        {
            return 1;
        }

        return Math.Exp(
            -verticalOpticalDepth /
            solarZenithCosine);
    }

    private static void ValidateSolarZenithCosine(
        double solarZenithCosine)
    {
        if (!double.IsFinite(solarZenithCosine) ||
            solarZenithCosine < -1 ||
            solarZenithCosine > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(solarZenithCosine),
                "Solar-zenith cosine must be finite and in the range [-1, 1].");
        }
    }

    private static void ValidateVerticalOpticalDepth(
        double verticalOpticalDepth)
    {
        if (!double.IsFinite(verticalOpticalDepth) ||
            verticalOpticalDepth < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(verticalOpticalDepth),
                "Vertical optical depth must be finite and non-negative.");
        }
    }

    private static double DegreesToRadians(
        double degrees)
    {
        return degrees *
               Math.PI /
               180;
    }
}
