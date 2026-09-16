using System.Collections.Immutable;
using Est.Simulation.Planets;
using Est.Simulation.Surface;

namespace Est.Simulation.Invertebrates;

/// <summary>
/// Durable authoritative aggregate invertebrate state for one planet.
///
/// The surface-grid definition is preserved so opaque cell identities remain
/// reconstructable across world copies, snapshots, and timeline loads.
/// </summary>
public sealed record PlanetInvertebrateState
{
    public PlanetInvertebrateState(
        PlanetId planetId,
        SurfaceGridDefinition gridDefinition,
        IEnumerable<InvertebrateCellState> cells)
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
                "Invertebrate cells cannot contain null entries.",
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
                "Invertebrates cannot contain duplicate surface-cell identities.",
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

    public ImmutableArray<InvertebrateCellState> Cells { get; }

    public void ValidateFor(
        PlanetState planet)
    {
        ArgumentNullException.ThrowIfNull(
            planet);

        if (planet.Id !=
            PlanetId)
        {
            throw new ArgumentException(
                "Invertebrates belong to a different planet.",
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
                "Invertebrates must contain exactly one state for every surface cell.",
                nameof(planet));
        }

        var invertebrateCellIds =
            Cells
                .Select(
                    cell =>
                        cell.CellId)
                .ToHashSet();

        if (grid.Cells.Any(
                cell =>
                    !invertebrateCellIds.Contains(
                        cell.Id)))
        {
            throw new ArgumentException(
                "Invertebrates contain cells that do not match their surface-grid definition.",
                nameof(planet));
        }
    }

    public InvertebrateCellState GetCell(
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
            $"Invertebrates do not contain surface cell {cellId.Value}.");
    }
}
