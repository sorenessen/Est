namespace Est.Simulation.Population;

public sealed record PregnancyState
{
    public PregnancyState(
        long conceptionTimeSeconds,
        PersonId fatherId)
    {
        if (fatherId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Father identity cannot be empty.",
                nameof(fatherId));
        }

        ConceptionTimeSeconds = conceptionTimeSeconds;
        FatherId = fatherId;
    }

    public long ConceptionTimeSeconds { get; }

    public PersonId FatherId { get; }
}
