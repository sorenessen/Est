# ADR 0017: Hydrology Regional Temperature Consumption

- Status: Accepted
- Decision date: 2026-09-19

## Context

ADR 0016 activated durable regional thermal authority.

A planet configured with `RegionalThermalModelDefinition` now has authoritative
surface-system and atmospheric-column temperature for every authoritative
surface cell.

Under regional thermal authority:

`PlanetRegionalThermalState`

owns evolving temperature state, while:

`PlanetEnvironment.MeanSurfaceTemperatureKelvin`

is only an area-weighted compatibility projection for downstream systems that
have not yet migrated to regional climate inputs.

The current hydrology system still uses that compatibility planetary mean for
water phase decisions.

For every hydrology cell it currently evaluates:

`MeanSurfaceTemperatureKelvin <= FreezingTemperatureKelvin`

to permit liquid-water freezing, and:

`MeanSurfaceTemperatureKelvin >= MeltingTemperatureKelvin`

to permit snow/ice melting.

That means one planetary mean currently determines phase behavior for every
surface cell even when authoritative regional surface temperatures differ.

ADR 0016 explicitly identifies cell-local hydrology temperature consumption as
deferred consumer migration.

ADR 0016 also establishes an important separate boundary:

- regional thermal evolution is currently radiative only;
- hydrology may move water between liquid and snow/ice stores;
- latent heat from those phase changes is not yet exchanged with thermal
  reservoirs;
- future hydrology-energy coupling must conserve energy with equal and opposite
  accounting.

This ADR migrates the hydrology temperature signal without prematurely adding
that later energy coupling.

## Decision

Hydrology phase decisions will select their temperature source from regional
thermal model configuration for the target planet.

For a planet without configured regional thermal authority, including a planet
using planetary energy-balance authority or a planet with no causal thermal
model configured:

`PlanetEnvironment.MeanSurfaceTemperatureKelvin`

remains the hydrology phase-change temperature signal.

For a planet using regional thermal authority:

`RegionalThermalCellState.SurfaceTemperatureKelvin`

for the matching surface cell becomes the hydrology phase-change temperature
signal.

The mere presence of `PlanetRegionalThermalState` is not sufficient to select
regional temperature.

Thermal model configuration remains authoritative.

Dormant regional thermal migration state must not alter hydrology while the
planetary energy-balance model remains configured.

## Scope of the migration

This milestone changes only the temperature input used by the existing
hydrology freezing and melting rules.

For each hydrology cell under regional thermal authority:

`T_phase = RegionalThermalCellState.SurfaceTemperatureKelvin`

The existing phase rules remain:

`T_phase <= FreezingTemperatureKelvin`

permits freezing subject to the existing source-store and maximum-rate limits.

`T_phase >= MeltingTemperatureKelvin`

permits melting subject to the existing source-store and maximum-rate limits.

If the configured freezing and melting thresholds define a gap, temperatures
inside that gap continue to leave liquid and snow/ice phase stores unchanged.

No new phase equation is introduced by this ADR.

## Surface temperature, not atmospheric temperature

Hydrology freezing and melting currently transfer water between:

- surface liquid water;
- snow / ice water equivalent.

These are surface-system stores.

The first regional consumer migration therefore uses:

`SurfaceTemperatureKelvin`

and not:

`AtmosphericTemperatureKelvin`.

Regional atmospheric-column temperature may become relevant to future
evaporation, condensation, precipitation, humidity, or atmospheric-water
models.

This ADR does not introduce those couplings.

## Thermal authority selection

Hydrology must not infer authority from state presence.

The causal hydrology system must be told, directly or through an equivalent
explicit construction policy, whether regional thermal authority is configured
for its planet.

Session construction already owns the simulation definition and therefore knows
whether the target planet has a configured:

`RegionalThermalModelDefinition`.

That configuration is the authority switch for hydrology temperature
consumption.

If no regional thermal model is configured, hydrology remains on the
compatibility planetary-temperature path whether the planet uses a
`PlanetaryEnergyBalanceModelDefinition` or has no causal thermal model.

A suitable implementation may pass an explicit thermal-source policy into
`HydrologySystem`.

