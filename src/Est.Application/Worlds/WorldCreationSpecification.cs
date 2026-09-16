namespace Est.Application.Worlds;

public sealed record WorldCreationSpecification(
    IReadOnlyList<PlanetCreationSpecification> Planets);

public sealed record PlanetCreationSpecification(
    string Name,
    double MassKilograms,
    double MeanRadiusMeters,
    PlanetEnvironmentCreationSpecification Environment,
    SyntheticPopulationCreationSpecification? SyntheticPopulation = null,
    SyntheticAnimalCreationSpecification? SyntheticAnimals = null,
    GeneratedTerrainCreationSpecification? GeneratedTerrain = null,
    GeneratedHydrologyCreationSpecification? GeneratedHydrology = null,
    GeneratedVegetationCreationSpecification? GeneratedVegetation = null,
    GeneratedInvertebrateCreationSpecification? GeneratedInvertebrates = null,
    GeneratedBirdCreationSpecification? GeneratedBirds = null,
    GeneratedGrazerCreationSpecification? GeneratedGrazers = null);

public sealed record GeneratedTerrainCreationSpecification(
    int Seed,
    int LatitudeBandCount = 72,
    int LongitudeBandCount = 144,
    int PlateCount = 24,
    double ContinentalPlateFraction = 0.45);

public sealed record GeneratedHydrologyCreationSpecification(
    double SurfaceLiquidWaterInventoryKilograms);

public sealed record GeneratedVegetationCreationSpecification(
    double InitialLiveBiomassKilogramsPerSquareMeter);

public sealed record GeneratedInvertebrateCreationSpecification(
    double CarryingCapacityKilogramsPerKilogramLiveVegetation = 0.02,
    double InitialFractionOfLocalCarryingCapacity = 0.25);

public sealed record GeneratedBirdCreationSpecification(
    double CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass =
        0.000001,
    double InitialFractionOfLocalCarryingCapacity = 0.25,
    int MinimumInitialFlockMemberCount = 10,
    int MaximumInitialFlockCount = 64);

public sealed record GeneratedGrazerCreationSpecification(
    double CarryingCapacityGrazersPerKilogramLiveVegetationBiomass =
        0.000001,
    double InitialFractionOfLocalCarryingCapacity = 0.25,
    int MinimumInitialCohortMemberCount = 10,
    int MaximumInitialCohortCount = 64);

public sealed record SyntheticPopulationCreationSpecification(
    int FounderCount,
    int Seed,
    double CenterLatitudeDegrees,
    double CenterLongitudeDegrees,
    double SpreadDegrees,
    double MinimumAgeYears = 18,
    double MaximumAgeYears = 35);

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
