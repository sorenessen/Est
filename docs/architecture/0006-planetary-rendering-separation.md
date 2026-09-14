# ADR 0006: Planetary Rendering and Simulation Spatial Separation

## Status

Accepted for implementation.

## Context

Est's authoritative simulation and its visual planet have reached an important
architectural boundary.

ADR 0003 established that presentation may use different representations at
different viewing scales and that presentation does not own simulation truth.

ADR 0005 established a reusable planet-surface grid for terrain, hydrology,
vegetation, regional climate, and other spatial simulation fields. Its initial
latitude/longitude tessellation was deliberately chosen as a simple,
replaceable implementation behind an opaque surface-grid abstraction.

Recent runtime work exposed a critical distinction that was not yet made
explicit enough:

The spatial resolution and topology used to simulate a planet must not be
treated as the geometry used to draw that planet.

The current development planet uses a coarse authoritative surface grid.
That grid is useful for causal simulation, conservation, neighborhood
relationships, durable terrain values, hydrology, ecology, and aggregation.

It is not an acceptable finished visual surface.

Several Cesium-based experiments demonstrated the failure mode.

First, rendering standing water directly as one polygon per authoritative
surface cell exposed the simulation tessellation visually. Coastlines became
blocky and stair-stepped because the renderer was drawing the data structure
rather than a continuous world represented by that data.

Second, the known-good Cesium `CustomHeightmapTerrainProvider` could reproduce
authoritative elevation geometry, but its terrain-lighting path did not provide
the surface-normal control required for Est's intended visual treatment.

A custom quantized-mesh experiment attempted to recover that control. Its unit
tests and production build passed, but browser validation failed with visible
tile-sector artifacts, seams, disappearing regions, and camera-dependent
geometry instability. The experiment was rolled back.

A second experiment generated Est-controlled raster surface imagery with
terrain coloring and hillshade. Its tests and production build also passed, but
runtime validation again failed the visual gate. The planet still read as
large painted cells, useful terrain relief was absent, and the approach did not
solve the underlying representation problem. That experiment was also rolled
back.

These failures are useful architectural evidence.

They show that Est should stop adapting its simulated planetary fields into a
GIS renderer's terrain and imagery abstractions and instead use the same broad
class of rendering architecture already established by planet-scale games and
real-time procedural-world systems.

## Decision

Est will explicitly separate simulation spatial representation from render
spatial representation.

The authoritative simulation surface grid remains a domain and simulation
structure.

The visual planet will use a separate, multi-resolution rendering structure
whose topology, density, and level of detail are chosen for rendering quality
and performance rather than simulation storage.

The core relationship is:

    authoritative simulation fields
      -> renderer-neutral sampling / presentation inputs
      -> multi-resolution visual planet
      -> GPU geometry, materials, lighting, and effects

The renderer must never infer simulation truth from visual detail.

### Authoritative simulation surface

The shared Est surface grid continues to own stable macro-scale spatial state
for systems that need it.

Examples include:

- durable terrain elevation;
- hydrology stores and fluxes;
- regional climate fields;
- vegetation and biomass;
- ecological densities and cohorts;
- aggregation and neighborhood relationships.

The current latitude/longitude tessellation remains an implementation behind
the existing surface-grid abstraction.

It is not promoted to the visual mesh.

A future equal-area, hierarchical, geodesic, or other discrete global grid may
replace it without requiring the renderer to use the same topology.

### Render surface

The production planetary renderer will use a cube-sphere divided into
hierarchical quadtree patches.

Each cube face can recursively subdivide according to view-dependent level of
detail.

Visual terrain patches will use a reusable regular mesh topology. Patch
vertices will be displaced from a spherical base using terrain sampled from
Est-owned presentation inputs.

The renderer may use substantially more vertices and pixels than the
authoritative simulation has cells.

That is expected.

Render resolution answers:

    How much geometry and visual information are needed for this view?

Simulation resolution answers:

    At what spatial resolution must authoritative state be modeled?

Those are separate questions.

### Terrain sampling and visual detail

Authoritative terrain remains simulation truth at the resolution represented
by Est.

