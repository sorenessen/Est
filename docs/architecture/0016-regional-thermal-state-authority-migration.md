# ADR 0016: Regional Thermal State and Thermal-Authority Migration

## Status

Accepted.

Decision date: 2026-09-19.

## Context

ADRs 0010 through 0015 now establish the pure physical foundations needed for
regional thermal evolution.

The existing chain can determine:

- latitude-specific daily-mean top-of-atmosphere shortwave geometry;
- atmospheric shortwave transmission, absorption, and scattering;
- surface shortwave reflection and absorption;
- graybody surface and atmospheric longwave emission;
- explicit surface-atmosphere-space longwave transfer;
- signed net radiative flux for a surface thermal reservoir;
- signed net radiative flux for an atmospheric-column thermal reservoir;
- thermal-reservoir temperature response to a signed net heat flux.

Those foundations are deliberately pure.

They do not yet own evolving regional temperature state.

The current authoritative thermal state remains:

`PlanetEnvironment.MeanSurfaceTemperatureKelvin`

and the current causal thermal writer remains:

`PlanetaryEnergyBalanceSystem`

Current hydrology and biological systems consume that planetary mean directly or
derive coarse local temperature from it.

ADR 0015 established the next topology:

- one surface-system thermal reservoir per surface cell;
- one vertically integrated atmospheric-column thermal reservoir per surface
  cell;
- space as an external radiative sink;
- no horizontal transport yet;
- no vertical non-radiative transport yet.

The next milestone must establish durable regional thermal state and define how
thermal authority migrates without allowing two independent causal climate
models to write the same conceptual planet.

## Decision

Est will introduce durable regional thermal state associated with the existing
authoritative surface grid.

Regional thermal state will contain two temperatures for every surface cell:

1. surface-system temperature;
2. atmospheric-column temperature.

The presence of regional thermal state alone does not make it authoritative.

Thermal authority is selected by simulation model configuration.

A planet may use either:

- the existing zero-dimensional planetary energy-balance model; or
- the regional thermal model.

A planet may not use both as causal thermal writers at the same time.

## Regional thermal cell state

The future cell state will be represented conceptually as:

`RegionalThermalCellState`

with:

- `SurfaceCellId`;
- `SurfaceTemperatureKelvin`;
- `AtmosphericTemperatureKelvin`.

Both temperatures must be:

- finite;
- at or above absolute zero.

The cell state stores thermal state only.

It does not own:

- heat capacity;
- emissivity;
- albedo;
- atmospheric optical depth;
- water mass;
- terrain;
- vegetation;
- material composition.

Those remain model policy, derived properties, or authoritative state owned by
their existing subsystems.

## Planet regional thermal state

The planet-level durable state will be represented conceptually as:

`PlanetRegionalThermalState`

containing:

- `PlanetId`;
- `SurfaceGridDefinition`;
- exactly one `RegionalThermalCellState` for every authoritative surface cell.

The state must:

- belong to a valid planet;
- preserve the surface-grid definition;
- reject null cell entries;
- reject duplicate surface-cell identities;
- contain exactly one thermal state for every surface cell;
- reject cells outside the declared surface grid;
- survive world copy and fork;
- survive snapshots, checkpoints, archives, replay, and branching.

`SurfaceCell` remains geometry and topology only.

Thermal state must not be stored directly on `SurfaceCell`.

## Shared surface topology

Regional thermal state will use the same planet-surface grid as terrain.

A world containing regional thermal state for a planet must also contain terrain
for that planet.

Their `SurfaceGridDefinition` values must match.

Regional thermal state does not require hydrology to exist.

Dry planets and planets without detailed water-cycle state must remain valid
thermal worlds.

When hydrology does exist, it must use the same surface-grid definition through
the existing world-state invariants.

## WorldState ownership

`WorldState` owns evolving regional thermal state.

The world will therefore eventually expose a collection conceptually equivalent
to:

`ImmutableArray<PlanetRegionalThermalState> RegionalThermal`

Only one regional thermal state may exist for a given planet.

