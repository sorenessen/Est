namespace Est.Simulation.Biogeochemistry;

/// <summary>
/// Policy for first-pass decomposition and nitrogen cycling.
///
/// Decomposition will be constrained by moisture and temperature, consume
/// detrital biomass, and transfer the corresponding detrital nitrogen into the
/// plant-available pool. Detailed microbial immobilization, carbon cycling,
/// phosphorus, and soil chemistry remain later extensions.
/// </summary>
public sealed record BiogeochemistryModelParameters
{
    public BiogeochemistryModelParameters(
        long maximumIntegrationStepSeconds = 21_600,
        double maximumRelativeDecompositionRatePerDay = 0.05,
        double soilWaterForFullDecompositionKilogramsPerSquareMeter = 50,
        double minimumDecompositionTemperatureKelvin = 263.15,
        double optimumDecompositionTemperatureKelvin = 293.15,
        double maximumDecompositionTemperatureKelvin = 313.15,
        double temperatureLapseRateKelvinPerMeter = 0.0065)
    {
        if (maximumIntegrationStepSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumIntegrationStepSeconds),
                "Maximum integration step must be greater than zero.");
        }

        ValidateNonnegativeFinite(
            maximumRelativeDecompositionRatePerDay,
            nameof(maximumRelativeDecompositionRatePerDay));

        ValidatePositiveFinite(
            soilWaterForFullDecompositionKilogramsPerSquareMeter,
            nameof(soilWaterForFullDecompositionKilogramsPerSquareMeter));

        ValidateNonnegativeFinite(
            minimumDecompositionTemperatureKelvin,
            nameof(minimumDecompositionTemperatureKelvin));

        ValidateNonnegativeFinite(
            optimumDecompositionTemperatureKelvin,
            nameof(optimumDecompositionTemperatureKelvin));

        ValidateNonnegativeFinite(
            maximumDecompositionTemperatureKelvin,
            nameof(maximumDecompositionTemperatureKelvin));

        ValidateNonnegativeFinite(
            temperatureLapseRateKelvinPerMeter,
            nameof(temperatureLapseRateKelvinPerMeter));

        if (optimumDecompositionTemperatureKelvin <=
            minimumDecompositionTemperatureKelvin)
        {
            throw new ArgumentException(
                "Optimum decomposition temperature must exceed minimum decomposition temperature.",
                nameof(optimumDecompositionTemperatureKelvin));
        }

        if (maximumDecompositionTemperatureKelvin <=
            optimumDecompositionTemperatureKelvin)
        {
            throw new ArgumentException(
                "Maximum decomposition temperature must exceed optimum decomposition temperature.",
                nameof(maximumDecompositionTemperatureKelvin));
        }

        MaximumIntegrationStepSeconds =
            maximumIntegrationStepSeconds;

        MaximumRelativeDecompositionRatePerDay =
            maximumRelativeDecompositionRatePerDay;

        SoilWaterForFullDecompositionKilogramsPerSquareMeter =
            soilWaterForFullDecompositionKilogramsPerSquareMeter;

        MinimumDecompositionTemperatureKelvin =
            minimumDecompositionTemperatureKelvin;

        OptimumDecompositionTemperatureKelvin =
            optimumDecompositionTemperatureKelvin;

        MaximumDecompositionTemperatureKelvin =
            maximumDecompositionTemperatureKelvin;

        TemperatureLapseRateKelvinPerMeter =
            temperatureLapseRateKelvinPerMeter;
    }

    public long MaximumIntegrationStepSeconds { get; }

    public double MaximumRelativeDecompositionRatePerDay { get; }

    public double
        SoilWaterForFullDecompositionKilogramsPerSquareMeter
    {
        get;
    }

    public double MinimumDecompositionTemperatureKelvin { get; }

    public double OptimumDecompositionTemperatureKelvin { get; }

    public double MaximumDecompositionTemperatureKelvin { get; }

    public double TemperatureLapseRateKelvinPerMeter { get; }

    private static void ValidateNonnegativeFinite(
        double value,
        string parameterName)
    {
        if (!double.IsFinite(value) ||
            value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Biogeochemistry model values must be finite and nonnegative.");
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
                "Biogeochemistry model values must be finite and positive.");
        }
    }
}
