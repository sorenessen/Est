using Est.Api;
using Est.Api.Contracts;
using Est.Persistence.Archives;
using Est.Persistence.Storage;
using Est.Application.Sessions;
using Est.Application.Worlds;
using Est.Simulation.Biogeochemistry;
using Est.Simulation.Birds;
using Est.Simulation.Climate;
using Est.Simulation.Grazers;
using Est.Simulation.Definitions;
using Est.Simulation.Ecology;
using Est.Simulation.Hydrology;
using Est.Simulation.Invertebrates;
using Est.Simulation.Vegetation;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Seasons;
using Est.Simulation.Social;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Timelines;
using Est.Simulation.Worlds;

var builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<
    SimulationSessionManager>();

builder.Services.AddSingleton<
    TimelineArchiveFileStore>();

builder.Services.AddSingleton<
    SimulationSessionArchiveService>();

builder.Services.AddSingleton(
    new SessionArchiveLocation(
        builder.Configuration["Est:ArchiveDirectory"]
        ?? builder.Configuration["Aion:ArchiveDirectory"]
        ?? Path.Combine(
            builder.Environment.ContentRootPath,
            "archives")));

var app =
    builder.Build();

app.MapGet(
    "/health",
    () => Results.Ok(
        new
        {
            service = "Est.Api",
            status = "healthy"
        }));

