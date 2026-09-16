using Est.Simulation.Grazers;
using Est.Simulation.Planets;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

public sealed record ReplacePlanetGrazerCohortsOperation
    : ISimulationOperation
{
    public ReplacePlanetGrazerCohortsOperation(
        PlanetId planetId,
        IEnumerable<GrazerCohortState> grazerCohorts)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(
            grazerCohorts);

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
    }

    public PlanetId PlanetId { get; }

    public IReadOnlyList<GrazerCohortState>
        GrazerCohorts { get; }

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
            world.GrazerCohorts.Where(
                cohort =>
                    cohort.PlanetId !=
                    PlanetId);

        return world.ReplaceGrazerCohorts(
            preserved.Concat(
                GrazerCohorts));
    }
}
