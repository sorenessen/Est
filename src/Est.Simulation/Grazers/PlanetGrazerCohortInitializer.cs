using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Vegetation;

namespace Est.Simulation.Grazers;

/// <summary>
/// Creates coarse initial grazer cohorts from authoritative live vegetation
/// support.
///
/// Vegetation biomass is read as carrying-capacity support only. No biomass is
/// removed during initialization.
///
/// Planet-wide supported population is preserved when many supported surface
/// cells are compressed into a bounded number of coarse cohort entities.
/// Surface cells determine deterministic initial cohort centers, not permanent
/// occupancy.
/// </summary>
public static class PlanetGrazerCohortInitializer
{
    public static IReadOnlyList<GrazerCohortState>
        FromVegetationSupport(
            PlanetState planet,
            PlanetVegetationState vegetation,
            GrazerModelParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(
            planet);

        ArgumentNullException.ThrowIfNull(
            vegetation);

        ArgumentNullException.ThrowIfNull(
            parameters);

        vegetation.ValidateFor(
            planet);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                vegetation.GridDefinition);

        var candidates =
            new List<Candidate>();

        var totalSupportedGrazers =
            0d;

        foreach (var surfaceCell in
                 grid.Cells)
        {
            var vegetationCell =
                vegetation.GetCell(
                    surfaceCell.Id);

            var totalLiveVegetationBiomassKilograms =
                vegetationCell
                    .LiveBiomassKilogramsPerSquareMeter *
                surfaceCell.AreaSquareMeters;

            var localSupportedGrazers =
                totalLiveVegetationBiomassKilograms *
                parameters
                    .CarryingCapacityGrazersPerKilogramLiveVegetationBiomass *
                parameters
                    .InitialFractionOfLocalCarryingCapacity;

            if (!double.IsFinite(
                    localSupportedGrazers))
            {
                throw new InvalidOperationException(
                    "Grazer initialization produced a non-finite local population.");
            }

            totalSupportedGrazers +=
                localSupportedGrazers;

            if (!double.IsFinite(
                    totalSupportedGrazers))
            {
                throw new InvalidOperationException(
                    "Grazer initialization produced a non-finite planet population.");
            }

            if (localSupportedGrazers > 0)
            {
                candidates.Add(
                    new Candidate(
                        surfaceCell,
                        localSupportedGrazers));
            }
        }

        if (candidates.Count == 0 ||
            totalSupportedGrazers <
            parameters.MinimumInitialCohortMemberCount)
        {
            return [];
        }

        if (totalSupportedGrazers >
            long.MaxValue)
        {
            throw new InvalidOperationException(
                "Grazer initialization produced a planet population larger than the supported range.");
        }

        var totalMemberCount =
            (long)Math.Floor(
                totalSupportedGrazers);

        if (totalMemberCount <
            parameters.MinimumInitialCohortMemberCount)
        {
            return [];
        }

        var maximumCohortsFromPopulation =
            totalMemberCount /
            parameters.MinimumInitialCohortMemberCount;

        var cohortCount =
            (int)Math.Min(
                Math.Min(
                    (long)candidates.Count,
                    parameters.MaximumInitialCohortCount),
                maximumCohortsFromPopulation);

        if (cohortCount <= 0)
        {
            return [];
        }

        var baseMemberCount =
            totalMemberCount /
            cohortCount;

        var remainder =
            totalMemberCount %
            cohortCount;

        if (baseMemberCount >
            int.MaxValue)
        {
            throw new InvalidOperationException(
                "Grazer initialization cannot represent the supported population within the configured cohort-count limit.");
        }

        var selectedCenters =
            candidates
                .OrderByDescending(
                    candidate =>
                        candidate.SupportedGrazers)
                .ThenBy(
                    candidate =>
                        candidate.Cell.Id.Value)
                .Take(
                    cohortCount)
                .ToArray();

        return selectedCenters
            .Select(
                (candidate, index) =>
                {
                    var memberCount =
                        baseMemberCount +
                        (index < remainder
                            ? 1
                            : 0);

                    if (memberCount >
                        int.MaxValue)
                    {
                        throw new InvalidOperationException(
                            "Grazer initialization produced a cohort larger than the supported member-count range.");
                    }

                    return new GrazerCohortState(
                        GrazerCohortId.CreateDeterministic(
                            planet.Id,
                            candidate.Cell.Id),
                        planet.Id,
                        (int)memberCount,
                        candidate.Cell.CenterLatitudeDegrees,
                        candidate.Cell.CenterLongitudeDegrees);
                })
            .ToArray();
    }

    private sealed record Candidate(
        SurfaceCell Cell,
        double SupportedGrazers);
}