The exact type name is not mandated by this ADR.

The policy must make the two modes explicit:

1. planetary compatibility temperature when regional thermal authority is not
   configured;
2. regional surface-cell temperature when regional thermal authority is
   configured.

It must not silently choose regional temperature merely because regional state
exists.

## Causal ordering

The existing causal order remains:

`seasonal astronomy`
`-> thermal authority`
`-> hydrology`
`-> biogeochemistry`
`-> vegetation`
`-> consumers`

For regional thermal authority, this means hydrology consumes the regional
surface temperatures produced by the thermal evaluation for the current causal
step.

No additional thermal evaluation occurs inside hydrology.

Hydrology must not modify regional thermal state in this milestone.

## Shared grid requirement

Hydrology and regional thermal state already belong to the authoritative
surface-grid architecture.

When regional temperature consumption is active, hydrology must require:

- the same planet identity;
- the same `SurfaceGridDefinition`;
- exactly one regional thermal cell for every hydrology surface cell.

Existing `WorldState` and state-level validation already establish much of this
invariant through the shared terrain grid.

The hydrology causal boundary must still reject a missing or incompatible
regional thermal state rather than fall back silently to planetary mean
temperature while regional authority is configured.

There is no interpolation between hydrology and thermal grids in this
milestone.

## No lapse-rate reconstruction

When regional thermal authority is active, hydrology must consume the
authoritative regional surface temperature directly.

It must not:

- start from the compatibility planetary mean;
- apply terrain elevation;
- apply an atmospheric lapse rate;
- blend the planetary mean with regional temperature;
- synthesize another local-temperature estimate.

Regional thermal state is already the local thermal authority.

## Compatibility behavior without regional thermal authority

Hydrology must remain usable on planets that do not use regional thermal
authority.

Whenever no regional thermal model is configured, existing behavior remains:

`T_phase = PlanetEnvironment.MeanSurfaceTemperatureKelvin`

This includes:

- planets using planetary energy-balance authority;
- planets with hydrology but no configured causal thermal model.

No regional thermal state is required for that path.

If dormant regional thermal state is present without a configured regional
thermal model, hydrology must ignore that dormant state.

State presence alone does not activate regional temperature consumption.

This preserves the authority rules established by ADR 0016.

## Water-state authority

This ADR does not change hydrology ownership.

`PlanetHydrologyState` remains authoritative for:

- atmospheric water mass per unit area;
- surface liquid water mass per unit area;
- soil water mass per unit area;
- snow / ice water-equivalent mass per unit area.

`PlanetRegionalThermalState` remains authoritative only for regional thermal
state.

Temperature consumption does not transfer ownership of water state to the
thermal subsystem.

## No latent-heat coupling yet

This migration does not add thermodynamic feedback from water phase change.

Freezing does not yet add latent heat to a thermal reservoir.

Melting does not yet remove latent heat from a thermal reservoir.

The regional thermal system does not inspect hydrology phase-change mass during
this milestone.

Hydrology does not modify surface or atmospheric regional temperature.

This remains a known thermodynamic incompleteness.

A later hydrology-energy coupling milestone must introduce explicit,
equal-and-opposite energy accounting between water phase changes and the
appropriate thermal reservoir.

That future work must conserve energy rather than hiding latent heat inside a
temperature threshold or effective heat-capacity adjustment.

## No regional cryosphere authority yet

Using cell-local surface temperature for hydrology freezing and melting does not
make:

`PlanetEnvironment.IceCoverageFraction`

an authoritative regional cryosphere state.

The coarse planetary ice-coverage field remains unchanged under regional
thermal authority as established by ADR 0016.

Detailed snow and ice water mass remains in hydrology.

A future cryosphere model may derive broader ice properties from regional
hydrology and thermal state.

That is outside this milestone.

## Evaporation and precipitation remain unchanged

The current first-pass hydrology model uses configured bounded rates for
evaporation and precipitation.

This ADR does not make those rates functions of:

- regional surface temperature;
- atmospheric-column temperature;
- humidity;
- vapor pressure;
- saturation vapor pressure;
- wind;
- radiation;
- latent heat.

