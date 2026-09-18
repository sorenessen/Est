namespace Est.Simulation.Seasons;

/// <summary>
/// Parameters for the deliberately simplified circular-orbit seasonal model.
/// </summary>
public sealed record CircularOrbitSeasonalParameters
{
    public CircularOrbitSeasonalParameters(
        double orbitalPeriodSeconds,
        double axialTiltDegrees,
        double cycleFractionAtTimeZero = 0)
    {
        if (!double.IsFinite(orbitalPeriodSeconds) ||
            orbitalPeriodSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(orbitalPeriodSeconds),
                "Orbital period must be a finite positive duration.");
        }

        if (!double.IsFinite(axialTiltDegrees) ||
            axialTiltDegrees < 0 ||
            axialTiltDegrees > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(axialTiltDegrees),
                "Axial tilt must be finite and in the range [0, 180] degrees.");
        }

        if (!double.IsFinite(cycleFractionAtTimeZero) ||
            cycleFractionAtTimeZero < 0 ||
            cycleFractionAtTimeZero >= 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cycleFractionAtTimeZero),
                "Initial cycle fraction must be finite and in the range [0, 1).");
        }

        OrbitalPeriodSeconds =
            orbitalPeriodSeconds;

        AxialTiltDegrees =
            axialTiltDegrees;

        CycleFractionAtTimeZero =
            cycleFractionAtTimeZero;
    }

    public double OrbitalPeriodSeconds { get; }

    public double AxialTiltDegrees { get; }

    public double CycleFractionAtTimeZero { get; }
}
