using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;

namespace Est.Simulation.Hydrology;

/// <summary>
/// Redistributes ponded surface water within derived terrain basins.
///
/// Water below a basin's spill elevation settles to one common local water
/// surface. Water above the spill elevation is retained to the spill crest
/// and the excess is transferred across the basin's spill connection.
///
/// Spill transfers are simultaneous for this pass. They do not recursively
/// cascade through additional basins until a later integration substep.
/// </summary>
public static class PlanetHydrologyBasinEquilibrator
{
    private const double WaterDensityKilogramsPerCubicMeter =
        1_000d;

    public static BasinEquilibriumResult Equilibrate(
        PlanetState planet,
        PlanetTerrainState terrain,
        PlanetHydrologyState hydrology,
        PlanetTerrainBasinTopology basins)
    {
        ArgumentNullException.ThrowIfNull(
            planet);

        ArgumentNullException.ThrowIfNull(
            terrain);

        ArgumentNullException.ThrowIfNull(
            hydrology);

        ArgumentNullException.ThrowIfNull(
            basins);

        terrain.ValidateFor(
            planet);

        hydrology.ValidateFor(
            planet);

        if (terrain.GridDefinition !=
            hydrology.GridDefinition)
        {
            throw new ArgumentException(
                "Hydrology and terrain must use the same surface grid.");
        }

        if (basins.PlanetId != planet.Id ||
            basins.GridDefinition !=
            terrain.GridDefinition)
        {
            throw new ArgumentException(
                "Basin topology must describe the same planet and surface grid.",
                nameof(basins));
        }

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                terrain.GridDefinition);

        var terrainByCellId =
            terrain.Cells.ToDictionary(
                cell =>
                    cell.CellId);

        var hydrologyByCellId =
            hydrology.Cells.ToDictionary(
                cell =>
                    cell.CellId);

        var surfaceCellsById =
            grid.Cells.ToDictionary(
                cell =>
                    cell.Id);

        var nextSurfaceWater =
            hydrology.Cells.ToDictionary(
                cell =>
                    cell.CellId,
                cell =>
                    cell
                        .SurfaceLiquidWaterKilogramsPerSquareMeter);

        var incomingSpillMass =
            new Dictionary<
                SurfaceCellId,
                double>();

        var totalSpillMass =
            0d;

        foreach (var basin in
                 basins.Basins)
        {
            var sinkHydrology =
                hydrologyByCellId[
                    basin.SinkCellId];

            if (sinkHydrology
                    .SurfaceLiquidWaterKilogramsPerSquareMeter <=
                0)
            {
                continue;
            }

            var sinkWaterSurfaceElevation =
                terrainByCellId[
                    basin.SinkCellId]
                .ElevationMeters +
                sinkHydrology
                    .SurfaceLiquidWaterKilogramsPerSquareMeter /
                WaterDensityKilogramsPerCubicMeter;

            var included =
                basin.RetainedCellIds
                    .Where(
                        cellId =>
                            cellId ==
                            basin.SinkCellId ||
                            terrainByCellId[
                                cellId]
                            .ElevationMeters <=
                            sinkWaterSurfaceElevation)
                    .ToHashSet();

            double waterSurfaceElevation;

            while (true)
            {
                var poolMassKilograms =
                    included.Sum(
                        cellId =>
                            hydrologyByCellId[
                                cellId]
                            .SurfaceLiquidWaterKilogramsPerSquareMeter *
                            surfaceCellsById[
                                cellId]
                            .AreaSquareMeters);

                var poolVolumeCubicMeters =
                    poolMassKilograms /
                    WaterDensityKilogramsPerCubicMeter;

                waterSurfaceElevation =
                    SolveWaterSurfaceElevation(
                        basin.RetainedCellIds,
                        poolVolumeCubicMeters,
                        terrainByCellId,
                        surfaceCellsById);

                if (basin
                        .SpillElevationMeters
                        .HasValue &&
                    waterSurfaceElevation >=
                    basin
                        .SpillElevationMeters
                        .Value)
                {
                    included =
                        basin
                            .RetainedCellIds
                            .ToHashSet();

                    poolMassKilograms =
                        included.Sum(
                            cellId =>
                                hydrologyByCellId[
                                    cellId]
                                .SurfaceLiquidWaterKilogramsPerSquareMeter *
                                surfaceCellsById[
                                    cellId]
                                .AreaSquareMeters);

                    var spillElevation =
                        basin
                            .SpillElevationMeters
                            .Value;

                    var retainedVolumeCubicMeters =
                        basin
                            .RetainedCellIds
                            .Sum(
                                cellId =>
                                    Math.Max(
                                        0,
                                        spillElevation -
                                        terrainByCellId[
                                            cellId]
                                        .ElevationMeters) *
                                    surfaceCellsById[
                                        cellId]
                                    .AreaSquareMeters);

                    var retainedMassKilograms =
                        retainedVolumeCubicMeters *
                        WaterDensityKilogramsPerCubicMeter;

                    var spillMassKilograms =
                        Math.Max(
                            0,
                            poolMassKilograms -
                            retainedMassKilograms);

                    waterSurfaceElevation =
                        spillElevation;

                    if (spillMassKilograms > 0)
                    {
                        var destinationId =
                            basin
                                .SpillDestinationCellId
                            ?? throw new InvalidOperationException(
                                "A spilling basin must define a spill destination.");

                        if (incomingSpillMass.TryGetValue(
                                destinationId,
                                out var existingMass))
                        {
                            incomingSpillMass[
                                destinationId] =
                                existingMass +
                                spillMassKilograms;
                        }
                        else
                        {
                            incomingSpillMass[
                                destinationId] =
                                spillMassKilograms;
                        }

                        totalSpillMass +=
                            spillMassKilograms;
                    }

                    break;
                }

                var newlyReached =
                    basin
                        .RetainedCellIds
                        .Where(
                            cellId =>
                                !included.Contains(
                                    cellId) &&
                                terrainByCellId[
                                    cellId]
                                .ElevationMeters <=
                                waterSurfaceElevation)
                        .ToArray();

                if (newlyReached.Length == 0)
                {
                    break;
                }

                foreach (var cellId in
                         newlyReached)
                {
                    included.Add(
                        cellId);
                }
            }

            foreach (var cellId in
                     included)
            {
                nextSurfaceWater[
                    cellId] =
                    Math.Max(
                        0,
                        waterSurfaceElevation -
                        terrainByCellId[
                            cellId]
                        .ElevationMeters) *
                    WaterDensityKilogramsPerCubicMeter;
            }
        }

