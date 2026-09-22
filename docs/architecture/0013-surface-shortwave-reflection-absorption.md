# ADR 0013: Surface Shortwave Reflection and Absorption Foundation

## Status

Accepted.

Decision date: 2026-09-19.

## Context

ADR 0010 established latitude-specific top-of-atmosphere solar geometry.

ADR 0011 established clear-sky direct-beam atmospheric extinction.

ADR 0012 established first-order atmospheric shortwave energy partitioning into:

- direct surface radiation;
- downward diffuse surface radiation;
- atmospheric absorption;
- upward atmospheric scattering.

The resulting surface-downwelling shortwave factor is:

`F_surface_down = F_direct + F_diffuse_down`

This quantity represents shortwave energy arriving at the planetary surface.

It deliberately does not determine how much of that incoming energy the
surface:

- reflects;
- absorbs;
- stores;
- transfers as sensible heat;
- transfers as latent heat;
- transmits into water, ice, soil, or another subsurface medium.

The next required physical boundary is therefore the first-order optical
response of the planetary surface.

Est already contains albedo parameters in `PlanetaryEnergyBalanceParameters`:

- `IceFreeAlbedo`;
- `IceAlbedo`.

Those values belong to the existing zero-dimensional
`PlanetaryEnergyBalanceSystem`.

That system applies an effective albedo directly to:

`StellarFluxWattsPerSquareMeter / 4`

to compute its coarse global absorbed-solar term.

Those parameters therefore remain part of the existing global energy-balance
model.

They are not adopted as authoritative local surface optical properties by this
ADR.

Reusing them directly in the new surface-radiation chain would silently change
their semantics and could double-count atmospheric reflection already
represented by ADR 0012.

## Decision

Est will add a pure deterministic surface shortwave reflection-and-absorption
calculation.

The first model will consume:

- a finite non-negative surface-downwelling shortwave factor;
- an explicit effective broadband surface albedo `alpha_surface`.

The valid albedo range is:

`0 <= alpha_surface <= 1`

The incoming shortwave factor remains dimensionless relative to the same
normal-incidence stellar-flux reference used by ADRs 0010 through 0012.

The surface calculation does not own stellar-flux magnitude.

## Effective broadband surface albedo

`alpha_surface` represents the effective fraction of incoming broadband
shortwave energy reflected away from the modeled surface system.

It is a local surface optical parameter.

It is not:

- Bond albedo;
- planetary albedo;
- the existing zero-dimensional energy-balance albedo;
- atmospheric reflectance;
- cloud albedo;
- a spectral reflectance curve;
- a bidirectional reflectance distribution function;
- a direct-beam-only reflectance;
- a diffuse-only reflectance.

The first model applies one effective albedo to total surface-downwelling
shortwave.

That is an explicit first-order closure.

Future surface optics may distinguish direct and diffuse incidence, wavelength,
angle, polarization, material, roughness, or other physical properties.

## Surface shortwave partition

Let:

`F_in = F_surface_down`

and:

`alpha = alpha_surface`

The reflected surface shortwave factor is:

`F_reflected = F_in * alpha`

The surface-system absorbed shortwave factor is:

`F_absorbed_surface = F_in - F_reflected`

Equivalently:

`F_absorbed_surface = F_in * (1 - alpha)`

The subtraction form is preferred in implementation so the local conservation
identity remains numerically tight.

## Surface energy-conservation invariant

For every valid input:

`F_in = F_reflected + F_absorbed_surface`

within floating-point tolerance.

The surface optical layer must not create or destroy shortwave energy.

It only partitions incoming energy.

## Meaning of surface-system absorption

`F_absorbed_surface` represents shortwave energy that is not reflected by the
effective modeled surface boundary.

It does not imply that all such energy is absorbed at an infinitesimally thin
surface skin.

Depending on future surface physics, the non-reflected energy may physically be
absorbed within:

- vegetation;
- soil;
- rock;
- snow;
- ice;
- shallow water;
- another unresolved surface or near-surface reservoir.

