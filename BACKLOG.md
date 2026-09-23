# Est Backlog

## Current Release

# v0.1.0 - First Light

Objective:

Build the smallest complete vertical foundation for Est: one persistent planet, a functioning simulation clock, one causal planetary system, and a browser-visible globe.

## Phase 0 - Foundation

### Project Setup

- [x] Initialize Git repository
- [x] Pin .NET SDK 10.0.301
- [x] Create Est.slnx
- [x] Create Est.Simulation
- [x] Create Est.Simulation.Tests
- [x] Add project references
- [x] Verify clean test run
- [x] Establish README
- [x] Establish project charter
- [x] Establish backlog

### Architecture Foundation

- [x] Establish simulation/application/persistence/API ownership boundaries
- [x] Define world identity model
- [x] Define immutable simulation-time representation
- [x] Define simulation clock
- [x] Define world state
- [x] Define planet state
- [x] Define simulation step/tick contract
- [x] Define event/history model
- [ ] Define intervention model
- [x] Define provenance concept without overbuilding it
- [x] Record initial architecture decisions

## Phase 1 - Time Exists

Goal:

A world can exist and advance through simulation time without any user interface.

Acceptance criteria:

- [x] World has stable identity
- [x] World has current simulation time
- [x] Time can advance deterministically
- [x] Simulation speed is separate from simulation state
- [x] Clock can pause
- [x] Clock supports explicit advancement
- [x] Tests prove deterministic advancement
- [x] No rendering dependency exists in Est.Simulation

## Phase 2 - First Planet

Goal:

A world owns one coherent planet state.

Initial candidate properties:

- [x] Radius
- [x] Surface gravity
- [x] Mean surface temperature
- [x] Atmospheric pressure
- [x] Atmospheric composition
- [x] Surface water fraction
- [x] Ice coverage
- [x] Habitability assessment foundation
  - [x] Define profile-specific assessment contract
  - [x] Preserve uncertainty and missing-factor reporting
  - [x] Establish Earth-like surface life reference profile
  - [x] Avoid universal uninhabitability claims from limited surface data
  - [ ] Implement scientifically calibrated biological profiles when supporting environmental models are available

Acceptance criteria:

- [x] Planet can be created from explicit initial conditions
- [x] Planet state is serializable
  - [x] Add separate Est.Persistence project
  - [x] Implement versioned JSON snapshot DTOs
  - [x] Reconstruct through validating domain constructors
  - [x] Verify basic round trip and invalid-domain rejection
  - [x] Complete malformed-snapshot and multi-planet coverage
  - [x] Establish serialization checkpoint
- [x] Planet state can be cloned or forked safely
  - [x] Copy preserves world identity and state
  - [x] Simple fork creates a new world identity
  - [x] Fork preserves planet identities and starting state
  - [x] Immutable operations allow independent divergence
  - [x] Historical timeline branching completed in Phase 4
- [x] Establish defined simulation-operation boundary
  - [x] Define ISimulationOperation and SimulationOperationExecutor
  - [x] Implement time advancement and planet-environment replacement operations
  - [x] Route SimulationClock through the operation boundary
  - [x] Verify immutable state transitions and target identity preservation
  - [ ] Revisit enforcement when causal systems require stronger execution guarantees

## Phase 3 - First Causal System

Goal:

Introduce one deliberately constrained planetary feedback model.

Candidate first system:

Temperature and ice-albedo feedback.

Acceptance criteria:

- [x] System consumes defined inputs
- [x] System produces deterministic outputs where expected
- [x] Advancing time changes planetary state
- [x] Cause of change can be represented in history
- [x] Tests cover stable, warming, and cooling cases
- [x] Model assumptions are documented
- [x] Model is clearly identified as simplified, not a real Earth climate model

## Phase 4 - Timeline and Branching

Goal:

World history becomes inspectable and forkable.

- [x] Append-only event history
- [x] Simulation checkpoints
- [x] Reset to initial state
- [x] Fork timeline from checkpoint
- [x] Parent and child timeline identity
- [x] Preserve immutable history semantics

Principle:

History is immutable. Futures branch.

## Phase 5 - Persistence

Goal:

A world can leave memory and return unchanged.

