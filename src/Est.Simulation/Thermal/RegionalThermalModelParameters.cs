namespace Est.Simulation.Thermal;

/// <summary>
/// Explicit spatially uniform policy for the first regional thermal model.
///
/// These values define forcing and effective thermal properties only. They do
/// not constitute durable regional thermal state.
/// </summary>
public sealed record RegionalThermalModelParameters
{
    public RegionalThermalModelParameters(
        double stellarFluxWattsPerSquareMeter,
        double atmosphericShortwaveVerticalOpticalDepth,
        double atmosphericShortwaveSingleScatteringAlbedo,
        double atmosphericShortwaveDownwardScatteringFraction,
        double surfaceShortwaveAlbedo,
        double surfaceLongwaveEmissivity,
        double atmosphericLongwaveEmissivity,
        double surfaceEffectiveArealHeatCapacityJoulesPerSquareMeterKelvin,
        double atmosphericEffectiveArealHeatCapacityJoulesPerSquareMeterKelvin,
        long maximumIntegrationStepSeconds)
    {
        ValidateNonnegativeFinite(
            stellarFluxWattsPerSquareMeter,
            nameof(stellarFluxWattsPerSquareMeter));

        ValidateNonnegativeFinite(
            atmosphericShortwaveVerticalOpticalDepth,
            nameof(atmosphericShortwaveVerticalOpticalDepth));

        ValidateUnitFraction(
            atmosphericShortwaveSingleScatteringAlbedo,
            nameof(atmosphericShortwaveSingleScatteringAlbedo));

        ValidateUnitFraction(
            atmosphericShortwaveDownwardScatteringFraction,
            nameof(atmosphericShortwaveDownwardScatteringFraction));

        ValidateUnitFraction(
            surfaceShortwaveAlbedo,
            nameof(surfaceShortwaveAlbedo));

        ValidateUnitFraction(
            surfaceLongwaveEmissivity,
            nameof(surfaceLongwaveEmissivity));

        ValidateUnitFraction(
            atmosphericLongwaveEmissivity,
            nameof(atmosphericLongwaveEmissivity));

        ValidatePositiveFinite(
            surfaceEffectiveArealHeatCapacityJoulesPerSquareMeterKelvin,
            nameof(surfaceEffectiveArealHeatCapacityJoulesPerSquareMeterKelvin));

        ValidatePositiveFinite(
            atmosphericEffectiveArealHeatCapacityJoulesPerSquareMeterKelvin,
            nameof(atmosphericEffectiveArealHeatCapacityJoulesPerSquareMeterKelvin));

        if (maximumIntegrationStepSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumIntegrationStepSeconds),
                "Maximum regional thermal integration step must be positive.");
        }

        StellarFluxWattsPerSquareMeter =
            stellarFluxWattsPerSquareMeter;

        AtmosphericShortwaveVerticalOpticalDepth =
            atmosphericShortwaveVerticalOpticalDepth;

        AtmosphericShortwaveSingleScatteringAlbedo =
            atmosphericShortwaveSingleScatteringAlbedo;

        AtmosphericShortwaveDownwardScatteringFraction =
            atmosphericShortwaveDownwardScatteringFraction;

        SurfaceShortwaveAlbedo =
            surfaceShortwaveAlbedo;

        SurfaceLongwaveEmissivity =
            surfaceLongwaveEmissivity;

        AtmosphericLongwaveEmissivity =
            atmosphericLongwaveEmissivity;

        SurfaceEffectiveArealHeatCapacityJoulesPerSquareMeterKelvin =
            surfaceEffectiveArealHeatCapacityJoulesPerSquareMeterKelvin;

        AtmosphericEffectiveArealHeatCapacityJoulesPerSquareMeterKelvin =
            atmosphericEffectiveArealHeatCapacityJoulesPerSquareMeterKelvin;

        MaximumIntegrationStepSeconds =
            maximumIntegrationStepSeconds;
    }

    public double StellarFluxWattsPerSquareMeter { get; }

    public double AtmosphericShortwaveVerticalOpticalDepth { get; }

    public double AtmosphericShortwaveSingleScatteringAlbedo { get; }

    public double AtmosphericShortwaveDownwardScatteringFraction { get; }

    public double SurfaceShortwaveAlbedo { get; }

    public double SurfaceLongwaveEmissivity { get; }

    public double AtmosphericLongwaveEmissivity { get; }

    public double
        SurfaceEffectiveArealHeatCapacityJoulesPerSquareMeterKelvin
    {
        get;
    }

    public double
        AtmosphericEffectiveArealHeatCapacityJoulesPerSquareMeterKelvin
    {
        get;
    }

    public long MaximumIntegrationStepSeconds { get; }

    private static void ValidateNonnegativeFinite(
        double value,
        string parameterName)
    {
        if (!double.IsFinite(value) ||
            value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Regional thermal forcing values must be finite and nonnegative.");
        }
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
                "Regional thermal optical fractions must be finite and in the range [0, 1].");
        }
    }

    private static void ValidatePositiveFinite(
        double value,
        string parameterName)
    {
        if (!double.IsFinite(value) ||
            value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Regional thermal heat capacities must be finite and positive.");
        }
    }
}
