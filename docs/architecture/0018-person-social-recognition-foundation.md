# ADR 0018: Person Social Recognition Foundation

- Status: Accepted
- Decision date: 2026-09-19

## Context

Est's human population already has authoritative persistent individual identity.

A `PersonState` currently carries concepts including:

- `PersonId`;
- planet identity;
- sex;
- birth time;
- location;
- parent identity;
- physiological needs;
- current activity;
- pregnancy;
- organism material.

`PersonState` is immutable.

State transitions such as movement, survival changes, pregnancy changes, and
material changes create a replacement `PersonState` while preserving the
person's existing authoritative identity and unrelated state.

`WorldState` owns the authoritative population.

Population state is preserved through:

- world copies;
- timeline checkpoints;
- timeline forks;
- world snapshots;
- timeline archives.

ADR 0008 intentionally deferred the final cognition, knowledge, and technology
architecture.

The Living Worlds experience now requires a smaller prerequisite before those
larger systems.

A person who encounters the player should not behave indefinitely as though
that encounter never happened.

The first useful social proof is therefore not:

- natural-language conversation;
- friendship;
- romance;
- reputation;
- religion;
- institutions;
- economic roles;
- a complete episodic-memory system.

It is:

**persistent actor recognition.**

The authoritative world must be able to represent that a particular person has
encountered a particular actor before.

That fact must survive time, persistence, and timeline branching.

## Decision

Est will introduce a minimal authoritative social-recognition foundation owned
by each `PersonState`.

The intended conceptual chain is:

    social actor identity
      -> person social state
      -> persistent contact state
      -> recognized-versus-unknown distinction

This is the first social causal slice.

It establishes continuity between encounters without defining the final
cognition architecture.

## Stable Ester identity

The simulation needs a stable identity for an Est player.

A new simulation-domain identity concept will represent an Ester independently
of runtime session identity.

The working type name is:

`EsterId`

An `EsterId` must:

- be stable enough to refer to the same Ester across repeated encounters;
- survive world save and load when stored in world history or social state;
- be usable in more than one independent world;
- reject an empty identity;
- remain distinct from simulated human identity.

`EsterId` must not be an alias for:

- `SimulationSessionId`;
- `WorldId`;
- `TimelineId`;
- `PersonId`.

`SimulationSessionId` is an application/runtime handle.

A new session identity is created when a simulation session is created or
restored.

It therefore cannot mean:

> the same Ester this person encountered previously.

The eventual account, authentication, networking, invitation, and shared-world
systems may supply an `EsterId`.

They do not define its simulation semantics.

## Social actor identity

Recognition must not be limited to Esters forever.

People will eventually need to recognize simulated people as well.

The first implementation will therefore use a simulation-level social actor
identity capable of representing at least:

- a simulated person;
- an Ester.

The working concepts are:

`SocialActorIdentity`

and an associated actor kind such as:

- `Person`;
- `Ester`.

The exact C# representation is an implementation detail.

It may be a record, record struct, tagged identity, or another immutable value
type.

The required semantics are:

1. actor kind is explicit;
2. actor identity is non-empty;
3. a person identity and Ester identity cannot compare equal merely because
   their underlying GUID values happen to match;
4. equality is deterministic;
5. the type can be persisted without presentation or account dependencies.

The first implementation does not need to generalize this identity to:

- institutions;
- households;
- animals;
- places;
- governments;
- unknown anonymous actors.

Those may justify additional actor or subject identity concepts later.

## Person social state

`PersonState` will gain authoritative social state.

The working type name is:

`PersonSocialState`.

A new person defaults to an empty social state.

`PersonSocialState` is immutable.

The initial state owns a collection of known social contacts.

The collection is keyed semantically by `SocialActorIdentity`.

It must not contain duplicate contacts for the same actor.

The exact collection representation is an implementation detail.

## Social contact state

The first recognition record is deliberately small.

The working type name is:

`PersonSocialContactState`.

For one remembered actor it contains:

- actor identity;
- first encounter simulation time;
- most recent encounter simulation time;
- encounter count.

Conceptually:

    actor
    firstEncounterTimeSeconds
    lastEncounterTimeSeconds
    encounterCount

The following invariants apply:

- encounter count is at least one;
- first encounter time is not after the most recent encounter time;
- encounter times are authoritative simulation times;
- repeated contact with the same actor updates the existing contact rather than
  creating a duplicate;
- the first encounter time remains stable across later encounters;
- the most recent encounter time advances when a later encounter is recorded;
- encounter count increments exactly once for each recorded encounter.

A person must not contain a social contact identifying themselves as another
person actor.

## Recognition is not relationship

A known contact means only:

> this person has encountered this actor before.

It does not yet mean:

- friendship;
- trust;
- affection;
- hostility;
- fear;
- attraction;
- respect;
- obligation;
- reputation;
- belief;
- agreement;
- factual knowledge about the actor.

Those concepts require their own authoritative models.

Recognition must not silently infer them.

For example:

    encountered Ester X twice

does not imply:

    trusts Ester X

or:

    likes Ester X.

This separation allows later relationship dimensions to evolve from actual
events rather than being hidden inside a recognition counter.

## Recognition is not complete memory

`PersonSocialContactState` is a first continuity primitive.

It is not the final human memory architecture.

It does not initially store:

- dialogue transcripts;
- arbitrary natural-language memories;
- beliefs;
- propositions;
- witnessed event details;
- emotional interpretation;
- promises;
- crimes;
- rumors;
- event confidence;
- forgetting;
- false memories.

Those remain future cognition work.

The first milestone answers only:

> Have I encountered this particular actor before?

and, if so:

> When did I first encounter them, when did I last encounter them, and how many
> encounters have been recorded?

## Encounter recording

An encounter must enter authoritative state through an explicit simulation
state change.

The implementation may use a dedicated operation or equivalent authoritative
mutation boundary.

The working operation concept is:

`RecordPersonSocialEncounterOperation`.

The operation identifies:

- the person whose social state changes;
- the social actor encountered.

The encounter time comes from authoritative `WorldState.CurrentTime`.

The operation must not accept presentation time, wall-clock time, or a
renderer-generated timestamp as simulation truth.

When applied:

1. the target person must exist;
2. the actor identity must be valid;
3. a person cannot record themselves as a social contact;
4. an unknown actor creates a new contact;
5. an already-known actor updates the existing contact;
6. all unrelated person state remains unchanged;
7. all unrelated world state remains unchanged.

The exact operation name is not mandated if an equivalent authoritative
boundary fits the existing operation architecture better.

## Recognized versus unknown

The social state must expose a deterministic way to distinguish:

- an actor the person has never encountered;
- an actor with an existing social contact.

The exact API is not mandated.

Possible implementations include concepts such as:

`HasEncountered(actor)`

or:

`GetContact(actor)`.

The first implementation does not require a complex behavior tree.

The causal proof is that a later encounter can query authoritative state and
obtain a different result because an earlier encounter occurred.

Conceptually:

    before first encounter:
      Ester X -> unknown

    after first encounter:
      Ester X -> recognized

    unrelated Ester Y -> unknown

    after second encounter with Ester X:
      Ester X -> recognized
      encounterCount == 2

That distinction is sufficient to establish the first persistent social
continuity.

## Person state preservation

Every existing `PersonState` transition must preserve social state unless that
transition explicitly changes it.

This includes at minimum:

- `MoveTo`;
- `WithSurvivalState`;
- `WithPregnancy`;
- `WithoutPregnancy`;
- `WithMaterial`.

Adding recognition must not create the class of bug where movement,
starvation, pregnancy, material growth, or another unrelated update silently
erases whom the person knows.

Future `PersonState` transition methods inherit the same rule.

## World ownership

Social recognition belongs to authoritative simulation state.

It is not owned by:

- the renderer;
- browser state;
- UI components;
- API response caches;
- an LLM context window;
- session-local presentation state.

Because it is part of `PersonState`, the containing `WorldState` owns it through
the existing authoritative population boundary.

Existing world population replacement semantics remain authoritative.

No parallel social database is introduced by this milestone.

## World copy behavior

`WorldState.Copy()` must preserve person social state exactly.

Copying a world must not:

- drop contacts;
- reset encounter counts;
- change encounter times;
- generate new actor identities.

The copied world represents the same authoritative state.

## Timeline checkpoint behavior

A simulation checkpoint captures the social recognition state that exists at
that checkpoint's simulation time.

