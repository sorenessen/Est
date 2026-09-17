# ADR 0007: Seasonal Control and Derived Seasonality Foundation

## Status

Accepted.

Decision date: 2026-09-17.

## Context

Est is intended to support both Earth simulation and configurable living worlds.

Seasonal dynamics are important to Earth and to many plausible living worlds,
but the final product policy is not yet settled. Est may ultimately:

- enforce physically derived seasons for some worlds;
- allow seasons to be disabled for some simulations;
- allow a user, scenario, or experiment to override the effective season;
- restrict manual seasonal control in realism-focused products such as an Earth
  Observatory;
- permit full seasonal "god mode" in sandbox or Living World experiences.

The architecture must preserve these options while the product is still being
defined.

Current authoritative simulation time is expressed independently as elapsed
simulation seconds. The existing lifecycle substrate intentionally does not
assume a Gregorian calendar, Earth orbital year, or fixed seasonal model.

Species systems already contain seasonal-policy vocabulary, but no existing
biological system should become seasonally constrained until a common seasonal
control substrate exists.

## Decision

Est will introduce seasonal capability as an optional, dormant simulation
foundation before any existing ecological or lifecycle system is required to
obey it.

Simulation time and seasonal state are separate concepts.

Advancing simulation time does not, by itself, require seasonal forcing.
Likewise, manually changing effective seasonal state must not silently alter
authoritative simulation time.

The seasonal framework must support three conceptual control modes.

### Disabled

Seasonal forcing is inactive.

This is the initial compatibility behavior. Adding the seasonal framework must
not change existing simulation outcomes merely because the capability exists.

### Derived

Effective seasonal state is produced by an authoritative seasonal provider.

Future providers may derive seasonality from:

- planetary rotation and orbital state;
- axial tilt and orbital geometry;
- latitude and photoperiod;
- calendar or epoch mappings;
- climate and environmental signals;
- configurable Living World seasonal cycles.

Earth-like physical seasonality is therefore a first-class future capability,
not something excluded by the generic simulation architecture.

### Override

Effective seasonal state may be explicitly controlled independently of the
underlying simulation date or naturally derived state.

For example, a future user or scenario may be able to make the effective
season winter while the simulation calendar still reports July.

Such an override must be explicit and observable. It must not rewrite
simulation time or pretend that naturally derived planetary conditions changed.

Whether a particular product exposes, restricts, or forbids manual seasonal
override is a later product-policy decision.

## Architectural rules

Seasonal control is a source of authoritative simulation context, not a direct
command to individual biological systems.

Consumers such as vegetation, hydrology, climate, wolves, birds, grazers,
invertebrates, migration, dormancy, hibernation, agriculture, or other future
systems decide independently whether and how to respond to seasonal context.

The framework must keep the following concerns separable:

1. authoritative simulation time;
2. planetary or calendar-derived seasonal signals;
3. manual seasonal override;
4. effective seasonal context exposed to simulation systems;
5. species-specific and system-specific responses to that context.

A manual override must be distinguishable from naturally derived seasonality
in persistence, telemetry, API state, debugging, and future replay or scenario
inspection.

The seasonal abstraction must not require Earth-specific calendar concepts in
the simulation core.

At the same time, the abstraction must be rich enough for Earth-specific
providers to represent physically meaningful seasonal dynamics rather than
reducing Earth to an arbitrary repeating timer.

## Initial implementation boundary

The first seasonal milestone will build only the common control and state
foundation.

It must prove that:

- disabled mode preserves current behavior;
- an effective seasonal state can exist without changing simulation time;
- manual override can replace the effective seasonal state explicitly;
- derived mode has a stable extension point for future astronomy, calendar,
  climate, or configurable-cycle providers;
- save/load and API boundaries can distinguish disabled, derived, and
  overridden state once persistence is introduced.

The first milestone does not require:

- an orbital mechanics model;
- Gregorian calendar support;
- Earth astronomy;
- climate-driven season derivation;
- wolf breeding restrictions;
- bird migration;
- hibernation or dormancy;
- seasonal vegetation changes;
- a final decision on whether players may use seasonal god mode.

Those are consumers or policy decisions that build on the common foundation.

## Product-policy deferral

This ADR deliberately does not decide whether seasonal override remains
available in the final product.

The implementation preserves the capability so Est can experiment with:

- unrestricted seasonal sandbox control;
- realism-locked Earth simulations;
- scenario-specific permissions;
- configurable Living Worlds;
- fully derived seasonality.

A later product decision may constrain which control modes are exposed without
requiring the simulation architecture to be redesigned.

## Consequences

Est gains a stable seasonal extension point without changing current world
behavior.

Earth seasonal dynamics remain viable as a physically derived implementation.

Living Worlds remain free to use Earth-like, custom, disabled, or manually
controlled seasonality.

Species lifecycle work no longer needs to invent its own annual clock or season
representation.

The immediate next implementation task is the dormant seasonal-control
foundation, not seasonal enforcement in any organism system.
