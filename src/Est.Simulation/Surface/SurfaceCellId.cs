namespace Est.Simulation.Surface;

public readonly record struct SurfaceCellId
{
    public SurfaceCellId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Surface cell identity cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }
}
