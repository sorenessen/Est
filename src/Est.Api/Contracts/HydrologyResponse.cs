namespace Est.Api.Contracts;

public sealed record HydrologyResponse(
    Guid PlanetId,
    SurfaceGridResponse Grid,
    double TotalWaterMassKilograms,
    HydrologyCellResponse[] Cells);

public sealed record HydrologyCellResponse(
    Guid CellId,
    double AtmosphericWaterKilogramsPerSquareMeter,
    double SurfaceLiquidWaterKilogramsPerSquareMeter,
    double SoilWaterKilogramsPerSquareMeter,
    double SnowIceWaterEquivalentKilogramsPerSquareMeter);
