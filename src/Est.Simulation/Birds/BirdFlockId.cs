namespace Est.Simulation.Birds;

public readonly record struct BirdFlockId(Guid Value)
{
    public static BirdFlockId New()
    {
        return new BirdFlockId(
            Guid.NewGuid());
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
