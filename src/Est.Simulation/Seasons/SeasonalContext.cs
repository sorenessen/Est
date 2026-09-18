namespace Est.Simulation.Seasons;

/// <summary>
/// Generic seasonal context that does not assume an Earth calendar or a
/// particular set of named seasons.
/// </summary>
public sealed record SeasonalContext
{
    public SeasonalContext(
        string phaseId,
        double? cycleFraction = null)
    {
        if (string.IsNullOrWhiteSpace(phaseId))
        {
            throw new ArgumentException(
                "Seasonal phase identity cannot be empty.",
                nameof(phaseId));
        }

        if (cycleFraction.HasValue &&
            (!double.IsFinite(cycleFraction.Value) ||
             cycleFraction.Value < 0 ||
             cycleFraction.Value >= 1))
        {
            throw new ArgumentOutOfRangeException(
                nameof(cycleFraction),
                "Seasonal cycle fraction must be finite and in the range [0, 1).");
        }

        PhaseId = phaseId;
        CycleFraction = cycleFraction;
    }

    public string PhaseId { get; }

    /// <summary>
    /// Optional normalized position within a repeating seasonal cycle.
    /// Zero is the beginning of the provider-defined cycle and values approach
    /// one at the end. The simulation core assigns no calendar meaning to it.
    /// </summary>
    public double? CycleFraction { get; }
}
