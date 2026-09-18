namespace Est.Simulation.Seasons;

/// <summary>
/// Generic seasonal context that does not assume an Earth calendar or a
/// particular set of named seasons.
/// </summary>
public sealed record SeasonalContext
{
    public SeasonalContext(
        string phaseId,
        double? cycleFraction = null,
        double? subsolarLatitudeDegrees = null)
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

        if (subsolarLatitudeDegrees.HasValue &&
            (!double.IsFinite(
                 subsolarLatitudeDegrees.Value) ||
             subsolarLatitudeDegrees.Value < -90 ||
             subsolarLatitudeDegrees.Value > 90))
        {
            throw new ArgumentOutOfRangeException(
                nameof(subsolarLatitudeDegrees),
                "Subsolar latitude must be finite and in the range [-90, 90] degrees.");
        }

        PhaseId = phaseId;
        CycleFraction = cycleFraction;
        SubsolarLatitudeDegrees =
            subsolarLatitudeDegrees;
    }

    public string PhaseId { get; }

    /// <summary>
    /// Optional normalized position within a repeating seasonal cycle.
    /// Zero is the beginning of the provider-defined cycle and values approach
    /// one at the end. The simulation core assigns no calendar meaning to it.
    /// </summary>
    public double? CycleFraction { get; }

    /// <summary>
    /// Optional latitude of the subsolar point in degrees.
    ///
    /// This is a physical derived signal rather than a calendar label.
    /// Positive values are north of the equator and negative values are south.
    /// </summary>
    public double? SubsolarLatitudeDegrees { get; }
}
