namespace Est.Api.Contracts;

public sealed record SimulationDefinitionResponse(
    PlanetaryEnergyBalanceModelResponse[]
        PlanetaryEnergyBalanceModels,
    PopulationModelResponse[]
        PopulationModels,
    VegetationModelResponse[]
        VegetationModels,
    InvertebrateModelResponse[]
        InvertebrateModels);

public sealed record PlanetaryEnergyBalanceModelResponse(
    Guid PlanetId,
    double StellarFluxWattsPerSquareMeter,
    double EffectiveLongwaveEmissivity,
    double EffectiveHeatCapacityJoulesPerSquareMeterKelvin,
    double IceFreeAlbedo,
    double IceAlbedo,
    double FullIceTemperatureKelvin,
    double IceFreeTemperatureKelvin,
    double IceResponseTimescaleSeconds);


public sealed record PopulationModelResponse(
    Guid PlanetId,
    int Seed,
    double AnnualBirthRatePerEligibleFemale,
    double AnnualAdultMigrationRate,
    double AnnualBaseMortalityRate,
    double AnnualElderMortalityRate,
    double ReproductiveAgeMinimumYears,
    double ReproductiveAgeMaximumYears,
    double ElderAgeYears,
    double LocalMigrationDegrees,
    double LongMigrationProbability,
    double LongMigrationDegrees,
    VegetationForagingResponse? VegetationForaging);

public sealed record VegetationForagingResponse(
    double KilogramsLiveBiomassPerEnergyReserveUnit,
    double MaximumHarvestKilogramsPerPersonPerDay);


public sealed record VegetationModelResponse(
    Guid PlanetId,
    long MaximumIntegrationStepSeconds,
    double CarryingCapacityKilogramsPerSquareMeter,
    double MaximumRelativeGrowthRatePerDay,
    double SoilWaterForFullProductivityKilogramsPerSquareMeter,
    double MinimumGrowthTemperatureKelvin,
    double OptimumGrowthTemperatureKelvin,
    double MaximumGrowthTemperatureKelvin,
    double TemperatureLapseRateKelvinPerMeter);


public sealed record InvertebrateModelResponse(
    Guid PlanetId,
    long MaximumIntegrationStepSeconds,
    double CarryingCapacityKilogramsPerKilogramLiveVegetation,
    double InitialFractionOfLocalCarryingCapacity,
    double MaximumRelativeGrowthRatePerDay,
    double BaselineMortalityRatePerDay);
