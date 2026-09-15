using System.Net;
using System.Net.Http.Json;
using Est.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Est.Api.Tests.Sessions;

public sealed class StandingWaterEndpointTests
{
    [Fact]
    public async Task StandingWaterEndpoint_ExposesDerivedPlanetWaterBodies()
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
                    "Standing Water API World",
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

        var terrain =
            await client.GetFromJsonAsync<TerrainResponse>(
                $"/sessions/{created.SessionId}/planets/{planet.PlanetId}/terrain");

        var standingWater =
            await client.GetFromJsonAsync<StandingWaterResponse>(
                $"/sessions/{created.SessionId}/planets/{planet.PlanetId}/standing-water");

        Assert.NotNull(
            terrain);

        Assert.NotNull(
            standingWater);

        Assert.Equal(
            planet.PlanetId,
            standingWater.PlanetId);

        Assert.Equal(
            terrain.Grid,
            standingWater.Grid);

        Assert.Equal(
            32,
            standingWater.Cells.Length);

        Assert.Equal(
            32,
            standingWater.Cells
                .Select(
                    cell =>
                        cell.CellId)
                .Distinct()
                .Count());

        var terrainByCellId =
            terrain.Cells.ToDictionary(
                cell =>
                    cell.CellId);

        Assert.All(
            standingWater.Cells,
            cell =>
            {
                Assert.True(
                    double.IsFinite(
                        cell.WaterDepthMeters));

                Assert.True(
                    double.IsFinite(
                        cell.WaterSurfaceElevationMeters));

                Assert.True(
                    cell.WaterDepthMeters >=
                    0);

                Assert.True(
                    terrainByCellId.TryGetValue(
                        cell.CellId,
                        out var terrainCell));

                Assert.NotNull(
                    terrainCell);

                var expectedSurfaceElevation =
                    terrainCell.ElevationMeters +
                    cell.WaterDepthMeters;

                Assert.InRange(
                    Math.Abs(
                        cell.WaterSurfaceElevationMeters -
                        expectedSurfaceElevation),
                    0,
                    1e-9);

                if (cell.Kind == "Dry")
                {
                    Assert.Null(
                        cell.WaterBodyAnchorCellId);

                    Assert.Equal(
                        0,
                        cell.WaterDepthMeters);
                }
                else
                {
                    Assert.True(
                        cell.Kind == "Ocean" ||
                        cell.Kind == "Lake");

                    Assert.NotNull(
                        cell.WaterBodyAnchorCellId);

                    Assert.True(
                        cell.WaterDepthMeters >
                        0);
                }
            });

        Assert.NotEmpty(
            standingWater.WaterBodies);

        var ocean =
            Assert.Single(
                standingWater.WaterBodies,
                body =>
                    body.Kind ==
                    "Ocean");

        Assert.NotEqual(
            Guid.Empty,
            ocean.AnchorCellId);

        Assert.NotEmpty(
            ocean.CellIds);

        Assert.Contains(
            ocean.AnchorCellId,
            ocean.CellIds);

        Assert.All(
            standingWater.WaterBodies,
            body =>
            {
                Assert.True(
                    body.Kind == "Ocean" ||
                    body.Kind == "Lake");

                Assert.NotEqual(
                    Guid.Empty,
                    body.AnchorCellId);

                Assert.NotEmpty(
                    body.CellIds);

                Assert.True(
                    body.SurfaceAreaSquareMeters >
                    0);

                Assert.True(
                    body.WaterVolumeCubicMeters >
                    0);

                Assert.Contains(
                    body.AnchorCellId,
                    body.CellIds);

                Assert.All(
                    body.CellIds,
                    cellId =>
                    {
                        var cell =
                            Assert.Single(
                                standingWater.Cells,
                                candidate =>
                                    candidate.CellId ==
                                    cellId);

                        Assert.Equal(
                            body.Kind,
                            cell.Kind);

                        Assert.Equal(
                            body.AnchorCellId,
                            cell.WaterBodyAnchorCellId);
                    });
            });
    }
}
