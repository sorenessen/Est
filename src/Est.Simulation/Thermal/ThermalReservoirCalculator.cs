namespace Est.Simulation.Thermal;

/// <summary>
/// Applies a finite net heat flux to an effective areal thermal reservoir.
///
/// The calculation is pure and does not own or mutate authoritative simulation
/// temperature state.
/// </summary>
public static class ThermalReservoirCalculator
{
    public static ThermalReservoirResponse Calculate(
        double initialTemperatureKelvin,
        double effectiveArealHeatCapacityJoulesPerSquareMeterKelvin,
        double netHeatFluxWattsPerSquareMeter,
        double elapsedSeconds)
    {
        if (!double.IsFinite(initialTemperatureKelvin) ||
            initialTemperatureKelvin < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialTemperatureKelvin),
                "Initial temperature must be finite and at or above absolute zero.");
        }

        if (!double.IsFinite(
                effectiveArealHeatCapacityJoulesPerSquareMeterKelvin) ||
            effectiveArealHeatCapacityJoulesPerSquareMeterKelvin <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    effectiveArealHeatCapacityJoulesPerSquareMeterKelvin),
                "Effective areal heat capacity must be finite and positive.");
        }

        if (!double.IsFinite(netHeatFluxWattsPerSquareMeter))
        {
            throw new ArgumentOutOfRangeException(
                nameof(netHeatFluxWattsPerSquareMeter),
                "Net heat flux must be finite.");
        }

        if (!double.IsFinite(elapsedSeconds) ||
            elapsedSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elapsedSeconds),
                "Elapsed time must be finite and non-negative.");
        }

        var arealEnergyChange =
            netHeatFluxWattsPerSquareMeter
            *
            elapsedSeconds;

        if (!double.IsFinite(arealEnergyChange))
        {
            throw new InvalidOperationException(
                "Thermal-reservoir integration produced a non-finite areal energy change.");
        }

        var temperatureChange =
            arealEnergyChange
            /
            effectiveArealHeatCapacityJoulesPerSquareMeterKelvin;

        if (!double.IsFinite(temperatureChange))
        {
            throw new InvalidOperationException(
                "Thermal-reservoir integration produced a non-finite temperature change.");
        }

        var finalTemperature =
            initialTemperatureKelvin
            +
            temperatureChange;

        if (!double.IsFinite(finalTemperature) ||
            finalTemperature < 0)
        {
            throw new InvalidOperationException(
                "Thermal-reservoir integration produced an invalid temperature. Use a smaller step or different thermal parameters.");
        }

        return new ThermalReservoirResponse(
            initialTemperatureKelvin,
            effectiveArealHeatCapacityJoulesPerSquareMeterKelvin,
            netHeatFluxWattsPerSquareMeter,
            elapsedSeconds,
            arealEnergyChange,
            temperatureChange,
            finalTemperature);
    }
}