World construction must validate planet identity and shared surface-grid
compatibility.

World copy and fork must preserve regional thermal state exactly, just as they
preserve terrain, hydrology, vegetation, biogeochemistry, and seasonal state.

## Regional thermal replacement operation

Regional thermal state will be changed only through immutable simulation
operations.

The first operation will conceptually replace the regional thermal state for
one planet while preserving thermal state belonging to other planets.

It must:

- reject unknown planets;
- preserve world identity;
- preserve simulation time;
- route final validation through `WorldState`.

A causal regional thermal system must not mutate cell state in place.

The Phase A replacement operation changes regional thermal state only.

Because Phase A does not activate regional thermal authority, it does not
rewrite `PlanetEnvironment.MeanSurfaceTemperatureKelvin`.

## Initialization

Regional thermal state requires an explicit deterministic initialization rule.

The first initializer will seed every surface cell from the current planetary
thermal baseline.

For every surface cell:

`T_surface_initial = PlanetEnvironment.MeanSurfaceTemperatureKelvin`

and:

`T_atmosphere_initial = PlanetEnvironment.MeanSurfaceTemperatureKelvin`

This is a compatibility initialization rule.

It is not a claim that a real atmospheric column has the same physical
temperature as the surface.

The purpose is to create regional state without introducing:

- hidden Earth-specific atmospheric temperatures;
- arbitrary lapse rates;
- terrain-derived temperature gradients;
- water-dependent heat-capacity assumptions;
- an unrecorded equilibrium solver.

The regional model may then evolve away from this uniform initial condition
according to its configured energy budget.

A later initializer may derive a richer state from elevation, pressure, water,
or a radiative-equilibrium solution.

Such initialization must remain explicit.

## No implicit session initialization

Constructing a simulation session must not silently create regional thermal
state.

If a regional thermal model is configured, required regional thermal state must
already exist in the world.

This preserves deterministic snapshot, replay, and branch semantics.

World creation or an explicit migration operation may initialize regional
thermal state before a regional model is activated.

## Thermal authority

Thermal authority is determined by configured causal model ownership.

For a given planet:

### Planetary energy-balance authority

If a `PlanetaryEnergyBalanceModelDefinition` is configured and no regional
thermal model is configured, the existing zero-dimensional
`PlanetaryEnergyBalanceSystem` remains authoritative.

It continues to evolve:

- `PlanetEnvironment.MeanSurfaceTemperatureKelvin`;
- `PlanetEnvironment.IceCoverageFraction`;

using its existing semantics.

Regional thermal state may be absent.

If regional thermal state is present while the planetary EBM remains active,
that regional state is dormant migration state.

It is not causally advanced and must not be consumed as authoritative climate.

### Regional thermal authority

If a regional thermal model is configured, the regional thermal system becomes
the only causal thermal writer for that planet.

A `PlanetaryEnergyBalanceModelDefinition` must not also be configured for that
planet.

The existing `PlanetaryEnergyBalanceSystem` therefore must not be constructed
for that planet.

This mutual exclusion is an architectural invariant.

## SimulationDefinition mutual exclusion

`SimulationDefinition` already prevents duplicate model definitions of the same
kind for one planet.

Regional thermal configuration will extend validation so a planet cannot appear
in both:

- `PlanetaryEnergyBalanceModels`;
- `RegionalThermalModels`.

Attempting to configure both must fail before causal execution begins.

This prevents ordering from becoming an accidental authority-selection
mechanism.

## Regional thermal model definition

Regional thermal policy belongs in `SimulationDefinition`, not in durable
thermal state.

The regional model will conceptually use:

`RegionalThermalModelDefinition`

containing:

- `PlanetId`;
- `RegionalThermalModelParameters`.

The first parameter set will remain explicit and spatially uniform.

It will include:

- stellar flux in `W/m^2`;
- atmospheric shortwave extinction optical depth;
- atmospheric shortwave single-scattering albedo;
- atmospheric shortwave downward-scattering fraction;
- surface shortwave albedo;
- surface longwave emissivity;
- atmospheric longwave emissivity;
- surface effective areal heat capacity in `J m^-2 K^-1`;
- atmospheric effective areal heat capacity in `J m^-2 K^-1`;
- maximum regional thermal integration step in seconds.

No Earth-specific defaults are required by this ADR.

Callers and creation policy must provide deliberate parameters.

## Stellar-flux authority migration

Earlier shortwave ADRs identified:

`PlanetaryEnergyBalanceParameters.StellarFluxWattsPerSquareMeter`

as the current stellar-flux authority.

That remains true while the planetary EBM is the configured thermal model.

Once a planet migrates to regional thermal authority, there is no
`PlanetaryEnergyBalanceModelDefinition` for that planet.

For that mode, the regional thermal model's configured stellar-flux parameter
becomes the sole stellar-flux authority for the planet.

There must never be two simultaneously active stellar-flux magnitudes for one
planet.

This ADR therefore changes stellar-flux ownership from one globally named
parameter type to one active thermal-model authority per planet.

The regional radiative calculator from ADR 0015 still does not own stellar-flux
magnitude.

It receives already physical shortwave fluxes in `W/m^2`.

## Shortwave forcing under regional authority

The regional thermal causal system will compose the existing pure solar
foundations.

For each cell and thermal substep it will derive daily-mean shortwave using:

- surface-cell latitude;
- a physical subsolar latitude;
- ADR 0010 surface solar geometry;
- ADR 0011 atmospheric shortwave transmission;
- ADR 0012 atmospheric shortwave energy partition;
- ADR 0013 surface shortwave reflection and absorption;
- the active regional model's stellar-flux magnitude.

The resulting dimensionless absorbed factors become physical fluxes:

`Q_sw_atmosphere
 = StellarFluxWattsPerSquareMeter
   * F_absorbed_atmosphere`

and:

`Q_sw_surface
 = StellarFluxWattsPerSquareMeter
   * F_absorbed_surface`

No additional global `/4` factor is applied.

The regional geometry already represents spatially resolved daily-mean solar
forcing.

## Astronomical signal

Regional radiative forcing requires a physical subsolar latitude.

The preferred source is seasonal astronomical state already produced by the
configured seasonal provider.

The thermal system must resolve the physical signal as follows:

1. if the effective seasonal context contains `SubsolarLatitudeDegrees`, use it;
2. otherwise, if Override is effective, a matching configured seasonal model is
   actively maintaining the underlying derived context, and that derived
   context contains `SubsolarLatitudeDegrees`, use that natural astronomical
   signal;
3. otherwise, reject regional thermal evaluation for that planet.

This allows a seasonal override that changes labels or other seasonal context
without accidentally erasing actively maintained physical astronomy.

An override that explicitly supplies subsolar latitude may intentionally alter
regional solar forcing.

A configured Derived seasonal model is the normal continuing natural source
because it refreshes physical astronomy as simulation time advances.

A persisted Override state whose effective context explicitly contains
`SubsolarLatitudeDegrees` is also a valid continuing source without a seasonal
model. In that case the fixed astronomical forcing is deliberate override
state.

A persisted Derived context by itself is not sufficient continuing astronomy.

Without its matching configured seasonal model, that derived value would become
stale as simulation time advances and must not be treated as naturally evolving
orbital state.

Likewise, an Override lacking its own subsolar latitude may fall back to
`DerivedContext` only while a matching configured seasonal model is actively
maintaining that derived context.

If an Override is the only available source and that override is later removed,
regional thermal evaluation must fail unless a configured physical
subsolar-latitude source has become available.

The thermal system must not invent an equatorial subsolar latitude when no
physical signal exists.

## Causal ordering

The existing causal pipeline evaluates systems sequentially against the working
world.

Regional thermal authority will preserve the order:

`seasonal astronomy`
`-> thermal authority`
`-> hydrology`
`-> biogeochemistry`
`-> vegetation`
`-> consumers`
`-> consequences`

For different planets, planetary EBM systems and regional thermal systems may
coexist in the same simulation definition.

