namespace Est.Simulation.Animals;

public readonly record struct AnimalId(Guid Value)
{
    public static AnimalId New() => new(Guid.NewGuid());
}
