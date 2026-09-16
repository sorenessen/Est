using Est.Simulation.Organisms;
using Est.Simulation.Planets;

namespace Est.Simulation.Animals;

public sealed record AnimalState
{
    public AnimalState(
        AnimalId id,
        PlanetId planetId,
        AnimalSpecies species,
        double latitudeDegrees,
        double longitudeDegrees,
        double energyReserve = 1,
        double health = 1,
        AnimalActivity activity = AnimalActivity.Idle,
        OrganismMaterialState? material = null,
        long birthTimeSeconds = 0,
        AnimalId? parentId = null,
        WolfLifecycleState? wolfLifecycle = null)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Animal identity cannot be empty.",
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
                nameof(latitudeDegrees));
        }

        if (!double.IsFinite(longitudeDegrees) ||
            longitudeDegrees < -180 ||
            longitudeDegrees > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitudeDegrees));
        }

        if (!double.IsFinite(energyReserve) ||
            energyReserve < 0 ||
            energyReserve > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(energyReserve));
        }

        if (!double.IsFinite(health) ||
            health < 0 ||
            health > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(health));
        }

        if (parentId is not null &&
            parentId.Value.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Parent identity cannot be empty.",
                nameof(parentId));
        }

        if (wolfLifecycle is not null &&
            species != AnimalSpecies.Wolf)
        {
            throw new ArgumentException(
                "Wolf lifecycle state can only be attached to a wolf.",
                nameof(wolfLifecycle));
        }

        Id = id;
        PlanetId = planetId;
        Species = species;
        LatitudeDegrees = latitudeDegrees;
        LongitudeDegrees = longitudeDegrees;
        EnergyReserve = energyReserve;
        Health = health;
        Activity = activity;
        Material =
            material ??
            new OrganismMaterialState(
                liveBiomassKilograms: 0,
                liveNitrogenKilograms: 0);
        BirthTimeSeconds = birthTimeSeconds;
        ParentId = parentId;
        WolfLifecycle =
            species == AnimalSpecies.Wolf
                ? wolfLifecycle ??
                  new WolfLifecycleState(
                      WolfSex.Unknown)
                : null;
    }

    public AnimalId Id { get; private init; }

    public PlanetId PlanetId { get; private init; }

    public AnimalSpecies Species { get; private init; }

    public double LatitudeDegrees { get; private init; }

    public double LongitudeDegrees { get; private init; }

    public double EnergyReserve { get; private init; }

    public double Health { get; private init; }

    public AnimalActivity Activity { get; private init; }

    public OrganismMaterialState Material { get; private init; }

    public long BirthTimeSeconds { get; private init; }

    public AnimalId? ParentId { get; private init; }

    public WolfLifecycleState? WolfLifecycle
    {
        get;
        private init;
    }

    public double AgeYears(long currentTimeSeconds)
    {
        const double secondsPerYear = 31_536_000d;

        return OrganismLifecycleClock.AgeSeconds(
                   BirthTimeSeconds,
                   currentTimeSeconds)
               / secondsPerYear;
    }

    public AnimalState WithState(
        double latitudeDegrees,
        double longitudeDegrees,
        double energyReserve,
        double health,
        AnimalActivity activity)
    {
        return new AnimalState(
            Id,
            PlanetId,
            Species,
            latitudeDegrees,
            longitudeDegrees,
            Math.Clamp(energyReserve, 0, 1),
            Math.Clamp(health, 0, 1),
            activity,
            Material,
            BirthTimeSeconds,
            ParentId,
            WolfLifecycle);
    }

    public AnimalState WithWolfLifecycle(
        WolfLifecycleState wolfLifecycle)
    {
        ArgumentNullException.ThrowIfNull(
            wolfLifecycle);

        if (Species != AnimalSpecies.Wolf)
        {
            throw new InvalidOperationException(
                "Wolf lifecycle state can only be attached to a wolf.");
        }

        return new AnimalState(
            Id,
            PlanetId,
            Species,
            LatitudeDegrees,
            LongitudeDegrees,
            EnergyReserve,
            Health,
            Activity,
            Material,
            BirthTimeSeconds,
            ParentId,
            wolfLifecycle);
    }
}