Restoring or reading that checkpoint must expose the same contacts and encounter
history stored in the checkpointed world.

Later encounters must not retroactively alter earlier checkpoints.

## Timeline fork behavior

Timeline branching must preserve causal history up to the fork point.

If a person has already encountered Ester X at the checkpoint used to create a
child timeline:

- both parent history and child history recognize Ester X at the fork point.

After the fork, social state may diverge.

For example:

    checkpoint:
      Ester X encounterCount = 1

    parent future:
      no additional encounter
      encounterCount = 1

    child future:
      Ester X encountered again
      encounterCount = 2

The second encounter in the child must not modify the parent timeline.

Likewise, an encounter occurring only after the fork must not appear in the
other branch.

This is required for meaningful alternate history.

## Snapshot persistence

The world snapshot schema will be incremented for the introduction of persisted
person social state.

The expected next schema version is:

`25`

A current-schema person snapshot will persist the person's social contacts.

Each persisted contact must contain enough information to reconstruct:

- actor kind;
- actor identity;
- first encounter time;
- last encounter time;
- encounter count.

For snapshot schema versions before the social-recognition schema:

`PersonSocialState`

defaults to empty.

Historical snapshots must therefore remain loadable without inventing social
history that did not exist in those files.

Current-schema snapshots must reject malformed social state rather than silently
repairing it.

Examples of malformed state include:

- empty actor identity;
- unsupported actor kind;
- encounter count below one;
- most recent encounter earlier than first encounter;
- duplicate social contacts for the same actor.

## Timeline archive persistence

`TimelineArchiveSerializer` already embeds authoritative world state by calling:

`WorldSnapshotSerializer.Serialize(world)`

for the current world and checkpoints.

It restores those worlds through:

`WorldSnapshotSerializer.Deserialize(...)`.

Therefore this milestone does not require a second person-social serialization
model inside `TimelineArchiveSerializer`.

Once world snapshot schema 25 preserves social state:

- current-world social state is preserved in timeline archives;
- checkpoint social state is preserved in timeline archives.

The outer timeline archive schema does not need to change solely because the
embedded world snapshot schema changes.

A future archive-format change may independently require an archive schema
increment.

## Timeline events are not the live cognition store

`TimelineEvent` remains historical evidence.

The current timeline-event model contains concepts such as:

- cause;
- summary;
- affected planet;
- elapsed duration;
- numeric metrics.

This milestone does not reconstruct live recognition state by replaying
timeline events.

A person's recognition state remains part of authoritative current world state.

This avoids requiring global timeline history to act as the runtime cognition
database.

Future richer historical events may carry actor identities where useful.

That is a separate concern.

## Shared-world compatibility

This foundation intentionally supports the shared-world direction without
implementing networking.

An invited Ester may eventually enter another player's world under granted
permissions.

When inhabitants encounter that visiting Ester, their social state must be able
to refer to that Ester independently of:

- which network connection they used;
- which session instance is currently running;
- who owns the world;
- which client rendered the encounter.

The same `EsterId` may therefore be recognized differently in different worlds.

World A may contain:

    Person 1 -> recognizes Ester X

while World B contains:

    Person 2 -> has never encountered Ester X

No global cross-world reputation is introduced.

Each world's social history remains authoritative for itself.

## No networking dependency

This ADR does not require:

- multiplayer transport;
- matchmaking;
- invitations;
- accounts;
- authentication;
- cloud persistence;
- authority handoff;
- synchronization;
- concurrent player conflict resolution.

Those systems may later use `EsterId`.

The social-recognition model must remain testable without them.

## No renderer dependency

Recognition must be simulation truth before it is presented visually.

The renderer may eventually show:

- recognition animations;
- names;
- remembered-player indicators;
- dialogue changes;
- social UI.

Those are presentations of authoritative state.

They must not create the state.

The authority direction remains:

    world event
      -> authoritative social state
      -> API / presentation

not:

    renderer noticed proximity
      -> therefore simulation says they remember

without an authoritative encounter boundary.

## No natural-language dependency

The first social proof requires no LLM.

Natural-language dialogue may eventually consume authoritative social state.

For example, a dialogue system may later be told that:

