using System.Net;
using System.Net.Http.Json;
using Est.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Est.Api.Tests.Sessions;

public sealed class GrazerCohortEndpointTests
{
    [Fact]
    public async Task CreateSession_WithGrazers_CreatesRunsAndExposesModel()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateSessionRequest(
            [
                new PlanetCreationRequest(
                    "Grazer World",
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
                            0),
                    GeneratedVegetation:
                        new GeneratedVegetationCreationRequest(
                            1),
                    GeneratedGrazers:
                        new GeneratedGrazerCreationRequest(
                            CarryingCapacityGrazersPerKilogramLiveVegetationBiomass:
                                0.000000000001,
                            InitialFractionOfLocalCarryingCapacity:
                                0.25,
                            MinimumInitialCohortMemberCount:
                                1,
                            MaximumInitialCohortCount:
                                3,
                            LiveBiomassKilogramsPerGrazer:
                                320,
                            LiveNitrogenKilogramsPerGrazer:
                                8),
                    GrazerModel:
                        new GrazerModelRequest
                        {
                            CarryingCapacityGrazersPerKilogramLiveVegetationBiomass =
                                0.000000000001,
                            InitialFractionOfLocalCarryingCapacity =
                                0.25,
                            MinimumInitialCohortMemberCount =
                                1,
                            MaximumInitialCohortCount =
                                3,
                            MaximumIntegrationStepSeconds =
                                3_600,
                            MaximumTravelMetersPerDay =
                                0,
                            MaximumGrazeKilogramsPerGrazerPerDay =
                                24,
                            FoodShortageMortalityRatePerDay =
                                0,
                            WaterAbsenceMortalityRatePerDay =
                                0,
                            HabitatAbsenceMortalityRatePerDay =
                                0,
                            LiveBiomassKilogramsPerGrazer =
                                300,
                            LiveNitrogenKilogramsPerGrazer =
                                7.5
                        })
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

        var definition =
            await client.GetFromJsonAsync<
                SimulationDefinitionResponse>(
                $"/sessions/{created.SessionId}/definition");

        Assert.NotNull(
            definition);

        var model =
            Assert.Single(
                definition.GrazerModels);

        Assert.Equal(
            0.000000000001,
            model.CarryingCapacityGrazersPerKilogramLiveVegetationBiomass);

        Assert.Equal(
            0.25,
            model.InitialFractionOfLocalCarryingCapacity);

        Assert.Equal(
            1,
            model.MinimumInitialCohortMemberCount);

        Assert.Equal(
            3,
            model.MaximumInitialCohortCount);

        Assert.Equal(
            3_600,
            model.MaximumIntegrationStepSeconds);

        Assert.Equal(
            0,
            model.MaximumTravelMetersPerDay);

        Assert.Equal(
            24,
            model.MaximumGrazeKilogramsPerGrazerPerDay);

        Assert.Equal(
            0,
            model.FoodShortageMortalityRatePerDay);

        Assert.Equal(
            0,
            model.WaterAbsenceMortalityRatePerDay);

        Assert.Equal(
            0,
            model.HabitatAbsenceMortalityRatePerDay);

        Assert.Equal(
            300,
            model.LiveBiomassKilogramsPerGrazer);

        Assert.Equal(
            7.5,
            model.LiveNitrogenKilogramsPerGrazer);

        var world =
            await client.GetFromJsonAsync<WorldResponse>(
                $"/sessions/{created.SessionId}/world");

        Assert.NotNull(
            world);

        var planetId =
            Assert.Single(
                    world.Planets)
                .PlanetId;

        var before =
            await client.GetFromJsonAsync<GrazerCohortsResponse>(
                $"/sessions/{created.SessionId}/planets/{planetId}/grazer-cohorts");

        Assert.NotNull(
            before);

        Assert.Equal(
            planetId,
            before.PlanetId);

        Assert.Equal(
            3,
            before.Cohorts.Length);

        Assert.All(
            before.Cohorts,
            cohort =>
            {
                Assert.True(
                    cohort.MemberCount > 0);

                Assert.Equal(
                    cohort.MemberCount * 320,
                    cohort.Material.LiveBiomassKilograms);

                Assert.Equal(
                    cohort.MemberCount * 8,
                    cohort.Material.LiveNitrogenKilograms);
            });

        var beforeById =
            before.Cohorts.ToDictionary(
                cohort =>
                    cohort.CohortId);

        var advanceResponse =
            await client.PostAsJsonAsync(
                $"/sessions/{created.SessionId}/advance",
                new AdvanceTimeRequest(
                    3_600));

        Assert.Equal(
            HttpStatusCode.OK,
            advanceResponse.StatusCode);

        var timeline =
            await client.GetFromJsonAsync<TimelineResponse>(
                $"/sessions/{created.SessionId}/timeline");

        Assert.NotNull(
            timeline);

        var grazerEvent =
            Assert.Single(
                timeline.Events,
                timelineEvent =>
                    timelineEvent.Cause ==
                    "planetary-grazers");

        Assert.Equal(
            3_600,
            grazerEvent.ElapsedSeconds);

        var after =
            await client.GetFromJsonAsync<GrazerCohortsResponse>(
                $"/sessions/{created.SessionId}/planets/{planetId}/grazer-cohorts");

        Assert.NotNull(
            after);

        Assert.Equal(
            before.Cohorts.Length,
            after.Cohorts.Length);

        foreach (var cohort in after.Cohorts)
        {
            var original =
                beforeById[
                    cohort.CohortId];

            Assert.Equal(
                original.MemberCount,
                cohort.MemberCount);

            Assert.Equal(
                original.LatitudeDegrees,
                cohort.LatitudeDegrees);

            Assert.Equal(
                original.LongitudeDegrees,
                cohort.LongitudeDegrees);

            Assert.Equal(
                original.Material,
                cohort.Material);
        }
    }

    [Fact]
    public async Task GetGrazerCohorts_ExistingPlanetWithoutGrazers_ReturnsEmptyState()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var createResponse =
            await client.PostAsJsonAsync(
                "/sessions",
                new CreateSessionRequest(
                [
                    new PlanetCreationRequest(
                        "Grazerless World",
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
                                })))
                ]));

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

        var planetId =
            Assert.Single(
                    world.Planets)
                .PlanetId;

        var response =
            await client.GetAsync(
                $"/sessions/{created.SessionId}/planets/{planetId}/grazer-cohorts");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var state =
            await response.Content
                .ReadFromJsonAsync<GrazerCohortsResponse>();

        Assert.NotNull(
            state);

        Assert.Equal(
            planetId,
            state.PlanetId);

        Assert.Empty(
            state.Cohorts);
    }

    [Fact]
    public async Task CreateSession_GrazerModelWithoutVegetation_ReturnsBadRequest()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateSessionRequest(
            [
                new PlanetCreationRequest(
                    "Invalid Grazer World",
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
                    GrazerModel:
                        new GrazerModelRequest())
            ]);

        var response =
            await client.PostAsJsonAsync(
                "/sessions",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
}
