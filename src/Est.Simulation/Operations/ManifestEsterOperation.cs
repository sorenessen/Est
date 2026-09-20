using Est.Simulation.Players;
using Est.Simulation.Planets;
using Est.Simulation.Social;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

public sealed record ManifestEsterOperation
    : ISimulationOperation
{
    public ManifestEsterOperation(
        EsterId esterId,
        PlanetId planetId,
        double latitudeDegrees,
        double longitudeDegrees)
    {
        EsterId = esterId;
        PlanetId = planetId;
        LatitudeDegrees = latitudeDegrees;
        LongitudeDegrees = longitudeDegrees;
    }

    public EsterId EsterId { get; }

    public PlanetId PlanetId { get; }

    public double LatitudeDegrees { get; }

    public double LongitudeDegrees { get; }

    public WorldState Apply(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!world.Planets.Any(
                planet =>
                    planet.Id == PlanetId))
        {
            throw new PlanetNotFoundException(
                PlanetId);
        }

        if (world.ManifestedEsters.Any(
                manifested =>
                    manifested.EsterId == EsterId))
        {
            throw new InvalidOperationException(
                "This Ester is already manifested in the world.");
        }

        var manifestation =
            new ManifestedEsterState(
                EsterId,
                PlanetId,
                LatitudeDegrees,
                LongitudeDegrees);

        return world.ReplaceManifestedEsters(
            world.ManifestedEsters.Add(
                manifestation));
    }
}
