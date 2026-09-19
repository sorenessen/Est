# ADR 0014: Thermal Reservoir and Longwave Radiation Foundation

## Status

Accepted.

Decision date: 2026-09-19.

## Context

ADRs 0010 through 0013 now establish a deterministic shortwave chain from
astronomical geometry through atmospheric and surface optical accounting.

The chain can determine:

- top-of-atmosphere shortwave geometry;
- direct-beam atmospheric extinction;
- atmospheric shortwave absorption;
- atmospheric scattering;
- direct plus diffuse surface downwelling;
- surface shortwave reflection;
- surface-system shortwave absorption.

The chain deliberately stops before temperature forcing.

ADR 0013 defines absorbed surface shortwave as optical energy deposition, not
as a temperature tendency or heat-storage assignment.

That distinction is necessary because converting absorbed radiative flux into
temperature requires a thermal reservoir.

A thermal reservoir needs, at minimum:

- a temperature;
- an effective heat capacity per unit area;
- a net energy flux into or out of the reservoir;
- an integration duration.

A radiating thermal reservoir also requires a longwave-emission model.

Est already contains these concepts inside the zero-dimensional
`PlanetaryEnergyBalanceSystem`.

That system currently computes:

`absorbed solar flux`

minus:

`effective emissivity * Stefan-Boltzmann constant * temperature^4`

and converts the resulting net flux to a mean planetary temperature change
using an effective areal heat capacity.

That existing system remains the current authoritative global thermal model.

The regional foundation must therefore extract reusable thermal physics without
silently replacing, partially rewiring, or changing the semantics of the
existing planetary energy-balance system.

## Decision

Est will add a pure deterministic thermal-energy foundation with two separate
responsibilities:

1. graybody longwave emission;
2. areal thermal-reservoir energy response.

Neither calculation will mutate simulation state.

Neither calculation will own regional climate state.

Neither calculation will replace `PlanetaryEnergyBalanceSystem`.

## Longwave emission

The first longwave model will use the Stefan-Boltzmann relation.

For a radiating boundary with:

- absolute temperature `T` in kelvin;
- effective broadband longwave emissivity `epsilon`;

the hemispheric emitted longwave flux is:

`F_longwave = epsilon * sigma * T^4`

where:

`0 <= epsilon <= 1`

and:

`sigma = 5.670374419e-8 W m^-2 K^-4`

The result is in:

`W/m^2`

The implementation must use one authoritative Stefan-Boltzmann constant for
the new foundation.

The existing `PlanetaryEnergyBalanceSystem` currently contains the same
physical constant.

A later migration may make both systems consume a shared constant, but this
milestone must not modify the existing energy-balance system merely to remove
that duplication.

## Meaning of effective emissivity

The first model accepts effective broadband longwave emissivity explicitly.

It does not derive emissivity from:

- atmospheric composition;
- pressure;
- humidity;
- clouds;
- surface material;
- vegetation;
- snow;
- ice;
- water;
- wavelength;
- temperature-dependent spectroscopy.

The parameter is an effective optical property.

For a material boundary, it represents the fraction of blackbody hemispheric
thermal emission represented by the first-order model.

For a future atmospheric slab or column model, additional architectural rules
will be required to determine:

- longwave absorptivity;
- upward versus downward emission;
- optical depth;
- vertical structure;
- exchange with the surface;
- escape to space.

ADR 0014 does not claim that a single emissivity value is a complete
atmospheric greenhouse model.

## Longwave direction and geometry

The base longwave calculation produces one hemispheric emitted flux.

It does not itself decide whether that flux is:

- upward to space;
- downward toward a surface;
- emitted from a surface boundary;
- emitted from one face of an atmospheric layer.

Those are responsibilities of a future radiative-energy-budget model.

This prevents directional greenhouse assumptions from being hidden inside the
Stefan-Boltzmann calculation.

## Thermal reservoir model

The first thermal-reservoir calculation will use an effective areal heat
capacity:

`C`

with units:

`J m^-2 K^-1`

and:

`C > 0`

For a finite net heat flux:

`F_net`

in:

`W/m^2`

applied for:

`dt`

seconds, the areal energy change is:

`Delta_E = F_net * dt`

with units:

`J/m^2`

The corresponding temperature change is:

`Delta_T = Delta_E / C`

