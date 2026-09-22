# ADR 0015: Regional Surface-Atmosphere Radiative Energy-Budget Topology

## Status

Accepted.

Decision date: 2026-09-19.

## Context

ADRs 0010 through 0014 now provide the physical foundations required to define
a regional radiative energy budget without yet making regional temperature
authoritative.

The established chain can determine:

- latitude-specific top-of-atmosphere shortwave geometry;
- clear-sky direct-beam atmospheric extinction;
- atmospheric shortwave absorption and scattering;
- direct plus diffuse surface-downwelling shortwave;
- surface shortwave reflection and absorption;
- graybody longwave emission;
- thermal-reservoir response to signed net heat flux.

ADR 0014 deliberately stopped before creating regional thermal state.

ADR 0015 therefore defines the regional radiative topology while remaining
independent from thermal-state authority.

ADR 0016 defines the durable regional thermal state, model policy, persistence,
causal stepping, and mutually exclusive thermal-authority migration that consume
this topology.

## Decision

The first regional thermal architecture will use two conceptual thermal
reservoirs for each authoritative planet-surface cell:

1. a surface-system thermal reservoir;
2. a vertically integrated atmospheric-column thermal reservoir.

Space is an external radiative sink.

Space is not a thermal reservoir in `WorldState`.

The initial energy-budget topology is local to each surface cell.

There is no horizontal heat transport in the first model.

## Surface-system thermal reservoir

The surface-system reservoir represents unresolved thermally active material at
or near the planetary surface.

Depending on future physical state, that effective reservoir may eventually
represent some combination of:

- vegetation;
- soil;
- rock;
- snow;
- ice;
- shallow liquid water;
- an ocean mixed layer;
- constructed material;
- other near-surface material.

ADR 0015 does not yet decide the physical depth or material composition of that
reservoir.

It does not derive its heat capacity from existing hydrology, vegetation,
terrain, or other simulation state.

Its effective areal heat capacity remains an explicit future model property.

Its conceptual temperature will be called:

`T_surface`

The eventual durable regional surface temperature must be associated with
`SurfaceCellId`.

It must not be stored on `SurfaceCell` itself.

`SurfaceCell` remains geometry and topology only.

## Atmospheric-column thermal reservoir

Each surface cell also has one conceptual vertically integrated atmospheric
thermal reservoir.

It represents the effective thermal state of the atmospheric column above that
surface cell.

Its conceptual temperature will be called:

`T_atmosphere`

The first architecture deliberately does not model:

- vertical atmospheric layers;
- lapse rates within the atmospheric column;
- stratosphere versus troposphere;
- explicit pressure levels;
- convective adjustment;
- vertical moisture transport.

The atmospheric-column reservoir is therefore a one-layer effective model.

Existing `AtmosphereState` remains the coarse planetary atmospheric-composition
and pressure state.

ADR 0015 does not add regional temperature fields to `AtmosphereState`.

Existing per-cell hydrology atmospheric water is a water-mass reservoir.

It must not be reinterpreted as atmospheric thermal state.

## Space boundary

Space is modeled as an external radiative sink.

Energy crossing the top-of-atmosphere longwave boundary into space leaves the
modeled thermal system.

The first model does not include:

- incoming longwave radiation from space;
- cosmic background heating;
- external infrared irradiation from another body.

The stellar energy source remains shortwave and is handled through the
established stellar-flux and shortwave chain.

## Shortwave inputs to the regional thermal budget

ADR 0012 produces atmospheric absorbed-shortwave factors.

ADR 0013 produces surface-system absorbed-shortwave factors.

Those factors remain dimensionless relative to normal-incidence stellar flux.

Downstream regional thermal composition converts them into physical fluxes
using the configured authoritative stellar-flux magnitude:

`Q_sw_atmosphere = StellarFluxWattsPerSquareMeter * F_absorbed_atmosphere`

`Q_sw_surface = StellarFluxWattsPerSquareMeter * F_absorbed_surface`

The result is in:

`W/m^2`

The regional energy-budget calculation itself will consume those physical
absorbed fluxes.

It will not own stellar-flux magnitude.

It will not apply an additional spherical `/4` factor.

The regional solar-geometry chain has already accounted for spatial illumination
geometry.

Applying `/4` again would double-count spherical geometry.

## Separation of shortwave and longwave optical properties

Shortwave and longwave optical properties remain distinct.

Atmospheric:

- shortwave extinction optical depth;
- shortwave single-scattering albedo;
- shortwave downward-scattering fraction;

remain ADR 0011 and ADR 0012 concepts.

