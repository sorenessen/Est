namespace Est.Api.Contracts;

public sealed record SurfaceResponse(
    Guid PlanetId,
    SurfaceGridResponse Grid,
    SurfaceCellResponse[] Cells);

public sealed record SurfaceGridResponse(
    string Kind,
    int IdentityVersion,
    int LatitudeBandCount,
    int LongitudeBandCount);

public sealed record SurfaceCellResponse(
    Guid CellId,
    double CenterLatitudeDegrees,
    double CenterLongitudeDegrees,
    double AreaSquareMeters,
    SurfaceCoordinateResponse[] Boundary);

public sealed record SurfaceCoordinateResponse(
    double LatitudeDegrees,
    double LongitudeDegrees);
