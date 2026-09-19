# ADR 0011: Atmospheric Shortwave Transmission Foundation

## Status

Accepted.

Decision date: 2026-09-19.

## Context

ADR 0010 established latitude-specific daily-mean top-of-atmosphere solar
geometry.

That foundation deliberately stops at the top of the atmosphere.

The next physical layer is atmospheric attenuation of incoming shortwave
radiation before regional climate or biological consumers are introduced.

Current authoritative `AtmosphereState` contains:

- surface pressure;
- atmospheric composition by mole fraction.

It does not contain:

- spectral absorption cross-sections;
- Rayleigh-scattering coefficients;
- aerosol state;
- aerosol optical properties;
- cloud optical properties;
- ozone-column state;
- wavelength-resolved stellar spectra;
- broadband atmospheric optical depth.

Surface pressure and composition alone are therefore not sufficient to derive
physically justified broadband shortwave transmission without introducing
additional atmospheric-optics assumptions.

The existing `PlanetaryEnergyBalanceSystem` must also remain unchanged during
this foundation milestone. Its current global absorbed-solar calculation is a
coarse planetary thermal baseline and must not be partially modified with a
direct-beam-only atmospheric term.

## Decision

Est will introduce a pure atmospheric shortwave-transmission foundation based
on explicit broadband vertical extinction optical depth.

The first implementation will model clear-sky direct-beam extinction.

It will not claim to represent total downwelling surface shortwave radiation.

The physical chain is:

`stellar flux magnitude`
`-> top-of-atmosphere solar geometry`
`-> atmospheric direct-beam extinction`
`-> future direct + diffuse surface radiation`
`-> future regional climate`

## Vertical optical depth

The first transmission calculation accepts a dimensionless broadband vertical
extinction optical depth:

`tau >= 0`

This value represents effective clear-sky extinction along a vertical
atmospheric path.

The initial milestone does not derive `tau` from `AtmosphereState`.

That derivation requires a separate atmospheric-optics model with enough
information to justify how pressure, composition, gravity, wavelength,
absorption, scattering, and aerosols contribute.

Keeping optical depth explicit prevents hidden Earth-specific assumptions from
entering the generic planet model.

A vacuum corresponds physically to:

`tau = 0`

## Direct-beam transmission

For positive solar-zenith cosine `mu`:

`mu = cos(zenith angle)`

the first model uses the plane-parallel Beer-Lambert relation:

`T_direct = exp(-tau / mu)`

where:

- `tau` is vertical extinction optical depth;
- `1 / mu` is the plane-parallel relative air mass;
- `T_direct` is the fraction of direct-normal radiation remaining after
  atmospheric extinction.

The corresponding transmitted direct horizontal factor is:

`mu * exp(-tau / mu)`

relative to normal-incidence top-of-atmosphere stellar flux.

When the Sun is at or below the geometric horizon:

`mu <= 0`

the direct horizontal contribution is zero.

## Horizon behavior

The plane-parallel air-mass approximation becomes geometrically inaccurate very
near the horizon because real atmospheric curvature, scale height, and
refraction become important.

The first Est implementation will not introduce an Earth-calibrated empirical
air-mass correction.

Instead:

- the model will remain explicitly plane-parallel;
- transmitted horizontal direct flux will approach zero at the horizon;
- the known near-horizon approximation limitation will remain documented;
- a future spherical-atmosphere model may replace the air-mass approximation
  without changing the higher-level transmission contract.

This preserves arbitrary-planet behavior without pretending Earth-specific
refraction or scale-height assumptions are universal.

## Daily-mean surface direct radiation

ADR 0010 already defines the instantaneous geometric solar-zenith relation
through latitude, subsolar latitude, and rotational hour angle.

The atmospheric transmission layer may integrate the transmitted direct
horizontal factor over one full rotation to produce a dimensionless
daily-mean direct surface insolation factor.

For hour angle `h`:

`mu(h) = sin(phi) * sin(delta)
       + cos(phi) * cos(delta) * cos(h)`

Only intervals where `mu(h) > 0` contribute.