- this person recognizes Ester X;
- they last encountered Ester X at a particular time;
- other relationship or memory facts exist.

Generated text must not become the source of those facts.

This ADR therefore creates useful grounding for future natural-language
interaction without depending on it.

## Relationship to ADR 0008

ADR 0008 remains authoritative for human environmental exposure, shelter,
structures, and settlement formation.

ADR 0008 also intentionally deferred the final cognition, knowledge, and
technology architecture.

This ADR does not supersede that decision.

It introduces the minimum social continuity required for persistent individual
interaction:

    actor identity
      -> prior contact
      -> recognition

Future knowledge, learning, households, relationships, institutions, culture,
law, politics, religion, and settlement systems may build on this foundation.

They are not defined here.

## First implementation boundary

The first implementation should include only:

1. stable `EsterId`;
2. social actor identity supporting person and Ester actors;
3. immutable `PersonSocialContactState`;
4. immutable `PersonSocialState`;
5. `PersonState` ownership of social state;
6. an authoritative way to record one encounter;
7. recognized-versus-unknown lookup;
8. world snapshot schema 25 persistence;
9. copy, checkpoint, archive, and fork preservation;
10. focused automated tests.

It should not yet include:

- trust;
- friendship;
- hostility;
- romance;
- reputation;
- dialogue;
- beliefs;
- knowledge transfer;
- forgetting;
- rumors;
- institutions;
- jobs;
- law;
- politics;
- religion;
- player manifestation;
- multiplayer networking;
- social UI.

The purpose is to establish a durable causal seam before adding richer social
meaning.

## Acceptance gates

The milestone is accepted only when all of the following are demonstrated by
automated tests.

### A1: Stable Ester identity

- `EsterId` rejects an empty GUID.
- Two different Ester identities remain distinct.
- An Ester actor and person actor remain distinct even if constructed from the
  same underlying GUID value.

### A2: New people have no invented social history

A newly constructed person:

- has an empty social state;
- recognizes no Ester by default;
- recognizes no unrelated person by default.

No random or inferred contacts are created.

### A3: First encounter creates recognition

Given:

- Person A;
- Ester X;
- simulation time `T1`;

before the encounter:

    Person A recognizes Ester X == false

after one authoritative encounter:

    Person A recognizes Ester X == true
    encounterCount == 1
    firstEncounterTime == T1
    lastEncounterTime == T1

### A4: Repeated encounter preserves continuity

Given a prior Person A / Ester X contact at `T1` and a second encounter at
`T2`, where `T2 >= T1`:

    encounterCount == 2
    firstEncounterTime == T1
    lastEncounterTime == T2

The second encounter must update the existing contact rather than append a
duplicate actor record.

### A5: Recognition is actor-specific

After Person A encounters Ester X:

- Person A recognizes Ester X;
- Person A does not recognize Ester Y;
- another Person B does not automatically recognize Ester X.

Recognition does not propagate without a causal social mechanism.

### A6: Existing person transitions preserve social state

After Person A has recognized Ester X, each existing unrelated state transition
must preserve that contact:

- movement;
- survival-state replacement;
- pregnancy addition where valid;
- pregnancy removal where valid;
- material replacement.

No existing person lifecycle behavior may erase recognition.

### A7: World copy preserves recognition

After Person A recognizes Ester X:

`WorldState.Copy()`

must preserve:

- actor identity;
- first encounter time;
- last encounter time;
- encounter count.

### A8: Snapshot round trip preserves recognition

A world snapshot serialize/deserialize round trip must preserve the complete
person social-recognition state.

Existing population value-equality round-trip expectations should remain valid.

### A9: Historical snapshots remain compatible

A valid schema-24 world snapshot containing population but no social state must
deserialize successfully under schema 25 code.

Its restored people receive empty `PersonSocialState`.

No invented prior contacts are created.

### A10: Timeline archive round trip preserves recognition

A timeline archive containing:

- current-world recognition state;
- checkpointed recognition state

must round trip without losing either.

This must be achieved through the existing embedded `WorldSnapshotSerializer`
boundary rather than a duplicate archive-specific person-social schema.

### A11: Checkpoints preserve historical social state

Given:

1. Person A encounters Ester X once;
2. a checkpoint is created;
3. Person A encounters Ester X again;

