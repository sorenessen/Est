using Est.Simulation.Planets;

namespace Est.Simulation.Ecology;

public sealed record FoodResourceState
{
    public FoodResourceState(
        FoodResourceId id,
        PlanetId planetId,
        double latitudeDegrees,
        double longitudeDegrees,
        double availableEnergy)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Food resource identity cannot be empty.",
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

        if (!double.IsFinite(availableEnergy) ||
            availableEnergy < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(availableEnergy),
                "Available energy must be finite and non-negative.");
        }

        Id = id;
        PlanetId = planetId;
        LatitudeDegrees = latitudeDegrees;
        LongitudeDegrees = longitudeDegrees;
        AvailableEnergy = availableEnergy;
    }

    public FoodResourceId Id { get; }

    public PlanetId PlanetId { get; }

    public double LatitudeDegrees { get; }

    public double LongitudeDegrees { get; }

    public double AvailableEnergy { get; }

    public FoodResourceState Consume(
        double energy)
    {
        if (!double.IsFinite(energy) ||
            energy < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(energy));
        }

        if (energy > AvailableEnergy)
        {
            throw new InvalidOperationException(
                "Food resource does not contain enough available energy.");
        }

        return new FoodResourceState(
            Id,
            PlanetId,
            LatitudeDegrees,
            LongitudeDegrees,
            AvailableEnergy - energy);
    }
}
