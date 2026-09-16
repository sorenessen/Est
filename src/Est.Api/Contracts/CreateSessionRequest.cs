using System.Text.Json.Serialization;

namespace Est.Api.Contracts;

public sealed record CreateSessionRequest(
    [property: JsonRequired]
    PlanetCreationRequest[] Planets);

public sealed record PlanetCreationRequest(
    [property: JsonRequired]
    string Name,
    [property: JsonRequired]
    double MassKilograms,
    [property: JsonRequired]
    double MeanRadiusMeters,
    [property: JsonRequired]
    PlanetEnvironmentCreationRequest Environment,
    PlanetaryEnergyBalanceModelRequest? EnergyBalanceModel = null,
    SyntheticPopulationCreationRequest? SyntheticPopulation = null,
    SyntheticAnimalCreationRequest? SyntheticAnimals = null,
    GeneratedTerrainCreationRequest? GeneratedTerrain = null,
    GeneratedHydrologyCreationRequest? GeneratedHydrology = null,
    HydrologyModelRequest? HydrologyModel = null,
    GeneratedVegetationCreationRequest? GeneratedVegetation = null,
    VegetationModelRequest? VegetationModel = null,
    GeneratedInvertebrateCreationRequest? GeneratedInvertebrates = null,
    InvertebrateModelRequest? InvertebrateModel = null,
    GeneratedBirdCreationRequest? GeneratedBirds = null,
    BirdModelRequest? BirdModel = null,
    GeneratedGrazerCreationRequest? GeneratedGrazers = null,
    GrazerModelRequest? GrazerModel = null,
    GeneratedBiogeochemistryCreationRequest? GeneratedBiogeochemistry = null,
    BiogeochemistryModelRequest? BiogeochemistryModel = null);

public sealed record GeneratedTerrainCreationRequest(
    [property: JsonRequired]
    int Seed,
    int LatitudeBandCount = 72,
    int LongitudeBandCount = 144,
    int PlateCount = 24,
    double ContinentalPlateFraction = 0.45);

public sealed record GeneratedHydrologyCreationRequest(
    [property: JsonRequired]
    double SurfaceLiquidWaterInventoryKilograms);

public sealed record GeneratedVegetationCreationRequest(
    [property: JsonRequired]
    double InitialLiveBiomassKilogramsPerSquareMeter);

public sealed record GeneratedInvertebrateCreationRequest(
    double CarryingCapacityKilogramsPerKilogramLiveVegetation = 0.02,
    double InitialFractionOfLocalCarryingCapacity = 0.25);

public sealed record GeneratedBirdCreationRequest(
    double CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass =
        0.000001,
    double InitialFractionOfLocalCarryingCapacity = 0.25,
    int MinimumInitialFlockMemberCount = 10,
    int MaximumInitialFlockCount = 64);

public sealed record GeneratedGrazerCreationRequest(
    double CarryingCapacityGrazersPerKilogramLiveVegetationBiomass =
        0.000001,
    double InitialFractionOfLocalCarryingCapacity = 0.25,
    int MinimumInitialCohortMemberCount = 10,
    int MaximumInitialCohortCount = 64);

public sealed record GeneratedBiogeochemistryCreationRequest(
    double InitialDetritalBiomassKilogramsPerSquareMeter = 0,
    double InitialDetritalNitrogenKilogramsPerSquareMeter = 0,
    double InitialPlantAvailableNitrogenKilogramsPerSquareMeter = 0);

public sealed record BirdModelRequest
{
    public double
        CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass
    {
        get;
        init;
    } = 0.000001;

    public double InitialFractionOfLocalCarryingCapacity
    {
        get;
        init;
    } = 0.25;

    public int MinimumInitialFlockMemberCount { get; init; } =
        10;

    public int MaximumInitialFlockCount { get; init; } =
        64;

    public long MaximumIntegrationStepSeconds { get; init; } =
        21_600;

    public double MaximumTravelMetersPerDay { get; init; } =
        250_000;

    public double FoodShortageMortalityRatePerDay { get; init; } =
        0.05;

    public double WaterAbsenceMortalityRatePerDay { get; init; } =
        0.20;

    public double HabitatAbsenceMortalityRatePerDay { get; init; } =
        0.02;
}

public sealed record GrazerModelRequest
{
    public double
        CarryingCapacityGrazersPerKilogramLiveVegetationBiomass
    {
        get;
        init;
    } = 0.000001;

    public double InitialFractionOfLocalCarryingCapacity
    {
        get;
        init;
    } = 0.25;

    public int MinimumInitialCohortMemberCount { get; init; } =
        10;

    public int MaximumInitialCohortCount { get; init; } =
        64;

    public long MaximumIntegrationStepSeconds { get; init; } =
        21_600;

    public double MaximumTravelMetersPerDay { get; init; } =
        50_000;

    public double MaximumGrazeKilogramsPerGrazerPerDay
    {
        get;
        init;
    } = 10;

    public double FoodShortageMortalityRatePerDay { get; init; } =
        0.05;

    public double WaterAbsenceMortalityRatePerDay { get; init; } =
        0.20;

    public double HabitatAbsenceMortalityRatePerDay { get; init; } =
        0.02;
}

public sealed record InvertebrateModelRequest
{
    public long MaximumIntegrationStepSeconds { get; init; } =
        21_600;

    public double
        CarryingCapacityKilogramsPerKilogramLiveVegetation
    {
        get;
        init;
    } = 0.02;

    public double InitialFractionOfLocalCarryingCapacity
    {
        get;
        init;
    } = 0.25;

    public double MaximumRelativeGrowthRatePerDay
    {
        get;
        init;
    } = 0.10;

