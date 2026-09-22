# ADR 0020: Local Actor Presentation Across Simulation Resolutions

## Status

Accepted for the current Living Worlds local-actor presentation foundation.

## Date

2026-09-21

## Context

Est presents one authoritative simulated world at multiple spatial resolutions.

ADR 0003 establishes that presentation may use different representations of the
same authoritative world state at different scales.

ADR 0006 separates simulation authority from rendering.

ADR 0019 establishes playable Ester manifestation, local metre-space rendering,
and nearby human presentation tied to stable simulated person identity.

The next Living Worlds requirement is broader.

A manifested Ester must eventually share the local world with:

- simulated people across sex and life stage;
- individually simulated animals such as wolves;
- very large ecological populations such as grazers that are intentionally
  represented by coarse authoritative cohorts rather than millions of
  independently simulated individuals.

Those categories do not have the same authoritative resolution.

A simulated person has stable `PersonId`.

A simulated wolf has stable `AnimalId`.

A grazer cohort has stable `GrazerCohortId`, member count, material inventory,
and a coarse geographic location. The cohort model intentionally compresses
planet-wide grazer abundance into a bounded number of aggregate entities.

Treating all three categories identically would create false simulation detail.

In particular, treating a grazer cohort coordinate as the literal walking-scale
location of every member would misrepresent coarse ecological state as precise
individual geography.

Conversely, refusing to render grazers because they are cohort-backed would make
the local world contradict authoritative ecological abundance and would prevent
future direct gameplay such as hunting.

Est therefore needs an explicit boundary between authoritative simulation
resolution and local physical presentation.

## Decision

### Local actor projection is an Est-owned boundary

Living Worlds will use an Est-owned local actor projection layer between
authoritative world state and renderer-specific presentation.

The projection layer determines which authoritative entities or aggregate
populations have local physical representatives near a manifested Ester.

It does not become a second simulation.

Babylon meshes, animation rigs, LOD state, presentation instances, and visual
spawn slots do not independently define simulation truth.

The causal direction remains:

    authoritative world state
      -> local actor projection
      -> renderer presentation

Gameplay interactions that change the world must return through an explicit
authoritative operation.

### Authoritative individual actors

When the simulation already owns individual identity, local presentation must
preserve it.

Current examples are:

- human `PersonId`;
- wolf `AnimalId`;
- manifested `EsterId`.

Changing mesh, rig, animation, LOD, body presentation, or other visual
representation does not create a new authoritative individual.

The same simulated individual must remain the same actor across presentation
changes.

### Human life-stage presentation

Human age is authoritative through birth time and current simulation time.

Visual life stage is presentation policy derived from authoritative lifecycle
state. It does not require inventing a second simulated age field merely for
rendering.

A person's presentation may therefore change as the person grows while the
underlying `PersonId` remains unchanged.

Sex, age, material state, health, clothing, animation, and other future visual
inputs may affect presentation without redefining person identity.

Exact visual stage boundaries and asset choices remain presentation policy
unless the simulation later requires explicit lifecycle categories for causal
behavior.

### Individually simulated animals

Animals represented by authoritative `AnimalState`, currently wolves, may map
directly to local presentation keyed by `AnimalId`.

Their authoritative geographic position, lifecycle state, health, activity,
sex, and other simulation state remain independent from the renderer.

Animation and visual movement smoothing may interpret that state but may not
replace it.

### Manifested Ester surface locality is derived

A manifested Ester remains authoritatively located by planet and geographic
latitude/longitude as established by ADR 0019.

When that planet has authoritative surface topology, the API may project the
opaque `SurfaceCellId` containing the Ester's current position.

This is derived spatial context for consumers such as local ecological
presentation. It is not additional durable Ester state.

If the planet has no authoritative surface topology, the projected
`SurfaceCellId` is absent.

The client must consume this projection rather than reproduce
`IPlanetSurfaceGrid.LocateCell`, infer the current grid's row/column structure,
or otherwise make the current latitude/longitude tessellation part of the web
contract.

### Aggregate grazer authority

Grazer cohorts remain authoritative aggregate ecological state.

A cohort owns facts such as:

- `GrazerCohortId`;
- member count;
- material inventory;
- current coarse geographic position;
- the authoritative surface cell containing that position.

The cohort coordinate and surface cell are macro simulation state.

They must not be interpreted as proof that every member of the cohort occupies
one exact walking-scale point or one exact local herd.

