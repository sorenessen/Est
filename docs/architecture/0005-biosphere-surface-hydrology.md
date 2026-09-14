# ADR 0005: Planet Surface Fields and Hydrology Foundation

## Status

Accepted for implementation.

## Context

Est now has causal planetary climate, durable population state,
synthetic food resources, individual animals, reproduction, and
predator interactions.

Those systems are sufficient for a vertical simulation slice, but they
do not yet form a self-sustaining biosphere.

The next biological layers require shared environmental state:

- plants require water, climate, soil, and light;
- invertebrates require plants, moisture, detritus, and other
  invertebrates;
- birds and terrestrial animals require vegetation, prey, water, and
  habitat;
- decomposition eventually requires dead biomass, fungi, microbes, and
  nutrient cycling.

Water is the first missing physical cycle underneath all of those
systems.

The current `PlanetEnvironment` contains deliberately coarse planetary
properties:

- mean surface temperature;
- surface water fraction;
- ice coverage fraction;
- atmosphere.

Those values are planetary boundary state. They are not sufficient to
represent evolving regional hydrology such as precipitation, soil
moisture, runoff, drought, snowpack, or groundwater.

Est also currently has no common spatial substrate for environmental
fields. Adding one bespoke coordinate model for hydrology, another for
vegetation, and another for small-animal populations would create
avoidable architectural fragmentation.

## Decision

Est will introduce a reusable planet-surface grid abstraction before
building detailed hydrology.

The surface grid will provide stable cell identity and spatial topology
for environmental and biological fields.

Hydrology, vegetation, invertebrate populations, regional climate, and
other future spatial systems will reference the same surface-cell
identity rather than creating independent location grids.

### Planet independence

The simulation core must support arbitrary spherical planets.

The surface-grid contract must therefore not expose an Earth-specific
grid implementation as a domain primitive.

Hierarchical discrete global grid systems such as H3 and S2 demonstrate
useful properties for Est:

- stable compact cell identifiers;
- neighborhood traversal;
- hierarchical resolution;
- efficient aggregation;
- suitability for spatial flow.

However, Est will not make H3, S2, WGS84, or any other Earth reference
system part of its domain contract.

The initial implementation may use a simple latitude/longitude
tessellation behind the abstraction because it is transparent,
deterministic, dependency-free, and sufficient to establish causal
behavior.

The tessellation implementation must remain replaceable without
changing hydrology, vegetation, or fauna state contracts.

### Surface-cell identity

Surface cells will have opaque stable identity.

Domain systems must not infer cell geometry by decoding the identifier
themselves.

A surface-grid implementation will own:

- cell creation;
- center coordinates;
- physical area;
- neighbor lookup;
- coordinate-to-cell lookup;
- resolution metadata.

This leaves open future replacement with a hierarchical equal-area or
near-equal-area discrete global grid.

### Static geometry versus evolving fields

Surface geometry and evolving environmental state are different
concerns.

The shared surface layer describes where a cell is and how it relates
to neighboring cells.

Hydrology describes water currently present in that cell.

Vegetation will later describe plant biomass currently present in that
cell.

Small-animal and insect systems may describe population density or
cohorts currently present in that cell.

Large animals and people may continue to exist as individual agents
whose coordinates can be mapped into surface cells when interaction
with environmental fields is required.

### Hydrology state

Hydrology must represent water as conserved physical stores and fluxes,
not merely visual or normalized percentages.

The first durable hydrology state should support water held as:

- atmospheric water;
- surface liquid water;
- soil water;
- snow or ice water equivalent.

Groundwater will remain an explicit extension point rather than being
faked as surface water.

State should use physical units suitable for mass-balance checks.
Per-cell water depth or kilograms per square meter are preferred because
they compose naturally with cell area.

### Hydrology processes

The first hydrology causal system will establish a conservative water
cycle with bounded integration.

Initial processes:

1. evaporation from exposed liquid water;
2. evapotranspiration hook for future vegetation;
3. condensation / precipitation;
4. infiltration into soil;
5. surface runoff;
6. freezing and melting where temperature supports it.

Later processes may add:

- groundwater recharge and discharge;
- river routing;
- lake and basin formation;
- snow transport;
- regional atmospheric moisture transport;
- vegetation-dependent interception;
- erosion and sediment transport.

The first implementation is a causal foundation, not a complete Earth
hydrology model.

### Conservation

Except where a model explicitly exchanges water with an external
reservoir, total modeled water mass must be conserved within numerical
tolerance.

Every hydrology step must expose enough telemetry to verify:

- water before the step;
- water after the step;
- evaporation;
- precipitation;
- infiltration;
- runoff;
- freeze / melt transfer;
- conservation error.

