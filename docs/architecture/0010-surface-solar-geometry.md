# ADR 0010: Surface Solar Geometry Foundation

## Status

Accepted.

Decision date: 2026-09-18.

## Context

ADR 0009 introduced the first authoritative Derived astronomical seasonal
signal.

For configured planets, Est can now derive:

- normalized orbital cycle fraction;
- subsolar latitude.

That state is physically meaningful, but no surface system yet translates
subsolar latitude into latitude-specific sunlight geometry.

ADR 0005 establishes the shared planet-surface grid as the authoritative
topology for future regional climate and explicitly anticipates cell-level
insolation.

The existing `PlanetaryEnergyBalanceSystem` remains a zero-dimensional global
climate model. Its configured `StellarFluxWattsPerSquareMeter` is the current
authority for top-of-atmosphere stellar flux magnitude, and its division by four
represents the spherical global-mean geometric factor.

The next foundation must therefore add spatial solar geometry without:

- creating a second stellar-flux authority;
- replacing the global energy-balance model;
- introducing regional temperature state prematurely;
- making climate or biology respond to seasonal labels;
- depending on the current latitude/longitude grid implementation.

## Decision

Est will add a pure surface solar-geometry calculation that derives
latitude-specific daily-mean sunlight geometry from:

- surface latitude in degrees;
- subsolar latitude in degrees.

The calculation will not own stellar flux magnitude.

Its primary outputs will be:

- geometric daylight fraction, normalized to `[0, 1]`;
- daily-mean top-of-atmosphere insolation factor, dimensionless and normalized
  relative to the configured normal-incidence stellar flux.

A future spatial climate system may obtain actual daily-mean incoming stellar
flux by multiplying the existing authoritative stellar-flux magnitude by the
dimensionless insolation factor.

## Daily-mean geometry

Let:

- `phi` be surface latitude;
- `delta` be subsolar latitude;
- `H0` be the sunset hour angle.

For ordinary non-polar cases:

`cos(H0) = -tan(phi) * tan(delta)`

with the geometric limits:

- polar night: `H0 = 0`;
- continuous daylight: `H0 = pi`.

The daylight fraction is:

`H0 / pi`

The daily-mean top-of-atmosphere insolation factor is:

`(H0 * sin(phi) * sin(delta)
 + cos(phi) * cos(delta) * sin(H0)) / pi`

The implementation must handle polar limits explicitly and must not allow
floating-point roundoff to produce non-finite or physically invalid results.

## Meaning of the insolation factor

The insolation factor is dimensionless.

It is not:

- absorbed solar energy;
- surface shortwave radiation;
- atmospheric transmission;
- cloud forcing;
- albedo;
- temperature forcing;
- photosynthetically active radiation.

Those require downstream physical models.

For a sphere, the area-weighted global mean of this daily-mean geometric factor
is one quarter.

A discretized surface grid that samples cell-center latitude should converge
toward `0.25` as spatial resolution increases.

This preserves consistency with the existing global energy-balance convention:

`global mean incoming flux = stellar flux / 4`

## Surface-grid interaction

The solar-geometry calculation itself will operate on latitude rather than on a
specific grid type.

Surface systems that apply it to cells must obtain latitude through the
authoritative `IPlanetSurfaceGrid` / `SurfaceCell` geometry contract.

They must not:

- decode `SurfaceCellId`;
- infer latitude from row ordering;
- depend on `LatLonPlanetSurfaceGrid`;
- create a separate climate-specific spatial grid.

This preserves future replacement of the initial latitude/longitude
tessellation.

## Longitude and time of day

This foundation uses daily-mean geometry.

Longitude is therefore not an input.

It does not model:

- local solar time;
- sunrise clock time;
- sunset clock time;
- instantaneous solar zenith angle;
- diurnal temperature cycles.

Those require authoritative rotation and time-of-day semantics that Est does not
yet model.

The daylight result is a fraction rather than a duration so this foundation
does not silently assume a planetary rotation period.

## Relationship to seasonal state

The circular-orbit seasonal provider remains responsible for deriving
subsolar latitude.

Surface solar geometry consumes the physical `SubsolarLatitudeDegrees` signal.

It must not make decisions from:

- `SeasonalControlMode`;
- `PhaseId`;
- named seasons;
- orbital quarter labels.

This keeps downstream physics coupled to physical quantities rather than
seasonal labels.

An explicit seasonal Override that does not provide subsolar latitude therefore
does not automatically create synthetic solar geometry.

Product policy for what an override should mean to physical consumers remains a
separate decision.

## State and persistence

The initial solar-geometry milestone is a pure deterministic calculation.

It does not add:

- new `WorldState`;
- new simulation operations;
- snapshot fields;
- timeline archive fields;
- schema-version changes;
- API resources;
- user controls.

Durable regional radiation or climate state may be added later if a causal
consumer requires it.

## Initial validation boundary

The first implementation must prove:

- deterministic results for identical inputs;
- equinox symmetry between hemispheres;
- opposite-solstice symmetry between hemispheres;
- expected continuous-daylight and polar-night behavior;
- bounded daylight fraction in `[0, 1]`;
- bounded non-negative insolation factor;
- equatorial equinox daily-mean factor of `1 / pi`;
- area-weighted global mean over a sufficiently fine authoritative surface grid
  is approximately `0.25`;
- no dependency on longitude;
- no mutation of simulation state.

## Deferred work

This milestone does not add:

- eccentric-orbit distance forcing;
- variable stellar luminosity;
- atmospheric transmission;
- clouds;
- terrain slope or aspect illumination;
- horizon shadowing;
- regional temperature;
- regional humidity;
- atmospheric heat transport;
- photoperiod-driven biology;
- vegetation seasonality;
- breeding seasons;
- migration;
- dormancy or hibernation;
- user-facing seasonal controls.

Those remain downstream consumers or richer astronomical, atmospheric, terrain,
and biological models.

## Consequences

Est gains the physical bridge between astronomical seasonality and the shared
surface substrate without prematurely coupling it to climate or biology.

Future regional climate can consume:

`stellar flux magnitude -> solar geometry -> local incoming radiation`

while preserving the existing global energy-balance model as the current
planetary thermal baseline.

Biological systems that later need photoperiod can consume daylight geometry
without interpreting abstract season labels.
