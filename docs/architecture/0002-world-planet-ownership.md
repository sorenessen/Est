# ADR 0002: World and Planet Ownership

## Status

Accepted

## Context

Est begins with one planet, but its long-term product direction may eventually include multiple planets, star systems, and larger spatial hierarchies.

The First Light milestone does not require those future systems. Modeling them now would introduce speculative infrastructure and assumptions before they are needed.

At the same time, making `WorldState` permanently own exactly one `PlanetState` through a singular property would encode an unnecessary restriction into a foundational domain boundary.

## Decision

`WorldState` owns an immutable collection of `PlanetState` instances.

A world may currently contain zero or more planets.

Planet identity is stable through `PlanetId`, and duplicate planet identities within a world are rejected.

Planet membership changes through explicit `WorldState` operations:

- `AddPlanet`
- `ReplacePlanet`

Both operations return a new `WorldState` rather than mutating authoritative state in place.

No `StarSystem`, generic `CelestialBody`, orbital hierarchy, or galaxy model is introduced at this stage.

## Rationale

This is the smallest ownership model that supports the current one-planet milestone without making one planet a permanent architectural constraint.

It preserves room for future planetary expansion while avoiding speculative abstractions whose requirements are not yet known.

The design follows Est's architecture principle:

> Future possibility is a design constraint, not a requirement to build the future now.

## Consequences

Positive:

- Planet ownership is explicit.
- Authoritative world state remains immutable.
- One planet works without special-case architecture.
- Additional planets can be represented later without changing the fundamental world-to-planet ownership relationship.
- Future star-system or orbital models can be added above or around this boundary if their requirements justify them.

Tradeoffs:

- `WorldState` can represent an empty collection even though the initial product experience will normally use one planet.
- The collection does not currently model orbital relationships, primary bodies, stars, moons, or spatial hierarchy.
- Those concepts must be introduced later only when their real simulation requirements are understood.

## Deferred Questions

- Whether planets ultimately belong directly to a world, to star systems, or to another spatial ownership layer.
- How moons and non-planetary celestial bodies should be represented.
- How orbital relationships and interplanetary simulation should be modeled.
- Whether an empty world should remain valid once world creation workflows exist.

These questions are deliberately deferred rather than answered speculatively.
