using Est.Application.Sessions;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Social;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Application.Tests.Sessions;

public sealed class ManifestedEsterSessionTests
{
    [Fact]
    public void ManifestAndMove_AreAuthoritativeZeroTimeInterventions()
    {
        var planet = CreateEarth();

        var session =
            new SimulationSession(
                new WorldState(
                    WorldId.New(),
                    new SimulationTime(600),
                    [planet]));

        var esterId =
            EsterId.New();

        var originalTimelineId =
            session.Timeline.Id;

        session.ManifestEster(
            esterId,
            planet.Id,
            47.0379,
            -122.9007);

        var manifested =
            session.GetManifestedEster(
                esterId);

        Assert.NotNull(
            manifested);

        Assert.Equal(
            47.0379,
            manifested.LatitudeDegrees);

        Assert.Equal(
            -122.9007,
            manifested.LongitudeDegrees);

        Assert.Equal(
            600,
            session.CurrentWorld
                .CurrentTime
                .TotalSeconds);

        Assert.Equal(
            originalTimelineId,
            session.Timeline.Id);

        session.MoveManifestedEster(
            esterId,
            47.0381,
            -122.9004);

        var moved =
            session.GetManifestedEster(
                esterId);

        Assert.NotNull(
            moved);

        Assert.Equal(
            esterId,
            moved.EsterId);

        Assert.Equal(
            planet.Id,
            moved.PlanetId);

        Assert.Equal(
            47.0381,
            moved.LatitudeDegrees);

        Assert.Equal(
            -122.9004,
            moved.LongitudeDegrees);

        Assert.Equal(
            600,
            session.CurrentWorld
                .CurrentTime
                .TotalSeconds);
    }