the checkpoint must retain:

    encounterCount == 1

while the later current world contains:

    encounterCount == 2

Later social changes must not rewrite checkpoint history.

### A12: Forks preserve and then diverge

Given a checkpoint where Person A has encountered Ester X once:

- a child timeline forked from that checkpoint begins with
  `encounterCount == 1`;
- recording another Person A / Ester X encounter in the child changes the child
  to `encounterCount == 2`;
- the parent timeline remains unchanged.

### A13: Unknown versus recognized is causally different

A social recognition query performed before the first encounter returns
unknown.

The same query after the first authoritative encounter returns recognized.

That difference must come only from authoritative world state changed by the
encounter.

This is the minimum proof that:

**the second encounter can be different because the first encounter happened.**

### A14: No presentation authority

No test may require a browser, renderer, camera, UI component, or generated
dialogue to establish recognition.

The simulation and persistence test suites must prove the feature independently
of presentation.

## Implementation sequence

Implement in this order:

1. `EsterId`;
2. social actor identity;
3. contact and person social-state value types;
4. `PersonState` integration and transition preservation;
5. authoritative encounter recording;
6. recognition query;
7. focused simulation tests;
8. world snapshot schema 25;
9. snapshot backward-compatibility tests;
10. checkpoint and fork tests;
11. timeline archive round-trip tests;
12. full simulation, persistence, application, API, and web regression gates.

Do not add dialogue, relationship scoring, networking, or presentation merely to
make the first proof appear more game-like.

The first milestone is intentionally infrastructural.

## Architectural invariants

Future implementation must preserve these rules:

1. Social recognition is authoritative world state.
2. `EsterId` is not session identity.
3. Simulated people and Esters remain distinguishable actor kinds.
4. Recognition belongs to the recognizing person.
5. Recognition does not imply trust, friendship, hostility, belief, or
   reputation.
6. Social state survives unrelated person-state changes.
7. Social state survives world copying and persistence.
8. Timeline checkpoints preserve the social state that existed when they were
   created.
9. Timeline branches share history up to the fork and may diverge afterward.
10. Historical snapshot compatibility must not invent prior social history.
11. Timeline events are historical evidence, not the live cognition store.
12. Presentation and generated language do not create authoritative recognition.
13. Multiplayer infrastructure is not required for the simulation-domain
    identity model.
14. Different worlds may remember the same Ester differently.
15. Richer cognition must grow from explicit authoritative state rather than
    replacing recognition with ungrounded generated behavior.

## Playable manifestation integration

The recognition foundation composes with the manifested-Ester architecture in
ADR 0019.

A qualifying physical encounter follows the authoritative chain:

    manifested Ester and Person A occupy qualifying world proximity
      -> application requests an encounter state transition
      -> simulation validates and records the encounter
      -> Person A recognizes that Ester thereafter
      -> Ester leaves
      -> same Ester later returns
      -> recognition is observed before another encounter is recorded

The renderer does not create recognition because two meshes overlap or because
an animation plays.

Presentation may display proximity or recognition evidence, but the encounter
boundary and resulting social-state change remain simulation/application
concerns.

## Consequences

### Positive

- Individual humans gain their first persistent social continuity.
- The same person can distinguish a prior actor from a stranger.
- Future relationships and memory have a stable identity foundation.
- Ester identity becomes independent from runtime simulation sessions.
- Shared-world participation gains a simulation-level identity seam without
  requiring networking now.
- Recognition naturally follows world snapshots, checkpoints, archives, and
  timeline branching.
- Future natural-language dialogue can be grounded in authoritative social
  state.
- Existing population authority remains intact.

### Costs

- `PersonState` gains another durable state family.
- Every person-state transition must preserve social state.
- World snapshot schema increases.
- Persistence validation gains another compatibility path.
- Large future populations will eventually require social-state memory and
  fidelity policies.
- A richer cognition architecture will eventually need to distinguish simple
  recognition from episodic memory, knowledge, beliefs, and relationships.

These costs are appropriate for the first durable social-simulation seam.

## Working proof

The foundational social proof is:

> A human encountered an Ester, time passed, the world was saved, loaded, or
> branched, and that same human still knew they had met that same Ester before.

That establishes the smallest causal foundation needed for a world in which
social history can matter.
