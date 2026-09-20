using Est.Application.Sessions;
using Est.Persistence.Archives;
using Est.Persistence.Storage;
using Est.Simulation.Climate;
using Est.Simulation.Definitions;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Social;
using Est.Simulation.Seasons;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Application.Tests.Sessions;

public sealed class SimulationSessionArchiveServiceTests
{
    [Fact]
    public void Load_CorruptArchive_DoesNotRegisterSession()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "Est.Tests",
            Guid.NewGuid().ToString("N"),
            "timeline.json");

        try
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(path)!);

            File.WriteAllText(
                path,
                "{ not valid json");

            var manager =
                new SimulationSessionManager();

            var service =
                new SimulationSessionArchiveService(
                    manager,
                    new TimelineArchiveFileStore());

            Assert.Throws<System.Text.Json.JsonException>(
                () => service.Load(path));

            var knownId =
                SimulationSessionId.New();

            Assert.False(
                manager.TryGet(
                    knownId,
                    out _));
        }
        finally
        {
            var directory =
                Path.GetDirectoryName(path)!;

            if (Directory.Exists(directory))
            {
                Directory.Delete(
                    directory,
                    recursive: true);
            }
        }
    }

    [Fact]
    public void SaveAndLoad_PreservesReturningEsterRecognitionBeforeSecondEncounter()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "Est.Tests",
            Guid.NewGuid().ToString("N"),
            "timeline.json");

        try
        {
            var manager =
                new SimulationSessionManager();

            var service =
                new SimulationSessionArchiveService(
                    manager,
                    new TimelineArchiveFileStore());

            var planet =
                new PlanetState(
                    PlanetId.New(),
                    "Social Recognition World",
                    5.9722e24,
                    6_371_000,
                    new PlanetEnvironment(
                        288.15,
                        0.71,
                        0.1,
                        AtmosphereState.Vacuum));

            var person =
                new PersonState(
                    PersonId.New(),
                    planet.Id,
                    PersonSex.Female,
                    birthTimeSeconds: 0,
                    latitudeDegrees: 0,
                    longitudeDegrees: 0);

            var returningActor =
                SocialActorIdentity.ForEster(
                    EsterId.New());

            var unknownActor =
                SocialActorIdentity.ForEster(
                    EsterId.New());

            var originalId =
                manager.Create(
                    new WorldState(
                        WorldId.New(),
                        new SimulationTime(120),
                        [planet],
                        [person]));

            var original =
                manager.Get(
                    originalId);

            Assert.Null(
                original.GetPersonSocialContact(
                    person.Id,
                    returningActor));

            Assert.Null(
                original.GetPersonSocialContact(
                    person.Id,
                    unknownActor));

            original.RecordPersonSocialEncounter(
                person.Id,
                returningActor);

            original.Advance(
                240);

            var beforeSave =
                original.GetPersonSocialContact(
                    person.Id,
                    returningActor);

            Assert.NotNull(
                beforeSave);

            Assert.Equal(
                1,
                beforeSave.EncounterCount);

            Assert.Equal(
                120,
                beforeSave.FirstEncounterTimeSeconds);

            Assert.Equal(
                120,
                beforeSave.LastEncounterTimeSeconds);

            Assert.Equal(
                360,
                original.CurrentWorld
                    .CurrentTime.TotalSeconds);

            service.Save(
                originalId,
                path,
                new TimelineArchiveProvenance(
                    "Est",
                    "0.1.0-alpha",
                    "simulation"));

            var loadedId =
                service.Load(
                    path);

            var loaded =
                manager.Get(
                    loadedId);

            Assert.NotEqual(
                originalId,
                loadedId);

            Assert.NotSame(
                original,
                loaded);

            Assert.Equal(
                360,
                loaded.CurrentWorld
                    .CurrentTime.TotalSeconds);

            var recognizedBeforeSecondEncounter =
                loaded.GetPersonSocialContact(
                    person.Id,
                    returningActor);

            Assert.NotNull(
                recognizedBeforeSecondEncounter);

            Assert.Equal(
                1,
                recognizedBeforeSecondEncounter
                    .EncounterCount);

            Assert.Equal(
                120,
                recognizedBeforeSecondEncounter
                    .FirstEncounterTimeSeconds);

            Assert.Equal(
                120,
                recognizedBeforeSecondEncounter
                    .LastEncounterTimeSeconds);

            Assert.Null(
                loaded.GetPersonSocialContact(
                    person.Id,
                    unknownActor));

            loaded.RecordPersonSocialEncounter(
                person.Id,
                returningActor);

            var afterSecondEncounter =
                loaded.GetPersonSocialContact(
                    person.Id,
                    returningActor);

            Assert.NotNull(
                afterSecondEncounter);

            Assert.Equal(
                2,
                afterSecondEncounter.EncounterCount);

            Assert.Equal(
                120,
                afterSecondEncounter
                    .FirstEncounterTimeSeconds);

            Assert.Equal(
                360,
                afterSecondEncounter
                    .LastEncounterTimeSeconds);

            var originalContact =
                original.GetPersonSocialContact(
                    person.Id,
                    returningActor);

            Assert.NotNull(
                originalContact);

            Assert.Equal(
                1,
                originalContact.EncounterCount);

            Assert.Equal(
                120,
                originalContact
                    .LastEncounterTimeSeconds);
        }
        finally
        {
            var directory =
                Path.GetDirectoryName(path)!;

            if (Directory.Exists(directory))
            {
                Directory.Delete(
                    directory,
                    recursive: true);
            }
        }
    }

    [Fact]
    public void SaveAndLoad_PreservesHistoryAndCreatesIndependentSession()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "Est.Tests",
            Guid.NewGuid().ToString("N"),
            "timeline.json");

        try
        {
            var manager = new SimulationSessionManager();
            var service = new SimulationSessionArchiveService(
                manager,
                new TimelineArchiveFileStore());

            var planet = new PlanetState(
                PlanetId.New(),
                "Test Planet",
                5.9722e24,
                6_371_000,
                new PlanetEnvironment(
                    288.15,
                    0.71,
                    0.1,
                    AtmosphereState.Vacuum));

            var originalId = manager.Create(
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet]));

            var original = manager.Get(originalId);
            original.Advance(120);

            original.ReplacePlanetEnvironment(
                planet.Id,
                new PlanetEnvironment(
                    300,
                    0.65,
                    0.02,
                    AtmosphereState.Vacuum));

            var savedTimeline = original.Timeline;

            service.Save(
                originalId,
                path,
                new TimelineArchiveProvenance(
                    "Est",
                    "0.1.0-alpha",
                    "simulation"));

            var loadedId = service.Load(path);
            var loaded = manager.Get(loadedId);

            Assert.NotEqual(originalId, loadedId);
            Assert.NotSame(original, loaded);
            Assert.Equal(savedTimeline.Id, loaded.Timeline.Id);
            Assert.Equal(
                savedTimeline.CurrentWorld.Id,
                loaded.CurrentWorld.Id);
            Assert.Equal(
                savedTimeline.CurrentWorld.CurrentTime,
                loaded.CurrentWorld.CurrentTime);

            Assert.Equal(
                savedTimeline.Checkpoints.Select(x => x.Id),
                loaded.Timeline.Checkpoints.Select(x => x.Id));

            Assert.Equal(
                savedTimeline.Events.Select(x => x.Id),
                loaded.Timeline.Events.Select(x => x.Id));

            var restoredPlanet = Assert.Single(
                loaded.CurrentWorld.Planets);

            Assert.Equal(planet.Id, restoredPlanet.Id);
            Assert.Equal(
                300,
                restoredPlanet.Environment.MeanSurfaceTemperatureKelvin);
            Assert.Equal(
                0.65,
                restoredPlanet.Environment.SurfaceWaterFraction);
            Assert.Equal(
                0.02,
                restoredPlanet.Environment.IceCoverageFraction);

            Assert.Equal(2, loaded.Timeline.Events.Length);
            Assert.Equal(
                "User intervention",
                loaded.Timeline.Events[1].Cause);
            Assert.Equal(
                planet.Id,
                loaded.Timeline.Events[1].AffectedPlanetId);
            Assert.Equal(
                0,
                loaded.Timeline.Events[1].ElapsedSeconds);

            loaded.Advance(60);

            Assert.Equal(
                180,
                loaded.CurrentWorld.CurrentTime.TotalSeconds);
            Assert.Equal(
                120,
                original.CurrentWorld.CurrentTime.TotalSeconds);
            Assert.Equal(3, loaded.Timeline.Events.Length);
            Assert.Equal(2, original.Timeline.Events.Length);
        }
        finally
        {
            var directory = Path.GetDirectoryName(path)!;

            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void SaveAndLoad_PreservesSeasonalOverrideAndResumedBehavior()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "Est.Tests",
            Guid.NewGuid().ToString("N"),
            "timeline.json");

        try
        {
            var manager =
                new SimulationSessionManager();

            var service =
                new SimulationSessionArchiveService(
                    manager,
                    new TimelineArchiveFileStore());

            var planet =
                new PlanetState(
                    PlanetId.New(),
                    "Seasonal World",
                    5.9722e24,
                    6_371_000,
                    new PlanetEnvironment(
                        288.15,
                        0.71,
                        0.03,
                        AtmosphereState.Vacuum));

            var derivedContext =
                new SeasonalContext(
                    "warm",
                    0.25);

            var initialSeasonalState =
                new PlanetSeasonalState(
                    planet.Id,
                    SeasonalControlMode.Derived,
                    derivedContext:
                        derivedContext);

            var sessionId =
                manager.Create(
                    new WorldState(
                        WorldId.New(),
                        SimulationTime.Zero,
                        [planet],
                        [],
                        seasonalStates:
                            [initialSeasonalState]));

            var original =
                manager.Get(
                    sessionId);

            var overrideContext =
                new SeasonalContext(
                    "cold",
                    0.75);

            original.OverridePlanetSeasonalState(
                planet.Id,
                overrideContext);

            Assert.Equal(
                0,
                original.CurrentWorld.CurrentTime.TotalSeconds);

            service.Save(
                sessionId,
                path,
                new TimelineArchiveProvenance(
                    "Est",
                    "0.1.0-alpha",
                    "simulation"));

            var restoredId =
                service.Load(
                    path);

            var restored =
                manager.Get(
                    restoredId);

            var restoredState =
                Assert.Single(
                    restored.CurrentWorld.SeasonalStates);

            Assert.Equal(
                SeasonalControlMode.Override,
                restoredState.ControlMode);

            Assert.Equal(
                derivedContext,
                restoredState.DerivedContext);

            Assert.Equal(
                overrideContext,
                restoredState.OverrideContext);

            Assert.Equal(
                overrideContext,
                restoredState.EffectiveContext);

            Assert.Equal(
                0,
                restored.CurrentWorld.CurrentTime.TotalSeconds);

            var intervention =
                Assert.Single(
                    restored.Timeline.Events);

            Assert.Equal(
                "User intervention",
                intervention.Cause);

            Assert.Equal(
                planet.Id,
                intervention.AffectedPlanetId);

            Assert.Equal(
                0,
                intervention.ElapsedSeconds);

            restored.Advance(
                60);

            Assert.Equal(
                60,
                restored.CurrentWorld.CurrentTime.TotalSeconds);

            var advancedState =
                Assert.Single(
                    restored.CurrentWorld.SeasonalStates);

            Assert.Equal(
                SeasonalControlMode.Override,
                advancedState.ControlMode);

            Assert.Equal(
                overrideContext,
                advancedState.EffectiveContext);

            Assert.Equal(
                0,
                original.CurrentWorld.CurrentTime.TotalSeconds);

            Assert.Equal(
                SeasonalControlMode.Override,
                Assert.Single(
                    original.CurrentWorld.SeasonalStates)
                    .ControlMode);
        }
        finally
        {
            var directory =
                Path.GetDirectoryName(
                    path)!;

            if (Directory.Exists(
                    directory))
            {
                Directory.Delete(
                    directory,
                    recursive: true);
            }
        }
    }

    [Fact]
    public void SaveAndLoad_PreservesSimulationDefinition()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "Est.Tests",
            Guid.NewGuid().ToString("N"),
            "timeline.json");

        try
        {
            var manager =
                new SimulationSessionManager();

            var service =
                new SimulationSessionArchiveService(
                    manager,
                    new TimelineArchiveFileStore());

            var planet =
                new PlanetState(
                    PlanetId.New(),
                    "Earth",
                    5.9722e24,
                    6_371_000,
                    new PlanetEnvironment(
                        288.15,
                        0.71,
                        0.03,
                        AtmosphereState.Vacuum));

            var world =
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet]);

            var parameters =
                new PlanetaryEnergyBalanceParameters(
                    1361,
                    0.61,
                    1.0e8,
                    0.30,
                    0.60,
                    263.15,
                    273.15,
                    31_536_000);

            var seasonalParameters =
                new CircularOrbitSeasonalParameters(
                    orbitalPeriodSeconds: 400,
                    axialTiltDegrees: 23.5,
                    cycleFractionAtTimeZero: 0.125);

            var definition =
                new SimulationDefinition(
                [
                    new PlanetaryEnergyBalanceModelDefinition(
                        planet.Id,
                        parameters)
                ],
                seasonalModels:
                [
                    new CircularOrbitSeasonalModelDefinition(
                        planet.Id,
                        seasonalParameters)
                ]);

            var originalId =
                manager.Create(
                    world,
                    definition);

            service.Save(
                originalId,
                path,
                new TimelineArchiveProvenance(
                    "Est",
                    "0.1.0-alpha",
                    "simulation"));

            var restoredId =
                service.Load(path);

            var restored =
                manager.Get(restoredId);

            var model =
                Assert.Single(
                    restored.Definition
                        .PlanetaryEnergyBalanceModels);

            Assert.Equal(
                planet.Id,
                model.PlanetId);

            Assert.Equal(
                parameters,
                model.Parameters);

            var seasonalModel =
                Assert.Single(
                    restored.Definition
                        .SeasonalModels);

            Assert.Equal(
                planet.Id,
                seasonalModel.PlanetId);

            Assert.Equal(
                seasonalParameters,
                seasonalModel.Parameters);

            restored.Advance(50);

            var seasonalState =
                Assert.Single(
                    restored.CurrentWorld
                        .SeasonalStates);

            Assert.Equal(
                SeasonalControlMode.Derived,
                seasonalState.ControlMode);

            Assert.Equal(
                0.25,
                seasonalState.DerivedContext!
                    .CycleFraction!.Value,
                precision: 12);
        }
        finally
        {
            var directory =
                Path.GetDirectoryName(path)!;

            if (Directory.Exists(directory))
            {
                Directory.Delete(
                    directory,
                    recursive: true);
            }
        }
    }


}