- [x] Save world
- [x] Load world
- [x] Version serialized state
- [x] Detect incompatible or corrupt state
- [x] Round-trip tests
- [x] Preserve timeline and history
- [x] Preserve provenance metadata

## Phase 6 - Est API

Goal:

Expose simulation operations without coupling the engine to presentation.

- [x] Create Est.Api
- [x] World creation endpoint
- [x] World-state endpoint
- [x] Advance-time endpoint
- [x] Pause and resume semantics
- [x] Intervention endpoint
- [x] Timeline endpoint
- [x] Persistence boundary
- [x] API integration tests

Acceptance: 202 tests passing. API creation, inspection, advancement,
intervention, and archive round trips are verified.

## Phase 7 - First Globe

Goal:

See Est.

Production client direction:

TypeScript web application with Babylon.js as the first production planetary
renderer.

Cesium is retained only as historical/evaluation evidence. It is not a
current renderer, alternate development path, fallback renderer, smoke-test
target, or runtime-validation target. Do not use `/cesium.html` for current
simulation visualization. ADR 0006 records the renderer reset and the explicit
separation between simulation spatial resolution and render spatial resolution.

Earlier Cesium work proved that a browser-hosted globe, authoritative API
integration, orbit/zoom interaction, local geometry, and multi-scale
presentation are feasible. Those proofs remain useful, but their rendering
implementation is not the production foundation.

- [x] Create Est.Web
- [x] Prove browser-hosted 3D globe interaction in the evaluation renderer
- [x] Prove authoritative world-state fetch and display
- [x] Prove continuous authoritative simulation advancement in a globe view
- [x] Prove authoritative population presentation and movement
- [x] Build the production Babylon planetary renderer
- [x] Add production-quality simulation time controls
- [x] Pause and resume
- [x] User-selectable 1x
- [x] User-selectable 2x
- [x] User-selectable 4x
- [x] User-selectable 10x
- [x] User-selectable 100x
- [x] User-selectable 1000x

### Planetary presentation

Architecture:

`docs/architecture/0006-planetary-rendering-separation.md`

Core rule:

`simulation spatial resolution != render spatial resolution`

The current production planet uses the immutable Babylon icosphere described in
ADR 0006 and `docs/CURRENT_STATE.md`.

The earlier cube-sphere, terrain-patch, quadtree, culling, stitching, and
camera-driven planetary LOD work is preserved as engineering evidence rather
than active implementation work. See ADR 0006 and
`docs/SURFACE_EVALUATION.md`.

Current presentation backlog:

- [ ] Add atmosphere after the current terrain, lighting, and standing-water
      foundation.
- [ ] Select representations by view scale when concrete product requirements
      require representation transitions or LOD.
- [x] Preserve `simulation truth -> API -> presentation`.
- [x] Do not infer simulation truth from renderer animation or procedural
      decoration.

Historical renderer cleanup:

- [x] Retarget normal Play Est flow to the production Babylon renderer.
- [x] Remove Cesium from the production application path.
- [x] Retain useful Cesium and regional-rendering evidence as historical
      engineering reference.
- [ ] Remove obsolete Cesium-specific terrain, imagery, and water adapters when
      doing so does not erase useful historical evidence.
- [ ] Remove obsolete Cesium dependencies when no preserved evaluation artifact
      requires them.
- [x] Keep launcher and development documentation on the Babylon production
      path.

Renderer milestones require browser runtime validation.

A green build or automated test suite does not override a failed visual runtime
gate.

### Local development launcher

- [x] Add a Sparrow workspace with Play Est as the primary action.
- [x] Preserve independent Start API and Start Web development tasks.
- [x] Launch API and Web in separate iTerm windows and wait for health.
- [x] Reuse healthy Est-owned listeners without duplicate service windows.
- [x] Refuse automatic termination or restart of occupied ports.
- [x] Create a fresh Earth session and open the current globe client from Play.
- [x] Validate cold-start Play from Sparrow and subsequent service reuse.
- [x] Retarget Play Est to the Babylon production renderer after R1 is
      runtime-green.
- [ ] Consider portable terminal integration if Est development expands beyond macOS.
- [ ] Revisit durable session selection/resumption when the product requires it.

### Living population vertical slice

The First Light scope now includes a deliberately small individual-population
prototype because it provides visible causal simulation behavior on the globe.
This does not imply a commitment to present-day census scale or full
civilization simulation.

