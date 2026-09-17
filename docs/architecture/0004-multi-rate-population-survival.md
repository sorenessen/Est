# ADR 0004: Multi-Rate Population Survival Integration

## Status

Accepted

## Context

Est now contains individual population, survival needs, planetary food
resources, demographic change, and short-timescale survival behavior.

These systems do not naturally operate at the same useful temporal cadence.

Demographic processes such as baseline mortality, births, and broad migration
can reasonably evaluate over relatively long requested simulation intervals.

Physiological and ecological processes such as energy depletion, starvation,
feeding, and short-range resource seeking require substantially finer
integration to avoid unrealistic large-step behavior.

One possible implementation would globally split every requested
`SimulationSession` advancement into daily steps.

That approach conflicts with the existing timeline semantics.

`SimulationStepRunner` evaluates causal systems for one requested elapsed
duration and returns causal changes whose elapsed duration matches that outer
step. `SimulationTimeline` records those changes as history for that step.

Globally converting a one-year user advance into hundreds of daily outer
simulation steps would therefore create hundreds of timeline event groups even
when the user requested one conceptual advance.

Alternatively, internally running daily behavior but reporting each internal
change as though it represented the entire outer duration would make history
semantically false.

## Decision

Est uses multi-rate simulation where a causal system may integrate its own
short-timescale behavior internally while still returning one causal change for
the full requested outer duration.

The current population implementation separates responsibilities as follows:

`PopulationSystem`

- baseline mortality
- broad demographic migration

`ReproductionSystem`

- partner seeking and mating
- conception and gestation
- materially sourced births

`ForagingSystem`

- energy depletion
- feeding
- starvation health loss and death
- food-resource consumption
- short-range food discovery
- purposeful travel toward food

`ForagingSystem` currently limits its internal integration step to one
simulated day.

The outer `SimulationSession`, `SimulationStepRunner`, and timeline are not
globally substepped merely to satisfy survival-model cadence.

Authoritative movement produced by survival behavior remains simulation state.
Presentation clients may animate or visualize that movement but must not own
the behavioral result.

## Rationale

This preserves the useful semantics of an explicit user-requested simulation
advance while allowing individual causal systems to choose numerically and
behaviorally appropriate internal cadence.

It also prevents short-timescale physiology from forcing unrelated long-scale
systems to run hundreds of unnecessary outer steps.

The design keeps cadence responsibility close to the model that requires it
instead of introducing a global scheduler before Est has demonstrated the need
for one.

The decision follows the existing Est architecture principle:

> Build the smallest implementation that works now without embedding
> assumptions that unnecessarily constrain Est's long-term vision.

## Consequences

Positive:

- Survival behavior remains stable across long outer simulation advances.
- Timeline history continues to correspond to requested simulation advances.
- Population demographics do not need to run at physiological cadence.
- The simulation remains runnable without presentation.
- Future causal systems may use different internal cadences when justified.
- A global scheduler or event engine is not required yet.

Tradeoffs:

- A causal system may contain its own internal integration loop.
- Internal substeps are not individually represented as top-level timeline
  events.
- Model-specific cadence constants require deliberate ownership and testing.
- Interactions between systems operating at different effective cadences may
  eventually require a more explicit orchestration model.

## Current Simplifications

The current one-day maximum survival integration step is a practical proof
value, not a universal biological constant.

Current food, energy, health, movement, and discovery parameters are simplified
model values.

Purposeful food-seeking currently uses latitude/longitude degree-space
approximation rather than terrain-aware or geodesic locomotion.

These simplifications can change without reversing the architectural decision
that short-timescale causal systems may integrate internally.

## Deferred Questions

- Whether future interacting ecological systems require a shared substep
  coordinator.
- Whether internal model transitions need optional diagnostic history without
  becoming top-level timeline events.
- How cadence should be configured when physiology, weather, hunting,
  predation, disease, and other systems interact.
- Whether deterministic fixed-step integration remains appropriate for all
  short-timescale systems.
- When a real scheduler becomes justified by demonstrated cross-system
  requirements.

Do not introduce a generic scheduler solely because multiple cadences are now
possible. Add broader orchestration only when concrete system interaction
requires it.
