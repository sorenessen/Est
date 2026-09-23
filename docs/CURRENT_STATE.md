# Est Current State

Updated: 2026-09-21

## Purpose

This is the canonical tracked summary of what Est currently implements.

Use this document to answer questions such as:

- What exists now?
- Which systems currently own authoritative state?
- Which causal behaviors currently run?
- Which renderer and presentation paths are current?
- Which foundations exist without yet constituting complete product behavior?

This document is not a development history, ADR, roadmap, backlog, design
specification, or session handoff.

The live repository, tests, and verified runtime behavior remain final authority
if documentation and implementation disagree.

## Documentation Authority

Use Est documentation according to these responsibilities:

- `docs/CURRENT_STATE.md`
  - canonical summary of present implementation status;

- `docs/architecture/*`
  - accepted architectural decisions, constraints, and rationale;
  - dated implementation checkpoints and historical "next step" language do not
    supersede this document for present implementation status;

- `BACKLOG.md`
  - incomplete, deferred, and milestone work;
  - completion checkboxes are useful evidence but are not the sole authority for
    determining current capability;

- `docs/design/*`
  - product intent, experience design, and proof definitions;
  - intended behavior is not proof of implementation;

- `docs/DEVELOPMENT.md`
  - development, launch, testing, recovery, and operational procedures;

- `docs/SURFACE_EVALUATION.md` and `docs/archive/*`
  - historical engineering evaluation where identified as such;

- root `HANDOFF.md`
  - local-only continuation state for the active branch and task;
  - never tracked or committed.

## Status Vocabulary

Do not collapse different levels of implementation into one word.

### Foundation

The architecture, state, contracts, or extension points exist, but meaningful
downstream behavior may not yet consume them broadly.

### Active causal behavior

The authoritative simulation currently executes the behavior and changes
authoritative world state.

### Presentation capability

The renderer currently represents authoritative state or a deterministic
presentation refinement of it. Presentation capability does not imply that the
renderer owns equivalent simulation state.

A subsystem may have an implemented foundation without having complete active
causal behavior or complete product behavior.

## Maintenance Rules

Keep this document small and current.

When implementation changes materially:

1. replace stale present-state wording rather than appending a chronology;
2. describe only behavior supported by current repository or verified runtime
   evidence;
3. distinguish foundation from active causal behavior and presentation;
4. link to ADRs for rationale instead of duplicating their reasoning;
5. keep planned work in `BACKLOG.md`;
6. keep active branch and task state in root `HANDOFF.md`;
7. do not add commit-by-commit development history;
8. do not add milestone diaries, dated completion sections, or "next step"
   sections;
9. prefer replacing or removing text over growing the document indefinitely;
10. if a subsystem requires detailed explanation, summarize its present state
    here and keep the detail in the appropriate ADR, design, or development
    document.

A completed capability must not remain described here as missing merely because
an older ADR, backlog item, design document, or handoff once described it as
future work.

Likewise, the existence of infrastructure must not be described as complete
product behavior when downstream systems do not yet use it meaningfully.

## Current Platform

Est is simulation-first.

Authoritative world state and causal simulation are independent of rendering.
Presentation consumes authoritative state but does not create simulation truth.

The current browser renderer is Babylon.js.

Cesium is retired from the production renderer path. Retained Cesium material
is historical or evaluation evidence, not an alternate current runtime.

Simulation spatial resolution and rendering spatial resolution are independent.

## Simulation Time Control

Session time is authoritative application/simulation state.

The current Babylon client exposes Pause/Resume and selectable 1x, 2x, 4x, 10x,
100x, and 1000x simulation rates. While a session is running, elapsed real time
is submitted through the API as authoritative simulation ticks. Presentation
refresh and animation do not independently advance simulation truth.

## Current Living-World Simulation

Active authoritative state currently includes:

- world and planet identity;
- simulation time;
- terrain and shared surface topology;
- hydrology and standing-water state;
- vegetation;
- biogeochemistry and nutrient cycling;
- individual human population state;
- aggregate invertebrate biomass;
- bird flocks;
- grazer cohorts;
- individually simulated wolves;
- organism material state across current biological representations.

