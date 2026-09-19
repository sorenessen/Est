namespace Est.Simulation.Solar;

/// <summary>
/// Energy-conserving optical partition of shortwave radiation arriving at the
/// modeled surface system.
///
/// All values are dimensionless relative to normal-incidence stellar flux.
/// </summary>
public sealed record SurfaceShortwaveEnergyPartition
{
    private const double ConservationTolerance = 1e-12;

    public SurfaceShortwaveEnergyPartition(
        double incomingSurfaceShortwaveFactor,
        double reflectedSurfaceShortwaveFactor,
        double absorbedSurfaceShortwaveFactor)
    {
        ValidateNonNegativeFinite(
            incomingSurfaceShortwaveFactor,
            nameof(incomingSurfaceShortwaveFactor));

        ValidateNonNegativeFinite(
            reflectedSurfaceShortwaveFactor,
            nameof(reflectedSurfaceShortwaveFactor));

        ValidateNonNegativeFinite(
            absorbedSurfaceShortwaveFactor,
            nameof(absorbedSurfaceShortwaveFactor));

        if (reflectedSurfaceShortwaveFactor >
            incomingSurfaceShortwaveFactor)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reflectedSurfaceShortwaveFactor),
                "Reflected surface shortwave cannot exceed incoming surface shortwave.");
        }

        if (absorbedSurfaceShortwaveFactor >
            incomingSurfaceShortwaveFactor)
        {
            throw new ArgumentOutOfRangeException(
                nameof(absorbedSurfaceShortwaveFactor),
                "Absorbed surface shortwave cannot exceed incoming surface shortwave.");
        }

        var accountedShortwave =
            reflectedSurfaceShortwaveFactor
            +
            absorbedSurfaceShortwaveFactor;

        if (Math.Abs(
                accountedShortwave
                -
                incomingSurfaceShortwaveFactor)
            >
            ConservationTolerance)
        {
            throw new ArgumentException(
                "Surface shortwave partition must conserve incoming shortwave energy.");
        }

        IncomingSurfaceShortwaveFactor =
            incomingSurfaceShortwaveFactor;

        ReflectedSurfaceShortwaveFactor =
            reflectedSurfaceShortwaveFactor;

        AbsorbedSurfaceShortwaveFactor =
            absorbedSurfaceShortwaveFactor;
    }

    public double IncomingSurfaceShortwaveFactor { get; }

    public double ReflectedSurfaceShortwaveFactor { get; }

    public double AbsorbedSurfaceShortwaveFactor { get; }

    private static void ValidateNonNegativeFinite(
        double value,
        string parameterName)
    {
        if (!double.IsFinite(value) ||
            value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Surface shortwave factors must be finite and non-negative.");
        }
    }
}