app.MapPost(
    "/sessions",
    (
        CreateSessionRequest request,
        SimulationSessionManager manager) =>
    {
        if (request.Planets is null)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Planets collection is required."
                });
        }

        for (var index = 0; index < request.Planets.Length; index++)
        {
            var planet = request.Planets[index];

            if (planet is null)
            {
                return Results.BadRequest(
                    new
                    {
                        error = $"Planets[{index}] is required."
                    });
            }

            if (planet.Environment is null)
            {
                return Results.BadRequest(
                    new
                    {
                        error = $"Planets[{index}].Environment is required."
                    });
            }

            if (planet.Environment.Atmosphere is null)
            {
                return Results.BadRequest(
                    new
                    {
                        error = $"Planets[{index}].Environment.Atmosphere is required."
                    });
            }

            if (planet.Environment.Atmosphere.CompositionByMoleFraction is null)
            {
                return Results.BadRequest(
                    new
                    {
                        error = $"Planets[{index}].Environment.Atmosphere.CompositionByMoleFraction is required."
                    });
            }
        }

        try
        {
            var specification =
                new WorldCreationSpecification(
                    request.Planets
                        .Select(
                            planet =>
                                new PlanetCreationSpecification(
                                    planet.Name,
                                    planet.MassKilograms,
                                    planet.MeanRadiusMeters,
                                    new PlanetEnvironmentCreationSpecification(
                                        planet.Environment
                                            .MeanSurfaceTemperatureKelvin,
                                        planet.Environment
                                            .SurfaceWaterFraction,
                                        planet.Environment
                                            .IceCoverageFraction,
                                        new AtmosphereCreationSpecification(
                                            planet.Environment
                                                .Atmosphere
                                                .SurfacePressurePascals,
                                            planet.Environment
                                                .Atmosphere
                                                .CompositionByMoleFraction)),
                                    planet.SyntheticPopulation is null
                                        ? null
                                        : new SyntheticPopulationCreationSpecification(
                                            planet.SyntheticPopulation
                                                .FounderCount,
                                            planet.SyntheticPopulation
                                                .Seed,
                                            planet.SyntheticPopulation
                                                .CenterLatitudeDegrees,
                                            planet.SyntheticPopulation
                                                .CenterLongitudeDegrees,
                                            planet.SyntheticPopulation
                                                .SpreadDegrees,
                                            planet.SyntheticPopulation
                                                .MinimumAgeYears,
                                            planet.SyntheticPopulation
                                                .MaximumAgeYears,
                                            planet.SyntheticPopulation
                                                .LiveBiomassKilogramsPerPerson,
                                            planet.SyntheticPopulation
                                                .LiveNitrogenKilogramsPerPerson),
                                    planet.SyntheticAnimals is null
                                        ? null
                                        : new SyntheticAnimalCreationSpecification(
                                            planet.SyntheticAnimals
                                                .WolfCount,
                                            planet.SyntheticAnimals
                                                .Seed,
                                            planet.SyntheticAnimals
                                                .CenterLatitudeDegrees,
                                            planet.SyntheticAnimals
                                                .CenterLongitudeDegrees,
                                            planet.SyntheticAnimals
                                                .SpreadDegrees,
                                            planet.SyntheticAnimals
                                                .LiveBiomassKilogramsPerWolf,
                                            planet.SyntheticAnimals
                                                .LiveNitrogenKilogramsPerWolf),
                                    planet.GeneratedTerrain is null
                                        ? null
                                        : new GeneratedTerrainCreationSpecification(
                                            planet.GeneratedTerrain
                                                .Seed,
                                            planet.GeneratedTerrain
                                                .LatitudeBandCount,
                                            planet.GeneratedTerrain
                                                .LongitudeBandCount,
                                            planet.GeneratedTerrain
                                                .PlateCount,
                                            planet.GeneratedTerrain
                                                .ContinentalPlateFraction),
                                    planet.GeneratedHydrology is null
                                        ? null
                                        : new GeneratedHydrologyCreationSpecification(
                                            planet.GeneratedHydrology
                                                .SurfaceLiquidWaterInventoryKilograms),
                                    planet.GeneratedVegetation is null
                                        ? null
                                        : new GeneratedVegetationCreationSpecification(
                                            planet.GeneratedVegetation
                                                .InitialLiveBiomassKilogramsPerSquareMeter),
                                    planet.GeneratedInvertebrates is null
                                        ? null
                                        : new GeneratedInvertebrateCreationSpecification(
                                            planet.GeneratedInvertebrates
                                                .CarryingCapacityKilogramsPerKilogramLiveVegetation,
                                            planet.GeneratedInvertebrates
                                                  .InitialFractionOfLocalCarryingCapacity,
                                              planet.GeneratedInvertebrates
                                                  .LiveNitrogenKilogramsPerKilogramLiveBiomass
                                              ?? planet.InvertebrateModel
                                                  ?.LiveNitrogenKilogramsPerKilogramLiveBiomass
                                              ?? 0),
                                    planet.GeneratedBirds is null
                                        ? null
                                        : new GeneratedBirdCreationSpecification(
                                            planet.GeneratedBirds
                                                .CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass,
                                            planet.GeneratedBirds
                                                .InitialFractionOfLocalCarryingCapacity,
                                            planet.GeneratedBirds
                                                .MinimumInitialFlockMemberCount,
                                            planet.GeneratedBirds
                                                .MaximumInitialFlockCount,
                                            planet.GeneratedBirds
                                                .LiveBiomassKilogramsPerBird,
                                            planet.GeneratedBirds
                                                .LiveNitrogenKilogramsPerBird),
                                    planet.GeneratedGrazers is null
                                        ? null
                                        : new GeneratedGrazerCreationSpecification(
                                            planet.GeneratedGrazers
                                                .CarryingCapacityGrazersPerKilogramLiveVegetationBiomass,
                                            planet.GeneratedGrazers
                                                .InitialFractionOfLocalCarryingCapacity,
                                            planet.GeneratedGrazers
                                                .MinimumInitialCohortMemberCount,
                                            planet.GeneratedGrazers
                                                .MaximumInitialCohortCount,
                                            planet.GeneratedGrazers
                                                .LiveBiomassKilogramsPerGrazer,
                                            planet.GeneratedGrazers
                                                .LiveNitrogenKilogramsPerGrazer),
                                    planet.GeneratedBiogeochemistry is null
                                        ? null
                                        : new GeneratedBiogeochemistryCreationSpecification(
                                            planet.GeneratedBiogeochemistry
                                                .InitialDetritalBiomassKilogramsPerSquareMeter,
                                            planet.GeneratedBiogeochemistry
                                                .InitialDetritalNitrogenKilogramsPerSquareMeter,
                                            planet.GeneratedBiogeochemistry
                                                .InitialPlantAvailableNitrogenKilogramsPerSquareMeter)))
                        .ToArray());

            var world =
                WorldFactory.Create(specification);

            var energyBalanceModels =
                request.Planets
                    .Select(
                        (planet, index) =>
                            planet.EnergyBalanceModel is null
                                ? null
                                : new PlanetaryEnergyBalanceModelDefinition(
                                    world.Planets[index].Id,
                                    new PlanetaryEnergyBalanceParameters(
                                        planet.EnergyBalanceModel
                                            .StellarFluxWattsPerSquareMeter,
                                        planet.EnergyBalanceModel
                                            .EffectiveLongwaveEmissivity,
                                        planet.EnergyBalanceModel
                                            .EffectiveHeatCapacityJoulesPerSquareMeterKelvin,
                                        planet.EnergyBalanceModel
                                            .IceFreeAlbedo,
                                        planet.EnergyBalanceModel
                                            .IceAlbedo,
                                        planet.EnergyBalanceModel
                                            .FullIceTemperatureKelvin,
                                        planet.EnergyBalanceModel
                                            .IceFreeTemperatureKelvin,
                                        planet.EnergyBalanceModel
                                            .IceResponseTimescaleSeconds)))
                    .Where(model => model is not null)
                    .Cast<PlanetaryEnergyBalanceModelDefinition>()
                    .ToArray();

            var populationModels =
                request.Planets
                    .Select(
                        (planet, index) =>
                            planet.SyntheticPopulation is null
                                ? null
                                : new PopulationModelDefinition(
                                    world.Planets[index].Id,
                                    new PopulationModelParameters(
                                        seed:
                                            planet.SyntheticPopulation
                                                .Seed),
                                    vegetationForaging:
                                        planet.SyntheticPopulation
                                            .VegetationForaging is null
                                            ? null
                                            : new VegetationForagingParameters(
                                                planet.SyntheticPopulation
                                                    .VegetationForaging
                                                    .KilogramsLiveBiomassPerEnergyReserveUnit,
                                                planet.SyntheticPopulation
                                                    .VegetationForaging
                                                    .MaximumHarvestKilogramsPerPersonPerDay)))
                    .Where(model => model is not null)
                    .Cast<PopulationModelDefinition>()
                    .ToArray();

            var hydrologyModels =
                request.Planets
                    .Select(
                        (planet, index) =>
                            planet.HydrologyModel is null
                                ? null
                                : new HydrologyModelDefinition(
                                    world.Planets[index].Id,
                                    new HydrologyModelParameters(
                                        planet.HydrologyModel
                                            .MaximumIntegrationStepSeconds,
                                        planet.HydrologyModel
                                            .MaximumEvaporationRateKilogramsPerSquareMeterPerDay,
                                        planet.HydrologyModel
                                            .AtmosphericPrecipitationThresholdKilogramsPerSquareMeter,
                                        planet.HydrologyModel
                                            .MaximumPrecipitationRateKilogramsPerSquareMeterPerDay,
                                        planet.HydrologyModel
                                            .SoilWaterCapacityKilogramsPerSquareMeter,
                                        planet.HydrologyModel
                                            .MaximumInfiltrationRateKilogramsPerSquareMeterPerDay,
                                        planet.HydrologyModel
                                            .MaximumRunoffRateKilogramsPerSquareMeterPerDay,
                                        planet.HydrologyModel
                                            .FreezingTemperatureKelvin,
                                        planet.HydrologyModel
                                            .MeltingTemperatureKelvin,
                                        planet.HydrologyModel
                                            .MaximumFreezingRateKilogramsPerSquareMeterPerDay,
                                        planet.HydrologyModel
                                            .MaximumMeltingRateKilogramsPerSquareMeterPerDay)))
                    .Where(model => model is not null)
                    .Cast<HydrologyModelDefinition>()
                    .ToArray();

            var vegetationModels =
                request.Planets
                    .Select(
                        (planet, index) =>
                            planet.VegetationModel is null
                                ? null
                                : new VegetationModelDefinition(
                                    world.Planets[index].Id,
                                    new VegetationModelParameters(
                                        planet.VegetationModel
                                            .MaximumIntegrationStepSeconds,
                                        planet.VegetationModel
                                            .CarryingCapacityKilogramsPerSquareMeter,
                                        planet.VegetationModel
                                            .MaximumRelativeGrowthRatePerDay,
                                        planet.VegetationModel
                                            .SoilWaterForFullProductivityKilogramsPerSquareMeter,
                                        planet.VegetationModel
                                            .MinimumGrowthTemperatureKelvin,
                                        planet.VegetationModel
                                            .OptimumGrowthTemperatureKelvin,
                                        planet.VegetationModel
                                            .MaximumGrowthTemperatureKelvin,
                                        planet.VegetationModel
                                            .TemperatureLapseRateKelvinPerMeter,
                                        planet.VegetationModel
                                            .PlantNitrogenKilogramsPerKilogramLiveBiomass,
                                        planet.VegetationModel
                                            .BaselineMortalityRatePerDay)))
                    .Where(
                        model =>
                            model is not null)
                    .Cast<VegetationModelDefinition>()
                    .ToArray();

            var invertebrateModels =
                request.Planets
                    .Select(
                        (planet, index) =>
                            planet.InvertebrateModel is null
                                ? null
                                : new InvertebrateModelDefinition(
                                    world.Planets[index].Id,
                                    new InvertebrateModelParameters(
                                        planet.InvertebrateModel
                                            .MaximumIntegrationStepSeconds,
                                        planet.InvertebrateModel
                                            .CarryingCapacityKilogramsPerKilogramLiveVegetation,
                                        planet.InvertebrateModel
                                            .InitialFractionOfLocalCarryingCapacity,
                                        planet.InvertebrateModel
                                            .MaximumRelativeGrowthRatePerDay,
                                        planet.InvertebrateModel
                                            .BaselineMortalityRatePerDay,
                                        planet.InvertebrateModel
                                            .LiveNitrogenKilogramsPerKilogramLiveBiomass)))
                    .Where(
                        model =>
                            model is not null)
                    .Cast<InvertebrateModelDefinition>()
                    .ToArray();

            var birdModels =
                request.Planets
                    .Select(
                        (planet, index) =>
                            planet.BirdModel is null
                                ? null
                                : new BirdModelDefinition(
                                    world.Planets[index].Id,
                                    new BirdModelParameters(
                                        carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass:
                                            planet.BirdModel
                                                .CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass,
                                        initialFractionOfLocalCarryingCapacity:
                                            planet.BirdModel
                                                .InitialFractionOfLocalCarryingCapacity,
                                        minimumInitialFlockMemberCount:
                                            planet.BirdModel
                                                .MinimumInitialFlockMemberCount,
                                        maximumInitialFlockCount:
                                            planet.BirdModel
                                                .MaximumInitialFlockCount,
                                        maximumIntegrationStepSeconds:
                                            planet.BirdModel
                                                .MaximumIntegrationStepSeconds,
                                        maximumTravelMetersPerDay:
                                            planet.BirdModel
                                                .MaximumTravelMetersPerDay,
                                        foodShortageMortalityRatePerDay:
                                            planet.BirdModel
                                                .FoodShortageMortalityRatePerDay,
                                        waterAbsenceMortalityRatePerDay:
                                            planet.BirdModel
                                                .WaterAbsenceMortalityRatePerDay,
                                        habitatAbsenceMortalityRatePerDay:
                                            planet.BirdModel
                                                .HabitatAbsenceMortalityRatePerDay,
                                        liveBiomassKilogramsPerBird:
                                            planet.BirdModel
                                                .LiveBiomassKilogramsPerBird,
                                        liveNitrogenKilogramsPerBird:
                                            planet.BirdModel
                                                .LiveNitrogenKilogramsPerBird,
                                        maximumPreyConsumptionKilogramsPerBirdPerDay:
                                            planet.BirdModel
                                                .MaximumPreyConsumptionKilogramsPerBirdPerDay,
                                        maximumRecruitmentRatePerDay:
                                            planet.BirdModel
                                                .MaximumRecruitmentRatePerDay)))
                    .Where(
                        model =>
                            model is not null)
                    .Cast<BirdModelDefinition>()
                    .ToArray();

            var grazerModels =
                request.Planets
                    .Select(
                        (planet, index) =>
                            planet.GrazerModel is null
                                ? null
                                : new GrazerModelDefinition(
                                    world.Planets[index].Id,
                                    new GrazerModelParameters(
                                        carryingCapacityGrazersPerKilogramLiveVegetationBiomass:
                                            planet.GrazerModel
                                                .CarryingCapacityGrazersPerKilogramLiveVegetationBiomass,
                                        initialFractionOfLocalCarryingCapacity:
                                            planet.GrazerModel
                                                .InitialFractionOfLocalCarryingCapacity,
                                        minimumInitialCohortMemberCount:
                                            planet.GrazerModel
                                                .MinimumInitialCohortMemberCount,
                                        maximumInitialCohortCount:
                                            planet.GrazerModel
                                                .MaximumInitialCohortCount,
                                        maximumIntegrationStepSeconds:
                                            planet.GrazerModel
                                                .MaximumIntegrationStepSeconds,
                                        maximumTravelMetersPerDay:
                                            planet.GrazerModel
                                                .MaximumTravelMetersPerDay,
                                        maximumGrazeKilogramsPerGrazerPerDay:
                                            planet.GrazerModel
                                                .MaximumGrazeKilogramsPerGrazerPerDay,
                                        foodShortageMortalityRatePerDay:
                                            planet.GrazerModel
                                                .FoodShortageMortalityRatePerDay,
                                        waterAbsenceMortalityRatePerDay:
                                            planet.GrazerModel
                                                .WaterAbsenceMortalityRatePerDay,
                                        useSurfaceWaterForMovement:
                                            planet.GrazerModel
                                                .UseSurfaceWaterForMovement,
                                        habitatAbsenceMortalityRatePerDay:
                                            planet.GrazerModel
                                                .HabitatAbsenceMortalityRatePerDay,
                                        liveBiomassKilogramsPerGrazer:
                                            planet.GrazerModel
                                                .LiveBiomassKilogramsPerGrazer,
                                        liveNitrogenKilogramsPerGrazer:
                                            planet.GrazerModel
                                                .LiveNitrogenKilogramsPerGrazer,
                                        maximumRecruitmentRatePerDay:
                                            planet.GrazerModel
                                                .MaximumRecruitmentRatePerDay)))
                    .Where(
                        model =>
                            model is not null)
                    .Cast<GrazerModelDefinition>()
                    .ToArray();

            var biogeochemistryModels =
                request.Planets
                    .Select(
                        (planet, index) =>
                            planet.BiogeochemistryModel is null
                                ? null
                                : new BiogeochemistryModelDefinition(
                                    world.Planets[index].Id,
                                    new BiogeochemistryModelParameters(
                                        planet.BiogeochemistryModel
                                            .MaximumIntegrationStepSeconds,
                                        planet.BiogeochemistryModel
                                            .MaximumRelativeDecompositionRatePerDay,
                                        planet.BiogeochemistryModel
                                            .SoilWaterForFullDecompositionKilogramsPerSquareMeter,
                                        planet.BiogeochemistryModel
                                            .MinimumDecompositionTemperatureKelvin,
                                        planet.BiogeochemistryModel
                                            .OptimumDecompositionTemperatureKelvin,
                                        planet.BiogeochemistryModel
                                            .MaximumDecompositionTemperatureKelvin,
                                        planet.BiogeochemistryModel
                                            .TemperatureLapseRateKelvinPerMeter)))
                    .Where(
                        model =>
                            model is not null)
                    .Cast<BiogeochemistryModelDefinition>()
                    .ToArray();

            var definition =
                new SimulationDefinition(
                    energyBalanceModels,
                    populationModels,
                    hydrologyModels,
                    vegetationModels,
                    invertebrateModels,
                    birdModels,
                    grazerModels,
                    biogeochemistryModels);

            var sessionId =
                manager.Create(
                    world,
                    definition);

            var session =
                manager.Get(sessionId);

            return Results.Created(
                $"/sessions/{sessionId.Value}",
                ToResponse(
                    sessionId,
                    session));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(
                new
                {
                    error = exception.Message
                });
        }
    });