The daily-mean direct surface insolation factor is therefore:

`1 / (2*pi) * integral(mu(h) * exp(-tau / mu(h)) dh)`

over one complete rotation, with the integrand equal to zero when the Sun is
below the horizon.

This integral may be evaluated with deterministic bounded numerical quadrature.

The result remains dimensionless relative to normal-incidence
top-of-atmosphere stellar flux.

## Relationship to ADR 0010

Atmospheric transmission does not replace solar geometry.

ADR 0010 remains authoritative for:

- daylight geometry;
- top-of-atmosphere daily-mean insolation factor;
- latitude and subsolar-latitude relationship.

For any non-negative optical depth:

`daily mean direct surface factor <= daily mean TOA factor`

For:

`tau = 0`

the daily-mean direct surface factor must converge to the ADR 0010
top-of-atmosphere daily-mean insolation factor.

This vacuum identity is an architectural invariant.

## Direct versus diffuse radiation

Extinction of the direct beam includes both absorption and scattering out of the
direct solar beam.

Scattered radiation is not necessarily lost from the planetary energy budget.

Therefore the first transmission result must not be named or interpreted as:

- total surface insolation;
- total downwelling shortwave radiation;
- absorbed solar energy;
- atmospheric heating;
- net radiation.

A later radiative-transfer layer may partition atmospheric extinction into:

- absorption;
- scattering;
- direct surface radiation;
- diffuse surface radiation;
- atmospheric energy deposition.

Only after that accounting exists should atmospheric transmission alter the
authoritative regional or global thermal energy budget.

## Relationship to atmospheric state

The current `AtmosphereState` remains unchanged.

Broadband optical depth must not be added directly to `AtmosphereState` merely
to make this milestone convenient.

Optical depth is model-dependent and may vary with:

- wavelength;
- absorber abundance;
- atmospheric column mass;
- gravity;
- aerosols;
- clouds;
- altitude;
- stellar spectrum.

A future atmospheric-optics provider may derive effective optical properties
from authoritative atmospheric state plus immutable model configuration.

That provider is outside this initial milestone.

## State and persistence

The initial atmospheric-transmission milestone is pure deterministic physics.

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

Those become necessary only when an authoritative atmospheric-optics model or
causal surface-radiation state is introduced.

## Initial validation boundary

The first implementation must prove:

- deterministic results for identical inputs;
- rejection of negative or non-finite optical depth;
- direct-beam transmission remains in `[0, 1]`;
- increasing optical depth cannot increase direct transmission;
- decreasing solar elevation cannot increase direct transmission for fixed
  optical depth;
- zero optical depth gives unit direct-normal transmission above the horizon;
- zero optical depth reproduces ADR 0010 daily-mean top-of-atmosphere geometry;
- positive optical depth produces a daily-mean direct surface factor no greater
  than the top-of-atmosphere factor;
- polar night produces zero daily-mean direct surface factor;
- results remain finite and non-negative across representative latitude,
  subsolar-latitude, and optical-depth inputs;
- no simulation state is mutated.

## Deferred work

This milestone does not add:

- derivation of optical depth from atmospheric composition;
- spectral radiative transfer;
- molecular absorption databases;
- Rayleigh-scattering derivation;
- aerosols;
- ozone-column chemistry;
- water-vapor absorption;
- clouds;
- diffuse shortwave radiation;
- multiple scattering;
- atmospheric shortwave heating;
- surface albedo feedback;
- terrain slope or aspect illumination;
- horizon shadowing;
- regional temperature;
- regional humidity;
- atmospheric heat transport;
- photoperiod-driven biology.

## Consequences

Est gains a physically explicit atmospheric-extinction layer without embedding
unsupported atmospheric chemistry or Earth-specific empirical assumptions.

The new layer can attenuate direct solar radiation while preserving the
important distinction between direct-beam extinction and total planetary energy
loss.

Future atmospheric-optics work can derive optical depth from richer
authoritative state without changing the fundamental Beer-Lambert transmission
contract.

Future regional radiation can combine direct and diffuse components before any
thermal or biological consumer becomes causal.
