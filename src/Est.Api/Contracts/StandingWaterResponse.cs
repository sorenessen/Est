namespace Est.Api.Contracts;

public sealed record StandingWaterResponse(
    Guid PlanetId,
    SurfaceGridResponse Grid,
    StandingWaterCellResponse[] Cells,
    StandingWaterBodyResponse[] WaterBodies);

public sealed record StandingWaterCellResponse(
    Guid CellId,
    string Kind,
    Guid? WaterBodyAnchorCellId,
    double WaterDepthMeters,
    double WaterSurfaceElevationMeters);

public sealed record StandingWaterBodyResponse(
    Guid AnchorCellId,
    string Kind,
    Guid[] CellIds,
    double SurfaceAreaSquareMeters,
    double WaterVolumeCubicMeters);