app.MapGet(
    "/sessions/{id:guid}",
    (
        Guid id,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty)
            return Results.NotFound();

        var sessionId =
            new SimulationSessionId(id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(
            ToResponse(
                sessionId,
                session));
    });

app.MapGet(
    "/sessions/{id:guid}/definition",
    (
        Guid id,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty)
            return Results.NotFound();

        var sessionId =
            new SimulationSessionId(id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(
            ToDefinitionResponse(
                session.Definition));
    });

app.MapGet(
    "/sessions/{id:guid}/world",
    (
        Guid id,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty)
            return Results.NotFound();

        var sessionId =
            new SimulationSessionId(id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(
            ToWorldResponse(
                session.CurrentWorld));
    });

app.MapGet(
    "/sessions/{id:guid}/people/{personId:guid}/social/esters/{esterId:guid}",
    (
        Guid id,
        Guid personId,
        Guid esterId,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty ||
            personId == Guid.Empty ||
            esterId == Guid.Empty)
        {
            return Results.NotFound();
        }

        var sessionId =
            new SimulationSessionId(id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        var personIdentity =
            new PersonId(personId);

        var esterIdentity =
            new EsterId(esterId);

        var actor =
            SocialActorIdentity.ForEster(
                esterIdentity);

        try
        {
            var contact =
                session.GetPersonSocialContact(
                    personIdentity,
                    actor);

            return Results.Ok(
                ToPersonSocialRecognitionResponse(
                    personIdentity,
                    esterIdentity,
                    contact));
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    });

app.MapPost(
    "/sessions/{id:guid}/people/{personId:guid}/social/esters/{esterId:guid}",
    (
        Guid id,
        Guid personId,
        Guid esterId,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty ||
            personId == Guid.Empty ||
            esterId == Guid.Empty)
        {
            return Results.NotFound();
        }

        var sessionId =
            new SimulationSessionId(id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        var personIdentity =
            new PersonId(personId);

        var esterIdentity =
            new EsterId(esterId);

        var actor =
            SocialActorIdentity.ForEster(
                esterIdentity);

        try
        {
            session.RecordPersonSocialEncounter(
                personIdentity,
                actor);

            var contact =
                session.GetPersonSocialContact(
                    personIdentity,
                    actor);

            return Results.Ok(
                ToPersonSocialRecognitionResponse(
                    personIdentity,
                    esterIdentity,
                    contact));
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    });

app.MapGet(
    "/sessions/{id:guid}/esters/{esterId:guid}/manifestation",
    (
        Guid id,
        Guid esterId,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty ||
            esterId == Guid.Empty)
        {
            return Results.NotFound();
        }

        var sessionId =
            new SimulationSessionId(id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        var manifested =
            session.GetManifestedEster(
                new EsterId(esterId));

        return manifested is null
            ? Results.NotFound()
            : Results.Ok(
                ToManifestedEsterResponse(
                    manifested));
    });

app.MapPost(
    "/sessions/{id:guid}/esters/{esterId:guid}/manifest",
    (
        Guid id,
        Guid esterId,
        ManifestEsterRequest request,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty ||
            esterId == Guid.Empty)
        {
            return Results.NotFound();
        }

        if (request.PlanetId == Guid.Empty)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Planet identity cannot be empty."
                });
        }

        var sessionId =
            new SimulationSessionId(id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        var esterIdentity =
            new EsterId(esterId);

        if (session.GetManifestedEster(
                esterIdentity) is not null)
        {
            return Results.Conflict(
                new
                {
                    error =
                        "This Ester is already manifested in the world."
                });
        }

        try
        {
            session.ManifestEster(
                esterIdentity,
                new PlanetId(
                    request.PlanetId),
                request.LatitudeDegrees,
                request.LongitudeDegrees);

            var manifested =
                session.GetManifestedEster(
                    esterIdentity)
                ?? throw new InvalidOperationException(
                    "Manifestation was not recorded.");

            return Results.Ok(
                ToManifestedEsterResponse(
                    manifested));
        }
        catch (PlanetNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(
                new
                {
                    error = exception.Message
                });
        }
    });