- [x] Define authoritative individual `PersonState` population.
- [x] Persist population through world snapshots and timeline archives.
- [x] Seed a deterministic synthetic founder distribution on Earth.
- [x] Expose authoritative population through Est.Api.
- [x] Render the authoritative population on the globe.
- [x] Add basic energy, health, starvation, and activity state.
- [x] Add durable planetary food resources.
- [x] Integrate survival/ecology at bounded internal cadence without globally substepping session history.
- [x] Expose survival state through Est.Api.
- [x] Advance the authoritative simulation continuously in the development globe view.
- [x] Add purposeful food-seeking travel rather than teleporting distant food consumption.
- [ ] Replace the current proof-oriented ecology constants with explicit model policy when the next requirements justify it.
- [x] Add meaningful resource renewal and depletion pressure.
- [x] Add scarcity-driven migration toward viable food beyond the local foraging radius.
- [x] Evaluate a minimal prey/hunting loop.
- [x] Replace purely demographic reproduction with condition/interaction-driven reproduction when agent interaction is ready.
- [ ] Add presentation LOD for population clusters versus individual people.
- [x] Select and integrate a durable animated person presentation now that close-range manifested interaction is establishing its requirements.

### Playable Ester embodiment and encounter slice

The current Living Worlds path now has enough simulation, identity, and
presentation foundation to move from observing inhabitants toward physically
participating in the same authoritative world.

Architecture:

- `docs/architecture/0018-person-social-recognition-foundation.md`
- `docs/architecture/0019-playable-ester-manifestation.md`
- `docs/design/returning-ester-recognition-proof.md`

Completed foundation:

- [x] Give an Ester stable identity independent of one runtime simulation
      session.
- [x] Add authoritative manifested-Ester state with planet and geographic
      location.
- [x] Route manifestation and movement through application/API authority rather
      than allowing the renderer to own player location.
- [x] Support responsive WASD movement with client-side presentation prediction
      while retaining server-authoritative geographic state.
- [x] Establish a Babylon local metre-space around the manifested Ester without
      creating a second simulation coordinate authority.
- [x] Keep Ester visually centered while translating authoritative nearby world
      state into the local presentation frame.
- [x] Present local terrain by continuously sampling authoritative macro terrain
      and layering explicitly non-authoritative deterministic visual detail.
- [x] Add geographically stable local surface scatter and animated grass.
- [x] Stream grass independently of terrain re-anchoring and eliminate visible
      whole-field regeneration popping during normal movement.
- [x] Key nearby human presentation by stable authoritative `PersonId`.
- [x] Preserve the existing person-owned social-recognition model needed for a
      returning Ester to be recognized later.

Completed causal slice:

- [x] Replace the temporary nearby-human capsule with the first proper human
      avatar presentation while preserving `PersonId` as identity and keeping
      the visual asset non-authoritative.
- [x] Define the physical encounter boundary from authoritative Ester/person
      world positions rather than renderer mesh overlap.
- [x] Route a qualifying world-generated Ester/person encounter through the
      existing authoritative simulation operation.
- [x] Expose whether that person recognized the Ester before a repeated
      encounter is recorded.
- [x] Runtime-prove the sequence:
      `approach person -> first encounter -> leave -> return -> same person
      recognizes same Ester`.
- [x] Keep dialogue, relationships, reputation, generated language, and richer
      cognition outside this slice while making the physical recognition loop
      authoritative and observable.

Presentation follow-up after the causal loop is green:

- [x] Add human locomotion/orientation presentation driven by authoritative
      movement rather than renderer-authored behavior.
- [ ] Refine human terrain contact and close-range camera framing.
- [ ] Add local vegetation/detail LOD only when runtime evidence shows the
      current presentation requires it.
- [ ] Replace temporary Ester capsule presentation when a durable player-avatar
      requirement is selected.

The priority is the causal gameplay loop, not additional cosmetic polishing of
the already-proven terrain and grass presentation.

### Local fauna presentation verification

The wolf and grazer presentation foundations exist, but close-range animation
quality must pass the same browser-runtime acceptance standard used for human
locomotion. Automated tests do not substitute for visual verification.

- [ ] Runtime-verify wolf locomotion against successive authoritative snapshots,
      including facing, gait activation, visual speed, foot sliding, stopping,
      and interpolation behavior.
