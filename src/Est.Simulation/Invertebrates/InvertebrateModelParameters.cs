namespace Est.Simulation.Invertebrates;

/// <summary>
/// Policy for first-pass aggregate invertebrate biomass dynamics.
///
/// Authoritative live vegetation defines local ecological support capacity.
/// The aggregate does not imply herbivory or direct plant consumption.
/// Species structure, trophic guilds, dispersal, predation, decomposition,
/// and nutrient cycling remain later ecological layers.
/// </summary>
public sealed record InvertebrateModelParameters
{
    public InvertebrateModelParameters(
        long maximumIntegrationStepSeconds = 21_600,
        double carryingCapacityKilogramsPerKilogramLiveVegetation = 0.02,
        double initialFractionOfLocalCarryingCapacity = 0.25,
        double maximumRelativeGrowthRatePerDay = 0.10,
        double baselineMortalityRatePerDay = 0.02,
        double liveNitrogenKilogramsPerKilogramLiveBiomass = 0)
    {
        if (maximumIntegrationStepSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumIntegrationStepSeconds),
                "Maximum integration step must be greater than zero.");
        }

        ValidateNonnegativeFinite(
            carryingCapacityKilogramsPerKilogramLiveVegetation,
            nameof(carryingCapacityKilogramsPerKilogramLiveVegetation));

        if (!double.IsFinite(
                initialFractionOfLocalCarryingCapacity) ||
            initialFractionOfLocalCarryingCapacity < 0 ||
            initialFractionOfLocalCarryingCapacity > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialFractionOfLocalCarryingCapacity),
                "Initial carrying-capacity fraction must be finite and between zero and one.");
        }

        ValidateNonnegativeFinite(
            maximumRelativeGrowthRatePerDay,
            nameof(maximumRelativeGrowthRatePerDay));

        ValidateNonnegativeFinite(
            baselineMortalityRatePerDay,
            nameof(baselineMortalityRatePerDay));

        ValidateNonnegativeFinite(
            liveNitrogenKilogramsPerKilogramLiveBiomass,
            nameof(
                liveNitrogenKilogramsPerKilogramLiveBiomass));

        if (liveNitrogenKilogramsPerKilogramLiveBiomass > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    liveNitrogenKilogramsPerKilogramLiveBiomass),
                "Live invertebrate nitrogen cannot exceed live biomass.");
        }

        MaximumIntegrationStepSeconds =
            maximumIntegrationStepSeconds;

        CarryingCapacityKilogramsPerKilogramLiveVegetation =
            carryingCapacityKilogramsPerKilogramLiveVegetation;

        InitialFractionOfLocalCarryingCapacity =
            initialFractionOfLocalCarryingCapacity;

        MaximumRelativeGrowthRatePerDay =
            maximumRelativeGrowthRatePerDay;

        BaselineMortalityRatePerDay =
            baselineMortalityRatePerDay;

        LiveNitrogenKilogramsPerKilogramLiveBiomass =
            liveNitrogenKilogramsPerKilogramLiveBiomass;
    }

    public long MaximumIntegrationStepSeconds { get; }

    public double
        CarryingCapacityKilogramsPerKilogramLiveVegetation
    {
        get;
    }

    public double InitialFractionOfLocalCarryingCapacity
    {
        get;
    }

    public double MaximumRelativeGrowthRatePerDay { get; }

    public double BaselineMortalityRatePerDay { get; }

    public double
        LiveNitrogenKilogramsPerKilogramLiveBiomass
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
                "Invertebrate model values must be finite and nonnegative.");
        }
    }
}