app.MapPost(
    "/sessions/{id:guid}/esters/{esterId:guid}/move",
    (
        Guid id,
        Guid esterId,
        MoveManifestedEsterRequest request,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty ||
            esterId == Guid.Empty)
        {
            return Results.NotFound();
        }

        var sessionId =
            new SimulationSessionId(id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        var esterIdentity =
            new EsterId(esterId);

        if (session.GetManifestedEster(
                esterIdentity) is null)
        {
            return Results.NotFound();
        }

        try
        {
            session.MoveManifestedEster(
                esterIdentity,
                request.LatitudeDegrees,
                request.LongitudeDegrees);

            var manifested =
                session.GetManifestedEster(
                    esterIdentity)
                ?? throw new InvalidOperationException(
                    "Manifestation disappeared after movement.");

            return Results.Ok(
                ToManifestedEsterResponse(
                    manifested));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(
                new
                {
                    error = exception.Message
                });
        }
    });

app.MapGet(
    "/sessions/{id:guid}/planets/{planetId:guid}/surface",
    (
        Guid id,
        Guid planetId,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty ||
            planetId == Guid.Empty)
        {
            return Results.NotFound();
        }

        var sessionId =
            new SimulationSessionId(id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        var planetIdentity =
            new PlanetId(
                planetId);

        var planet =
            session.CurrentWorld.Planets
                .FirstOrDefault(
                    candidate =>
                        candidate.Id ==
                        planetIdentity);

        if (planet is null)
        {
            return Results.NotFound();
        }

        var terrain =
            session.CurrentWorld.Terrain
                .FirstOrDefault(
                    candidate =>
                        candidate.PlanetId ==
                        planetIdentity);

        if (terrain is null)
        {
            return Results.NotFound();
        }

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                terrain.GridDefinition);

        return Results.Ok(
            ToSurfaceResponse(
                planet,
                terrain.GridDefinition,
                grid));
    });

