using Est.Simulation.Organisms;
using Est.Simulation.Planets;

namespace Est.Simulation.Birds;

/// <summary>
/// Durable authoritative state for one coarse bird flock.
///
/// This first representation intentionally stores flock identity, membership,
/// and geographic center only. Species, individual birds, energy, health,
/// movement, predation, reproduction, and migration remain later model
/// concerns.
/// </summary>
public sealed record BirdFlockState
{
    public BirdFlockState(
        BirdFlockId id,
        PlanetId planetId,
        int memberCount,
        double latitudeDegrees,
        double longitudeDegrees,
        OrganismMaterialState? material = null)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Bird flock identity cannot be empty.",
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
                "Bird flock member count must be greater than zero.");
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

        Id =
            id;

        PlanetId =
            planetId;

        MemberCount =
            memberCount;

        LatitudeDegrees =
            latitudeDegrees;

        LongitudeDegrees =
            longitudeDegrees;

        Material =
            material ??
            new OrganismMaterialState(
                liveBiomassKilograms: 0,
                liveNitrogenKilograms: 0);
    }

    public BirdFlockId Id { get; }

    public PlanetId PlanetId { get; }

    public int MemberCount { get; }

    public double LatitudeDegrees { get; }

    public double LongitudeDegrees { get; }

    public OrganismMaterialState Material { get; }

    public BirdFlockState WithSurvivalState(
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

        return new BirdFlockState(
            Id,
            PlanetId,
            survivingMemberCount,
            latitudeDegrees,
            longitudeDegrees,
            Material.RetainFraction(
                retainedFraction));
    }
}
