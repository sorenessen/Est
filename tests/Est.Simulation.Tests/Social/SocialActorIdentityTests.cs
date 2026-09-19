using Est.Simulation.Population;
using Est.Simulation.Social;

namespace Est.Simulation.Tests.Social;

public sealed class SocialActorIdentityTests
{
    [Fact]
    public void EsterId_RejectsEmptyIdentity()
    {
        Assert.Throws<ArgumentException>(
            () => new EsterId(Guid.Empty));
    }

    [Fact]
    public void EsterId_DistinguishesDifferentIdentities()
    {
        var first = EsterId.New();
        var second = EsterId.New();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ActorIdentity_DistinguishesPersonAndEsterWithSameGuid()
    {
        var value = Guid.NewGuid();

        var person =
            SocialActorIdentity.ForPerson(
                new PersonId(value));

        var ester =
            SocialActorIdentity.ForEster(
                new EsterId(value));

        Assert.Equal(
            SocialActorKind.Person,
            person.Kind);

        Assert.Equal(
            SocialActorKind.Ester,
            ester.Kind);

        Assert.NotEqual(person, ester);
    }
}