For this first accounting layer, those distinctions remain unresolved.

Therefore `F_absorbed_surface` is an optical energy-deposition quantity, not yet
a temperature tendency or heat-storage assignment.

## Surface transmission

The first model does not expose a separate transmitted-shortwave term.

At the current coarse surface boundary, all non-reflected shortwave is assigned
to the unresolved surface-system absorbed term.

This is a model closure, not a claim that every physical planetary material is
opaque.

A future model may introduce explicit penetration or transmission into:

- water columns;
- ice;
- snow;
- soil;
- vegetation canopies;
- subsurface layers.

If that occurs, the surface accounting may be refined to:

`incoming = reflected + absorbed + transmitted`

while preserving the higher-level incoming and reflected energy contracts.

## Relationship to direct and diffuse radiation

ADR 0012 retains separate direct and downward-diffuse surface components.

ADR 0013 initially consumes their total:

`F_surface_down = F_direct + F_diffuse_down`

The first effective surface albedo applies equally to the combined incoming
shortwave.

ADR 0013 does not claim that real materials have identical direct and diffuse
reflectance.

A future surface-optics model may distinguish them.

## Relationship to ADR 0012

ADR 0012 remains authoritative for atmospheric shortwave energy partitioning.

ADR 0013 begins at the surface-downwelling boundary.

It must not:

- recompute atmospheric transmission;
- recompute atmospheric scattering;
- alter atmospheric absorption;
- reinterpret upward atmospheric scattering.

The atmospheric result and the surface result compose into a larger
first-order shortwave conservation identity:

`F_toa
 = F_absorbed_atmosphere
 + F_scattered_up
 + F_reflected_surface
 + F_absorbed_surface`

within floating-point tolerance.

This end-to-end identity is a required integration invariant for the current
first-order shortwave chain.

## Instantaneous and daily-mean use

The surface optical partition is algebraic.

It may therefore operate on either:

- instantaneous surface-downwelling shortwave;
- daily-mean surface-downwelling shortwave.

When the effective surface albedo is constant over the integration interval,
partitioning a daily-mean incoming factor is equivalent to integrating the
instantaneous reflected and absorbed factors separately.

The first milestone assumes the supplied effective albedo is constant over the
quantity being partitioned.

It does not model:

- solar-angle-dependent albedo;
- diurnal material-state change;
- changing snow or ice state during the integration interval;
- changing vegetation state during the integration interval.

## Relationship to the surface grid

The authoritative surface topology remains `IPlanetSurfaceGrid` and
`SurfaceCell`.

ADR 0013 does not add optical state to `SurfaceCell`.

`SurfaceCell` remains geometry and topology only.

The first implementation does not decode or infer surface optical properties
from `SurfaceCellId`, latitude, longitude, or grid implementation details.

A future surface-optics provider may evaluate an effective surface albedo for a
cell using authoritative physical state associated with that cell.

## Future surface-optics provider

The first milestone accepts surface albedo explicitly.

It does not derive albedo from existing simulation state.

A future surface-optics provider may consider authoritative state such as:

- terrain or substrate;
- standing water;
- water depth;
- snow;
- ice;
- vegetation;
- canopy structure;
- soil moisture;
- surface roughness;
- land cover;
- constructed structures;
- wavelength or stellar spectrum;
- solar incidence angle.

Such a provider must preserve the distinction between:

- authoritative physical surface state;
- derived optical properties;
- radiative energy accounting.

## Relationship to hydrology and vegetation

Hydrology and vegetation already have authoritative cell-associated state.

ADR 0013 does not yet make those systems causal inputs to surface albedo.

In particular, this milestone does not invent fixed albedos for:

- ocean;
- lakes;
- bare ground;
- vegetation;
- snow;
- ice;
- cities.

Those values require a deliberate surface-optics model rather than hidden
constants inside the energy-partition calculation.

## Relationship to thermal authority

ADR 0013 does not reinterpret planetary energy-balance albedos as local surface
reflectance and does not itself select or replace thermal authority.

