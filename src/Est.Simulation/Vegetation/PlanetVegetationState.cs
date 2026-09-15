using System.Collections.Immutable;
using Est.Simulation.Planets;
using Est.Simulation.Surface;

namespace Est.Simulation.Vegetation;

/// <summary>
/// Durable authoritative vegetation state for one planet.
///
/// The grid definition is preserved with the state so opaque surface-cell
/// identities remain reconstructable after snapshots and timeline loads.
/// </summary>
public sealed record PlanetVegetationState
{
    public PlanetVegetationState(
        PlanetId planetId,
        SurfaceGridDefinition gridDefinition,
        IEnumerable<VegetationCellState> cells)
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
                "Vegetation cells cannot contain null entries.",
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
                "Vegetation cannot contain duplicate surface-cell identities.",
                nameof(cells));
        }

        PlanetId =
            planetId;

        GridDefinition =
            gridDefinition;

        Cells =
            cellArray;
    }

    public PlanetId PlanetId { get; }

    public SurfaceGridDefinition GridDefinition { get; }

    public ImmutableArray<VegetationCellState> Cells { get; }

    public void ValidateFor(
        PlanetState planet)
    {
        ArgumentNullException.ThrowIfNull(
            planet);

        if (planet.Id !=
            PlanetId)
        {
            throw new ArgumentException(
                "Vegetation belongs to a different planet.",
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
                "Vegetation must contain exactly one state for every surface cell.",
                nameof(planet));
        }

        var vegetationCellIds =
            Cells
                .Select(
                    cell =>
                        cell.CellId)
                .ToHashSet();

        if (grid.Cells.Any(
                cell =>
                    !vegetationCellIds.Contains(
                        cell.Id)))
        {
            throw new ArgumentException(
                "Vegetation contains cells that do not match its surface-grid definition.",
                nameof(planet));
        }
    }

    public VegetationCellState GetCell(
        SurfaceCellId cellId)
    {
        foreach (var cell in
                 Cells)
        {
            if (cell.CellId ==
                cellId)
            {
                return cell;
            }
        }

        throw new KeyNotFoundException(
            $"Vegetation does not contain surface cell {cellId.Value}.");
    }
}
