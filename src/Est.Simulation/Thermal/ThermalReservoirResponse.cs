namespace Est.Simulation.Thermal;

/// <summary>
/// Pure result of applying a net areal heat flux to an effective thermal
/// reservoir for a finite duration.
/// </summary>
public sealed record ThermalReservoirResponse
{
    internal ThermalReservoirResponse(
        double initialTemperatureKelvin,
        double effectiveArealHeatCapacityJoulesPerSquareMeterKelvin,
        double netHeatFluxWattsPerSquareMeter,
        double elapsedSeconds,
        double arealEnergyChangeJoulesPerSquareMeter,
        double temperatureChangeKelvin,
        double finalTemperatureKelvin)
    {
        InitialTemperatureKelvin =
            initialTemperatureKelvin;

        EffectiveArealHeatCapacityJoulesPerSquareMeterKelvin =
            effectiveArealHeatCapacityJoulesPerSquareMeterKelvin;

        NetHeatFluxWattsPerSquareMeter =
            netHeatFluxWattsPerSquareMeter;

        ElapsedSeconds =
            elapsedSeconds;

        ArealEnergyChangeJoulesPerSquareMeter =
            arealEnergyChangeJoulesPerSquareMeter;

        TemperatureChangeKelvin =
            temperatureChangeKelvin;

        FinalTemperatureKelvin =
            finalTemperatureKelvin;
    }

    public double InitialTemperatureKelvin { get; }

    public double
        EffectiveArealHeatCapacityJoulesPerSquareMeterKelvin { get; }

    public double NetHeatFluxWattsPerSquareMeter { get; }

    public double ElapsedSeconds { get; }

    public double ArealEnergyChangeJoulesPerSquareMeter { get; }

    public double TemperatureChangeKelvin { get; }

    public double FinalTemperatureKelvin { get; }
}
