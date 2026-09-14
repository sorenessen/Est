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
    SyntheticFoodCreationRequest? SyntheticFood = null,
    SyntheticAnimalCreationRequest? SyntheticAnimals = null,
    GeneratedTerrainCreationRequest? GeneratedTerrain = null,
    GeneratedHydrologyCreationRequest? GeneratedHydrology = null,
    HydrologyModelRequest? HydrologyModel = null);

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
    double MaximumAgeYears = 35);

public sealed record SyntheticFoodCreationRequest(
    [property: JsonRequired]
    int PatchCount,
    [property: JsonRequired]
    int Seed,
    [property: JsonRequired]
    double CenterLatitudeDegrees,
    [property: JsonRequired]
    double CenterLongitudeDegrees,
    [property: JsonRequired]
    double SpreadDegrees,
    [property: JsonRequired]
    double EnergyPerPatch,
    double RecoveryEnergyPerDay = 0);

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