or equivalently:

`Delta_T = F_net * dt / C`

The resulting temperature is:

`T_next = T_initial + Delta_T`

The calculation permits signed net heat flux:

- positive flux warms the reservoir;
- negative flux cools the reservoir;
- zero flux leaves temperature unchanged.

## Thermal-reservoir validity

The first implementation will require:

- finite initial temperature;
- initial temperature at or above absolute zero;
- finite positive effective areal heat capacity;
- finite net heat flux;
- non-negative finite integration duration.

The resulting temperature must remain finite and at or above absolute zero.

If a requested integration step would produce an invalid physical temperature,
the calculation must reject that result rather than clamp it.

The caller may then use a smaller time step or a different physical model.

## Effective areal heat capacity

The first foundation accepts effective areal heat capacity explicitly.

It does not derive heat capacity from:

- material density;
- layer depth;
- specific heat;
- soil composition;
- vegetation biomass;
- water depth;
- ocean mixed-layer depth;
- snow depth;
- ice thickness;
- atmospheric column mass;
- atmospheric composition.

Those belong to future thermal-property providers.

The explicit effective parameter allows the thermal response law to exist
without inventing hidden Earth-specific reservoir depths or material
properties.

## Radiative flux versus stored energy

Radiative flux and stored thermal energy remain distinct concepts.

`W/m^2` describes an energy-transfer rate per unit area.

`J/m^2` describes areal energy transferred or stored.

Kelvin describes thermal state.

ADR 0014 must not collapse these into a single quantity.

The conversion chain is explicitly:

`net flux`
`-> integrate over time`
`-> areal energy change`
`-> divide by areal heat capacity`
`-> temperature change`

## Relationship to ADR 0012 atmospheric absorption

ADR 0012 exposes an atmospheric absorbed-shortwave factor.

That factor may eventually contribute energy to an atmospheric thermal
reservoir.

ADR 0014 does not yet assign it to one.

In particular, ADR 0014 does not create:

- atmospheric temperature state;
- atmospheric areal heat capacity;
- vertical atmospheric layers;
- convective adjustment;
- longwave atmospheric absorption;
- downward atmospheric longwave radiation.

Those require a deliberate atmospheric energy-budget model.

## Relationship to ADR 0013 surface absorption

ADR 0013 exposes a surface-system absorbed-shortwave factor.

That factor may eventually contribute energy to one or more surface or
near-surface thermal reservoirs.

ADR 0014 does not yet decide whether absorbed shortwave heats:

- vegetation;
- soil;
- rock;
- snow;
- ice;
- shallow water;
- an ocean mixed layer;
- a composite effective surface reservoir.

That assignment requires a future regional thermal-state model.

## Converting shortwave factors to physical flux

ADRs 0010 through 0013 use dimensionless factors relative to normal-incidence
stellar flux.

Before a shortwave factor can become a thermal input in `W/m^2`, a future
energy-budget model must multiply it by the authoritative stellar-flux
magnitude.

`PlanetaryEnergyBalanceParameters.StellarFluxWattsPerSquareMeter` remains the
current stellar-flux authority.

ADR 0014 does not create a second stellar-flux authority.

The thermal-reservoir calculator itself consumes physical net flux in `W/m^2`
and therefore does not know about dimensionless shortwave factors.

## Relationship to current authoritative temperature

`PlanetEnvironment.MeanSurfaceTemperatureKelvin` remains authoritative.

Existing systems currently consume that planetary mean temperature directly or
derive coarse local temperature from it using elevation lapse-rate rules.

Those consumers include current hydrology and biological systems.

ADR 0014 does not change those consumers.

It does not introduce a competing regional temperature field.

It does not make local thermal calculations authoritative merely because the
pure foundation exists.

## Relationship to PlanetaryEnergyBalanceSystem

`PlanetaryEnergyBalanceSystem` remains unchanged and causal.

Its current:

- global absorbed-solar calculation;
- effective longwave emissivity;
- effective areal heat capacity;
- Stefan-Boltzmann outgoing-longwave calculation;
- mean-surface-temperature evolution;
- ice-temperature feedback;

remain the active zero-dimensional thermal baseline.

ADR 0014 extracts compatible reusable physical concepts for future regional
climate without partially migrating the existing model.

A later ADR must define the migration boundary before regional thermal state can
replace or coexist causally with the planetary mean-temperature model.

