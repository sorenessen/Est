using Est.Simulation.Planets;

namespace Est.Simulation.Surface;

public interface IPlanetSurfaceGrid
{
    PlanetId PlanetId { get; }

    int CellCount { get; }

    double TotalSurfaceAreaSquareMeters { get; }

    IReadOnlyList<SurfaceCell> Cells { get; }

    SurfaceCell GetCell(SurfaceCellId cellId);

    SurfaceCell LocateCell(
        double latitudeDegrees,
        double longitudeDegrees);

    IReadOnlyList<SurfaceCellId> GetNeighbors(
        SurfaceCellId cellId);
}
