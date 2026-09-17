using Est.Simulation.Biogeochemistry;
using Est.Simulation.Invertebrates;
using Est.Simulation.Planets;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

/// <summary>
/// Atomically applies aggregate invertebrate, vegetation, and optional
/// biogeochemistry consequences for one planet.
/// </summary>
public sealed record ReplacePlanetInvertebrateVegetationStateOperation
    : ISimulationOperation
{
    public ReplacePlanetInvertebrateVegetationStateOperation(
        PlanetInvertebrateState invertebrates,
        PlanetVegetationState vegetation,
        PlanetBiogeochemistryState? biogeochemistry = null)
    {
        ArgumentNullException.ThrowIfNull(
            invertebrates);

        ArgumentNullException.ThrowIfNull(
            vegetation);

        if (invertebrates.PlanetId !=
            vegetation.PlanetId)
        {
            throw new ArgumentException(
                "Invertebrates and vegetation must belong to the same planet.");
        }

        if (biogeochemistry is not null &&
            biogeochemistry.PlanetId !=
                invertebrates.PlanetId)
        {
            throw new ArgumentException(
                "Invertebrates and biogeochemistry must belong to the same planet.");
        }

        Invertebrates =
            invertebrates;

        Vegetation =
            vegetation;

        Biogeochemistry =
            biogeochemistry;
    }

    public PlanetInvertebrateState Invertebrates { get; }

    public PlanetVegetationState Vegetation { get; }

    public PlanetBiogeochemistryState? Biogeochemistry { get; }

    public WorldState Apply(
        WorldState world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        if (!world.Planets.Any(
                planet =>
                    planet.Id ==
                    Invertebrates.PlanetId))
        {
            throw new PlanetNotFoundException(
                Invertebrates.PlanetId);
        }

        var preservedInvertebrates =
            world.Invertebrates.Where(
                candidate =>
                    candidate.PlanetId !=
                    Invertebrates.PlanetId);

        var preservedVegetation =
            world.Vegetation.Where(
                candidate =>
                    candidate.PlanetId !=
                    Vegetation.PlanetId);

        var next =
            world
                .ReplaceInvertebrates(
                    preservedInvertebrates.Append(
                        Invertebrates))
                .ReplaceVegetation(
                    preservedVegetation.Append(
                        Vegetation));

        if (Biogeochemistry is null)
        {
            return next;
        }

        var preservedBiogeochemistry =
            world.Biogeochemistry.Where(
                candidate =>
                    candidate.PlanetId !=
                    Biogeochemistry.PlanetId);

        return next.ReplaceBiogeochemistry(
            preservedBiogeochemistry.Append(
                Biogeochemistry));
    }
}
