using Est.Simulation.Biogeochemistry;
using Est.Simulation.Planets;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

/// <summary>
/// Atomically applies vegetation growth and plant-available nitrogen uptake for
/// one planet.
/// </summary>
public sealed record ReplacePlanetVegetationBiogeochemistryStateOperation
    : ISimulationOperation
{
    public ReplacePlanetVegetationBiogeochemistryStateOperation(
        PlanetVegetationState vegetation,
        PlanetBiogeochemistryState biogeochemistry)
    {
        ArgumentNullException.ThrowIfNull(
            vegetation);

        ArgumentNullException.ThrowIfNull(
            biogeochemistry);

        if (vegetation.PlanetId !=
            biogeochemistry.PlanetId)
        {
            throw new ArgumentException(
                "Vegetation and biogeochemistry must belong to the same planet.");
        }

        Vegetation =
            vegetation;

        Biogeochemistry =
            biogeochemistry;
    }

    public PlanetVegetationState Vegetation { get; }

    public PlanetBiogeochemistryState Biogeochemistry { get; }

    public WorldState Apply(
        WorldState world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        if (!world.Planets.Any(
                planet =>
                    planet.Id ==
                    Vegetation.PlanetId))
        {
            throw new PlanetNotFoundException(
                Vegetation.PlanetId);
        }

        var preservedVegetation =
            world.Vegetation.Where(
                candidate =>
                    candidate.PlanetId !=
                    Vegetation.PlanetId);

        var preservedBiogeochemistry =
            world.Biogeochemistry.Where(
                candidate =>
                    candidate.PlanetId !=
                    Biogeochemistry.PlanetId);

        return world
            .ReplaceVegetation(
                preservedVegetation.Append(
                    Vegetation))
            .ReplaceBiogeochemistry(
                preservedBiogeochemistry.Append(
                    Biogeochemistry));
    }
}
