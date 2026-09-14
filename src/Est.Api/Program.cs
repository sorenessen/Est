using Est.Api;
using Est.Api.Contracts;
using Est.Persistence.Archives;
using Est.Persistence.Storage;
using Est.Application.Sessions;
using Est.Application.Worlds;
using Est.Simulation.Climate;
using Est.Simulation.Definitions;
using Est.Simulation.Hydrology;
using Est.Simulation.Planets;
using Est.Simulation.Population;
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
                                                .MaximumAgeYears),
                                    planet.SyntheticFood is null
                                        ? null
                                        : new SyntheticFoodCreationSpecification(
                                            planet.SyntheticFood
                                                .PatchCount,
                                            planet.SyntheticFood
                                                .Seed,
                                            planet.SyntheticFood
                                                .CenterLatitudeDegrees,
                                            planet.SyntheticFood
                                                .CenterLongitudeDegrees,
                                            planet.SyntheticFood
                                                .SpreadDegrees,
                                            planet.SyntheticFood
                                                .EnergyPerPatch,
                                            planet.SyntheticFood
                                                .RecoveryEnergyPerDay),
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
                                                .SpreadDegrees),
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
                                                .SurfaceLiquidWaterInventoryKilograms)))
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
                                                .Seed)))
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

            var definition =
                new SimulationDefinition(
                    energyBalanceModels,
                    populationModels,
                    hydrologyModels);

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
                        person.Needs.Health))
            .ToArray(),
        world.FoodResources
            .Select(
                resource =>
                    new FoodResourceResponse(
                        resource.Id.Value,
                        resource.PlanetId.Value,
                        resource.LatitudeDegrees,
                        resource.LongitudeDegrees,
                        resource.AvailableEnergy,
                        resource.CapacityEnergy,
                        resource.RecoveryEnergyPerDay))
            .ToArray(),
        world.Animals
            .Select(
                animal =>
                    new AnimalResponse(
                        animal.Id.Value,
                        animal.PlanetId.Value,
                        animal.Species.ToString(),
                        animal.LatitudeDegrees,
                        animal.LongitudeDegrees,
                        animal.EnergyReserve,
                        animal.Health,
                        animal.Activity.ToString()))
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
                            .LongMigrationDegrees))
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