For the same planet, they may not.

Regional thermal evaluation must occur after seasonal state is updated and
before hydrology.

## Regional radiative calculation

For each surface cell, the regional thermal system will use ADR 0015 to compute:

- surface net radiative flux;
- atmospheric net radiative flux;
- outgoing longwave radiation to space;
- explicit conservation error.

ADR 0015 remains authoritative for the one-layer gray
surface-atmosphere-space radiative topology.

The causal system must not reimplement those equations.

## Thermal integration

The regional thermal system will use ADR 0014 thermal-reservoir response for
both reservoirs.

For each bounded substep:

`T_surface_next
 = T_surface
   + Q_surface_net * dt / C_surface`

and:

`T_atmosphere_next
 = T_atmosphere
   + Q_atmosphere_net * dt / C_atmosphere`

The ADR 0014 calculator remains authoritative for validation of the thermal
response.

The regional system must not clamp an invalid temperature.

If a configured step would produce a non-finite temperature or a temperature
below absolute zero, evaluation must fail rather than silently alter the
physics.

## Bounded integration

Regional thermal integration will use a configurable maximum integration step.

Long advances must be divided deterministically into bounded thermal substeps.

Every substep must:

1. read the current regional thermal state;
2. evaluate all cell radiative budgets from that substep's starting state;
3. compute new surface and atmospheric temperatures;
4. form the next complete planet thermal state;
5. continue from that complete next state.

Cell iteration order must not create causal differences.

The first model has no horizontal heat transfer, but simultaneous state
replacement remains the required pattern for future coupling.

## Compatibility mean surface temperature

Once regional thermal authority is active,
`PlanetEnvironment.MeanSurfaceTemperatureKelvin` must stop being an independent
thermal state variable.

It becomes a compatibility projection derived from regional surface
temperature.

After regional thermal integration:

`T_mean
 = sum(T_surface_cell * cell_area)
   / sum(cell_area)`

The regional thermal operation will update the planet environment so:

`MeanSurfaceTemperatureKelvin = T_mean`

This derived value exists so current downstream systems can continue operating
during staged migration.

It must not be used as an input to advance regional thermal state.

The causal direction is:

`regional surface temperatures`
`-> area-weighted compatibility mean`

and never:

`compatibility mean`
`-> overwrite regional temperatures`

after regional authority has been activated.

## Atomic regional-authority world update

Once regional thermal authority is active, one causal evaluation produces two
logically coupled authoritative consequences:

1. replacement of the planet's regional thermal state;
2. projection of the area-weighted regional surface temperature into
   `PlanetEnvironment.MeanSurfaceTemperatureKelvin`.

Those consequences must be applied as one atomic simulation operation.

The Phase C regional thermal system must therefore return one operation capable
of replacing the regional thermal state and updating the target planet's
compatibility environment in the same `WorldState` transition.

The exact operation name is not mandated by this ADR.

Conceptually it may resemble:

`ReplacePlanetRegionalThermalStateAndEnvironmentOperation`

That operation must:

- preserve world identity;
- preserve simulation time;
- replace only the target planet's regional thermal state;
- change only `MeanSurfaceTemperatureKelvin` within the target
  `PlanetEnvironment`;
- preserve `SurfaceWaterFraction`;
- preserve `IceCoverageFraction`;
- preserve `AtmosphereState`;
- preserve regional thermal state belonging to other planets;
- route the resulting complete state through normal `WorldState` validation.

The regional causal system must not emit one thermal-state operation followed by
a separate planet-environment operation.

Doing so would expose an intermediate world in which regional thermal state and
the compatibility mean disagree, and downstream causal systems could observe
that mixed-authority state.

Atomic replacement makes the authority boundary explicit.

## Existing hydrology and biological consumers

ADR 0016 does not yet migrate current consumers to cell-local temperature.

Hydrology, vegetation, biogeochemistry, and other existing systems may continue
reading the compatibility planetary mean under regional thermal authority.

Because regional thermal evaluation occurs before them, they receive the
updated area-weighted compatibility value for the current causal step.

