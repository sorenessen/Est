# ADR 0019: Playable Ester Manifestation and Local Embodiment

## Status

Accepted for the current playable Living Worlds foundation.

## Date

2026-09-20

## Context

Est already separates authoritative simulation state from presentation.

ADR 0003 allows different representations at different scales.

ADR 0006 establishes that simulation spatial resolution and render spatial
resolution are independent and that Babylon.js is the current production
renderer.

ADR 0018 gives simulated people durable actor-specific recognition and gives an
Ester a stable simulation-domain identity independent of one runtime session.

The next product requirement is different from merely observing the world.

An Ester must be able to enter the world, move through it at human scale,
approach a stable simulated person, leave, return later, and eventually
participate in an authoritative encounter that the person can remember.

That requires a clear boundary between:

- persistent Ester identity;
- authoritative world position;
- responsive client movement;
- close-range local rendering;
- presentation-only surface detail;
- authoritative simulated people;
- authoritative social encounters.

Without that boundary, ground-level play could accidentally create a second
simulation inside the renderer.

## Decision

### Stable Ester identity

A manifested player is identified by a stable `EsterId`.

`EsterId` is not:

- a browser tab identity;
- a render-mesh identity;
- a session ID;
- a camera identity;
- a network connection identity.

Presentation may persist or recover the player's stable identity, but simulation
semantics use the domain identity defined by ADR 0018.

### Authoritative manifestation

Physical presence in a world is represented by authoritative manifested-Ester
state.

At minimum that state identifies:

- the Ester;
- the planet;
- geographic latitude;
- geographic longitude.

The simulation/application boundary owns those facts.

When authoritative surface topology exists for the manifested planet, API
presentation contracts may additionally expose the `SurfaceCellId` containing
the Ester's current geographic position.

That surface-cell identity is derived from the authoritative latitude/longitude
through the planet-surface-grid abstraction. It is not additional durable
manifested-Ester state and it is not required for worlds that do not have
authoritative surface topology.

A world without terrain therefore remains a valid manifestation target and may
report no surface-cell identity.

Presentation consumers must not reproduce the current latitude/longitude grid's
cell-location algorithm or decode opaque surface-cell identity themselves.

The Babylon scene does not become authoritative merely because it displays the
Ester.

### Movement authority

Player locomotion is an intervention into authoritative world position.

Movement must therefore pass through an explicit application/simulation
operation.

The current proof treats ordinary player movement as a zero-simulation-time
intervention. Walking changes spatial state without silently advancing
`WorldState.CurrentTime`.

Future locomotion rules may add collision, access, capability, stamina,
vehicles, or other constraints. Those rules must remain explicit rather than
being inferred from the current presentation mesh.

### Client prediction and smoothing

Responsive local movement may use client-side prediction, interpolation, or
smoothing.

Those techniques are presentation behavior.

They may reduce perceived latency but they do not transfer spatial authority to
the client.

The client must reconcile with authoritative manifested state.

### Local metre-space rendering

The manifested Ester is authoritatively located in geographic coordinates.

Close-range Babylon rendering may use a local metre-space centered on or near
the Ester.

Nearby authoritative geographic positions are transformed into that local
frame.

The local frame is a render transform, not an alternate simulation coordinate
system.

A useful mental model is:

    authoritative planet + latitude/longitude
      -> geographic-to-local transform
      -> local Babylon metres

The local representation may shift or re-anchor around the Ester as they move.

That translation must not rewrite authoritative world coordinates.

### Local terrain

The authoritative surface grid remains the source of simulation terrain state.

Close-range presentation may continuously sample that authoritative terrain and
generate denser local geometry than the simulation grid contains.

Presentation may additionally add deterministic sub-grid detail for visual
quality.

Examples include:

- small-scale relief;
- albedo variation;
- bump detail;
- stones;
- grass placement;
- wind animation.

Such detail is not authoritative terrain, ecology, collision, habitat, or
material inventory unless a future simulation model explicitly promotes it into
world state.

### Geographically stable presentation detail

Presentation-only procedural detail should be derived from stable geographic
and planetary inputs rather than camera or patch-local randomness when visual
continuity requires it.