- [ ] Runtime-verify grazer locomotion against its authoritative cohort-backed
      presentation inputs, including facing, gait activation, visual speed,
      foot sliding, stopping, and representative continuity.
- [ ] Verify idle/walk transitions do not introduce mesh deformation, popping,
      skating, treadmill motion, root stalls, or reconciliation snaps.
- [ ] Compare authored animation cadence with actual presented root travel and
      correct any proven timing or speed mismatch.
- [ ] Keep all fauna fixes presentation-only unless runtime evidence identifies
      a genuine authoritative simulation defect.
- [ ] Require a clean browser runtime acceptance pass for each fauna path before
      treating its locomotion presentation as visually complete.

### Biosphere support foundation

The living-population prototype now needs a physical ecological
foundation capable of supporting plants, invertebrates, birds, terrestrial
animals, and eventually decomposition and nutrient cycling.

Architecture: `docs/architecture/0005-biosphere-surface-hydrology.md`.

- [x] Define reusable planet-surface cell identity and grid contract.
- [x] Implement deterministic coarse spherical surface grid.
- [x] Verify cell area accounting, lookup, and neighbor topology.
- [x] Define durable per-cell terrain state.
- [x] Generate deterministic tectonic-informed planet-scale topography from a seed.
- [x] Derive flooded land/ocean/lake state from terrain plus hydrologic water inventory.
  - [x] Derive connected standing-water bodies from authoritative per-cell
        surface-liquid state.
  - [x] Initialize global-equilibrium flooded distribution from planet-level
        water inventory and terrain geometry.
  - [x] Add basin-local fill / spill equilibrium for perched inland lakes.
- [x] Derive slope and downhill-neighbor topology for drainage.
- [x] Preserve terrain through world copy, fork, snapshot, and archive.
- [x] Expose authoritative terrain through Est.Api.
- [x] Define durable per-cell hydrology state.
- [x] Preserve hydrology through world copy, fork, snapshot, and archive.
- [x] Define explicit hydrology model policy in `SimulationDefinition`.
- [x] Implement conservative evaporation / precipitation / infiltration /
      runoff transfers.
- [x] Add freezing and melting water transfers.
- [x] Add water mass-conservation telemetry and tests.
- [x] Seed deterministic generated terrain and hydrology for the development planet.
- [x] Expose authoritative hydrology through Est.Api.
- [x] Prove authoritative hydrology can be visualized after state is stable.
  - [x] Expose shared surface-cell geometry needed to locate hydrology cells.
  - [x] Prove renderer access to authoritative surface-liquid state.
  - [x] Reject direct per-cell water polygons as a production representation
        after runtime evaluation exposed simulation-grid coastlines.
  - [x] Re-present large standing-water bodies through the R4 continuous-ocean
        renderer without changing authoritative hydrology ownership.
- [x] Build plant biomass on the shared surface substrate.
  - [x] Define durable per-cell live plant biomass on the shared surface grid.
  - [x] Preserve vegetation through world copy, fork, snapshot, and archive.
  - [x] Define explicit vegetation model policy in `SimulationDefinition`.
  - [x] Make terrain, soil-water availability, and climate causal inputs to
        plant productivity.
  - [x] Seed deterministic generated vegetation only on dry surface cells.
  - [x] Execute vegetation after hydrology and before biological consumers.
  - [x] Expose generated vegetation and vegetation model policy through Est.Api.
  - [x] Expose current authoritative vegetation state through Est.Api and present
        broad biomass as terrain-surface coverage on the immutable Babylon sphere.
  - [x] Keep globe-scale vegetation out of the sprite layer; reserve discrete
        vegetation assets for future closer-scale representations where useful.
  - [x] Constrain generated terrestrial human and wolf founders to dry habitat
        when authoritative terrain and hydrology are available.
  - [x] Connect population foraging to authoritative live plant biomass through
        an explicit harvest / energy-conversion policy.
  - [x] Preserve biomass accounting during consumption and keep synthetic food
        out of the vegetation-backed foraging path.
  - [x] Prove the existing population survival vertical slice can run against
        authoritative vegetation through session, API, and persistence tests.
  - [ ] Add explicit plant stress / mortality behavior for conditions such as
        later inundation, drought, temperature extremes, and other ecological
        losses when that lifecycle layer is designed.
