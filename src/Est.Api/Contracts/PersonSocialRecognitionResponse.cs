namespace Est.Api.Contracts;

public sealed record PersonSocialRecognitionResponse(
    Guid PersonId,
    Guid EsterId,
    bool Recognized,
    long? FirstEncounterTimeSeconds,
    long? LastEncounterTimeSeconds,
    long EncounterCount);
