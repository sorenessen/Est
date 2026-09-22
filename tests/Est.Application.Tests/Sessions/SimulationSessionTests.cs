using Est.Application.Sessions;
using Est.Simulation.Climate;
using Est.Simulation.Definitions;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Social;
using Est.Simulation.Seasons;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Application.Tests.Sessions;

public sealed class SimulationSessionTests
{
    [Fact]
    public void Create_PreservesInitialWorld()
    {
        var world = new WorldState(WorldId.New(), SimulationTime.Zero);
        var session = new SimulationSession(world);

        Assert.Equal(world.Id, session.CurrentWorld.Id);
        Assert.Equal(0, session.CurrentWorld.CurrentTime.TotalSeconds);
        Assert.Single(session.Timeline.Checkpoints);
        Assert.Empty(session.Timeline.Events);
    }

    [Fact]
    public void Advance_UpdatesTimeAndRecordsHistory()
    {
        var session = CreateSession();

        var timeline = session.Advance(60);

        Assert.Equal(60, timeline.CurrentWorld.CurrentTime.TotalSeconds);
        Assert.Single(timeline.Events);
        Assert.Equal(60, timeline.Events[0].ElapsedSeconds);
        Assert.Equal(timeline.Id, timeline.Events[0].TimelineId);
    }

    [Fact]
    public void ExplicitAdvance_WorksWhilePaused()
    {
        var session = CreateSession();
        session.Pause();

        session.Advance(30);

        Assert.True(session.IsPaused);
        Assert.Equal(30, session.CurrentWorld.CurrentTime.TotalSeconds);
    }

    [Fact]
    public void Tick_RespectsPauseAndResume()
    {
        var session = CreateSession();
        session.Pause();

        session.Tick(60);
        Assert.Equal(0, session.CurrentWorld.CurrentTime.TotalSeconds);
        Assert.Empty(session.Timeline.Events);

        session.Resume();
        session.Tick(60);

        Assert.Equal(60, session.CurrentWorld.CurrentTime.TotalSeconds);
        Assert.Single(session.Timeline.Events);
    }

    [Fact]
    public void Create_StartsAtOneTimesSimulationRate()
    {
        var session = CreateSession();

        Assert.Equal(
            1,
            session.SimulationRateMultiplier);
    }

    [Fact]
    public void Tick_AppliesConfiguredSimulationRate()
    {
        var session = CreateSession();

        session.SetSimulationRateMultiplier(
            4);

        session.Tick(
            15);

        Assert.Equal(
            4,
            session.SimulationRateMultiplier);

        Assert.Equal(
            60,
            session.CurrentWorld
                .CurrentTime
                .TotalSeconds);

        Assert.Single(
            session.Timeline.Events);

        Assert.Equal(
            60,
            session.Timeline.Events[0]
                .ElapsedSeconds);
    }

    [Fact]
    public void Pause_PreservesConfiguredSimulationRate()
    {
        var session = CreateSession();

        session.SetSimulationRateMultiplier(
            100);

        session.Pause();

        session.Tick(
            60);

        Assert.True(
            session.IsPaused);

        Assert.Equal(
            100,
            session.SimulationRateMultiplier);

        Assert.Equal(
            0,
            session.CurrentWorld
                .CurrentTime
                .TotalSeconds);

        session.Resume();

        session.Tick(
            1);

        Assert.Equal(
            100,
            session.CurrentWorld
                .CurrentTime
                .TotalSeconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void SetSimulationRateMultiplier_RejectsUnsupportedRate(
        int multiplier)
    {
        var session = CreateSession();

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                session.SetSimulationRateMultiplier(
                    multiplier));

        Assert.Equal(
            1,
            session.SimulationRateMultiplier);
    }

    [Fact]
    public void Advance_RejectsNegativeDurationWithoutChangingState()
    {
        var session = CreateSession();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => session.Advance(-1));

        Assert.Equal(0, session.CurrentWorld.CurrentTime.TotalSeconds);
        Assert.Empty(session.Timeline.Events);
    }