Current biological material flows are causal. Configured feeding, predation,
mortality, growth, birth, and aggregate recruitment operate through
authoritative simulation state rather than renderer behavior.

## Human Pregnancy and Reproduction

Human pregnancy and reproduction are active causal behavior in the current
individual population model.

Current behavior includes:

- condition and proximity based mating opportunities;
- conception;
- persistent pregnancy state;
- gestation;
- prevention of reconception while pregnant;
- birth after gestation;
- causal offspring material requirements;
- no child creation when required material is unavailable;
- pregnancy persistence through current snapshot serialization.

Pregnancy therefore must not be described as generally missing or broken unless
new repository or runtime evidence demonstrates a specific regression.

The broader complete human lifecycle is a larger concern than pregnancy alone
and must not be conflated with whether pregnancy currently works.

## Seasonal Capability

Est has an implemented seasonal-control foundation.

That foundation includes:

- authoritative planetary seasonal state;
- disabled, derived, and override control semantics;
- an authoritative seasonal-provider abstraction;
- derived seasonal-system support;
- circular-orbit seasonal parameters and provider;
- seasonal-state replacement through the simulation operation boundary;
- persistence and API representation of seasonal state and configuration;
- tests covering seasonal control and derived seasonal behavior.

When a simulation explicitly configures circular-orbit seasonality together
with regional thermal authority, derived or overridden physical seasonal
context supplies subsolar latitude to the regional thermal system before
thermal evaluation.

The ordinary public API session-creation path does not currently expose
seasonal-model or regional-thermal-model configuration, so that physical
seasonal chain is not automatically active in ordinary API-created worlds.

This does not mean that Est currently has broadly realized seasonal ecology or
gameplay.

Species-specific seasonal responses such as breeding windows, migration,
torpor, dormancy, hibernation, and vegetation seasonality remain separate
future consumer behavior.

The architecture deliberately avoids hardcoding one Earth four-season model.
It preserves room for physically derived Earth-like seasonality as well as
other world-specific seasonal structures and control policies.

## Embodied Living World

A manifested Ester has stable identity and authoritative planetary geographic
state.

The Babylon embodied view uses a local metre-space presentation centered on the
manifested Ester while preserving authoritative geographic ownership outside
the renderer.

Current local presentation includes:

- animated humans keyed by stable `PersonId`;
- human locomotion and heading derived from successive authoritative person
  snapshots;
- authored human idle/walk animation with gait phase advanced in presentation
  time;
- velocity-preserving human presentation interpolation and short-term
  continuation between authoritative snapshots without creating simulation
  truth;
- individually authoritative animated wolves keyed by stable `AnimalId`;
- deterministic cohort-backed grazer representatives without invented
  `AnimalId`;
- fauna movement observation from successive authoritative snapshots;
- bounded fauna presentation interpolation without renderer-authored simulation
  movement.

Wolf and grazer presentation foundations are implemented, but their close-range
animation quality has not yet completed the same deliberate browser-runtime
acceptance pass used for human locomotion. Their authority contracts are
current; gait, foot contact, transition quality, root travel, and reconciliation
remain active visual-verification work.

Human partner-seeking movement is authoritative simulation behavior expressed
as physical surface distance, currently calibrated to 1.31 metres per second.
Human rendering consumes that movement and orientation while keeping geographic
position ownership in simulation state.

The returning-Ester recognition proof is active authoritative behavior. A
manifested Ester can approach a simulated person, create an authoritative
encounter, leave, return, and expose whether that same person already recognized
the same Ester before the repeated encounter is recorded.

The Ester capsule remains presentation scaffolding, not authoritative identity.

## Planetary Presentation

The production planetary renderer is Babylon.js.

Current presentation includes authoritative terrain sampling, continuous
standing-water presentation, planetary lighting, and living vegetation
presentation.

The embodied view adds local terrain and deterministic presentation detail
without creating a second terrain or ecological authority.

Camera movement, renderer animation, local projection, procedural detail, and
asset choice do not alter authoritative simulation state.
