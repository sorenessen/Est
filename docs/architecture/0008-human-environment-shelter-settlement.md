# ADR 0008: Human Environmental Exposure, Shelter, and Settlement Foundation

## Status

Accepted.

Decision date: 2026-09-18.

## Context

Est's human population is already part of an authoritative living-world
simulation.

Humans currently have persistent identity, location, age, health, energy,
reproduction, movement, foraging, and organism-material state. The shared
planetary surface already provides terrain, hydrology, vegetation, and other
environmental state.

Future seasonal and regional-climate work will make environmental conditions
more spatially and temporally variable.

That creates an important human-survival requirement.

Humans should not respond directly to an abstract seasonal label such as
`Winter`. They should respond to the physical environmental conditions produced
by the world around them.

Examples include:

- local air temperature;
- precipitation;
- snow and ice;
- wind;
- humidity and wetness;
- solar exposure and photoperiod where physiologically relevant;
- elevation-dependent conditions;
- resource scarcity associated with environmental change.

The same human-survival model must remain useful outside Earth-like seasonal
cycles.

A person may require protection because of:

- an ordinary winter;
- an unusual summer cold event;
- high elevation;
- severe wind or precipitation;
- a cold configurable Living World;
- another future environmental condition.

Encoding survival directly as `Winter -> health loss` would therefore put
calendar or seasonal semantics in the wrong architectural layer.

Est also intends to scale toward settlements, buildings, communities, and
individual inhabitants.

Existing multi-scale presentation work proves that local building geometry can
be represented independently of planetary presentation. It deliberately does
not make those presentation features authoritative simulation state.

Future simulated structures require a separate authoritative domain model.

## Decision

Est will treat human environmental protection as a causal chain:

    seasonal / astronomical / environmental drivers
      -> regional and local environmental conditions
      -> human environmental exposure
      -> protection need
      -> shelter use
      -> constructed structures
      -> persistent habitation
      -> settlements

Seasonal state does not directly damage, protect, house, or otherwise command a
human.

Humans respond to authoritative environmental conditions.

Shelter modifies the human's exposure to those conditions.

Constructed shelters and buildings, when implemented, are authoritative world
state rather than renderer-owned decoration.

Settlements emerge from persistent spatial and social organization around
authoritative inhabitants, structures, resources, and activity rather than
being created merely as presentation labels.

## Environmental exposure

Human survival will eventually include an environmental-exposure model.

The first useful version should remain deliberately smaller than a complete
human thermoregulation model.

It may begin with inputs such as:

- local temperature;
- precipitation or wetness;
- wind exposure;
- available protection.

Later versions may incorporate:

- clothing;
- fire and other heat sources;
- humidity;
- solar radiation;
- exertion;
- nutrition;
- age-dependent vulnerability;
- health-dependent vulnerability;
- acclimatization;
- building thermal properties.

Exposure should be represented as authoritative causal state or a deterministically
derived physiological consequence of authoritative state.

The model must not infer exposure from presentation appearance.

## Relationship to seasonal control

ADR 0007 remains authoritative for seasonal-control policy.

Seasonal context is an upstream source of simulation context.

A seasonal provider may eventually influence climate, photoperiod, hydrology,
vegetation, migration, agriculture, or other systems.

Human shelter requirements must not be implemented as a direct check of
effective season.

For example:

    if season == Winter:
        damage human

is not an accepted architectural model.

The intended direction is closer to:

    seasonal and planetary state
      -> local climate
      -> local environmental exposure
      -> physiological consequence

This preserves physically meaningful behavior under derived seasons, explicit
seasonal overrides, non-Earth calendars, unusual weather, and configurable
Living Worlds.

## Regional climate dependency

The current planetary energy-balance model provides coarse thermal boundary
conditions.

Detailed human exposure requires environmental state at a finer spatial
resolution.

A future regional-climate layer may provide cell-level or otherwise spatially
resolved state such as:

- temperature;
- precipitation;
- humidity;
- wind;
- insolation;
- snow or ice conditions.

Human exposure should consume that authoritative environmental state through
stable simulation contracts.

The human model must not own regional climate.

## Shelter

Shelter is protection from environmental exposure.

The first shelter capability does not require houses, architecture, settlement
governance, crafting interfaces, or detailed construction simulation.

A minimal shelter model may represent:

- location;
- protection from environmental conditions;
- capacity;
- occupancy;
- condition or integrity.

This allows Est to establish the causal survival relationship before committing
to a detailed building system.

Natural and constructed shelter may eventually coexist.

Examples may include:

- caves;
- temporary shelters;
- simple constructed shelters;
- permanent dwellings;
- larger communal structures.

The shelter abstraction must not assume that every world or culture begins with
modern houses.

## Constructed structures

When humans begin constructing persistent shelter, structures become
authoritative simulation entities or aggregates.

A structure may eventually contain state such as:

- stable identity;
- planet and spatial location;
- physical footprint;
- type or function;
- occupancy capacity;
- current occupants or users;
- thermal and environmental protection;
- structural condition;
- construction materials;
- stored resources;
- ownership, household, group, or community association where required.

