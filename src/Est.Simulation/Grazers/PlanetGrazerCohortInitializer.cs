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
            SelectSpatiallyDistributedCenters(
                candidates,
                cohortCount);

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
                        candidate.Cell.CenterLongitudeDegrees,
                        material:
                            parameters.MaterialPerGrazer.ForUnits(
                                (int)memberCount));
                })
            .ToArray();
    }

    private static Candidate[]
        SelectSpatiallyDistributedCenters(
            IReadOnlyList<Candidate> candidates,
            int count)
    {
        var ordered =
            candidates
                .OrderByDescending(
                    candidate =>
                        candidate.SupportedGrazers)
                .ThenBy(
                    candidate =>
                        candidate.Cell.Id.Value)
                .ToArray();

        if (count >= ordered.Length)
        {
            return ordered;
        }

        var selected =
            new List<Candidate>(
                count)
            {
                ordered[0]
            };

        var remaining =
            ordered
                .Skip(1)
                .Select(
                    candidate =>
                        (
                            Candidate: candidate,
                            MinimumDistance:
                                SurfaceDistanceScore(
                                    ordered[0].Cell,
                                    candidate.Cell)
                        ))
                .ToList();

        while (selected.Count < count)
        {
            var bestIndex =
                0;

            for (var index = 1;
                 index < remaining.Count;
                 index++)
            {
                if (IsBetterCenter(
                        remaining[index],
                        remaining[bestIndex]))
                {
                    bestIndex =
                        index;
                }
            }

            var next =
                remaining[bestIndex]
                    .Candidate;

            selected.Add(
                next);

            remaining.RemoveAt(
                bestIndex);

            for (var index = 0;
                 index < remaining.Count;
                 index++)
            {
                var item =
                    remaining[index];

                var distance =
                    SurfaceDistanceScore(
                        next.Cell,
                        item.Candidate.Cell);

                if (distance <
                    item.MinimumDistance)
                {
                    remaining[index] =
                        (
                            item.Candidate,
                            distance
                        );
                }
            }
        }

        return selected.ToArray();
    }

    private static bool IsBetterCenter(
        (
            Candidate Candidate,
            double MinimumDistance
        ) candidate,
        (
            Candidate Candidate,
            double MinimumDistance
        ) currentBest)
    {
        const double tolerance =
            1e-12;

        if (candidate.MinimumDistance >
            currentBest.MinimumDistance +
            tolerance)
        {
            return true;
        }

        if (candidate.MinimumDistance <
            currentBest.MinimumDistance -
            tolerance)
        {
            return false;
        }

        var supportComparison =
            candidate.Candidate.SupportedGrazers
                .CompareTo(
                    currentBest
                        .Candidate
                        .SupportedGrazers);

        if (supportComparison != 0)
        {
            return supportComparison >
                0;
        }

        return candidate
                   .Candidate
                   .Cell
                   .Id
                   .Value
                   .CompareTo(
                       currentBest
                           .Candidate
                           .Cell
                           .Id
                           .Value) <
               0;
    }

    private static double SurfaceDistanceScore(
        SurfaceCell left,
        SurfaceCell right)
    {
        const double degreesToRadians =
            Math.PI /
            180d;

        var leftLatitude =
            left.CenterLatitudeDegrees *
            degreesToRadians;

        var rightLatitude =
            right.CenterLatitudeDegrees *
            degreesToRadians;

        var longitudeDelta =
            (
                left.CenterLongitudeDegrees -
                right.CenterLongitudeDegrees
            ) *
            degreesToRadians;

        var cosine =
            Math.Sin(leftLatitude) *
                Math.Sin(rightLatitude) +
            Math.Cos(leftLatitude) *
                Math.Cos(rightLatitude) *
                Math.Cos(longitudeDelta);

        return 1d -
            Math.Clamp(
                cosine,
                -1d,
                1d);
    }

    private sealed record Candidate(
        SurfaceCell Cell,
        double SupportedGrazers);
}