app.MapGet(
    "/sessions/{id:guid}/planets/{planetId:guid}/terrain",
    (
        Guid id,
        Guid planetId,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty ||
            planetId == Guid.Empty)
        {
            return Results.NotFound();
        }

        var sessionId =
            new SimulationSessionId(id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        var planetIdentity =
            new PlanetId(
                planetId);

        var planet =
            session.CurrentWorld.Planets
                .FirstOrDefault(
                    candidate =>
                        candidate.Id ==
                        planetIdentity);

        if (planet is null)
        {
            return Results.NotFound();
        }

        var terrain =
            session.CurrentWorld.Terrain
                .FirstOrDefault(
                    candidate =>
                        candidate.PlanetId ==
                        planetIdentity);

        if (terrain is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(
            ToTerrainResponse(
                planet,
                terrain));
    });

app.MapGet(
    "/sessions/{id:guid}/planets/{planetId:guid}/hydrology",
    (
        Guid id,
        Guid planetId,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty ||
            planetId == Guid.Empty)
        {
            return Results.NotFound();
        }

        var sessionId =
            new SimulationSessionId(id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        var planetIdentity =
            new PlanetId(
                planetId);

        var planet =
            session.CurrentWorld.Planets
                .FirstOrDefault(
                    candidate =>
                        candidate.Id ==
                        planetIdentity);

        if (planet is null)
        {
            return Results.NotFound();
        }

        var hydrology =
            session.CurrentWorld.Hydrology
                .FirstOrDefault(
                    candidate =>
                        candidate.PlanetId ==
                        planetIdentity);

        if (hydrology is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(
            ToHydrologyResponse(
                planet,
                hydrology));
    });

app.MapGet(
    "/sessions/{id:guid}/planets/{planetId:guid}/biogeochemistry",
    (
        Guid id,
        Guid planetId,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty ||
            planetId == Guid.Empty)
        {
            return Results.NotFound();
        }

        var sessionId =
            new SimulationSessionId(
                id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        var planetIdentity =
            new PlanetId(
                planetId);

        var planet =
            session.CurrentWorld.Planets
                .FirstOrDefault(
                    candidate =>
                        candidate.Id ==
                        planetIdentity);

        if (planet is null)
        {
            return Results.NotFound();
        }

        var biogeochemistry =
            session.CurrentWorld.Biogeochemistry
                .FirstOrDefault(
                    candidate =>
                        candidate.PlanetId ==
                        planetIdentity);

        if (biogeochemistry is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(
            ToBiogeochemistryResponse(
                planet,
                biogeochemistry));
    });

app.MapGet(
    "/sessions/{id:guid}/planets/{planetId:guid}/vegetation",
    (
        Guid id,
        Guid planetId,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty ||
            planetId == Guid.Empty)
        {
            return Results.NotFound();
        }

        var sessionId =
            new SimulationSessionId(id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        var planetIdentity =
            new PlanetId(
                planetId);

        var planet =
            session.CurrentWorld.Planets
                .FirstOrDefault(
                    candidate =>
                        candidate.Id ==
                        planetIdentity);

        if (planet is null)
        {
            return Results.NotFound();
        }

        var vegetation =
            session.CurrentWorld.Vegetation
                .FirstOrDefault(
                    candidate =>
                        candidate.PlanetId ==
                        planetIdentity);

        if (vegetation is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(
            ToVegetationResponse(
                planet,
                vegetation));
    });


app.MapGet(
    "/sessions/{id:guid}/planets/{planetId:guid}/invertebrates",
    (
        Guid id,
        Guid planetId,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty ||
            planetId == Guid.Empty)
        {
            return Results.NotFound();
        }

        var sessionId =
            new SimulationSessionId(id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        var planetIdentity =
            new PlanetId(
                planetId);

        var planet =
            session.CurrentWorld.Planets
                .FirstOrDefault(
                    candidate =>
                        candidate.Id ==
                        planetIdentity);

        if (planet is null)
        {
            return Results.NotFound();
        }

        var invertebrates =
            session.CurrentWorld.Invertebrates
                .FirstOrDefault(
                    candidate =>
                        candidate.PlanetId ==
                        planetIdentity);

        if (invertebrates is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(
            ToInvertebrateResponse(
                planet,
                invertebrates));
    });

app.MapGet(
    "/sessions/{id:guid}/planets/{planetId:guid}/bird-flocks",
    (
        Guid id,
        Guid planetId,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty ||
            planetId == Guid.Empty)
        {
            return Results.NotFound();
        }

        var sessionId =
            new SimulationSessionId(
                id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        var planetIdentity =
            new PlanetId(
                planetId);

        var planet =
            session.CurrentWorld.Planets
                .FirstOrDefault(
                    candidate =>
                        candidate.Id ==
                        planetIdentity);

        if (planet is null)
        {
            return Results.NotFound();
        }

        var flocks =
            session.CurrentWorld.BirdFlocks
                .Where(
                    flock =>
                        flock.PlanetId ==
                        planetIdentity)
                .OrderBy(
                    flock =>
                        flock.Id.Value)
                .Select(
                    flock =>
                        new BirdFlockResponse(
                            flock.Id.Value,
                            flock.MemberCount,
                            flock.LatitudeDegrees,
                            flock.LongitudeDegrees,
                            new OrganismMaterialResponse(
                                flock.Material.LiveBiomassKilograms,
                                flock.Material.LiveNitrogenKilograms),
                            flock.RecruitmentAccumulator))
                .ToArray();

        return Results.Ok(
            new BirdFlocksResponse(
                planetIdentity.Value,
                flocks));
    });

app.MapGet(
    "/sessions/{id:guid}/planets/{planetId:guid}/grazer-cohorts",
    (
        Guid id,
        Guid planetId,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty ||
            planetId == Guid.Empty)
        {
            return Results.NotFound();
        }

        var sessionId =
            new SimulationSessionId(
                id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        var planetIdentity =
            new PlanetId(
                planetId);

        var planet =
            session.CurrentWorld.Planets
                .FirstOrDefault(
                    candidate =>
                        candidate.Id ==
                        planetIdentity);

        if (planet is null)
        {
            return Results.NotFound();
        }

        var cohorts =
            session.CurrentWorld.GrazerCohorts
                .Where(
                    cohort =>
                        cohort.PlanetId ==
                        planetIdentity)
                .OrderBy(
                    cohort =>
                        cohort.Id.Value)
                .Select(
                    cohort =>
                        new GrazerCohortResponse(
                            cohort.Id.Value,
                            cohort.MemberCount,
                            cohort.LatitudeDegrees,
                            cohort.LongitudeDegrees,
                            new OrganismMaterialResponse(
                                cohort.Material.LiveBiomassKilograms,
                                cohort.Material.LiveNitrogenKilograms)))
                .ToArray();

        return Results.Ok(
            new GrazerCohortsResponse(
                planetIdentity.Value,
                cohorts));
    });

app.MapGet(
    "/sessions/{id:guid}/planets/{planetId:guid}/standing-water",
    (
        Guid id,
        Guid planetId,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty ||
            planetId == Guid.Empty)
        {
            return Results.NotFound();
        }

        var sessionId =
            new SimulationSessionId(id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        var planetIdentity =
            new PlanetId(
                planetId);

        var planet =
            session.CurrentWorld.Planets
                .FirstOrDefault(
                    candidate =>
                        candidate.Id ==
                        planetIdentity);

        if (planet is null)
        {
            return Results.NotFound();
        }

        var terrain =
            session.CurrentWorld.Terrain
                .FirstOrDefault(
                    candidate =>
                        candidate.PlanetId ==
                        planetIdentity);

        var hydrology =
            session.CurrentWorld.Hydrology
                .FirstOrDefault(
                    candidate =>
                        candidate.PlanetId ==
                        planetIdentity);

        if (terrain is null ||
            hydrology is null)
        {
            return Results.NotFound();
        }

        var standingWater =
            PlanetStandingWaterState.Derive(
                planet,
                terrain,
                hydrology);

        return Results.Ok(
            ToStandingWaterResponse(
                standingWater));
    });

app.MapGet(
    "/sessions/{id:guid}/timeline",
    (
        Guid id,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty)
            return Results.NotFound();

        var sessionId =
            new SimulationSessionId(id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(
            ToTimelineResponse(
                session.Timeline));
    });

app.MapPost(
    "/sessions/{id:guid}/pause",
    (
        Guid id,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty)
            return Results.NotFound();

        var sessionId =
            new SimulationSessionId(id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        session.Pause();

        return Results.Ok(
            ToResponse(
                sessionId,
                session));
    });

app.MapPost(
    "/sessions/{id:guid}/resume",
    (
        Guid id,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty)
            return Results.NotFound();

        var sessionId =
            new SimulationSessionId(id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        session.Resume();

        return Results.Ok(
            ToResponse(
                sessionId,
                session));
    });

app.MapPost(
    "/sessions/{id:guid}/planets/{planetId:guid}/environment",
    (
        Guid id,
        Guid planetId,
        ReplacePlanetEnvironmentRequest request,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty ||
            planetId == Guid.Empty)
        {
            return Results.NotFound();
        }

        var sessionId =
            new SimulationSessionId(id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        if (request.Atmosphere is null)
        {
            return Results.BadRequest(
                new
                {
                    error = "Atmosphere is required."
                });
        }

        if (request.Atmosphere.CompositionByMoleFraction is null)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Atmosphere composition is required."
                });
        }

        try
        {
            var environment =
                new PlanetEnvironment(
                    request.MeanSurfaceTemperatureKelvin,
                    request.SurfaceWaterFraction,
                    request.IceCoverageFraction,
                    new AtmosphereState(
                        request.Atmosphere
                            .SurfacePressurePascals,
                        request.Atmosphere
                            .CompositionByMoleFraction));

            session.ReplacePlanetEnvironment(
                new PlanetId(planetId),
                environment);

            return Results.Ok(
                ToWorldResponse(
                    session.CurrentWorld));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(
                new
                {
                    error = exception.Message
                });
        }
        catch (PlanetNotFoundException)
        {
            return Results.NotFound();
        }
    });

app.MapPost(
    "/sessions/{id:guid}/advance",
    (
        Guid id,
        AdvanceTimeRequest request,
        SimulationSessionManager manager) =>
    {
        if (id == Guid.Empty)
            return Results.NotFound();

        if (request.Seconds < 0)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Simulation time cannot advance by a negative duration."
                });
        }

        var sessionId =
            new SimulationSessionId(id);

        if (!manager.TryGet(
                sessionId,
                out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        try
        {
            session.Advance(
                request.Seconds);
        }
        catch (OverflowException)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Requested advancement exceeds the supported simulation time range."
                });
        }

        return Results.Ok(
            ToResponse(
                sessionId,
                session));
    });

