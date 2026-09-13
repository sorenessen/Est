using Est.Simulation.Planets;

namespace Est.Simulation.Population;

public sealed record PersonState
{
    public PersonState(
        PersonId id,
        PlanetId planetId,
        PersonSex sex,
        long birthTimeSeconds,
        double latitudeDegrees,
        double longitudeDegrees,
        PersonId? parentId = null)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Person identity cannot be empty.",
                nameof(id));
        }

        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        if (!double.IsFinite(latitudeDegrees) ||
            latitudeDegrees < -90 ||
            latitudeDegrees > 90)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitudeDegrees),
                "Latitude must be finite and within [-90, 90].");
        }

        if (!double.IsFinite(longitudeDegrees) ||
            longitudeDegrees < -180 ||
            longitudeDegrees > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitudeDegrees),
                "Longitude must be finite and within [-180, 180].");
        }

        Id = id;
        PlanetId = planetId;
        Sex = sex;
        BirthTimeSeconds = birthTimeSeconds;
        LatitudeDegrees = latitudeDegrees;
        LongitudeDegrees = longitudeDegrees;
        ParentId = parentId;
    }

    public PersonId Id { get; private init; }

    public PlanetId PlanetId { get; private init; }

    public PersonSex Sex { get; private init; }

    public long BirthTimeSeconds { get; private init; }

    public double LatitudeDegrees { get; private init; }

    public double LongitudeDegrees { get; private init; }

    public PersonId? ParentId { get; private init; }

    public double AgeYears(long currentTimeSeconds)
    {
        const double secondsPerYear = 31_536_000d;

        return Math.Max(
            0,
            (currentTimeSeconds - BirthTimeSeconds)
            / secondsPerYear);
    }

    public PersonState MoveTo(
        double latitudeDegrees,
        double longitudeDegrees)
    {
        return new PersonState(
            Id,
            PlanetId,
            Sex,
            BirthTimeSeconds,
            latitudeDegrees,
            longitudeDegrees,
            ParentId);
    }
}
