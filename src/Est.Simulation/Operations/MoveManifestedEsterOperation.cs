using Est.Simulation.Players;
using Est.Simulation.Social;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

public sealed record MoveManifestedEsterOperation
    : ISimulationOperation
{
    public MoveManifestedEsterOperation(
        EsterId esterId,
        double latitudeDegrees,
        double longitudeDegrees)
    {
        EsterId = esterId;
        LatitudeDegrees = latitudeDegrees;
        LongitudeDegrees = longitudeDegrees;
    }

    public EsterId EsterId { get; }

    public double LatitudeDegrees { get; }

    public double LongitudeDegrees { get; }

    public WorldState Apply(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var manifestation =
            world.ManifestedEsters.FirstOrDefault(
                candidate =>
                    candidate.EsterId == EsterId)
            ?? throw new InvalidOperationException(
                "The Ester is not manifested in this world.");

        ManifestedEsterState moved =
            manifestation.MoveTo(
                LatitudeDegrees,
                LongitudeDegrees);

        return world.ReplaceManifestedEsters(
            world.ManifestedEsters.Select(
                candidate =>
                    candidate.EsterId == EsterId
                        ? moved
                        : candidate));
    }
}
