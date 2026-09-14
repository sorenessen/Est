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

Recent runtime work exposed a distinction that must now become explicit:

Simulation spatial resolution and render spatial resolution are separate
systems.

The authoritative surface grid is useful for causal simulation, conservation,
neighborhood relationships, durable terrain values, hydrology, ecology, and
aggregation.

It is not the visual planet.

Several Cesium experiments exposed the consequences of treating it as though it
were.

Direct standing-water rendering used one polygon per authoritative surface
cell. This made the simulation tessellation visible as blocky and stair-stepped
coastlines.

The stable Cesium `CustomHeightmapTerrainProvider` could reproduce the
authoritative terrain field as globe geometry, but the rendering path did not
provide the terrain-normal and shading control Est requires.

A custom quantized-mesh provider was then tested to recover explicit vertex
normals. Its unit tests and production build passed, but browser validation
failed with tile-sector artifacts, seams, disappearing or changing geometry,
and camera-dependent instability. The experiment was rolled back.

A second experiment generated Est-controlled raster terrain imagery with
elevation coloring and deterministic hillshade. Its tests and production build
also passed. Browser validation nevertheless failed the visual gate: terrain
still read as large painted areas, relief was inadequate, and the approach did
not solve the underlying representation problem. It was rolled back.

These experiments demonstrate that successful tests and builds are not enough
to validate a planetary renderer.

More importantly, they demonstrate that Est should not continue adapting its
simulation topology into GIS-oriented terrain, imagery, or per-cell polygon
representations.

## Decision

Est will explicitly separate authoritative simulation geography from visual
planet geometry.

The authoritative simulation surface grid remains a domain structure.

The production visual planet will use a separate multi-resolution rendering
structure whose topology, density, and level of detail are chosen for visual
quality and runtime performance.

The architectural flow is:

    authoritative simulation fields
      -> Est-owned continuous sampling / presentation inputs
      -> multi-resolution render surface
      -> GPU geometry, materials, lighting, and effects

The renderer may contain far more vertices and pixels than the authoritative
simulation contains cells.

That is expected.

Simulation resolution answers:

    At what spatial resolution must authoritative world state be modeled?

Render resolution answers:

    At what spatial resolution must the current view be drawn?

They are independent questions.

### Authoritative simulation surface

The Est planet-surface grid continues to provide stable spatial identity and
topology for simulation fields such as:

- durable terrain elevation;
- hydrology stores and fluxes;
- regional climate;
- soil and vegetation;
- ecological density and cohorts;
- spatial aggregation and neighborhood relationships.

The current latitude/longitude tessellation remains an implementation behind
the existing replaceable surface-grid abstraction.

It is not promoted to visual geometry.

A future equal-area, hierarchical, geodesic, or other planetary grid may
replace it without requiring the render surface to use the same topology.

### Production render surface

The production planetary renderer will use a cube-sphere divided into
hierarchical quadtree patches.

Each of the six cube faces can recursively subdivide according to view-dependent
level of detail.

Terrain patches will use reusable regular mesh topology. Their vertices will be
projected from cube space onto the planetary sphere and displaced using
Est-owned terrain sampling.

The render hierarchy belongs to presentation.

Its patch identities are not `SurfaceCellId` values.

Its subdivisions are not authoritative world state.

Its level of detail may change continuously as the camera moves without
changing simulation state.

### Terrain sampling

Authoritative terrain remains simulation truth at the resolution represented by
Est.

Presentation may derive a continuous terrain field from that state through
deterministic interpolation.

The renderer may later combine authoritative macro terrain with additional
presentation inputs such as:

- deterministic procedural visual detail;
- higher-resolution Earth elevation products;
- geology or erosion presentation products;
- local spatial geometry;
- biome and material information.

Presentation-only detail must not silently become authoritative terrain state.

### Terrain rendering

Terrain normals, materials, lighting, and displacement required for the visual
planet will be controlled by Est's production renderer.

Lighting should derive from the actual rendered surface or a mathematically
equivalent representation.

Est will not depend on a third-party globe renderer's fixed terrain-lighting
pipeline for production visual quality.

### Water

Standing water will not be rendered as one polygon per simulation cell.

Large oceans should be continuous visual surfaces.

The authoritative simulation determines physical water state and water-surface
conditions.

Presentation determines how those conditions are drawn.

Visible coastlines should arise from the intersection of terrain with the
appropriate water surface rather than from the boundaries of hydrology storage
cells.

Lakes, rivers, wetlands, snow, ice, waves, and other water representations may
require additional visual techniques as their simulation requirements mature.

The simulation tessellation must not become visible merely because water is
stored or solved per cell.

### Renderer direction

Cesium is retired as Est's production planetary-renderer direction.

The existing Cesium work remains useful evaluation evidence and may remain
temporarily available for comparison, diagnostics, or historical reference.

New production renderer development should not extend the Cesium terrain,
imagery, quantized-mesh, or simulation-cell polygon paths.

The first implementation target for the replacement browser renderer is
Babylon.js.

Babylon is a presentation implementation choice, not a simulation dependency.

Renderer-neutral sampling, presentation policy, and simulation ownership must
remain separable so that Babylon itself remains replaceable if future evidence
justifies another renderer.

### Browser direction

The browser-hosted product direction remains valid.

Replacing Cesium does not imply abandoning the browser client.

Authoritative world state remains independent of the machine rendering it.

### Multi-scale presentation

ADR 0003 remains accepted.

This decision strengthens it.

