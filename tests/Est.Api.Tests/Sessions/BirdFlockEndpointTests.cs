using System.Net;
using System.Net.Http.Json;
using Est.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Est.Api.Tests.Sessions;

public sealed class BirdFlockEndpointTests
{
    [Fact]
    public async Task CreateSession_WithBirds_CreatesRunsAndExposesModel()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateSessionRequest(
            [
                new PlanetCreationRequest(
                    "Bird World",
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
                    GeneratedInvertebrates:
                        new GeneratedInvertebrateCreationRequest(
                            CarryingCapacityKilogramsPerKilogramLiveVegetation:
                                0.02,
                            InitialFractionOfLocalCarryingCapacity:
                                0.25),
                    InvertebrateModel:
                        new InvertebrateModelRequest
                        {
                            MaximumIntegrationStepSeconds =
                                3_600,
                            CarryingCapacityKilogramsPerKilogramLiveVegetation =
                                0.02,
                            InitialFractionOfLocalCarryingCapacity =
                                0.25,
                            MaximumRelativeGrowthRatePerDay =
                                0,
                            BaselineMortalityRatePerDay =
                                0
                        },
                    GeneratedBirds:
                        new GeneratedBirdCreationRequest(
                            CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass:
                                0.000001,
                            InitialFractionOfLocalCarryingCapacity:
                                0.25,
                            MinimumInitialFlockMemberCount:
                                1,
                            MaximumInitialFlockCount:
                                3,
                            LiveBiomassKilogramsPerBird:
                                0.8,
                            LiveNitrogenKilogramsPerBird:
                                0.02),
                    BirdModel:
                        new BirdModelRequest
                        {
                            CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass =
                                0.000001,
                            InitialFractionOfLocalCarryingCapacity =
                                0.25,
                            MinimumInitialFlockMemberCount =
                                1,
                            MaximumInitialFlockCount =
                                3,
                            MaximumIntegrationStepSeconds =
                                3_600,
                            MaximumTravelMetersPerDay =
                                0,
                            FoodShortageMortalityRatePerDay =
                                0,
                            WaterAbsenceMortalityRatePerDay =
                                0,
                            HabitatAbsenceMortalityRatePerDay =
                                0,
                            LiveBiomassKilogramsPerBird =
                                0.9,
                            LiveNitrogenKilogramsPerBird =
                                0.0225,
                            MaximumPreyConsumptionKilogramsPerBirdPerDay =
                                0.42,
                            MaximumRecruitmentRatePerDay =
                                0.015
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
                definition.BirdModels);

        Assert.Equal(
            0.000001,
            model.CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass);

        Assert.Equal(
            0.25,
            model.InitialFractionOfLocalCarryingCapacity);

        Assert.Equal(
            1,
            model.MinimumInitialFlockMemberCount);

        Assert.Equal(
            3,
            model.MaximumInitialFlockCount);

        Assert.Equal(
            3_600,
            model.MaximumIntegrationStepSeconds);

        Assert.Equal(
            0,
            model.MaximumTravelMetersPerDay);

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
            0.9,
            model.LiveBiomassKilogramsPerBird);

        Assert.Equal(
            0.0225,
            model.LiveNitrogenKilogramsPerBird);

        Assert.Equal(
            0.42,
            model.MaximumPreyConsumptionKilogramsPerBirdPerDay);

        Assert.Equal(
            0.015,
            model.MaximumRecruitmentRatePerDay);

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
            await client.GetFromJsonAsync<BirdFlocksResponse>(
                $"/sessions/{created.SessionId}/planets/{planetId}/bird-flocks");

        Assert.NotNull(
            before);

        Assert.Equal(
            planetId,
            before.PlanetId);

        Assert.Equal(
            3,
            before.Flocks.Length);

        Assert.All(
            before.Flocks,
            flock =>
            {
                Assert.True(
                    flock.MemberCount > 0);

                Assert.Equal(
                    flock.MemberCount * 0.8,
                    flock.Material.LiveBiomassKilograms);

                Assert.Equal(
                    flock.MemberCount * 0.02,
                    flock.Material.LiveNitrogenKilograms);

                Assert.Equal(
                    0,
                    flock.RecruitmentAccumulator);
            });

        var beforeById =
            before.Flocks.ToDictionary(
                flock =>
                    flock.FlockId);

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

        var birdEvent =
            Assert.Single(
                timeline.Events,
                timelineEvent =>
                    timelineEvent.Cause ==
                    "planetary-birds");

        Assert.Equal(
            3_600,
            birdEvent.ElapsedSeconds);

        var after =
            await client.GetFromJsonAsync<BirdFlocksResponse>(
                $"/sessions/{created.SessionId}/planets/{planetId}/bird-flocks");

        Assert.NotNull(
            after);

        Assert.Equal(
            before.Flocks.Length,
            after.Flocks.Length);

        foreach (var flock in after.Flocks)
        {
            var original =
                beforeById[flock.FlockId];

            Assert.Equal(
                original.MemberCount,
                flock.MemberCount);

            Assert.Equal(
                original.LatitudeDegrees,
                flock.LatitudeDegrees);

            Assert.Equal(
                original.LongitudeDegrees,
                flock.LongitudeDegrees);

            Assert.Equal(
                original.Material,
                flock.Material);
        }
    }

    [Fact]
    public async Task GetBirdFlocks_ExistingPlanetWithoutBirds_ReturnsEmptyState()
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
                        "Birdless World",
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
                $"/sessions/{created.SessionId}/planets/{planetId}/bird-flocks");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var state =
            await response.Content
                .ReadFromJsonAsync<BirdFlocksResponse>();

        Assert.NotNull(
            state);

        Assert.Equal(
            planetId,
            state.PlanetId);

        Assert.Empty(
            state.Flocks);
    }

    [Fact]
    public async Task CreateSession_BirdModelWithoutInvertebrates_ReturnsBadRequest()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateSessionRequest(
            [
                new PlanetCreationRequest(
                    "Invalid Bird World",
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
                    BirdModel:
                        new BirdModelRequest())
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
