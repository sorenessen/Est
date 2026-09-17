using Est.Simulation.Organisms;

namespace Est.Simulation.Grazers;

/// <summary>
/// Policy for coarse terrestrial-grazer initialization and causal behavior.
///
/// Authoritative live vegetation biomass establishes initial carrying support
/// and is directly consumed by the first-pass grazing model. This remains a
/// coarse functional-grazer model rather than a mature diet, edible-fraction,
/// or species-specific nutrition model.
///
/// Vegetated cells provide coarse terrestrial habitat. Surface liquid water
/// can be used as an explicit coarse movement signal and as an explicit
/// mortality signal. The current generated hydrology field does not represent
/// terrestrial drinking-water access well enough to enable either behavior by
/// default.
/// </summary>
public sealed record GrazerModelParameters
{
    public GrazerModelParameters(
        double carryingCapacityGrazersPerKilogramLiveVegetationBiomass =
            0.000001,
        double initialFractionOfLocalCarryingCapacity = 0.25,
        int minimumInitialCohortMemberCount = 10,
        int maximumInitialCohortCount = 64,
        long maximumIntegrationStepSeconds = 21_600,
        double maximumTravelMetersPerDay = 50_000,
        double maximumGrazeKilogramsPerGrazerPerDay = 10,
        double foodShortageMortalityRatePerDay = 0.05,
        double waterAbsenceMortalityRatePerDay = 0,
        double habitatAbsenceMortalityRatePerDay = 0.02,
        double liveBiomassKilogramsPerGrazer = 250,
        double liveNitrogenKilogramsPerGrazer = 6.25,
        bool useSurfaceWaterForMovement = false,
        double maximumRecruitmentRatePerDay = 0)
    {
        if (!double.IsFinite(
                carryingCapacityGrazersPerKilogramLiveVegetationBiomass) ||
            carryingCapacityGrazersPerKilogramLiveVegetationBiomass < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    carryingCapacityGrazersPerKilogramLiveVegetationBiomass),
                "Grazer carrying-capacity support must be finite and nonnegative.");
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

        if (minimumInitialCohortMemberCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumInitialCohortMemberCount),
                "Minimum initial cohort member count must be greater than zero.");
        }

        if (maximumInitialCohortCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumInitialCohortCount),
                "Maximum initial cohort count must be greater than zero.");
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
            maximumGrazeKilogramsPerGrazerPerDay,
            nameof(maximumGrazeKilogramsPerGrazerPerDay));

        ValidateNonnegativeFinite(
            foodShortageMortalityRatePerDay,
            nameof(foodShortageMortalityRatePerDay));

        ValidateNonnegativeFinite(
            waterAbsenceMortalityRatePerDay,
            nameof(waterAbsenceMortalityRatePerDay));

        ValidateNonnegativeFinite(
            habitatAbsenceMortalityRatePerDay,
            nameof(habitatAbsenceMortalityRatePerDay));

        ValidateNonnegativeFinite(
            maximumRecruitmentRatePerDay,
            nameof(maximumRecruitmentRatePerDay));

        CarryingCapacityGrazersPerKilogramLiveVegetationBiomass =
            carryingCapacityGrazersPerKilogramLiveVegetationBiomass;

        InitialFractionOfLocalCarryingCapacity =
            initialFractionOfLocalCarryingCapacity;

        MinimumInitialCohortMemberCount =
            minimumInitialCohortMemberCount;

        MaximumInitialCohortCount =
            maximumInitialCohortCount;

        MaximumIntegrationStepSeconds =
            maximumIntegrationStepSeconds;

        MaximumTravelMetersPerDay =
            maximumTravelMetersPerDay;

        MaximumGrazeKilogramsPerGrazerPerDay =
            maximumGrazeKilogramsPerGrazerPerDay;

        FoodShortageMortalityRatePerDay =
            foodShortageMortalityRatePerDay;

        WaterAbsenceMortalityRatePerDay =
            waterAbsenceMortalityRatePerDay;

        UseSurfaceWaterForMovement =
            useSurfaceWaterForMovement;

        HabitatAbsenceMortalityRatePerDay =
            habitatAbsenceMortalityRatePerDay;

        MaximumRecruitmentRatePerDay =
            maximumRecruitmentRatePerDay;

        MaterialPerGrazer =
            new OrganismMaterialComposition(
                liveBiomassKilogramsPerGrazer,
                liveNitrogenKilogramsPerGrazer);
    }

    public double
        CarryingCapacityGrazersPerKilogramLiveVegetationBiomass
    {
        get;
    }

    public double InitialFractionOfLocalCarryingCapacity { get; }

    public int MinimumInitialCohortMemberCount { get; }

    public int MaximumInitialCohortCount { get; }

    public long MaximumIntegrationStepSeconds { get; }

    public double MaximumTravelMetersPerDay { get; }

    public double MaximumGrazeKilogramsPerGrazerPerDay { get; }

    public double FoodShortageMortalityRatePerDay { get; }

    public double WaterAbsenceMortalityRatePerDay { get; }

    public bool UseSurfaceWaterForMovement { get; }

    public double HabitatAbsenceMortalityRatePerDay { get; }

    public double MaximumRecruitmentRatePerDay { get; }

    public OrganismMaterialComposition MaterialPerGrazer { get; }

    private static void ValidateNonnegativeFinite(
        double value,
        string parameterName)
    {
        if (!double.IsFinite(value) ||
            value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Grazer behavior parameters must be finite and nonnegative.");
        }
    }
}