A cube-sphere quadtree provides the planetary terrain foundation. It does not
require one representation to serve every feature at every scale.

Regional imagery, materials, buildings, roads, vegetation, settlements,
characters, and other representations may still appear, disappear, aggregate,
stream, or transition according to viewing scale and significance.

## Implementation Sequence

### R1: Babylon planetary foundation

1. Add a Babylon renderer entry point separate from the Cesium evaluation.
2. Define six renderer-owned cube faces.
3. Define deterministic cube-to-sphere mapping.
4. Render a stable sphere from six independently addressable faces.
5. Validate orbit, zoom, resize, and ordinary camera movement.
6. Confirm there are no visible cube-face gaps or orientation errors.

Do not integrate terrain simulation yet.

### R2: quadtree terrain patches

1. Introduce reusable regular terrain patches.
2. Subdivide and merge patches according to view-dependent LOD.
3. Preserve continuity between neighboring LOD levels.
4. Add frustum and horizon culling.
5. Keep patch topology independent of the authoritative simulation grid.

Runtime gate:

- smooth planetary orbit and zoom;
- stable patch geometry;
- no disappearing sectors;
- no visible cracks or cube-face seams.

### R3: authoritative terrain sampling

1. Connect the renderer to Est's terrain-sampling boundary.
2. Sample authoritative macro terrain into render vertices.
3. Verify continental and major relief correspondence.
4. Preserve deterministic output.
5. Add presentation-only intermediate detail only behind an explicit ownership
   boundary.

Runtime gate:

The planet must display continuous recognizable terrain without exposing
authoritative surface cells.

### R4: materials, lighting, ocean, atmosphere

1. Compute normals from rendered terrain.
2. Add Est-controlled terrain materials.
3. Add physically coherent directional lighting.
4. Add a continuous ocean representation driven by hydrologic state.
5. Verify terrain/water intersection produces continuous shorelines.
6. Add atmosphere only after terrain and water are stable.

Runtime gate:

The globe must read as one coherent planet rather than GIS tiles, classified
cells, or simulation polygons.

### R5: reconnect living-world presentation

1. Reconnect population, animals, resources, and other authoritative state.
2. Use view-appropriate representations rather than drawing every object the
   same way at every distance.
3. Preserve:

       simulation truth -> API -> presentation

4. Do not invent authoritative behavior in the renderer.

### R6: Cesium retirement cleanup

After the replacement renderer satisfies its runtime gates:

1. remove Cesium from the production application path;
2. retain only evaluation artifacts still worth preserving;
3. remove obsolete Cesium-specific adapters and dependencies;
4. update development scripts and documentation;
5. archive or delete failed experiments that no longer provide useful evidence.

## Runtime Acceptance Gates

Renderer milestones require runtime visual validation.

A successful build or unit-test suite is necessary but not sufficient.

At minimum the rebuilt planetary foundation must demonstrate:

- no visible authoritative simulation-cell grid in terrain;
- no cell-shaped ocean coastline;
- no cube-face seams during ordinary viewing;
- no quadtree cracks;
- no disappearing or camera-dependent terrain sectors;
- stable geometry while the camera moves;
- consistent terrain normals and lighting;
- continuous ocean presentation;
- shoreline determined by terrain/water intersection;
- deterministic correspondence with authoritative macro terrain;
- smooth global-to-regional viewing;
- usable browser performance on the primary Apple Silicon development machine;
- authoritative simulation remains runnable and testable without graphics.

A failed runtime visual gate overrides a green build.

## Consequences

### Positive

- Simulation resolution no longer dictates rendering resolution.
- Coarse causal fields can coexist with high-detail presentation.
- The simulation tessellation no longer needs to become visible geometry.
- Terrain lighting and materials become directly controllable.
- Ocean presentation can be continuous.
- Visual LOD can evolve independently from simulation LOD.
- Earth Observatory and Living Worlds can share the same planetary rendering
  foundation while providing different data.
- The simulation grid and renderer remain independently replaceable.

### Costs

- Est now owns more real-time planetary rendering machinery.
- Cube-sphere mapping, quadtree LOD, patch continuity, culling, precision, and
  eventually streaming become explicit engineering responsibilities.
- Presentation-only procedural detail requires disciplined ownership so it is
  not confused with simulation truth.
- Cesium evaluation code temporarily coexists with the replacement renderer.
- Visual quality must be validated continuously in the browser.

These costs are preferable to continuing to force simulation geography through
a rendering architecture that exposes the wrong representation.

## Superseded Assumptions

This ADR supersedes the assumptions that:

- authoritative surface cells may directly serve as finished visual terrain;
- hydrology-cell polygons are acceptable production oceans or coastlines;
- simulation and rendering should share one spatial resolution;
- Cesium terrain and imagery abstractions are the intended production
  foundation;
- renderer success can be established solely through automated tests or builds.

It does not supersede:

- ADR 0003 multi-scale presentation;
- ADR 0005 shared planet-surface fields and hydrology ownership;
- authoritative `WorldState`;
- simulation / API / presentation ownership boundaries;
- the browser-hosted direction;
- renderer replaceability.

## Immediate Next Step

Stop modifying the Cesium production path.

Begin R1 with a separate Babylon renderer.

The first proof is intentionally small:

Render one mathematically correct six-face cube-sphere, orbit it, zoom it, and
prove that its geometry is stable.

Do not add terrain displacement, hydrology, atmosphere, population, procedural
detail, or local geometry until that foundation is runtime-green.
