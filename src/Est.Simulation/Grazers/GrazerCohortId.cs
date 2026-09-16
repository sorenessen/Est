namespace Est.Simulation.Grazers;

public readonly record struct GrazerCohortId(Guid Value)
{
    public static GrazerCohortId New()
    {
        return new GrazerCohortId(
            Guid.NewGuid());
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
