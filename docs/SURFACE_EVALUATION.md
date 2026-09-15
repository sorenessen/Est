# Regional Surface Evaluation

## Current status — 2026-09-15

This document is preserved as engineering evidence.

Cesium is not the current production planetary renderer. The active First Light
globe uses one immutable Babylon icosphere with authoritative terrain baked
into fixed spherical geometry.

That change does not invalidate the evidence collected here. The regional
surface, TMS, multi-scale presentation, local-building, Longmire,
camera-significance, source-normalization, and renderer-neutral preparation
work may inform future Est enhancements.

Do not interpret preservation of these experiments as a commitment to restore
Cesium as the production renderer. Do not interpret the current Babylon sphere
as a reason to discard useful Cesium evidence.

## Purpose

Validate a real categorical land-cover pipeline for Est without coupling
authoritative simulation state to a particular source dataset or renderer.
This is a rendering and data-ownership evaluation, not a spatial simulation
model or final visual design.

## Source

- Product: USGS Annual NLCD Land Cover 2025
- Collection: 1.2
- Version: 1.2, June 2026
- MRLC order: `5a3e2c72-778a-4f3b-9c57-be4e7b1fef82`
- Requested bounds: longitude -123.3 to -121.3, latitude 46.6 to 47.4
- Source raster: 5705 x 4230, one band, uint8, 30 meters
- CRS: Albers Equal Area, WGS84
- NoData: 250
- Expected source classes: 11, 12, 21, 22, 23, 24, 31, 41, 42, 43, 52, 71, 81, 82, 90, 95

The source archive SHA256 is:

`bf7f3ba86b462a8a3b89116286be1c9f1d60ecbca1eb1119cddff3d27f71b727`

Raw source data and full generated rasters are intentionally excluded
from Git. The local source manifest and generated manifest preserve
additional metadata and provenance.

## Conversion

The canonical category definition is
`src/Est.Web/src/surface/surface-categories.json`.

Est categories are Unknown, OpenWater, Developed, Barren, Forest,
Shrubland, Grassland, Agriculture, Wetland, and SnowIce. Source-specific
NLCD class mappings are kept separate from renderer-specific treatment.

The converter uses NumPy and Rasterio with pinned dependencies in
`scripts/requirements-geospatial.txt`. It preserves categorical values
through nearest-neighbor resampling and does not infer land cover from
satellite RGB or elevation.

From the repository root, after creating and activating a Python virtual
environment and installing the pinned requirements:

```sh
python scripts/convert-nlcd.py \
  data/source/nlcd/extracted/Annual_NLCD_LndCov_2025_CU_C1V2_5a3e2c72-778a-4f3b-9c57-be4e7b1fef82.tiff
```

The default output directory is `data/generated/nlcd/`. The converter
produces `surface-categories.tif`, `surface-preview-native.png`,
`surface-preview-geographic.png`, and `surface-manifest.json`.

The geographic preview is 1800 x 1053, EPSG:4326, with bounds:

- West: -123.61633988971845
- South: 46.23644679019992
- East: -121.00788955795646
- North: 47.76234468508979

The browser evaluation uses a small copy of the geographic PNG and a
reduced manifest under `src/Est.Web/public/evaluation/nlcd-2025/`.
The full generated manifest remains local.

## Browser validation

The Cesium evaluation is `src/Est.Web/cesium.html`. Its Est Surface Study
mode loads the geographic preview as a single-tile imagery layer over
Cesium World Terrain. The other three modes remain available for
comparison: Baseline, Terrain Study, and USGS Land Cover.

On September 8, 2026, browser inspection confirmed coherent geographic
alignment around Puget Sound, Tacoma/Olympia, and Mount Rainier. Close
inspection showed the surface following terrain relief, with coherent
coastline and snow/ice placement. The web production build passed.

The current preview is intentionally diagnostic. At close range it becomes
blocky, and its rectangular coverage boundary is visible. The dark-blue
surface outside coverage is not an Est land-cover classification. These
limitations should not be confused with source-data misalignment.

## Next evaluation

- Preserve the working baseline and source-to-category conversion.
- Evaluate full-resolution regional delivery and level of detail.
- Improve coverage fallback and regional transitions.
- Develop natural visual treatment from Est-owned categories.
- Evaluate water, forests, developed areas, mountains, snow, close-range
  quality, and performance.
- Keep renderer selection open and avoid premature geospatial infrastructure.

No regional spatial state has been added to Est.Simulation. The current
planetary environment remains aggregate authoritative state.

## Full-resolution TMS evaluation checkpoint

On September 9, 2026, the regional surface study gained a fifth Cesium
mode, Est Surface TMS. It preserves the original single-image study as a
comparison control and serves a static geographic TMS pyramid generated
from Est-owned categorical raster data.

The pyramid uses EPSG:4326, 256-pixel tiles, south-origin TMS coordinates,
and levels 7 through 11. It contains 758 PNG tiles. Nearest-neighbor
reprojection preserves categorical values, and Unknown remains transparent.
The generator, validator, and geometry tests are under scripts/surface/
and tests/python/. The published evaluation tiles are under
src/Est.Web/public/evaluation/nlcd-2025/surface-tms/.

Validation completed before the checkpoint: 10 Python geometry tests
passed, the published pyramid validator checked all 758 tiles and all
10 canonical colors, and the web production build passed. The existing
large-chunk build warning remains non-blocking.

Browser inspection confirmed that the new mode loads and that close-range
lake and shoreline detail is substantially less pixelated than the original
preview. This is a promising visual result, not final acceptance. The
dataset's approximately 30-meter source resolution remains the detail
ceiling.

