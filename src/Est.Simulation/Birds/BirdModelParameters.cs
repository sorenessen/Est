namespace Est.Simulation.Birds;

/// <summary>
/// Policy for deterministic first-pass bird-flock initialization.
///
/// Authoritative aggregate invertebrate biomass is treated as ecological
/// support / prey availability. Initialization does not consume that biomass.
///
/// Surface cells provide candidate flock centers, but birds remain mobile
/// flock entities rather than per-cell biomass state.
/// </summary>
public sealed record BirdModelParameters
{
    public BirdModelParameters(
        double carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass =
            0.000001,
        double initialFractionOfLocalCarryingCapacity = 0.25,
        int minimumInitialFlockMemberCount = 10,
        int maximumInitialFlockCount = 64)
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

        CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass =
            carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass;

        InitialFractionOfLocalCarryingCapacity =
            initialFractionOfLocalCarryingCapacity;

        MinimumInitialFlockMemberCount =
            minimumInitialFlockMemberCount;

        MaximumInitialFlockCount =
            maximumInitialFlockCount;
    }

    public double
        CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass
    {
        get;
    }

    public double InitialFractionOfLocalCarryingCapacity { get; }

    public int MinimumInitialFlockMemberCount { get; }

    public int MaximumInitialFlockCount { get; }
}