- [x] Remove the synthetic food-resource scaffolding now that authoritative
      vegetation preserves the population survival vertical slice.
  - [x] Remove synthetic food from authoritative world state, current world
        creation, and Est.Api contracts.
  - [x] Make vegetation-backed foraging the only configured biological
        food-consumption path.
  - [x] Advance world snapshot schema to 11 without serializing synthetic food.
  - [x] Preserve read compatibility for snapshot schemas 4-10 by validating
        and discarding legacy food-resource payloads.
- [x] Add aggregate invertebrate / bug populations.
  - [x] Add durable authoritative per-cell aggregate live-biomass state on the
        shared surface grid, including world invariants and snapshot/timeline
        persistence.
  - [x] Add explicit per-planet invertebrate model policy and deterministic
        initialization from existing authoritative ecological state.
  - [x] Add causal aggregate biomass dynamics supported by authoritative
        vegetation without assuming a trophic role that the aggregate state
        does not encode.
  - [x] Wire aggregate invertebrate dynamics into the causal session order
        after vegetation and validate configuration, persistence, and runtime
        behavior.
- [x] Add bird population / flock representation.
  - [x] Add durable authoritative flock state with stable identity, planet,
        member count, and geographic center, including world invariants and
        snapshot/timeline persistence.
  - [x] Add explicit per-planet bird model policy and deterministic flock
        initialization from authoritative ecological state.
  - [x] Add causal flock movement and survival behavior supported by
        authoritative habitat, prey, and water without prematurely
        materializing individual birds.
  - [x] Wire bird dynamics into the causal session order after invertebrates,
        expose current flock state through the session/API boundary, and
        validate configuration, persistence, and runtime behavior.
- [x] Generalize terrestrial fauna beyond the current wolf-specific slice.
  - [x] Add durable authoritative grazer-cohort state with stable identity,
        planet, member count, and geographic center, including world invariants
        and snapshot/timeline persistence.
  - [x] Add explicit per-planet grazer model policy and deterministic cohort
        initialization from authoritative vegetation support.
  - [x] Add causal grazing, movement, and survival behavior supported by
        authoritative vegetation, water, and habitat.
  - [x] Wire grazer dynamics through the session/API boundary and validate
        configuration, persistence, and runtime behavior end to end.
  - [x] Connect wolves to ecologically appropriate terrestrial prey without
        collapsing cohort fauna into the existing wolf-specific individual
        `AnimalState` representation.
- [x] Add decomposition and nutrient cycling.
  - [x] Add durable authoritative per-cell biogeochemistry state for detrital
        biomass, detrital nitrogen, and plant-available nitrogen on the shared
        surface grid, including world invariants and snapshot/archive
        persistence.
  - [x] Add explicit per-planet decomposition and nutrient-cycling model policy.
  - [x] Add environmentally constrained decomposition that transfers nitrogen
        from detritus into the plant-available pool with explicit mass balance.
  - [x] Make plant productivity consume and respond to authoritative available
        nitrogen without breaking existing biomass accounting.
  - [x] Route mortality from authoritative material-bearing biological state
        into detritus across current plant, invertebrate, human, wolf, bird, and
        grazer representations while preserving explicit biomass and nitrogen
        accounting.
  - [x] Wire biogeochemistry through the session/API boundary and validate the
        closed terrestrial nutrient loop end to end.