The rendering layer may derive a continuous terrain field from that state
through deterministic interpolation and may add deterministic
presentation-only detail where appropriate.

Presentation detail must not silently become authoritative terrain state.

The renderer may eventually combine:

- authoritative macro elevation;
- deterministic procedural intermediate detail;
- high-resolution Earth data where provenance permits;
- erosion or geology presentation products;
- local geometric features;
- material and biome presentation inputs.

The ownership boundary remains explicit.

### GPU terrain presentation

The visual renderer will own normal calculation, lighting, material evaluation,
and mesh displacement required to draw the prepared visual surface.

Terrain shading should therefore derive from the actual displaced render
geometry or its mathematically equivalent surface representation.

Est will not depend on a third-party globe renderer's fixed terrain-lighting
model for production visual quality.

### Water

Standing water will not be rendered as one polygon per simulation cell.

Large oceans should be represented as continuous visual surfaces whose
intersection with terrain naturally produces the visible coastline.

The authoritative hydrology model determines physical water state and the
appropriate water-surface conditions.

Presentation determines how those conditions are drawn.

Lakes, rivers, wetlands, snow, ice, groundwater expression, waves, and other
water representations may require separate visual techniques as their
simulation requirements mature.

The simulation-cell tessellation must not become visible merely because water
is stored or solved per cell.

### Production renderer direction

Cesium is retired as Est's production planetary-renderer direction.

Existing Cesium evaluation work remains useful historical evidence and may
remain temporarily available for comparison or reference, but new production
planet-renderer development should not extend the Cesium terrain, imagery, or
cell-polygon architecture.

The first implementation target for the rebuilt browser renderer is Babylon.js
using its modern WebGPU-capable rendering stack.

Babylon is an implementation choice, not a simulation dependency.

Renderer-neutral sampling, world-state ownership, and presentation contracts
must remain separable enough that Babylon can later be replaced if concrete
requirements justify doing so.

### Browser direction

The browser-hosted product direction remains valid.

Replacing Cesium does not imply abandoning the web client.

The rebuilt renderer should remain compatible with Est's long-term model in
which authoritative world state is independent of the device displaying it.

### Multi-scale presentation

ADR 0003 remains accepted.

This decision strengthens it.

A cube-sphere quadtree solves planetary terrain representation and
view-dependent terrain density. It does not imply that one terrain mesh must
represent every feature at every scale.

Regional imagery, procedural materials, buildings, roads, vegetation,
characters, settlements, and other representations may still appear,
disappear, aggregate, or transition according to scale and significance.

The planet renderer is a foundation for those representations, not a
replacement for multi-scale presentation policy.

## Initial Implementation Sequence

### Phase R1: renderer foundation

1. Establish a Babylon-based Est renderer entry point separate from the Cesium
   evaluation.
2. Define renderer-owned cube-face and quadtree patch identities.
3. Define cube-to-sphere mapping independent of authoritative simulation-cell
   geometry.
4. Render one stable spherical planet from six cube faces.
5. Prove orbit, zoom, resize, and camera behavior.

No terrain simulation integration is required for the first proof.

### Phase R2: quadtree terrain geometry

1. Introduce reusable regular terrain patches.
2. Subdivide and merge patches based on view-dependent error or equivalent LOD
   criteria.
3. Prevent cracks between neighboring LOD levels.
4. Add horizon and frustum culling.
5. Keep terrain patch generation independent of simulation-grid topology.

The runtime gate is smooth planetary orbit and zoom without visible cube-face
or patch seams.

### Phase R3: authoritative terrain sampling

1. Connect the renderer to Est's continuous terrain-sampling boundary.
2. Sample authoritative macro terrain without rendering authoritative cells.
3. Verify that continents and major terrain structures correspond to simulation
   state.
4. Add deterministic visual detail only through an explicit presentation
   layer.
5. Preserve reproducibility from world and terrain seeds.

The runtime gate is recognizable continuous terrain without visible simulation
cells.

### Phase R4: materials, lighting, atmosphere, and ocean

