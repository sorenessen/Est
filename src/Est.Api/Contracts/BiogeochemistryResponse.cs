namespace Est.Api.Contracts;

public sealed record BiogeochemistryResponse(
    Guid PlanetId,
    SurfaceGridResponse Grid,
    BiogeochemistryCellResponse[] Cells);

public sealed record BiogeochemistryCellResponse(
    Guid SurfaceCellId,
    double DetritalBiomassKilogramsPerSquareMeter,
    double DetritalNitrogenKilogramsPerSquareMeter,
    double PlantAvailableNitrogenKilogramsPerSquareMeter);
