using System.Collections.Immutable;
using Est.Simulation.Planets;
using Est.Simulation.Surface;

namespace Est.Simulation.Thermal;

/// <summary>
/// Durable regional thermal state for one planet.
///
/// The surface-grid definition is preserved with the state so opaque
/// surface-cell identities remain reconstructable through snapshots,
/// checkpoints, replay, and timeline branching.
/// </summary>
public sealed record PlanetRegionalThermalState
{
    public PlanetRegionalThermalState(
        PlanetId planetId,
        SurfaceGridDefinition gridDefinition,
        IEnumerable<RegionalThermalCellState> cells)
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
                "Regional thermal cells cannot contain null entries.",
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
                "Regional thermal state cannot contain duplicate surface-cell identities.",
                nameof(cells));
        }

        PlanetId = planetId;
        GridDefinition = gridDefinition;
        Cells = cellArray;
    }

    public PlanetId PlanetId { get; }

    public SurfaceGridDefinition GridDefinition { get; }

    public ImmutableArray<RegionalThermalCellState> Cells { get; }

    public void ValidateFor(
        PlanetState planet)
    {
        ArgumentNullException.ThrowIfNull(
            planet);

        if (planet.Id != PlanetId)
        {
            throw new ArgumentException(
                "Regional thermal state belongs to a different planet.",
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
                "Regional thermal state must contain exactly one state for every surface cell.",
                nameof(planet));
        }

        var thermalCellIds =
            Cells
                .Select(
                    cell =>
                        cell.CellId)
                .ToHashSet();

        if (grid.Cells.Any(
                cell =>
                    !thermalCellIds.Contains(
                        cell.Id)))
        {
            throw new ArgumentException(
                "Regional thermal state contains cells that do not match its surface-grid definition.",
                nameof(planet));
        }
    }

    public RegionalThermalCellState GetCell(
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
            $"Regional thermal state does not contain surface cell {cellId.Value}.");
    }
}
