namespace Est.Api.Contracts;

public sealed record BirdFlocksResponse(
    Guid PlanetId,
    BirdFlockResponse[] Flocks);

public sealed record BirdFlockResponse(
    Guid FlockId,
    int MemberCount,
    double LatitudeDegrees,
    double LongitudeDegrees,
    OrganismMaterialResponse Material,
    double RecruitmentAccumulator);
