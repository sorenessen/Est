using Est.Application.Sessions;
using Est.Simulation.Time;
using Est.Simulation.Worlds;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Est.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Est.Api.Tests.Sessions;

public sealed class SessionEndpointTests
{
    [Fact]
    public async Task CreateReadAndAdvance_PreservesSessionIdentityAndHistory()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var createdResponse = await client.PostAsJsonAsync("/sessions", new CreateSessionRequest([]));

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);

        var created = await createdResponse.Content
            .ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.SessionId);
        Assert.NotEqual(Guid.Empty, created.WorldId);
        Assert.NotEqual(Guid.Empty, created.TimelineId);
        Assert.Equal(0, created.CurrentTimeSeconds);
        Assert.Equal(0, created.EventCount);
        Assert.Equal(1, created.CheckpointCount);

        var world = await client.GetFromJsonAsync<WorldResponse>(
            $"/sessions/{created.SessionId}/world");

        Assert.NotNull(world);
        Assert.Equal(created.WorldId, world.WorldId);
        Assert.Equal(0, world.CurrentTimeSeconds);
        Assert.Empty(world.Planets);

        var advanceResponse = await client.PostAsJsonAsync(
            $"/sessions/{created.SessionId}/advance",
            new AdvanceTimeRequest(60));

        Assert.Equal(HttpStatusCode.OK, advanceResponse.StatusCode);

        var advanced = await advanceResponse.Content
            .ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(advanced);
        Assert.Equal(created.SessionId, advanced.SessionId);
        Assert.Equal(created.WorldId, advanced.WorldId);
        Assert.Equal(created.TimelineId, advanced.TimelineId);
        Assert.Equal(60, advanced.CurrentTimeSeconds);
        Assert.Equal(1, advanced.EventCount);
        Assert.Equal(1, advanced.CheckpointCount);
    }

    [Fact]
    public async Task UnknownSession_ReturnsNotFound()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var id = Guid.NewGuid();

        var read = await client.GetAsync($"/sessions/{id}/world");
        var advance = await client.PostAsJsonAsync(
            $"/sessions/{id}/advance",
            new AdvanceTimeRequest(60));

        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, advance.StatusCode);
    }

    [Fact]
    public async Task NegativeAdvance_ReturnsBadRequestWithoutChangingState()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var created = await (await client.PostAsJsonAsync("/sessions", new CreateSessionRequest([])))
            .Content.ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(created);

        var response = await client.PostAsJsonAsync(
            $"/sessions/{created.SessionId}/advance",
            new AdvanceTimeRequest(-1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var unchanged = await client.GetFromJsonAsync<WorldResponse>(
            $"/sessions/{created.SessionId}/world");

        Assert.NotNull(unchanged);
        Assert.Equal(0, unchanged.CurrentTimeSeconds);
        Assert.Empty(unchanged.Planets);
    }
    [Fact]
    public async Task SessionResource_ReturnsRuntimeMetadata()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var created =
            await (await client.PostAsJsonAsync("/sessions", new CreateSessionRequest([])))
                .Content
                .ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(created);

        var session =
            await client.GetFromJsonAsync<SessionResponse>(
                $"/sessions/{created.SessionId}");

        Assert.NotNull(session);
        Assert.Equal(created, session);
    }

    [Fact]
    public async Task PauseAndResume_UpdateSessionState()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var created =
            await (await client.PostAsJsonAsync("/sessions", new CreateSessionRequest([])))
                .Content
                .ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(created);
        Assert.False(created.IsPaused);

        var pauseResponse =
            await client.PostAsync(
                $"/sessions/{created.SessionId}/pause",
                null);

        Assert.Equal(
            HttpStatusCode.OK,
            pauseResponse.StatusCode);

        var paused =
            await pauseResponse.Content
                .ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(paused);
        Assert.True(paused.IsPaused);

        var resumeResponse =
            await client.PostAsync(
                $"/sessions/{created.SessionId}/resume",
                null);

        Assert.Equal(
            HttpStatusCode.OK,
            resumeResponse.StatusCode);

        var resumed =
            await resumeResponse.Content
                .ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(resumed);
        Assert.False(resumed.IsPaused);
    }

    [Fact]
    public async Task ExplicitAdvance_WorksWhileSessionIsPaused()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var created =
            await (await client.PostAsJsonAsync("/sessions", new CreateSessionRequest([])))
                .Content
                .ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(created);

        await client.PostAsync(
            $"/sessions/{created.SessionId}/pause",
            null);

        var advanceResponse =
            await client.PostAsJsonAsync(
                $"/sessions/{created.SessionId}/advance",
                new AdvanceTimeRequest(60));

        Assert.Equal(
            HttpStatusCode.OK,
            advanceResponse.StatusCode);

        var advanced =
            await advanceResponse.Content
                .ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(advanced);
        Assert.True(advanced.IsPaused);
        Assert.Equal(
            60,
            advanced.CurrentTimeSeconds);
        Assert.Equal(
            1,
            advanced.EventCount);
    }

    [Fact]
    public async Task UnknownSessionControls_ReturnNotFound()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var id =
            Guid.NewGuid();

        var get =
            await client.GetAsync(
                $"/sessions/{id}");

        var pause =
            await client.PostAsync(
                $"/sessions/{id}/pause",
                null);

        var resume =
            await client.PostAsync(
                $"/sessions/{id}/resume",
                null);

        Assert.Equal(
            HttpStatusCode.NotFound,
            get.StatusCode);

        Assert.Equal(
            HttpStatusCode.NotFound,
            pause.StatusCode);

        Assert.Equal(
            HttpStatusCode.NotFound,
            resume.StatusCode);
    }

    [Fact]
    public async Task TimelineResource_ExposesRecordedHistory()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var created =
            await (await client.PostAsJsonAsync("/sessions", new CreateSessionRequest([])))
                .Content
                .ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(created);

        var initial =
            await client.GetFromJsonAsync<TimelineResponse>(
                $"/sessions/{created.SessionId}/timeline");

        Assert.NotNull(initial);
        Assert.Equal(created.TimelineId, initial.TimelineId);
        Assert.Equal(created.WorldId, initial.WorldId);
        Assert.Null(initial.ParentTimelineId);
        Assert.Null(initial.ParentCheckpointId);
        Assert.Equal(0, initial.CurrentTimeSeconds);
        Assert.Single(initial.Checkpoints);
        Assert.Equal(0, initial.Checkpoints[0].TimeSeconds);
        Assert.Empty(initial.Events);

        await client.PostAsJsonAsync(
            $"/sessions/{created.SessionId}/advance",
            new AdvanceTimeRequest(60));

        var timeline =
            await client.GetFromJsonAsync<TimelineResponse>(
                $"/sessions/{created.SessionId}/timeline");

        Assert.NotNull(timeline);
        Assert.Equal(created.TimelineId, timeline.TimelineId);
        Assert.Equal(60, timeline.CurrentTimeSeconds);
        Assert.Single(timeline.Checkpoints);
        Assert.Single(timeline.Events);

        var timelineEvent = timeline.Events[0];

        Assert.NotEqual(Guid.Empty, timelineEvent.EventId);
        Assert.Equal(60, timelineEvent.OccurredAtSeconds);
        Assert.Equal(60, timelineEvent.ElapsedSeconds);
        Assert.False(string.IsNullOrWhiteSpace(timelineEvent.Cause));
        Assert.False(string.IsNullOrWhiteSpace(timelineEvent.Summary));
        Assert.Null(timelineEvent.AffectedPlanetId);
        Assert.NotNull(timelineEvent.Metrics);
    }

    [Fact]
    public async Task UnknownTimelineSession_ReturnsNotFound()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var response =
            await client.GetAsync(
                $"/sessions/{Guid.NewGuid()}/timeline");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateSession_WithPlanet_PreservesPhysicalState()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateSessionRequest(
                [
                    new PlanetCreationRequest(
                        "Test Earth",
                        5.9722e24,
                        6_371_000,
                        new PlanetEnvironmentCreationRequest(
                            288.15,
                            0.71,
                            0.10,
                            new AtmosphereCreationRequest(
                                101_325,
                                new Dictionary<string, double>
                                {
                                    ["N2"] = 0.78,
                                    ["O2"] = 0.21,
                                    ["Ar"] = 0.01
                                })))
                ]);

        var createResponse =
            await client.PostAsJsonAsync(
                "/sessions",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(created);
        Assert.Equal(1, created.PlanetCount);

        var world =
            await client.GetFromJsonAsync<WorldResponse>(
                $"/sessions/{created.SessionId}/world");

        Assert.NotNull(world);
        Assert.Single(world.Planets);

        var planet =
            world.Planets[0];

        Assert.NotEqual(
            Guid.Empty,
            planet.PlanetId);

        Assert.Equal(
            "Test Earth",
            planet.Name);

        Assert.Equal(
            5.9722e24,
            planet.MassKilograms);

        Assert.Equal(
            6_371_000,
            planet.MeanRadiusMeters);

        Assert.True(
            planet.SurfaceGravityMetersPerSecondSquared > 0);

        Assert.Equal(
            288.15,
            planet.Environment.MeanSurfaceTemperatureKelvin);

        Assert.Equal(
            0.71,
            planet.Environment.SurfaceWaterFraction);

        Assert.Equal(
            0.10,
            planet.Environment.IceCoverageFraction);

        Assert.Equal(
            101_325,
            planet.Environment.Atmosphere.SurfacePressurePascals);

        Assert.Equal(
            0.78,
            planet.Environment.Atmosphere
                .CompositionByMoleFraction["N2"]);

        Assert.Equal(
            0.21,
            planet.Environment.Atmosphere
                .CompositionByMoleFraction["O2"]);

        Assert.Equal(
            0.01,
            planet.Environment.Atmosphere
                .CompositionByMoleFraction["Ar"]);
    }

    [Fact]
    public async Task CreateSession_WithGeneratedHydrology_AdvancesWaterCycle()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateSessionRequest(
            [
                new PlanetCreationRequest(
                    "Hydrology World",
                    5.0e24,
                    6_000_000,
                    new PlanetEnvironmentCreationRequest(
                        288,
                        0.60,
                        0.05,
                        new AtmosphereCreationRequest(
                            100_000,
                            new Dictionary<string, double>
                            {
                                ["N2"] = 1
                            })),
                    GeneratedTerrain:
                        new GeneratedTerrainCreationRequest(
                            Seed: 42,
                            LatitudeBandCount: 4,
                            LongitudeBandCount: 8,
                            PlateCount: 4,
                            ContinentalPlateFraction: 0.45),
                    GeneratedHydrology:
                        new GeneratedHydrologyCreationRequest(
                            1.0e19),
                    HydrologyModel:
                        new HydrologyModelRequest())
            ]);

        var createResponse =
            await client.PostAsJsonAsync(
                "/sessions",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(
            created);

        var advanceResponse =
            await client.PostAsJsonAsync(
                $"/sessions/{created.SessionId}/advance",
                new AdvanceTimeRequest(
                    86_400));

        Assert.Equal(
            HttpStatusCode.OK,
            advanceResponse.StatusCode);

        var timeline =
            await client.GetFromJsonAsync<TimelineResponse>(
                $"/sessions/{created.SessionId}/timeline");

        Assert.NotNull(
            timeline);

        var hydrologyEvent =
            Assert.Single(
                timeline.Events,
                timelineEvent =>
                    timelineEvent.Cause ==
                    "planetary-hydrology");

        Assert.Equal(
            86_400,
            hydrologyEvent.ElapsedSeconds);

        Assert.True(
            hydrologyEvent.Metrics.ContainsKey(
                "initialWaterMassKilograms"));

        Assert.True(
            hydrologyEvent.Metrics.ContainsKey(
                "finalWaterMassKilograms"));

        Assert.InRange(
            Math.Abs(
                hydrologyEvent.Metrics[
                    "relativeWaterMassConservationError"]),
            0,
            1e-12);
    }

    [Fact]
    public async Task HydrologyEndpoint_ExposesAuthoritativePlanetWaterState()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        const double requestedInventory =
            1.0e19;

        var request =
            new CreateSessionRequest(
            [
                new PlanetCreationRequest(
                    "Hydrology API World",
                    5.0e24,
                    6_000_000,
                    new PlanetEnvironmentCreationRequest(
                        288,
                        0.60,
                        0.05,
                        new AtmosphereCreationRequest(
                            100_000,
                            new Dictionary<string, double>
                            {
                                ["N2"] = 1
                            })),
                    GeneratedTerrain:
                        new GeneratedTerrainCreationRequest(
                            Seed: 84,
                            LatitudeBandCount: 4,
                            LongitudeBandCount: 8,
                            PlateCount: 4,
                            ContinentalPlateFraction: 0.45),
                    GeneratedHydrology:
                        new GeneratedHydrologyCreationRequest(
                            requestedInventory),
                    HydrologyModel:
                        new HydrologyModelRequest())
            ]);

        var createResponse =
            await client.PostAsJsonAsync(
                "/sessions",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(
            created);

        var world =
            await client.GetFromJsonAsync<WorldResponse>(
                $"/sessions/{created.SessionId}/world");

        Assert.NotNull(
            world);

        var planet =
            Assert.Single(
                world.Planets);

        var hydrology =
            await client.GetFromJsonAsync<HydrologyResponse>(
                $"/sessions/{created.SessionId}/planets/{planet.PlanetId}/hydrology");

        Assert.NotNull(
            hydrology);

        Assert.Equal(
            planet.PlanetId,
            hydrology.PlanetId);

        Assert.Equal(
            "LatitudeLongitude",
            hydrology.Grid.Kind);

        Assert.True(
            hydrology.Grid.IdentityVersion >
            0);

        Assert.Equal(
            4,
            hydrology.Grid.LatitudeBandCount);

        Assert.Equal(
            8,
            hydrology.Grid.LongitudeBandCount);

        Assert.Equal(
            32,
            hydrology.Cells.Length);

        Assert.Equal(
            32,
            hydrology.Cells
                .Select(
                    cell =>
                        cell.CellId)
                .Distinct()
                .Count());

        Assert.All(
            hydrology.Cells,
            cell =>
            {
                Assert.NotEqual(
                    Guid.Empty,
                    cell.CellId);

                Assert.True(
                    cell.AtmosphericWaterKilogramsPerSquareMeter >=
                    0);

                Assert.True(
                    cell.SurfaceLiquidWaterKilogramsPerSquareMeter >=
                    0);

                Assert.True(
                    cell.SoilWaterKilogramsPerSquareMeter >=
                    0);

                Assert.True(
                    cell.SnowIceWaterEquivalentKilogramsPerSquareMeter >=
                    0);
            });

        var relativeError =
            Math.Abs(
                hydrology.TotalWaterMassKilograms -
                requestedInventory) /
            requestedInventory;

        Assert.InRange(
            relativeError,
            0,
            1e-12);

        var initialTotalWaterMass =
            hydrology.TotalWaterMassKilograms;

        var advanceResponse =
            await client.PostAsJsonAsync(
                $"/sessions/{created.SessionId}/advance",
                new AdvanceTimeRequest(
                    86_400));

        Assert.Equal(
            HttpStatusCode.OK,
            advanceResponse.StatusCode);

        var advancedHydrology =
            await client.GetFromJsonAsync<HydrologyResponse>(
                $"/sessions/{created.SessionId}/planets/{planet.PlanetId}/hydrology");

        Assert.NotNull(
            advancedHydrology);

        var advancedRelativeError =
            Math.Abs(
                advancedHydrology.TotalWaterMassKilograms -
                initialTotalWaterMass) /
            initialTotalWaterMass;

        Assert.InRange(
            advancedRelativeError,
            0,
            1e-12);
    }

    [Fact]
    public async Task HydrologyEndpoint_WithoutPlanetHydrology_ReturnsNotFound()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateSessionRequest(
            [
                new PlanetCreationRequest(
                    "Dry API World",
                    5.0e24,
                    6_000_000,
                    new PlanetEnvironmentCreationRequest(
                        288,
                        0,
                        0,
                        new AtmosphereCreationRequest(
                            0,
                            new Dictionary<string, double>())))
            ]);

        var createResponse =
            await client.PostAsJsonAsync(
                "/sessions",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(
            created);

        var world =
            await client.GetFromJsonAsync<WorldResponse>(
                $"/sessions/{created.SessionId}/world");

        Assert.NotNull(
            world);

        var planet =
            Assert.Single(
                world.Planets);

        var response =
            await client.GetAsync(
                $"/sessions/{created.SessionId}/planets/{planet.PlanetId}/hydrology");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateSession_WithInvalidPlanet_ReturnsBadRequest()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateSessionRequest(
                [
                    new PlanetCreationRequest(
                        "Impossible",
                        -1,
                        1,
                        new PlanetEnvironmentCreationRequest(
                            300,
                            0,
                            0,
                            new AtmosphereCreationRequest(
                                0,
                                new Dictionary<string, double>())))
                ]);

        var response =
            await client.PostAsJsonAsync(
                "/sessions",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateSession_WithMissingEnvironment_ReturnsBadRequest()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/sessions",
                new
                {
                    planets = new[]
                    {
                        new
                        {
                            name = "Incomplete",
                            massKilograms = 1,
                            meanRadiusMeters = 1
                        }
                    }
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateSession_WithMissingComposition_ReturnsBadRequest()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/sessions",
                new
                {
                    planets = new[]
                    {
                        new
                        {
                            name = "Incomplete",
                            massKilograms = 1,
                            meanRadiusMeters = 1,
                            environment = new
                            {
                                meanSurfaceTemperatureKelvin = 300,
                                surfaceWaterFraction = 0,
                                iceCoverageFraction = 0,
                                atmosphere = new
                                {
                                    surfacePressurePascals = 0
                                }
                            }
                        }
                    }
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateSession_WithEmptyPlanets_CreatesEmptyWorld()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/sessions",
                new CreateSessionRequest([]));

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var created =
            await response.Content
                .ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(created);
        Assert.Equal(0, created.PlanetCount);

        var world =
            await client.GetFromJsonAsync<WorldResponse>(
                $"/sessions/{created.SessionId}/world");

        Assert.NotNull(world);
        Assert.Empty(world.Planets);
    }

    [Fact]
    public async Task CreateSession_WithOmittedScalar_ReturnsBadRequest()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/sessions",
                new
                {
                    planets = new[]
                    {
                        new
                        {
                            name = "Incomplete",
                            massKilograms = 1,
                            meanRadiusMeters = 1,
                            environment = new
                            {
                                meanSurfaceTemperatureKelvin = 300,
                                // SurfaceWaterFraction intentionally omitted.
                                iceCoverageFraction = 0,
                                atmosphere = new
                                {
                                    surfacePressurePascals = 0,
                                    compositionByMoleFraction =
                                        new Dictionary<string, double>()
                                }
                            }
                        }
                    }
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Advance_WhenTimeOverflows_ReturnsBadRequestWithoutChangingState()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var manager =
            factory.Services.GetRequiredService<SimulationSessionManager>();

        var initialWorld =
            new WorldState(
                WorldId.New(),
                new SimulationTime(long.MaxValue - 1));

        var sessionId =
            manager.Create(initialWorld);

        var response =
            await client.PostAsJsonAsync(
                $"/sessions/{sessionId.Value}/advance",
                new AdvanceTimeRequest(2));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var session =
            manager.Get(sessionId);

        Assert.Equal(
            long.MaxValue - 1,
            session.CurrentWorld.CurrentTime.TotalSeconds);

        Assert.Empty(
            session.Timeline.Events);
    }

    [Fact]
    public async Task ReplaceEnvironment_UpdatesWorldAndRecordsIntervention()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var creation =
            await client.PostAsJsonAsync(
                "/sessions",
                new CreateSessionRequest(
                [
                    new PlanetCreationRequest(
                        "Test Planet",
                        5.9722e24,
                        6_371_000,
                        new PlanetEnvironmentCreationRequest(
                            288.15,
                            0.71,
                            0.1,
                            new AtmosphereCreationRequest(
                                0,
                                new Dictionary<string, double>())))
                ]));

        creation.EnsureSuccessStatusCode();

        var created =
            await creation.Content.ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(created);

        var originalWorld =
            await client.GetFromJsonAsync<WorldResponse>(
                $"/sessions/{created.SessionId}/world");

        Assert.NotNull(originalWorld);

        var originalPlanet =
            Assert.Single(originalWorld.Planets);

        var response =
            await client.PostAsJsonAsync(
                $"/sessions/{created.SessionId}/planets/{originalPlanet.PlanetId}/environment",
                new ReplacePlanetEnvironmentRequest(
                    300,
                    0.65,
                    0.02,
                    new AtmosphereReplacementRequest(
                        0,
                        new Dictionary<string, double>())));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var world =
            await response.Content.ReadFromJsonAsync<WorldResponse>();

        Assert.NotNull(world);

        var planet =
            Assert.Single(world.Planets);

        Assert.Equal(originalWorld.WorldId, world.WorldId);
        Assert.Equal(originalPlanet.PlanetId, planet.PlanetId);
        Assert.Equal(originalPlanet.Name, planet.Name);
        Assert.Equal(originalPlanet.MassKilograms, planet.MassKilograms);
        Assert.Equal(originalPlanet.MeanRadiusMeters, planet.MeanRadiusMeters);
        Assert.Equal(300, planet.Environment.MeanSurfaceTemperatureKelvin);
        Assert.Equal(0.65, planet.Environment.SurfaceWaterFraction);
        Assert.Equal(0.02, planet.Environment.IceCoverageFraction);
        Assert.Equal(0, world.CurrentTimeSeconds);

        var timeline =
            await client.GetFromJsonAsync<TimelineResponse>(
                $"/sessions/{created.SessionId}/timeline");

        Assert.NotNull(timeline);

        var intervention =
            Assert.Single(timeline.Events);

        Assert.Equal(originalPlanet.PlanetId, intervention.AffectedPlanetId);
        Assert.Equal(0, intervention.ElapsedSeconds);
        Assert.Equal(0, intervention.OccurredAtSeconds);
        Assert.Equal("User intervention", intervention.Cause);
    }

    [Fact]
    public async Task ReplaceEnvironment_UnknownPlanet_ReturnsNotFound()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var creation =
            await client.PostAsJsonAsync(
                "/sessions",
                new CreateSessionRequest([]));

        creation.EnsureSuccessStatusCode();

        var created =
            await creation.Content.ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(created);

        var response =
            await client.PostAsJsonAsync(
                $"/sessions/{created.SessionId}/planets/{Guid.NewGuid()}/environment",
                new ReplacePlanetEnvironmentRequest(
                    300,
                    0.5,
                    0,
                    new AtmosphereReplacementRequest(
                        0,
                        new Dictionary<string, double>())));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var timeline =
            await client.GetFromJsonAsync<TimelineResponse>(
                $"/sessions/{created.SessionId}/timeline");

        Assert.NotNull(timeline);
        Assert.Empty(timeline.Events);
    }

    [Fact]
    public async Task ReplaceEnvironment_InvalidValues_ReturnsBadRequest()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var creation =
            await client.PostAsJsonAsync(
                "/sessions",
                new CreateSessionRequest([]));

        creation.EnsureSuccessStatusCode();

        var created =
            await creation.Content.ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(created);

        var response =
            await client.PostAsJsonAsync(
                $"/sessions/{created.SessionId}/planets/{Guid.NewGuid()}/environment",
                new ReplacePlanetEnvironmentRequest(
                    -1,
                    0.5,
                    0,
                    new AtmosphereReplacementRequest(
                        0,
                        new Dictionary<string, double>())));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Theory]
    [InlineData("""{"meanSurfaceTemperatureKelvin":300,"surfaceWaterFraction":0.5,"iceCoverageFraction":0,"atmosphere":null}""")]
    [InlineData("""{"meanSurfaceTemperatureKelvin":300,"surfaceWaterFraction":0.5,"iceCoverageFraction":0,"atmosphere":{"surfacePressurePascals":0,"compositionByMoleFraction":null}}""")]
    [InlineData("""{"meanSurfaceTemperatureKelvin":300,"iceCoverageFraction":0,"atmosphere":{"surfacePressurePascals":0,"compositionByMoleFraction":{}}}""")]
    [InlineData("""{"meanSurfaceTemperatureKelvin":300,"surfaceWaterFraction":0.5,"iceCoverageFraction":0,"atmosphere":{"surfacePressurePascals":0}}""")]
    public async Task ReplaceEnvironment_MalformedRequest_ReturnsBadRequest(
        string json)
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var created = await (await client.PostAsJsonAsync(
            "/sessions",
            new CreateSessionRequest([])))
            .Content.ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(created);

        using var content = new StringContent(
            json,
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync(
            $"/sessions/{created.SessionId}/planets/{Guid.NewGuid()}/environment",
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReplaceEnvironment_InvalidValues_PreservesExistingWorldAndTimeline()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var creation = await client.PostAsJsonAsync(
            "/sessions",
            new CreateSessionRequest(
            [
                new PlanetCreationRequest(
                    "Test Planet",
                    5.9722e24,
                    6_371_000,
                    new PlanetEnvironmentCreationRequest(
                        288.15,
                        0.71,
                        0.1,
                        new AtmosphereCreationRequest(
                            0,
                            new Dictionary<string, double>())))
            ]));

        creation.EnsureSuccessStatusCode();

        var created = await creation.Content
            .ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(created);

        var before = await client.GetFromJsonAsync<WorldResponse>(
            $"/sessions/{created.SessionId}/world");

        Assert.NotNull(before);
        var planet = Assert.Single(before.Planets);

        var response = await client.PostAsJsonAsync(
            $"/sessions/{created.SessionId}/planets/{planet.PlanetId}/environment",
            new ReplacePlanetEnvironmentRequest(
                -1,
                0.5,
                0,
                new AtmosphereReplacementRequest(
                    0,
                    new Dictionary<string, double>())));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var after = await client.GetFromJsonAsync<WorldResponse>(
            $"/sessions/{created.SessionId}/world");

        Assert.NotNull(after);
        Assert.Equal(before.WorldId, after.WorldId);
        Assert.Equal(before.CurrentTimeSeconds, after.CurrentTimeSeconds);

        var unchanged = Assert.Single(after.Planets);
        Assert.Equal(planet.PlanetId, unchanged.PlanetId);
        Assert.Equal(
            planet.Environment.MeanSurfaceTemperatureKelvin,
            unchanged.Environment.MeanSurfaceTemperatureKelvin);
        Assert.Equal(
            planet.Environment.SurfaceWaterFraction,
            unchanged.Environment.SurfaceWaterFraction);
        Assert.Equal(
            planet.Environment.IceCoverageFraction,
            unchanged.Environment.IceCoverageFraction);

        var timeline = await client.GetFromJsonAsync<TimelineResponse>(
            $"/sessions/{created.SessionId}/timeline");

        Assert.NotNull(timeline);
        Assert.Equal(created.TimelineId, timeline.TimelineId);
        Assert.Empty(timeline.Events);
        Assert.Single(timeline.Checkpoints);
    }


    [Fact]
    public async Task CreateAndAdvance_WithEnergyBalanceModelChangesPlanet()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateSessionRequest(
            [
                new PlanetCreationRequest(
                    "Earth",
                    5.9722e24,
                    6_371_000,
                    new PlanetEnvironmentCreationRequest(
                        288.15,
                        0.71,
                        0.03,
                        new AtmosphereCreationRequest(
                            0,
                            new Dictionary<string, double>())),
                    new PlanetaryEnergyBalanceModelRequest(
                        1361,
                        0.61,
                        1.0e8,
                        0.30,
                        0.60,
                        263.15,
                        273.15,
                        31_536_000))
            ]);

        var createResponse =
            await client.PostAsJsonAsync(
                "/sessions",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(created);

        var before =
            await client.GetFromJsonAsync<WorldResponse>(
                $"/sessions/{created.SessionId}/world");

        Assert.NotNull(before);

        var originalPlanet =
            Assert.Single(before.Planets);

        var advanceResponse =
            await client.PostAsJsonAsync(
                $"/sessions/{created.SessionId}/advance",
                new AdvanceTimeRequest(3600));

        Assert.Equal(
            HttpStatusCode.OK,
            advanceResponse.StatusCode);

        var advanced =
            await advanceResponse.Content
                .ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(advanced);
        Assert.Equal(3600, advanced.CurrentTimeSeconds);
        Assert.Equal(1, advanced.EventCount);

        var after =
            await client.GetFromJsonAsync<WorldResponse>(
                $"/sessions/{created.SessionId}/world");

        Assert.NotNull(after);

        var changedPlanet =
            Assert.Single(after.Planets);

        Assert.Equal(
            originalPlanet.PlanetId,
            changedPlanet.PlanetId);

        Assert.NotEqual(
            originalPlanet.Environment
                .MeanSurfaceTemperatureKelvin,
            changedPlanet.Environment
                .MeanSurfaceTemperatureKelvin);

        Assert.NotEqual(
            originalPlanet.Environment
                .IceCoverageFraction,
            changedPlanet.Environment
                .IceCoverageFraction);
    }



    [Fact]
    public async Task DefinitionResource_ReturnsConfiguredEnergyBalanceModel()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateSessionRequest(
            [
                new PlanetCreationRequest(
                    "Earth",
                    5.9722e24,
                    6_371_000,
                    new PlanetEnvironmentCreationRequest(
                        288.15,
                        0.71,
                        0.03,
                        new AtmosphereCreationRequest(
                            0,
                            new Dictionary<string, double>())),
                    new PlanetaryEnergyBalanceModelRequest(
                        1361,
                        0.61,
                        1.0e8,
                        0.30,
                        0.60,
                        263.15,
                        273.15,
                        31_536_000))
            ]);

        var created =
            await (await client.PostAsJsonAsync(
                    "/sessions",
                    request))
                .Content
                .ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(created);

        var world =
            await client.GetFromJsonAsync<WorldResponse>(
                $"/sessions/{created.SessionId}/world");

        var definition =
            await client.GetFromJsonAsync<
                SimulationDefinitionResponse>(
                $"/sessions/{created.SessionId}/definition");

        Assert.NotNull(world);
        Assert.NotNull(definition);

        var planet =
            Assert.Single(world.Planets);

        var model =
            Assert.Single(
                definition.PlanetaryEnergyBalanceModels);

        Assert.Equal(
            planet.PlanetId,
            model.PlanetId);

        Assert.Equal(
            1361,
            model.StellarFluxWattsPerSquareMeter);

        Assert.Equal(
            0.61,
            model.EffectiveLongwaveEmissivity);

        Assert.Equal(
            1.0e8,
            model.EffectiveHeatCapacityJoulesPerSquareMeterKelvin);

        Assert.Equal(
            0.30,
            model.IceFreeAlbedo);

        Assert.Equal(
            0.60,
            model.IceAlbedo);

        Assert.Equal(
            263.15,
            model.FullIceTemperatureKelvin);

        Assert.Equal(
            273.15,
            model.IceFreeTemperatureKelvin);

        Assert.Equal(
            31_536_000,
            model.IceResponseTimescaleSeconds);
    }

    [Fact]
    public async Task Create_WithInvalidEnergyBalanceModel_ReturnsBadRequest()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateSessionRequest(
            [
                new PlanetCreationRequest(
                    "Earth",
                    5.9722e24,
                    6_371_000,
                    new PlanetEnvironmentCreationRequest(
                        288.15,
                        0.71,
                        0.03,
                        new AtmosphereCreationRequest(
                            0,
                            new Dictionary<string, double>())),
                    new PlanetaryEnergyBalanceModelRequest(
                        1361,
                        0.61,
                        0,
                        0.30,
                        0.60,
                        263.15,
                        273.15,
                        31_536_000))
            ]);

        var response =
            await client.PostAsJsonAsync(
                "/sessions",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }


    [Fact]
    public async Task CreateSession_WithSyntheticFood_ExposesFoodResources()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateSessionRequest(
            [
                new PlanetCreationRequest(
                    "Earth",
                    5.9722e24,
                    6_371_000,
                    new PlanetEnvironmentCreationRequest(
                        288.15,
                        0.71,
                        0.03,
                        new AtmosphereCreationRequest(
                            101_325,
                            new Dictionary<string, double>
                            {
                                ["N2"] = 0.7808,
                                ["O2"] = 0.2095,
                                ["Ar"] = 0.0093,
                                ["CO2"] = 0.0004
                            })),
                    SyntheticFood:
                        new SyntheticFoodCreationRequest(
                            40,
                            84,
                            0,
                            25,
                            3,
                            100,
                            RecoveryEnergyPerDay: 2))
            ]);

        var createResponse =
            await client.PostAsJsonAsync(
                "/sessions",
                request);

        var createBody =
            await createResponse.Content
                .ReadAsStringAsync();

        Assert.True(
            createResponse.StatusCode ==
            HttpStatusCode.Created,
            $"Expected Created but received " +
            $"{createResponse.StatusCode}: {createBody}");

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(created);

        var world =
            await client.GetFromJsonAsync<WorldResponse>(
                $"/sessions/{created.SessionId}/world");

        Assert.NotNull(world);

        var planet =
            Assert.Single(world.Planets);

        Assert.Equal(
            40,
            world.FoodResources.Length);

        Assert.All(
            world.FoodResources,
            resource =>
            {
                Assert.NotEqual(
                    Guid.Empty,
                    resource.FoodResourceId);

                Assert.Equal(
                    planet.PlanetId,
                    resource.PlanetId);

                Assert.InRange(
                    resource.LatitudeDegrees,
                    -3,
                    3);

                Assert.InRange(
                    resource.LongitudeDegrees,
                    22,
                    28);

                Assert.Equal(
                    100,
                    resource.AvailableEnergy);

                Assert.Equal(
                    100,
                    resource.CapacityEnergy);

                Assert.Equal(
                    2,
                    resource.RecoveryEnergyPerDay);
            });
    }


    [Fact]
    public async Task CreateSession_WithSyntheticPopulation_ExposesPopulationAndModel()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateSessionRequest(
            [
                new PlanetCreationRequest(
                    "Earth",
                    5.9722e24,
                    6_371_000,
                    new PlanetEnvironmentCreationRequest(
                        288.15,
                        0.71,
                        0.03,
                        new AtmosphereCreationRequest(
                            101_325,
                            new Dictionary<string, double>
                            {
                                ["N2"] = 0.7808,
                                ["O2"] = 0.2095,
                                ["Ar"] = 0.0093,
                                ["CO2"] = 0.0004
                            })),
                    null,
                    new SyntheticPopulationCreationRequest(
                        100,
                        42,
                        0,
                        25,
                        3))
            ]);

        var createResponse =
            await client.PostAsJsonAsync(
                "/sessions",
                request);

        var createBody =
            await createResponse.Content.ReadAsStringAsync();

        Assert.True(
            createResponse.StatusCode ==
            HttpStatusCode.Created,
            $"Expected Created but received " +
            $"{createResponse.StatusCode}: {createBody}");

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(created);

        var world =
            await client.GetFromJsonAsync<WorldResponse>(
                $"/sessions/{created.SessionId}/world");

        var definition =
            await client.GetFromJsonAsync<
                SimulationDefinitionResponse>(
                $"/sessions/{created.SessionId}/definition");

        Assert.NotNull(world);
        Assert.NotNull(definition);

        var planet =
            Assert.Single(world.Planets);

        Assert.Equal(
            100,
            world.Population.Length);

        Assert.All(
            world.Population,
            person =>
            {
                Assert.Equal(
                    planet.PlanetId,
                    person.PlanetId);

                Assert.InRange(
                    person.LatitudeDegrees,
                    -3,
                    3);

                Assert.InRange(
                    person.LongitudeDegrees,
                    22,
                    28);

                Assert.Null(
                    person.ParentId);

                Assert.Equal(
                    "Idle",
                    person.Activity);

                Assert.Equal(
                    1,
                    person.EnergyReserve);

                Assert.Equal(
                    1,
                    person.Health);
            });

        var populationModel =
            Assert.Single(
                definition.PopulationModels);

        Assert.Equal(
            planet.PlanetId,
            populationModel.PlanetId);

        Assert.Equal(
            42,
            populationModel.Seed);
    }


}
