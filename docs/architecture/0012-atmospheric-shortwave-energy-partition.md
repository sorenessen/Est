# ADR 0012: Atmospheric Shortwave Energy Partition Foundation

## Status

Accepted.

Decision date: 2026-09-19.

## Context

ADR 0011 established clear-sky direct-beam atmospheric shortwave extinction
using explicit broadband vertical optical depth and the Beer-Lambert relation.

That model can determine:

- top-of-atmosphere horizontal incident shortwave geometry;
- direct-beam transmission through the atmosphere;
- direct horizontal shortwave reaching the surface.

It deliberately does not determine what happens to energy removed from the
direct beam.

Direct-beam extinction may represent:

- atmospheric absorption;
- scattering out of the direct beam.

Scattered shortwave may subsequently travel:

- downward toward the surface;
- upward toward space.

Without that partition, Est cannot account for the shortwave energy budget
consumed by the regional thermal chain.

Optical depth alone is insufficient to determine this partition.

The first energy-partition layer therefore needs explicit optical parameters
rather than hidden Earth-specific aerosol, gas, cloud, or phase-function
assumptions.

## Decision

Est will add a pure deterministic atmospheric shortwave energy-partition
calculation.

The first model will use three explicit optical inputs:

- broadband vertical extinction optical depth `tau`;
- broadband single-scattering albedo `omega`;
- effective downward-scattering fraction `f_down`.

Their valid ranges are:

`tau >= 0`

`0 <= omega <= 1`

`0 <= f_down <= 1`

The model remains clear-sky and broadband.

It does not derive these values from atmospheric composition.

## Single-scattering albedo

`omega` represents the fraction of extinguished direct-beam energy attributed
to scattering rather than absorption.

Therefore:

`scattered fraction of extinction = omega`

`absorbed fraction of extinction = 1 - omega`

The initial model treats `omega` as an explicit effective broadband property.

It does not infer single-scattering albedo from:

- gas mole fractions;
- pressure;
- aerosol identity;
- wavelength;
- stellar spectrum.

Those require a future atmospheric-optics provider.

## Downward-scattering fraction

`f_down` represents the effective fraction of scattered shortwave energy that
is redirected into the downward hemisphere.

Therefore:

`downward scattered fraction = f_down`

`upward scattered fraction = 1 - f_down`

This is an explicit first-order closure parameter.

It is not:

- a complete scattering phase function;
- an asymmetry parameter;
- a multiple-scattering solution;
- an Earth-calibrated aerosol assumption.

A future radiative-transfer model may replace this closure with richer angular
physics without changing the higher-level energy-accounting contract.

## Instantaneous horizontal energy partition

For positive solar-zenith cosine:

`mu = cos(zenith angle)`

the top-of-atmosphere horizontal shortwave factor relative to
normal-incidence stellar flux is:

`F_toa = mu`

ADR 0011 defines direct-normal transmission:

`T = exp(-tau / mu)`

The direct surface horizontal factor is:

`F_direct = mu * T`

The direct-beam energy removed by atmospheric extinction is:

`F_extinguished = mu * (1 - T)`

That extinguished energy is partitioned as follows.

Atmospheric absorption:

`F_absorbed_atmosphere = F_extinguished * (1 - omega)`

Total scattering:

`F_scattered = F_extinguished * omega`

Downward single-scattered diffuse shortwave:

`F_diffuse_down = F_scattered * f_down`

Upward scattered shortwave:

`F_scattered_up = F_scattered * (1 - f_down)`

When:

`mu <= 0`

all shortwave factors are zero.

## Energy-conservation invariant

For every valid input above the horizon:

`F_toa
 = F_direct
 + F_absorbed_atmosphere
 + F_diffuse_down
 + F_scattered_up`

within floating-point tolerance.

This invariant is mandatory.

The first model must not create or destroy shortwave energy internally.

## Surface downwelling shortwave

The first-order total downwelling shortwave factor at the surface is:

`F_surface_down = F_direct + F_diffuse_down`

This value represents incoming shortwave at the surface before surface
reflection or absorption.

It is not:

- absorbed surface energy;
- net surface radiation;
- surface heating;
- photosynthetically active radiation.

Those require downstream surface optical and thermal models.

## Atmospheric shortwave absorption

`F_absorbed_atmosphere` represents the fraction of incoming horizontal
shortwave deposited in the atmospheric column by the first-order model.

This is the first Est shortwave quantity that may legitimately contribute to a
future atmospheric thermal budget.

It does not yet produce:

- atmospheric temperature change;
- vertical heating profiles;
- convection;
- circulation;
- humidity response.

Those require later causal systems.

## Scattering semantics

The initial calculation is a single-interaction energy accounting model.

Scattered radiation is partitioned immediately into effective downward and
upward hemispheric components.

It does not model:

- subsequent scattering events;
- diffuse attenuation before reaching the surface;
- scattering altitude;
- angular redistribution inside either hemisphere;
- surface-reflected shortwave re-entering the atmosphere.

These are deliberate limitations.

The names and API of the first model must not imply a full radiative-transfer
solution.

## Relationship to ADR 0011

ADR 0011 remains authoritative for direct-beam Beer-Lambert transmission.

ADR 0012 consumes the same explicit vertical extinction optical depth and
extends the accounting around the extinguished portion of that beam.

