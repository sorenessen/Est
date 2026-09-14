namespace Est.Api.Contracts;

public sealed record HydrologyResponse(
    Guid PlanetId,
    SurfaceGridResponse Grid,
    double TotalWaterMassKilograms,
    HydrologyCellResponse[] Cells);

public sealed record SurfaceGridResponse(
    string Kind,
    int IdentityVersion,
    int LatitudeBandCount,
    int LongitudeBandCount);

public sealed record HydrologyCellResponse(
    Guid CellId,
    double AtmosphericWaterKilogramsPerSquareMeter,
    double SurfaceLiquidWaterKilogramsPerSquareMeter,
    double SoilWaterKilogramsPerSquareMeter,
    double SnowIceWaterEquivalentKilogramsPerSquareMeter);
