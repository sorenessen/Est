<h1 align="center">=== EST ===</h1>

<p align="center">
  <img width="1216" height="466" alt="Screenshot 2026-09-12 at 12 04 45 AM" src="https://github.com/user-attachments/assets/1a5425fe-1427-4e2c-818f-7a11fa8d0ae2" />
</p>

## A living planetary simulation.

<p align="center">
  <img width="250" height="120" alt="mount-rainier-night-terrain" src="https://github.com/user-attachments/assets/3f3123e0-af0e-41c9-94b3-ac2966fc7d5c" />
  <img width="250" height="120" alt="Screenshot 2026-09-12 at 12 08 40 AM" src="https://github.com/user-attachments/assets/b1a3c767-a876-4bf4-ade0-0e979c52458e" />
  <img width="250" height="120" alt="Screenshot 2026-09-12 at 12 07 52 AM" src="https://github.com/user-attachments/assets/acbe955c-f79f-436d-be10-cb4aa3daef6e" />
</p>


  

Est is a simulation-first platform for modeling worlds as interconnected physical, environmental, biological, societal, and eventually individual systems that evolve through time.

The project begins with a single planet viewed from space and is designed to scale in resolution toward regions, settlements, communities, and individual agents, and eventually outward toward star systems and galaxies.

## Product Directions

### Earth Observatory

A read-only representation of the real Earth grounded in historical evidence, authoritative datasets, and current real-world information.

Earth Observatory may support future-facing scenario experiments, but observed reality, model assumptions, and simulated outcomes must remain clearly distinguishable.

### Living Worlds

The game side of Est.

Players create or fork worlds, alter conditions, advance time, influence civilizations, and eventually interact with governments, settlements, and individual simulated inhabitants.

## Current Milestone

### Est 0.1 - First Light

<p align="center"><img width="560" height="425" alt="earth-baseline" src="https://github.com/user-attachments/assets/bc77e758-55b6-4053-9def-8c971112c93e" /></p>


Goal:

Create a planet. Make time move. Make the planet change. Make those changes understandable.

Original First Light scope:


- Headless simulation engine
- Persistent world state
- Simulation clock
- One planet
- Small set of planetary variables
- One causal simulation system
- Timeline and event history
- Save and load
- Browser-based 3D globe
- Pause and simulation-speed controls

The implementation has since expanded beyond that original vertical slice while
remaining within the First Light goal. Current authoritative systems now include
shared terrain and surface topology, hydrology, vegetation, biogeochemistry,
multiple consumer layers, material-backed lifecycle transitions, and an
independent Babylon planetary presentation.

Explicitly out of scope for First Light:

- AI-driven inhabitants
- Governments
- Civilizations
- Present-day population-scale simulation
- Multiplayer
- News ingestion
- Real Earth synchronization
- Star systems
- Galaxies

## Development Environment

- .NET SDK 10.0.301
- Target framework: .NET 10
- Primary development platform: macOS Apple Silicon

## Architectural Principle

The simulation engine must remain independent of rendering.

A world must be able to exist, advance, save, load, and be tested without any graphical client.

Est should preserve architectural escape hatches. Existing implementation decisions are not sacred if they begin to materially constrain performance, maintainability, reliability, scientific integrity, or future product possibilities.

## Current Living Biosphere Prototype

First Light now contains an interconnected authoritative living-world slice
rather than a standalone synthetic population prototype.

Humans remain individual agents with persistent identity, age, location,
survival state, reproduction, material state, and purposeful movement.
Configured human foraging consumes authoritative vegetation rather than a
separate synthetic food-resource layer.

The shared planetary surface now also carries authoritative hydrology,
vegetation, biogeochemistry, and aggregate invertebrate biomass. Terrestrial
grazers are represented as cohorts, birds as flocks, and wolves as individual
animals where behavior requires that resolution.

Current biological material flows are causal. Feeding and predation remove
authoritative source material; tracked nitrogen has an explicit destination;
mortality returns remaining organism material to detrital pools; and configured
growth, birth, or aggregate recruitment requires an explicit material source.
Current examples include material-backed wolf provisioning and reproduction,
grazer recruitment from consumed vegetation, invertebrate growth from consumed
vegetation, and bird recruitment from consumed invertebrate prey.

The browser globe advances the same authoritative simulation through Est.Api.
Simulation spatial resolution remains independent from the Babylon presentation
mesh, and biological behavior is never invented by the renderer.

Broad vegetation is now presented as living surface state rather than
globe-scale decoration. Current authoritative plant biomass is exposed through
the API, mapped onto the immutable Babylon sphere as per-vertex coverage, and
used by the terrain shader to present vegetated land continuously.

Generated terrestrial humans and wolves are constrained to dry habitat when
generated terrain and hydrology provide authoritative standing-water context.

This remains an intentionally bounded living-world simulation rather than a
reconstruction of modern or historical census-scale Earth. Species detail,
seasonal lifecycle activation, regional climate, soils, richer decomposition,
and evolutionary inheritance remain later work.

The current causal direction is:

`physical environment -> hydrology -> nutrients -> vegetation -> consumers -> predation / reproduction -> mortality -> detritus / nutrients`

The models remain deliberately coarse where finer resolution has not yet earned
its complexity, but the current biological stack now shares common authoritative
material and surface state.

## Current Planetary Renderer

Est's current planetary renderer is Babylon.js.

The production browser route is:

`http://127.0.0.1:5173/?session=<session-id>&view=observatory`

Cesium is retired from the current planetary-rendering path. Any Cesium code,
pages, screenshots, experiments, or documentation retained in this repository
are historical/evaluation evidence only.

**Do not use `/cesium.html` for current development, launch, smoke testing,
runtime validation, or simulation visualization.**

The current production planet uses one immutable Babylon icosphere. Authoritative
terrain is sampled from Est simulation state and baked into that fixed render
geometry. Simulation spatial resolution and render spatial resolution remain
independent.

Historical Cesium work established useful renderer-neutral evidence around
regional surfaces, local geometry, presentation scale, and browser-hosted globe
interaction. That evidence is preserved, but Cesium is not a current renderer
candidate and is not an alternate current runtime path.

For local development, `./scripts/dev/play.sh` opens a fresh Earth session in
the embodied Living World, while `./scripts/dev/observatory.sh` opens a fresh
Earth session in the planetary Observatory. Both use the same Babylon
application and authoritative simulation model; the presentation view can also
be switched in-app without creating a new session.

Focused presentation proofs can use development hints such as
`./scripts/dev/play.sh --focus fauna`.

See `docs/DEVELOPMENT.md` for development commands and
`docs/SURFACE_EVALUATION.md` for the data pipeline and next steps.