    [Fact]
    public void Query_UnknownEsterIsNonMutating()
    {
        var planet = CreateEarth();

        var session =
            new SimulationSession(
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet]));

        var before =
            session.Timeline;

        Assert.Null(
            session.GetManifestedEster(
                EsterId.New()));

        Assert.Same(
            before,
            session.Timeline);
    }

    [Fact]
    public void Move_EnteringPersonRangeRecordsEncounterOnceWhileRemainingInside()
    {
        var planet =
            CreateEarth();

        var person =
            CreatePerson(
                planet.Id,
                northMeters: 0);

        var session =
            new SimulationSession(
                new WorldState(
                    WorldId.New(),
                    new SimulationTime(
                        600),
                    [planet],
                    [person]));

        var esterId =
            EsterId.New();

        var actor =
            SocialActorIdentity.ForEster(
                esterId);

        session.ManifestEster(
            esterId,
            planet.Id,
            LatitudeForNorthMeters(
                10,
                planet.MeanRadiusMeters),
            0);

        Assert.Null(
            session.GetPersonSocialContact(
                person.Id,
                actor));

        var firstMove =
            session.MoveManifestedEsterWithEncounters(
                esterId,
                LatitudeForNorthMeters(
                    2,
                    planet.MeanRadiusMeters),
                0);

        var firstEncounter =
            Assert.Single(
                firstMove.Encounters);

        Assert.Equal(
            person.Id,
            firstEncounter.PersonId);

        Assert.False(
            firstEncounter
                .RecognizedBeforeEncounter);

        Assert.Equal(
            0,
            firstEncounter
                .EncounterCountBefore);

        Assert.Equal(
            1,
            firstEncounter
                .EncounterCountAfter);

        var firstContact =
            session.GetPersonSocialContact(
                person.Id,
                actor);

        Assert.NotNull(
            firstContact);

        Assert.Equal(
            1,
            firstContact.EncounterCount);

        Assert.Equal(
            600,
            firstContact
                .FirstEncounterTimeSeconds);

        Assert.Equal(
            600,
            firstContact
                .LastEncounterTimeSeconds);

        var insideMove =
            session.MoveManifestedEsterWithEncounters(
                esterId,
                LatitudeForNorthMeters(
                    1,
                    planet.MeanRadiusMeters),
                0);

        Assert.Empty(
            insideMove.Encounters);

        var stillInsideContact =
            session.GetPersonSocialContact(
                person.Id,
                actor);

        Assert.NotNull(
            stillInsideContact);

        Assert.Equal(
            1,
            stillInsideContact
                .EncounterCount);

        Assert.Equal(
            600,
            session.CurrentWorld
                .CurrentTime
                .TotalSeconds);
    }

    [Fact]
    public void Move_LeavingAndReenteringPersonRangeRecordsSecondEncounter()
    {
        var planet =
            CreateEarth();

        var person =
            CreatePerson(
                planet.Id,
                northMeters: 0);

        var session =
            new SimulationSession(
                new WorldState(
                    WorldId.New(),
                    new SimulationTime(
                        900),
                    [planet],
                    [person]));

        var esterId =
            EsterId.New();

        var actor =
            SocialActorIdentity.ForEster(
                esterId);

        session.ManifestEster(
            esterId,
            planet.Id,
            LatitudeForNorthMeters(
                10,
                planet.MeanRadiusMeters),
            0);

        var firstMove =
            session.MoveManifestedEsterWithEncounters(
                esterId,
                LatitudeForNorthMeters(
                    2,
                    planet.MeanRadiusMeters),
                0);

        var firstEncounter =
            Assert.Single(
                firstMove.Encounters);

        Assert.False(
            firstEncounter
                .RecognizedBeforeEncounter);

        Assert.Equal(
            0,
            firstEncounter
                .EncounterCountBefore);

        Assert.Equal(
            1,
            firstEncounter
                .EncounterCountAfter);

        var firstContact =
            session.GetPersonSocialContact(
                person.Id,
                actor);

        Assert.NotNull(
            firstContact);

        Assert.Equal(
            1,
            firstContact.EncounterCount);

        var leaveMove =
            session.MoveManifestedEsterWithEncounters(
                esterId,
                LatitudeForNorthMeters(
                    5,
                    planet.MeanRadiusMeters),
                0);

        Assert.Empty(
            leaveMove.Encounters);

        var afterLeaving =
            session.GetPersonSocialContact(
                person.Id,
                actor);

        Assert.NotNull(
            afterLeaving);

        Assert.Equal(
            1,
            afterLeaving.EncounterCount);

        var returnMove =
            session.MoveManifestedEsterWithEncounters(
                esterId,
                LatitudeForNorthMeters(
                    2,
                    planet.MeanRadiusMeters),
                0);

        var returnEncounter =
            Assert.Single(
                returnMove.Encounters);

        Assert.Equal(
            person.Id,
            returnEncounter.PersonId);

        Assert.True(
            returnEncounter
                .RecognizedBeforeEncounter);

        Assert.Equal(
            1,
            returnEncounter
                .EncounterCountBefore);

        Assert.Equal(
            2,
            returnEncounter
                .EncounterCountAfter);

        var returnedContact =
            session.GetPersonSocialContact(
                person.Id,
                actor);

        Assert.NotNull(
            returnedContact);

        Assert.Equal(
            2,
            returnedContact
                .EncounterCount);

        Assert.Equal(
            900,
            returnedContact
                .FirstEncounterTimeSeconds);

        Assert.Equal(
            900,
            returnedContact
                .LastEncounterTimeSeconds);

        Assert.Equal(
            900,
            session.CurrentWorld
                .CurrentTime
                .TotalSeconds);
    }

    [Fact]
    public void Move_StoppingOutsidePersonRangeDoesNotRecordEncounter()
    {
        var planet =
            CreateEarth();

        var person =
            CreatePerson(
                planet.Id,
                northMeters: 0);

        var session =
            new SimulationSession(
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet],
                    [person]));

        var esterId =
            EsterId.New();

        var actor =
            SocialActorIdentity.ForEster(
                esterId);

        session.ManifestEster(
            esterId,
            planet.Id,
            LatitudeForNorthMeters(
                10,
                planet.MeanRadiusMeters),
            0);

        session.MoveManifestedEster(
            esterId,
            LatitudeForNorthMeters(
                3,
                planet.MeanRadiusMeters),
            0);

        Assert.Null(
            session.GetPersonSocialContact(
                person.Id,
                actor));
    }

    private static PersonState CreatePerson(
        PlanetId planetId,
        double northMeters)
    {
        const double earthRadiusMeters =
            6_371_000;

        return new PersonState(
            PersonId.New(),
            planetId,
            PersonSex.Female,
            birthTimeSeconds: 0,
            latitudeDegrees:
                LatitudeForNorthMeters(
                    northMeters,
                    earthRadiusMeters),
            longitudeDegrees: 0);
    }

    private static double LatitudeForNorthMeters(
        double northMeters,
        double planetRadiusMeters)
    {
        return northMeters /
               planetRadiusMeters *
               180d /
               Math.PI;
    }

    private static PlanetState CreateEarth()
    {
        return new PlanetState(
            PlanetId.New(),
            "Earth",
            5.9722e24,
            6_371_000,
            new PlanetEnvironment(
                288.15,
                0.71,
                0.03,
                new AtmosphereState(
                    101_325,
                    new Dictionary<string, double>
                    {
                        ["N2"] = 0.78,
                        ["O2"] = 0.21,
                        ["Ar"] = 0.01
                    })));
    }
}