Subsequent browser evaluation confirmed:
- Renderer-only inspection daylight works without changing authoritative
  Est simulation state.
- Surface modes can be switched without moving the camera.
- No obvious tile-to-tile seams were observed in populated regional
  coverage.
- Unknown pixels remain transparent as designed.
- The finite regional coverage boundary is clearly visible when the
  surrounding globe has no Est-owned surface fallback. This is an
  evaluation-data limitation, not a TMS alignment failure. Do not hide it
  by inventing classifications or stretching regional data beyond its
  source coverage. A future surface stack should allow detailed regional
  semantics to fall back to coarser Est-owned planetary semantics.

Remaining evaluation work:
- Evaluate natural category materials and close-range visual quality,
  performance, and scale transitions.
- Harden and test publication rollback behavior before treating the
  generator as a general-purpose production pipeline.

No tile server, new backend service, or regional simulation state was
introduced. The original Baseline and other comparison modes remain
available.

## Natural material presentation checkpoint

On September 10, 2026, the regional TMS pipeline gained an Est-owned
presentation layer between semantic surface categories and generated
imagery. Semantic identity remains in `surface-categories.json`;
`surface-presentation.json` defines presentation-only material parameters,
and `scripts/surface/presentation.py` interprets them. Cesium continues to
consume generated imagery and does not own Est surface semantics.

Presentation version 2, `natural-material-study`, adds deterministic
geographic tonal variation to the base color of each known surface category.
The variation is calculated from longitude and latitude rather than tile-local
random state, so a geographic coordinate receives the same treatment
independently of the tile or render call that contains it. Unknown remains
fully transparent. Category IDs and boundaries are not blended or modified.

The published evaluation pyramid remains 758 tiles across levels 7 through
11. Validation now permits the many RGB values intentionally produced by
material variation while enforcing binary alpha, zero RGB for transparent
pixels, and agreement between the published manifest and the current Est
presentation definition.

Browser A/B inspection against the earlier flat Est Surface Study confirmed
that the presentation seam works without Cesium-specific material logic.
The difference is visible, especially across broad forest, barren, developed,
and snow/ice regions, but subtle tonal variation alone does not remove the
classified-raster appearance. Terrain relief contributes much of the useful
small-scale visual structure, while the approximately 30-meter source
classification remains visible at close range.

This result is useful even though it is not a final art direction. It
establishes that Est surface categories can describe presentation materials
rather than only fixed colors while preserving semantic ownership and
renderer independence. Further work should build category-specific material
structure on this seam rather than spending substantial effort tuning a
single generic noise treatment. Any added structure must remain
presentation-only and must not imply unsupported simulation facts.

Validation for this checkpoint includes 20 passing Python tests: 10
presentation/material tests and the existing 10 geographic TMS geometry
tests. The presentation tests cover deterministic rendering, geographic
coordinate stability across render calls, Unknown transparency, alpha
preservation, category-boundary preservation, material variation, exact
zero-variation rendering, semantic category coverage, and input-shape
validation.

## Category-specific material presentation checkpoint

On September 10, 2026, presentation version 3,
`category-material-study`, extended each surface material from one generic
variation amplitude to independent broad-, medium-, and fine-scale geographic
variation. The underlying noise components remain deterministic in geographic
space, while each category controls how strongly it responds at each scale.
Semantic category IDs, category boundaries, Unknown transparency, TMS geometry,
and Cesium ownership remain unchanged.

The category profiles produce measurably different spatial responses. A focused
frequency-response test confirmed that fine-scale treatment creates
substantially more local variation than broad-scale treatment over the same
geographic region. The full Python suite now contains 21 passing tests,
including this material-profile behavior.

The generated and published version 3 pyramid remains 758 tiles across levels
7-11 with the same semantic coverage and 18,081,876 opaque pixels. Browser A/B
inspection confirmed that category-specific spatial character is visible and
that the TMS remains sharper and more useful than the blurred single-image
study. No obvious tile-boundary artifact was observed.

The visual experiment also identified the dominant remaining limitation.
Category-specific tonal structure does not substantially change the fact that
the regional surface reads as a classified raster draped over terrain. At close
range, discrete source-cell and category-boundary geometry dominates perception
more than the internal material variation. Increasing generic procedural
variation would decorate those classified regions rather than address that
limitation and could make the result look artificially noisy.

Preserve version 3 as an architectural capability, but do not spend the next
iteration tuning broad, medium, and fine amplitudes. The next presentation
experiment should investigate how discrete semantic classifications can drive a
more continuous-looking physical surface without altering authoritative
category identity, inventing unsupported classifications, or moving Est
surface semantics into Cesium. Blurring categorical truth is not an acceptable
substitute for a presentation model.

## Source-class visual structure checkpoint

On September 11, 2026, the regional surface evaluation tested whether the
classified-raster appearance was primarily caused by Est collapsing the
original NLCD classes into its smaller semantic vocabulary.

Direct geometry comparison confirmed that the original Annual NLCD raster and
`surface-categories.tif` have identical dimensions, CRS, 30-meter resolution,
bounds, and affine transform. Est's semantic conversion therefore does not
introduce or coarsen the source-cell geometry. It changes categorical identity
only.

An adjacency analysis measured 4,807,796 class transitions between known
neighboring NLCD cells. Est's semantic categories retain 2,868,083 of those
transitions and intentionally collapse 1,939,713, or 40.35 percent. The largest
collapsed groups are distinctions within developed intensity and forest type,
with smaller losses within wetlands and agriculture.

