namespace Est.Application.Worlds;

public sealed record WorldCreationSpecification(
    IReadOnlyList<PlanetCreationSpecification> Planets);

public sealed record PlanetCreationSpecification(
    string Name,
    double MassKilograms,
    double MeanRadiusMeters,
    PlanetEnvironmentCreationSpecification Environment,
    SyntheticPopulationCreationSpecification? SyntheticPopulation = null);

public sealed record SyntheticPopulationCreationSpecification(
    int FounderCount,
    int Seed,
    double CenterLatitudeDegrees,
    double CenterLongitudeDegrees,
    double SpreadDegrees,
    double MinimumAgeYears = 18,
    double MaximumAgeYears = 35);

public sealed record PlanetEnvironmentCreationSpecification(
    double MeanSurfaceTemperatureKelvin,
    double SurfaceWaterFraction,
    double IceCoverageFraction,
    AtmosphereCreationSpecification Atmosphere);

public sealed record AtmosphereCreationSpecification(
    double SurfacePressurePascals,
    IReadOnlyDictionary<string, double> CompositionByMoleFraction);
