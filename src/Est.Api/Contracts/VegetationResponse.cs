namespace Est.Api.Contracts;

public sealed record VegetationResponse(
    Guid PlanetId,
    SurfaceGridResponse Grid,
    VegetationCellResponse[] Cells);

public sealed record VegetationCellResponse(
    Guid SurfaceCellId,
    double LiveBiomassKilogramsPerSquareMeter);
