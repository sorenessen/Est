using System.Net;
using System.Net.Http.Json;
using Est.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Est.Api.Tests.Sessions;

public sealed class ManifestedEsterEndpointTests
{
    [Fact]
    public async Task ManifestMoveAndRead_UseAuthoritativeSessionState()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var created =
            await (await client.PostAsJsonAsync(
                    "/sessions",
                    CreateWorldRequest()))
                .Content
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

        var esterId =
            Guid.NewGuid();

        var manifestationPath =
            $"/sessions/{created.SessionId}" +
            $"/esters/{esterId}/manifestation";

        var beforeManifestation =
            await client.GetAsync(
                manifestationPath);

        Assert.Equal(
            HttpStatusCode.NotFound,
            beforeManifestation.StatusCode);

        var manifestResponse =
            await client.PostAsJsonAsync(
                $"/sessions/{created.SessionId}" +
                $"/esters/{esterId}/manifest",
                new ManifestEsterRequest(
                    planet.PlanetId,
                    47.0379,
                    -122.9007));

        Assert.Equal(
            HttpStatusCode.OK,
            manifestResponse.StatusCode);

        var manifested =
            await manifestResponse.Content
                .ReadFromJsonAsync<
                    ManifestedEsterResponse>();

        Assert.NotNull(
            manifested);

        Assert.Equal(
            esterId,
            manifested.EsterId);

        Assert.Equal(
            planet.PlanetId,
            manifested.PlanetId);

        Assert.Equal(
            47.0379,
            manifested.LatitudeDegrees);

        Assert.Equal(
            -122.9007,
            manifested.LongitudeDegrees);

        Assert.Null(
            manifested.SurfaceCellId);

        var read =
            await client.GetFromJsonAsync<
                ManifestedEsterResponse>(
                manifestationPath);

        Assert.NotNull(
            read);

        Assert.Equal(
            manifested,
            read);

        var moveResponse =
            await client.PostAsJsonAsync(
                $"/sessions/{created.SessionId}" +
                $"/esters/{esterId}/move",
                new MoveManifestedEsterRequest(
                    47.0381,
                    -122.9004));

        Assert.Equal(
            HttpStatusCode.OK,
            moveResponse.StatusCode);

        var moved =
            await moveResponse.Content
                .ReadFromJsonAsync<
                    ManifestedEsterResponse>();

        Assert.NotNull(
            moved);

        Assert.Equal(
            esterId,
            moved.EsterId);

        Assert.Equal(
            planet.PlanetId,
            moved.PlanetId);

        Assert.Equal(
            47.0381,
            moved.LatitudeDegrees);

        Assert.Equal(
            -122.9004,
            moved.LongitudeDegrees);

        Assert.Null(
            moved.SurfaceCellId);

        var afterMove =
            await client.GetFromJsonAsync<
                ManifestedEsterResponse>(
                manifestationPath);

        Assert.NotNull(
            afterMove);

        Assert.Equal(
            moved,
            afterMove);

        var session =
            await client.GetFromJsonAsync<SessionResponse>(
                $"/sessions/{created.SessionId}");

        Assert.NotNull(
            session);

        Assert.Equal(
            0,
            session.CurrentTimeSeconds);