When `PlanetaryEnergyBalanceSystem` is configured, its existing:

- `IceFreeAlbedo`;
- `IceAlbedo`;
- global `StellarFluxWattsPerSquareMeter / 4` convention;
- ice-temperature feedback;

retain their planetary-model semantics.

Regional thermal authority consumes the local optical accounting through the
separate authority and migration boundary defined by ADR 0016.

## Thermal semantics

Absorbed shortwave is necessary for downstream thermal forcing, but it is not
sufficient by itself to define temperature change.

`F_absorbed_surface` does not yet determine:

- surface temperature;
- atmospheric temperature;
- sensible heat;
- latent heat;
- evaporation;
- conduction;
- subsurface heat storage;
- ocean heat storage;
- snow or ice melt;
- outgoing longwave radiation.

Those require explicit thermal reservoirs and energy-transfer systems.

No existing temperature state is modified by this milestone.

## State and persistence

The initial surface shortwave reflection-and-absorption milestone remains pure
deterministic physics.

It does not add:

- new `WorldState`;
- new `SurfaceCell` fields;
- new terrain state;
- new hydrology state;
- new vegetation state;
- new planet-environment fields;
- new simulation operations;
- new `SimulationDefinition` model configuration;
- snapshot fields;
- timeline archive fields;
- schema-version changes;
- API resources;
- web contracts;
- user controls.

Durable surface optical properties belong to a future surface-optics model.

Durable radiative-energy state belongs to a later causal regional-energy
milestone.

## Initial validation boundary

The first implementation must prove:

- deterministic results for identical inputs;
- rejection of negative or non-finite incoming shortwave;
- rejection of surface albedo outside `[0, 1]`;
- reflected shortwave is finite and non-negative;
- absorbed shortwave is finite and non-negative;
- reflected shortwave cannot exceed incoming shortwave;
- absorbed shortwave cannot exceed incoming shortwave;
- zero incoming shortwave produces zero reflected and absorbed energy;
- `alpha_surface = 0` produces zero reflection and complete absorption;
- `alpha_surface = 1` produces complete reflection and zero absorption;
- intermediate albedo partitions energy correctly;
- surface conservation holds across representative inputs;
- composition with ADR 0012 preserves end-to-end shortwave conservation;
- both instantaneous and daily-mean ADR 0012 results can be consumed without
  changing their semantics;
- no simulation state is mutated.

## Deferred work

This milestone does not add:

- physical derivation of surface albedo;
- material-specific albedo tables;
- wavelength-dependent surface optics;
- spectral radiative transfer;
- direct-versus-diffuse surface reflectance;
- angle-dependent reflectance;
- Fresnel reflection;
- bidirectional reflectance;
- water-column penetration;
- snow optics;
- ice optics;
- vegetation-canopy optics;
- soil optics;
- urban-material optics;
- terrain slope or aspect illumination;
- horizon shadowing;
- surface-reflected shortwave re-entering the atmosphere;
- multiple atmosphere-surface reflections;
- clouds;
- atmospheric longwave radiation;
- surface longwave radiation;
- regional temperature;
- subsurface temperature;
- ocean heat storage;
- sensible heat;
- latent heat;
- evaporation;
- convection;
- circulation;
- hydrology coupling;
- biological radiation response.

## Consequences

Est gains an explicit, energy-conserving optical boundary between incoming
surface shortwave radiation and downstream thermal forcing.

The physical shortwave chain becomes:

`stellar flux magnitude`
`-> solar geometry`
`-> direct-beam atmospheric extinction`
`-> atmospheric absorption + scattering`
`-> direct + diffuse surface downwelling`
`-> surface reflection + absorption`
`-> thermal-reservoir energy deposition`
`-> regional thermal response`

The surface-optics foundation remains deliberately independent of thermal
authority selection.

That separation allows the same optical accounting to feed planetary or
regional thermal models without making the optics layer itself a climate-state
authority.