A disposable geographic diagnostic rendered all 16 known NLCD source classes
with distinct presentation colors. Restoring those source distinctions visibly
adds meaningful regional structure, especially within developed areas, forest,
agriculture, and wetlands. It does not, however, resolve the fundamental visual
limitation. The result still reads as categorical land-cover imagery, and the
native 30-meter classified-cell geometry remains visually dominant.

This changes the next presentation direction. Do not spend the next iteration
smoothing Est semantic boundaries or further tuning procedural noise merely to
hide categorical geometry. NLCD remains valuable authoritative input for Est
surface semantics and may also contribute non-authoritative information to
presentation, but direct category colorization should not be treated as the
intended final visual-surface strategy.

Preserve the current semantic pipeline and the version 3 presentation/TMS work
as proven capabilities and evaluation controls. The next architectural
investigation should define a visual-surface input and composition seam that is
explicitly separate from authoritative simulation semantics. Presentation may
legitimately use more source information than the simulation vocabulary and may
combine multiple visual inputs, provided those inputs never become simulation
truth and Cesium remains a presentation consumer rather than the owner of Est
surface semantics.

The guiding distinction is now: simulation semantics answer what is at a
location; presentation determines how that location should look. Those concerns
are related but are not required to use the same data product.

## Continuous visual surface checkpoint

On September 11, 2026, the regional surface evaluation added a continuous
visual-surface path using Sentinel-2 RGB imagery as a presentation-only input.
The semantic category raster continues to determine Est surface identity and
known-versus-Unknown coverage. It does not colorize the imagery, blend category
materials into it, or otherwise define the visual appearance of known pixels.

The experiment deliberately excluded slope response, category tinting, and
procedural material variation. Inside the known semantic footprint, the
prepared RGB imagery is used directly as the visual basis; Unknown remains
transparent. This isolates the architectural question of whether a continuous
visual source can replace categorical land-cover geometry as the dominant
rendered surface while preserving Est semantic ownership.

The published continuous-surface pyramid remains EPSG:4326 geographic TMS with
256-pixel tiles, south-origin Y coordinates, levels 7 through 11, and 758
tiles. Its metadata records the visual RGB input separately from the semantic
source. Cesium consumes the resulting RGBA tiles and does not interpret NLCD
classes or Est semantic categories.

Browser A/B evaluation validated the architecture hypothesis. The continuous
surface no longer reads primarily as discrete NLCD polygons. Snowfields, rock,
forest, ridges, drainage, roads, shoreline, developed areas, and industrial
structure remain visually continuous across semantic-category boundaries.
Semantic classification is therefore better treated as a description of the
world than as the direct paint used to render the world.

The experiment also exposed a separate resolution problem. Sentinel-2 RGB is
approximately 10-meter source imagery, but the current imagery-preparation
pipeline resamples it onto the approximately 30-meter semantic working grid
before TMS generation. Close-range buildings consequently lose useful source
detail and become soft or paint-like even though the continuous visual model
itself is working.

Do not address that limitation by increasing TMS zoom over the existing
30-meter aligned RGB product. That would only magnify already-discarded detail.
The next visual-surface iteration should decouple semantic and presentation
resolution: keep authoritative semantics on their appropriate grid, preserve
imagery near its useful source resolution, and sample both independently during
visual composition. Semantic categories should use categorical nearest-neighbor
sampling; continuous imagery should use an appropriate continuous resampler.
The visual TMS level ceiling should follow actual visual-source resolution
rather than the semantic raster resolution.

This checkpoint establishes the intended ownership direction without adding a
tile server, backend service, regional simulation state, or renderer-owned
surface semantics. The categorical and material-based modes remain valuable
evaluation controls and fallback capabilities.

## Visual-resolution decoupling checkpoint

On September 11-12, 2026, the continuous visual-surface pipeline was
decoupled from the semantic raster's approximately 30-meter working
resolution. Sentinel-2 RGB is now composed on a separate 10-meter visual grid
covering the same projected regional bounds, while Est surface semantics
remain on their existing 30-meter grid.

The 10-meter visual grid is 17,115 by 12,690 pixels. Semantic coverage is
projected onto it with nearest-neighbor sampling so categorical identity is
not blurred or reinterpreted. Continuous RGB imagery is sampled bilinearly.
This establishes that semantic/reference resolution and presentation
resolution are independent concerns and need not share one raster grid.

The imagery-preparation pipeline also gained a bounded repair for small,
fully enclosed presentation-coverage gaps. The evaluation region initially
contained 349 missing required visual pixels across 131 enclosed components;
the largest component contained 24 pixels. The repair accepts components no
larger than 32 pixels, limits total repair to 512 pixels, rejects components
that touch the visual-grid edge, requires a covered external boundary, and
fills inward deterministically from covered neighboring RGB values. All 349
required pixels were repaired. Uncovered pixels outside the required semantic
surface remain uncovered.

TMS generation was independently decoupled from semantic resolution. Semantic
source bounds continue to define the publication extent, but when a visual RGB
source is present its effective geographic resolution determines the maximum
detail level. The approximately 30-meter semantic source resolves through
level 11; the 10-meter visual source resolves through level 13.

Visual coverage is validated once as a pyramid preflight invariant rather than
by repeatedly reading and reprojecting the full visual dataset mask for every
tile. This matters at the 10-meter working size of more than 217 million
pixels and keeps per-tile rendering focused on RGB reprojection.

The resulting local experimental pyramid contains 11,188 PNG tiles across
levels 7 through 13 and occupies approximately 435 MB. Its tile counts and
manifest were independently verified. The 10-meter aligned RGB artifact is
approximately 517 MB. These are evaluation artifacts, not a decision to store
large high-resolution pyramids in normal Git history. The existing smaller
level-11 continuous-surface artifact remains the committed evaluation
checkpoint.

