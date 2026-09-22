namespace Est.Api.Contracts;

public sealed record SessionResponse(
    Guid SessionId,
    Guid WorldId,
    Guid TimelineId,
    long CurrentTimeSeconds,
    bool IsPaused,
    int SimulationRateMultiplier,
    int PlanetCount,
    int EventCount,
    int CheckpointCount);