Conservation is an architectural invariant, not merely a tuning goal.

### Climate coupling

The existing planetary energy-balance model remains the current source
of thermal boundary conditions.

Hydrology will consume climate state after climate has evaluated for
the current simulation step.

The existing ordered `SimulationStepRunner` already permits this:

    climate
      -> hydrology
      -> vegetation
      -> consumers
      -> consequences

A later regional-climate model can replace global temperature input with
cell-level temperature, insolation, humidity, and atmospheric transport
without changing hydrology ownership.

### PlanetEnvironment ownership

`PlanetEnvironment.SurfaceWaterFraction` will remain for now as a coarse
planetary surface descriptor and compatibility field.

It must not become the dynamic water-cycle reservoir.

As detailed surface geography and hydrology mature, the relationship
between coarse surface-water fraction and spatial water state should be
made explicit. The coarse field may eventually become derived state or
creation metadata.

### WorldState ownership

`WorldState` owns evolving simulation state.

Hydrology therefore belongs in world state, analogous to population,
animals, and current food resources.

Hydrology state must:

- belong to a valid planet;
- reference valid surface cells;
- reject duplicate cell state;
- survive copy and fork;
- survive snapshots and timeline archives;
- remain immutable through simulation operations.

### SimulationDefinition ownership

Hydrology policy belongs in `SimulationDefinition`, not in individual
water-state objects.

Hydrology model parameters may include, as justified by implementation:

- integration cadence;
- evaporation coefficients;
- soil water capacity;
- infiltration limits;
- runoff behavior;
- precipitation thresholds;
- freezing / melting response.

Model parameters must be persisted with timeline archives so replay and
branching preserve simulation semantics.

### Biosphere progression

The intended causal stack is:

    stellar energy
      -> planetary / regional climate
      -> hydrology
      -> soil and nutrient state
      -> plant biomass
      -> invertebrate populations
      -> birds / beasts / other consumers
      -> predation / scavenging
      -> decomposition
      -> nutrients
      -> plant biomass

This is a dependency direction, not a mandate that all systems run at
the same temporal or spatial resolution.

### Multiple biological scales

The common surface layer does not imply that every organism becomes a
cell-density field.

Expected representation:

- grasses and broad vegetation:
  biomass fields;
- forests:
  biomass fields plus selected individual trees when useful;
- insects and other very small organisms:
  densities, colonies, swarms, or cohorts;
- birds:
  cohorts or flocks with optional individual materialization;
- large terrestrial animals:
  cohorts where appropriate and individual agents where behavior
  matters;
- humans:
  individual agents at the current detailed simulation scale.

Future simulation LOD may materialize and dematerialize individuals from
aggregate populations while preserving conserved population and biomass
state.

## Initial implementation sequence

### Phase A: shared surface substrate

1. Define opaque `SurfaceCellId`.
2. Define a planet-surface grid contract.
3. Implement deterministic coarse spherical latitude/longitude grid.
4. Verify cell identity, area accounting, coordinate lookup, and
   neighbors.
5. Keep grid implementation replaceable.

### Phase B: hydrology state

1. Define per-cell hydrology state.
2. Add immutable world ownership and replacement operation.
3. Add snapshot persistence.
4. Add world copy / fork tests.
5. Expose conservation-safe constructors and transitions.

### Phase C: hydrology causal system

1. Define explicit model parameters.
2. Establish bounded integration.
3. Implement conservative transfers among water stores.
4. Add conservation telemetry.
5. Add dry, wet, freezing, melting, and long-step tests.

### Phase D: application and presentation integration

1. Seed deterministic hydrology for development Earth.
2. Add hydrology model definition to session execution.
3. Expose hydrology telemetry through the API.
4. Add globe visualization for precipitation / soil moisture /
   surface-water state only after the state is authoritative.

### Phase E: vegetation

Build plant biomass on the shared surface cells and make water
availability a causal input to productivity.

Synthetic food resources remain temporary scaffolding until vegetation
can replace their ecological role without breaking the living-population
vertical slice.

## Consequences

### Positive

- Water becomes causal simulation state rather than decoration.
- Plants and small organisms gain a common spatial substrate.
- Future fauna no longer requires hand-authored food patches.
- Spatial LOD remains possible.
- Arbitrary planets remain first-class.
- A later DGGS implementation can replace the initial grid.
- Mass conservation becomes testable.

### Costs

- `WorldState` and persistence gain another durable state family.
- Simulation definitions gain hydrology policy.
- Spatial topology becomes a foundational subsystem.
- Regional realism still requires later climate, terrain, soils, and
  atmospheric transport.

These costs are preferable to letting each biosphere subsystem invent
its own spatial representation.
