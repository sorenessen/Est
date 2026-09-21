# Returning Ester Recognition Proof

Status: Active proof plan

## Purpose

This proof establishes the first end-to-end social continuity in Est.

ADR 0018 already establishes the authoritative simulation foundation for
persistent actor recognition. This proof does not introduce a new architecture
decision. It demonstrates that the accepted foundation can participate in a
real Est execution path beyond isolated domain operations.

The central claim to prove is:

> A person encounters a specific Ester, the Ester leaves, simulation time and a
> persistence boundary pass, and when that same Ester returns the person
> recognizes them before the second encounter is recorded, while an unknown
> Ester remains unrecognized.

The important causal fact is not merely that two encounter records can be
written.

It is:

> The second meeting can begin differently because the first meeting happened.

## What "Recognition" Means In This Proof

Recognition is deliberately minimal.

The person can authoritatively distinguish:

- an actor they have encountered before; from
- an actor they have never encountered.

Recognition does not yet mean that the person remembers the content of the
interaction.

This proof does not establish episodic memories such as:

- what the Ester said;
- what the Ester was wearing;
- what action the Ester performed;
- whether the person liked or feared the Ester;
- what promises were made;
- why the encounter mattered.

Those belong to later cognition, memory, relationship, and communication
slices.

For this proof, the authoritative meaning is only:

> I have encountered this specific actor before.

## Canonical Scenario

Use three stable identities:

- `Person A`: the simulated human;
- `Ester X`: the returning Ester;
- `Ester Y`: an unrelated Ester used as the negative control.

The proof proceeds in this order.

### 1. Establish the initial world

A world exists containing `Person A`.

`Person A` has no prior social contact with either Ester.

The system must report:

    Person A recognizes Ester X == false
    Person A recognizes Ester Y == false

### 2. Record the first encounter

At simulation time `T1`, `Ester X` encounters `Person A` through the
authoritative encounter path.

Afterward:

    Person A recognizes Ester X == true
    Person A recognizes Ester Y == false
    Ester X encounterCount == 1
    Ester X firstEncounterTime == T1
    Ester X lastEncounterTime == T1

### 3. Ester X leaves the interaction

`Ester X` is no longer participating in the encounter.

For this proof, "leaves" is a causal scenario boundary, not yet a manifested
body, locomotion, despawn system, or multiplayer disconnect mechanism.

No social state may be erased merely because the Ester is absent.

### 4. Simulation time passes

Advance the authoritative world to a later simulation time `T2`, where:

    T2 > T1

The person's recognition of `Ester X` must remain intact.

### 5. Cross a persistence boundary

The world must cross a real persistence or historical continuity boundary used
by Est.

The proof should use an existing authoritative boundary such as:

- snapshot save and restore;
- timeline archive round trip;
- checkpoint and later restoration/fork.

The proof must not depend only on retaining the original in-memory object
graph.

After restoration or continuation, `Person A` must still have the same
actor-specific contact with `Ester X`.

### 6. Ester X returns

Present the same stable `EsterId` for `Ester X` again at `T2`.

Before recording any second encounter, query `Person A`'s authoritative social
state.

The result must be:

    Person A recognizes Ester X == true

This query is the critical observation in the proof.

Recognition must exist before encounter number two is written.

Otherwise the proof would establish only that the system can append or update
encounter records twice.

### 7. Query the negative control

At the same point, query `Person A` about `Ester Y`.

The result must remain:

    Person A recognizes Ester Y == false

This demonstrates that recognition is actor-specific rather than a generic
"has met an Ester" flag.

### 8. Record the second encounter

Only after the pre-encounter recognition query succeeds should the second
authoritative encounter with `Ester X` be recorded.

Afterward:

    Person A recognizes Ester X == true
    Ester X encounterCount == 2
    Ester X firstEncounterTime == T1
    Ester X lastEncounterTime == T2

There must still be exactly one contact record for `Ester X`.

## Required Causal Chain

The proof must establish this complete chain:

    stable Ester identity
      -> first authoritative encounter
      -> person-specific recognition
      -> Ester absence
      -> simulation time passes
      -> persistence/history boundary
      -> same Ester identity returns
      -> recognition queried before second encounter
      -> unrelated Ester remains unknown
      -> second encounter updates existing history

Breaking or bypassing any link weakens the proof.

## End-to-End Boundary

The simulation-domain behavior already has focused automated coverage.

This proof should now demonstrate the recognition flow through the real Est
layers required to use it:

    Simulation
      -> Application/session authority
      -> API
      -> minimal observable presentation

The application layer must not bypass simulation authority.