Those processes retain their existing semantics.

They may be replaced by more physical formulations in later milestones.

## Runoff and basin behavior remain unchanged

This migration does not alter:

- infiltration;
- runoff;
- drainage topology;
- basin equilibrium;
- standing-water derivation;
- physical cell-area accounting;
- simultaneous runoff application.

Temperature source selection must not change the existing water-mass
conservation behavior.

## Determinism

For identical:

- world state;
- simulation definition;
- hydrology parameters;
- elapsed duration;

hydrology must produce identical results.

Regional thermal cell iteration order must not affect hydrology results.

Hydrology already addresses cells by `SurfaceCellId`.

The regional-temperature path must also resolve temperature by
`SurfaceCellId`, not by positional array index.

## Zero-duration evaluation

A zero-duration hydrology evaluation must not transfer water regardless of
thermal authority.

Selecting a regional temperature source must not create a hidden state change.

## Simulation-definition validation

The existing regional thermal model definition already requires authoritative
regional thermal state before a session can activate regional thermal authority.

This migration does not require regional thermal state merely because hydrology
is configured.

The requirement is conditional:

- hydrology without regional thermal authority does not require regional
  thermal state;
- this includes both planetary EBM worlds and worlds with no causal thermal
  model configured;
- hydrology + regional thermal authority requires the regional thermal state
  already required by that thermal model.

No new independent hydrology thermal-authority configuration should be allowed
to contradict the simulation definition.

## Initial validation boundary

Implementation must prove at minimum:

- hydrology without regional thermal authority preserves existing
  planetary-mean phase behavior;
- that compatibility behavior is preserved both with planetary EBM authority
  and with no causal thermal model configured;
- dormant regional thermal state is ignored whenever regional thermal authority
  is not configured;
- regional thermal authority uses cell-local surface temperature for freezing;
- regional thermal authority uses cell-local surface temperature for melting;
- two cells on the same planet may make different phase decisions during the
  same hydrology evaluation;
- planetary compatibility mean cannot override a regional cell phase decision;
- regional atmospheric-column temperature is not used for surface phase
  decisions;
- missing regional thermal state is rejected when the regional path is active;
- mismatched regional thermal and hydrology grids are rejected;
- cell lookup is by `SurfaceCellId` and independent of stored cell order;
- water-mass conservation remains within the existing tolerance;
- hydrology still runs after thermal authority in the session pipeline;
- hydrology does not modify regional thermal state;
- freezing does not modify thermal state;
- melting does not modify thermal state;
- coarse `IceCoverageFraction` remains unchanged;
- evaporation and precipitation behavior remains unchanged by this migration;
- zero-duration evaluation transfers no water.

## Deferred work

This ADR does not add:

- latent heat of fusion;
- latent heat of vaporization;
- evaporation-energy coupling;
- condensation heating;
- temperature-dependent evaporation;
- humidity-dependent evaporation;
- vapor-pressure or saturation physics;
- atmospheric regional water transport;
- sensible heat;
- convection;
- surface-atmosphere conduction;
- regional ice-fraction authority;
- glacier dynamics;
- sea ice dynamics;
- snow thermodynamics;
- ocean mixed-layer thermodynamics;
- horizontal heat transport;
- vegetation cell-local temperature migration;
- biogeochemistry cell-local temperature migration.

Each of those requires its own explicit physical and authority boundary.

## Consequences

Hydrology becomes the first downstream consumer to leave the planetary
compatibility-temperature bridge when regional thermal authority is active.

The causal chain for regional phase behavior becomes:

`regional surface temperature`
`-> cell-local hydrology freezing / melting decision`
`-> authoritative regional water stores`

while the thermal feedback chain intentionally stops there for now.

For worlds without regional thermal authority, including planetary EBM worlds
and worlds with no causal thermal model configured, the existing chain remains:

`planetary mean surface temperature`
`-> hydrology freezing / melting decision`
`-> authoritative regional water stores`

This preserves backward behavior while allowing regional climate to produce
physically distinct hydrology phase behavior across the planet.

The later energy-coupling milestone will extend the regional path to include
conserved latent-heat exchange rather than changing the authority decision
defined here.