The full Python suite contains 41 passing tests at this checkpoint, and
`git diff --check` passes.

Architecturally, this experiment succeeded. Est can preserve semantic truth on
one grid, presentation information on another, derive rendering detail from
the appropriate source, validate cross-grid coverage, and continue to keep
Cesium outside semantic interpretation.

## Close-range raster conclusion

Browser A/B evaluation of the 10-meter result established a different limit:
preserving all available Sentinel-2 detail does not make that imagery suitable
as Est's close-range urban representation.

At regional scale the continuous visual surface remains substantially more
natural than direct categorical land-cover rendering. At close urban scale,
however, individual buildings and site features become broad or blurry color
shapes. The Washington State Capitol area provided the clearest comparison:
the baseline can resolve recognizable buildings, roads, parking areas, paths,
trees, roof structure, and surrounding site organization that the 10-meter
Sentinel surface cannot.

This is no longer a semantic-grid or TMS-level problem. It is an information
limit in the visual source at the requested viewing scale.

Do not continue this line of investigation by generating level 14 or 15 tiles
from the same imagery, sharpening or interpolating the source, adding more
generic procedural noise, tinting imagery from semantic categories, tuning
scalar slope darkening, smoothing semantic boundaries, or acquiring additional
Sentinel dates in an attempt to manufacture street-level detail.

The work remains useful. Continuous imagery is a viable regional presentation
layer, and the independent-resolution composition architecture should be
preserved. The failed hypothesis is narrower: one raster surface should not be
expected to provide Est's useful representation at every camera distance.

## Multi-scale presentation pivot

The next presentation investigation will evaluate different representations
at different spatial and camera scales while preserving one authoritative Est
world state.

The provisional model is:

- planetary scale: terrain, atmosphere, and coarse/global surface appearance
- regional scale: terrain, imagery/materials, land-cover-informed appearance,
  and broad vegetation or water treatment
- local/city scale: buildings, roads, bridges, vegetation, water features,
  and other justified geometry
- street/immediate scale: higher-detail geometry, materials, and local
  presentation detail where evidence and product requirements justify them

These are evaluation categories, not committed LOD boundaries or a final
rendering architecture. The renderer may change how a feature is represented
as the camera approaches it, but representation must not redefine simulation
truth.

The guiding ownership rule remains:

> Simulation determines what exists and what state it is in. Presentation
> determines how that state should be represented at the current scale.

The next experiment is intentionally narrow: the Washington State Capitol
campus in Olympia. Keep World Terrain and baseline imagery, acquire real
building footprints for a small surrounding area, convert them through an
Est-owned renderer-neutral preparation boundary, and let the Cesium adapter
render the resulting building geometry.

The first question is not whether Est can build a complete city system. It is
whether a recognizable real building can transition from being primarily part
of regional imagery at altitude to readable geometry during close descent
without an unacceptable visual discontinuity.

Do not begin the spike with roads, vegetation, props, facade generation, or a
general planetary geometry system. Those become justified follow-ups only if
the building experiment proves the central multi-scale hypothesis.

The regional surface work is therefore not abandoned. It has established the
presentation/data boundaries needed for a larger system and remains useful at
the scales where its source information is appropriate. The next evaluation
moves to local geometry because the browser evidence shows that additional
raster refinement is now solving the wrong problem.

## Local-geometry checkpoint

On September 12, 2026, the Washington State Capitol local-geometry experiment
validated the first multi-scale presentation hypothesis.

A small OpenStreetMap extract around the Capitol was converted through an
Est-owned preparation boundary into a renderer-neutral GeoJSON
FeatureCollection. Source-specific OSM building semantics are normalized
before reaching Cesium. The initial artifact contains 871 building features.
Each feature has an Est-defined identifier and normalized height in metres;
the Cesium adapter consumes those prepared properties rather than interpreting
`building`, `building:levels`, or other OSM tags.

The first renderer intentionally does very little. It places the prepared
building footprints over Cesium World Terrain and baseline World Imagery and
extrudes them to their normalized heights. The Washington State Capitol itself
is represented from its real mapped footprint with an 18-metre height derived
from six mapped levels. No dome, facade, roof system, procedural architecture,
road geometry, vegetation, props, or handcrafted Capitol detail was added.

Browser evaluation strongly validated the representation change. From broader
city and campus views, mapped structures become clearly legible spatial
objects aligned with the underlying imagery. During close descent they remain
geometry with position, footprint, and volume rather than becoming enlarged
raster pixels. The experiment therefore changes the close-range problem from
"find enough raster detail to look like a building" to "provide the geometric
and material information needed to represent the building."

Visual blandness is explicitly not a failure criterion for this checkpoint.
Uniform light-colored extrusions, flat roofs, approximate heights, and missing
architectural detail are expected limitations of the deliberately minimal
proof. The important result is that local structures are now independently
addressable presentation objects that can be enriched later without requiring
the regional surface representation to carry street-level information.

This result also preserves the value of the earlier terrain and regional
surface work. Est-owned terrain, procedural/material surface treatment,
satellite or aerial imagery, or combinations of those sources may continue to
provide planetary and regional appearance. Local mapped geometry can resolve
structures, roads, vegetation, and other features as closer viewing scales
justify them. No single surface source is required to solve every scale.

The emerging capability stack is therefore:

1. terrain and elevation establish the land form;
2. regional surface presentation establishes broad appearance;
3. mapped or generated spatial features establish local structure;
4. local geometry provides persistent addressable objects;
5. authoritative Est state can eventually drive relevant object state and
   behavior without transferring simulation ownership to the renderer;
6. presentation can progressively enrich those objects with materials,
   architectural detail, effects, and other scale-appropriate representations.

