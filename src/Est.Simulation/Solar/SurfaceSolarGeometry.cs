namespace Est.Simulation.Solar;

/// <summary>
/// Daily-mean geometric relationship between a surface latitude and the Sun.
///
/// Values are dimensionless. This type does not own stellar flux,
/// atmospheric transmission, albedo, temperature, or biological response.
/// </summary>
public sealed record SurfaceSolarGeometry
{
    public SurfaceSolarGeometry(
        double daylightFraction,
        double dailyMeanInsolationFactor)
    {
        if (!double.IsFinite(daylightFraction) ||
            daylightFraction < 0 ||
            daylightFraction > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(daylightFraction),
                "Daylight fraction must be finite and in the range [0, 1].");
        }

        if (!double.IsFinite(dailyMeanInsolationFactor) ||
            dailyMeanInsolationFactor < 0 ||
            dailyMeanInsolationFactor > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dailyMeanInsolationFactor),
                "Daily-mean insolation factor must be finite and in the range [0, 1].");
        }

        DaylightFraction = daylightFraction;
        DailyMeanInsolationFactor =
            dailyMeanInsolationFactor;
    }

    public double DaylightFraction { get; }

    public double DailyMeanInsolationFactor { get; }
}
