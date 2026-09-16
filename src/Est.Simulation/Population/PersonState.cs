using Est.Simulation.Organisms;
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
        PersonId? parentId = null,
        PersonNeedsState? needs = null,
        PersonActivity activity = PersonActivity.Idle,
        PregnancyState? pregnancy = null,
        OrganismMaterialState? material = null)
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

        if (pregnancy is not null &&
            sex != PersonSex.Female)
        {
            throw new ArgumentException(
                "Only a female person can carry a pregnancy.",
                nameof(pregnancy));
        }

        Id = id;
        PlanetId = planetId;
        Sex = sex;
        BirthTimeSeconds = birthTimeSeconds;
        LatitudeDegrees = latitudeDegrees;
        LongitudeDegrees = longitudeDegrees;
        ParentId = parentId;
        Needs = needs ?? new PersonNeedsState();
        Activity = activity;
        Pregnancy = pregnancy;
        Material =
            material ??
            new OrganismMaterialState(
                liveBiomassKilograms: 0,
                liveNitrogenKilograms: 0);
    }

    public PersonId Id { get; private init; }

    public PlanetId PlanetId { get; private init; }

    public PersonSex Sex { get; private init; }

    public long BirthTimeSeconds { get; private init; }

    public double LatitudeDegrees { get; private init; }

    public double LongitudeDegrees { get; private init; }

    public PersonId? ParentId { get; private init; }

    public PersonNeedsState Needs { get; private init; }

    public PersonActivity Activity { get; private init; }

    public PregnancyState? Pregnancy { get; private init; }

    public OrganismMaterialState Material { get; private init; }

    public double AgeYears(long currentTimeSeconds)
    {
        const double secondsPerYear = 31_536_000d;

        return Math.Max(
            0,
            (currentTimeSeconds - BirthTimeSeconds)
            / secondsPerYear);
    }

    public PersonState WithSurvivalState(
        PersonNeedsState needs,
        PersonActivity activity)
    {
        ArgumentNullException.ThrowIfNull(needs);

        return new PersonState(
            Id,
            PlanetId,
            Sex,
            BirthTimeSeconds,
            LatitudeDegrees,
            LongitudeDegrees,
            ParentId,
            needs,
            activity,
            Pregnancy,
            Material);
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
            ParentId,
            Needs,
            Activity,
            Pregnancy,
            Material);
    }

    public PersonState WithPregnancy(
        PregnancyState pregnancy)
    {
        ArgumentNullException.ThrowIfNull(pregnancy);

        return new PersonState(
            Id,
            PlanetId,
            Sex,
            BirthTimeSeconds,
            LatitudeDegrees,
            LongitudeDegrees,
            ParentId,
            Needs,
            Activity,
            pregnancy,
            Material);
    }

    public PersonState WithoutPregnancy()
    {
        return new PersonState(
            Id,
            PlanetId,
            Sex,
            BirthTimeSeconds,
            LatitudeDegrees,
            LongitudeDegrees,
            ParentId,
            Needs,
            Activity,
            pregnancy: null,
            material: Material);
    }
}