Surface shortwave albedo remains an ADR 0013 concept.

ADR 0015 introduces separate effective broadband longwave optical properties.

No shortwave property is silently reused as a longwave property.

## First-order longwave model

The first regional topology will use a one-layer gray longwave atmosphere.

For the surface:

`epsilon_surface`

is the effective broadband longwave emissivity.

For the atmospheric column:

`epsilon_atmosphere`

is the effective broadband longwave emissivity.

Both must satisfy:

`0 <= epsilon <= 1`

The first model applies Kirchhoff-consistent gray closure within the longwave
band.

For the opaque surface:

`longwave absorptivity = epsilon_surface`

and:

`longwave reflectivity = 1 - epsilon_surface`

The first model exposes no longwave transmission through the surface.

For the atmospheric layer:

`longwave absorptivity = epsilon_atmosphere`

and:

`longwave transmissivity = 1 - epsilon_atmosphere`

The first atmospheric layer has no longwave reflection.

These are broadband effective model properties.

They are not derived yet from material composition or atmospheric spectroscopy.

## Emitted longwave flux

ADR 0014 remains authoritative for graybody emission.

Define:

`E_surface = epsilon_surface * sigma * T_surface^4`

and:

`E_atmosphere = epsilon_atmosphere * sigma * T_atmosphere^4`

Each quantity is a hemispheric emitted flux in:

`W/m^2`

`E_surface` is emitted upward from the surface.

`E_atmosphere` is the atmospheric emission from one face of the effective
atmospheric layer.

The one-layer atmosphere emits:

- `E_atmosphere` upward;
- `E_atmosphere` downward.

Its total longwave emission loss is therefore:

`2 * E_atmosphere`

The regional budget must reuse the ADR 0014 graybody emission calculation rather
than duplicating Stefan-Boltzmann physics.

## Surface longwave emission path

Surface-emitted longwave radiation enters the atmospheric column.

The atmosphere absorbs:

`Q_surface_lw_absorbed_by_atmosphere
 = epsilon_atmosphere * E_surface`

The remainder transmits directly to space:

`Q_surface_lw_direct_to_space
 = (1 - epsilon_atmosphere) * E_surface`

These two paths conserve surface-emitted longwave energy:

`E_surface
 = Q_surface_lw_absorbed_by_atmosphere
 + Q_surface_lw_direct_to_space`

## Downward atmospheric longwave path

The atmosphere emits downward:

`E_atmosphere`

The surface absorbs:

`Q_atmosphere_lw_absorbed_by_surface
 = epsilon_surface * E_atmosphere`

The opaque surface reflects the remainder upward:

`Q_atmosphere_lw_reflected_by_surface
 = (1 - epsilon_surface) * E_atmosphere`

These two paths conserve the downward atmospheric emission:

`E_atmosphere
 = Q_atmosphere_lw_absorbed_by_surface
 + Q_atmosphere_lw_reflected_by_surface`

## Reflected atmospheric longwave path

The upward longwave reflected from the surface passes through the atmospheric
layer.

The atmosphere absorbs:

`Q_reflected_lw_reabsorbed_by_atmosphere
 = epsilon_atmosphere
   * Q_atmosphere_lw_reflected_by_surface`

The remainder transmits to space:

`Q_reflected_lw_to_space
 = (1 - epsilon_atmosphere)
   * Q_atmosphere_lw_reflected_by_surface`

The atmospheric layer does not reflect longwave radiation in this first model.

Therefore this optical reflection path terminates without an infinite
surface-atmosphere reflection series.

Reabsorbed energy affects the atmospheric energy budget and may influence later
temperature evolution, but it is not instantaneously re-emitted again within
the same algebraic radiation evaluation.

## Upward atmospheric longwave path

The upward-facing atmospheric emission:

`E_atmosphere`

crosses directly to space in the one-layer model.

The effective layer does not reabsorb its own outward hemispheric emission
before that emission reaches the external space boundary.

This is the standard closure of the first isothermal gray-layer topology.

## Surface radiative net flux

Positive net flux means energy enters the surface thermal reservoir.

The surface radiative net flux is:

`Q_surface_net
 = Q_sw_surface
 + Q_atmosphere_lw_absorbed_by_surface
 - E_surface`

or:

`Q_surface_net
 = Q_sw_surface
 + epsilon_surface * E_atmosphere
 - E_surface`

This is a radiative flux only.

ADR 0015 does not yet add:

- sensible heat;
- latent heat;
- conduction;
- phase-change energy;
- geothermal energy;
- biological heat.

