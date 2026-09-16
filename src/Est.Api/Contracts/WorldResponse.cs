namespace Est.Api.Contracts;

public sealed record WorldResponse(
    Guid WorldId,
    long CurrentTimeSeconds,
    PlanetResponse[] Planets,
    PopulationPersonResponse[] Population,
    AnimalResponse[] Animals);

public sealed record PopulationPersonResponse(
    Guid PersonId,
    Guid PlanetId,
    string Sex,
    long BirthTimeSeconds,
    double LatitudeDegrees,
    double LongitudeDegrees,
    Guid? ParentId,
    bool IsPregnant,
    long? PregnancyConceptionTimeSeconds,
    Guid? PregnancyFatherId,
    string Activity,
    double EnergyReserve,
    double Health);

public sealed record AnimalResponse(
    Guid AnimalId,
    Guid PlanetId,
    string Species,
    double LatitudeDegrees,
    double LongitudeDegrees,
    double EnergyReserve,
    double Health,
    string Activity);

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