This checkpoint does not establish final LOD distances, streaming strategy,
planetary geometry storage, a general feature schema, or Cesium as the
permanent local renderer. Those remain evidence-driven follow-up decisions.

The next local-scene work should continue to prioritize capability over
cosmetic polish. Uniform building materials are sufficient while Est proves
which additional spatial structures and state-bearing boundaries are required.

## Longmire cross-environment local-geometry checkpoint

On September 12, 2026, the local-geometry evaluation moved from the Washington
State Capitol urban/civic environment to Longmire in Mount Rainier National
Park.

The purpose was not to improve the appearance of the neutral building
extrusions. It was to test whether the same renderer-neutral local-scene
boundary remained useful in a terrain-rich environment where regional
landscape presentation is visually dominant and mapped structures are sparse.

The Longmire evaluation reused the existing Est local-scene preparation model:
OpenStreetMap building ways are normalized before reaching Cesium, building
height is represented through Est-defined `heightMeters`, and Cesium consumes
the prepared geographic geometry rather than interpreting raw OSM building
semantics.

Browser inspection provided strong positive evidence across several camera
conditions. From broad mountainous views, the structures remain spatially
coherent within the valley rather than behaving like raster detail. During
oblique descent they remain terrain-relative three-dimensional objects against
steep surrounding relief. At close range their footprint and volume remain
legible. Top-down comparison with the underlying imagery also showed strong
geographic agreement between prepared geometry and visible building locations.

The result is significant because Olympia and Longmire exercise the same
presentation boundary in materially different environments. Olympia
demonstrated dense urban/civic structure. Longmire demonstrates sparse local
structure embedded in a terrain-dominant mountain landscape.

This validates the composition model:

`terrain -> regional surface/imagery -> mapped spatial features -> local geometry`

The layers have distinct responsibilities. Terrain describes land form.
Regional surface treatment or imagery provides broad visual appearance.
Prepared local geometry provides persistent, independently addressable
structures. None of those presentation representations becomes authoritative
simulation state merely by being rendered.

The comparison also clarifies the role of imagery. High-quality imagery can be
extremely useful as regional or local appearance and as a visual alignment
reference without being required to carry structural responsibility for
buildings. A building can visually coincide with imagery while remaining a
separate Est-prepared geometric object that can later receive materials,
richer architectural representation, effects, or state derived from the
authoritative world.

The neutral extrusions remain intentionally crude. Approximate heights, simple
roof forms, uniform materials, missing facade detail, and incomplete source
metadata are not failures of this checkpoint. The experiment validates spatial
composition and ownership boundaries, not final presentation quality.

Do not infer from this checkpoint that Est has solved automatic LOD selection,
geometry streaming, planetary local-feature storage, detailed building
generation, material generation, vegetation geometry, road geometry, or
simulation-driven building behavior. Those remain evidence-driven follow-up
work.

## View-dependent representation-selection checkpoint

On September 12, 2026, the multi-scale presentation evaluation moved from
proving that local geometry works to investigating when that geometry should
participate in the current representation.

The experiment deliberately separates presentation policy from Cesium. An
Est-owned TypeScript policy accepts renderer-neutral view context and returns
presentation state. The initial policy uses camera height with experimental
2,000-metre entry and 3,000-metre exit thresholds. Hysteresis proved useful for
preventing representation flicker, but these values are evaluation parameters,
not committed LOD boundaries.

The first runtime experiment also exposed an update-cadence problem. Evaluating
the policy from Cesium's sparse camera-changed event produced visibly different
transition timing during otherwise comparable camera movement. Evaluating
against sufficiently current rendered view state removed that artifact. This
does not establish that production policy must execute every frame. It
establishes that renderer event cadence must not materially alter Est's
semantic representation decision.

Camera height alone is also insufficient. A camera can remain at approximately
the same altitude while looking directly at a local scene, looking away from
it, or occupying materially different positions relative to its features.
Feature-relative distance and current-view relevance therefore carry
information that altitude does not.

Several candidate screen-space measurements were tested before accepting any
architectural signal. Projecting one bounding sphere around the complete
Longmire scene produced unstable and exaggerated values near or inside the
sphere. In controlled close views, the sphere-derived significance could exceed
100 percent while failing to describe the actual visible building contribution.
A second experiment projected all local-scene footprint vertices into one
clipped viewport rectangle. That also failed in the close-range regime and
treated the settlement too much like one spatial object.

The next experiment changed the measurement unit from the complete scene to
individual prepared features. Each building is evaluated independently from
its footprint geometry. Initial feature measurements also failed because roof
positions were accidentally constructed from building height as absolute
ellipsoid height while the rendered Cesium entities use heights relative to
terrain. At Longmire this placed the diagnostic geometry far below the rendered
structures.

The corrected experiment samples each building footprint against the same World
Terrain provider used by the viewer. Measurement geometry then uses the sampled
terrain elevation for base positions and terrain elevation plus normalized
`heightMeters` for roof positions. This makes the diagnostic geometry occupy
the same vertical world-space regime as the rendered terrain-relative
structures.

Controlled browser observations on the main display then produced coherent
feature-aware results:

- looking away at approximately 1,033 metres camera height and 309 metres
  feature-relative distance: 0 of 59 features visible and 0.0 percent aggregate
  feature-box coverage
- close and centered at approximately 1,033 metres camera height and 502 metres
  feature-relative distance: 57 of 59 features visible and 5.1 percent
  aggregate feature-box coverage
- farther and centered at approximately 2,073 metres camera height and 3,465
  metres feature-relative distance: 59 of 59 features visible and 0.2 percent
  aggregate feature-box coverage

