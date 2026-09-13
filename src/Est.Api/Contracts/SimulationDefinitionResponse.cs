namespace Est.Api.Contracts;

public sealed record SimulationDefinitionResponse(
    PlanetaryEnergyBalanceModelResponse[]
        PlanetaryEnergyBalanceModels,
    PopulationModelResponse[]
        PopulationModels);

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
    double LongMigrationDegrees);
