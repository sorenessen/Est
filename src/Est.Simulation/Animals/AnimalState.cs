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
        AnimalActivity activity = AnimalActivity.Idle)
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

        Id = id;
        PlanetId = planetId;
        Species = species;
        LatitudeDegrees = latitudeDegrees;
        LongitudeDegrees = longitudeDegrees;
        EnergyReserve = energyReserve;
        Health = health;
        Activity = activity;
    }

    public AnimalId Id { get; private init; }

    public PlanetId PlanetId { get; private init; }

    public AnimalSpecies Species { get; private init; }

    public double LatitudeDegrees { get; private init; }

    public double LongitudeDegrees { get; private init; }

    public double EnergyReserve { get; private init; }

    public double Health { get; private init; }

    public AnimalActivity Activity { get; private init; }

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
            activity);
    }
}