        foreach (var pair in
                 incomingSpillMass)
        {
            nextSurfaceWater[
                pair.Key] +=
                pair.Value /
                surfaceCellsById[
                    pair.Key]
                .AreaSquareMeters;
        }

        var nextCells =
            hydrology.Cells.Select(
                cell =>
                    new HydrologyCellState(
                        cell.CellId,
                        cell
                            .AtmosphericWaterKilogramsPerSquareMeter,
                        nextSurfaceWater[
                            cell.CellId],
                        cell
                            .SoilWaterKilogramsPerSquareMeter,
                        cell
                            .SnowIceWaterEquivalentKilogramsPerSquareMeter));

        return new BasinEquilibriumResult(
            new PlanetHydrologyState(
                hydrology.PlanetId,
                hydrology.GridDefinition,
                nextCells),
            totalSpillMass);
    }

    private static double SolveWaterSurfaceElevation(
        IEnumerable<SurfaceCellId> retainedCellIds,
        double targetVolumeCubicMeters,
        IReadOnlyDictionary<
            SurfaceCellId,
            TerrainCellState> terrainByCellId,
        IReadOnlyDictionary<
            SurfaceCellId,
            SurfaceCell> surfaceCellsById)
    {
        if (!double.IsFinite(
                targetVolumeCubicMeters) ||
            targetVolumeCubicMeters < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetVolumeCubicMeters));
        }

        var cells =
            retainedCellIds
                .Select(
                    cellId =>
                        new CellGeometry(
                            cellId,
                            terrainByCellId[
                                cellId]
                            .ElevationMeters,
                            surfaceCellsById[
                                cellId]
                            .AreaSquareMeters))
                .OrderBy(
                    cell =>
                        cell.ElevationMeters)
                .ThenBy(
                    cell =>
                        cell.CellId.Value)
                .ToArray();

        if (cells.Length == 0)
        {
            throw new ArgumentException(
                "At least one retained cell is required.",
                nameof(retainedCellIds));
        }

        var activeArea =
            0d;

        var elevationAreaSum =
            0d;

        for (var index = 0;
             index < cells.Length;
             index++)
        {
            var cell =
                cells[index];

            activeArea +=
                cell.AreaSquareMeters;

            elevationAreaSum +=
                cell.ElevationMeters *
                cell.AreaSquareMeters;

            if (index <
                cells.Length - 1)
            {
                var nextElevation =
                    cells[index + 1]
                        .ElevationMeters;

                var volumeAtNextElevation =
                    nextElevation *
                    activeArea -
                    elevationAreaSum;

                if (targetVolumeCubicMeters >
                    volumeAtNextElevation)
                {
                    continue;
                }
            }

            return
                (targetVolumeCubicMeters +
                 elevationAreaSum) /
                activeArea;
        }

        throw new InvalidOperationException(
            "Unable to solve basin water-surface elevation.");
    }

    private sealed record CellGeometry(
        SurfaceCellId CellId,
        double ElevationMeters,
        double AreaSquareMeters);
}

public sealed record BasinEquilibriumResult(
    PlanetHydrologyState Hydrology,
    double SpillMassKilograms);
