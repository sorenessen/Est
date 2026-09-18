namespace Est.Api.Contracts;

public sealed record WorldResponse(
    Guid WorldId,
    long CurrentTimeSeconds,
    PlanetResponse[] Planets,
    PopulationPersonResponse[] Population,
    AnimalResponse[] Animals,
    SeasonalStateResponse[] SeasonalStates);

public sealed record SeasonalStateResponse(
    Guid PlanetId,
    string ControlMode,
    SeasonalContextResponse? DerivedContext,
    SeasonalContextResponse? OverrideContext,
    SeasonalContextResponse? EffectiveContext);

public sealed record SeasonalContextResponse(
    string PhaseId,
    double? CycleFraction);

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
    double Health,
    OrganismMaterialResponse Material);

public sealed record AnimalResponse(
    Guid AnimalId,
    Guid PlanetId,
    string Species,
    long BirthTimeSeconds,
    Guid? ParentId,
    string? Sex,
    bool IsPregnant,
    long? PregnancyConceptionTimeSeconds,
    Guid? PregnancyFatherId,
    double LatitudeDegrees,
    double LongitudeDegrees,
    double EnergyReserve,
    double Health,
    string Activity,
    OrganismMaterialResponse Material);

public sealed record OrganismMaterialResponse(
    double LiveBiomassKilograms,
    double LiveNitrogenKilograms);

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
