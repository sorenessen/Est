using System.Net;
using System.Net.Http.Json;
using Est.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Est.Api.Tests.Sessions;

public sealed class BiogeochemistryEndpointTests
{
    [Fact]
    public async Task CreateSession_WithBiogeochemistry_CreatesRunsAndExposesStateAndModels()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateSessionRequest(
            [
                new PlanetCreationRequest(
                    "Biogeochemistry World",
                    5.0e24,
                    6_000_000,
                    new PlanetEnvironmentCreationRequest(
                        293.15,
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
                            1),
                    VegetationModel:
                        new VegetationModelRequest
                        {
                            MaximumIntegrationStepSeconds =
                                3_600,
                            CarryingCapacityKilogramsPerSquareMeter =
                                5,
                            MaximumRelativeGrowthRatePerDay =
                                0.10,
                            SoilWaterForFullProductivityKilogramsPerSquareMeter =
                                50,
                            MinimumGrowthTemperatureKelvin =
                                263.15,
                            OptimumGrowthTemperatureKelvin =
                                293.15,
                            MaximumGrowthTemperatureKelvin =
                                313.15,
                            TemperatureLapseRateKelvinPerMeter =
                                0.0065,
                            PlantNitrogenKilogramsPerKilogramLiveBiomass =
                                0.02,
                            BaselineMortalityRatePerDay =
                                0.01
                        },
                    GeneratedBiogeochemistry:
                        new GeneratedBiogeochemistryCreationRequest(
                            InitialDetritalBiomassKilogramsPerSquareMeter:
                                1,
                            InitialDetritalNitrogenKilogramsPerSquareMeter:
                                0.02,
                            InitialPlantAvailableNitrogenKilogramsPerSquareMeter:
                                0.01),
                    BiogeochemistryModel:
                        new BiogeochemistryModelRequest
                        {
                            MaximumIntegrationStepSeconds =
                                3_600,
                            MaximumRelativeDecompositionRatePerDay =
                                0.05,
                            SoilWaterForFullDecompositionKilogramsPerSquareMeter =
                                50,
                            MinimumDecompositionTemperatureKelvin =
                                263.15,
                            OptimumDecompositionTemperatureKelvin =
                                293.15,
                            MaximumDecompositionTemperatureKelvin =
                                313.15,
                            TemperatureLapseRateKelvinPerMeter =
                                0.0065
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

        var biogeochemistryModel =
            Assert.Single(
                definition.BiogeochemistryModels);

        Assert.Equal(
            3_600,
            biogeochemistryModel.MaximumIntegrationStepSeconds);

        Assert.Equal(
            0.05,
            biogeochemistryModel.MaximumRelativeDecompositionRatePerDay);

        Assert.Equal(
            50,
            biogeochemistryModel
                .SoilWaterForFullDecompositionKilogramsPerSquareMeter);

        Assert.Equal(
            263.15,
            biogeochemistryModel.MinimumDecompositionTemperatureKelvin);

        Assert.Equal(
            293.15,
            biogeochemistryModel.OptimumDecompositionTemperatureKelvin);

        Assert.Equal(
            313.15,
            biogeochemistryModel.MaximumDecompositionTemperatureKelvin);

        Assert.Equal(
            0.0065,
            biogeochemistryModel.TemperatureLapseRateKelvinPerMeter);

        var vegetationModel =
            Assert.Single(
                definition.VegetationModels);

        Assert.Equal(
            0.02,
            vegetationModel
                .PlantNitrogenKilogramsPerKilogramLiveBiomass);

        Assert.Equal(
            0.01,
            vegetationModel.BaselineMortalityRatePerDay);

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
            await client.GetFromJsonAsync<
                BiogeochemistryResponse>(
                $"/sessions/{created.SessionId}/planets/{planetId}/biogeochemistry");

        Assert.NotNull(
            before);

        Assert.NotEmpty(
            before.Cells);

        Assert.All(
            before.Cells,
            cell =>
            {
                Assert.Equal(
                    1,
                    cell.DetritalBiomassKilogramsPerSquareMeter,
                    12);

                Assert.Equal(
                    0.02,
                    cell.DetritalNitrogenKilogramsPerSquareMeter,
                    12);

                Assert.Equal(
                    0.01,
                    cell.PlantAvailableNitrogenKilogramsPerSquareMeter,
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

        var biogeochemistryEvent =
            Assert.Single(
                timeline.Events,
                timelineEvent =>
                    timelineEvent.Cause ==
                    "planetary-biogeochemistry");

        Assert.Equal(
            86_400,
            biogeochemistryEvent.ElapsedSeconds);

        var vegetationEvent =
            Assert.Single(
                timeline.Events,
                timelineEvent =>
                    timelineEvent.Cause ==
                    "planetary-vegetation");

        Assert.Equal(
            86_400,
            vegetationEvent.ElapsedSeconds);

        var after =
            await client.GetFromJsonAsync<
                BiogeochemistryResponse>(
                $"/sessions/{created.SessionId}/planets/{planetId}/biogeochemistry");

        Assert.NotNull(
            after);

        Assert.Equal(
            before.Cells.Length,
            after.Cells.Length);
    }

    [Fact]
    public async Task GetBiogeochemistry_WithoutState_ReturnsNotFound()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateSessionRequest(
            [
                new PlanetCreationRequest(
                    "No Biogeochemistry",
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

        var planetId =
            Assert.Single(
                world.Planets)
            .PlanetId;

        var response =
            await client.GetAsync(
                $"/sessions/{created.SessionId}/planets/{planetId}/biogeochemistry");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateSession_BiogeochemistryModelWithoutState_ReturnsBadRequest()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var request =
            new CreateSessionRequest(
            [
                new PlanetCreationRequest(
                    "Invalid Biogeochemistry World",
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
                    BiogeochemistryModel:
                        new BiogeochemistryModelRequest())
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