The surface-cell identity exposed by the grazer API is an authoritative
geographic anchor, not a declaration of precise individual occupancy.

### Local grazer realization

Living Worlds may deterministically refine authoritative grazer abundance into a
bounded number of local physical representatives.

Those representatives are presentation entities backed by real authoritative
cohort abundance.

They are not decorative wildlife unrelated to simulation state.

A valid local realization must preserve these properties:

- it is derived from one or more authoritative grazer cohorts;
- it cannot imply more authoritative animals than the backing state supports;
- it respects authoritative habitat information where that information is
  available;
- repeated projection from unchanged authoritative inputs is deterministic
  enough to avoid arbitrary visual regeneration;
- it remains bounded for runtime and rendering performance;
- it does not manufacture `AnimalId` values for cohort members that are not
  individually simulated.

The exact weighting, distribution, streaming radius, density conversion,
representative count, and placement algorithm are implementation policy rather
than simulation constants.

They may evolve as local-play requirements become better understood.

### Habitat-aware refinement

The current grazer model derives carrying support from authoritative vegetation
and evolves cohorts against coarse ecological surface state.

Local grazer realization should therefore use available authoritative habitat
state rather than treating a cohort center as a conventional game spawn point.

Local refinement may use vegetation support and deterministic spatial policy,
but it must remain consistent with the cohort's authoritative current macro
locality.

The current simulation treats cohort latitude/longitude as the cohort's current
macro position. During grazer evolution that position is located into the
surface grid and movement evaluates the current cell and neighboring cells
before selecting a local habitat target.

A planet-wide nearest-cohort partition is therefore rejected for the current
model. A distant habitat cell must not become presentation territory for a
cohort merely because that cohort happens to be the closest cohort on the
planet.

The cohort's current surface cell is still only macro locality. It does not
assert uniform member occupancy throughout the cell or provide walking-scale
individual positions.

This ADR still does not freeze one bounded local influence-region, density,
representative-count, or placement algorithm.

Any adopted algorithm must preserve the distinction between:

- authoritative aggregate abundance;
- authoritative macro geography;
- deterministic local presentation refinement.

### Presentation identity is not simulation identity

A locally realized grazer may need a stable presentation key so that rendering
does not visibly reshuffle every frame or camera movement.

Such a key may be derived from stable authoritative inputs such as cohort
identity, surface region, and deterministic slot information.

A presentation key is not an `AnimalId`.

The exact key encoding is an implementation detail.

### Gameplay interaction with aggregate representatives

Future gameplay may allow a manifested player or simulated actor to interact
with a locally realized grazer.

A gameplay interaction cannot become authoritative merely by mutating or
deleting the representative mesh.

For example, a successful hunt must eventually cause an authoritative operation
against the backing grazer state, including appropriate population and material
consequences.

The local representative identifies what the player interacted with.

The simulation determines the resulting authoritative change.

### Optional promotion to individual authority

Some future gameplay may require one previously aggregate animal to retain
individual history.

Examples may include:

- tracking a wounded animal over time;
- taming;
- tagging;
- capture;
- persistent injury;
- another interaction requiring durable individual identity.

If such requirements arise, Est may introduce an explicit promotion or
materialization operation that creates individually authoritative state from an
aggregate population.

That mechanism is not required merely to render or hunt ordinary grazers.

Est should not create millions of individual grazer entities solely to support
local presentation.

### Population and material conservation

Local realization does not itself change authoritative population count or
material inventory.

Creation, removal, hiding, streaming, LOD replacement, or destruction of a
presentation entity has no simulation consequence unless an explicit
authoritative operation is performed.

Any future operation that converts aggregate animals into individually
authoritative state must preserve population and material accounting across the
boundary.

### Renderer independence

Local actor projection is not Babylon-specific.

Babylon consumes projected local actors and provides visual representation.

It must remain possible to test projection policy without creating meshes or
loading renderer assets.

This keeps ecological refinement, actor identity, and simulation authority out
of renderer orchestration code.

### Asset independence

Third-party meshes, rigs, animations, and other visual assets may be used as
production foundations.

They do not own Est actor identity or simulation semantics.

Est must remain capable of replacing an external asset family without changing:

- `PersonId`;
- `AnimalId`;
- `EsterId`;
- `GrazerCohortId`;
- authoritative locations;
- lifecycle state;
- social state;
- ecological population;
- material accounting;
- gameplay history.

