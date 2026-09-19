namespace Est.Simulation.Thermal;

/// <summary>
/// Calculates hemispheric graybody longwave emission using the
/// Stefan-Boltzmann relation.
///
/// This calculator determines emitted flux only. It does not assign a
/// direction, receiving reservoir, or atmospheric greenhouse response.
/// </summary>
public static class GraybodyLongwaveEmissionCalculator
{
    public const double
        StefanBoltzmannConstantWattsPerSquareMeterKelvinFourth =
            5.670374419e-8;

    public static double CalculateEmittedFluxWattsPerSquareMeter(
        double temperatureKelvin,
        double effectiveEmissivity)
    {
        if (!double.IsFinite(temperatureKelvin) ||
            temperatureKelvin < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(temperatureKelvin),
                "Temperature must be finite and at or above absolute zero.");
        }

        if (!double.IsFinite(effectiveEmissivity) ||
            effectiveEmissivity < 0 ||
            effectiveEmissivity > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(effectiveEmissivity),
                "Effective emissivity must be finite and in the range [0, 1].");
        }

        if (temperatureKelvin == 0 ||
            effectiveEmissivity == 0)
        {
            return 0;
        }

        var emittedFlux =
            effectiveEmissivity
            *
            StefanBoltzmannConstantWattsPerSquareMeterKelvinFourth
            *
            Math.Pow(
                temperatureKelvin,
                4);

        if (!double.IsFinite(emittedFlux) ||
            emittedFlux < 0)
        {
            throw new InvalidOperationException(
                "Graybody longwave emission produced an invalid flux.");
        }

        return emittedFlux;
    }
}
