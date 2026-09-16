using System.Collections.Immutable;
using Est.Simulation.Planets;
using Est.Simulation.Surface;

namespace Est.Simulation.Biogeochemistry;

/// <summary>
/// Durable authoritative biogeochemical state for one planet.
///
/// State shares the authoritative simulation surface grid used by terrain,
/// hydrology, vegetation, and other spatial biosphere fields.
/// </summary>
public sealed record PlanetBiogeochemistryState
{
    public PlanetBiogeochemistryState(
        PlanetId planetId,
        SurfaceGridDefinition gridDefinition,
        IEnumerable<BiogeochemistryCellState> cells)
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
                "Biogeochemistry cells cannot contain null entries.",
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
                "Biogeochemistry cannot contain duplicate surface-cell identities.",
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

    public ImmutableArray<BiogeochemistryCellState> Cells { get; }

    public void ValidateFor(
        PlanetState planet)
    {
        ArgumentNullException.ThrowIfNull(
            planet);

        if (planet.Id !=
            PlanetId)
        {
            throw new ArgumentException(
                "Biogeochemistry belongs to a different planet.",
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
                "Biogeochemistry must contain exactly one state for every surface cell.",
                nameof(planet));
        }

        var biogeochemistryCellIds =
            Cells
                .Select(
                    cell =>
                        cell.CellId)
                .ToHashSet();

        if (grid.Cells.Any(
                cell =>
                    !biogeochemistryCellIds.Contains(
                        cell.Id)))
        {
            throw new ArgumentException(
                "Biogeochemistry contains cells that do not match its surface-grid definition.",
                nameof(planet));
        }
    }

    public BiogeochemistryCellState GetCell(
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
            $"Biogeochemistry does not contain surface cell {cellId.Value}.");
    }
}