External art may provide leverage, but no external asset pack should become an
irreplaceable definition of an Est simulation entity.

### Authoritative movement observation

Individual local actors may derive presentation-facing direction from successive
authoritative geographic positions.

This derived heading is presentation state only.

It must preserve these properties:

- actor identity remains the authoritative `PersonId` or `AnimalId`;
- geographic positions come from successive authoritative world snapshots;
- moving Ester, moving the camera, or changing local presentation origin must
  not be interpreted as actor motion;
- no heading is invented before authoritative displacement is observed;
- when an authoritative actor becomes stationary, presentation may preserve the
  last valid facing direction;
- visual smoothing and animation may interpolate between authoritative
  observations, but may not manufacture simulation movement.

Repeated projection of the same authoritative world snapshot must preserve the
same movement observation. Presentation refresh caused by Ester movement,
camera movement, terrain refresh, or another renderer concern must not turn one
authoritative displacement into a later false stationary observation.

Authoritative animal activity and observed locomotion are separate presentation
inputs. For example, a wolf may remain authoritatively `Hunting` whether or not
the latest authoritative snapshot contains geographic displacement. Renderer
animation must not infer or replace simulation activity.

The current local wolf presentation uses bounded interpolation between the
previous rendered position and a newly observed authoritative target. The
presentation may animate gait only while traversing those two known endpoints.
It must land exactly on the authoritative target and cease locomotion
presentation when that interpolation completes.

The current wolf simulation does not store authoritative heading or velocity.
Local wolf facing is therefore derived from observed displacement rather than
adding renderer-owned direction to simulation state.

### Simulation time during embodied play

Current Play mode does not install the Observatory automatic simulation
heartbeat.

That is an intentional unresolved product and simulation-control question, not
permission for the presentation layer to advance time implicitly.

How simulation time advances while an Ester is embodied will be decided
separately.

Local actor projection must work from the authoritative world snapshot it is
given regardless of the eventual time-control policy.

## Implementation evidence

The local-actor proof established that:

- authoritative individual humans and wolves can be projected into local
  metre-space while preserving stable `PersonId` and `AnimalId`;
- aggregate grazer cohorts can produce deterministic, bounded presentation
  representatives without inventing individual simulation identity;
- local animal motion observation can derive presentation displacement and
  heading from successive authoritative geographic snapshots;
- repeated projection of one authoritative timestamp preserves the same motion
  observation rather than allowing renderer refresh frequency to redefine
  locomotion;
- wolf activity remains distinct from observed locomotion, while bounded
  interpolation and temporary gait remain presentation state;
- grazer representatives expose only presentation locomotion when authoritative
  cohort state does not provide per-representative activity, heading, velocity,
  or path;
- explicit authoritative fauna stepping demonstrated that camera or local-view
  movement alone does not create actor locomotion, while authoritative
  displacement can drive a bounded transition to the projected target;
- cohort surface-cell vegetation may provide macro habitat context for local
  grazer presentation without asserting sub-cell ecological distribution.

The final abundance-to-representative density policy and habitat-aware local
placement algorithm remain undecided.

## Consequences

The renderer-independent local actor projection boundary now exists for
authoritative individual actors.

Humans and wolves can use direct authoritative individual identity.

New actor categories should continue to enter local presentation through that
boundary rather than accumulating independent mesh-specific authority in
`main.ts`.

Grazers still require deterministic aggregate-to-local refinement. The rejected
planet-wide nearest-cohort experiment narrows that design space without
selecting the final bounded local-realization algorithm.

Projection policy remains unit-testable without Babylon.

Rendering assets can then be attached downstream to projected actors without
changing simulation authority.

Future hunting and other direct ecological interactions will have a defined
causal path back to authoritative world state rather than operating only on
visual objects.

## Non-decisions

This ADR does not yet decide:

- the final grazer local-density formula;
- the exact number of visible representative grazers;
- the exact deterministic hash or placement algorithm;
- the final wolf or grazer asset library;
- final human age-stage thresholds or visual assets;
- combat or hunting mechanics;
- carcass simulation;
- persistent wounded-animal representation;
- the promotion schema for aggregate animals;
- Play-mode simulation-time cadence;
- final LOD distances;
- local actor networking or multiplayer behavior.

Those decisions should be added only when focused implementation work provides
evidence for them.