## Atmospheric radiative net flux

Positive net flux means energy enters the atmospheric thermal reservoir.

The atmospheric radiative net flux is:

`Q_atmosphere_net
 = Q_sw_atmosphere
 + Q_surface_lw_absorbed_by_atmosphere
 + Q_reflected_lw_reabsorbed_by_atmosphere
 - 2 * E_atmosphere`

or:

`Q_atmosphere_net
 = Q_sw_atmosphere
 + epsilon_atmosphere * E_surface
 + epsilon_atmosphere
   * (1 - epsilon_surface)
   * E_atmosphere
 - 2 * E_atmosphere`

The atmospheric reservoir therefore receives:

- atmospheric shortwave absorption;
- absorbed surface-emitted longwave;
- reabsorbed surface-reflected atmospheric longwave.

It loses longwave energy by emission from both faces.

## Outgoing longwave radiation to space

The first regional model defines top-of-atmosphere outgoing longwave radiation:

`Q_lw_to_space
 = Q_surface_lw_direct_to_space
 + E_atmosphere
 + Q_reflected_lw_to_space`

or:

`Q_lw_to_space
 = (1 - epsilon_atmosphere) * E_surface
 + E_atmosphere
 + (1 - epsilon_atmosphere)
   * (1 - epsilon_surface)
   * E_atmosphere`

This quantity is the thermal-radiation energy leaving the modeled
surface-atmosphere system.

It is not yet aggregated into a global planetary observable.

A later diagnostic may area-weight cell values to obtain global outgoing
longwave flux.

## Cell-level radiative conservation

The regional radiative topology must conserve energy.

For each cell:

`Q_sw_surface + Q_sw_atmosphere
 = Q_surface_net
 + Q_atmosphere_net
 + Q_lw_to_space`

within floating-point tolerance.

Internal surface-atmosphere transfers must cancel from the combined
surface-plus-atmosphere budget.

This conservation identity is an architectural invariant.

It is not merely a validation convenience.

## Relationship to ADR 0014 thermal-reservoir response

ADR 0015 determines signed net radiative fluxes for the two conceptual
reservoirs.

ADR 0014 remains authoritative for converting each net flux into a thermal
response.

For a future surface reservoir with effective areal heat capacity:

`C_surface`

the future integration may use:

`Delta_T_surface
 = Q_surface_net * dt / C_surface`

For a future atmospheric reservoir with effective areal heat capacity:

`C_atmosphere`

the future integration may use:

`Delta_T_atmosphere
 = Q_atmosphere_net * dt / C_atmosphere`

ADR 0015 does not yet perform those integrations causally against `WorldState`.

## Durable regional thermal state

Regional thermal state follows the established planet-associated
surface-field pattern defined by ADR 0016.

The intended structure is conceptually:

`PlanetRegionalThermalState`

containing:

- `PlanetId`;
- `SurfaceGridDefinition`;
- exactly one thermal cell state for every authoritative surface cell.

Each regional thermal cell state requires at minimum:

- `SurfaceCellId`;
- surface-system temperature;
- atmospheric-column temperature.

The exact type names are not mandated by this ADR.

Durable thermal state must:

- belong to a valid planet;
- preserve the surface-grid definition;
- contain no duplicate surface-cell identities;
- contain exactly one state for each surface cell;
- reject invalid temperatures;
- survive world copy and fork;
- survive snapshots and timeline archives;
- remain immutable through simulation operations.

The surface grid itself remains geometry and topology only.

## Regional thermal model policy

Thermal model policy belongs in `SimulationDefinition`, not in individual
thermal-state cells.

Per-planet regional thermal policy owns effective parameters such as:

- surface longwave emissivity;
- atmospheric longwave emissivity;
- surface effective areal heat capacity;
- atmospheric effective areal heat capacity;
- maximum thermal integration step.

Those parameters may later become derived or spatially varying.

The first durable model should not hide Earth-specific constants inside the
thermal-state records.

Regional thermal model policy is persisted with timeline archives so replay
and branching preserve thermal semantics.

## Relationship to hydrology, terrain, and vegetation

Terrain, hydrology, vegetation, and biogeochemistry already occupy the
authoritative shared surface topology.

Regional thermal state uses the same grid definition for a planet.

ADR 0015 does not yet derive thermal properties from:

- terrain elevation;
- standing water;
- water depth;
- soil water;
- snow or ice;
- vegetation;
- biomass;
- substrate;
- constructed structures.

Those dependencies require deliberate thermal-property providers.

The first topology must not introduce hidden fixed thermal values for ocean,
lake, soil, rock, vegetation, snow, ice, or cities.

