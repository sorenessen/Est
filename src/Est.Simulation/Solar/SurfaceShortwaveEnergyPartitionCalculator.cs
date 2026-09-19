namespace Est.Simulation.Solar;

/// <summary>
/// Partitions surface-downwelling shortwave into reflected and absorbed
/// components using an explicit effective broadband surface albedo.
///
/// This is an optical energy-accounting model, not a thermal model.
/// </summary>
public static class SurfaceShortwaveEnergyPartitionCalculator
{
    public static SurfaceShortwaveEnergyPartition Calculate(
        double incomingSurfaceShortwaveFactor,
        double surfaceAlbedo)
    {
        if (!double.IsFinite(incomingSurfaceShortwaveFactor) ||
            incomingSurfaceShortwaveFactor < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(incomingSurfaceShortwaveFactor),
                "Incoming surface shortwave must be finite and non-negative.");
        }

        if (!double.IsFinite(surfaceAlbedo) ||
            surfaceAlbedo < 0 ||
            surfaceAlbedo > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(surfaceAlbedo),
                "Surface albedo must be finite and in the range [0, 1].");
        }

        var reflectedSurfaceShortwaveFactor =
            incomingSurfaceShortwaveFactor
            *
            surfaceAlbedo;

        var absorbedSurfaceShortwaveFactor =
            incomingSurfaceShortwaveFactor
            -
            reflectedSurfaceShortwaveFactor;

        return new SurfaceShortwaveEnergyPartition(
            incomingSurfaceShortwaveFactor,
            reflectedSurfaceShortwaveFactor,
            absorbedSurfaceShortwaveFactor);
    }
}
