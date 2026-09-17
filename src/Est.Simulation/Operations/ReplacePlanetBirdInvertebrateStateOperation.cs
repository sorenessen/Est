using Est.Simulation.Biogeochemistry;
using Est.Simulation.Birds;
using Est.Simulation.Invertebrates;
using Est.Simulation.Planets;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

/// <summary>
/// Atomically applies bird-flock and invertebrate-prey consequences for one
/// planet, with optional associated biogeochemistry.
/// </summary>
public sealed record ReplacePlanetBirdInvertebrateStateOperation
    : ISimulationOperation
{
    public ReplacePlanetBirdInvertebrateStateOperation(
        PlanetId planetId,
        IEnumerable<BirdFlockState> birdFlocks,
        PlanetInvertebrateState invertebrates,
        PlanetBiogeochemistryState? biogeochemistry = null)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(
            birdFlocks);

        ArgumentNullException.ThrowIfNull(
            invertebrates);

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

        if (invertebrates.PlanetId !=
            planetId)
        {
            throw new ArgumentException(
                "Invertebrates belong to another planet.",
                nameof(invertebrates));
        }

        Invertebrates =
            invertebrates;

        if (biogeochemistry is not null &&
            biogeochemistry.PlanetId !=
                planetId)
        {
            throw new ArgumentException(
                "Biogeochemistry belongs to another planet.",
                nameof(biogeochemistry));
        }

        Biogeochemistry =
            biogeochemistry;
    }

    public PlanetId PlanetId { get; }

    public IReadOnlyList<BirdFlockState>
        BirdFlocks { get; }

    public PlanetInvertebrateState Invertebrates { get; }

    public PlanetBiogeochemistryState? Biogeochemistry { get; }

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

        var preservedFlocks =
            world.BirdFlocks.Where(
                flock =>
                    flock.PlanetId !=
                    PlanetId);

        var preservedInvertebrates =
            world.Invertebrates.Where(
                state =>
                    state.PlanetId !=
                    PlanetId);

        var next =
            world
                .ReplaceBirdFlocks(
                    preservedFlocks.Concat(
                        BirdFlocks))
                .ReplaceInvertebrates(
                    preservedInvertebrates.Append(
                        Invertebrates));

        if (Biogeochemistry is null)
        {
            return next;
        }

        var preservedBiogeochemistry =
            world.Biogeochemistry.Where(
                state =>
                    state.PlanetId !=
                    PlanetId);

        return next.ReplaceBiogeochemistry(
            preservedBiogeochemistry.Append(
                Biogeochemistry));
    }
}
