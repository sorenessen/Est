using System.Net;
using System.Net.Http.Json;
using Est.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Est.Api.Tests.Sessions;

public sealed class InvertebrateEndpointTests
{
    [Fact]
    public async Task CreateSession_WithInvertebrates_CreatesRunsAndExposesModel()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateSessionRequest(
            [
                new PlanetCreationRequest(
                    "Invertebrate World",
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
                    VegetationModel:
                        new VegetationModelRequest
                        {
                            MaximumRelativeGrowthRatePerDay =
                                0,
                            PlantNitrogenKilogramsPerKilogramLiveBiomass =
                                0.025,
                            BaselineMortalityRatePerDay =
                                0
                        },
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
                                0.20,
                            BaselineMortalityRatePerDay =
                                0,
                            LiveNitrogenKilogramsPerKilogramLiveBiomass =
                                0.025
                        },
                    GeneratedBiogeochemistry:
                        new GeneratedBiogeochemistryCreationRequest(
                            InitialDetritalBiomassKilogramsPerSquareMeter:
                                0,
                            InitialDetritalNitrogenKilogramsPerSquareMeter:
                                0,
                            InitialPlantAvailableNitrogenKilogramsPerSquareMeter:
                                0.10))
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
                definition.InvertebrateModels);

        Assert.Equal(
            3_600,
            model.MaximumIntegrationStepSeconds);

        Assert.Equal(
            0.02,
            model.CarryingCapacityKilogramsPerKilogramLiveVegetation);

        Assert.Equal(
            0.25,
            model.InitialFractionOfLocalCarryingCapacity);

        Assert.Equal(
            0.20,
            model.MaximumRelativeGrowthRatePerDay);

        Assert.Equal(
            0,
            model.BaselineMortalityRatePerDay);

        Assert.Equal(
            0.025,
            model.LiveNitrogenKilogramsPerKilogramLiveBiomass);

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
            await client.GetFromJsonAsync<InvertebrateResponse>(
                $"/sessions/{created.SessionId}/planets/{planetId}/invertebrates");

        Assert.NotNull(
            before);

        Assert.NotEmpty(
            before.Cells);

        Assert.All(
            before.Cells,
            cell =>
            {
                Assert.Equal(
                    0.005,
                    cell.LiveBiomassKilogramsPerSquareMeter,
                    12);

                Assert.Equal(
                    0.000125,
                    cell.LiveNitrogenKilogramsPerSquareMeter,
                    12);
            });

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

        var invertebrateEvent =
            Assert.Single(
                timeline.Events,
                timelineEvent =>
                    timelineEvent.Cause ==
                    "planetary-invertebrates");

        Assert.Equal(
            86_400,
            invertebrateEvent.ElapsedSeconds);

        var after =
            await client.GetFromJsonAsync<InvertebrateResponse>(
                $"/sessions/{created.SessionId}/planets/{planetId}/invertebrates");

        Assert.NotNull(
            after);

        Assert.All(
            after.Cells,
            cell =>
            {
                Assert.True(
                    cell.LiveBiomassKilogramsPerSquareMeter >
                    0.005);

                Assert.True(
                    cell.LiveNitrogenKilogramsPerSquareMeter >
                    0.000125);

                Assert.Equal(
                    cell.LiveBiomassKilogramsPerSquareMeter *
                    0.025,
                    cell.LiveNitrogenKilogramsPerSquareMeter,
                    12);
            });
    }

    [Fact]
    public async Task CreateSession_InvertebrateModelWithoutState_ReturnsBadRequest()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateSessionRequest(
            [
                new PlanetCreationRequest(
                    "Invalid Invertebrate World",
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
                    InvertebrateModel:
                        new InvertebrateModelRequest())
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
