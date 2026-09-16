namespace Est.Simulation.Vegetation;

/// <summary>
/// Policy for first-pass plant biomass productivity.
///
/// Growth is limited by available soil water, temperature suitability, and
/// remaining carrying capacity. Optional plant-tissue nitrogen policy also
/// allows growth to consume and respond to authoritative available nitrogen.
/// Regional climate, species composition, mortality, and decomposition remain
/// later layers.
/// </summary>
public sealed record VegetationModelParameters
{
    public VegetationModelParameters(
        long maximumIntegrationStepSeconds = 21_600,
        double carryingCapacityKilogramsPerSquareMeter = 5,
        double maximumRelativeGrowthRatePerDay = 0.10,
        double soilWaterForFullProductivityKilogramsPerSquareMeter = 50,
        double minimumGrowthTemperatureKelvin = 273.15,
        double optimumGrowthTemperatureKelvin = 293.15,
        double maximumGrowthTemperatureKelvin = 313.15,
        double temperatureLapseRateKelvinPerMeter = 0.0065,
        double? plantNitrogenKilogramsPerKilogramLiveBiomass = null)
    {
        if (maximumIntegrationStepSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumIntegrationStepSeconds),
                "Maximum integration step must be greater than zero.");
        }

        ValidatePositiveFinite(
            carryingCapacityKilogramsPerSquareMeter,
            nameof(carryingCapacityKilogramsPerSquareMeter));

        ValidateNonnegativeFinite(
            maximumRelativeGrowthRatePerDay,
            nameof(maximumRelativeGrowthRatePerDay));

        ValidatePositiveFinite(
            soilWaterForFullProductivityKilogramsPerSquareMeter,
            nameof(soilWaterForFullProductivityKilogramsPerSquareMeter));

        ValidateNonnegativeFinite(
            minimumGrowthTemperatureKelvin,
            nameof(minimumGrowthTemperatureKelvin));

        ValidateNonnegativeFinite(
            optimumGrowthTemperatureKelvin,
            nameof(optimumGrowthTemperatureKelvin));

        ValidateNonnegativeFinite(
            maximumGrowthTemperatureKelvin,
            nameof(maximumGrowthTemperatureKelvin));

        ValidateNonnegativeFinite(
            temperatureLapseRateKelvinPerMeter,
            nameof(temperatureLapseRateKelvinPerMeter));

        if (plantNitrogenKilogramsPerKilogramLiveBiomass is not null)
        {
            ValidatePositiveFinite(
                plantNitrogenKilogramsPerKilogramLiveBiomass.Value,
                nameof(plantNitrogenKilogramsPerKilogramLiveBiomass));
        }

        if (optimumGrowthTemperatureKelvin <=
            minimumGrowthTemperatureKelvin)
        {
            throw new ArgumentException(
                "Optimum growth temperature must exceed minimum growth temperature.",
                nameof(optimumGrowthTemperatureKelvin));
        }

        if (maximumGrowthTemperatureKelvin <=
            optimumGrowthTemperatureKelvin)
        {
            throw new ArgumentException(
                "Maximum growth temperature must exceed optimum growth temperature.",
                nameof(maximumGrowthTemperatureKelvin));
        }

        MaximumIntegrationStepSeconds =
            maximumIntegrationStepSeconds;

        CarryingCapacityKilogramsPerSquareMeter =
            carryingCapacityKilogramsPerSquareMeter;

        MaximumRelativeGrowthRatePerDay =
            maximumRelativeGrowthRatePerDay;

        SoilWaterForFullProductivityKilogramsPerSquareMeter =
            soilWaterForFullProductivityKilogramsPerSquareMeter;

        MinimumGrowthTemperatureKelvin =
            minimumGrowthTemperatureKelvin;

        OptimumGrowthTemperatureKelvin =
            optimumGrowthTemperatureKelvin;

        MaximumGrowthTemperatureKelvin =
            maximumGrowthTemperatureKelvin;

        TemperatureLapseRateKelvinPerMeter =
            temperatureLapseRateKelvinPerMeter;

        PlantNitrogenKilogramsPerKilogramLiveBiomass =
            plantNitrogenKilogramsPerKilogramLiveBiomass;
    }

    public long MaximumIntegrationStepSeconds { get; }

    public double CarryingCapacityKilogramsPerSquareMeter { get; }

    public double MaximumRelativeGrowthRatePerDay { get; }

    public double
        SoilWaterForFullProductivityKilogramsPerSquareMeter
    {
        get;
    }

    public double MinimumGrowthTemperatureKelvin { get; }

    public double OptimumGrowthTemperatureKelvin { get; }

    public double MaximumGrowthTemperatureKelvin { get; }

    public double TemperatureLapseRateKelvinPerMeter { get; }

    public double? PlantNitrogenKilogramsPerKilogramLiveBiomass
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
                "Vegetation model values must be finite and nonnegative.");
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
                "Vegetation model values must be finite and positive.");
        }
    }
}
