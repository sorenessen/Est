using Est.Simulation.Biogeochemistry;
using Est.Simulation.Grazers;
using Est.Simulation.Planets;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

/// <summary>
/// Atomically applies grazer-cohort and vegetation consequences for one planet.
/// </summary>
public sealed record ReplacePlanetGrazerVegetationStateOperation
    : ISimulationOperation
{
    public ReplacePlanetGrazerVegetationStateOperation(
        PlanetId planetId,
        IEnumerable<GrazerCohortState> grazerCohorts,
        PlanetVegetationState vegetation,
        PlanetBiogeochemistryState? biogeochemistry = null)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(
            grazerCohorts);

        ArgumentNullException.ThrowIfNull(
            vegetation);

        PlanetId =
            planetId;

        GrazerCohorts =
            grazerCohorts.ToArray();

        if (GrazerCohorts.Any(
                cohort =>
                    cohort is null))
        {
            throw new ArgumentException(
                "Grazer cohorts cannot contain null entries.",
                nameof(grazerCohorts));
        }

        if (GrazerCohorts.Any(
                cohort =>
                    cohort.PlanetId !=
                    planetId))
        {
            throw new ArgumentException(
                "Grazer cohorts contain a cohort assigned to another planet.",
                nameof(grazerCohorts));
        }

        if (vegetation.PlanetId !=
            planetId)
        {
            throw new ArgumentException(
                "Vegetation belongs to another planet.",
                nameof(vegetation));
        }

        Vegetation =
            vegetation;

        if (biogeochemistry is not null &&
            biogeochemistry.PlanetId != planetId)
        {
            throw new ArgumentException(
                "Biogeochemistry belongs to another planet.",
                nameof(biogeochemistry));
        }

        Biogeochemistry = biogeochemistry;
    }

    public PlanetId PlanetId { get; }

    public IReadOnlyList<GrazerCohortState>
        GrazerCohorts { get; }

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
                    PlanetId))
        {
            throw new PlanetNotFoundException(
                PlanetId);
        }

        var preservedCohorts =
            world.GrazerCohorts.Where(
                cohort =>
                    cohort.PlanetId !=
                    PlanetId);

        var preservedVegetation =
            world.Vegetation.Where(
                vegetation =>
                    vegetation.PlanetId !=
                    PlanetId);

        var next =
            world
                .ReplaceGrazerCohorts(
                    preservedCohorts.Concat(
                        GrazerCohorts))
                .ReplaceVegetation(
                    preservedVegetation.Append(
                        Vegetation));

        if (Biogeochemistry is null)
        {
            return next;
        }

        var preservedBiogeochemistry =
            world.Biogeochemistry.Where(
                state =>
                    state.PlanetId != PlanetId);

        return next.ReplaceBiogeochemistry(
            preservedBiogeochemistry.Append(
                Biogeochemistry));
    }
}