app.MapPost(
    "/sessions/{id:guid}/archives",
    (
        Guid id,
        SimulationSessionManager manager,
        SimulationSessionArchiveService archives,
        SessionArchiveLocation location) =>
    {
        if (id == Guid.Empty)
            return Results.NotFound();

        var sessionId = new SimulationSessionId(id);

        if (!manager.TryGet(sessionId, out var session) ||
            session is null)
        {
            return Results.NotFound();
        }

        var archiveId = Guid.NewGuid();

        var timeline = archives.Save(
            sessionId,
            location.GetPath(archiveId),
            new TimelineArchiveProvenance(
                "Est",
                "0.1.0-alpha",
                "simulation"));

        return Results.Created(
            $"/archives/{archiveId}",
            new ArchiveResponse(
                archiveId,
                timeline.CurrentWorld.Id.Value,
                timeline.Id.Value));
    });

app.MapPost(
    "/archives/{archiveId:guid}/load",
    (
        Guid archiveId,
        SimulationSessionManager manager,
        SimulationSessionArchiveService archives,
        SessionArchiveLocation location) =>
    {
        if (archiveId == Guid.Empty)
            return Results.NotFound();

        var path = location.GetPath(archiveId);

        if (!File.Exists(path))
            return Results.NotFound();

        var sessionId = archives.Load(path);
        var session = manager.Get(sessionId);

        return Results.Created(
            $"/sessions/{sessionId.Value}",
            ToResponse(sessionId, session));
    });

app.Run();

static ManifestedEsterResponse
    ToManifestedEsterResponse(
        Est.Simulation.Players.ManifestedEsterState manifested)
{
    return new ManifestedEsterResponse(
        manifested.EsterId.Value,
        manifested.PlanetId.Value,
        manifested.LatitudeDegrees,
        manifested.LongitudeDegrees);
}

static PersonSocialRecognitionResponse
    ToPersonSocialRecognitionResponse(
        PersonId personId,
        EsterId esterId,
        PersonSocialContactState? contact)
{
    return new PersonSocialRecognitionResponse(
        personId.Value,
        esterId.Value,
        contact is not null,
        contact?.FirstEncounterTimeSeconds,
        contact?.LastEncounterTimeSeconds,
        contact?.EncounterCount ?? 0);
}

static TimelineResponse ToTimelineResponse(
    SimulationTimeline timeline)
{
    return new TimelineResponse(
        timeline.Id.Value,
        timeline.CurrentWorld.Id.Value,
        timeline.ParentTimelineId?.Value,
        timeline.ParentCheckpointId,
        timeline.CurrentWorld.CurrentTime.TotalSeconds,
        timeline.Checkpoints
            .Select(
                checkpoint =>
                    new CheckpointResponse(
                        checkpoint.Id,
                        checkpoint.Time.TotalSeconds))
            .ToArray(),
        timeline.Events
            .Select(
                timelineEvent =>
                    new TimelineEventResponse(
                        timelineEvent.Id,
                        timelineEvent.OccurredAt.TotalSeconds,
                        timelineEvent.Cause,
                        timelineEvent.Summary,
                        timelineEvent.AffectedPlanetId?.Value,
                        timelineEvent.ElapsedSeconds,
                        timelineEvent.Metrics))
            .ToArray());
}

static SurfaceResponse ToSurfaceResponse(
    PlanetState planet,
    SurfaceGridDefinition gridDefinition,
    IPlanetSurfaceGrid grid)
{
    return new SurfaceResponse(
        planet.Id.Value,
        new SurfaceGridResponse(
            gridDefinition.Kind.ToString(),
            gridDefinition.IdentityVersion,
            gridDefinition.LatitudeBandCount,
            gridDefinition.LongitudeBandCount),
        grid.Cells
            .Select(
                cell =>
                    new SurfaceCellResponse(
                        cell.Id.Value,
                        cell.CenterLatitudeDegrees,
                        cell.CenterLongitudeDegrees,
                        cell.AreaSquareMeters,
                        grid.GetBoundary(
                                cell.Id)
                            .Select(
                                coordinate =>
                                    new SurfaceCoordinateResponse(
                                        coordinate.LatitudeDegrees,
                                        coordinate.LongitudeDegrees))
                            .ToArray()))
            .ToArray());
}

static TerrainResponse ToTerrainResponse(
    PlanetState planet,
    PlanetTerrainState terrain)
{
    terrain.ValidateFor(
        planet);

    return new TerrainResponse(
        planet.Id.Value,
        new SurfaceGridResponse(
            terrain.GridDefinition.Kind.ToString(),
            terrain.GridDefinition.IdentityVersion,
            terrain.GridDefinition.LatitudeBandCount,
            terrain.GridDefinition.LongitudeBandCount),
        terrain.Cells
            .Select(
                cell =>
                    new TerrainCellResponse(
                        cell.CellId.Value,
                        cell.ElevationMeters))
            .ToArray());
}

static HydrologyResponse ToHydrologyResponse(
    PlanetState planet,
    PlanetHydrologyState hydrology)
{
    hydrology.ValidateFor(
        planet);

    return new HydrologyResponse(
        planet.Id.Value,
        new SurfaceGridResponse(
            hydrology.GridDefinition.Kind.ToString(),
            hydrology.GridDefinition.IdentityVersion,
            hydrology.GridDefinition.LatitudeBandCount,
            hydrology.GridDefinition.LongitudeBandCount),
        hydrology.TotalWaterMassKilograms(
            planet),
        hydrology.Cells
            .Select(
                cell =>
                    new HydrologyCellResponse(
                        cell.CellId.Value,
                        cell.AtmosphericWaterKilogramsPerSquareMeter,
                        cell.SurfaceLiquidWaterKilogramsPerSquareMeter,
                        cell.SoilWaterKilogramsPerSquareMeter,
                        cell.SnowIceWaterEquivalentKilogramsPerSquareMeter))
            .ToArray());
}

static BiogeochemistryResponse ToBiogeochemistryResponse(
    PlanetState planet,
    PlanetBiogeochemistryState biogeochemistry)
{
    biogeochemistry.ValidateFor(
        planet);

    return new BiogeochemistryResponse(
        planet.Id.Value,
        new SurfaceGridResponse(
            biogeochemistry.GridDefinition.Kind.ToString(),
            biogeochemistry.GridDefinition.IdentityVersion,
            biogeochemistry.GridDefinition.LatitudeBandCount,
            biogeochemistry.GridDefinition.LongitudeBandCount),
        biogeochemistry.Cells
            .Select(
                cell =>
                    new BiogeochemistryCellResponse(
                        cell.CellId.Value,
                        cell.DetritalBiomassKilogramsPerSquareMeter,
                        cell.DetritalNitrogenKilogramsPerSquareMeter,
                        cell.PlantAvailableNitrogenKilogramsPerSquareMeter))
            .ToArray());
}

