namespace Est.Api.Contracts;

public sealed record ManifestedEsterResponse(
    Guid EsterId,
    Guid PlanetId,
    double LatitudeDegrees,
    double LongitudeDegrees);
