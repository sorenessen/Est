using Est.Simulation.Population;

namespace Est.Simulation.Social;

public readonly record struct SocialActorIdentity
{
    public SocialActorIdentity(
        SocialActorKind kind,
        Guid value)
    {
        if (!Enum.IsDefined(
                typeof(SocialActorKind),
                kind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                "Social actor kind is not supported.");
        }

        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Social actor identity cannot be empty.",
                nameof(value));
        }

        Kind = kind;
        Value = value;
    }

    public SocialActorKind Kind { get; }

    public Guid Value { get; }

    public static SocialActorIdentity ForPerson(
        PersonId personId)
    {
        return new SocialActorIdentity(
            SocialActorKind.Person,
            personId.Value);
    }

    public static SocialActorIdentity ForEster(
        EsterId esterId)
    {
        return new SocialActorIdentity(
            SocialActorKind.Ester,
            esterId.Value);
    }

    public override string ToString()
    {
        return $"{Kind}:{Value}";
    }
}