This is a migration bridge.

It is not the final regional climate coupling.

A later consumer-migration milestone may replace that global input with
cell-local surface or atmospheric temperature where physically appropriate.

## IceCoverageFraction under regional authority

The current planetary EBM owns a coarse temperature-driven
`IceCoverageFraction` response.

When regional thermal authority is active, that EBM feedback is disabled with
the rest of the planetary EBM.

ADR 0016 does not replace it with a regional cryosphere model.

Therefore under regional thermal authority:

`PlanetEnvironment.IceCoverageFraction`

remains unchanged by the regional thermal system.

It becomes a compatibility descriptor until regional snow, ice, hydrology, and
phase-change energetics are deliberately coupled.

The regional shortwave model does not derive surface albedo from this coarse
field in the first implementation.

## SurfaceWaterFraction under regional authority

`PlanetEnvironment.SurfaceWaterFraction` also remains unchanged by regional
thermal evolution.

Detailed water state remains owned by hydrology.

The regional thermal system must not reinterpret the coarse compatibility field
as a dynamic thermal or water reservoir.

## AtmosphereState under regional authority

`PlanetEnvironment.Atmosphere` remains the coarse planetary atmospheric
composition and pressure state.

Regional atmospheric-column temperature belongs in
`PlanetRegionalThermalState`.

The regional thermal system must not duplicate:

- atmospheric gas composition;
- pressure;
- hydrology atmospheric water mass.

Future property providers may consume those authoritative states to derive
thermal or radiative properties.

## Effective regional properties

The first causal regional model uses explicit spatially uniform model
parameters.

It does not yet derive:

- surface albedo from water, ice, vegetation, or terrain;
- surface emissivity from material state;
- surface heat capacity from water depth, soil, snow, or rock;
- atmospheric emissivity from gas composition or humidity;
- atmospheric heat capacity from column mass or composition;
- shortwave optical depth from atmospheric chemistry.

Those are later derived-property providers.

Keeping them explicit first isolates causal thermal-state mechanics from
property-model complexity.

## No non-radiative heat exchange yet

The regional thermal model defined here remains radiative only.

It does not yet include:

- sensible heat;
- latent heat;
- evaporation energy;
- condensation heating;
- surface-atmosphere conduction;
- convection;
- turbulent boundary-layer exchange;
- soil heat diffusion;
- ocean mixing;
- ocean heat transport;
- atmospheric advection;
- atmospheric circulation;
- geothermal heating.

These additions must conserve energy between participating reservoirs.

## No phase-change energetics yet

Hydrology may continue moving water between liquid and snow/ice stores using its
current compatibility thermal boundary.

Regional thermal evolution does not yet exchange latent heat with those phase
changes.

That remains a known thermodynamic incompleteness.

A later hydrology-energy coupling milestone must add equal and opposite energy
accounting rather than silently embedding latent heat in either subsystem.

## Persistence

Regional thermal state is durable simulation state.

World snapshots must persist:

- planet identity;
- surface-grid definition;
- every surface-cell thermal state;
- surface temperature;
- atmospheric-column temperature.

Introducing durable regional thermal state requires a deliberate world-snapshot
schema version increment.

Older snapshots that predate regional thermal state must load with an empty
regional thermal-state collection.

Regional thermal model configuration is simulation-definition policy.

Timeline archives must persist:

- regional thermal model definitions;
- all regional thermal model parameters.

Introducing regional thermal model policy requires a deliberate timeline-archive
schema version increment.

Older archives that predate regional thermal models must load with no regional
thermal model definitions.

## Archive and replay semantics

A timeline archive must contain enough information to resume the same thermal
authority after load.

If a planet was using the planetary EBM before save, load must restore that
planetary EBM configuration.

If a planet was using regional thermal authority before save, load must restore:

- regional thermal state;
- regional thermal model parameters;
- the astronomical authority needed to resume forcing.

That astronomical authority may be:

- configured seasonal-model policy that actively derives physical subsolar
  latitude as simulation time advances; or