These observations are important for two reasons. First, the feature-aware
screen contribution changes with what is actually important in the current
view: it falls to zero when looking away, becomes materially larger during
close inspection, and becomes small when the same local scene is viewed from
farther away. Second, visible feature count alone is not a significance metric.
More Longmire buildings fit into the farther view even though their aggregate
screen contribution is much smaller.

The result supports a presentation architecture in which the renderer adapter
derives current view facts from the camera and available spatial
representations, while Est-owned presentation policy decides which
representations should participate. The policy should consume renderer-neutral
facts rather than Cesium objects or Cesium-specific visibility concepts.

Do not promote aggregate feature-box coverage itself to production policy yet.
The current diagnostic sums clipped per-feature screen rectangles, so
overlapping rectangles can be counted more than once. It is also not an
occlusion solution and does not yet establish how terrain, structures, or other
features should hide one another for significance purposes.

The validated result is narrower and more useful: local-representation
selection should be view-dependent and feature-aware, and Est can own that
decision independently of the renderer. Final significance metrics, thresholds,
update cadence, transition behavior, feature partitioning, residency, and
streaming remain evidence-driven follow-up work.

### Cross-environment screen-significance refinement

A follow-up experiment refined the feature-aware screen-contribution diagnostic
before allowing it to influence presentation policy.

The earlier aggregate feature-box coverage sums the area of every clipped
per-feature screen rectangle. That is useful diagnostically, but overlapping
rectangles are counted repeatedly. A renderer-independent rectangle-union
calculation was therefore added so the same projected feature envelopes can
also report the fraction of the viewport covered by their geometric union.

Controlled Longmire observations produced:

- close and partially framed at approximately 972 metres camera height and
  82 metres feature-relative distance: 33 of 59 features visible, 4.0 percent
  aggregate feature-box coverage, and 3.2 percent union coverage
- looking away at approximately 972 metres camera height and 210 metres
  feature-relative distance: 0 of 59 features visible and 0.0 percent aggregate
  and union coverage
- farther and centered at approximately 1,999 metres camera height and
  4,038 metres feature-relative distance: 59 of 59 features visible and
  0.1 percent aggregate and union coverage
- close and centered at approximately 982 metres camera height and 200 metres
  feature-relative distance: 57 of 59 features visible, 6.1 percent aggregate
  coverage, and 5.2 percent union coverage

The same diagnostic was then evaluated against the existing Olympia Capitol
local-scene asset, which contains 871 prepared building features and provides a
materially denser environment:

- farther and centered at approximately 1,159 metres camera height and
  4,226 metres feature-relative distance: 871 of 871 features visible,
  1.8 percent aggregate coverage, and 1.2 percent union coverage
- close and looking away at approximately 245 metres camera height and
  217 metres feature-relative distance: 0 of 871 features visible and
  0.0 percent aggregate and union coverage
- close and partially framed at approximately 245 metres camera height and
  effectively zero feature-relative distance: 603 of 871 features visible,
  13.5 percent aggregate coverage, and 8.0 percent union coverage
- close and centered at approximately 245 metres camera height and 332 metres
  feature-relative distance: 734 of 871 features visible, 26.5 percent
  aggregate coverage, and 18.1 percent union coverage

The qualitative ordering remained coherent across both environments: close
centered views produced the strongest screen contribution, partial framing
reduced it, distant centered views produced a small contribution, and looking
away reduced it to zero.

Olympia also made the aggregate metric's overlap inflation substantially more
visible. In the close centered observation, aggregate feature-box coverage was
26.5 percent while union coverage was 18.1 percent. Union coverage is therefore
a better diagnostic than summed feature-box area for the current experiment.

This still does not make projected feature rectangles Est's definition of
visual significance. The durable presentation concept is renderer-neutral
local-representation screen significance. The Cesium adapter may currently
estimate that semantic value from terrain-corrected projected feature envelopes
and their screen-space union, while another renderer may derive an equivalent
view-relative significance value differently.

`PresentationViewContext` now carries a normalized
`localRepresentationScreenSignificance` value in addition to camera height.
The current Cesium evaluation supplies that value from feature-box union
coverage. A subsequent significance-aware policy experiment now requires
nonzero screen significance in addition to the existing camera-height
boundaries before local structures are selected or retained. This exact-zero
gate remains experimental and does not establish final significance thresholds,
hysteresis, or feature-box union coverage as production policy.

This preserves the ownership seam:

`renderer measurements -> renderer-neutral presentation significance -> Est-owned presentation policy`

The remaining risk is treating the current Cesium-side estimator as the
semantic contract. Union coverage still uses projected feature envelopes and
is not an occlusion solution. Final significance composition, thresholds,
update cadence, transition behavior, partitioning, residency, and streaming
remain unresolved.

### Observatory observer freedom and below-reference camera states

Low-altitude stress testing of the Olympia local scene exposed an unsupported
assumption in the experimental presentation policy. `cameraHeightMeters` had
been required to be non-negative even though the Cesium evaluation adapter
currently supplies `viewer.camera.positionCartographic.height` directly.

A sufficiently low observer position could therefore produce a finite negative
cartographic height and cause presentation policy validation to throw,
terminating rendering. That is not acceptable Observatory behavior.

The finding clarified a broader product and architecture principle. The Earth
Observatory user is a privileged observer and simulation operator rather than a
simulated world entity. The observer must remain free to inspect unconventional
locations and orientations, including future subsurface, cave, volcano,
water-level, interior, and similarly unusual viewpoints, subject primarily to
actual numerical or rendering limitations.