    [Fact]
    public void ReplacePlanetEnvironment_ChangesWorldWithoutAdvancingTime()
    {
        var planet = CreatePlanet();
        var session = new SimulationSession(
            new WorldState(
                WorldId.New(),
                new SimulationTime(120),
                [planet]));

        var originalTimeline = session.Timeline;

        var environment = new PlanetEnvironment(
            300,
            0.65,
            0.02,
            AtmosphereState.Vacuum);

        var timeline = session.ReplacePlanetEnvironment(
            planet.Id,
            environment);

        var changedPlanet = Assert.Single(timeline.CurrentWorld.Planets);

        Assert.Equal(planet.Id, changedPlanet.Id);
        Assert.Equal(planet.Name, changedPlanet.Name);
        Assert.Equal(planet.MassKilograms, changedPlanet.MassKilograms);
        Assert.Equal(planet.MeanRadiusMeters, changedPlanet.MeanRadiusMeters);
        Assert.Same(environment, changedPlanet.Environment);
        Assert.Equal(120, timeline.CurrentWorld.CurrentTime.TotalSeconds);
        Assert.Equal(originalTimeline.Id, timeline.Id);
        Assert.Single(timeline.Events);
        Assert.Equal(0, timeline.Events[0].ElapsedSeconds);
        Assert.Equal(planet.Id, timeline.Events[0].AffectedPlanetId);
        Assert.Equal(120, timeline.Events[0].OccurredAt.TotalSeconds);

        Assert.Empty(originalTimeline.Events);
        Assert.Same(planet.Environment,
            originalTimeline.CurrentWorld.Planets[0].Environment);
    }

