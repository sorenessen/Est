namespace Est.Simulation.Social;

public sealed record PersonSocialContactState
{
    public PersonSocialContactState(
        SocialActorIdentity actor,
        long firstEncounterTimeSeconds,
        long lastEncounterTimeSeconds,
        long encounterCount = 1)
    {
        if (lastEncounterTimeSeconds <
            firstEncounterTimeSeconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lastEncounterTimeSeconds),
                "Last encounter time cannot precede first encounter time.");
        }

        if (encounterCount < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(encounterCount),
                "Encounter count must be at least one.");
        }

        Actor = actor;
        FirstEncounterTimeSeconds =
            firstEncounterTimeSeconds;
        LastEncounterTimeSeconds =
            lastEncounterTimeSeconds;
        EncounterCount = encounterCount;
    }

    public SocialActorIdentity Actor { get; }

    public long FirstEncounterTimeSeconds { get; }

    public long LastEncounterTimeSeconds { get; }

    public long EncounterCount { get; }

    public PersonSocialContactState RecordEncounter(
        long encounterTimeSeconds)
    {
        if (encounterTimeSeconds <
            LastEncounterTimeSeconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(encounterTimeSeconds),
                "Encounter time cannot precede the most recent encounter.");
        }

        return new PersonSocialContactState(
            Actor,
            FirstEncounterTimeSeconds,
            encounterTimeSeconds,
            checked(EncounterCount + 1));
    }
}
