namespace Est.Simulation.Hydrology;

/// <summary>
/// Policy for the first conservative surface-water cycle.
///
/// Water-transfer rates are expressed as kilograms of water per square meter
/// per simulated day. The causal system may integrate them in smaller bounded
/// steps but must never transfer more water than the source store contains.
/// </summary>
public sealed record HydrologyModelParameters
{
    public HydrologyModelParameters(
        long maximumIntegrationStepSeconds = 21_600,
        double maximumEvaporationRateKilogramsPerSquareMeterPerDay = 4,
        double atmosphericPrecipitationThresholdKilogramsPerSquareMeter = 20,
        double maximumPrecipitationRateKilogramsPerSquareMeterPerDay = 12,
        double soilWaterCapacityKilogramsPerSquareMeter = 150,
        double maximumInfiltrationRateKilogramsPerSquareMeterPerDay = 20,
        double maximumRunoffRateKilogramsPerSquareMeterPerDay = 25,
        double freezingTemperatureKelvin = 273.15,
        double meltingTemperatureKelvin = 273.15,
        double maximumFreezingRateKilogramsPerSquareMeterPerDay = 20,
        double maximumMeltingRateKilogramsPerSquareMeterPerDay = 20)
    {
        if (maximumIntegrationStepSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumIntegrationStepSeconds),
                "Maximum integration step must be greater than zero.");
        }

        ValidateNonnegativeFinite(
            maximumEvaporationRateKilogramsPerSquareMeterPerDay,
            nameof(maximumEvaporationRateKilogramsPerSquareMeterPerDay));

        ValidateNonnegativeFinite(
            atmosphericPrecipitationThresholdKilogramsPerSquareMeter,
            nameof(atmosphericPrecipitationThresholdKilogramsPerSquareMeter));

        ValidateNonnegativeFinite(
            maximumPrecipitationRateKilogramsPerSquareMeterPerDay,
            nameof(maximumPrecipitationRateKilogramsPerSquareMeterPerDay));

        ValidateNonnegativeFinite(
            soilWaterCapacityKilogramsPerSquareMeter,
            nameof(soilWaterCapacityKilogramsPerSquareMeter));

        ValidateNonnegativeFinite(
            maximumInfiltrationRateKilogramsPerSquareMeterPerDay,
            nameof(maximumInfiltrationRateKilogramsPerSquareMeterPerDay));

        ValidateNonnegativeFinite(
            maximumRunoffRateKilogramsPerSquareMeterPerDay,
            nameof(maximumRunoffRateKilogramsPerSquareMeterPerDay));

        ValidateNonnegativeFinite(
            freezingTemperatureKelvin,
            nameof(freezingTemperatureKelvin));

        ValidateNonnegativeFinite(
            meltingTemperatureKelvin,
            nameof(meltingTemperatureKelvin));

        ValidateNonnegativeFinite(
            maximumFreezingRateKilogramsPerSquareMeterPerDay,
            nameof(maximumFreezingRateKilogramsPerSquareMeterPerDay));

        ValidateNonnegativeFinite(
            maximumMeltingRateKilogramsPerSquareMeterPerDay,
            nameof(maximumMeltingRateKilogramsPerSquareMeterPerDay));

        if (meltingTemperatureKelvin <
            freezingTemperatureKelvin)
        {
            throw new ArgumentException(
                "Melting temperature cannot be lower than freezing temperature.",
                nameof(meltingTemperatureKelvin));
        }

        MaximumIntegrationStepSeconds =
            maximumIntegrationStepSeconds;

        MaximumEvaporationRateKilogramsPerSquareMeterPerDay =
            maximumEvaporationRateKilogramsPerSquareMeterPerDay;

        AtmosphericPrecipitationThresholdKilogramsPerSquareMeter =
            atmosphericPrecipitationThresholdKilogramsPerSquareMeter;

        MaximumPrecipitationRateKilogramsPerSquareMeterPerDay =
            maximumPrecipitationRateKilogramsPerSquareMeterPerDay;

        SoilWaterCapacityKilogramsPerSquareMeter =
            soilWaterCapacityKilogramsPerSquareMeter;

        MaximumInfiltrationRateKilogramsPerSquareMeterPerDay =
            maximumInfiltrationRateKilogramsPerSquareMeterPerDay;

        MaximumRunoffRateKilogramsPerSquareMeterPerDay =
            maximumRunoffRateKilogramsPerSquareMeterPerDay;

        FreezingTemperatureKelvin =
            freezingTemperatureKelvin;

        MeltingTemperatureKelvin =
            meltingTemperatureKelvin;

        MaximumFreezingRateKilogramsPerSquareMeterPerDay =
            maximumFreezingRateKilogramsPerSquareMeterPerDay;

        MaximumMeltingRateKilogramsPerSquareMeterPerDay =
            maximumMeltingRateKilogramsPerSquareMeterPerDay;
    }

    public long MaximumIntegrationStepSeconds { get; }

    public double
        MaximumEvaporationRateKilogramsPerSquareMeterPerDay
    {
        get;
    }

    public double
        AtmosphericPrecipitationThresholdKilogramsPerSquareMeter
    {
        get;
    }

    public double
        MaximumPrecipitationRateKilogramsPerSquareMeterPerDay
    {
        get;
    }

    public double SoilWaterCapacityKilogramsPerSquareMeter { get; }

    public double
        MaximumInfiltrationRateKilogramsPerSquareMeterPerDay
    {
        get;
    }

    public double
        MaximumRunoffRateKilogramsPerSquareMeterPerDay
    {
        get;
    }

    public double FreezingTemperatureKelvin { get; }

    public double MeltingTemperatureKelvin { get; }

    public double
        MaximumFreezingRateKilogramsPerSquareMeterPerDay
    {
        get;
    }

    public double
        MaximumMeltingRateKilogramsPerSquareMeterPerDay
    {
        get;
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
                "Hydrology model values must be finite and nonnegative.");
        }
    }
}
