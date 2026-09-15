using System.Net;
using System.Net.Http.Json;
using Est.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Est.Api.Tests.Sessions;

public sealed class VegetationEndpointTests
{
    [Fact]
    public async Task CreateSession_WithVegetation_CreatesRunsAndExposesModel()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateSessionRequest(
            [
                new PlanetCreationRequest(
                    "Vegetation World",
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
                        new HydrologyModelRequest(),
                    GeneratedVegetation:
                        new GeneratedVegetationCreationRequest(
                            0.25),
                    VegetationModel:
                        new VegetationModelRequest
                        {
                            MaximumIntegrationStepSeconds =
                                3_600,
                            CarryingCapacityKilogramsPerSquareMeter =
                                7,
                            MaximumRelativeGrowthRatePerDay =
                                0.2,
                            SoilWaterForFullProductivityKilogramsPerSquareMeter =
                                60,
                            MinimumGrowthTemperatureKelvin =
                                270,
                            OptimumGrowthTemperatureKelvin =
                                292,
                            MaximumGrowthTemperatureKelvin =
                                315,
                            TemperatureLapseRateKelvinPerMeter =
                                0.006
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
                definition.VegetationModels);

        Assert.Equal(
            3_600,
            model.MaximumIntegrationStepSeconds);

        Assert.Equal(
            7,
            model.CarryingCapacityKilogramsPerSquareMeter);

        Assert.Equal(
            0.2,
            model.MaximumRelativeGrowthRatePerDay);

        Assert.Equal(
            60,
            model.SoilWaterForFullProductivityKilogramsPerSquareMeter);

        Assert.Equal(
            270,
            model.MinimumGrowthTemperatureKelvin);

        Assert.Equal(
            292,
            model.OptimumGrowthTemperatureKelvin);

        Assert.Equal(
            315,
            model.MaximumGrowthTemperatureKelvin);

        Assert.Equal(
            0.006,
            model.TemperatureLapseRateKelvinPerMeter);

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

        var vegetationEvent =
            Assert.Single(
                timeline.Events,
                timelineEvent =>
                    timelineEvent.Cause ==
                    "planetary-vegetation");

        Assert.Equal(
            86_400,
            vegetationEvent.ElapsedSeconds);

        Assert.True(
            vegetationEvent.Metrics.ContainsKey(
                "initialLiveBiomassKilograms"));

        Assert.True(
            vegetationEvent.Metrics.ContainsKey(
                "finalLiveBiomassKilograms"));

        Assert.True(
            vegetationEvent.Metrics.ContainsKey(
                "biomassGrowthKilograms"));
    }
}
