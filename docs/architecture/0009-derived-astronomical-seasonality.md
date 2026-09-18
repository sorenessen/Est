# ADR 0009: Derived Astronomical Seasonality Foundation

## Status

Accepted.

Decision date: 2026-09-18.

## Context

ADR 0007 established dormant seasonal control with Disabled, Derived, and
Override modes.

The simulation now has authoritative seasonal state and an `ISeasonalProvider`
extension point, but it does not yet have an implementation that derives
seasonality from simulation time or planetary astronomy.

Est also does not yet contain authoritative orbital state such as an orbital
ephemeris, eccentricity, longitude of periapsis, or a general celestial
mechanics model. `PlanetState` currently contains physical planet identity,
mass, radius, and environment, but not orbital elements.

The first Derived implementation therefore must not silently assume Earth
calendar dates or pretend that a full orbital mechanics model already exists.

## Decision

Est will introduce a configurable circular-orbit seasonal provider as the
first authoritative Derived seasonality implementation.

The model is deliberately explicit about its approximation.

For each configured planet it will use:

- orbital period in simulation seconds;
- axial tilt in degrees;
- normalized orbital cycle fraction at simulation time zero.

The provider will derive its result solely from authoritative simulation state
and immutable model configuration.

## Derived context

`SeasonalContext` will continue to expose a provider-defined phase identity and
normalized cycle fraction.

It will additionally permit an optional physical signal:

- subsolar latitude in degrees.

The circular-orbit provider will populate this value from orbital phase and
axial tilt.

Manual override contexts are not required to provide a subsolar latitude.
An override remains an explicit effective seasonal context and does not
masquerade as naturally derived astronomy.

## Orbital phase convention

Cycle fraction is normalized to `[0, 1)`.

For the circular-orbit provider:

- `0.00` is the equatorial crossing with the subsolar point moving northward;
- `0.25` is the northernmost subsolar latitude;
- `0.50` is the equatorial crossing with the subsolar point moving southward;
- `0.75` is the southernmost subsolar latitude.

This convention is astronomical rather than Gregorian. It does not assign
month names or Earth calendar dates.

The provider may expose stable physical phase identifiers describing these
orbital quarters. Consumers must not interpret those identifiers as commands.

## Causal execution

Derived seasonality will participate in the existing deterministic simulation
step pipeline.

`SimulationStepRunner` evaluates causal systems before it applies authoritative
time advancement. Therefore the seasonal causal system will derive against the
world projected to the end of the current step while leaving the actual time
advance to `SimulationStepRunner`.

This preserves one authority for simulation time while ensuring that the
derived seasonal state recorded after a step corresponds to the resulting
simulation time.

A zero-duration step derives against the current simulation time.

## Control-mode interaction

A configured Derived provider updates the authoritative derived context.

If the planet is in Derived mode, the newly derived context is effective.

If the planet is in Override mode, the newly derived context is preserved and
updated underneath the override while the explicit override remains effective.

Disabled seasonal state is not implicitly activated merely because a provider
implementation exists.

Provider configuration is what opts a planet into derived astronomical
seasonality.

## Persistence

Circular-orbit seasonal model configuration is part of
`SimulationDefinition` and must round-trip through timeline archives.

The additional physical seasonal signal must round-trip through world
snapshots.

Schema versions must preserve backward compatibility with archives and world
snapshots created before this capability existed.

## Initial implementation boundary

This milestone must prove that:

- no configuration preserves existing behavior;
- configured Derived state is deterministic from simulation time and model
  parameters;
- advancing time updates derived cycle fraction and subsolar latitude;
- zero-duration evaluation reflects current time without advancing it;
- Override remains effective while its underlying derived context continues
  to update;
- save/load preserves model configuration and resulting seasonal state;
- API state can expose the additional derived physical signal.

This milestone does not add:

- eccentric orbits;
- periapsis or orbital-element propagation;
- N-body or general celestial mechanics;
- Gregorian calendars or named Earth seasons;
- latitude-specific photoperiod;
- climate forcing from seasonality;
- vegetation seasonality;
- breeding windows;
- migration;
- dormancy or hibernation;
- user-facing seasonal controls.

Those remain later providers, consumers, or product-policy work.

## Consequences

Est gains a physically meaningful Derived seasonal signal without hard-coding
Earth calendar assumptions or prematurely introducing a full astronomy engine.

The provider is replaceable. A future orbital-mechanics provider can satisfy
the same seasonal-provider boundary while supplying richer astronomy.

Existing simulations remain unchanged unless a seasonal model is explicitly
configured.
