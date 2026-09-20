using Est.Simulation.Population;
using Est.Simulation.Social;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

public sealed record RecordPersonSocialEncounterOperation
    : ISimulationOperation
{
    public RecordPersonSocialEncounterOperation(
        PersonId personId,
        SocialActorIdentity actor)
    {
        if (personId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Person identity cannot be empty.",
                nameof(personId));
        }

        if (actor.Value == Guid.Empty ||
            !Enum.IsDefined(
                typeof(SocialActorKind),
                actor.Kind))
        {
            throw new ArgumentException(
                "Social actor identity must be valid and nonempty.",
                nameof(actor));
        }

        if (actor.Kind == SocialActorKind.Person &&
            actor.Value == personId.Value)
        {
            throw new ArgumentException(
                "A person cannot encounter themselves as a social actor.",
                nameof(actor));
        }

        PersonId = personId;
        Actor = actor;
    }

    public PersonId PersonId { get; }

    public SocialActorIdentity Actor { get; }

    public WorldState Apply(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var person =
            world.Population.FirstOrDefault(
                candidate =>
                    candidate.Id == PersonId)
            ?? throw new InvalidOperationException(
                "The target person does not exist in this world.");

        var updatedSocialState =
            person.SocialState.RecordEncounter(
                Actor,
                world.CurrentTime.TotalSeconds);

        var updatedPerson =
            person.WithSocialState(
                updatedSocialState);

        var updatedPopulation =
            world.Population.Select(
                candidate =>
                    candidate.Id == PersonId
                        ? updatedPerson
                        : candidate);

        return world.ReplacePopulation(
            updatedPopulation);
    }
}
