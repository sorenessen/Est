using Est.Simulation.Biogeochemistry;
using Est.Simulation.Invertebrates;
using Est.Simulation.Planets;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

/// <summary>
/// Atomically applies aggregate invertebrate biomass dynamics and associated
/// mortality-detritus changes for one planet.
/// </summary>
public sealed record ReplacePlanetInvertebrateBiogeochemistryStateOperation
    : ISimulationOperation
{
    public ReplacePlanetInvertebrateBiogeochemistryStateOperation(
        PlanetInvertebrateState invertebrates,
        PlanetBiogeochemistryState biogeochemistry)
    {
        ArgumentNullException.ThrowIfNull(
            invertebrates);

        ArgumentNullException.ThrowIfNull(
            biogeochemistry);

        if (invertebrates.PlanetId !=
            biogeochemistry.PlanetId)
        {
            throw new ArgumentException(
                "Invertebrates and biogeochemistry must belong to the same planet.");
        }

        Invertebrates =
            invertebrates;

        Biogeochemistry =
            biogeochemistry;
    }

    public PlanetInvertebrateState Invertebrates { get; }

    public PlanetBiogeochemistryState Biogeochemistry { get; }

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

        var preservedBiogeochemistry =
            world.Biogeochemistry.Where(
                candidate =>
                    candidate.PlanetId !=
                    Biogeochemistry.PlanetId);

        return world
            .ReplaceInvertebrates(
                preservedInvertebrates.Append(
                    Invertebrates))
            .ReplaceBiogeochemistry(
                preservedBiogeochemistry.Append(
                    Biogeochemistry));
    }
}
