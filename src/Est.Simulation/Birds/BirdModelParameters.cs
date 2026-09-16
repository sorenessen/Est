using Est.Simulation.Organisms;

namespace Est.Simulation.Birds;

/// <summary>
/// Policy for coarse bird-flock initialization and causal behavior.
///
/// Aggregate invertebrate biomass is an ecological support / prey-availability
/// proxy. It is not assumed to be wholly edible, and the current bird model
/// does not consume that biomass.
///
/// Live vegetation currently provides coarse habitat presence. Surface liquid
/// water provides coarse water availability. These first-pass signals remain
/// species-independent until later ecological detail justifies richer policy.
/// </summary>
public sealed record BirdModelParameters
{
    public BirdModelParameters(
        double carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass =
            0.000001,
        double initialFractionOfLocalCarryingCapacity = 0.25,
        int minimumInitialFlockMemberCount = 10,
        int maximumInitialFlockCount = 64,
        long maximumIntegrationStepSeconds = 21_600,
        double maximumTravelMetersPerDay = 250_000,
        double foodShortageMortalityRatePerDay = 0.05,
        double waterAbsenceMortalityRatePerDay = 0.20,
        double habitatAbsenceMortalityRatePerDay = 0.02,
        double liveBiomassKilogramsPerBird = 1,
        double liveNitrogenKilogramsPerBird = 0.025)
    {
        if (!double.IsFinite(
                carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass) ||
            carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass),
                "Bird carrying-capacity support must be finite and nonnegative.");
        }

        if (!double.IsFinite(
                initialFractionOfLocalCarryingCapacity) ||
            initialFractionOfLocalCarryingCapacity < 0 ||
            initialFractionOfLocalCarryingCapacity > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialFractionOfLocalCarryingCapacity),
                "Initial carrying-capacity fraction must be finite and between zero and one.");
        }

        if (minimumInitialFlockMemberCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumInitialFlockMemberCount),
                "Minimum initial flock member count must be greater than zero.");
        }

        if (maximumInitialFlockCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumInitialFlockCount),
                "Maximum initial flock count must be greater than zero.");
        }

        if (maximumIntegrationStepSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumIntegrationStepSeconds),
                "Maximum integration step must be greater than zero.");
        }

        ValidateNonnegativeFinite(
            maximumTravelMetersPerDay,
            nameof(maximumTravelMetersPerDay));

        ValidateNonnegativeFinite(
            foodShortageMortalityRatePerDay,
            nameof(foodShortageMortalityRatePerDay));

        ValidateNonnegativeFinite(
            waterAbsenceMortalityRatePerDay,
            nameof(waterAbsenceMortalityRatePerDay));

        ValidateNonnegativeFinite(
            habitatAbsenceMortalityRatePerDay,
            nameof(habitatAbsenceMortalityRatePerDay));

        CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass =
            carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass;

        InitialFractionOfLocalCarryingCapacity =
            initialFractionOfLocalCarryingCapacity;

        MinimumInitialFlockMemberCount =
            minimumInitialFlockMemberCount;

        MaximumInitialFlockCount =
            maximumInitialFlockCount;

        MaximumIntegrationStepSeconds =
            maximumIntegrationStepSeconds;

        MaximumTravelMetersPerDay =
            maximumTravelMetersPerDay;

        FoodShortageMortalityRatePerDay =
            foodShortageMortalityRatePerDay;

        WaterAbsenceMortalityRatePerDay =
            waterAbsenceMortalityRatePerDay;

        HabitatAbsenceMortalityRatePerDay =
            habitatAbsenceMortalityRatePerDay;

        MaterialPerBird =
            new OrganismMaterialComposition(
                liveBiomassKilogramsPerBird,
                liveNitrogenKilogramsPerBird);
    }

    public double
        CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass
    {
        get;
    }

    public double InitialFractionOfLocalCarryingCapacity { get; }

    public int MinimumInitialFlockMemberCount { get; }

    public int MaximumInitialFlockCount { get; }

    public long MaximumIntegrationStepSeconds { get; }

    public double MaximumTravelMetersPerDay { get; }

    public double FoodShortageMortalityRatePerDay { get; }

    public double WaterAbsenceMortalityRatePerDay { get; }

    public double HabitatAbsenceMortalityRatePerDay { get; }

    public OrganismMaterialComposition MaterialPerBird { get; }

    private static void ValidateNonnegativeFinite(
        double value,
        string parameterName)
    {
        if (!double.IsFinite(value) ||
            value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Bird behavior parameters must be finite and nonnegative.");
        }
    }
}
