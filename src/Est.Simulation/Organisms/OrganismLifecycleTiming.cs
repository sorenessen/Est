namespace Est.Simulation.Organisms;

/// <summary>
/// Species-policy lifecycle timing expressed in authoritative simulation
/// seconds.
///
/// This defines shared lifecycle semantics without prescribing mating,
/// pregnancy, nesting, recruitment, migration, or other species behavior.
/// </summary>
public sealed record OrganismLifecycleTiming
{
    public OrganismLifecycleTiming(
        long maturityAgeSeconds,
        long reproductiveAgeMinimumSeconds,
        long? reproductiveAgeMaximumSeconds = null,
        long? senescenceAgeSeconds = null)
    {
        ValidateNonnegative(
            maturityAgeSeconds,
            nameof(maturityAgeSeconds));

        ValidateNonnegative(
            reproductiveAgeMinimumSeconds,
            nameof(reproductiveAgeMinimumSeconds));

        if (reproductiveAgeMinimumSeconds <
            maturityAgeSeconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reproductiveAgeMinimumSeconds),
                "Reproductive eligibility cannot begin before lifecycle maturity.");
        }

        if (reproductiveAgeMaximumSeconds is
            long maximumReproductiveAge)
        {
            ValidateNonnegative(
                maximumReproductiveAge,
                nameof(reproductiveAgeMaximumSeconds));

            if (maximumReproductiveAge <
                reproductiveAgeMinimumSeconds)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reproductiveAgeMaximumSeconds),
                    "Maximum reproductive age cannot precede minimum reproductive age.");
            }
        }

        if (senescenceAgeSeconds is
            long senescenceAge)
        {
            ValidateNonnegative(
                senescenceAge,
                nameof(senescenceAgeSeconds));

            if (senescenceAge <
                maturityAgeSeconds)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(senescenceAgeSeconds),
                    "Senescence cannot begin before lifecycle maturity.");
            }
        }

        MaturityAgeSeconds =
            maturityAgeSeconds;

        ReproductiveAgeMinimumSeconds =
            reproductiveAgeMinimumSeconds;

        ReproductiveAgeMaximumSeconds =
            reproductiveAgeMaximumSeconds;

        SenescenceAgeSeconds =
            senescenceAgeSeconds;
    }

    public long MaturityAgeSeconds { get; }

    public long ReproductiveAgeMinimumSeconds { get; }

    public long? ReproductiveAgeMaximumSeconds { get; }

    public long? SenescenceAgeSeconds { get; }

    public OrganismLifecycleStage StageAtAgeSeconds(
        long ageSeconds)
    {
        ValidateNonnegative(
            ageSeconds,
            nameof(ageSeconds));

        if (ageSeconds <
            MaturityAgeSeconds)
        {
            return OrganismLifecycleStage.Immature;
        }

        if (SenescenceAgeSeconds is
                long senescenceAge &&
            ageSeconds >= senescenceAge)
        {
            return OrganismLifecycleStage.Senescent;
        }

        return OrganismLifecycleStage.Mature;
    }

    public bool IsReproductivelyEligibleAtAgeSeconds(
        long ageSeconds)
    {
        ValidateNonnegative(
            ageSeconds,
            nameof(ageSeconds));

        if (ageSeconds <
            ReproductiveAgeMinimumSeconds)
        {
            return false;
        }

        return ReproductiveAgeMaximumSeconds is
                   not long maximumAge ||
               ageSeconds <= maximumAge;
    }

    private static void ValidateNonnegative(
        long value,
        string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Lifecycle time values cannot be negative.");
        }
    }
}
