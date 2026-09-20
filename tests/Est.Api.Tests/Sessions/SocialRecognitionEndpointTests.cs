using System.Net;
using System.Net.Http.Json;
using Est.Api;
using Est.Api.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Est.Api.Tests.Sessions;

public sealed class SocialRecognitionEndpointTests
{
    [Fact]
    public async Task ReturningEster_IsRecognizedBeforeSecondEncounterAfterArchiveLoad()
    {
        var directory =
            CreateTempDirectory();

        try
        {
            await using var factory =
                CreateFactory(directory);

            using var client =
                factory.CreateClient();

            var creation =
                await client.PostAsJsonAsync(
                    "/sessions",
                    new CreateSessionRequest(
                    [
                        new PlanetCreationRequest(
                            "Recognition World",
                            5.9722e24,
                            6_371_000,
                            new PlanetEnvironmentCreationRequest(
                                288.15,
                                0.71,
                                0.1,
                                new AtmosphereCreationRequest(
                                    0,
                                    new Dictionary<string, double>())),
                            SyntheticPopulation:
                                new SyntheticPopulationCreationRequest(
                                    FounderCount: 1,
                                    Seed: 17,
                                    CenterLatitudeDegrees: 0,
                                    CenterLongitudeDegrees: 0,
                                    SpreadDegrees: 0))
                    ]));

            Assert.Equal(
                HttpStatusCode.Created,
                creation.StatusCode);

            var created =
                await creation.Content
                    .ReadFromJsonAsync<SessionResponse>();

            Assert.NotNull(created);

            var world =
                await client.GetFromJsonAsync<WorldResponse>(
                    $"/sessions/{created.SessionId}/world");

            Assert.NotNull(world);

            var person =
                Assert.Single(
                    world.Population);

            var returningEsterId =
                Guid.NewGuid();

            var unknownEsterId =
                Guid.NewGuid();

            var returningPath =
                $"/sessions/{created.SessionId}" +
                $"/people/{person.PersonId}" +
                $"/social/esters/{returningEsterId}";

            var unknownPath =
                $"/sessions/{created.SessionId}" +
                $"/people/{person.PersonId}" +
                $"/social/esters/{unknownEsterId}";

            var initiallyUnknown =
                await client.GetFromJsonAsync<
                    PersonSocialRecognitionResponse>(
                    returningPath);

            Assert.NotNull(
                initiallyUnknown);

            Assert.Equal(
                person.PersonId,
                initiallyUnknown.PersonId);

            Assert.Equal(
                returningEsterId,
                initiallyUnknown.EsterId);

            Assert.False(
                initiallyUnknown.Recognized);

            Assert.Null(
                initiallyUnknown
                    .FirstEncounterTimeSeconds);

            Assert.Null(
                initiallyUnknown
                    .LastEncounterTimeSeconds);

            Assert.Equal(
                0,
                initiallyUnknown.EncounterCount);

            var firstEncounter =
                await client.PostAsync(
                    returningPath,
                    content: null);

            Assert.Equal(
                HttpStatusCode.OK,
                firstEncounter.StatusCode);

            var afterFirst =
                await firstEncounter.Content
                    .ReadFromJsonAsync<
                        PersonSocialRecognitionResponse>();

            Assert.NotNull(
                afterFirst);

            Assert.True(
                afterFirst.Recognized);

            Assert.Equal(
                0,
                afterFirst
                    .FirstEncounterTimeSeconds);

            Assert.Equal(
                0,
                afterFirst
                    .LastEncounterTimeSeconds);

            Assert.Equal(
                1,
                afterFirst.EncounterCount);

            var advance =
                await client.PostAsJsonAsync(
                    $"/sessions/{created.SessionId}/advance",
                    new AdvanceTimeRequest(
                        240));

            Assert.Equal(
                HttpStatusCode.OK,
                advance.StatusCode);

            var save =
                await client.PostAsync(
                    $"/sessions/{created.SessionId}/archives",
                    content: null);

            Assert.Equal(
                HttpStatusCode.Created,
                save.StatusCode);

            var archive =
                await save.Content
                    .ReadFromJsonAsync<ArchiveResponse>();

            Assert.NotNull(
                archive);

            var load =
                await client.PostAsync(
                    $"/archives/{archive.ArchiveId}/load",
                    content: null);

            Assert.Equal(
                HttpStatusCode.Created,
                load.StatusCode);

            var restored =
                await load.Content
                    .ReadFromJsonAsync<SessionResponse>();

            Assert.NotNull(
                restored);

            Assert.NotEqual(
                created.SessionId,
                restored.SessionId);

            Assert.Equal(
                created.WorldId,
                restored.WorldId);

            Assert.Equal(
                created.TimelineId,
                restored.TimelineId);

            Assert.Equal(
                240,
                restored.CurrentTimeSeconds);

            var restoredReturningPath =
                $"/sessions/{restored.SessionId}" +
                $"/people/{person.PersonId}" +
                $"/social/esters/{returningEsterId}";

            var restoredUnknownPath =
                $"/sessions/{restored.SessionId}" +
                $"/people/{person.PersonId}" +
                $"/social/esters/{unknownEsterId}";

            var recognizedBeforeSecondEncounter =
                await client.GetFromJsonAsync<
                    PersonSocialRecognitionResponse>(
                    restoredReturningPath);

            Assert.NotNull(
                recognizedBeforeSecondEncounter);

            Assert.True(
                recognizedBeforeSecondEncounter
                    .Recognized);

            Assert.Equal(
                1,
                recognizedBeforeSecondEncounter
                    .EncounterCount);

            Assert.Equal(
                0,
                recognizedBeforeSecondEncounter
                    .FirstEncounterTimeSeconds);

            Assert.Equal(
                0,
                recognizedBeforeSecondEncounter
                    .LastEncounterTimeSeconds);

            var stillUnknown =
                await client.GetFromJsonAsync<
                    PersonSocialRecognitionResponse>(
                    restoredUnknownPath);

            Assert.NotNull(
                stillUnknown);

            Assert.False(
                stillUnknown.Recognized);

            Assert.Equal(
                0,
                stillUnknown.EncounterCount);

            Assert.Null(
                stillUnknown
                    .FirstEncounterTimeSeconds);

            Assert.Null(
                stillUnknown
                    .LastEncounterTimeSeconds);

            var secondEncounter =
                await client.PostAsync(
                    restoredReturningPath,
                    content: null);

            Assert.Equal(
                HttpStatusCode.OK,
                secondEncounter.StatusCode);

            var afterSecond =
                await secondEncounter.Content
                    .ReadFromJsonAsync<
                        PersonSocialRecognitionResponse>();

            Assert.NotNull(
                afterSecond);

            Assert.True(
                afterSecond.Recognized);

            Assert.Equal(
                2,
                afterSecond.EncounterCount);

            Assert.Equal(
                0,
                afterSecond
                    .FirstEncounterTimeSeconds);

            Assert.Equal(
                240,
                afterSecond
                    .LastEncounterTimeSeconds);

            var originalAfterRestore =
                await client.GetFromJsonAsync<
                    PersonSocialRecognitionResponse>(
                    returningPath);

            Assert.NotNull(
                originalAfterRestore);

            Assert.True(
                originalAfterRestore.Recognized);

            Assert.Equal(
                1,
                originalAfterRestore
                    .EncounterCount);
        }
        finally
        {
            Directory.Delete(
                directory,
                recursive: true);
        }
    }

    [Fact]
    public async Task Recognition_MissingSessionOrPerson_ReturnsNotFound()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var missingSession =
            await client.GetAsync(
                $"/sessions/{Guid.NewGuid()}" +
                $"/people/{Guid.NewGuid()}" +
                $"/social/esters/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            missingSession.StatusCode);

        var created =
            await (await client.PostAsJsonAsync(
                    "/sessions",
                    new CreateSessionRequest([])))
                .Content
                .ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(
            created);

        var missingPerson =
            await client.GetAsync(
                $"/sessions/{created.SessionId}" +
                $"/people/{Guid.NewGuid()}" +
                $"/social/esters/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            missingPerson.StatusCode);
    }

    private static WebApplicationFactory<Program>
        CreateFactory(
            string directory)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder =>
                    builder.UseSetting(
                        "Est:ArchiveDirectory",
                        directory));
    }

    private static string CreateTempDirectory()
    {
        var directory =
            Path.Combine(
                Path.GetTempPath(),
                "Est.Api.Tests",
                Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(
            directory);

        return directory;
    }
}