This allows terrain detail and vegetation presentation to remain spatially
coherent while local render patches re-anchor or stream.

Streaming policy, fade distances, mesh density, texture density, and similar
values remain implementation details rather than simulation constants.

### Simulated people

A nearby human presentation represents an authoritative simulated person.

The presentation entity must therefore remain tied to stable `PersonId`.

Changing a person's visual asset, LOD, animation, rig, or temporary placeholder
must not create a new simulated person.

The current local human presentation uses an animated avatar keyed by stable
`PersonId`.

Changing or replacing that avatar still must not change person identity,
location authority, history, needs, relationships, or social recognition.

### Ester presentation

The current Ester capsule is likewise temporary presentation scaffolding.

Its shape, material, cosmetic skin, or other visual decoration does not belong
to authoritative simulation merely because it is visible during play.

Durable avatar/customization state, if later required, needs its own explicit
ownership decision.

### Physical encounters

Physical proximity alone does not permit the renderer to mutate social state.

The current gameplay encounter policy uses authoritative Ester and person
geographic positions on the same planet. Entering a 2.5 metre encounter radius
from outside that radius qualifies an encounter. Remaining inside does not
repeatedly record encounters; leaving and entering again qualifies a later
encounter.

The resulting social change passes through the application/simulation authority
path established by ADR 0018.

The intended causal flow is:

    authoritative Ester position
      + authoritative Person A position
      -> encounter qualification
      -> authoritative encounter operation
      -> Person A social state changes
      -> API exposes recognition
      -> presentation displays the result

Renderer mesh overlap is not authoritative encounter state.

### Observatory separation

This ADR applies to manifested participation in Living Worlds.

It does not constrain the Earth Observatory's privileged observer camera.

The Observatory may use viewpoints that an embodied entity could not physically
reach. Camera freedom and entity locomotion remain separate concepts as already
established by ADR 0003.


## Completed causal proof

The runtime proof established:

    Ester approaches Person A
      -> Person A is the same stable simulated individual represented by PersonId
      -> authoritative world positions qualify encounter one
      -> simulation records Person A / Ester encounter
      -> Ester leaves the encounter radius
      -> Ester returns
      -> Person A is recognized before encounter two is recorded
      -> existing encounter history increments from one to two

The presentation reports this result but does not determine it. Visual asset
selection remains independent of simulation identity and social authority.

## Non-goals

This decision does not yet establish:

- final character rigs or art direction;
- final Ester avatar customization;
- collision architecture;
- navigation meshes;
- physics-engine authority;
- animation authority;
- combat;
- dialogue;
- generated language;
- episodic memory;
- trust or relationship scoring;
- reputation;
- multiplayer networking;
- local simulation sub-worlds;
- walking-scale authoritative terrain everywhere;
- final vegetation LOD policy.

Those capabilities may be added when their causal requirements are known.

## Consequences

### Positive

- Ground-level play remains connected to the same authoritative world as the
  globe.
- Player movement does not create a renderer-owned location model.
- Close-range visual density can increase without globally increasing
  simulation-grid resolution.
- Character presentation can evolve independently of stable person and Ester
  identity.
- Physical encounters route through authoritative simulation social state.
- Procedural local detail can improve visual quality without inventing
  simulation truth.

### Tradeoffs

- Client prediction requires reconciliation with server authority.
- Local render re-anchoring requires careful continuity handling.
- Presentation-only detail must remain clearly separated from future
  walking-scale simulation requirements.
- The current 2.5 metre entry policy is intentionally simple and may require
  refinement when richer interaction requirements are known.
- Human locomotion and activity presentation must continue to preserve stable
  person identity and simulation authority.

## Relationships

- ADR 0003 remains authoritative for multi-scale presentation.
- ADR 0005 remains authoritative for simulation terrain/surface ownership.
- ADR 0006 remains authoritative for planetary rendering and simulation/render
  spatial separation.
- ADR 0018 remains authoritative for persistent actor recognition and social
  encounter state.
- `docs/design/returning-ester-recognition-proof.md` defines the returning-Ester
  causal recognition proof.
- `docs/design/est-experience-and-game-design.md` defines the broader embodiment
  experience direction.
