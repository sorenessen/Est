namespace Est.Simulation.Grazers;

/// <summary>
/// Policy for coarse terrestrial-grazer initialization.
///
/// Authoritative live vegetation biomass is the initial ecological support
/// signal. Initialization reads that biomass without consuming it. Grazing,
/// water dependence, movement, mortality, and other causal behavior remain
/// later model concerns.
/// </summary>
public sealed record GrazerModelParameters
{
    public GrazerModelParameters(
        double carryingCapacityGrazersPerKilogramLiveVegetationBiomass =
            0.000001,
        double initialFractionOfLocalCarryingCapacity = 0.25,
        int minimumInitialCohortMemberCount = 10,
        int maximumInitialCohortCount = 64)
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

        CarryingCapacityGrazersPerKilogramLiveVegetationBiomass =
            carryingCapacityGrazersPerKilogramLiveVegetationBiomass;

        InitialFractionOfLocalCarryingCapacity =
            initialFractionOfLocalCarryingCapacity;

        MinimumInitialCohortMemberCount =
            minimumInitialCohortMemberCount;

        MaximumInitialCohortCount =
            maximumInitialCohortCount;
    }

    public double
        CarryingCapacityGrazersPerKilogramLiveVegetationBiomass
    {
        get;
    }

    public double InitialFractionOfLocalCarryingCapacity { get; }

    public int MinimumInitialCohortMemberCount { get; }

    public int MaximumInitialCohortCount { get; }
}
