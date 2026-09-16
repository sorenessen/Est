namespace Est.Simulation.Organisms;

/// <summary>
/// Authoritative physical material carried by a living organism or aggregate
/// organism representation.
///
/// The same value object is used regardless of whether the owning simulation
/// state represents one individual, a cohort, or a flock.
/// </summary>
public sealed record OrganismMaterialState
{
    public OrganismMaterialState(
        double liveBiomassKilograms,
        double liveNitrogenKilograms)
    {
        if (!double.IsFinite(liveBiomassKilograms) ||
            liveBiomassKilograms < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(liveBiomassKilograms),
                "Live biomass must be finite and non-negative.");
        }

        if (!double.IsFinite(liveNitrogenKilograms) ||
            liveNitrogenKilograms < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(liveNitrogenKilograms),
                "Live nitrogen must be finite and non-negative.");
        }

        if (liveNitrogenKilograms > liveBiomassKilograms)
        {
            throw new ArgumentOutOfRangeException(
                nameof(liveNitrogenKilograms),
                "Live nitrogen cannot exceed total live biomass.");
        }

        LiveBiomassKilograms =
            liveBiomassKilograms;

        LiveNitrogenKilograms =
            liveNitrogenKilograms;
    }

    public double LiveBiomassKilograms { get; }

    public double LiveNitrogenKilograms { get; }

    public bool IsEmpty =>
        LiveBiomassKilograms == 0 &&
        LiveNitrogenKilograms == 0;

    public OrganismMaterialState Subtract(
        OrganismMaterialState removedMaterial)
    {
        ArgumentNullException.ThrowIfNull(
            removedMaterial);

        if (removedMaterial.LiveBiomassKilograms >
                LiveBiomassKilograms ||
            removedMaterial.LiveNitrogenKilograms >
                LiveNitrogenKilograms)
        {
            throw new InvalidOperationException(
                "Cannot remove more live organism material than is available.");
        }

        return new OrganismMaterialState(
            Math.Max(
                0,
                LiveBiomassKilograms -
                removedMaterial.LiveBiomassKilograms),
            Math.Max(
                0,
                LiveNitrogenKilograms -
                removedMaterial.LiveNitrogenKilograms));
    }

    public OrganismMaterialState RetainFraction(
        double retainedFraction)
    {
        if (!double.IsFinite(retainedFraction) ||
            retainedFraction < 0 ||
            retainedFraction > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retainedFraction),
                "Retained fraction must be finite and within [0, 1].");
        }

        return new OrganismMaterialState(
            LiveBiomassKilograms *
            retainedFraction,
            LiveNitrogenKilograms *
            retainedFraction);
    }
}
