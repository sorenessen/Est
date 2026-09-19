namespace Est.Simulation.Solar;

/// <summary>
/// Partitions clear-sky shortwave energy removed from the direct solar beam
/// into atmospheric absorption and effective downward/upward scattering.
///
/// This is a first-order energy-accounting model, not a complete
/// radiative-transfer solution.
/// </summary>
public static class AtmosphericShortwaveEnergyPartitionCalculator
{
    public static AtmosphericShortwaveEnergyPartition CalculateInstantaneous(
        double solarZenithCosine,
        double verticalOpticalDepth,
        double singleScatteringAlbedo,
        double downwardScatteringFraction)
    {
        ValidateUnitFraction(
            singleScatteringAlbedo,
            nameof(singleScatteringAlbedo));

        ValidateUnitFraction(
            downwardScatteringFraction,
            nameof(downwardScatteringFraction));

        var directTransmission =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDirectBeam(
                    solarZenithCosine,
                    verticalOpticalDepth);

        var topOfAtmosphereFactor =
            Math.Max(
                0,
                solarZenithCosine);

        return Partition(
            topOfAtmosphereFactor,
            directTransmission
                .DirectHorizontalInsolationFactor,
            singleScatteringAlbedo,
            downwardScatteringFraction);
    }

    public static AtmosphericShortwaveEnergyPartition CalculateDailyMean(
        double latitudeDegrees,
        double subsolarLatitudeDegrees,
        double verticalOpticalDepth,
        double singleScatteringAlbedo,
        double downwardScatteringFraction)
    {
        ValidateUnitFraction(
            singleScatteringAlbedo,
            nameof(singleScatteringAlbedo));

        ValidateUnitFraction(
            downwardScatteringFraction,
            nameof(downwardScatteringFraction));

        var topOfAtmosphereFactor =
            SurfaceSolarGeometryCalculator
                .Calculate(
                    latitudeDegrees,
                    subsolarLatitudeDegrees)
                .DailyMeanInsolationFactor;

        var directSurfaceFactor =
            AtmosphericShortwaveTransmissionCalculator
                .CalculateDailyMeanDirectSurfaceInsolationFactor(
                    latitudeDegrees,
                    subsolarLatitudeDegrees,
                    verticalOpticalDepth);

        // With time-invariant effective optical properties, integrating each
        // extinction partition over the daylight interval is algebraically
        // equivalent to partitioning the integrated extinguished energy.
        return Partition(
            topOfAtmosphereFactor,
            directSurfaceFactor,
            singleScatteringAlbedo,
            downwardScatteringFraction);
    }

    private static AtmosphericShortwaveEnergyPartition Partition(
        double topOfAtmosphereFactor,
        double directSurfaceFactor,
        double singleScatteringAlbedo,
        double downwardScatteringFraction)
    {
        var extinguishedFactor =
            Math.Max(
                0,
                topOfAtmosphereFactor
                -
                directSurfaceFactor);

        var atmosphericAbsorbedFactor =
            extinguishedFactor
            *
            (1
             -
             singleScatteringAlbedo);

        var scatteredFactor =
            extinguishedFactor
            *
            singleScatteringAlbedo;

        var downwardDiffuseFactor =
            scatteredFactor
            *
            downwardScatteringFraction;

        var upwardScatteredFactor =
            scatteredFactor
            *
            (1
             -
             downwardScatteringFraction);

        return new AtmosphericShortwaveEnergyPartition(
            topOfAtmosphereFactor,
            directSurfaceFactor,
            downwardDiffuseFactor,
            atmosphericAbsorbedFactor,
            upwardScatteredFactor);
    }

    private static void ValidateUnitFraction(
        double value,
        string parameterName)
    {
        if (!double.IsFinite(value) ||
            value < 0 ||
            value > 1)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Optical fractions must be finite and in the range [0, 1].");
        }
    }
}
