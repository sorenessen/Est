namespace Est.Simulation.Solar;

/// <summary>
/// Calculates latitude-specific daily-mean solar geometry.
///
/// Longitude is intentionally absent because this calculation averages over
/// one complete rotation and does not model local solar time.
/// </summary>
public static class SurfaceSolarGeometryCalculator
{
    public static SurfaceSolarGeometry Calculate(
        double latitudeDegrees,
        double subsolarLatitudeDegrees)
    {
        ValidateLatitude(
            latitudeDegrees,
            nameof(latitudeDegrees));

        ValidateLatitude(
            subsolarLatitudeDegrees,
            nameof(subsolarLatitudeDegrees));

        var latitudeRadians =
            DegreesToRadians(
                latitudeDegrees);

        var subsolarLatitudeRadians =
            DegreesToRadians(
                subsolarLatitudeDegrees);

        var sinLatitude =
            Math.Sin(
                latitudeRadians);

        var cosLatitude =
            Math.Cos(
                latitudeRadians);

        var sinSubsolarLatitude =
            Math.Sin(
                subsolarLatitudeRadians);

        var cosSubsolarLatitude =
            Math.Cos(
                subsolarLatitudeRadians);

        var constantZenithComponent =
            sinLatitude *
            sinSubsolarLatitude;

        var rotatingZenithComponent =
            cosLatitude *
            cosSubsolarLatitude;

        var maximumZenithCosine =
            constantZenithComponent +
            rotatingZenithComponent;

        var minimumZenithCosine =
            constantZenithComponent -
            rotatingZenithComponent;

        double sunsetHourAngleRadians;

        if (maximumZenithCosine <= 0)
        {
            sunsetHourAngleRadians = 0;
        }
        else if (minimumZenithCosine >= 0)
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

        var daylightFraction =
            sunsetHourAngleRadians /
            Math.PI;

        var dailyMeanInsolationFactor =
            (
                sunsetHourAngleRadians *
                constantZenithComponent
                +
                rotatingZenithComponent *
                Math.Sin(
                    sunsetHourAngleRadians)
            )
            /
            Math.PI;

        return new SurfaceSolarGeometry(
            Math.Clamp(
                daylightFraction,
                0,
                1),
            Math.Clamp(
                dailyMeanInsolationFactor,
                0,
                1));
    }

    private static void ValidateLatitude(
        double latitudeDegrees,
        string parameterName)
    {
        if (!double.IsFinite(latitudeDegrees) ||
            latitudeDegrees < -90 ||
            latitudeDegrees > 90)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Latitude must be finite and in the range [-90, 90] degrees.");
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
