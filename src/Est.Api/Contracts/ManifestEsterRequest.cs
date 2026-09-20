namespace Est.Api.Contracts;

public sealed record ManifestEsterRequest(
    Guid PlanetId,
    double LatitudeDegrees,
    double LongitudeDegrees);
