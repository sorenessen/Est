using Est.Simulation.Organisms;
using Est.Simulation.Planets;

namespace Est.Simulation.Grazers;

/// <summary>
/// Durable authoritative state for one coarse terrestrial grazer cohort.
///
/// This first representation intentionally stores cohort identity, membership,
/// and geographic center only. Species, energy, health, age structure,
/// movement, grazing, predation, reproduction, and individual materialization
/// remain later model concerns.
/// </summary>
public sealed record GrazerCohortState
{
    public GrazerCohortState(
        GrazerCohortId id,
        PlanetId planetId,
        int memberCount,
        double latitudeDegrees,
        double longitudeDegrees,
        OrganismMaterialState? material = null,
        double recruitmentAccumulator = 0)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Grazer cohort identity cannot be empty.",
                nameof(id));
        }

        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        if (memberCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(memberCount),
                "Grazer cohort member count must be greater than zero.");
        }

        if (!double.IsFinite(latitudeDegrees) ||
            latitudeDegrees < -90 ||
            latitudeDegrees > 90)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitudeDegrees));
        }

        if (!double.IsFinite(longitudeDegrees) ||
            longitudeDegrees < -180 ||
            longitudeDegrees > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitudeDegrees));
        }

        if (!double.IsFinite(recruitmentAccumulator) ||
            recruitmentAccumulator < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recruitmentAccumulator),
                "Grazer recruitment accumulator must be finite and non-negative.");
        }

        Id = id;
        PlanetId = planetId;
        MemberCount = memberCount;
        LatitudeDegrees = latitudeDegrees;
        LongitudeDegrees = longitudeDegrees;
        Material =
            material ??
            new OrganismMaterialState(
                liveBiomassKilograms: 0,
                liveNitrogenKilograms: 0);

        RecruitmentAccumulator =
            recruitmentAccumulator;
    }

    public GrazerCohortId Id { get; }

    public PlanetId PlanetId { get; }

    public int MemberCount { get; }

    public double LatitudeDegrees { get; }

    public double LongitudeDegrees { get; }

    public OrganismMaterialState Material { get; }

    public double RecruitmentAccumulator { get; }

    public GrazerCohortState WithSurvivalState(
        int survivingMemberCount,
        double latitudeDegrees,
        double longitudeDegrees)
    {
        if (survivingMemberCount > MemberCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(survivingMemberCount),
                "Surviving membership cannot exceed current membership.");
        }

        var retainedFraction =
            survivingMemberCount /
            (double)MemberCount;

        return new GrazerCohortState(
            Id,
            PlanetId,
            survivingMemberCount,
            latitudeDegrees,
            longitudeDegrees,
            Material.RetainFraction(
                retainedFraction),
            RecruitmentAccumulator *
            retainedFraction);
    }
}
