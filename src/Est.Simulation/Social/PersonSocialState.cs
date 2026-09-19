using System.Collections.Immutable;

namespace Est.Simulation.Social;

public sealed class PersonSocialState
    : IEquatable<PersonSocialState>
{
    private readonly ImmutableDictionary<
        SocialActorIdentity,
        PersonSocialContactState> _contacts;

    public PersonSocialState(
        IEnumerable<PersonSocialContactState>? contacts = null)
    {
        var builder =
            ImmutableDictionary.CreateBuilder<
                SocialActorIdentity,
                PersonSocialContactState>();

        foreach (var contact in contacts ?? [])
        {
            ArgumentNullException.ThrowIfNull(contact);

            if (!builder.TryAdd(
                    contact.Actor,
                    contact))
            {
                throw new ArgumentException(
                    "Social state cannot contain duplicate actor contacts.",
                    nameof(contacts));
            }
        }

        _contacts = builder.ToImmutable();
    }

    private PersonSocialState(
        ImmutableDictionary<
            SocialActorIdentity,
            PersonSocialContactState> contacts)
    {
        _contacts = contacts;
    }

    public static PersonSocialState Empty { get; } =
        new();

    public ImmutableArray<PersonSocialContactState>
        Contacts =>
        _contacts.Values
            .OrderBy(contact => contact.Actor.Kind)
            .ThenBy(contact => contact.Actor.Value)
            .ToImmutableArray();

    public bool HasEncountered(
        SocialActorIdentity actor)
    {
        return _contacts.ContainsKey(actor);
    }

    public PersonSocialContactState? GetContact(
        SocialActorIdentity actor)
    {
        return _contacts.TryGetValue(
            actor,
            out var contact)
            ? contact
            : null;
    }

    public PersonSocialState RecordEncounter(
        SocialActorIdentity actor,
        long encounterTimeSeconds)
    {
        if (_contacts.TryGetValue(
                actor,
                out var existing))
        {
            return new PersonSocialState(
                _contacts.SetItem(
                    actor,
                    existing.RecordEncounter(
                        encounterTimeSeconds)));
        }

        var contact =
            new PersonSocialContactState(
                actor,
                encounterTimeSeconds,
                encounterTimeSeconds);

        return new PersonSocialState(
            _contacts.Add(
                actor,
                contact));
    }

    public bool Equals(PersonSocialState? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null ||
            _contacts.Count != other._contacts.Count)
        {
            return false;
        }

        foreach (var pair in _contacts)
        {
            if (!other._contacts.TryGetValue(
                    pair.Key,
                    out var otherContact) ||
                pair.Value != otherContact)
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is PersonSocialState other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var contact in Contacts)
        {
            hash.Add(contact);
        }

        return hash.ToHashCode();
    }
}
