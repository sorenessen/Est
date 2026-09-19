namespace Est.Simulation.Solar;

/// <summary>
/// Energy-conserving partition of incoming horizontal shortwave radiation.
///
/// All values are dimensionless relative to normal-incidence
/// top-of-atmosphere stellar flux.
/// </summary>
public sealed record AtmosphericShortwaveEnergyPartition
{
    private const double ConservationTolerance = 1e-12;

    public AtmosphericShortwaveEnergyPartition(
        double topOfAtmosphereInsolationFactor,
        double directSurfaceInsolationFactor,
        double downwardDiffuseSurfaceInsolationFactor,
        double atmosphericAbsorbedInsolationFactor,
        double upwardScatteredInsolationFactor)
    {
        ValidateFactor(
            topOfAtmosphereInsolationFactor,
            nameof(topOfAtmosphereInsolationFactor));

        ValidateFactor(
            directSurfaceInsolationFactor,
            nameof(directSurfaceInsolationFactor));

        ValidateFactor(
            downwardDiffuseSurfaceInsolationFactor,
            nameof(downwardDiffuseSurfaceInsolationFactor));

        ValidateFactor(
            atmosphericAbsorbedInsolationFactor,
            nameof(atmosphericAbsorbedInsolationFactor));

        ValidateFactor(
            upwardScatteredInsolationFactor,
            nameof(upwardScatteredInsolationFactor));

        var accountedInsolation =
            directSurfaceInsolationFactor
            +
            downwardDiffuseSurfaceInsolationFactor
            +
            atmosphericAbsorbedInsolationFactor
            +
            upwardScatteredInsolationFactor;

        if (Math.Abs(
                accountedInsolation
                -
                topOfAtmosphereInsolationFactor)
            >
            ConservationTolerance)
        {
            throw new ArgumentException(
                "Atmospheric shortwave partition must conserve incoming shortwave energy.");
        }

        TopOfAtmosphereInsolationFactor =
            topOfAtmosphereInsolationFactor;

        DirectSurfaceInsolationFactor =
            directSurfaceInsolationFactor;

        DownwardDiffuseSurfaceInsolationFactor =
            downwardDiffuseSurfaceInsolationFactor;

        AtmosphericAbsorbedInsolationFactor =
            atmosphericAbsorbedInsolationFactor;

        UpwardScatteredInsolationFactor =
            upwardScatteredInsolationFactor;

        TotalSurfaceDownwellingInsolationFactor =
            Math.Clamp(
                directSurfaceInsolationFactor
                +
                downwardDiffuseSurfaceInsolationFactor,
                0,
                1);
    }

    public double TopOfAtmosphereInsolationFactor { get; }

    public double DirectSurfaceInsolationFactor { get; }

    public double DownwardDiffuseSurfaceInsolationFactor { get; }

    public double TotalSurfaceDownwellingInsolationFactor { get; }

    public double AtmosphericAbsorbedInsolationFactor { get; }

    public double UpwardScatteredInsolationFactor { get; }

    private static void ValidateFactor(
        double value,
        string parameterName)
    {
        if (!double.IsFinite(value) ||
            value < 0 ||
            value > 1)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Shortwave energy factors must be finite and in the range [0, 1].");
        }
    }
}
