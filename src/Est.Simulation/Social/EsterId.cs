namespace Est.Simulation.Social;

public readonly record struct EsterId
{
    public EsterId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Ester identity cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public static EsterId New()
    {
        return new EsterId(Guid.NewGuid());
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