The API must expose observed authoritative recognition rather than inventing
social state.

The presentation layer is evidence only. It is not authoritative.

A developer-facing representation is sufficient. Examples include:

    Person A
    Ester X: recognized
    first encounter: T1
    last encounter: T1
    encounters: 1

Before encounter two, that display or equivalent observable API result must
already show `Ester X` as recognized.

## Acceptance Gates

### R1: Initial unknown state

Before the first encounter:

- `Person A` does not recognize `Ester X`;
- `Person A` does not recognize `Ester Y`.

### R2: First encounter creates recognition

After the first authoritative encounter at `T1`:

- `Person A` recognizes `Ester X`;
- the contact count is `1`;
- first and last encounter times are both `T1`;
- `Ester Y` remains unknown.

### R3: Absence does not erase recognition

Removing `Ester X` from the active interaction context does not erase the
contact.

No special "forget on leave" behavior may occur.

### R4: Time does not erase recognition

After advancing simulation time from `T1` to `T2`, the contact remains
authoritative.

No forgetting model is introduced by this proof.

### R5: Persistence boundary preserves recognition

After the chosen save/restore, archive, or checkpoint boundary:

- the same person still exists;
- the same `Ester X` contact exists;
- its original first-encounter time remains `T1`;
- its encounter count remains `1`.

### R6: Returning Ester is recognized before encounter two

When the same `EsterId` returns at `T2`, but before recording encounter two:

    Person A recognizes Ester X == true

This is the primary proof gate.

### R7: Different Ester remains unknown

At the same point:

    Person A recognizes Ester Y == false

The result must be identity-specific.

### R8: Second encounter updates continuity

After recording the second encounter with `Ester X` at `T2`:

- encounter count becomes `2`;
- first encounter remains `T1`;
- last encounter becomes `T2`;
- no duplicate contact is created.

### R9: Application path preserves authority

The complete scenario can be driven through the application/session boundary
without direct test-only mutation of internal social collections.

The application layer may invoke simulation operations, but it may not invent
recognition independently.

### R10: API can observe the distinction

An API-level consumer can distinguish at minimum:

- unknown actor;
- recognized actor.

The API result must reflect authoritative world state.

### R11: Minimal runtime observation

A human running Est can observe the proof through a deliberately minimal
developer-facing surface or equivalent runtime inspection.

No polished gameplay UI is required.

The observable sequence must make it possible to verify:

    unknown
      -> first encounter
      -> recognized
      -> persistence/time boundary
      -> returning actor recognized before encounter two
      -> encounter count becomes 2

### R12: Existing regression gates remain green

The existing:

- simulation tests;
- persistence tests;
- application tests;
- API tests;
- web tests;
- production web build

must remain green.

## Explicit Non-Goals

This proof does not require:

- character animation;
- manifested player bodies;
- physical proximity detection;
- locomotion;
- collision;
- dialogue;
- natural-language generation;
- episodic memory;
- trust;
- friendship;
- hostility;
- fear;
- romance;
- reputation;
- beliefs;
- rumors;
- communication between inhabitants;
- institutions;
- jobs;
- law;
- politics;
- religion;
- multiplayer networking;
- polished social UI.

Those systems must not be introduced merely to make this proof appear more
game-like.

## What This Proof Unlocks

If this proof succeeds, Est may rely on a stronger statement than
"social-recognition value types work."

It may rely on:

> A stable player identity can enter a real Est world interaction, become part
> of one person's durable social history, disappear, survive time and
> persistence, and be recognized when presented again.

That is the prerequisite for the next proof.

## Physical Gameplay Follow-On

### Implementation status — completed 2026-09-21

The physical gameplay follow-on to this design proof is now complete.

Est has a stable manifested Ester, authoritative geographic player location,
responsive movement, a local Babylon metre-space, animated nearby people keyed
by stable `PersonId`, and world-generated encounter qualification based on
authoritative positions.

The runtime sequence demonstrated:

    manifested Ester
      -> physical presence
      -> spatial interaction with Person A
      -> authoritative encounter generated by gameplay
      -> Ester leaves the encounter radius
      -> Ester returns
      -> Person A is recognized before encounter two is recorded
      -> encounter count becomes 2

The movement response exposes the recognition state that existed before the
qualifying encounter operation, so presentation can display recognition on
return without inventing social state.

This gameplay run occurred while authoritative simulation time remained paused,
so first and last encounter times were both the same simulation instant. It
therefore proves the physical world-generated recognition loop, not a new
runtime execution of this document's separate time-advance and
post-encounter persistence-boundary gates. Those remain distinct claims and
must not be inferred merely from the physical encounter proof.
