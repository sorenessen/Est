namespace Est.Simulation.Thermal;

/// <summary>
/// Pure regional surface-atmosphere-space radiative-energy accounting for one
/// authoritative surface column.
///
/// All transfer magnitudes are expressed in W/m². Surface and atmospheric net
/// fluxes are signed: positive values add energy to the corresponding thermal
/// reservoir.
/// </summary>
public sealed record RegionalRadiativeEnergyBudget
{
    internal RegionalRadiativeEnergyBudget(
        double surfaceAbsorbedShortwaveFluxWattsPerSquareMeter,
        double atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter,
        double surfaceLongwaveEmissionWattsPerSquareMeter,
        double atmosphericLongwaveEmissionPerFaceWattsPerSquareMeter,
        double surfaceLongwaveAbsorbedByAtmosphereWattsPerSquareMeter,
        double surfaceLongwaveTransmittedToSpaceWattsPerSquareMeter,
        double atmosphericDownwardLongwaveAbsorbedBySurfaceWattsPerSquareMeter,
        double atmosphericDownwardLongwaveReflectedBySurfaceWattsPerSquareMeter,
        double reflectedLongwaveReabsorbedByAtmosphereWattsPerSquareMeter,
        double reflectedLongwaveTransmittedToSpaceWattsPerSquareMeter,
        double surfaceNetRadiativeFluxWattsPerSquareMeter,
        double atmosphericNetRadiativeFluxWattsPerSquareMeter,
        double outgoingLongwaveFluxToSpaceWattsPerSquareMeter,
        double combinedRadiativeConservationErrorWattsPerSquareMeter)
    {
        SurfaceAbsorbedShortwaveFluxWattsPerSquareMeter =
            surfaceAbsorbedShortwaveFluxWattsPerSquareMeter;

        AtmosphericAbsorbedShortwaveFluxWattsPerSquareMeter =
            atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter;

        SurfaceLongwaveEmissionWattsPerSquareMeter =
            surfaceLongwaveEmissionWattsPerSquareMeter;

        AtmosphericLongwaveEmissionPerFaceWattsPerSquareMeter =
            atmosphericLongwaveEmissionPerFaceWattsPerSquareMeter;

        SurfaceLongwaveAbsorbedByAtmosphereWattsPerSquareMeter =
            surfaceLongwaveAbsorbedByAtmosphereWattsPerSquareMeter;

        SurfaceLongwaveTransmittedToSpaceWattsPerSquareMeter =
            surfaceLongwaveTransmittedToSpaceWattsPerSquareMeter;

        AtmosphericDownwardLongwaveAbsorbedBySurfaceWattsPerSquareMeter =
            atmosphericDownwardLongwaveAbsorbedBySurfaceWattsPerSquareMeter;

        AtmosphericDownwardLongwaveReflectedBySurfaceWattsPerSquareMeter =
            atmosphericDownwardLongwaveReflectedBySurfaceWattsPerSquareMeter;

        ReflectedLongwaveReabsorbedByAtmosphereWattsPerSquareMeter =
            reflectedLongwaveReabsorbedByAtmosphereWattsPerSquareMeter;

        ReflectedLongwaveTransmittedToSpaceWattsPerSquareMeter =
            reflectedLongwaveTransmittedToSpaceWattsPerSquareMeter;

        SurfaceNetRadiativeFluxWattsPerSquareMeter =
            surfaceNetRadiativeFluxWattsPerSquareMeter;

        AtmosphericNetRadiativeFluxWattsPerSquareMeter =
            atmosphericNetRadiativeFluxWattsPerSquareMeter;

        OutgoingLongwaveFluxToSpaceWattsPerSquareMeter =
            outgoingLongwaveFluxToSpaceWattsPerSquareMeter;

        CombinedRadiativeConservationErrorWattsPerSquareMeter =
            combinedRadiativeConservationErrorWattsPerSquareMeter;
    }

    public double SurfaceAbsorbedShortwaveFluxWattsPerSquareMeter { get; }

    public double AtmosphericAbsorbedShortwaveFluxWattsPerSquareMeter { get; }

    public double SurfaceLongwaveEmissionWattsPerSquareMeter { get; }

    public double AtmosphericLongwaveEmissionPerFaceWattsPerSquareMeter { get; }

    public double SurfaceLongwaveAbsorbedByAtmosphereWattsPerSquareMeter { get; }

    public double SurfaceLongwaveTransmittedToSpaceWattsPerSquareMeter { get; }

    public double
        AtmosphericDownwardLongwaveAbsorbedBySurfaceWattsPerSquareMeter
    {
        get;
    }

    public double
        AtmosphericDownwardLongwaveReflectedBySurfaceWattsPerSquareMeter
    {
        get;
    }

    public double
        ReflectedLongwaveReabsorbedByAtmosphereWattsPerSquareMeter
    {
        get;
    }

    public double ReflectedLongwaveTransmittedToSpaceWattsPerSquareMeter
    {
        get;
    }

    public double SurfaceNetRadiativeFluxWattsPerSquareMeter { get; }

    public double AtmosphericNetRadiativeFluxWattsPerSquareMeter { get; }

    public double OutgoingLongwaveFluxToSpaceWattsPerSquareMeter { get; }

    public double CombinedRadiativeConservationErrorWattsPerSquareMeter
    {
        get;
    }
}