Where a scenario permits intervention, the operator may also modify simulation
conditions or state to explore alternate outcomes. Such interventions should be
explicit simulation operations. The world should then continue evolving from
the altered state according to its applicable model and laws rather than
silently suspending those laws for the operator.

This differs from Est Living Worlds, where participating humans, animals,
agents, vehicles, and other world entities may be constrained by the physical,
behavioral, and gameplay rules appropriate to them.

For presentation architecture, the resulting rules are:

- presentation policy reacts to observer state but does not define observer
  validity;
- unusual but finite camera-derived measurements must not become errors solely
  because they fall outside ordinary above-ground ranges;
- Observatory observer freedom and operator authority are distinct from
  world-entity traversal and gameplay constraints;
- operator interventions alter simulation inputs, conditions, events, or state,
  after which simulation laws continue governing the resulting evolution;
- malformed or numerically unsafe measurements such as NaN or infinity may
  still be rejected; and
- Cesium cartographic camera height remains an experimental scale signal rather
  than a settled renderer-neutral definition of scale.

The experimental presentation policy was therefore changed to require its
camera-height measurement to be finite rather than non-negative.

### Stationary significance stability check

A follow-up runtime diagnostic distinguished representation transitions caused
by observer movement from transitions occurring while the camera pose remained
effectively unchanged.

The Cesium evaluation layer temporarily compared successive world-coordinate
camera position and orientation vectors and counted representation transitions
that occurred without meaningful observer movement. The diagnostic remained
evaluation-only and did not participate in presentation-policy decisions.

Stress testing Olympia included conventional views as well as unusually low,
below-reference, interior-like, and geometry-intersecting Observatory
viewpoints. Individual runs accumulated thousands to tens of thousands of
`postRender` presentation checks and multiple representation transitions while
the observer was moving.

Across the tested stationary views, however, the stationary-transition count
remained zero.

This changes the interpretation of earlier runs that showed transition counts
as high as approximately 15 near the local-scene visibility boundary. The
available evidence now indicates that those transitions were associated with
observer movement across the zero-significance boundary rather than spontaneous
per-frame measurement chatter at a fixed camera pose.

The experiment therefore does not currently justify adding arbitrary
screen-significance hysteresis thresholds. The exact-zero significance rule
remains experimental, but hysteresis or debounce should be introduced only if
later evidence demonstrates undesirable user-visible transition behavior rather
than merely because repeated transitions are possible during deliberate camera
movement.

The test also provided additional stress evidence for the Observatory observer
model: below-reference and otherwise unconventional finite camera states
continued through presentation evaluation without policy exceptions after the
non-negative camera-height assumption was removed.

### Center-view surface-distance scale experiment

The next Phase 7 scale experiment tested whether distance from the observer to
the world surface actually being viewed provides a more useful presentation
scale signal than Cesium cartographic camera height or distance to the current
local-scene representation.

The Cesium evaluation adapter now casts a ray through the center of the
viewport and, when that ray intersects the terrain globe, reports the
observer-to-surface distance. The measurement remains Cesium-side and
diagnostic-only. It does not participate in `PresentationViewContext` or
presentation-policy decisions.

Olympia produced several useful contrasting observations:

- at approximately -15 metres cartographic height while embedded in the local
  scene, local-scene distance was 0 metres and center-view surface distance was
  approximately 12 metres;
- at the same approximately -15-metre cartographic height while looking
  horizontally across the lake, local-scene distance was approximately 760
  metres while center-view surface distance was approximately 6 metres;
- at street level, approximately 18 metres cartographic height, center-view
  surface distance was approximately 19 metres;
- from a low oblique view at approximately 57 metres cartographic height,
  center-view surface distance increased to approximately 105 metres;
- from a nearly top-down view at approximately 339 metres cartographic height,
  center-view surface distance was approximately 322 metres;
- from a regional oblique view at approximately 2,188 metres cartographic
  height, center-view surface distance was approximately 15,182 metres; and
- while looking into the sky at approximately 38 metres cartographic height,
  the center ray had no terrain intersection and the diagnostic correctly
  reported no center-surface distance.

These observations distinguish three different concepts. Cartographic height
describes the observer relative to Cesium's reference ellipsoid.
`distanceToLocalSceneMeters` describes the observer relative to the current
local representation's aggregate bounding sphere and can collapse to zero
across materially different views when the observer lies inside that sphere.
Center-view surface distance instead changes with view direction and the world
surface actually being observed.

The experiment therefore provides evidence that cartographic camera height
should not automatically become Est's renderer-neutral definition of
presentation scale, and that distance to a particular local representation is
also insufficient as that definition.

A single center ray is not promoted as the replacement. Near a horizon, a
small orientation change can move the center ray between a distant terrain
intersection and no intersection at all.

### Multi-sample view-surface scale experiment

A follow-up diagnostic sampled five fixed viewport positions: center, left,
right, upper, and lower. Each sample independently cast a ray against the
terrain globe. The diagnostic deliberately exposed the raw hit count and
distances rather than choosing an aggregation rule in advance.

Olympia produced three especially useful regimes:

- a sky-facing view produced 0/5 surface intersections, with all five samples
  reporting no distance;
- a low horizon-facing view at approximately 18 metres cartographic height
  produced 4/5 intersections: approximately 47, 50, 49, no intersection, and
  24 metres for center, left, right, upper, and lower respectively; and
- an extremely close surface view produced 5/5 intersections with all five
  samples at approximately 2 metres.

The horizon-facing case demonstrates why a single center ray is too brittle as
the complete scale definition. The upper sample legitimately sees sky while
the remaining samples continue to describe a coherent local-scale relationship
to the world surface.

