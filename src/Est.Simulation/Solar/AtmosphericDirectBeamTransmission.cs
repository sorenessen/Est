namespace Est.Simulation.Solar;

/// <summary>
/// Clear-sky direct-beam shortwave transmission at a surface location.
///
/// Values are dimensionless and describe only the direct solar beam.
/// Scattered diffuse radiation is not represented here.
/// </summary>
public sealed record AtmosphericDirectBeamTransmission
{
    public AtmosphericDirectBeamTransmission(
        double directNormalTransmissionFraction,
        double directHorizontalInsolationFactor)
    {
        if (!double.IsFinite(directNormalTransmissionFraction) ||
            directNormalTransmissionFraction < 0 ||
            directNormalTransmissionFraction > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(directNormalTransmissionFraction),
                "Direct-normal transmission must be finite and in the range [0, 1].");
        }

        if (!double.IsFinite(directHorizontalInsolationFactor) ||
            directHorizontalInsolationFactor < 0 ||
            directHorizontalInsolationFactor > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(directHorizontalInsolationFactor),
                "Direct horizontal insolation factor must be finite and in the range [0, 1].");
        }

        DirectNormalTransmissionFraction =
            directNormalTransmissionFraction;

        DirectHorizontalInsolationFactor =
            directHorizontalInsolationFactor;
    }

    public double DirectNormalTransmissionFraction { get; }

    public double DirectHorizontalInsolationFactor { get; }
}
