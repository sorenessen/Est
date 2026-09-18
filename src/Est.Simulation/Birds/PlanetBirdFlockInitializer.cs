using Est.Simulation.Invertebrates;
using Est.Simulation.Planets;
using Est.Simulation.Surface;

namespace Est.Simulation.Birds;

/// <summary>
/// Creates coarse initial bird flocks from authoritative aggregate
/// invertebrate support.
///
/// Invertebrate biomass is read as prey / habitat support only. No biomass is
/// removed during initialization.
///
/// Planet-wide supported population is preserved when many supported surface
/// cells are compressed into a bounded number of coarse flock entities.
/// Surface cells determine deterministic flock centers, not permanent bird
/// occupancy.
/// </summary>
public static class PlanetBirdFlockInitializer
{
    public static IReadOnlyList<BirdFlockState>
        FromInvertebrateSupport(
            PlanetState planet,
            PlanetInvertebrateState invertebrates,
            BirdModelParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(
            planet);

        ArgumentNullException.ThrowIfNull(
            invertebrates);

        ArgumentNullException.ThrowIfNull(
            parameters);

        invertebrates.ValidateFor(
            planet);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                invertebrates.GridDefinition);

        var candidates =
            new List<Candidate>();

        var totalSupportedBirds =
            0d;

        foreach (var surfaceCell in
                 grid.Cells)
        {
            var invertebrateCell =
                invertebrates.GetCell(
                    surfaceCell.Id);

            var totalInvertebrateBiomassKilograms =
                invertebrateCell
                    .LiveBiomassKilogramsPerSquareMeter *
                surfaceCell.AreaSquareMeters;

            var localSupportedBirds =
                totalInvertebrateBiomassKilograms *
                parameters
                    .CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass *
                parameters
                    .InitialFractionOfLocalCarryingCapacity;

            if (!double.IsFinite(
                    localSupportedBirds))
            {
                throw new InvalidOperationException(
                    "Bird initialization produced a non-finite local population.");
            }

            totalSupportedBirds +=
                localSupportedBirds;

            if (!double.IsFinite(
                    totalSupportedBirds))
            {
                throw new InvalidOperationException(
                    "Bird initialization produced a non-finite planet population.");
            }

            if (localSupportedBirds > 0)
            {
                candidates.Add(
                    new Candidate(
                        surfaceCell,
                        localSupportedBirds));
            }
        }

        if (candidates.Count == 0 ||
            totalSupportedBirds <
            parameters.MinimumInitialFlockMemberCount)
        {
            return [];
        }

        if (totalSupportedBirds >
            long.MaxValue)
        {
            throw new InvalidOperationException(
                "Bird initialization produced a planet population larger than the supported range.");
        }

        var totalMemberCount =
            (long)Math.Floor(
                totalSupportedBirds);

        if (totalMemberCount <
            parameters.MinimumInitialFlockMemberCount)
        {
            return [];
        }

        var maximumFlocksFromPopulation =
            totalMemberCount /
            parameters.MinimumInitialFlockMemberCount;

        var flockCount =
            (int)Math.Min(
                Math.Min(
                    (long)candidates.Count,
                    parameters.MaximumInitialFlockCount),
                maximumFlocksFromPopulation);

        if (flockCount <= 0)
        {
            return [];
        }

        var baseMemberCount =
            totalMemberCount /
            flockCount;

        var remainder =
            totalMemberCount %
            flockCount;

        if (baseMemberCount >
            int.MaxValue)
        {
            throw new InvalidOperationException(
                "Bird initialization cannot represent the supported population within the configured flock-count limit.");
        }

        var selectedCenters =
            SelectSpatiallyDistributedCenters(
                candidates,
                flockCount);

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
                            "Bird initialization produced a flock larger than the supported member-count range.");
                    }

                    return new BirdFlockState(
                        BirdFlockId.CreateDeterministic(
                            planet.Id,
                            candidate.Cell.Id),
                        planet.Id,
                        (int)memberCount,
                        candidate.Cell.CenterLatitudeDegrees,
                        candidate.Cell.CenterLongitudeDegrees,
                        material:
                            parameters.MaterialPerBird.ForUnits(
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
                        candidate.SupportedBirds)
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
            candidate.Candidate.SupportedBirds
                .CompareTo(
                    currentBest
                        .Candidate
                        .SupportedBirds);

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
        double SupportedBirds);
}