- persisted Override state whose effective context explicitly supplies
  `SubsolarLatitudeDegrees`.

Persisted Derived context without its matching seasonal-model policy is not
sufficient because it would become stale after time advances.

If configured Derived astronomy exists underneath an Override, normal
seasonal-control semantics determine whether the explicit Override signal or the
actively maintained derived signal is used.

Definition validation after load must reassert thermal-model mutual exclusion.

Replay and branching must therefore preserve which thermal model owns each
planet.

## World creation and migration

New worlds may continue to use the existing planetary EBM without regional
thermal state.

This preserves current behavior.

Regional thermal state may be created explicitly using the compatibility
initializer while the EBM is still authoritative.

That permits staged migration:

1. create regional thermal state from the current global mean;
2. validate and persist the state;
3. replace the simulation definition so regional thermal authority is active
   and planetary EBM authority is absent;
4. begin regional causal evolution.

The switch in authority must be explicit.

A session must not silently infer it from the existence of regional state.

## Definition validation for regional authority

A regional thermal model definition must require:

- a valid target planet;
- exactly one regional thermal state for that planet;
- a matching surface grid;
- a usable source of physical subsolar latitude for causal evaluation.

At session construction, the astronomical requirement is satisfied when either:

- the definition contains a matching seasonal model whose provider supplies
  physical subsolar latitude; or
- the current persisted seasonal state is in Override mode and its effective
  context explicitly contains `SubsolarLatitudeDegrees`.

Persisted Derived context without matching provider configuration does not
satisfy this requirement.

The simulation definition must reject:

- duplicate regional thermal model definitions for one planet;
- simultaneous regional thermal and planetary EBM models for one planet.

Definition validation should verify whichever continuing astronomical authority
is available at construction time.

Runtime evaluation must still resolve the actual physical signal again and
reject a missing or stale-authority subsolar latitude because seasonal state can
change after session construction.

## Zero-duration evaluation

A zero-duration causal step may evaluate regional radiative diagnostics from the
current thermal and astronomical state.

It must not change surface or atmospheric temperature because:

`dt = 0`

The compatibility mean derived from unchanged regional surface temperatures
must also remain unchanged.

No special hidden time advancement is allowed.

## Diagnostics

The regional thermal system should expose enough deterministic telemetry to
audit the physical chain.

Planet-level telemetry should include at minimum:

- integration substep count;
- area-weighted previous surface temperature;
- area-weighted new surface temperature;
- area-weighted previous atmospheric temperature;
- area-weighted new atmospheric temperature;
- area-weighted absorbed surface shortwave flux;
- area-weighted absorbed atmospheric shortwave flux;
- area-weighted outgoing longwave flux to space;
- area-weighted surface net radiative flux;
- area-weighted atmospheric net radiative flux;
- maximum absolute cell radiative conservation error.

Diagnostics do not become separate state authority.

## Initial implementation sequence

ADR 0016 should be implemented in deliberate layers.

### Phase A: durable regional thermal state

Add:

- regional thermal cell state;
- planet regional thermal state;
- deterministic compatibility initializer;
- `WorldState` ownership and validation;
- immutable replacement operation;
- copy and fork preservation;
- world-snapshot persistence.

No causal regional thermal system is activated in Phase A.

### Phase B: regional thermal model policy

Add:

- regional thermal model parameters;
- regional thermal model definition;
- simulation-definition ownership;
- per-planet uniqueness;
- mutual exclusion with planetary EBM definitions;
- timeline-archive persistence.

No regional model may be treated as active until its causal system is included
in the session pipeline.

### Phase C: causal authority migration

Add:

- regional thermal causal system;
- composition of ADRs 0010 through 0015;
- bounded dual-reservoir integration;
- derived compatibility mean-surface temperature;
- atomic regional-state plus compatibility-environment replacement;
- session construction and ordering;
- archive-resume behavior.

When Phase C is complete, configured regional thermal models become active
thermal authority.

## Initial validation boundary

### State validation

The regional thermal-state implementation must prove:

- deterministic cell-state construction;
- rejection of invalid temperatures;
- rejection of duplicate surface-cell identities;
- rejection of incomplete surface-cell coverage;
- rejection of mismatched planet identity;
- rejection of mismatched surface-grid definition;
- world rejection of regional thermal state without matching terrain;
- copy and fork preservation;
- immutable replacement behavior;
- snapshot round-trip preservation;
- backward loading of snapshots without regional thermal state.

### Initialization validation

The initializer must prove:

- exactly one thermal state per surface cell;
- surface temperature initially equals the current planetary mean;
- atmospheric temperature initially equals the current planetary mean;
- grid identity is preserved;
- no terrain lapse-rate or other hidden adjustment is introduced.

### Model-policy validation

Regional model policy must prove:

- rejection of invalid stellar flux;
- rejection of invalid shortwave optical parameters;
- rejection of invalid shortwave albedo;
- rejection of invalid longwave emissivities;
- rejection of non-positive heat capacities;
- rejection of invalid integration cadence;
- duplicate regional model rejection;
- regional-model and planetary-EBM mutual exclusion;
- archive round-trip preservation;
- backward loading of archives without regional thermal configuration.

### Causal validation

The regional thermal system must prove:

- deterministic evolution for identical state and parameters;
- required regional state is enforced;
- required astronomical signal is enforced;
- seasonal evaluation occurs before regional forcing;
- hydrology remains after thermal evaluation;
- surface solar geometry uses cell latitude and physical subsolar latitude;
- shortwave factors are converted using the configured regional stellar flux;
- no extra `/4` factor is applied;
- ADR 0015 radiative accounting is reused;
- ADR 0014 thermal response is reused;
- bounded substeps are deterministic;
- cell iteration order cannot change results;
- zero-duration steps preserve temperatures;
- outgoing longwave and reservoir budgets remain finite;
- cell-level radiative conservation remains within tolerance;
- the planetary EBM does not execute for a regionally authoritative planet;
- regional thermal state and the compatibility environment update atomically;
- no downstream causal system can observe a mismatched regional state and
  compatibility mean;
- the area-weighted compatibility mean equals regional surface state;
- regional evolution does not change coarse ice coverage;
- regional evolution does not change coarse surface-water fraction;
- regional evolution does not mutate atmospheric composition or pressure;
- downstream compatibility consumers see the updated derived mean only after
  regional climate evaluates;
- save/load resumes with the same thermal authority and regional state.

## Deferred work

ADR 0016 does not yet add:

- cell-local hydrology temperature consumption;
- cell-local vegetation temperature consumption;
- cell-local biogeochemistry temperature consumption;
- regional ice-fraction authority;
- regional snow or ice energy coupling;
- latent heat;
- sensible heat;
- convection;
- conduction;
- evaporation-energy coupling;
- atmospheric circulation;
- horizontal atmospheric transport;
- ocean heat transport;
- terrain-driven lapse-rate initialization;
- surface-property derivation;
- atmospheric-property derivation;
- clouds;
- spectral radiation;
- multiple atmospheric layers;
- diurnal thermal cycles;
- API configuration for regional thermal models;
- web controls for thermal parameters.

## Consequences

Est gains a durable regional thermal-state architecture and an explicit path from
the zero-dimensional planetary EBM to regional thermal authority.

The migration is staged rather than implicit.

The critical authority rule is:

**For one planet, exactly one configured causal thermal model owns temperature
evolution.**

Before migration:

`PlanetaryEnergyBalanceSystem`
`-> MeanSurfaceTemperatureKelvin`

After migration:

`seasonal astronomy`
`-> regional shortwave + longwave budget`
`-> regional surface + atmospheric temperatures`
`-> area-weighted compatibility MeanSurfaceTemperatureKelvin`
`-> existing downstream compatibility consumers`

The compatibility mean remains available during consumer migration without
remaining an independent climate truth.

The next implementation work should begin with Phase A durable regional thermal
state and persistence before any new causal temperature evolution is activated.
