using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;

namespace Est.Simulation.Hydrology;

/// <summary>
/// Builds the initial authoritative hydrology field from a planet-level
/// surface-liquid water inventory and solid terrain.
///
/// The initialization solves one equilibrium water-surface elevation L such
/// that:
///
///   sum(max(0, L - terrainElevation) * cellArea * waterDensity)
///
/// equals the requested water inventory.
///
/// Terrain elevation remains relative to the mean-radius datum. The solved
/// water level is therefore an emergent consequence of terrain geometry and
/// water inventory rather than an assumed zero-elevation sea level.
/// </summary>
public static class PlanetHydrologyInitializer
{
    public static PlanetHydrologyState
        FromSurfaceLiquidWaterInventory(
            PlanetState planet,
            PlanetTerrainState terrain,
            double surfaceLiquidWaterInventoryKilograms)
    {
        ArgumentNullException.ThrowIfNull(
            planet);

        ArgumentNullException.ThrowIfNull(
            terrain);

        if (!double.IsFinite(
                surfaceLiquidWaterInventoryKilograms) ||
            surfaceLiquidWaterInventoryKilograms < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    surfaceLiquidWaterInventoryKilograms),
                "Surface-liquid water inventory must be finite and nonnegative.");
        }

        terrain.ValidateFor(
            planet);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                terrain.GridDefinition);

        var terrainById =
            terrain.Cells.ToDictionary(
                cell =>
                    cell.CellId);

        var geometry =
            grid.Cells
                .Select(
                    cell =>
                        new CellGeometry(
                            cell.Id,
                            terrainById[
                                cell.Id]
                                .ElevationMeters,
                            cell.AreaSquareMeters))
                .OrderBy(
                    cell =>
                        cell.ElevationMeters)
                .ThenBy(
                    cell =>
                        cell.CellId.Value)
                .ToArray();

        if (surfaceLiquidWaterInventoryKilograms ==
            0)
        {
            return new PlanetHydrologyState(
                planet.Id,
                terrain.GridDefinition,
                grid.Cells.Select(
                    cell =>
                        new HydrologyCellState(
                            cell.Id,
                            atmosphericWaterKilogramsPerSquareMeter:
                                0,
                            surfaceLiquidWaterKilogramsPerSquareMeter:
                                0,
                            soilWaterKilogramsPerSquareMeter:
                                0,
                            snowIceWaterEquivalentKilogramsPerSquareMeter:
                                0)));
        }

        var targetVolumeCubicMeters =
            surfaceLiquidWaterInventoryKilograms /
            PlanetStandingWaterState
                .LiquidWaterDensityKilogramsPerCubicMeter;

        var cumulativeArea = 0d;
        var cumulativeAreaElevation = 0d;
        var waterSurfaceElevationMeters =
            double.NaN;

        for (var index = 0;
             index < geometry.Length;
             index++)
        {
            var cell =
                geometry[index];

            cumulativeArea +=
                cell.AreaSquareMeters;

            cumulativeAreaElevation +=
                cell.AreaSquareMeters *
                cell.ElevationMeters;

            var candidateLevel =
                (targetVolumeCubicMeters +
                 cumulativeAreaElevation) /
                cumulativeArea;

            var nextElevation =
                index + 1 <
                geometry.Length
                    ? geometry[index + 1]
                        .ElevationMeters
                    : double.PositiveInfinity;

            if (candidateLevel <=
                nextElevation)
            {
                waterSurfaceElevationMeters =
                    candidateLevel;

                break;
            }
        }

        if (!double.IsFinite(
                waterSurfaceElevationMeters))
        {
            throw new InvalidOperationException(
                "A finite equilibrium water-surface elevation could not be solved.");
        }

        var hydrologyCells =
            new HydrologyCellState[
                grid.CellCount];

        for (var index = 0;
             index < grid.Cells.Count;
             index++)
        {
            var surfaceCell =
                grid.Cells[index];

            var terrainElevation =
                terrainById[
                    surfaceCell.Id]
                    .ElevationMeters;

            var waterDepthMeters =
                Math.Max(
                    0,
                    waterSurfaceElevationMeters -
                    terrainElevation);

            var surfaceWaterKilogramsPerSquareMeter =
                waterDepthMeters *
                PlanetStandingWaterState
                    .LiquidWaterDensityKilogramsPerCubicMeter;

            hydrologyCells[index] =
                new HydrologyCellState(
                    surfaceCell.Id,
                    atmosphericWaterKilogramsPerSquareMeter:
                        0,
                    surfaceLiquidWaterKilogramsPerSquareMeter:
                        surfaceWaterKilogramsPerSquareMeter,
                    soilWaterKilogramsPerSquareMeter:
                        0,
                    snowIceWaterEquivalentKilogramsPerSquareMeter:
                        0);
        }

        return new PlanetHydrologyState(
            planet.Id,
            terrain.GridDefinition,
            hydrologyCells);
    }

    private sealed record CellGeometry(
        SurfaceCellId CellId,
        double ElevationMeters,
        double AreaSquareMeters);
}