The exact structure model remains deferred until implementation evidence
justifies it.

This ADR establishes the ownership boundary, not the final schema.

## Material causality

Constructed structures must not appear from nothing during runtime simulation.

Construction must consume or transform authoritative resources when those
resource systems exist.

Examples may eventually include:

- wood;
- plant fiber;
- stone;
- soil or clay;
- animal material;
- manufactured materials.

The level of material detail may vary by simulation fidelity, but construction
must preserve the same causal principle already used elsewhere in Est:

runtime state changes require an explicit source.

Presentation geometry is not a substitute for construction state or material
accounting.

## Knowledge and construction capability

Humans should not acquire complex construction behavior from an arbitrary age
threshold or a globally assumed technology level.

The ability to create a particular shelter or structure should eventually come
from authoritative capability.

That capability may be represented through future systems such as:

- individual knowledge;
- learned skill;
- cultural transmission;
- group knowledge;
- technology;
- available tools;
- observed or inherited construction practices.

The final cognition, knowledge, and technology architecture remains deferred.

This ADR establishes only that construction capability must have a causal
source rather than appearing because the simulation requires a house.

## Settlement formation

A settlement is not initially defined as a decorative marker or manually
spawned presentation object.

Settlement state should emerge from durable organization in the authoritative
world.

Relevant evidence may eventually include:

- persistent co-location of inhabitants;
- constructed shelters and other structures;
- repeated habitation;
- shared resources or storage;
- paths and transportation;
- coordinated activity;
- households or social groups;
- common infrastructure.

A future settlement entity may aggregate and index that state for simulation
efficiency and higher-level behavior.

Creating such an aggregate must not erase the causal lower-level state that
justifies the settlement's existence.

This allows Est to scale from:

    individuals
      -> repeated habitation
      -> structures
      -> clustered structures and resources
      -> settlement
      -> larger social organization

without requiring the final civilization model today.

## Presentation boundary

ADR 0003 remains authoritative for multi-scale presentation.

ADR 0006 remains authoritative for planetary rendering and the separation of
simulation geography from visual geometry.

A structure may have different visual representations at different scales.

For example:

- absent or aggregated at planetary scale;
- settlement-level marker or footprint at regional scale;
- simplified building geometry at local scale;
- detailed geometry and materials at immediate scale.

Those representations do not own whether the structure exists.

The authoritative structure state determines existence, location, condition,
and simulation meaning.

Presentation determines how that state is shown.

Historical or externally sourced building geometry may remain useful for Earth
Observatory and evaluation work, but it must not silently become authoritative
Living World construction state.

## Initial implementation boundary

This ADR does not expand the current dormant seasonal-control milestone.

The seasonal-control foundation defined by ADR 0007 should be completed without
adding:

- human exposure damage;
- shelter-seeking behavior;
- houses;
- construction;
- crafting;
- knowledge or technology progression;
- settlements;
- civilization simulation.

Those capabilities build on environmental and seasonal foundations later.

The first human-environment implementation, when prioritized, should establish
causal exposure and protection before detailed construction or settlement
simulation.

## Architectural invariants

Future implementation must preserve these rules:

1. Humans respond to physical environmental conditions, not directly to a
   seasonal label.
2. Simulation time, seasonal state, environmental state, and physiological
   exposure remain separable concerns.
3. Regional climate owns regional environmental conditions.
4. Human survival systems consume environmental state rather than inventing it.
5. Shelter mitigates exposure.
6. Constructed shelter and buildings are authoritative simulation state when
   they exist in simulated worlds.
7. Renderer geometry does not create simulation structures.
8. Runtime construction requires an explicit causal resource source.
9. Construction capability requires an authoritative capability or knowledge
   source when that distinction becomes modeled.
10. Settlements arise from persistent authoritative inhabitants, structures,
    resources, and activity rather than presentation convenience.
11. Different visual representations may be used at different scales without
    changing simulation truth.
12. The architecture must remain usable for Earth-like worlds, non-Earth
    seasonal cycles, configurable Living Worlds, and environments without
    conventional four-season calendars.

## Consequences

### Positive

- Human survival becomes environmentally causal rather than calendar-driven.
- Seasonal control remains cleanly separated from biological response.
- Regional climate gains a clear future human consumer.
- Shelter can be introduced before the much larger building and civilization
  systems.
- Structures gain a clear authoritative ownership boundary.
- Existing multi-scale presentation work can later visualize simulated
  structures without becoming their source of truth.
- Settlement formation can emerge from simulation rather than being faked by
  the renderer.
- Construction can follow Est's existing causal-material philosophy.
- The design remains valid for arbitrary planets and configurable Living
  Worlds.

### Costs

- Human survival will eventually require finer environmental context than the
  current planetary energy-balance model provides.
- Persistent structures add another durable state family.
- Construction introduces resource, capability, and spatial requirements.
- Settlement modeling eventually requires aggregation across individual,
  structural, economic, and social state.

These costs are deferred until those capabilities are actually implemented.

The immediate implementation priority remains the dormant seasonal-control
foundation defined by ADR 0007.
