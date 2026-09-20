using Est.Simulation.Planets;
using Est.Simulation.Social;

namespace Est.Simulation.Players;

/// <summary>
/// Authoritative physical presence of one Ester inside a world.
///
/// Ester identity persists independently of manifestation. This state
/// represents where that Ester is physically participating while manifested.
/// </summary>
public sealed record ManifestedEsterState
{
    public ManifestedEsterState(
        EsterId esterId,
        PlanetId planetId,
        double latitudeDegrees,
        double longitudeDegrees)
    {
        if (esterId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Ester identity cannot be empty.",
                nameof(esterId));
        }

        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ValidatePosition(
            latitudeDegrees,
            longitudeDegrees);

        EsterId = esterId;
        PlanetId = planetId;
        LatitudeDegrees = latitudeDegrees;
        LongitudeDegrees = longitudeDegrees;
    }

    public EsterId EsterId { get; }

    public PlanetId PlanetId { get; }

    public double LatitudeDegrees { get; }

    public double LongitudeDegrees { get; }

    public ManifestedEsterState MoveTo(
        double latitudeDegrees,
        double longitudeDegrees)
    {
        ValidatePosition(
            latitudeDegrees,
            longitudeDegrees);

        return new ManifestedEsterState(
            EsterId,
            PlanetId,
            latitudeDegrees,
            longitudeDegrees);
    }

    private static void ValidatePosition(
        double latitudeDegrees,
        double longitudeDegrees)
    {
        if (!double.IsFinite(latitudeDegrees) ||
            latitudeDegrees < -90 ||
            latitudeDegrees > 90)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitudeDegrees),
                "Latitude must be finite and between -90 and 90 degrees.");
        }

        if (!double.IsFinite(longitudeDegrees) ||
            longitudeDegrees < -180 ||
            longitudeDegrees > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitudeDegrees),
                "Longitude must be finite and between -180 and 180 degrees.");
        }
    }
}
