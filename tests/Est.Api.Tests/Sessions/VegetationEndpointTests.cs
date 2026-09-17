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

    [Fact]
    public async Task CreateSession_WithVegetationForaging_ExposesAndRunsPolicy()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateSessionRequest(
            [
                new PlanetCreationRequest(
                    "Vegetation Foraging World",
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
                    SyntheticPopulation:
                        new SyntheticPopulationCreationRequest(
                            FounderCount: 1,
                            Seed: 42,
                            CenterLatitudeDegrees: 0,
                            CenterLongitudeDegrees: 0,
                            SpreadDegrees: 0,
                            VegetationForaging:
                                new VegetationForagingRequest(
                                    KilogramsLiveBiomassPerEnergyReserveUnit:
                                        0.8,
                                    MaximumHarvestKilogramsPerPersonPerDay:
                                        1.25)),
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
                    GeneratedVegetation:
                        new GeneratedVegetationCreationRequest(
                            1))
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

        Assert.NotNull(
            created);

        var definition =
            await client.GetFromJsonAsync<
                SimulationDefinitionResponse>(
                $"/sessions/{created.SessionId}/definition");

        Assert.NotNull(
            definition);

        var populationModel =
            Assert.Single(
                definition.PopulationModels);

        Assert.NotNull(
            populationModel.VegetationForaging);

        Assert.Equal(
            0.8,
            populationModel.VegetationForaging
                .KilogramsLiveBiomassPerEnergyReserveUnit);

        Assert.Equal(
            1.25,
            populationModel.VegetationForaging
                .MaximumHarvestKilogramsPerPersonPerDay);

        var advanceResponse =
            await client.PostAsJsonAsync(
                $"/sessions/{created.SessionId}/advance",
                new AdvanceTimeRequest(
                    15 * 86_400L));

        Assert.Equal(
            HttpStatusCode.OK,
            advanceResponse.StatusCode);

        var timeline =
            await client.GetFromJsonAsync<TimelineResponse>(
                $"/sessions/{created.SessionId}/timeline");

        Assert.NotNull(
            timeline);

        var foragingEvents =
            timeline.Events
                .Where(
                    timelineEvent =>
                        timelineEvent.Cause ==
                        "vegetation-foraging")
                .ToArray();

        Assert.Equal(
            15,
            foragingEvents.Length);

        for (var day = 0;
             day < foragingEvents.Length;
             day++)
        {
            Assert.Equal(
                (day + 1) * 86_400L,
                foragingEvents[day]
                    .OccurredAtSeconds);

            Assert.Equal(
                86_400,
                foragingEvents[day]
                    .ElapsedSeconds);
        }

        Assert.True(
            foragingEvents.Sum(
                timelineEvent =>
                    timelineEvent.Metrics[
                        "biomassHarvestedKilograms"]) >
            0);

        Assert.True(
            foragingEvents.Sum(
                timelineEvent =>
                    timelineEvent.Metrics[
                        "energyConsumed"]) >
            0);
    }


    [Fact]
    public async Task CreateSession_WithVegetationForagingButNoVegetation_ReturnsBadRequest()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateSessionRequest(
            [
                new PlanetCreationRequest(
                    "Invalid Vegetation Foraging World",
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
                    SyntheticPopulation:
                        new SyntheticPopulationCreationRequest(
                            FounderCount: 1,
                            Seed: 42,
                            CenterLatitudeDegrees: 0,
                            CenterLongitudeDegrees: 0,
                            SpreadDegrees: 0,
                            VegetationForaging:
                                new VegetationForagingRequest(
                                    KilogramsLiveBiomassPerEnergyReserveUnit:
                                        0.8,
                                    MaximumHarvestKilogramsPerPersonPerDay:
                                        1.25)))
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