    [Fact]
    public void ReplacePlanetEnvironment_RejectsUnknownPlanetWithoutChangingState()
    {
        var planet = CreatePlanet();
        var session = new SimulationSession(
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet]));

        var originalTimeline = session.Timeline;

        Assert.Throws<PlanetNotFoundException>(
            () => session.ReplacePlanetEnvironment(
                PlanetId.New(),
                new PlanetEnvironment(
                    300,
                    0.5,
                    0,
                    AtmosphereState.Vacuum)));

        Assert.Same(originalTimeline, session.Timeline);
        Assert.Empty(session.Timeline.Events);
    }

    [Fact]
    public void GetPersonSocialContact_UnknownActorReturnsNull()
    {
        var planet = CreatePlanet();

        var person =
            new PersonState(
                PersonId.New(),
                planet.Id,
                PersonSex.Female,
                birthTimeSeconds: 0,
                latitudeDegrees: 0,
                longitudeDegrees: 0);

        var actor =
            SocialActorIdentity.ForEster(
                EsterId.New());

        var session =
            new SimulationSession(
                new WorldState(
                    WorldId.New(),
                    new SimulationTime(120),
                    [planet],
                    [person]));

        Assert.Null(
            session.GetPersonSocialContact(
                person.Id,
                actor));

        Assert.Equal(
            120,
            session.CurrentWorld
                .CurrentTime.TotalSeconds);

        Assert.Empty(
            session.Timeline.Events);
    }

    [Fact]
    public void RecordPersonSocialEncounter_RecordsAuthoritativeContactWithoutAdvancingTime()
    {
        var planet = CreatePlanet();

        var person =
            new PersonState(
                PersonId.New(),
                planet.Id,
                PersonSex.Female,
                birthTimeSeconds: 0,
                latitudeDegrees: 0,
                longitudeDegrees: 0);

        var actor =
            SocialActorIdentity.ForEster(
                EsterId.New());

        var session =
            new SimulationSession(
                new WorldState(
                    WorldId.New(),
                    new SimulationTime(120),
                    [planet],
                    [person]));

        var originalTimeline =
            session.Timeline;

        var timeline =
            session.RecordPersonSocialEncounter(
                person.Id,
                actor);

        var contact =
            session.GetPersonSocialContact(
                person.Id,
                actor);

        Assert.NotNull(contact);

        Assert.Equal(
            1,
            contact.EncounterCount);

        Assert.Equal(
            120,
            contact.FirstEncounterTimeSeconds);

        Assert.Equal(
            120,
            contact.LastEncounterTimeSeconds);

        Assert.Equal(
            120,
            timeline.CurrentWorld
                .CurrentTime.TotalSeconds);

        Assert.Equal(
            originalTimeline.Id,
            timeline.Id);

        var intervention =
            Assert.Single(
                timeline.Events);

        Assert.Equal(
            "User intervention",
            intervention.Cause);

        Assert.Equal(
            "Recorded person social encounter.",
            intervention.Summary);

        Assert.Null(
            intervention.AffectedPlanetId);

        Assert.Equal(
            0,
            intervention.ElapsedSeconds);

        Assert.Empty(
            originalTimeline.Events);

        var originalPerson =
            Assert.Single(
                originalTimeline
                    .CurrentWorld
                    .Population);

        Assert.False(
            originalPerson.SocialState
                .HasEncountered(actor));
    }

    [Fact]
    public void RecordPersonSocialEncounter_PreservesRecognitionBeforeRepeatedEncounter()
    {
        var planet = CreatePlanet();

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

        var session =
            new SimulationSession(
                new WorldState(
                    WorldId.New(),
                    new SimulationTime(120),
                    [planet],
                    [person]));

        Assert.Null(
            session.GetPersonSocialContact(
                person.Id,
                returningActor));

        Assert.Null(
            session.GetPersonSocialContact(
                person.Id,
                unknownActor));

        session.RecordPersonSocialEncounter(
            person.Id,
            returningActor);

        session.Advance(
            240);

        var beforeSecondEncounter =
            session.GetPersonSocialContact(
                person.Id,
                returningActor);

        Assert.NotNull(
            beforeSecondEncounter);

        Assert.Equal(
            1,
            beforeSecondEncounter.EncounterCount);

        Assert.Equal(
            120,
            beforeSecondEncounter
                .FirstEncounterTimeSeconds);

        Assert.Equal(
            120,
            beforeSecondEncounter
                .LastEncounterTimeSeconds);

        Assert.Null(
            session.GetPersonSocialContact(
                person.Id,
                unknownActor));

        Assert.Equal(
            360,
            session.CurrentWorld
                .CurrentTime.TotalSeconds);

        session.RecordPersonSocialEncounter(
            person.Id,
            returningActor);

        var afterSecondEncounter =
            session.GetPersonSocialContact(
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

        Assert.Single(
            Assert.Single(
                    session.CurrentWorld
                        .Population)
                .SocialState
                .Contacts);
    }

    [Fact]
    public void Advance_WithoutSeasonalConfigurationPreservesDisabledBehavior()
    {
        var planet =
            CreatePlanet();

        var session =
            new SimulationSession(
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet]));

        session.Advance(100);

        Assert.Empty(
            session.CurrentWorld.SeasonalStates);
    }

    [Fact]
    public void Advance_WithSeasonalConfigurationDerivesResultingState()
    {
        var planet =
            CreatePlanet();

        var definition =
            new SimulationDefinition(
                seasonalModels:
                [
                    new CircularOrbitSeasonalModelDefinition(
                        planet.Id,
                        new CircularOrbitSeasonalParameters(
                            orbitalPeriodSeconds: 400,
                            axialTiltDegrees: 23.5))
                ]);

        var session =
            new SimulationSession(
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet]),
                definition);

        session.Advance(100);

        var state =
            Assert.Single(
                session.CurrentWorld.SeasonalStates);

        Assert.Equal(
            SeasonalControlMode.Derived,
            state.ControlMode);

        Assert.Equal(
            0.25,
            state.DerivedContext!
                .CycleFraction!.Value,
            precision: 12);

        Assert.Equal(
            23.5,
            state.DerivedContext
                .SubsolarLatitudeDegrees!.Value,
            precision: 10);

        Assert.Equal(
            100,
            session.CurrentWorld
                .CurrentTime.TotalSeconds);
    }

    [Fact]
    public void Advance_WithOverrideUpdatesDerivedContextButKeepsOverrideEffective()
    {
        var planet =
            CreatePlanet();

        var definition =
            new SimulationDefinition(
                seasonalModels:
                [
                    new CircularOrbitSeasonalModelDefinition(
                        planet.Id,
                        new CircularOrbitSeasonalParameters(
                            orbitalPeriodSeconds: 400,
                            axialTiltDegrees: 23.5))
                ]);

        var session =
            new SimulationSession(
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet]),
                definition);

        var overridden =
            new SeasonalContext(
                "forced-cold",
                0.90);

        session.OverridePlanetSeasonalState(
            planet.Id,
            overridden);

        session.Advance(100);

        var state =
            Assert.Single(
                session.CurrentWorld.SeasonalStates);

        Assert.Equal(
            SeasonalControlMode.Override,
            state.ControlMode);

        Assert.Equal(
            0.25,
            state.DerivedContext!
                .CycleFraction!.Value,
            precision: 12);

        Assert.Equal(
            23.5,
            state.DerivedContext
                .SubsolarLatitudeDegrees!.Value,
            precision: 10);

        Assert.Equal(
            overridden,
            state.OverrideContext);

        Assert.Equal(
            overridden,
            state.EffectiveContext);
    }

    [Fact]
    public void OverridePlanetSeasonalState_ChangesEffectiveContextWithoutAdvancingTime()
    {
        var planet = CreatePlanet();

        var derivedContext =
            new SeasonalContext(
                "warm",
                0.25);

        var originalSeasonalState =
            new PlanetSeasonalState(
                planet.Id,
                SeasonalControlMode.Derived,
                derivedContext:
                    derivedContext);

        var session =
            new SimulationSession(
                new WorldState(
                    WorldId.New(),
                    new SimulationTime(
                        120),
                    [planet],
                    [],
                    seasonalStates:
                        [originalSeasonalState]));

        var originalTimeline =
            session.Timeline;

        var overrideContext =
            new SeasonalContext(
                "cold",
                0.75);

        var timeline =
            session.OverridePlanetSeasonalState(
                planet.Id,
                overrideContext);

        var seasonalState =
            Assert.Single(
                timeline.CurrentWorld.SeasonalStates);

        Assert.Equal(
            SeasonalControlMode.Override,
            seasonalState.ControlMode);

        Assert.Equal(
            derivedContext,
            seasonalState.DerivedContext);

        Assert.Equal(
            overrideContext,
            seasonalState.OverrideContext);

        Assert.Equal(
            overrideContext,
            seasonalState.EffectiveContext);

        Assert.Equal(
            120,
            timeline.CurrentWorld.CurrentTime.TotalSeconds);

        Assert.Equal(
            originalTimeline.Id,
            timeline.Id);

        var intervention =
            Assert.Single(
                timeline.Events);

        Assert.Equal(
            0,
            intervention.ElapsedSeconds);

        Assert.Equal(
            planet.Id,
            intervention.AffectedPlanetId);

        Assert.Equal(
            120,
            intervention.OccurredAt.TotalSeconds);

        Assert.Equal(
            "User intervention",
            intervention.Cause);

        Assert.Empty(
            originalTimeline.Events);

        var originalState =
            Assert.Single(
                originalTimeline.CurrentWorld.SeasonalStates);

        Assert.Equal(
            SeasonalControlMode.Derived,
            originalState.ControlMode);

        Assert.Equal(
            derivedContext,
            originalState.EffectiveContext);
    }

    [Fact]
    public void OverridePlanetSeasonalState_RejectsUnknownPlanetWithoutChangingState()
    {
        var planet =
            CreatePlanet();

        var session =
            new SimulationSession(
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet]));

        var originalTimeline =
            session.Timeline;

        Assert.Throws<PlanetNotFoundException>(
            () =>
                session.OverridePlanetSeasonalState(
                    PlanetId.New(),
                    new SeasonalContext(
                        "cold")));

        Assert.Same(
            originalTimeline,
            session.Timeline);

        Assert.Empty(
            session.Timeline.Events);

        Assert.Empty(
            session.CurrentWorld.SeasonalStates);
    }

    private static PlanetState CreatePlanet() =>
        new(
            PlanetId.New(),
            "Test Planet",
            5.9722e24,
            6_371_000,
            new PlanetEnvironment(
                288.15,
                0.71,
                0.1,
                AtmosphereState.Vacuum));


    [Fact]
    public void Advance_AppliesConfiguredEnergyBalanceModel()
    {
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

        var definition =
            new SimulationDefinition(
            [
                new PlanetaryEnergyBalanceModelDefinition(
                    planet.Id,
                    parameters)
            ]);

        var session =
            new SimulationSession(
                world,
                definition);

        var timeline =
            session.Advance(3600);

        Assert.Equal(
            3600,
            timeline.CurrentWorld.CurrentTime.TotalSeconds);

        var changedPlanet =
            Assert.Single(
                timeline.CurrentWorld.Planets);

        Assert.NotEqual(
            planet.Environment.MeanSurfaceTemperatureKelvin,
            changedPlanet.Environment.MeanSurfaceTemperatureKelvin);

        Assert.NotEqual(
            planet.Environment.IceCoverageFraction,
            changedPlanet.Environment.IceCoverageFraction);

        Assert.Single(timeline.Events);

        Assert.Equal(
            "planetary-energy-balance",
            timeline.Events[0].Cause);

        Assert.Equal(
            planet.Id,
            timeline.Events[0].AffectedPlanetId);

        Assert.Equal(
            3600,
            timeline.Events[0].ElapsedSeconds);
    }

    [Fact]
    public void Advance_WithEmptyDefinitionOnlyAdvancesTime()
    {
        var session =
            CreateSession();

        var timeline =
            session.Advance(3600);

        Assert.Equal(
            3600,
            timeline.CurrentWorld.CurrentTime.TotalSeconds);

        Assert.Single(timeline.Events);

        Assert.Equal(
            "Explicit time advancement",
            timeline.Events[0].Cause);
    }


    private static SimulationSession CreateSession() =>
        new(new WorldState(WorldId.New(), SimulationTime.Zero));
}
