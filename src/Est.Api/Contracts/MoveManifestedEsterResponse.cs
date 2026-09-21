namespace Est.Api.Contracts;

public sealed record MoveManifestedEsterResponse(
    Guid EsterId,
    Guid PlanetId,
    double LatitudeDegrees,
    double LongitudeDegrees,
    Guid? SurfaceCellId,
    IReadOnlyList<PersonEncounterResponse> Encounters);
