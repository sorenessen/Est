namespace Est.Simulation.Organisms;

/// <summary>
/// Physical composition policy for one represented organism/member.
///
/// This is policy rather than live state. Individual states apply one unit;
/// cohort and flock states apply the policy across their represented members.
/// </summary>
public sealed record OrganismMaterialComposition
{
    public OrganismMaterialComposition(
        double liveBiomassKilogramsPerUnit,
        double liveNitrogenKilogramsPerUnit)
    {
        if (!double.IsFinite(
                liveBiomassKilogramsPerUnit) ||
            liveBiomassKilogramsPerUnit < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(liveBiomassKilogramsPerUnit),
                "Live biomass per unit must be finite and non-negative.");
        }

        if (!double.IsFinite(
                liveNitrogenKilogramsPerUnit) ||
            liveNitrogenKilogramsPerUnit < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(liveNitrogenKilogramsPerUnit),
                "Live nitrogen per unit must be finite and non-negative.");
        }

        if (liveNitrogenKilogramsPerUnit >
            liveBiomassKilogramsPerUnit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(liveNitrogenKilogramsPerUnit),
                "Live nitrogen per unit cannot exceed live biomass per unit.");
        }

        LiveBiomassKilogramsPerUnit =
            liveBiomassKilogramsPerUnit;

        LiveNitrogenKilogramsPerUnit =
            liveNitrogenKilogramsPerUnit;
    }

    public double LiveBiomassKilogramsPerUnit { get; }

    public double LiveNitrogenKilogramsPerUnit { get; }

    public OrganismMaterialState ForUnits(
        int unitCount)
    {
        if (unitCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitCount),
                "Unit count must be non-negative.");
        }

        var liveBiomassKilograms =
            LiveBiomassKilogramsPerUnit *
            unitCount;

        var liveNitrogenKilograms =
            LiveNitrogenKilogramsPerUnit *
            unitCount;

        if (!double.IsFinite(liveBiomassKilograms) ||
            !double.IsFinite(liveNitrogenKilograms))
        {
            throw new InvalidOperationException(
                "Organism material composition overflowed the supported range.");
        }

        return new OrganismMaterialState(
            liveBiomassKilograms,
            liveNitrogenKilograms);
    }
}