    public double BaselineMortalityRatePerDay
    {
        get;
        init;
    } = 0.02;
}

public sealed record VegetationModelRequest
{
    public long MaximumIntegrationStepSeconds { get; init; } =
        21_600;

    public double CarryingCapacityKilogramsPerSquareMeter
    {
        get;
        init;
    } = 5;

    public double MaximumRelativeGrowthRatePerDay
    {
        get;
        init;
    } = 0.10;

    public double
        SoilWaterForFullProductivityKilogramsPerSquareMeter
    {
        get;
        init;
    } = 50;

    public double MinimumGrowthTemperatureKelvin
    {
        get;
        init;
    } = 273.15;

    public double OptimumGrowthTemperatureKelvin
    {
        get;
        init;
    } = 293.15;

    public double MaximumGrowthTemperatureKelvin
    {
        get;
        init;
    } = 313.15;

    public double TemperatureLapseRateKelvinPerMeter
    {
        get;
        init;
    } = 0.0065;

    public double? PlantNitrogenKilogramsPerKilogramLiveBiomass
    {
        get;
        init;
    }

    public double BaselineMortalityRatePerDay
    {
        get;
        init;
    } = 0;
}

public sealed record BiogeochemistryModelRequest
{
    public long MaximumIntegrationStepSeconds { get; init; } =
        21_600;

    public double MaximumRelativeDecompositionRatePerDay
    {
        get;
        init;
    } = 0.05;

    public double
        SoilWaterForFullDecompositionKilogramsPerSquareMeter
    {
        get;
        init;
    } = 50;

    public double MinimumDecompositionTemperatureKelvin
    {
        get;
        init;
    } = 263.15;

    public double OptimumDecompositionTemperatureKelvin
    {
        get;
        init;
    } = 293.15;

    public double MaximumDecompositionTemperatureKelvin
    {
        get;
        init;
    } = 313.15;

    public double TemperatureLapseRateKelvinPerMeter
    {
        get;
        init;
    } = 0.0065;
}

public sealed record HydrologyModelRequest
{
    public long MaximumIntegrationStepSeconds { get; init; } =
        21_600;

    public double
        MaximumEvaporationRateKilogramsPerSquareMeterPerDay
    {
        get;
        init;
    } = 4;

    public double
        AtmosphericPrecipitationThresholdKilogramsPerSquareMeter
    {
        get;
        init;
    } = 20;

    public double
        MaximumPrecipitationRateKilogramsPerSquareMeterPerDay
    {
        get;
        init;
    } = 12;

    public double
        SoilWaterCapacityKilogramsPerSquareMeter
    {
        get;
        init;
    } = 150;

    public double
        MaximumInfiltrationRateKilogramsPerSquareMeterPerDay
    {
        get;
        init;
    } = 20;

    public double
        MaximumRunoffRateKilogramsPerSquareMeterPerDay
    {
        get;
        init;
    } = 25;

    public double FreezingTemperatureKelvin { get; init; } =
        273.15;

    public double MeltingTemperatureKelvin { get; init; } =
        273.15;

    public double
        MaximumFreezingRateKilogramsPerSquareMeterPerDay
    {
        get;
        init;
    } = 20;

    public double
        MaximumMeltingRateKilogramsPerSquareMeterPerDay
    {
        get;
        init;
    } = 20;
}

public sealed record SyntheticPopulationCreationRequest(
    [property: JsonRequired]
    int FounderCount,
    [property: JsonRequired]
    int Seed,
    [property: JsonRequired]
    double CenterLatitudeDegrees,
    [property: JsonRequired]
    double CenterLongitudeDegrees,
    [property: JsonRequired]
    double SpreadDegrees,
    double MinimumAgeYears = 18,
    double MaximumAgeYears = 35,
    VegetationForagingRequest? VegetationForaging = null);

public sealed record VegetationForagingRequest(
    [property: JsonRequired]
    double KilogramsLiveBiomassPerEnergyReserveUnit,
    [property: JsonRequired]
    double MaximumHarvestKilogramsPerPersonPerDay);

public sealed record SyntheticAnimalCreationRequest(
    [property: JsonRequired]
    int WolfCount,
    [property: JsonRequired]
    int Seed,
    [property: JsonRequired]
    double CenterLatitudeDegrees,
    [property: JsonRequired]
    double CenterLongitudeDegrees,
    [property: JsonRequired]
    double SpreadDegrees);

public sealed record PlanetaryEnergyBalanceModelRequest(
    [property: JsonRequired]
    double StellarFluxWattsPerSquareMeter,
    [property: JsonRequired]
    double EffectiveLongwaveEmissivity,
    [property: JsonRequired]
    double EffectiveHeatCapacityJoulesPerSquareMeterKelvin,
    [property: JsonRequired]
    double IceFreeAlbedo,
    [property: JsonRequired]
    double IceAlbedo,
    [property: JsonRequired]
    double FullIceTemperatureKelvin,
    [property: JsonRequired]
    double IceFreeTemperatureKelvin,
    [property: JsonRequired]
    double IceResponseTimescaleSeconds);

public sealed record PlanetEnvironmentCreationRequest(
    [property: JsonRequired]
    double MeanSurfaceTemperatureKelvin,
    [property: JsonRequired]
    double SurfaceWaterFraction,
    [property: JsonRequired]
    double IceCoverageFraction,
    [property: JsonRequired]
    AtmosphereCreationRequest Atmosphere);

public sealed record AtmosphereCreationRequest(
    [property: JsonRequired]
    double SurfacePressurePascals,
    [property: JsonRequired]
    IReadOnlyDictionary<string, double> CompositionByMoleFraction);
