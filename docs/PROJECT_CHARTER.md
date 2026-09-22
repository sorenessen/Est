# Est Project Charter

## Vision

Est is a living planetary simulation.

Est models worlds as interconnected physical, environmental, biological, societal, and eventually individual systems that change through time.

The experience begins with a planet viewed from space. The user can observe its state, move through time, inspect causes and effects, and, in simulated worlds, intervene.

Est is designed to grow in resolution rather than simply grow in size.

The long-term scale includes planets, regions, settlements, communities, and individual inhabitants. Much later, Est may expand outward to star systems and galaxies.

The first implementation does not need those future scales, but foundational architecture should avoid preventing them.

## Earth Observatory

Earth Observatory represents the real Earth.

Historical state should be derived from historical evidence and authoritative datasets. Contemporary state should be updated from trustworthy real-world data sources and carefully processed current events.

Recorded reality must never silently become generated or simulated information.

Est should distinguish among:

- observed measurements
- historical records
- estimates and reconstructed data
- current events
- model assumptions
- simulated projections

The canonical Earth is read-only.

Users may create scenario experiments from known Earth states.

Examples include changes to emissions, atmospheric composition, energy production, population, technology, and natural or cosmic events.

Scenario results are modeled possibilities, not claims of certain prediction.

The long-term goal is to support thoughtful experiments about what Earth could plausibly look like 10, 50, or 100 years into the future.

## Living Worlds

Living Worlds is Est as a game.

Worlds may be procedurally generated or created from templates, including historical Earth states.

Unlike Earth Observatory, these worlds are editable.

Players may eventually alter planetary conditions, resources, ecosystems, civilizations, technologies, governments, events, and individual inhabitants.

The long-term objective is emergent history.

The player creates conditions.

The simulation creates consequences.

## Shared Principle

Earth Observatory and Living Worlds may share simulation components, visualization systems, physical models, and data structures.

They do not share epistemic status.

Observed, estimated, modeled, simulated, and generated information must remain distinguishable.

## Time

Time is a first-class component of Est.

Worlds have persistent chronology.

Simulation may be paused or accelerated.

World state may be checkpointed.

A state may be forked into a new timeline without changing its parent.

History is immutable. Futures branch.

## Simulation Scale

Est does not attempt to simulate every entity at maximum resolution simultaneously.

Simulation resolution changes with observational scale.

At planetary scale, populations may be aggregates.

At regional scale, they may become settlements and demographic groups.

At sufficiently close scale, important or representative inhabitants may become persistent individual agents.

Higher-resolution state must remain consistent with lower-resolution aggregate state.

## Scientific Integrity

Earth Observatory is intended to become a broad simulation of real-world systems rather than a model confined to one scientific or human domain.

Its long-term scope may include physical, biological, environmental, economic, social, technological, and geopolitical systems wherever they can be represented responsibly.

Est should preserve the ability to model causal relationships across those domains, including effects involving resources, populations, economies, markets, commodities, trade, governments, technology, ecosystems, climate, and other measurable systems.

Earth simulation should prefer established domain models, authoritative datasets, and transparent assumptions over invented equations.

Est should represent uncertainty, disagreement, missing knowledge, and model limitations explicitly rather than manufacture false precision.

Artificial intelligence is not the authority governing physical reality.

AI may eventually assist with agent decision-making, interpretation, narration, explanation, culture generation, diplomacy, dialogue, and summarization.

Deterministic or probabilistic simulation systems establish world state.

## Online Future

A world is a persistent data object independent of the device displaying it.

Initially, worlds may run locally.

Eventually, Est may host worlds so users can sign in through a browser and continue observing or interacting with them from anywhere.

Worlds need not consume dedicated computing resources continuously. Checkpoints, elapsed simulation time, scheduled advancement, and event processing may be used to maintain persistent hosted worlds efficiently.

World owners may eventually allow others to observe, visit, or interact according to explicit permissions.

## General Simulation Platform

Est's long-term game vision is not limited to a single genre, scale, or prescribed game loop.

The distant goal is a coherent simulation platform capable of supporting many different kinds of experiences over shared authoritative world state.

A player may eventually observe, manage, influence, inhabit, or directly participate in different parts of the simulation, from planetary and ecological systems through civilizations, economies, organizations, communities, households, and individual agents.

Gameplay should emerge from interoperating simulation systems wherever practical rather than from separate incompatible versions of reality for each mode of play.

The user should ultimately be able to decide what kind of experience Est becomes for them.

This breadth is a design horizon. It does not override the requirement to earn complexity incrementally.

## First Principle

Est earns complexity.

We will not build a galaxy before one planet is compelling.

We will not build cities before planetary systems work.

We will not add finer-grained simulation detail unless a concrete behavior, experience, or scientific requirement justifies it.

We will not build multiplayer before a persistent world is worth sharing.

First goal:

Create a planet. Make time move. Make the planet change. Make those changes understandable.

## Software Architecture Credo

Est's implementation should remain simple, readable, maintainable, and adaptable as its functional demands grow.

- Apply KISS and DRY. Prefer the smallest clear implementation that meets current requirements without unnecessarily constraining future capabilities.
- Keep ownership and dependencies explicit. Favor independently understandable, composable components and directional dependencies where practical.
- Model necessary interdependence without creating unnecessary code-level entanglement. Avoid circular dependencies, shared mutable state, concrete implementation leakage, and god objects.
- Preserve practical escape hatches. Components should be replaceable, bypassable, degradable, migrated, or redesigned when requirements, dependencies, scientific models, or technologies change.
- Keep the authoritative simulation independent of presentation, persistence, hosting, and vendor-specific infrastructure wherever practical.
- Avoid unnecessary assumptions about operating systems, deployment environments, geographic regions, languages, calendars, units, or world representations in the core.
- Introduce abstractions when they establish a useful boundary, not merely because a hypothetical future implementation might exist.
- Treat architectural principles as strong defaults, not purity requirements. A deliberate exception is acceptable when it produces a materially simpler, faster, more reliable, or higher-quality implementation and the resulting cost to performance, maintainability, reliability, scientific integrity, extensibility, or future flexibility is negligible. Keep exceptions bounded, explicit, documented, observable where useful, and reasonably reversible. Do not add meaningful complexity solely to avoid insignificant architectural risk.
- Revisit foundational ownership and architecture when incremental patches would materially compromise performance, reliability, maintainability, capability, or future flexibility.
- At milestone reviews, explicitly ask: What are we becoming accidentally dependent on? What should we keep, simplify, replace, defer, or abandon?

Future-proofing means preserving useful options, not implementing every possible future today.