        Assert.Equal(
            2,
            session.EventCount);
    }

    [Fact]
    public async Task ManifestationEndpoints_ProjectAuthoritativeSurfaceCellWhenAvailable()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var created =
            await (await client.PostAsJsonAsync(
                    "/sessions",
                    CreateSurfaceBackedWorldRequest()))
                .Content
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

        var surface =
            await client.GetFromJsonAsync<SurfaceResponse>(
                $"/sessions/{created.SessionId}" +
                $"/planets/{planet.PlanetId}/surface");

        Assert.NotNull(
            surface);

        Assert.True(
            surface.Cells.Length >=
            2);

        var firstCell =
            surface.Cells[0];

        var secondCell =
            surface.Cells[^1];

        Assert.NotEqual(
            firstCell.CellId,
            secondCell.CellId);

        var esterId =
            Guid.NewGuid();

        var manifestationPath =
            $"/sessions/{created.SessionId}" +
            $"/esters/{esterId}/manifestation";

        var manifestResponse =
            await client.PostAsJsonAsync(
                $"/sessions/{created.SessionId}" +
                $"/esters/{esterId}/manifest",
                new ManifestEsterRequest(
                    planet.PlanetId,
                    firstCell.CenterLatitudeDegrees,
                    firstCell.CenterLongitudeDegrees));

        Assert.Equal(
            HttpStatusCode.OK,
            manifestResponse.StatusCode);

        var manifested =
            await manifestResponse.Content
                .ReadFromJsonAsync<
                    ManifestedEsterResponse>();

        Assert.NotNull(
            manifested);

        Assert.Equal(
            firstCell.CellId,
            manifested.SurfaceCellId);

        var read =
            await client.GetFromJsonAsync<
                ManifestedEsterResponse>(
                manifestationPath);

        Assert.NotNull(
            read);

        Assert.Equal(
            firstCell.CellId,
            read.SurfaceCellId);

        var moveResponse =
            await client.PostAsJsonAsync(
                $"/sessions/{created.SessionId}" +
                $"/esters/{esterId}/move",
                new MoveManifestedEsterRequest(
                    secondCell.CenterLatitudeDegrees,
                    secondCell.CenterLongitudeDegrees));

        Assert.Equal(
            HttpStatusCode.OK,
            moveResponse.StatusCode);

        var moved =
            await moveResponse.Content
                .ReadFromJsonAsync<
                    MoveManifestedEsterResponse>();

        Assert.NotNull(
            moved);

        Assert.Equal(
            secondCell.CellId,
            moved.SurfaceCellId);

        var afterMove =
            await client.GetFromJsonAsync<
                ManifestedEsterResponse>(
                manifestationPath);

        Assert.NotNull(
            afterMove);

        Assert.Equal(
            secondCell.CellId,
            afterMove.SurfaceCellId);
    }

    [Fact]
    public async Task ManifestationEndpoints_RejectInvalidAuthorityRequests()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var created =
            await (await client.PostAsJsonAsync(
                    "/sessions",
                    CreateWorldRequest()))
                .Content
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

        var esterId =
            Guid.NewGuid();

        var missingPlanet =
            await client.PostAsJsonAsync(
                $"/sessions/{created.SessionId}" +
                $"/esters/{esterId}/manifest",
                new ManifestEsterRequest(
                    Guid.NewGuid(),
                    0,
                    0));

        Assert.Equal(
            HttpStatusCode.NotFound,
            missingPlanet.StatusCode);

        var invalidLatitude =
            await client.PostAsJsonAsync(
                $"/sessions/{created.SessionId}" +
                $"/esters/{esterId}/manifest",
                new ManifestEsterRequest(
                    planet.PlanetId,
                    91,
                    0));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            invalidLatitude.StatusCode);

        var moveBeforeManifest =
            await client.PostAsJsonAsync(
                $"/sessions/{created.SessionId}" +
                $"/esters/{esterId}/move",
                new MoveManifestedEsterRequest(
                    0,
                    0));

        Assert.Equal(
            HttpStatusCode.NotFound,
            moveBeforeManifest.StatusCode);
    }

    private static CreateSessionRequest
        CreateSurfaceBackedWorldRequest()
    {
        return new CreateSessionRequest(
        [
            new PlanetCreationRequest(
                "Surface-backed Playable Earth",
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
                            ["N2"] = 0.78,
                            ["O2"] = 0.21,
                            ["Ar"] = 0.01
                        })),
                GeneratedTerrain:
                    new GeneratedTerrainCreationRequest(
                        Seed: 126,
                        LatitudeBandCount: 4,
                        LongitudeBandCount: 8,
                        PlateCount: 4,
                        ContinentalPlateFraction: 0.45))
        ]);
    }

    private static CreateSessionRequest
        CreateWorldRequest()
    {
        return new CreateSessionRequest(
        [
            new PlanetCreationRequest(
                "Playable Earth",
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
                            ["N2"] = 0.78,
                            ["O2"] = 0.21,
                            ["Ar"] = 0.01
                        })))
        ]);
    }
}