- [ ] Unify organism material and lifecycle accounting before evolutionary
      population dynamics.
  - [x] Add shared authoritative live biomass and nitrogen state that works for
        both individual organisms and aggregate cohorts/flocks without forcing
        them into one behavioral representation.
  - [x] Seed organism material through explicit world-creation/model policy and
        persist/expose it without hiding species composition assumptions inside
        behavior systems.
  - [x] Route human, wolf, bird, and grazer mortality through common detrital
        material-transfer semantics.
  - [x] Close human-foraging and grazer-grazing material leaks so consumed
        biomass and tracked nitrogen have explicit destinations.
  - [x] Replace count-and-energy-only predation with explicit prey-material
        accounting while preserving species-specific hunting behavior.
  - [x] Establish shared lifecycle semantics for birth/recruitment, growth,
        maturation, reproductive eligibility, aging, seasonal state, and death
        without forcing individual, flock, cohort, and spatial-aggregate
        organisms into one behavioral representation.
  - [ ] Complete individual human and wolf lifecycles, including sex and age,
        mate seeking, reproduction, causal offspring material, juvenile growth,
        maturation, senescence, and species-appropriate seasonal behavior.
  - [ ] Add demographic lifecycle structure to bird flocks so breeding,
        hatching/recruitment, juvenile maturation, mortality, migration, and
        other species-appropriate seasonal behavior can occur without
        materializing every bird.
  - [ ] Add demographic lifecycle structure to grazer cohorts so mating,
        gestation/calving or equivalent recruitment, juvenile maturation,
        senescence, mortality, and species-appropriate seasonal behavior can
        occur without materializing every grazer.
  - [x] Make invertebrate aggregate reproduction and biomass growth draw from an
        authoritative material source rather than allowing carrying capacity to
        create tracked biomass implicitly.
  - [ ] Give every runtime birth, hatch, recruitment, and growth transition a
        causal material source before inheritance depends on newly created
        organisms.
  - [ ] Add species-policy seasonal lifecycle strategies such as breeding
        seasons, migration, torpor, dormancy, and hibernation where biologically
        appropriate rather than treating any one strategy as universal.
  - [ ] Validate material and nitrogen accounting across feeding, growth,
        predation, reproduction, mortality, decomposition, persistence, and
        session advancement.
- [ ] Explore evolutionary population dynamics above the mature biosphere
      substrate.

Current ownership rule:

`Environment -> terrain -> hydrology -> biomass -> organisms -> needs -> decisions -> actions -> consequences`

Simulation owns the chain. The renderer visualizes its results.

### Regional climate and seasonal forcing foundation

The first regional physical-climate chain is now established without replacing
Est's broader long-term climate roadmap.

Architecture:

- `docs/architecture/0007-seasonal-control-foundation.md`
- `docs/architecture/0009-derived-astronomical-seasonality.md`
- `docs/architecture/0010-surface-solar-geometry.md`
- `docs/architecture/0011-atmospheric-shortwave-transmission.md`
- `docs/architecture/0012-atmospheric-shortwave-energy-partition.md`
- `docs/architecture/0013-surface-shortwave-reflection-absorption.md`
- `docs/architecture/0014-thermal-reservoir-longwave-foundation.md`
- `docs/architecture/0015-regional-surface-atmosphere-radiative-energy-budget.md`
- `docs/architecture/0016-regional-thermal-state-authority-migration.md`
- `docs/architecture/0017-hydrology-regional-temperature-consumption.md`

- [x] Add explicit dormant seasonal control without rewriting simulation time.
- [x] Derive astronomical seasonality and physical subsolar latitude.
- [x] Add deterministic latitude-specific surface solar geometry.
- [x] Add clear-sky atmospheric direct-shortwave transmission.
- [x] Partition atmospheric shortwave energy into direct, absorbed, downward
      scattered, and upward-scattered components.
- [x] Add surface shortwave reflection and absorption.
- [x] Add thermal-reservoir response and longwave-radiation foundations.
- [x] Add a two-reservoir regional surface-atmosphere radiative energy budget.
- [x] Define explicit migration from planetary EBM authority to regional thermal
      authority.
- [x] Add durable per-cell regional surface and atmospheric thermal state.
- [x] Persist regional thermal state and regional thermal model policy.
- [x] Enforce per-planet mutual exclusion between planetary EBM authority and
      regional thermal authority.
- [x] Activate deterministic causal regional thermal evolution with bounded
      integration.
- [x] Derive the compatibility planetary mean from authoritative regional
      surface temperatures under regional authority.
- [x] Preserve seasonal -> thermal -> hydrology causal ordering.
- [x] Make hydrology freezing and melting consume regional surface temperature
      only when regional thermal authority is configured.
- [x] Preserve planetary compatibility-temperature behavior for EBM worlds,
      worlds without causal thermal authority, and dormant regional state.

Further regional physical couplings remain intentionally deferred until a new
architecture decision selects them. Current completion does not imply
latent-heat coupling, regional cryosphere authority, temperature-dependent
evaporation, humidity/vapor-pressure physics, horizontal heat transport,
non-radiative surface-atmosphere exchange, or automatic migration of vegetation
and biology to cell-local temperature.

