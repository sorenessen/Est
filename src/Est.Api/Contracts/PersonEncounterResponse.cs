namespace Est.Api.Contracts;

public sealed record PersonEncounterResponse(
    Guid PersonId,
    bool RecognizedBeforeEncounter,
    long EncounterCountBefore,
    long EncounterCountAfter);
