namespace Est.Simulation.Ecology;

public readonly record struct FoodResourceId(Guid Value)
{
    public static FoodResourceId New() =>
        new(Guid.NewGuid());

    public override string ToString() =>
        Value.ToString();
}
