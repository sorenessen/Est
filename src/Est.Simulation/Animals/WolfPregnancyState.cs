namespace Est.Simulation.Animals;

public sealed record WolfPregnancyState
{
    public WolfPregnancyState(
        long conceptionTimeSeconds,
        AnimalId fatherId)
    {
        if (fatherId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Father identity cannot be empty.",
                nameof(fatherId));
        }

        ConceptionTimeSeconds =
            conceptionTimeSeconds;

        FatherId =
            fatherId;
    }

    public long ConceptionTimeSeconds { get; }

    public AnimalId FatherId { get; }
}
