namespace Est.Application.Worlds;

public sealed record WorldCreationSpecification(
    IReadOnlyList<PlanetCreationSpecification> Planets);

public sealed record PlanetCreationSpecification(
    string Name,
    double MassKilograms,
    double MeanRadiusMeters,
    PlanetEnvironmentCreationSpecification Environment,
    SyntheticPopulationCreationSpecification? SyntheticPopulation = null,
    SyntheticFoodCreationSpecification? SyntheticFood = null,
    SyntheticAnimalCreationSpecification? SyntheticAnimals = null,
    GeneratedTerrainCreationSpecification? GeneratedTerrain = null,
    GeneratedHydrologyCreationSpecification? GeneratedHydrology = null);

public sealed record GeneratedTerrainCreationSpecification(
    int Seed,
    int LatitudeBandCount = 72,
    int LongitudeBandCount = 144,
    int PlateCount = 24,
    double ContinentalPlateFraction = 0.45);

public sealed record GeneratedHydrologyCreationSpecification(
    double SurfaceLiquidWaterInventoryKilograms);

public sealed record SyntheticPopulationCreationSpecification(
    int FounderCount,
    int Seed,
    double CenterLatitudeDegrees,
    double CenterLongitudeDegrees,
    double SpreadDegrees,
    double MinimumAgeYears = 18,
    double MaximumAgeYears = 35);

public sealed record SyntheticFoodCreationSpecification(
    int PatchCount,
    int Seed,
    double CenterLatitudeDegrees,
    double CenterLongitudeDegrees,
    double SpreadDegrees,
    double EnergyPerPatch,
    double RecoveryEnergyPerDay = 0);

public sealed record SyntheticAnimalCreationSpecification(
    int WolfCount,
    int Seed,
    double CenterLatitudeDegrees,
    double CenterLongitudeDegrees,
    double SpreadDegrees);

public sealed record PlanetEnvironmentCreationSpecification(
    double MeanSurfaceTemperatureKelvin,
    double SurfaceWaterFraction,
    double IceCoverageFraction,
    AtmosphereCreationSpecification Atmosphere);

public sealed record AtmosphereCreationSpecification(
    double SurfacePressurePascals,
    IReadOnlyDictionary<string, double> CompositionByMoleFraction);
