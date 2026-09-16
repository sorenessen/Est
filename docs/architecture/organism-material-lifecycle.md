# Organism Material and Lifecycle Accounting

## Status

Organism-material foundation implemented. Lifecycle material transfers remain
an implementation prerequisite for evolutionary population dynamics.

## Problem

Est represents different organisms at different simulation resolutions:

- humans as individual `PersonState` values;
- wolves as individual `AnimalState` values;
- birds as aggregate `BirdFlockState` values;
- terrestrial grazers as aggregate `GrazerCohortState` values;
- invertebrates as spatial aggregate biomass.

Those differences in simulation resolution are intentional and useful.

The physical differences that existed before this foundation were not.

Before organism material was introduced, humans and wolves carried normalized
energy and health but no authoritative body material, while bird flocks and
grazer cohorts carried member counts but no authoritative body material.
Feeding, predation, mortality, and reproduction could therefore change organism
counts or energy while biological matter disappeared from, or appeared in, the
modeled system.

The original wolf representation was a prototype implementation and must not
define the general organism architecture.

## Architectural rule

Behavioral representation and physical material accounting are separate
concerns.

An organism may be represented as an individual, cohort, flock, or spatial
aggregate according to the resolution needed by its behavior. Every
material-bearing representation must nevertheless participate in the same
physical accounting rules.

For fauna and humans, the common physical substrate should initially track at
least:

- live biomass in kilograms;
- live nitrogen in kilograms.

For an individual these quantities describe that organism. For a cohort or
flock they describe the aggregate represented population.

This does not require humans, wolves, birds, and grazers to share one behavioral
state class.

## Initial conditions

World creation is allowed to seed biological material as an explicit initial
condition.

Material composition must come from explicit creation or model policy. Do not
hide species body masses, tissue-nitrogen ratios, or similar assumptions inside
behavior systems.

Legacy state may require explicit migration behavior, but runtime simulation
must not rely on implicit material creation.

## Runtime material rules

Runtime transitions must be causal.

### Mortality

When a material-bearing organism or represented fraction dies, its remaining
tracked live biomass and nitrogen transfer into authoritative detrital pools.

The transfer semantics are the same regardless of whether the source is a
human, wolf, bird flock, or grazer cohort.

### Feeding

Feeding removes authoritative material from a source.

The consumed material must then have explicit destinations. Material may become
consumer tissue, waste, or leave the tracked organic biomass pool through an
explicit metabolic process such as respiration.

Tracked nitrogen may not silently disappear. It must remain in living material
or return to an authoritative environmental pool.

### Predation

Predation is a feeding interaction, not a special material model.

Killing prey must transfer or return prey material explicitly. A prey count may
not simply disappear in exchange for a fixed abstract predator-energy value.

Predator energy and behavior may remain species-specific.

### Reproduction and lifecycle

Runtime births or aggregate recruitment must have a physical source for new live
material before evolutionary population dynamics depend on those transitions.

The exact pregnancy, egg, nesting, litter, cohort-recruitment, growth, and
parental-investment models may differ by species and simulation resolution, but
new live material must not appear without explicit accounting.

Every modeled animal representation must support a complete causal lifecycle
appropriate to its resolution:

- creation through birth, hatching, recruitment, or aggregate reproduction;
- juvenile or immature growth;
- maturation into reproductive eligibility;
- species-appropriate mating or reproductive behavior;
- aging and senescence where the representation resolves them;
- species-appropriate seasonal states and strategies;
- mortality with remaining tracked material returned through the common
  mortality pathway.

Shared lifecycle semantics do not imply shared behavior classes. Humans and
wolves may remain individual organisms. Bird flocks and grazer cohorts may
remain aggregates with demographic structure. Invertebrates may remain spatial
biomass aggregates until finer ecological resolution is justified.

Species policy determines how common lifecycle concepts are expressed. Examples
include pregnancy and litters, eggs and nesting, breeding seasons, rutting,
migration, torpor, dormancy, and hibernation. A strategy is enabled only where
biologically appropriate; hibernation is not a universal animal behavior.

### Growth and material sourcing

Newborn, juvenile, and aggregate population growth must conserve tracked
material.

Birth transfers or assembles an explicitly sourced initial material state.
Subsequent growth toward mature body mass must be supported by assimilated food
or another authoritative material source. Carrying capacity, energy reserve,
health, or population support may constrain growth, but none of those abstract
signals may create biomass or tracked nitrogen by themselves.

Aggregate reproduction follows the same rule. Increasing flock membership,
cohort membership, or spatial invertebrate biomass requires an explicit material
source even when individual organisms are not materialized.

### Birds

Current bird use of invertebrate biomass is ecological support only. Do not
remove invertebrate material until actual bird consumption is modeled.

## Existing energy and health state

Normalized energy reserve and health remain useful behavioral/physiological
signals. They are not substitutes for physical biomass or elemental
composition.

The existing human and wolf energy models may remain during migration, but food
and prey material flows must be made explicit underneath them.

## Implementation sequence

1. Add a shared organism-material value object that supports both individual and
   aggregate representations.
2. Attach authoritative material state to humans, wolves, bird flocks, and
   grazer cohorts without collapsing their behavioral representations.
3. Add explicit world-creation policy, persistence, API exposure, and backward
   compatibility for organism material.
4. Route human, wolf, bird, and grazer mortality through common detrital
   material-transfer semantics.
5. Close human-foraging and grazer-grazing material leaks, including tracked
   nitrogen.
6. Replace count-and-energy-only predation with explicit prey-material
   accounting while preserving species-specific hunting behavior.
7. Add shared lifecycle semantics for birth/recruitment, growth, maturation,
   reproductive eligibility, aging, seasonal state, and death without forcing
   species into one behavioral representation.
8. Complete individual lifecycle state and reproduction for humans and wolves,
   including causal offspring material and post-birth growth.
9. Add demographic lifecycle structure and causal reproduction/recruitment for
   bird flocks and grazer cohorts without materializing every individual.
10. Make aggregate invertebrate reproduction/growth consume an authoritative
    material source rather than treating carrying capacity as material creation.
11. Add species-policy seasonal lifecycle strategies such as breeding seasons,
    migration, torpor, dormancy, or hibernation where biologically appropriate.
12. Validate material and nitrogen accounting across feeding, growth,
    predation, reproduction, mortality, decomposition, snapshot restore, and
    session advancement.
13. Begin evolutionary inheritance and selection only after this substrate is
    green.

## Non-goals

This work does not require:

- materializing every bird or grazer as an individual;
- forcing all species into `AnimalState`;
- making all species share behavior, cognition, diet, movement, reproductive,
  or seasonal policy;
- modeling every individual bird, grazer, or invertebrate merely to represent a
  complete lifecycle;
- modeling every chemical element immediately;
- introducing detailed physiology before it is required.

The goal is common physical truth beneath appropriately different simulation
resolutions.