## Regional thermal-state boundary

The eventual regional thermal model is expected to use the authoritative
surface grid.

ADR 0014 does not yet add temperature to `SurfaceCell`.

`SurfaceCell` remains geometry and topology only.

Future evolving thermal state should be associated with surface-cell identity
through a dedicated regional thermal-state model rather than by mutating the
grid geometry type.

That model must define:

- which thermal reservoirs exist per cell;
- which temperature is authoritative for each reservoir;
- how reservoir properties are derived;
- how surface and atmospheric energy exchange is represented;
- how state survives snapshots, archives, replay, and branching;
- how existing global temperature consumers migrate.

## Longwave energy budget intentionally deferred

Longwave emission alone is not a complete longwave energy budget.

This milestone does not add:

- surface absorption of atmospheric longwave;
- atmospheric absorption of surface longwave;
- atmospheric longwave optical depth;
- atmospheric upward emission;
- atmospheric downward emission;
- multiple atmospheric layers;
- greenhouse feedback;
- cloud longwave forcing;
- spectral longwave transfer;
- window bands;
- pressure broadening;
- collision-induced absorption;
- water-vapor feedback;
- carbon-dioxide radiative forcing.

A later energy-budget model must compose directional longwave transfers
explicitly and conserve energy across the modeled reservoirs and space
boundary.

## Non-radiative energy transfer intentionally deferred

The first thermal foundation also does not add:

- sensible heat transfer;
- latent heat transfer;
- evaporation energy;
- condensation heating;
- conduction between reservoirs;
- soil heat diffusion;
- ocean mixing;
- ocean transport;
- convection;
- advection;
- atmospheric circulation;
- phase-change latent heat;
- geothermal heat;
- biological heat production.

Those require explicit causal systems and reservoirs.

## State and persistence

ADR 0014 remains pure deterministic physics.

It does not add:

- new `WorldState`;
- new planet-environment fields;
- new surface thermal state;
- new atmospheric thermal state;
- new simulation operations;
- new `SimulationDefinition` model configuration;
- snapshot fields;
- timeline archive fields;
- schema-version changes;
- API resources;
- web contracts;
- user controls.

No authoritative simulation state is mutated.

## Initial validation boundary

The first implementation must prove longwave emission behavior including:

- deterministic results for identical inputs;
- rejection of negative or non-finite temperature;
- rejection of emissivity outside `[0, 1]`;
- zero temperature produces zero emission;
- zero emissivity produces zero emission;
- unit emissivity produces blackbody emission;
- emitted longwave flux is finite and non-negative for representative inputs;
- emission increases monotonically with temperature for fixed positive
  emissivity;
- emission scales linearly with emissivity for fixed temperature;
- known Stefan-Boltzmann reference values are reproduced within floating-point
  tolerance.

The first implementation must prove thermal-reservoir behavior including:

- deterministic results for identical inputs;
- rejection of invalid initial temperature;
- rejection of non-positive or non-finite areal heat capacity;
- rejection of non-finite net heat flux;
- rejection of negative or non-finite integration duration;
- zero elapsed time produces zero energy and temperature change;
- zero net flux produces zero energy and temperature change;
- positive net flux increases temperature;
- negative net flux decreases temperature;
- areal energy change equals net flux multiplied by elapsed time;
- temperature change equals areal energy change divided by areal heat capacity;
- equal and opposite fluxes over equal intervals produce equal and opposite
  temperature changes when both resulting temperatures remain valid;
- an integration that would produce a temperature below absolute zero is
  rejected;
- no simulation state is mutated.

## Consequences

Est gains the reusable physical bridge between radiative energy accounting and
future authoritative thermal state without prematurely creating regional
climate.

The physical chain can now be built toward:

`stellar flux magnitude`
`-> solar geometry`
`-> atmospheric shortwave partition`
`-> surface shortwave partition`
`-> physical absorbed flux`
`-> thermal-reservoir energy deposition`
`-> temperature response`
`-> longwave emission`
`-> future surface / atmosphere / space energy exchange`

The current zero-dimensional planetary energy-balance model remains intact.

The next causal milestone after this foundation must explicitly define the
regional thermal reservoirs and the surface-atmosphere-space energy-budget
topology before any regional temperature becomes authoritative.