static VegetationResponse ToVegetationResponse(
    PlanetState planet,
    PlanetVegetationState vegetation)
{
    vegetation.ValidateFor(
        planet);

    return new VegetationResponse(
        planet.Id.Value,
        new SurfaceGridResponse(
            vegetation.GridDefinition.Kind.ToString(),
            vegetation.GridDefinition.IdentityVersion,
            vegetation.GridDefinition.LatitudeBandCount,
            vegetation.GridDefinition.LongitudeBandCount),
        vegetation.Cells
            .Select(
                cell =>
                    new VegetationCellResponse(
                        cell.CellId.Value,
                        cell.LiveBiomassKilogramsPerSquareMeter))
            .ToArray());
}

static InvertebrateResponse ToInvertebrateResponse(
    PlanetState planet,
    PlanetInvertebrateState invertebrates)
{
    invertebrates.ValidateFor(
        planet);

    return new InvertebrateResponse(
        planet.Id.Value,
        new SurfaceGridResponse(
            invertebrates.GridDefinition.Kind.ToString(),
            invertebrates.GridDefinition.IdentityVersion,
            invertebrates.GridDefinition.LatitudeBandCount,
            invertebrates.GridDefinition.LongitudeBandCount),
        invertebrates.Cells
            .Select(
                cell =>
                    new InvertebrateCellResponse(
                        cell.CellId.Value,
                        cell.LiveBiomassKilogramsPerSquareMeter,
                        cell.LiveNitrogenKilogramsPerSquareMeter))
            .ToArray());
}

static StandingWaterResponse ToStandingWaterResponse(
    PlanetStandingWaterState standingWater)
{
    return new StandingWaterResponse(
        standingWater.PlanetId.Value,
        new SurfaceGridResponse(
            standingWater.GridDefinition.Kind.ToString(),
            standingWater.GridDefinition.IdentityVersion,
            standingWater.GridDefinition.LatitudeBandCount,
            standingWater.GridDefinition.LongitudeBandCount),
        standingWater.Cells
            .Select(
                cell =>
                    new StandingWaterCellResponse(
                        cell.CellId.Value,
                        cell.Kind.ToString(),
                        cell.WaterBodyAnchorCellId?.Value,
                        cell.WaterDepthMeters,
                        cell.WaterSurfaceElevationMeters))
            .ToArray(),
        standingWater.WaterBodies
            .Select(
                body =>
                    new StandingWaterBodyResponse(
                        body.AnchorCellId.Value,
                        body.Kind.ToString(),
                        body.CellIds
                            .Select(
                                cellId =>
                                    cellId.Value)
                            .ToArray(),
                        body.SurfaceAreaSquareMeters,
                        body.WaterVolumeCubicMeters))
            .ToArray());
}

static WorldResponse ToWorldResponse(
    WorldState world)
{
    return new WorldResponse(
        world.Id.Value,
        world.CurrentTime.TotalSeconds,
        world.Planets
            .Select(
                planet =>
                    new PlanetResponse(
                        planet.Id.Value,
                        planet.Name,
                        planet.MassKilograms,
                        planet.MeanRadiusMeters,
                        planet.SurfaceGravityMetersPerSecondSquared,
                        new PlanetEnvironmentResponse(
                            planet.Environment.MeanSurfaceTemperatureKelvin,
                            planet.Environment.SurfaceWaterFraction,
                            planet.Environment.IceCoverageFraction,
                            new AtmosphereResponse(
                                planet.Environment.Atmosphere.SurfacePressurePascals,
                                planet.Environment.Atmosphere
                                    .CompositionByMoleFraction))))
            .ToArray(),
        world.Population
            .Select(
                person =>
                    new PopulationPersonResponse(
                        person.Id.Value,
                        person.PlanetId.Value,
                        person.Sex.ToString(),
                        person.BirthTimeSeconds,
                        person.LatitudeDegrees,
                        person.LongitudeDegrees,
                        person.ParentId?.Value,
                        person.Pregnancy is not null,
                        person.Pregnancy
                            ?.ConceptionTimeSeconds,
                        person.Pregnancy
                            ?.FatherId.Value,
                        person.Activity.ToString(),
                        person.Needs.EnergyReserve,
                        person.Needs.Health,
                        new OrganismMaterialResponse(
                            person.Material.LiveBiomassKilograms,
                            person.Material.LiveNitrogenKilograms)))
            .ToArray(),
        world.Animals
            .Select(
                animal =>
                    new AnimalResponse(
                        animal.Id.Value,
                        animal.PlanetId.Value,
                        animal.Species.ToString(),
                        animal.BirthTimeSeconds,
                        animal.ParentId?.Value,
                        animal.WolfLifecycle
                            ?.Sex.ToString(),
                        animal.WolfLifecycle
                            ?.Pregnancy is not null,
                        animal.WolfLifecycle
                            ?.Pregnancy
                            ?.ConceptionTimeSeconds,
                        animal.WolfLifecycle
                            ?.Pregnancy
                            ?.FatherId.Value,
                        animal.LatitudeDegrees,
                        animal.LongitudeDegrees,
                        animal.EnergyReserve,
                        animal.Health,
                        animal.Activity.ToString(),
                        new OrganismMaterialResponse(
                            animal.Material.LiveBiomassKilograms,
                            animal.Material.LiveNitrogenKilograms)))
            .ToArray(),
        world.Planets
            .Select(
                planet =>
                    world.SeasonalStates.FirstOrDefault(
                        state =>
                            state.PlanetId ==
                            planet.Id)
                    ?? new PlanetSeasonalState(
                        planet.Id))
            .Select(
                state =>
                    new SeasonalStateResponse(
                        state.PlanetId.Value,
                        state.ControlMode.ToString(),
                        state.DerivedContext is null
                            ? null
                            : new SeasonalContextResponse(
                                state.DerivedContext.PhaseId,
                                state.DerivedContext.CycleFraction,
                                state.DerivedContext
                                    .SubsolarLatitudeDegrees),
                        state.OverrideContext is null
                            ? null
                            : new SeasonalContextResponse(
                                state.OverrideContext.PhaseId,
                                state.OverrideContext.CycleFraction,
                                state.OverrideContext
                                    .SubsolarLatitudeDegrees),
                        state.EffectiveContext is null
                            ? null
                            : new SeasonalContextResponse(
                                state.EffectiveContext.PhaseId,
                                state.EffectiveContext.CycleFraction,
                                state.EffectiveContext
                                    .SubsolarLatitudeDegrees)))
            .ToArray());
}

