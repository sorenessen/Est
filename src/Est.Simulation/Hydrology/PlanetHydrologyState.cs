using System.Collections.Immutable;
using Est.Simulation.Planets;
using Est.Simulation.Surface;

namespace Est.Simulation.Hydrology;

/// <summary>
/// Durable authoritative water state for one planet.
///
/// The grid definition is preserved with the state so opaque surface-cell
/// identities remain reconstructable after snapshots and timeline loads.
/// </summary>
public sealed record PlanetHydrologyState
{
    public PlanetHydrologyState(
        PlanetId planetId,
        SurfaceGridDefinition gridDefinition,
        IEnumerable<HydrologyCellState> cells)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(
            gridDefinition);

        ArgumentNullException.ThrowIfNull(
            cells);

        var cellArray =
            cells.ToImmutableArray();

        if (cellArray.Any(
                cell =>
                    cell is null))
        {
            throw new ArgumentException(
                "Hydrology cells cannot contain null entries.",
                nameof(cells));
        }

        if (cellArray
            .GroupBy(
                cell =>
                    cell.CellId)
            .Any(
                group =>
                    group.Count() > 1))
        {
            throw new ArgumentException(
                "Hydrology cannot contain duplicate surface-cell identities.",
                nameof(cells));
        }

        PlanetId = planetId;
        GridDefinition = gridDefinition;
        Cells = cellArray;
    }

    public PlanetId PlanetId { get; }

    public SurfaceGridDefinition GridDefinition { get; }

    public ImmutableArray<HydrologyCellState> Cells { get; }

    public void ValidateFor(
        PlanetState planet)
    {
        ArgumentNullException.ThrowIfNull(
            planet);

        if (planet.Id != PlanetId)
        {
            throw new ArgumentException(
                "Hydrology belongs to a different planet.",
                nameof(planet));
        }

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                GridDefinition);

        if (Cells.Length !=
            grid.CellCount)
        {
            throw new ArgumentException(
                "Hydrology must contain exactly one state for every surface cell.",
                nameof(planet));
        }

        var hydrologyCellIds =
            Cells
                .Select(
                    cell =>
                        cell.CellId)
                .ToHashSet();

        if (grid.Cells.Any(
                cell =>
                    !hydrologyCellIds.Contains(
                        cell.Id)))
        {
            throw new ArgumentException(
                "Hydrology contains cells that do not match its surface-grid definition.",
                nameof(planet));
        }
    }

    public HydrologyCellState GetCell(
        SurfaceCellId cellId)
    {
        foreach (var cell in Cells)
        {
            if (cell.CellId ==
                cellId)
            {
                return cell;
            }
        }

        throw new KeyNotFoundException(
            $"Hydrology does not contain surface cell {cellId.Value}.");
    }

    public double TotalWaterMassKilograms(
        PlanetState planet)
    {
        ValidateFor(
            planet);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                GridDefinition);

        var cellsById =
            Cells.ToDictionary(
                cell =>
                    cell.CellId);

        var total = 0d;

        foreach (var surfaceCell in
                 grid.Cells)
        {
            total +=
                cellsById[
                    surfaceCell.Id]
                .TotalWaterKilogramsPerSquareMeter *
                surfaceCell.AreaSquareMeters;
        }

        return total;
    }
}