## Atmospheric water remains hydrology state

`HydrologyCellState` currently tracks atmospheric water mass as part of the
conservative water cycle.

That water mass remains hydrology state.

The future atmospheric thermal reservoir may eventually consume atmospheric
water as a radiative or heat-capacity input.

It must not duplicate atmospheric water mass inside regional thermal state.

This preserves one authoritative owner for water mass.

## No horizontal transport yet

The first regional radiative topology is column-local.

A cell does not exchange heat radiatively or dynamically with neighboring
cells.

ADR 0015 therefore does not add:

- atmospheric advection;
- atmospheric circulation;
- ocean heat transport;
- horizontal conduction;
- turbulent horizontal mixing.

Those processes are necessary for a mature climate model but are separate from
the local radiative energy-budget topology.

## No vertical non-radiative exchange yet

The surface and atmospheric reservoirs interact only radiatively in the first
topology.

The first model does not add:

- sensible heat exchange;
- latent heat exchange;
- evaporation energy;
- condensation heating;
- convection;
- conduction between surface and atmosphere;
- turbulent boundary-layer exchange.

Those transfers must later enter both reservoir budgets with equal and opposite
signs so combined energy remains conserved.

## No phase-change energetics yet

Existing hydrology already moves water between liquid and snow/ice stores based
on thermal boundary conditions.

ADR 0015 does not yet attach latent heat to those transfers.

Freezing and melting therefore remain thermodynamically incomplete until a
later coupling milestone explicitly exchanges latent energy with the relevant
thermal reservoir.

The regional radiative foundation must not silently invent such coupling.

## No surface-property derivation yet

The first regional energy budget accepts longwave emissivities explicitly.

It does not derive them from shortwave albedo or existing physical state.

Future surface-property providers may evaluate longwave emissivity and thermal
capacity from authoritative state such as:

- standing-water kind and depth;
- snow or ice mass;
- soil moisture;
- substrate;
- vegetation;
- structures.

Those providers must preserve the distinction between:

- authoritative material state;
- derived thermal or optical properties;
- radiative energy accounting;
- evolving thermal state.

## No atmospheric spectroscopy yet

The first atmospheric longwave emissivity is an effective broadband parameter.

It is not yet derived from:

- atmospheric gas composition;
- carbon dioxide concentration;
- water vapor;
- methane;
- pressure;
- temperature;
- pressure broadening;
- collision-induced absorption;
- spectral windows;
- clouds.

Those require a richer atmospheric-radiation model.

The one-layer gray topology is an explicit first-order closure, not a claim that
real atmospheres are gray or isothermal.

## Thermal authority boundary

ADR 0015 itself does not make regional temperature authoritative.

Its regional radiative calculation is pure physical accounting and does not
write `PlanetEnvironment`, hydrology, vegetation, biogeochemistry, population,
or animal state.

ADR 0016 defines thermal authority and enforces that one planet cannot run
planetary energy-balance and regional thermal models as competing causal
temperature writers.

## Thermal authority migration

ADR 0016 performs the thermal-authority migration anticipated by this topology.

For a regionally authoritative planet:

- `PlanetaryEnergyBalanceSystem` is not the causal temperature writer;
- regional thermal state owns causal surface and atmospheric temperatures;
- the regional model composes the radiative calculations defined here;
- the planetary mean remains available only through an explicit compatibility
  rule.

This preserves the core invariant that a planet has one causal thermal
authority.

## Compatibility meaning of MeanSurfaceTemperatureKelvin

Under regional thermal authority,
`PlanetEnvironment.MeanSurfaceTemperatureKelvin` is derived from authoritative
regional surface temperatures as an area-weighted compatibility value.

It is not an independent evolving thermal truth.

## Consumer migration

ADR 0017 makes hydrology consume regional surface temperature when regional
thermal authority is configured.

Other systems may continue to consume the compatibility planetary mean until a
deliberate regional consumer contract is introduced for them.

Consumer migration must preserve an explicit authority rule rather than allowing
some systems to read unrelated competing temperature truths.

## Simulation ordering

The configured causal order is:

`astronomical / seasonal context`
`-> regional radiative forcing`
`-> regional thermal integration`
`-> hydrology`
`-> vegetation / biogeochemistry`
`-> consumers`
`-> consequences`

This preserves the architectural direction in which climate provides thermal
boundary conditions to hydrology and biology.

ADR 0015 itself remains a pure radiative topology; ADRs 0016 and 0017 establish
the stateful thermal and hydrology ordering.