The direct result produced by ADR 0012 must be numerically consistent with
`AtmosphericShortwaveTransmissionCalculator`.

The energy-partition model must not introduce a second direct-transmission
formula with different semantics.

## Special cases

### Vacuum or zero optical depth

When:

`tau = 0`

then:

- direct transmission is `1`;
- direct surface factor equals top-of-atmosphere horizontal factor;
- atmospheric absorption is `0`;
- downward diffuse scattering is `0`;
- upward scattering is `0`.

### Pure absorption

When:

`omega = 0`

all extinguished energy is atmospheric absorption.

No diffuse or upward scattered energy is produced.

### Pure scattering

When:

`omega = 1`

no extinguished direct-beam energy is atmospherically absorbed.

All extinguished energy is divided between downward and upward scattering.

### All scattering downward

When:

`omega = 1`
and
`f_down = 1`

the first-order surface-downwelling factor equals the top-of-atmosphere
horizontal factor.

Direct and diffuse proportions may differ, but no shortwave energy is lost from
the surface-directed budget.

### All scattering upward

When:

`omega = 1`
and
`f_down = 0`

all extinguished direct-beam energy is redirected upward.

Only the surviving direct beam reaches the surface.

## Daily-mean partition

The instantaneous partition may be integrated over the daylight interval using
the same deterministic bounded numerical approach established by ADR 0011.

Daily-mean outputs may include:

- top-of-atmosphere shortwave factor;
- direct surface shortwave factor;
- downward diffuse shortwave factor;
- total surface-downwelling shortwave factor;
- atmospheric absorbed shortwave factor;
- upward scattered shortwave factor.

Each output remains dimensionless relative to normal-incidence stellar flux.

The daily-mean partition must preserve the same energy-conservation identity:

`TOA
 = direct surface
 + diffuse surface
 + atmospheric absorption
 + upward scattering`

within numerical tolerance.

## Relationship to surface albedo

Surface albedo is not part of this milestone.

Incoming surface shortwave is deliberately separated from what the surface
later:

- reflects;
- absorbs;
- converts to sensible heat;
- converts to latent heat;
- stores thermally.

This prevents atmospheric transmission and surface material response from
being collapsed into one parameter.

The existing global `PlanetaryEnergyBalanceSystem` remains unchanged.

## Relationship to atmospheric state

The current `AtmosphereState` remains unchanged.

ADR 0012 does not add optical depth, single-scattering albedo, or
downward-scattering fraction directly to `AtmosphereState`.

Those quantities are effective optical model properties.

A future atmospheric-optics provider may derive them from authoritative
physical state and immutable configuration.

That future provider may consider:

- atmospheric column mass;
- pressure;
- gravity;
- composition;
- wavelength;
- stellar spectrum;
- aerosols;
- clouds;
- absorber abundance;
- scattering phase functions.

## State and persistence

The initial energy-partition milestone remains pure deterministic physics.

It does not add:

- new `WorldState`;
- new atmosphere fields;
- new simulation operations;
- new `SimulationDefinition` model configuration;
- snapshot fields;
- timeline archive fields;
- schema-version changes;
- API resources;
- web contracts;
- user controls.

Durable radiation state belongs to the later causal regional-radiation
milestone.

## Initial validation boundary

The first implementation must prove:

- deterministic results for identical inputs;
- rejection of invalid optical depth;
- rejection of single-scattering albedo outside `[0, 1]`;
- rejection of downward-scattering fraction outside `[0, 1]`;
- all energy factors remain finite and non-negative;
- exact zero contribution when the Sun is at or below the horizon;
- zero optical depth produces only direct surface radiation;
- pure absorption sends all extinguished energy to atmospheric absorption;
- pure scattering produces zero atmospheric absorption;
- `f_down = 0` sends no scattered energy toward the surface;
- `f_down = 1` sends no scattered energy upward;
- direct surface radiation matches ADR 0011;
- surface-downwelling shortwave equals direct plus diffuse;
- instantaneous energy conservation holds across representative inputs;
- daily-mean energy conservation holds across representative latitude,
  subsolar-latitude, and optical-property inputs;
- no simulation state is mutated.

## Deferred work

This milestone does not add:

- atmospheric-optics derivation from composition;
- spectral radiative transfer;
- wavelength bands;
- molecular absorption databases;
- Rayleigh-scattering derivation;
- aerosol microphysics;
- cloud optics;
- scattering phase functions;
- asymmetry-parameter physics;
- multiple scattering;
- diffuse-path re-extinction;
- surface reflection;
- atmospheric longwave radiation;
- regional temperature;
- atmospheric temperature profiles;
- convection;
- humidity;
- circulation;
- hydrology coupling;
- biological radiation response.

## Consequences

Est gains an energy-conserving bridge between direct-beam atmospheric
extinction and downstream regional thermal forcing.

The physical shortwave chain becomes:

`stellar flux magnitude`
`-> solar geometry`
`-> direct-beam extinction`
`-> atmospheric absorption + scattering`
`-> direct + diffuse surface downwelling`
`-> surface reflection / absorption`
`-> regional thermal response`

The model remains deliberately replaceable.

More advanced radiative transfer can later improve how optical properties and
angular scattering are derived without changing the core requirement that
incoming shortwave energy be explicitly and conservatively accounted for.