## Phase 8 - First Intervention

Goal:

The user changes one thing and sees understandable consequences.

- [ ] Select one safe prototype intervention
- [ ] Apply intervention through simulation domain
- [ ] Record intervention in history
- [ ] Run simulation forward
- [ ] Compare before and after state
- [ ] Explain causal chain at prototype level

# Future Milestones

## Earth Observatory

Earth Observatory is intended to grow into a broad real-world systems simulation, not a simulator of any single domain.

Est should eventually represent as much of the physical, biological, environmental, economic, social, technological, and geopolitical world as can be modeled responsibly with available evidence, established domain models, transparent assumptions, and explicit uncertainty.

Candidate domains include, but are not limited to:

- atmosphere, weather, and climate
- oceans, hydrology, and cryosphere
- geology and natural hazards
- ecosystems and biodiversity
- agriculture and food systems
- natural resources and energy
- populations and demographics
- public health
- migration
- infrastructure and transportation
- housing and land use
- economies and macroeconomic indicators
- markets, commodities, prices, and supply chains
- finance, currencies, and trade
- industries, firms, labor, and employment
- governments, institutions, law, and public policy
- international relations and conflict
- science and technology
- education and human development
- culture and social change
- disasters and major real-world events
- interactions and causal effects among these systems

This breadth is a long-term design target, not a requirement to implement all domains at once.

- Real Earth canonical world
- Historical snapshots
- Data provenance
- Source attribution
- Current-condition ingestion
- News and event interpretation pipeline
- Scenario experiments
- Cross-domain causal simulation
- Economic, market, and commodity projections
- Uncertainty representation
- Model and version attribution
- Observed versus estimated versus simulated state

## Living Worlds

Living Worlds is the long-term game side of Est.

Est is not intended to prescribe one permanent game loop. The distant goal is a general simulation platform capable of supporting many different kinds of experiences within the same coherent world model.

A player may eventually choose to observe, manage, influence, inhabit, or directly participate in the simulation at different scales. Possible experiences may include planetary stewardship, ecosystem management, civilization building, economics, markets, government, warfare, exploration, business, city building, family or individual life, scientific experimentation, alternate history, spaceflight, or other forms of play that emerge from the simulated systems.

Different experiences should share authoritative underlying world state rather than becoming disconnected games with incompatible versions of reality.

This is a design horizon, not a requirement to build every possible game mode now.

- Procedural planets
- Biomes
- Resources
- Biosphere
- Populations
- Civilizations
- Technology
- Governments
- Culture
- Trade
- Conflict
- Migration
- Economies
- Markets
- Organizations and firms
- Households
- Persistent individual agents
- Exploration
- Spaceflight
- Emergent events
- Player interventions
- Multiple styles of play over shared simulation state

## Increased Resolution

- Regions
- Settlements
- Cities
- Communities
- Buildings
- Persistent individual agents
- Ground-level visualization
- Aggregate-to-individual reconciliation

## Online and Hosted Worlds

- Accounts
- Cloud persistence
- Hosted simulation workers
- Browser access
- Efficient catch-up simulation
- World permissions
- Public and private worlds
- Visiting worlds
- Shared interactions

## Beyond One Planet

- Moons
- Orbital relationships
- Star-system simulation
- Interplanetary travel
- Terraforming
- Colonization
- Known-system templates
- Procedural systems
- Galaxies

# Explicitly Deferred Ideas

These are intentionally not rejected. They are simply not allowed to distort First Light.

- LLM-driven civilizations
- Individual AI inhabitants
- Real-time multiplayer
- MMO architecture
- Photorealistic rendering
- Full physical climate modeling
- Full economic modeling
- Every person on Earth as an agent
- Detailed spacecraft
- Interstellar travel
- Galaxy generation
- Cross-universe travel

### Historical renderer evaluation

Completed and superseded Cesium, regional-surface, TMS, local-geometry,
view-significance, and multi-scale presentation experiments are preserved in:

- `docs/SURFACE_EVALUATION.md`
- `docs/archive/cesium/`
- `docs/architecture/0006-planetary-rendering-separation.md`

They are engineering evidence, not active backlog.

If a future product requirement needs one of those capabilities, add a new
current backlog item based on that requirement and reuse the preserved evidence
rather than reopening historical experiment checkboxes.
