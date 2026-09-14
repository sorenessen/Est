using System.Collections.Immutable;
using Est.Simulation.Planets;
using Est.Simulation.Surface;

namespace Est.Simulation.Terrain;

public sealed record PlanetTerrainState
{
    public PlanetTerrainState(
        PlanetId planetId,
        SurfaceGridDefinition gridDefinition,
        IEnumerable<TerrainCellState> cells)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(gridDefinition);
        ArgumentNullException.ThrowIfNull(cells);

        var cellArray =
            cells.ToImmutableArray();

        if (cellArray.Any(cell => cell is null))
        {
            throw new ArgumentException(
                "Terrain cells cannot contain null entries.",
                nameof(cells));
        }

        if (cellArray
            .GroupBy(cell => cell.CellId)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Terrain cannot contain duplicate surface-cell identities.",
                nameof(cells));
        }

        PlanetId = planetId;
        GridDefinition = gridDefinition;
        Cells = cellArray;
    }

    public PlanetId PlanetId { get; }

    public SurfaceGridDefinition GridDefinition { get; }

    public ImmutableArray<TerrainCellState> Cells { get; }

    public void ValidateFor(
        PlanetState planet)
    {
        ArgumentNullException.ThrowIfNull(planet);

        if (planet.Id != PlanetId)
        {
            throw new ArgumentException(
                "Terrain belongs to a different planet.",
                nameof(planet));
        }

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                GridDefinition);

        if (Cells.Length != grid.CellCount)
        {
            throw new ArgumentException(
                "Terrain must contain exactly one state for every surface cell.",
                nameof(planet));
        }

        var terrainCellIds =
            Cells
                .Select(cell => cell.CellId)
                .ToHashSet();

        if (grid.Cells.Any(
                cell =>
                    !terrainCellIds.Contains(
                        cell.Id)))
        {
            throw new ArgumentException(
                "Terrain contains cells that do not match its surface-grid definition.",
                nameof(planet));
        }
    }

    public TerrainCellState GetCell(
        SurfaceCellId cellId)
    {
        foreach (var cell in Cells)
        {
            if (cell.CellId == cellId)
            {
                return cell;
            }
        }

        throw new KeyNotFoundException(
            $"Terrain does not contain surface cell {cellId.Value}.");
    }
}