The experiment also suggests that a sampled view-to-world relationship contains
at least two distinct facts: how much of the sampled view intersects the world,
and the distribution of distances among the samples that do intersect it.
A 0/5 sky-facing view is therefore not an infinite-distance or invalid view.
It is a valid observer state for which sampled surface-distance scale is
unavailable.

These results support deriving a future renderer-neutral presentation-scale
concept from view-relative world measurements rather than directly equating
presentation scale with cartographic camera altitude. They do not yet establish
the number or placement of production samples, an aggregation statistic, final
scale thresholds, or the policy behavior for partially surface-facing views.

## September 14, 2026: Production planetary renderer conclusion

The Cesium surface-evaluation line is now closed as a production-renderer
investigation.

The accumulated evaluation work remains useful evidence, but Cesium is no
longer the intended production planetary renderer.

### What remained valid

Several earlier conclusions survived the renderer reset:

- authoritative simulation state must remain independent of presentation;
- semantic truth and visual appearance are separate concerns;
- different viewing scales may use different representations;
- local geometry is appropriate when raster imagery no longer contains enough
  structural information;
- renderer-specific measurements may feed renderer-neutral presentation policy;
- authoritative terrain and hydrology belong to simulation rather than the
  renderer.

The current Est surface-grid abstraction also remains valid as a simulation
substrate.

The failure was not that terrain or hydrology existed on a coarse grid.

The failure was exposing that simulation topology too directly through the
visual planet.

### Authoritative terrain result

The current deterministic terrain field and the
`CustomHeightmapTerrainProvider` integration established that Est's
authoritative terrain values can drive continuous globe geometry.

The terrain data itself was not shown to be the source of the major visual
failures.

Runtime inspection also showed that terrain lighting and appearance were being
strongly constrained by Cesium's rendering path. The heightmap provider did not
supply the explicit surface-normal path required for the desired terrain
lighting control.

This made further Cesium-specific terrain adaptation increasingly expensive
relative to the value it provided.

### Hydrology presentation result

The standing-water visualization used one polygon per authoritative surface
cell.

That representation correctly reflected simulation state but made the
simulation tessellation visible.

The resulting coastline was blocky and stair-stepped because the renderer was
drawing storage cells rather than a continuous water surface intersecting
continuous terrain.

This is now considered a presentation-architecture failure, not a requirement
to increase hydrology simulation resolution solely for appearance.

Future ocean rendering must decouple visual water geometry from hydrology cell
geometry.

### Quantized-mesh experiment

A custom Cesium quantized-mesh terrain provider was tested in order to gain
explicit terrain normals.

The experiment passed its focused unit tests and production build.

Browser runtime validation nevertheless failed decisively.

Observed failures included:

- visible terrain tile or sector boundaries;
- meridian or quadtree seams;
- large regions changing or disappearing with camera movement;
- radial or curved artifacts;
- unstable visual geometry despite green automated validation.

The custom provider was rolled back.

The experiment demonstrated that implementing Cesium's quantized-mesh machinery
inside the browser is not an appropriate production path for Est.

If quantized mesh is ever required for a bounded external use, it should use a
mature encoder and standard provider pipeline rather than reimplementing the
format inside Est's production renderer.

### Generated surface-imagery experiment

A second experiment retained the known-good heightmap terrain geometry but
replaced Cesium terrain coloring with an Est-controlled raster imagery provider.

The provider generated elevation-based terrain colors and deterministic
hillshade into imagery tiles.

Focused tests passed.

The production web build passed.

A polar sampling bug was found and corrected.

Browser runtime validation still failed the visual gate.

The result continued to read as large painted regions rather than convincing
terrain, useful relief was insufficient, and the approach did not solve the
fundamental representation problem.

That experiment was rolled back as well.

### Architectural conclusion

Two independent rendering workarounds failed for different reasons while the
underlying simulation remained coherent.

The relevant architectural mistake is now explicit:

`simulation spatial resolution != render spatial resolution`

The authoritative Est surface grid is a simulation structure.

It must not also be the finished terrain mesh, coastline geometry, imagery
pixel grid, or visual level-of-detail structure.

The replacement production renderer will use a game-style multi-resolution
planet surface.

The initial design is:

`authoritative fields -> continuous sampling -> cube-sphere quadtree -> GPU terrain presentation`

The visual terrain will have its own hierarchy and density.

The authoritative terrain field will be sampled into that hierarchy rather
than having its cells drawn directly.

Large standing-water bodies will be presented as continuous surfaces so visible
shorelines arise from terrain/water intersection rather than hydrology-cell
boundaries.

### Renderer direction

Cesium is retired as the production planetary-renderer direction.

Existing Cesium evaluation assets and code may remain temporarily where they
provide useful comparison evidence, but new production planet-rendering work
should not extend:

- Cesium heightmap presentation;
- custom Cesium quantized mesh;
- generated Cesium terrain imagery;
- per-cell water polygons;
- other workarounds whose purpose is to force Est's simulation grid directly
  into Cesium's visual surface.

The first implementation target for the replacement browser renderer is
Babylon.js.

The first rendering milestone is deliberately smaller than the work that
preceded it:

1. create a Babylon renderer entry point;
2. construct a six-face cube-sphere;
3. verify orientation and continuity;
4. support stable orbit and zoom;
5. add renderer-owned quadtree LOD;
6. only then connect authoritative terrain sampling.

Terrain materials, ocean, atmosphere, populations, local geometry, and other
features follow only after the base planetary geometry is runtime-green.

The detailed decision and acceptance gates are recorded in
`docs/architecture/0006-planetary-rendering-separation.md`.

The Cesium surface-evaluation work should now be treated as completed
architectural research rather than an unfinished production implementation.
