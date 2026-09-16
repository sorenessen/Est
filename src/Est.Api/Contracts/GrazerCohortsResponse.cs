namespace Est.Api.Contracts;

public sealed record GrazerCohortsResponse(
    Guid PlanetId,
    GrazerCohortResponse[] Cohorts);

public sealed record GrazerCohortResponse(
    Guid CohortId,
    int MemberCount,
    double LatitudeDegrees,
    double LongitudeDegrees,
    OrganismMaterialResponse Material);
