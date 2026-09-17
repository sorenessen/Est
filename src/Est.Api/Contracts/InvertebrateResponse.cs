namespace Est.Api.Contracts;

public sealed record InvertebrateResponse(
    Guid PlanetId,
    SurfaceGridResponse Grid,
    InvertebrateCellResponse[] Cells);

public sealed record InvertebrateCellResponse(
    Guid SurfaceCellId,
    double LiveBiomassKilogramsPerSquareMeter,
    double LiveNitrogenKilogramsPerSquareMeter);