## Persistence boundary

ADR 0015 itself introduces no persisted regional thermal state.

ADR 0016 adds durable regional thermal state and deliberately versions both
world snapshots and timeline archives around that authority.

The current spatial-state persistence pattern already preserves planet identity,
surface-grid definition, and per-cell state for terrain, hydrology, vegetation,
and biogeochemistry.

Regional thermal state should follow the same ownership pattern rather than
inventing a parallel persistence topology.

Regional thermal model policy must likewise persist with the simulation
definition in timeline archives.

## First implementation boundary

The implementation boundary of ADR 0015 remains pure deterministic physics.

It adds a regional radiative energy-budget calculation that consumes:

- physical surface absorbed-shortwave flux in `W/m^2`;
- physical atmospheric absorbed-shortwave flux in `W/m^2`;
- surface temperature;
- atmospheric temperature;
- surface effective broadband longwave emissivity;
- atmospheric effective broadband longwave emissivity.

It will produce enough explicit terms to verify:

- surface longwave emission;
- atmospheric one-face longwave emission;
- surface longwave absorbed by atmosphere;
- surface longwave transmitted directly to space;
- atmospheric downward longwave absorbed by surface;
- atmospheric downward longwave reflected by surface;
- reflected longwave reabsorbed by atmosphere;
- reflected longwave transmitted to space;
- surface net radiative flux;
- atmospheric net radiative flux;
- outgoing longwave radiation to space;
- combined cell-level radiative conservation.

The implementation reuses ADR 0014 graybody emission.

ADR 0015 itself does not create durable regional thermal state.

## Initial validation boundary

The first regional radiative-budget implementation must prove:

- deterministic results for identical inputs;
- rejection of negative or non-finite absorbed-shortwave fluxes;
- rejection of invalid surface temperature;
- rejection of invalid atmospheric temperature;
- rejection of surface emissivity outside `[0, 1]`;
- rejection of atmospheric emissivity outside `[0, 1]`;
- every explicit radiative-transfer magnitude is finite and non-negative;
- surface and atmospheric net fluxes may be signed;
- zero temperatures produce zero longwave emission;
- `epsilon_atmosphere = 0` produces complete atmospheric longwave transparency
  and no atmospheric longwave emission;
- `epsilon_atmosphere = 1` eliminates direct transmission of surface longwave to
  space;
- `epsilon_surface = 0` produces no surface thermal emission and complete
  reflection of downward atmospheric longwave;
- `epsilon_surface = 1` produces complete absorption of downward atmospheric
  longwave and no surface longwave reflection;
- surface-emission path conservation holds;
- downward-atmosphere path conservation holds;
- reflected-longwave path conservation holds;
- combined cell-level radiative conservation holds across representative
  boundary and interior cases;
- ADR 0014 emission results are reused rather than independently reimplemented;
- no simulation state is mutated.

## Deferred work

ADR 0015 does not add:

- authoritative regional temperature state;
- thermal state in `WorldState`;
- thermal simulation operations;
- thermal model definitions;
- snapshot or timeline schema changes;
- API contracts;
- web contracts;
- user controls;
- surface thermal-property derivation;
- atmospheric longwave-property derivation;
- spectral radiative transfer;
- multiple atmospheric layers;
- clouds;
- longwave atmospheric reflection;
- shortwave clouds;
- terrain slope illumination;
- horizon shadowing;
- diurnal thermal cycles;
- sensible heat;
- latent heat;
- evaporation energy;
- condensation heating;
- convection;
- surface-atmosphere conduction;
- soil heat diffusion;
- ocean mixed-layer dynamics;
- ocean heat transport;
- horizontal atmospheric transport;
- atmospheric circulation;
- geothermal heat;
- phase-change latent heat;
- hydrology-energy coupling;
- regional-temperature consumer migration.

## Consequences

Est gains an explicit regional surface-atmosphere-space radiative topology
without prematurely creating a second causal climate authority.

The regional physical chain becomes conceptually:

`stellar flux magnitude`
`-> solar geometry`
`-> atmospheric shortwave partition`
`-> surface shortwave partition`
`-> physical atmospheric + surface absorbed shortwave`
`-> surface / atmospheric longwave exchange`
`-> outgoing longwave radiation to space`
`-> signed surface + atmospheric net radiative flux`
`-> ADR 0014 thermal-reservoir response`
`-> authoritative regional thermal state when configured`

ADR 0015 remains a pure energy-accounting layer.

ADR 0016 provides the stateful regional thermal authority built on this
topology, and ADR 0017 connects that authority to hydrology.