static SimulationDefinitionResponse ToDefinitionResponse(
    SimulationDefinition definition)
{
    return new SimulationDefinitionResponse(
        definition.PlanetaryEnergyBalanceModels
            .Select(
                model =>
                    new PlanetaryEnergyBalanceModelResponse(
                        model.PlanetId.Value,
                        model.Parameters
                            .StellarFluxWattsPerSquareMeter,
                        model.Parameters
                            .EffectiveLongwaveEmissivity,
                        model.Parameters
                            .EffectiveHeatCapacityJoulesPerSquareMeterKelvin,
                        model.Parameters.IceFreeAlbedo,
                        model.Parameters.IceAlbedo,
                        model.Parameters
                            .FullIceTemperatureKelvin,
                        model.Parameters
                            .IceFreeTemperatureKelvin,
                        model.Parameters
                            .IceResponseTimescaleSeconds))
            .ToArray(),
        definition.PopulationModels
            .Select(
                model =>
                    new PopulationModelResponse(
                        model.PlanetId.Value,
                        model.Parameters.Seed,
                        model.Parameters
                            .AnnualBirthRatePerEligibleFemale,
                        model.Parameters
                            .AnnualAdultMigrationRate,
                        model.Parameters
                            .AnnualBaseMortalityRate,
                        model.Parameters
                            .AnnualElderMortalityRate,
                        model.Parameters
                            .ReproductiveAgeMinimumYears,
                        model.Parameters
                            .ReproductiveAgeMaximumYears,
                        model.Parameters
                            .ElderAgeYears,
                        model.Parameters
                            .LocalMigrationDegrees,
                        model.Parameters
                            .LongMigrationProbability,
                        model.Parameters
                            .LongMigrationDegrees,
                        model.VegetationForaging is null
                            ? null
                            : new VegetationForagingResponse(
                                model.VegetationForaging
                                    .KilogramsLiveBiomassPerEnergyReserveUnit,
                                model.VegetationForaging
                                    .MaximumHarvestKilogramsPerPersonPerDay)))
            .ToArray(),
        definition.VegetationModels
            .Select(
                model =>
                    new VegetationModelResponse(
                        model.PlanetId.Value,
                        model.Parameters
                            .MaximumIntegrationStepSeconds,
                        model.Parameters
                            .CarryingCapacityKilogramsPerSquareMeter,
                        model.Parameters
                            .MaximumRelativeGrowthRatePerDay,
                        model.Parameters
                            .SoilWaterForFullProductivityKilogramsPerSquareMeter,
                        model.Parameters
                            .MinimumGrowthTemperatureKelvin,
                        model.Parameters
                            .OptimumGrowthTemperatureKelvin,
                        model.Parameters
                            .MaximumGrowthTemperatureKelvin,
                        model.Parameters
                            .TemperatureLapseRateKelvinPerMeter,
                        model.Parameters
                            .PlantNitrogenKilogramsPerKilogramLiveBiomass,
                        model.Parameters
                            .BaselineMortalityRatePerDay))
            .ToArray(),
        definition.InvertebrateModels
            .Select(
                model =>
                    new InvertebrateModelResponse(
                        model.PlanetId.Value,
                        model.Parameters
                            .MaximumIntegrationStepSeconds,
                        model.Parameters
                            .CarryingCapacityKilogramsPerKilogramLiveVegetation,
                        model.Parameters
                            .InitialFractionOfLocalCarryingCapacity,
                        model.Parameters
                            .MaximumRelativeGrowthRatePerDay,
                        model.Parameters
                            .BaselineMortalityRatePerDay,
                        model.Parameters
                            .LiveNitrogenKilogramsPerKilogramLiveBiomass))
            .ToArray(),
        definition.BirdModels
            .Select(
                model =>
                    new BirdModelResponse(
                        model.PlanetId.Value,
                        model.Parameters
                            .CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass,
                        model.Parameters
                            .InitialFractionOfLocalCarryingCapacity,
                        model.Parameters
                            .MinimumInitialFlockMemberCount,
                        model.Parameters
                            .MaximumInitialFlockCount,
                        model.Parameters
                            .MaximumIntegrationStepSeconds,
                        model.Parameters
                            .MaximumTravelMetersPerDay,
                        model.Parameters
                            .FoodShortageMortalityRatePerDay,
                        model.Parameters
                            .WaterAbsenceMortalityRatePerDay,
                        model.Parameters
                            .HabitatAbsenceMortalityRatePerDay,
                        model.Parameters
                            .MaterialPerBird
                            .LiveBiomassKilogramsPerUnit,
                        model.Parameters
                            .MaterialPerBird
                            .LiveNitrogenKilogramsPerUnit,
                        model.Parameters
                            .MaximumPreyConsumptionKilogramsPerBirdPerDay,
                        model.Parameters
                            .MaximumRecruitmentRatePerDay))
            .ToArray(),
        definition.GrazerModels
            .Select(
                model =>
                    new GrazerModelResponse(
                        model.PlanetId.Value,
                        model.Parameters
                            .CarryingCapacityGrazersPerKilogramLiveVegetationBiomass,
                        model.Parameters
                            .InitialFractionOfLocalCarryingCapacity,
                        model.Parameters
                            .MinimumInitialCohortMemberCount,
                        model.Parameters
                            .MaximumInitialCohortCount,
                        model.Parameters
                            .MaximumIntegrationStepSeconds,
                        model.Parameters
                            .MaximumTravelMetersPerDay,
                        model.Parameters
                            .MaximumGrazeKilogramsPerGrazerPerDay,
                        model.Parameters
                            .FoodShortageMortalityRatePerDay,
                        model.Parameters
                            .WaterAbsenceMortalityRatePerDay,
                        model.Parameters
                            .UseSurfaceWaterForMovement,
                        model.Parameters
                            .HabitatAbsenceMortalityRatePerDay,
                        model.Parameters
                            .MaterialPerGrazer
                            .LiveBiomassKilogramsPerUnit,
                        model.Parameters
                            .MaterialPerGrazer
                            .LiveNitrogenKilogramsPerUnit,
                        model.Parameters
                            .MaximumRecruitmentRatePerDay))
            .ToArray(),
        definition.BiogeochemistryModels
            .Select(
                model =>
                    new BiogeochemistryModelResponse(
                        model.PlanetId.Value,
                        model.Parameters
                            .MaximumIntegrationStepSeconds,
                        model.Parameters
                            .MaximumRelativeDecompositionRatePerDay,
                        model.Parameters
                            .SoilWaterForFullDecompositionKilogramsPerSquareMeter,
                        model.Parameters
                            .MinimumDecompositionTemperatureKelvin,
                        model.Parameters
                            .OptimumDecompositionTemperatureKelvin,
                        model.Parameters
                            .MaximumDecompositionTemperatureKelvin,
                        model.Parameters
                            .TemperatureLapseRateKelvinPerMeter))
            .ToArray(),
        definition.SeasonalModels
            .Select(
                model =>
                    new CircularOrbitSeasonalModelResponse(
                        model.PlanetId.Value,
                        model.Parameters
                            .OrbitalPeriodSeconds,
                        model.Parameters
                            .AxialTiltDegrees,
                        model.Parameters
                            .CycleFractionAtTimeZero))
            .ToArray());
}

static SessionResponse ToResponse(
    SimulationSessionId sessionId,
    SimulationSession session)
{
    var timeline =
        session.Timeline;

    var world =
        timeline.CurrentWorld;

    return new SessionResponse(
        sessionId.Value,
        world.Id.Value,
        timeline.Id.Value,
        world.CurrentTime.TotalSeconds,
        session.IsPaused,
        world.Planets.Length,
        timeline.Events.Length,
        timeline.Checkpoints.Length);
}

public partial class Program;
