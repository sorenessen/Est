using Est.Simulation.Birds;
using Est.Simulation.Planets;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

public sealed record ReplacePlanetBirdFlocksOperation
    : ISimulationOperation
{
    public ReplacePlanetBirdFlocksOperation(
        PlanetId planetId,
        IEnumerable<BirdFlockState> birdFlocks)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(
            birdFlocks);

        PlanetId =
            planetId;

        BirdFlocks =
            birdFlocks.ToArray();

        if (BirdFlocks.Any(
                flock =>
                    flock is null))
        {
            throw new ArgumentException(
                "Bird flocks cannot contain null entries.",
                nameof(birdFlocks));
        }

        if (BirdFlocks.Any(
                flock =>
                    flock.PlanetId !=
                    planetId))
        {
            throw new ArgumentException(
                "Bird flocks contain a flock assigned to another planet.",
                nameof(birdFlocks));
        }
    }

    public PlanetId PlanetId { get; }

    public IReadOnlyList<BirdFlockState>
        BirdFlocks { get; }

    public WorldState Apply(
        WorldState world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        if (!world.Planets.Any(
                planet =>
                    planet.Id ==
                    PlanetId))
        {
            throw new PlanetNotFoundException(
                PlanetId);
        }

        var preserved =
            world.BirdFlocks.Where(
                flock =>
                    flock.PlanetId !=
                    PlanetId);

        return world.ReplaceBirdFlocks(
            preserved.Concat(
                BirdFlocks));
    }
}
