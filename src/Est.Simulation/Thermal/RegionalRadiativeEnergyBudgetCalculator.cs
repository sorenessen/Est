namespace Est.Simulation.Thermal;

/// <summary>
/// Calculates a one-layer gray regional surface-atmosphere-space radiative
/// energy budget without mutating simulation state.
/// </summary>
public static class RegionalRadiativeEnergyBudgetCalculator
{
    public static RegionalRadiativeEnergyBudget Calculate(
        double surfaceAbsorbedShortwaveFluxWattsPerSquareMeter,
        double atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter,
        double surfaceTemperatureKelvin,
        double atmosphericTemperatureKelvin,
        double surfaceEffectiveLongwaveEmissivity,
        double atmosphericEffectiveLongwaveEmissivity)
    {
        ValidateNonnegativeFinite(
            surfaceAbsorbedShortwaveFluxWattsPerSquareMeter,
            nameof(surfaceAbsorbedShortwaveFluxWattsPerSquareMeter));

        ValidateNonnegativeFinite(
            atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter,
            nameof(atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter));

        ValidateTemperature(
            surfaceTemperatureKelvin,
            nameof(surfaceTemperatureKelvin));

        ValidateTemperature(
            atmosphericTemperatureKelvin,
            nameof(atmosphericTemperatureKelvin));

        ValidateEmissivity(
            surfaceEffectiveLongwaveEmissivity,
            nameof(surfaceEffectiveLongwaveEmissivity));

        ValidateEmissivity(
            atmosphericEffectiveLongwaveEmissivity,
            nameof(atmosphericEffectiveLongwaveEmissivity));

        var surfaceEmission =
            GraybodyLongwaveEmissionCalculator
                .CalculateEmittedFluxWattsPerSquareMeter(
                    surfaceTemperatureKelvin,
                    surfaceEffectiveLongwaveEmissivity);

        var atmosphericEmissionPerFace =
            GraybodyLongwaveEmissionCalculator
                .CalculateEmittedFluxWattsPerSquareMeter(
                    atmosphericTemperatureKelvin,
                    atmosphericEffectiveLongwaveEmissivity);

        var surfaceLongwaveAbsorbedByAtmosphere =
            CheckedTransfer(
                atmosphericEffectiveLongwaveEmissivity
                *
                surfaceEmission);

        var surfaceLongwaveTransmittedToSpace =
            CheckedTransfer(
                (1 - atmosphericEffectiveLongwaveEmissivity)
                *
                surfaceEmission);

        var atmosphericDownwardLongwaveAbsorbedBySurface =
            CheckedTransfer(
                surfaceEffectiveLongwaveEmissivity
                *
                atmosphericEmissionPerFace);

        var atmosphericDownwardLongwaveReflectedBySurface =
            CheckedTransfer(
                (1 - surfaceEffectiveLongwaveEmissivity)
                *
                atmosphericEmissionPerFace);

        var reflectedLongwaveReabsorbedByAtmosphere =
            CheckedTransfer(
                atmosphericEffectiveLongwaveEmissivity
                *
                atmosphericDownwardLongwaveReflectedBySurface);

        var reflectedLongwaveTransmittedToSpace =
            CheckedTransfer(
                (1 - atmosphericEffectiveLongwaveEmissivity)
                *
                atmosphericDownwardLongwaveReflectedBySurface);

        var surfaceNetFlux =
            surfaceAbsorbedShortwaveFluxWattsPerSquareMeter
            +
            atmosphericDownwardLongwaveAbsorbedBySurface
            -
            surfaceEmission;

        EnsureFiniteNetFlux(
            surfaceNetFlux,
            "Surface radiative net flux");

        var atmosphericNetFlux =
            atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter
            +
            surfaceLongwaveAbsorbedByAtmosphere
            +
            reflectedLongwaveReabsorbedByAtmosphere
            -
            (2 * atmosphericEmissionPerFace);

        EnsureFiniteNetFlux(
            atmosphericNetFlux,
            "Atmospheric radiative net flux");

        var outgoingLongwaveToSpace =
            surfaceLongwaveTransmittedToSpace
            +
            atmosphericEmissionPerFace
            +
            reflectedLongwaveTransmittedToSpace;

        outgoingLongwaveToSpace =
            CheckedTransfer(
                outgoingLongwaveToSpace);

        var incomingShortwave =
            surfaceAbsorbedShortwaveFluxWattsPerSquareMeter
            +
            atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter;

        EnsureFiniteNetFlux(
            incomingShortwave,
            "Combined absorbed shortwave flux");

        var accountedEnergy =
            surfaceNetFlux
            +
            atmosphericNetFlux
            +
            outgoingLongwaveToSpace;

        EnsureFiniteNetFlux(
            accountedEnergy,
            "Combined radiative accounting flux");

        var conservationError =
            incomingShortwave
            -
            accountedEnergy;

        EnsureFiniteNetFlux(
            conservationError,
            "Combined radiative conservation error");

        return new RegionalRadiativeEnergyBudget(
            surfaceAbsorbedShortwaveFluxWattsPerSquareMeter,
            atmosphericAbsorbedShortwaveFluxWattsPerSquareMeter,
            surfaceEmission,
            atmosphericEmissionPerFace,
            surfaceLongwaveAbsorbedByAtmosphere,
            surfaceLongwaveTransmittedToSpace,
            atmosphericDownwardLongwaveAbsorbedBySurface,
            atmosphericDownwardLongwaveReflectedBySurface,
            reflectedLongwaveReabsorbedByAtmosphere,
            reflectedLongwaveTransmittedToSpace,
            surfaceNetFlux,
            atmosphericNetFlux,
            outgoingLongwaveToSpace,
            conservationError);
    }

    private static void ValidateNonnegativeFinite(
        double value,
        string parameterName)
    {
        if (!double.IsFinite(value) ||
            value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Radiative input flux must be finite and non-negative.");
        }
    }

    private static void ValidateTemperature(
        double value,
        string parameterName)
    {
        if (!double.IsFinite(value) ||
            value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Temperature must be finite and at or above absolute zero.");
        }
    }

    private static void ValidateEmissivity(
        double value,
        string parameterName)
    {
        if (!double.IsFinite(value) ||
            value < 0 ||
            value > 1)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Effective longwave emissivity must be finite and in the range [0, 1].");
        }
    }

    private static double CheckedTransfer(
        double value)
    {
        if (!double.IsFinite(value) ||
            value < 0)
        {
            throw new InvalidOperationException(
                "Regional radiative transfer produced an invalid flux.");
        }

        return value;
    }

    private static void EnsureFiniteNetFlux(
        double value,
        string quantityName)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidOperationException(
                $"{quantityName} is not finite.");
        }
    }
}
