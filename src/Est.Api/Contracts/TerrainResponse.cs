namespace Est.Api.Contracts;

public sealed record TerrainResponse(
    Guid PlanetId,
    SurfaceGridResponse Grid,
    TerrainCellResponse[] Cells);

public sealed record TerrainCellResponse(
    Guid CellId,
    double ElevationMeters);
