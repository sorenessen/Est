namespace Est.Api.Contracts;

public sealed record WorldResponse(
    Guid WorldId,
    long CurrentTimeSeconds,
    PlanetResponse[] Planets,
    PopulationPersonResponse[] Population,
    FoodResourceResponse[] FoodResources);

public sealed record PopulationPersonResponse(
    Guid PersonId,
    Guid PlanetId,
    string Sex,
    long BirthTimeSeconds,
    double LatitudeDegrees,
    double LongitudeDegrees,
    Guid? ParentId);

public sealed record FoodResourceResponse(
    Guid FoodResourceId,
    Guid PlanetId,
    double LatitudeDegrees,
    double LongitudeDegrees,
    double AvailableEnergy);

public sealed record PlanetResponse(
    Guid PlanetId,
    string Name,
    double MassKilograms,
    double MeanRadiusMeters,
    double SurfaceGravityMetersPerSecondSquared,
    PlanetEnvironmentResponse Environment);

public sealed record PlanetEnvironmentResponse(
    double MeanSurfaceTemperatureKelvin,
    double SurfaceWaterFraction,
    double IceCoverageFraction,
    AtmosphereResponse Atmosphere);

public sealed record AtmosphereResponse(
    double SurfacePressurePascals,
    IReadOnlyDictionary<string, double> CompositionByMoleFraction);