1. Compute lighting from the rendered planetary surface.
2. Introduce Est-owned terrain materials.
3. Add a continuous ocean representation driven by authoritative hydrologic
   conditions.
4. Verify that shorelines emerge from terrain and water-surface intersection
   rather than cell boundaries.
5. Add atmosphere only after terrain and water are stable.

The runtime gate is a convincing planet from orbital through regional view
without GIS-style cell artifacts.

### Phase R5: living-world presentation

1. Reconnect population, animals, resources, and other simulation state.
2. Select view-appropriate representations rather than drawing every
   authoritative object identically at every distance.
3. Preserve the existing ownership rule:

       simulation truth -> API -> presentation

4. Do not create presentation-only behavior and infer simulation truth from it.

### Phase R6: Cesium retirement

Once the rebuilt renderer satisfies the required runtime gates:

1. remove Cesium from the production application path;
2. retain only explicitly useful evaluation artifacts;
3. remove obsolete Cesium-specific adapters and dependencies;
4. update development scripts and documentation;
5. archive or delete failed experiments that no longer provide useful evidence.

## Acceptance Gates

The new planetary rendering foundation is not accepted merely because it
builds or passes unit tests.

It must satisfy runtime visual and interaction gates.

At minimum:

- no visible authoritative simulation-cell grid in terrain or coastline;
- no cube-face seams during ordinary orbit and zoom;
- no quadtree cracks or disappearing terrain patches;
- stable geometry while the camera moves;
- consistent terrain normals and lighting;
- continuous ocean presentation;
- shoreline geometry determined by terrain and water surface rather than
  polygonized simulation cells;
- deterministic correspondence with authoritative macro terrain;
- smooth transition from global to regional viewing scales;
- usable browser performance on the primary Apple Silicon development machine;
- authoritative simulation remains runnable and testable without graphics.

Browser validation is mandatory for renderer milestones.

A green unit-test suite or production build does not override a failed visual
runtime gate.

## Consequences

### Positive

- Simulation resolution no longer dictates visual resolution.
- Coarse causal fields can coexist with high-detail rendering.
- The authoritative surface grid remains useful without becoming visible
  geometry.
- Terrain lighting and materials become controllable by Est's renderer.
- Ocean presentation can become continuous.
- Visual LOD can scale independently from simulation LOD.
- Earth Observatory and Living Worlds can share a planetary rendering
  foundation while supplying different presentation inputs.
- Est preserves the ability to replace both the simulation grid and renderer
  independently.

### Costs

- Est must own more real-time planetary rendering machinery.
- Cube-sphere mapping, quadtree LOD, patch continuity, culling, precision, and
  streaming become explicit engineering responsibilities.
- Procedural visual detail requires clear provenance and ownership boundaries
  so presentation is not confused with authoritative state.
- A renderer migration temporarily leaves Cesium evaluation code beside the
  rebuilt renderer.
- Browser runtime quality must be validated continuously rather than inferred
  from tests alone.

These costs are preferable to continued dependence on a rendering architecture
that exposes the wrong spatial representation and constrains Est's intended
visual world.

## Superseded Assumptions

This ADR supersedes the following assumptions:

- authoritative surface cells may directly serve as finished visual terrain;
- hydrology-cell polygons are an acceptable production representation of
  oceans or coastlines;
- one terrain representation must serve simulation and rendering equally;
- Cesium terrain or imagery abstractions are the intended production
  foundation;
- renderer success can be established solely through unit tests or successful
  builds.

It does not supersede:

- ADR 0003 multi-scale presentation;
- ADR 0005 shared planet-surface fields and hydrology ownership;
- authoritative `WorldState`;
- simulation / API / presentation ownership boundaries;
- browser-hosted product direction;
- the requirement that renderer technology remain replaceable.

## Immediate Next Step

Stop modifying the Cesium production path.

Begin Phase R1 with a clean Babylon renderer foundation and prove a stable
cube-sphere planet before reconnecting authoritative terrain.

Do not add hydrology, atmosphere, population, procedural detail, or local
features until the base cube-sphere runtime gate is green.
