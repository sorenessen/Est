# Est Experience and Game Design

Status: Draft

This document defines the emerging product and game-design theory for Est.

It is not an architecture decision record and should not be treated as
immutable doctrine. Its purpose is to give product development a coherent
direction while the experience is still being discovered.

---

## 1. What Est Is

Est is a living-world simulation game built on a serious scientific simulation.

The primary product experience is **Living Worlds**.

The user enters a simulated world, observes what is happening, understands its
causes, intervenes when desired, advances time, and lives with the consequences.

The same authoritative simulation also supports two major analytical modes:

- **Earth Observatory**: a scientific and exploratory mode centered on
  understanding real-world systems and causal relationships.
- **Scenario Lab**: a controlled experimental mode for changing assumptions,
  branching timelines, advancing simulations, and comparing outcomes.

These are not three unrelated products.

They are different ways of interacting with the same simulated reality.

**The simulation owns reality. Presentation, narrative, analysis, and gameplay
reveal and interact with that reality rather than inventing a contradictory
one.**

### Product priority

When design goals conflict, Est should preserve this hierarchy:

1. The world must remain causally coherent.
2. The player must have meaningful agency.
3. Consequences must be understandable and visible.
4. Scientific and analytical depth should strengthen the experience rather
   than replace it.
5. Predictive claims must remain limited to domains where evidence justifies
   them.

Est is therefore not primarily a statistics dashboard, scientific visualization,
or forecasting application with game mechanics attached.

It is a game about living worlds whose simulation is deep enough to also support
serious observation and experimentation.

---

## 2. The Core Product Thesis

The central proposition of Est is simple:

**Give the player a world that does not need them.**

Est simulates worlds that continue to exist and change independently of the
user.

The user is not inherently the civilization, government, city, species, or
individual being simulated.

The player fundamentally exists outside the world's normal simulation
hierarchy, but is not confined there.

The player may observe the world from above and may also enter the world as an
active presence.

No fictional identity or prescribed theology is currently required. The game
does not need to declare that the player literally is a deity.

Functionally, however, Living Worlds should support the fantasy of a being with
extraordinary knowledge and power who can walk among the inhabitants of the
world.

The player may therefore behave as some combination of:

- observer,
- custodian,
- experimenter,
- historian,
- planner,
- disruptor,
- protector,
- participant,
- embodied presence.

Individual scenarios may assign a specific role, perspective, or constraint,
but the base experience should preserve both large-scale observation and
direct participation.

The user may influence a civilization without directly controlling every
member of it.

The user may alter circumstances without dictating the result.

The world remains causally authoritative.

### Core principle

**The world acts. The user intervenes. The simulation decides what follows.**

---

## 3. The Player Fantasy

The fundamental player fantasy is:

> I have been given a living world. I can understand it, influence it, change
> it, protect it, exploit it, or simply watch it. Its inhabitants have lives
> and goals of their own, and my actions become part of their history.

This is different from a conventional civilization game.

The player does not automatically own the civilization.

Civilizations are actors within the world.

They may:

- emerge,
- grow,
- migrate,
- cooperate,
- compete,
- fragment,
- industrialize,
- collapse,
- recover,
- change governments,
- develop technologies,
- alter ecosystems,
- and eventually expand beyond their planet.

The player may influence those processes.

The player does not automatically command them.

This preserves the independence of simulated people, animals, ecosystems,
institutions, and civilizations.

---

## 4. The Core Interaction Loop

The primary Est interaction loop is:

**Observe -> Understand -> Intervene -> Advance -> Witness -> Compare**

### Observe

The user sees what is happening.

Examples include:

- drought,
- flooding,
- population growth,
- migration,
- births,
- deaths,
- predation,
- ecosystem changes,
- settlement growth,
- resource shortages,
- technological transitions,
- political change.

### Understand

The user asks why.

Examples:

- Why is this population declining?
- Why did this lake shrink?
- Why are animals migrating?
- Why is food becoming scarce?
- Why did this settlement grow here?
- Why did this wolf attack a human?
- Why did a civilization fragment?

The interface should expose causal relationships instead of presenting only
state values.

### Intervene

The user changes something the simulation recognizes as a legitimate causal
input.

Possible intervention classes may eventually include:

- environmental modification,
- resource allocation,
- infrastructure,
- policy,
- technology,
- biological intervention,
- disaster response,
- settlement support,
- direct sandbox manipulation.

Intervention capability may differ by mode or scenario.

### Advance

Simulation time continues.

Time is a gameplay instrument.

The user should eventually be able to:

- pause,
- advance slowly,
- accelerate,
- jump across longer intervals,
- stop when meaningful events occur.

### Witness

The user sees what happened because the world continued.

The result should not merely be a changed number.

The user should be able to see the physical, biological, social, or historical
consequences of an intervention.

### Compare

The user asks:

- What changed?
- Why?
- What would have happened otherwise?
- Did my intervention work?
- What new problems did it create?

Timeline history and branching therefore serve both scientific and gameplay
purposes.

---

## 5. The World Is the Story

Est does not require one canonical authored story.

The history generated by the simulation is the primary story.

A drought that causes migration is a story.

A wolf killing a human is a story.

A family surviving through several generations is a story.

A settlement becoming a city is a story.

A civilization exhausting a resource is a story.

A species disappearing because of environmental change is a story.

A colony on another planet declaring independence is a story.

The role of the experience layer is to make those stories visible.

### Design principle

**Est should not manufacture stories on top of the simulation. Est should
discover stories inside the simulation and make them legible to the user.**

---

## 6. From Telemetry to Narrative

The current runtime already demonstrates the difference between simulation and
experience.

A statistics panel may show:

- wolf attacks: 5
- failed attacks: 4
- human predation deaths: 1

Those values are useful.

But the successful attack is also an event:

> A human was killed by a wolf.

That event can potentially connect the user to:

- the human who died,
- the wolf that attacked,
- the location,
- the simulation time,
- what each actor was doing,
- whether the wolf was starving,
- whether normal prey was scarce,
- nearby environmental conditions,
- the victim's household or dependents,
- later consequences for the population,
- later consequences for the wolf.

The simulation already produced the event.

The product experience should make the causal event discoverable.

### Design rule

**Important causal transitions should be exposed as inspectable events, not
buried only in aggregate counters.**

---

## 7. Startup Experience

Est should not begin by dropping a new user directly into the current
developer-style observatory.

A mature startup experience should establish context before exposing
complexity.

### 7.1 Splash

A restrained opening:

**EST**

The splash should be short.

Est should feel serious, expansive, and alive rather than theatrical for its
own sake.

### 7.2 Framing

One possible opening thesis:

> This world will continue without you.
>
> You may observe it.
>
> You may change it.
>
> Everything you change has consequences.

This framing establishes the user's relationship with the simulation without
requiring a specific fictional identity.

### 7.3 Main entry

Potential top-level experiences:

- **Continue**
- **Living Worlds**
- **Earth Observatory**
- **Scenario Lab**

Advanced tooling and developer controls should not dominate the first
experience.

### 7.4 First Living World

A first-time user should be able to enter a viable world without completing a
large configuration form.

The first world should demonstrate Est quickly.

Advanced creation can exist separately.

### 7.5 First orientation

The planet appears before the user sees a wall of controls.

Then Est directs attention to something that is actually happening.

For example:

- a hunt,
- a birth,
- a migration,
- a drought,
- a population under pressure,
- a death,
- an ecosystem boundary.

The tutorial should begin with a real event generated by the world.

The sequence becomes:

**Something is happening.**

Then:

**Here is why.**

Then:

**You may intervene.**

Then:

**Advance time and see what happens.**

That teaches Est through the actual product loop.

---

## 8. Living Worlds

Living Worlds is the principal game and sandbox experience.

The player creates, inherits, or selects a simulated world and develops a
relationship with its history.

There does not need to be one universal victory condition.

Possible player motivations may include:

- sustain a biosphere,
- protect a population,
- create a stable civilization,
- maximize biodiversity,
- accelerate technological development,
- prevent collapse,
- deliberately destabilize a system,
- reach another planet,
- preserve one family for generations,
- observe thousands of years without intervening.

A player may also simply experiment.

The sandbox should not require one moral interpretation of success.

### 8.1 Two Scales of Player Presence

Living Worlds should support two complementary ways of existing in the world.

#### World view

From outside the world, the player can:

- observe planetary and regional systems,
- inspect actors and events,
- move across large distances,
- understand causal relationships,
- advance simulation time,
- intervene at large scales,
- branch history,
- compare outcomes.

This is the player's broad, near-omniscient relationship with the simulation.

#### Manifested presence

The player should also be able to descend into the simulated world and interact
with it from within.

The intended fantasy is not merely:

> I control this world.

It is also:

> I can walk into this world.

A manifested player might eventually be able to:

- move through a settlement,
- approach an individual,
- follow a person or animal directly,
- communicate with inhabitants,
- pick up or move physical things,
- protect someone,
- attack or kill an animal,
- harm or kill a person,
- rescue someone from danger,
- heal or sustain an organism,
- create or destroy physical resources,
- alter local terrain or water,
- construct or destroy structures,
- cause environmental phenomena,
- perform other interventions that inhabitants can directly witness.

The exact capability set remains a design question.

The important decision is that direct presence is part of the core player
fantasy rather than an unrelated future mode.

### 8.2 A Manifested Player Is Causal

When the player enters the world, their actions must become legitimate causes
inside the authoritative simulation.

If the player kills a wolf, the wolf is dead because an actor killed it.

If the player moves a person out of danger, that person's position actually
changes.

If the player creates water, that water enters the world's material and
hydrological state through an explicit intervention.

If the player destroys a structure, inhabitants experience the resulting world
rather than a purely visual effect.

The renderer must not pretend that an intervention occurred.

Player action must enter simulation history.

### 8.3 The World May Perceive the Player

A manifested player should eventually be perceivable by inhabitants when the
simulation supports the required cognition and social systems.

This creates a major long-term source of emergent history.

People may eventually:

- witness the player's actions,
- remember them,
- communicate what they saw,
- misunderstand them,
- fear the player,
- trust the player,
- resent the player,
- seek the player's help,
- attempt to avoid the player,
- create stories about the player,
- form cultural or religious interpretations,
- use claims about the player for political purposes.

None of those reactions should be assumed merely because the player exists.

They should emerge from what inhabitants can perceive, remember, communicate,
and culturally interpret.

A civilization that has never encountered the player should not automatically
know what the player has done elsewhere.

### 8.4 Direct Interaction Is Not the Same as Unit Control

Allowing the player to walk among inhabitants does not require Est to become a
traditional unit-control game.

The player may interact directly with a person without automatically owning
that person's decisions.

For example, the player might:

- speak to someone,
- offer something,
- threaten them,
- rescue them,
- move them physically,
- alter their surroundings,
- issue a request,
- demonstrate extraordinary power.

The person may then react according to their own authoritative state.

This preserves the difference between:

> I interacted with this person.

and:

> I opened this person's control panel and rewrote their intentions.

Whether the game ever permits literal possession or direct control of another
actor remains a separate design question.

### 8.5 Scale Is Part of the Fantasy

The transition between planetary observation and manifested presence should
eventually feel like movement through one continuous world rather than choosing
between unrelated game modes.

At one moment the player may be examining climate across a continent.

At another they may be standing beside one human while a wolf approaches.

Both perspectives describe the same authoritative simulation.

This makes scale itself part of Est's identity:

**The player can care about a planet and one life at the same time.**

### 8.6 Divine Power Does Not Eliminate Consequence

God-like capability should not make consequence meaningless.

Power can be enormous while the world remains causal.

Saving one person may alter a lineage.

Creating a river may transform an ecosystem.

Destroying a predator population may destabilize prey populations.

Providing unlimited food may change settlement patterns and population growth.

Helping one civilization may alter relations with another.

Appearing repeatedly before one population may influence its culture in ways
that persist after the player leaves.

The design opportunity is not to prevent the player from having extraordinary
power.

It is to make extraordinary power produce extraordinary consequences.

### 8.7 Modes May Constrain Power Differently

The player's available powers do not need to be identical in every experience.

Living Worlds may provide broad god-like agency.

A scenario may deliberately restrict the player to a smaller set of
interventions.

Earth Observatory may emphasize observation and controlled experimentation.

Scenario Lab may expose explicit authoring powers that would be inappropriate
inside ordinary play.

Developer tools may remain broader still.

These differences should be intentional product rules rather than accidental
limitations of whichever APIs happen to exist.

### 8.8 Self-Directed Embodied Play

Living Worlds should support open-ended embodied play.

The player should eventually be able to enter the world and ask:

> What do I want to do here today?

The answer does not need to come from a quest, class, mission, or prescribed
role.

A player might decide to:

- become involved in local politics,
- create a religious movement or cult,
- act as a political or social agitator,
- teach people and attempt to increase their knowledge or skills,
- work as a healer,
- patrol roads or a city in a law-enforcement role,
- investigate a crime,
- travel,
- hunt,
- farm,
- build,
- trade,
- operate a business,
- socialize,
- form friendships,
- pursue romantic relationships,
- go to a bar,
- get into a fight,
- rescue someone,
- commit a crime,
- protect a community,
- sabotage a community,
- follow one family for a generation,
- or simply wander and see what happens.

These are not necessarily predefined character classes.

The player's role should emerge primarily from action and from the way the world
responds to those actions.

A player who repeatedly teaches may become known as a teacher.

A player who gathers followers, promotes beliefs, performs extraordinary acts,
and influences leaders may become a religious or political figure because
people interpret and respond to that behavior.

A player who patrols a region and intervenes in crimes may function as law
enforcement if the surrounding institutions recognize or tolerate that role.

The world determines what the player's behavior means socially.

### 8.9 The Player May Be a God Without Always Playing Like One

Extraordinary capability should coexist with ordinary interaction.

The player may be able to perform actions no normal inhabitant could perform.

But the player should also be free to constrain themselves voluntarily and
participate at the scale of ordinary life.

A player might choose to:

- walk instead of teleport,
- drive instead of moving instantly,
- acquire food instead of creating it,
- talk someone into cooperating instead of compelling them,
- earn trust instead of changing disposition directly,
- build something with available materials instead of manifesting it,
- intervene in a fight physically instead of freezing time.

This creates an important form of self-directed play.

**The player may be a god, but does not have to play like one.**

The choice between ordinary action and extraordinary intervention is itself
part of the experience.

### 8.10 Social Roles Should Emerge From Systems

The simulation should avoid assigning social meaning only through arbitrary
labels.

If the player wants to become a teacher, cult leader, police officer, criminal,
political organizer, merchant, healer, or other recognizable social figure,
that role should increasingly emerge from systems such as:

- identity,
- relationships,
- reputation,
- trust,
- memory,
- witnessed events,
- communication,
- knowledge,
- institutions,
- law,
- authority,
- employment,
- economics,
- politics,
- religion,
- culture.

Not all of these systems exist yet.

They represent long-term simulation requirements for credible embodied social
play.

A badge alone should not necessarily make the player a police officer.

A speech alone should not necessarily create followers.

Calling oneself a leader should not make a population obey.

The surrounding people and institutions should determine whether the claimed
role becomes socially real.

### 8.11 Witnesses Matter

What inhabitants actually observe should matter.

If the player performs an extraordinary act in an empty wilderness, distant
civilizations should not automatically know about it.

If ten people witness the player heal someone who appeared certain to die, that
event may enter their memories.

They may tell others.

Their accounts may agree, conflict, exaggerate, or change.

If those stories spread widely enough, the event may eventually affect:

- reputation,
- religion,
- mythology,
- politics,
- social movements,
- institutions,
- historical records.

This allows divine intervention to enter culture through the same world that
experienced it.

### 8.12 Ordinary Life Is Gameplay

Est should not require every meaningful session to involve planetary crisis or
major intervention.

A player spending a simulated day:

- teaching one person,
- traveling a road,
- visiting a settlement,
- forming a relationship,
- working,
- investigating something,
- protecting someone,
- creating conflict,
- or simply observing local life

can still be legitimate gameplay.

Small actions may become historically important later.

They may also remain small.

Both outcomes are valid.

This prevents the enormous scale of Est from making individual-scale experience
feel irrelevant.

### 8.13 Roles Are Not Separate Games

Embodied activities should not become disconnected minigames pasted onto the
simulation.

Driving should occur on authoritative roads in the same world.

Teaching should affect authoritative people and knowledge when those systems
exist.

Crime should interact with authoritative property, law, witnesses, and
institutions.

Politics should involve actual populations, relationships, institutions, and
beliefs.

Religion should develop through actual communication, memory, culture, and
social organization.

Relationships should involve the same people whose births, deaths, families,
movement, and history the wider simulation already tracks.

The objective is not to build many games inside Est.

The objective is to allow many kinds of play to emerge from one world.

### 8.14 Player-Created Purpose

Living Worlds does not need to tell the player what they must accomplish every
time they enter a world.

The player may create their own immediate purpose.

For one session:

> I want to see whether I can become influential in this town.

For another:

> I want to teach this person.

For another:

> I want to protect travelers on this road.

For another:

> I want to cause political chaos.

For another:

> I want to see whether this family survives.

For another:

> I want to change the climate of this continent.

The simulation provides the world.

The player supplies intention.

The world supplies consequences.

### 8.15 Manifestation

Entering the world should eventually be a first-class gameplay action.

The player begins from the broader world relationship and chooses a location
at which to manifest.

Manifestation creates a player presence that participates in the authoritative
world.

The player is no longer only a camera.

They are somewhere.

Other actors may be able to:

- see them,
- hear them,
- approach them,
- avoid them,
- speak to them,
- touch them,
- attack them,
- help them,
- remember them.

The manifested player should therefore have an authoritative location and
physical relationship with the surrounding world.

### 8.16 The Player and the Manifested Body Are Not Identical

The player's persistent identity exists outside any single manifested body.

A manifested body is how that player participates physically inside the world.

This distinction allows embodied play to have physical stakes without requiring
the god-like player to become an ordinary mortal character.

A body may potentially:

- become injured,
- become restrained,
- become unconscious,
- be physically displaced,
- be imprisoned,
- be killed or destroyed.

Such an event does not necessarily end the player's existence.

Instead, destruction of the manifested body may return the player to the
broader world view.

Re-manifestation then becomes possible according to the eventual rules of the
experience.

This creates an important possibility:

> The inhabitants may witness the player die.

And later:

> The inhabitants may witness the player return.

Those events can become part of history.

The exact vulnerability rules remain open.

### 8.17 Embodiment Should Preserve Physical Meaning

While manifested, ordinary physical interaction should matter.

If the player walks somewhere, they actually move through the world.

If the player drives a vehicle, the vehicle moves along authoritative terrain
or infrastructure.

If the player picks up an object, its physical state changes.

If the player strikes someone, an actual physical interaction occurred.

If the player is struck, restrained, or moved, that also occurred.

Embodied play should not be a decorative animation layered over an unrelated
simulation.

The local experience and the planetary simulation must describe the same world.

### 8.18 Ordinary Capability and Extraordinary Power

The manifested player should have access to ordinary actions and may also have
access to extraordinary actions.

Ordinary actions might eventually include:

- walking,
- running,
- climbing,
- driving,
- carrying objects,
- using tools,
- opening doors,
- eating,
- buying,
- trading,
- speaking,
- fighting,
- working,
- teaching.

Extraordinary actions might eventually include:

- instant relocation,
- healing,
- material creation,
- material destruction,
- environmental manipulation,
- extraordinary strength,
- protection,
- restoration,
- large-scale intervention.

These categories should not require separate worlds or characters.

They are different levels of agency available to the same player.

### 8.19 Power Should Be Invoked Deliberately

Extraordinary capability should not constantly interfere with ordinary play.

If the player wants to spend a day behaving like a normal inhabitant, the
interface should support that without accidental divine intervention.

God-like actions should therefore be deliberate.

The player should be able to distinguish between:

> Pick up this chair.

and:

> Move this chair through supernatural force.

Likewise:

> Drive across town.

and:

> Instantly relocate there.

The interface for extraordinary power should preserve the possibility of
ordinary embodied behavior.

### 8.20 Perception and Visibility

The world should not automatically treat every manifestation identically.

Long-term possibilities include:

- openly visible manifestation,
- concealed or disguised manifestation,
- appearance chosen by the player,
- appearance constrained by scenario,
- altered visibility to particular observers,
- manifestations interpreted differently by different cultures.

The authoritative requirement is simpler:

If an inhabitant could perceive an action, that fact may become available to
their cognition and social systems.

If they could not perceive it, they should not gain direct knowledge of it.

Appearance, disguise, concealment, and supernatural visibility remain open
design questions.

### 8.21 Communication

Direct conversation is central to the intended embodied experience.

A player who wants to teach, persuade, threaten, recruit, flirt, negotiate,
investigate, preach, organize, or simply socialize needs to communicate with
inhabitants.

Communication should eventually interact with authoritative systems such as:

- language,
- knowledge,
- memory,
- beliefs,
- relationships,
- trust,
- reputation,
- social context.

Conversation should not exist only as disconnected flavor text.

What is said may eventually change what another actor:

- knows,
- believes,
- remembers,
- intends,
- communicates to others.

The exact natural-language interaction model remains open.

### 8.22 Social Consequence Applies to the Player

The player should not be exempt from social interpretation merely because they
possess extraordinary power.

If the player starts a fight in a bar, witnesses may remember it.

If law enforcement exists, authorities may respond.

If the player repeatedly commits crimes, reputation may spread.

If the player helps a community, inhabitants may develop trust or gratitude.

If the player claims authority, institutions may accept, reject, fear, exploit,
or oppose that claim.

If the player behaves impossibly, witnesses may reinterpret what they believe
about the world.

The player's actions should enter the same social causal network as actions by
other actors wherever the relevant systems exist.

### 8.23 Recognition Is Local and Historical

The player should not have one universal reputation value.

Recognition should eventually depend on history and information flow.

One village may know the player as a healer.

Another may know stories about a dangerous supernatural figure.

A distant population may know nothing about the player.

A government may possess records that ordinary citizens do not.

A religion may interpret the same historical events differently from another
religion.

The player's identity can therefore become part of geography, culture, and
history.

### 8.24 Leaving the World

The player must be able to return from embodied presence to the broader world
view.

Leaving should not erase what happened while manifested.

The world continues.

People remember what they are capable of remembering.

Objects remain where they were left.

Damage remains.

Relationships remain.

Historical events remain.

The manifested session becomes another interval in the world's continuous
history.

### 8.25 Time While Embodied

Extreme time acceleration is probably incompatible with meaningful direct
embodiment.

When the player is physically present, the experience should favor time scales
at which local interaction remains intelligible.

The broader world view may permit much greater acceleration.

The eventual design must determine:

- whether time acceleration is limited while manifested,
- whether the player can pause during direct interaction,
- what happens to embodiment during large time jumps,
- whether the player can remain manifested across years,
- how local and planetary simulation rates remain coherent.

These are implementation and experience questions, but they should be resolved
without creating separate realities.

### 8.26 Death, Return, and Myth

A persistent player combined with a destructible manifestation creates unique
emergent possibilities.

An inhabitant might witness:

- the player being wounded,
- the player apparently dying,
- the body disappearing,
- the player returning later,
- the player returning with a different appearance.

Different people may interpret those facts differently.

Some may understand only that something impossible occurred.

Others may construct stories, doctrines, conspiracies, institutions, or
political claims around it.

Est should not prescribe those interpretations.

It should preserve the events from which interpretation can emerge.

### 8.27 Embodiment Design Questions

The following remain intentionally unresolved:

- Is manifestation always humanoid?
- Can the player customize appearance?
- Can the player age while manifested?
- Does the player experience hunger, fatigue, injury, or pain?
- Can a manifested player permanently lose a body?
- Is re-manifestation immediate or constrained?
- Can the player manifest multiple bodies?
- Can the player possess an existing inhabitant?
- Can inhabitants imprison the player?
- Can technology harm or contain a manifested player?
- Can the player conceal supernatural abilities?
- Can the player pass as an ordinary inhabitant?
- Can the player own property?
- Can the player hold legal office?
- Can the player have recognized employment?
- Can the player form marriages or families?
- Can the player have children?
- How does language work?
- How does direct conversation work?
- How does the camera transition between world view and embodiment?
- How much of the world is rendered and simulated at individual fidelity while
  the player is present?

These questions should be answered according to the central experience:

**The player exists beyond the world, but when they enter it, what they do
there is real.**

### Implementation checkpoint — 2026-09-20

The first playable manifestation foundation now exists.

A stable Ester can be manifested at an authoritative geographic position and
walk through a Babylon local scene while the simulation remains authoritative.
Nearby simulated humans retain stable `PersonId` identity, and local terrain
presentation is derived from the same world rather than being a disconnected
game map.

The current capsules are placeholders, not a commitment to final avatar form.

The immediate embodied-social slice is now intentionally narrow:

    proper human presentation tied to PersonId
      -> physical proximity with the manifested Ester
      -> authoritative encounter
      -> separation
      -> later return
      -> recognition of the same Ester

This is the first concrete implementation of the playable-causal-slice
principle described later in this document.

### 8.28 Power Is Capability, Not a Character Class

The player's extraordinary abilities should not require the player to choose a
separate "god mode" character.

The same persistent player may:

- walk into a bar,
- drive across a city,
- teach a student,
- participate in politics,
- get into a fight,
- and later alter a river or relocate instantly.

These are different expressions of one player identity.

The distinction should come from how the player chooses to act, not from
switching between unrelated games.

### 8.29 Ordinary Play Must Remain Deliberate

Ordinary embodied play remains meaningful even when greater power exists.

The player may intentionally choose to operate within local physical and social
rules.

For example, the player may choose to:

- travel normally rather than teleport,
- earn money rather than create resources,
- persuade rather than compel,
- fight physically rather than use overwhelming force,
- seek legal authority rather than ignore institutions,
- learn local information rather than inspect every hidden state.

The product should make this choice comfortable.

The player should not need to fight the interface in order to behave like an
ordinary inhabitant.

### 8.30 Extraordinary Power Should Not Be Accidental

God-like intervention should require deliberate invocation.

Ordinary controls should favor ordinary actions.

Extraordinary actions should be clearly distinguishable.

The player should not accidentally:

- teleport while trying to walk,
- alter matter while trying to pick something up,
- heal someone while trying to inspect them,
- manipulate weather while navigating the camera.

This protects embodied play from being overwhelmed by the broader power set.

### 8.31 Consequence Is the Primary Cost of Power

Living Worlds should not assume that extraordinary abilities require an
arbitrary energy meter, mana pool, or cooldown merely to create difficulty.

The more important cost is causal consequence.

Using extraordinary power may affect:

- witnesses,
- reputation,
- beliefs,
- religion,
- politics,
- law,
- ecology,
- resources,
- settlement patterns,
- institutions,
- future behavior.

A miracle performed publicly may solve one immediate problem while creating a
century of cultural consequences.

A river created casually may transform an ecosystem.

A person rescued from death may later have descendants who otherwise would not
have existed.

The player may be extraordinarily powerful.

The world should still make that power matter.

### 8.32 Power Does Not Guarantee Desired Outcomes

The player may control an intervention without controlling every consequence.

The player can decide to create water.

The player does not automatically decide everything that follows from the
water.

The player may save a political leader.

The player does not automatically determine how that leader later governs.

The player may teach an idea.

The player does not automatically control how the idea spreads or changes.

This preserves uncertainty and discovery even when the player's capabilities
are enormous.

### 8.33 Knowledge and Power Are Separate

The player may possess broad observational capability without automatically
knowing every hidden fact.

Est should distinguish between:

- what exists in authoritative simulation state,
- what the interface permits the player to inspect,
- what the manifested player could perceive locally,
- what inhabitants know,
- what the player has chosen to discover.

This creates room for investigation and discovery.

A player acting as a detective should not have that experience invalidated
merely because Est internally knows who committed the crime.

Whether the player may deliberately invoke deeper omniscient inspection remains
a design choice.

### 8.34 Presence Can Change the Meaning of Power

The same intervention may have different historical meaning depending on how it
occurs.

Rain appearing while the player is nowhere nearby may be interpreted as
weather.

Rain beginning immediately after the player publicly promises rain may be
interpreted very differently.

Healing a person secretly may alter one life.

Healing the same person before hundreds of witnesses may alter a culture.

Therefore an intervention is not defined only by its physical result.

Context, witnesses, attribution, and information flow may become part of its
social consequence.

### 8.35 Self-Limitation Is a Valid Form of Play

The player should be allowed to create personal rules without the simulation
needing to enforce every one of them.

A player may decide:

> I will live as an ordinary person for ten years.

or:

> I will never reveal my powers to this civilization.

or:

> I will help this town without performing anything visibly supernatural.

or:

> I am going to make them believe I am a god.

All are legitimate ways to interact with the same world.

Future scenarios may formalize such constraints when useful.

The sandbox does not need to.

### 8.36 Some Experiences May Constrain Power

Living Worlds can allow broad freedom while specific scenarios impose stronger
rules.

A scenario may limit:

- available powers,
- manifestation location,
- information access,
- resurrection,
- time manipulation,
- direct material creation,
- intervention scale.

Such constraints should exist because they serve the scenario.

They should not redefine the fundamental player fantasy of the unrestricted
sandbox.

### 8.37 Difficulty Comes From the World, Not From Artificial Helplessness

Est does not need to make the player weak in order to create meaningful play.

Challenge can arise from:

- complex causality,
- incomplete information,
- unintended consequences,
- conflicting goals,
- social resistance,
- institutional inertia,
- ecological feedback,
- large spatial scales,
- long time scales,
- moral ambiguity,
- competing civilizations,
- the impossibility of preserving everything at once.

The player may be able to perform miracles and still discover that managing the
consequences of miracles is difficult.

### 8.38 Power Model Working Principle

The current working principle is:

**Est should not make ordinary life meaningful by pretending the player is not
powerful. It should make ordinary life meaningful because the player chooses
how much power to use, and because every use of power enters a world that
remembers and responds.**

### 8.39 Social Play Requires Social Simulation

Open-ended embodied play cannot be created only through animations, dialogue
choices, or predefined jobs.

If the player wants to act as a teacher, police officer, religious leader,
politician, criminal, business owner, friend, romantic partner, agitator, or
ordinary citizen, the surrounding world must contain enough authoritative
social structure for those activities to mean something.

The long-term requirement is not:

> Give the player a list of roles.

It is:

> Simulate enough of society that recognizable roles can emerge from behavior.

A player should be able to become important because people and institutions
react to what they actually do.

### 8.40 People Need Persistent Identity

A human should eventually be more than a moving population unit.

Individual identity may include authoritative concepts such as:

- identity,
- age,
- body,
- location,
- household,
- family relationships,
- personal history,
- health,
- needs,
- abilities,
- knowledge,
- beliefs,
- memories,
- relationships,
- possessions,
- employment,
- institutional memberships,
- social reputation.

Not every system needs to reach full fidelity at once.

The important design principle is continuity.

The person encountered in a bar should be the same person who has parents,
friends, work, memories, possessions, and a place in world history.

### 8.41 Memory Creates Social Continuity

People need some ability to remember meaningful events.

Memory may eventually include:

- people encountered,
- conversations,
- favors,
- injuries,
- threats,
- crimes witnessed,
- promises,
- extraordinary events,
- deaths,
- relationships,
- important places,
- information learned.

Memory does not need to be perfect.

People may:

- forget,
- misremember,
- disagree,
- reinterpret events,
- learn false information.

The authoritative requirement is that social behavior should have temporal
continuity.

If the player starts a fight with someone on Monday, Tuesday should not begin as
though they have never met.

### 8.42 Knowledge Is Actor-Specific

The simulation should distinguish between world truth and actor knowledge.

A person should only know information available through some causal path.

Possible paths include:

- direct perception,
- conversation,
- education,
- records,
- media,
- institutional communication,
- inference,
- rumor.

This allows investigation, secrecy, deception, education, misinformation, and
discovery to exist naturally.

It also prevents omniscient NPC behavior.

### 8.43 Communication Moves Information

Communication should eventually move authoritative information between actors.

A conversation may transmit:

- facts,
- claims,
- requests,
- threats,
- promises,
- beliefs,
- rumors,
- instructions,
- questions,
- emotional or social signals.

Receiving information does not require believing it.

The listener may:

- accept it,
- reject it,
- doubt it,
- misunderstand it,
- remember it,
- forget it,
- repeat it.

This distinction is important for politics, religion, crime, teaching, and
ordinary relationships.

### 8.44 Natural-Language Dialogue Must Remain Grounded

Natural-language interaction may eventually be an important part of Est.

Generated dialogue must not become an alternate source of simulation truth.

A conversational system may express what an actor:

- knows,
- believes,
- wants,
- remembers,
- fears,
- intends.

It must not silently invent:

- possessions,
- relationships,
- jobs,
- laws,
- historical events,
- institutional authority,
- world facts

that the authoritative simulation does not support.

Language presentation may be generative.

World state remains authoritative.

### 8.45 Relationships Need Direction and History

Social relationships should eventually be more expressive than one universal
friendship score.

Relationships may contain concepts such as:

- familiarity,
- trust,
- affection,
- attraction,
- respect,
- fear,
- resentment,
- obligation,
- loyalty,
- dependency,
- hostility.

These properties may be asymmetric.

One person may trust another who does not trust them.

One person may love another who does not reciprocate.

A government official may respect the player while personally disliking them.

Relationships should evolve from events and interaction.

### 8.46 Skills and Learning Make Teaching Real

For teaching to be meaningful, people need something that can actually change
through learning.

This may eventually include:

- knowledge,
- practical skills,
- literacy,
- technical capability,
- professional expertise,
- cultural knowledge.

Teaching should not simply apply a generic intelligence bonus.

A player acting as a teacher should transmit or develop something represented
in authoritative state.

Learning may depend on:

- teacher capability,
- student capability,
- time,
- communication,
- prior knowledge,
- practice,
- available tools or institutions.

This allows education to become causal rather than cosmetic.

### 8.47 Households, Families, and Intimate Relationships

Individual lives should eventually exist inside durable social structures.

These may include:

- parent-child relationships,
- siblings,
- partners,
- marriages,
- households,
- dependents,
- extended families,
- inheritance.

This gives ordinary events larger consequences.

Saving one person may affect:

- a partner,
- children,
- household income,
- future descendants,
- property,
- social relationships.

A death becomes more than decrementing population.

### 8.48 Property and Ownership

Many forms of embodied play require authoritative ownership and possession.

The world may eventually need to understand:

- personal property,
- household property,
- business property,
- institutional property,
- public property,
- land,
- money,
- resources,
- vehicles.

Without ownership, concepts such as theft, trade, employment, business, taxes,
inheritance, and property crime have little causal meaning.

Ownership rules may vary between societies.

Est should not assume one universal economic model.

### 8.49 Work, Occupation, and Economic Activity

A job should eventually represent participation in an economic or institutional
system rather than a character label.

A person may:

- work,
- produce something,
- provide a service,
- receive compensation,
- own a business,
- hire others,
- lose employment,
- change occupations,
- retire.

This allows the player to participate in ordinary economic life when desired.

A player who spends time teaching for a school should be interacting with an
actual institution, students, schedules, and knowledge transfer when those
systems exist.

### 8.50 Law, Crime, and Authority

Playing a police officer, criminal, judge, investigator, or vigilante requires
more than crime-themed animations.

A society may eventually need authoritative concepts for:

- laws,
- jurisdiction,
- prohibited acts,
- evidence,
- witnesses,
- accusations,
- arrest authority,
- detention,
- courts,
- punishment,
- enforcement institutions.

Different societies may have different laws.

An action may therefore be legal in one place and illegal in another.

The player should not become law enforcement merely by selecting a costume.

Authority should come from the surrounding social and institutional system, or
the player may simply choose to behave as a vigilante and accept the resulting
response.

### 8.51 Institutions Make Roles Durable

Institutions allow society to persist beyond individual actors.

Examples may eventually include:

- households,
- businesses,
- schools,
- religious organizations,
- governments,
- courts,
- police organizations,
- militaries,
- political parties,
- civic organizations,
- universities.

Institutions may possess:

- membership,
- leadership,
- rules,
- property,
- resources,
- authority,
- records,
- goals,
- relationships with other institutions.

This allows roles to survive changes in individual personnel.

### 8.52 Politics Requires Competing Human Interests

Political simulation should emerge from people and institutions with competing
goals, interests, identities, resources, and beliefs.

Potential political concepts may eventually include:

- authority,
- legitimacy,
- leadership,
- representation,
- factions,
- policy,
- elections,
- succession,
- protest,
- coercion,
- negotiation,
- corruption,
- revolution.

The player may participate in those systems.

They may:

- support a faction,
- oppose a government,
- seek office,
- influence policy,
- organize people,
- spread information,
- undermine institutions,
- attempt to seize power.

Political outcomes should not simply be selected from a menu because the player
possesses god-like power.

The people and institutions involved should remain actors.

### 8.53 Religion Requires Belief, Memory, and Community

Religion should not exist merely as a civilization statistic.

It may eventually emerge from interacting systems such as:

- belief,
- witnessed events,
- testimony,
- tradition,
- ritual,
- authority,
- institutions,
- cultural identity,
- sacred places,
- historical narratives.

The player's existence creates unusual possibilities.

A player may deliberately:

- claim divinity,
- deny divinity,
- perform public miracles,
- remain hidden,
- preach doctrines,
- create followers,
- interfere with existing religions.

The world should determine what happens next.

People may believe.

Others may reject the claim.

Different groups may interpret the same event differently.

A religion may form without the player's intention.

A religion created by the player may later evolve into something the player no
longer recognizes.

### 8.54 Culture Gives Meaning to Events

The same action may have different social meanings in different societies.

Culture may eventually influence:

- customs,
- norms,
- taboos,
- family structures,
- clothing,
- language,
- religion,
- status,
- hospitality,
- violence,
- law,
- work,
- political expectations.

Culture should not need to be static.

It may change through:

- migration,
- communication,
- conflict,
- technology,
- generations,
- institutions,
- extraordinary historical events.

This allows the player to encounter genuinely different societies rather than
the same social simulation with different visual themes.

### 8.55 Information Must Travel

Large-scale social consequences require information flow.

Information may move through:

- direct conversation,
- travelers,
- families,
- institutions,
- writing,
- postal systems,
- telecommunications,
- news media,
- digital networks.

The available mechanisms depend on technology and society.

This matters because reputation, politics, religion, law, and rumor should not
propagate instantaneously across the planet without a causal channel.

As civilizations advance technologically, information may travel farther and
faster.

### 8.56 Transportation and Infrastructure Support Ordinary Life

Embodied play requires the world to contain routes and infrastructure people
actually use.

These may eventually include:

- paths,
- roads,
- bridges,
- rail,
- ports,
- airports,
- public transportation,
- utilities,
- buildings.

Vehicles should belong to the same authoritative world.

Driving a police car on an interstate should eventually mean:

- the road exists,
- the vehicle exists,
- the player occupies it,
- traffic and geography matter,
- the destination exists.

Transportation should not become a disconnected driving minigame.

### 8.57 Buildings Need Social Meaning

Buildings should eventually be more than visual geometry.

A building may have:

- location,
- ownership,
- occupants,
- purpose,
- access rules,
- contents,
- condition.

This enables places such as:

- homes,
- bars,
- schools,
- police stations,
- stores,
- hospitals,
- churches,
- government buildings.

A bar becomes meaningful because people actually visit it, work there,
socialize there, buy things there, fight there, and remember what happened
there.

### 8.58 Technology Changes What Society Can Do

Technology should eventually alter real capabilities rather than merely provide
abstract bonuses.

Technological development may enable:

- tools,
- agriculture,
- medicine,
- transportation,
- communication,
- industry,
- energy,
- weapons,
- computation,
- spaceflight.

Technology therefore changes both civilization-scale behavior and embodied
player possibilities.

The player entering an early agricultural society should encounter a different
range of institutions and daily activities than one entering a modern or
interplanetary civilization.

### 8.59 Society Must Continue Without the Player

These systems exist first for the inhabitants, not merely as activities for the
player.

Teachers should be able to teach when the player is absent.

Police should perform law enforcement when the player is elsewhere.

Businesses should operate.

Families should change.

Politics should continue.

Religions should evolve.

Crimes should occur.

Institutions should act.

The player's participation becomes meaningful because they enter a society that
already has processes of its own.

### 8.60 Multi-Scale Social Simulation

Est cannot require maximum individual fidelity for every person on a populated
planet at every moment.

The long-term social simulation must therefore support multiple scales of
representation.

Detailed individual simulation may be appropriate for:

- nearby people,
- followed people,
- historically important people,
- people directly interacting with the player.

More distant populations may require aggregate or lower-frequency
representations.

Changing representation must not casually change authoritative history.

When a person becomes detailed, Est should preserve continuity with what was
already true about that person or population.

This is a simulation architecture problem, but it is also a game-design
requirement.

A world containing billions of people is useful only if the player can still
care about one of them.

### 8.61 Social Simulation Development Principle

These systems do not all need to be built before Est becomes playable.

They provide the long-term dependency map for open-ended embodied society.

The implementation sequence should follow playable causal slices.

For example:

**person -> memory -> relationship -> communication**

can support meaningful recurring encounters before a complete economy exists.

Later:

**knowledge -> learning -> institution**

can make teaching real.

Later:

**property -> law -> witnesses -> enforcement**

can make crime and policing real.

Later:

**belief -> communication -> community -> institution**

can make religious influence real.

Later:

**interests -> institutions -> authority -> policy**

can make politics real.

The objective is not to build an entire civilization simulator before returning
to gameplay.

The objective is to add social systems in slices where each addition creates
new kinds of meaningful play.

### 8.62 Social World Working Principle

The current working principle is:

**The player should not be offered a menu of simulated lives. Est should build
a world rich enough that the player can walk into it and decide what kind of
life, influence, or disruption they want to attempt.**

### 8.63 Shared Worlds

Living Worlds should preserve the possibility that other Est players can enter
the same authoritative world.

A player may invite another player into their world.

A player may also grant another player continuing permission to visit that
world.

Likewise, a player may be invited into someone else's world.

This should not require creating a disconnected multiplayer copy of the world.

The visitor enters the host world's actual history.

### 8.64 Worlds Are Private Unless Shared

A Living World should not automatically become public merely because online
features exist.

The world owner should control who may enter.

Access may eventually include concepts such as:

- private,
- invite-only,
- approved visitors,
- persistent trusted access,
- scenario-specific participation.

The exact account and networking model remains open.

The important product principle is consent:

**Another player enters a world because its owner or authorized controller
allowed them to enter it.**

### 8.65 Visiting Permission Is Not All-or-Nothing

A world owner should eventually be able to decide what another Ester may do.

Possible permission levels may include:

- observe the world,
- inspect permitted information,
- manifest physically,
- interact through ordinary embodied actions,
- communicate with inhabitants,
- use extraordinary abilities,
- perform large-scale interventions,
- control simulation time,
- create branches,
- alter scenario configuration,
- administer world access.

These are conceptual capabilities rather than a final permissions schema.

A player might invite a friend simply to explore.

Another friend might be allowed to manifest but not alter planetary systems.

A trusted collaborator might receive broad god-like intervention authority.

### 8.66 Visitors Become Causal Participants

Once another Ester is permitted to interact with the world, their actions must
follow the same authority rules as the owner's actions.

A visitor who:

- moves an object,
- teaches a person,
- starts a fight,
- kills an animal,
- saves someone,
- builds something,
- destroys something,
- performs a miracle,
- influences politics

has changed the authoritative world.

Those effects should not disappear merely because the visitor disconnects.

The world continues with the consequences.

### 8.67 Player Identity Should Be Attributable

Shared worlds require the simulation to distinguish one player from another.

Meaningful interventions and embodied actions should eventually be attributable
to the player who performed them when the world has enough evidence to support
that attribution.

This matters for:

- history,
- witnesses,
- relationships,
- reputation,
- religion,
- politics,
- permissions,
- administration.

The simulation should distinguish:

> An extraordinary being did this.

from:

> This particular extraordinary being did this.

when inhabitants or records have a causal basis for making that distinction.

### 8.68 Inhabitants May Know Different Esters Differently

Multiple manifested players create new emergent possibilities.

One Ester may become known as a healer.

Another may become feared as a destroyer.

One religion may form around one player.

Another community may follow a different player.

Two Esters may cooperate.

They may undermine each other.

They may attempt to influence the same government.

They may become allies, rivals, competing religious figures, political forces,
or simply two strange people drinking in the same bar.

The simulation should not assume that inhabitants understand what an Ester is.

They experience what those players do.

Interpretation belongs to the inhabitants and their culture.

### 8.69 Players May Interact With Each Other Inside the World

Shared-world embodiment should eventually allow manifested players to encounter
one another directly.

They may potentially:

- speak,
- travel together,
- cooperate,
- trade,
- build,
- teach,
- fight,
- protect one another,
- interfere with one another,
- participate in the same institutions or communities.

When possible, those interactions should use the same world systems used for
interactions with simulated inhabitants.

The multiplayer layer should not create a second reality that sits beside the
simulation.

### 8.70 Shared Power Requires World Authority

Multiple god-like players create special authority problems.

Two players may attempt incompatible actions at nearly the same time.

One may attempt to destroy something another is using.

One may attempt to advance time while another is engaged in embodied
interaction.

One may perform a large intervention another player opposes.

The world must still have one authoritative history.

The eventual networking architecture must therefore resolve concurrent player
actions through a single authoritative world state rather than allowing clients
to invent conflicting realities.

The exact server, host, synchronization, and conflict-resolution architecture
remains open.

### 8.71 Time Requires Coordination

Simulation time is especially important in a shared world.

One Ester should not necessarily be able to accelerate centuries while another
is having a conversation in a bar.

Possible future approaches include:

- owner-controlled time,
- permission-based time control,
- consensus for major acceleration,
- bounded acceleration while players are manifested,
- scenario-specific time rules.

No specific mechanism is chosen yet.

The design requirement is that shared players experience one coherent
authoritative timeline.

### 8.72 Shared Worlds Preserve History

A visitor's departure should not erase what happened during the visit.

If another Ester spends a week in the world and:

- forms relationships,
- teaches people,
- damages property,
- starts a movement,
- helps a settlement,
- kills someone,
- changes an ecosystem,

those consequences remain part of world history.

Future inhabitants may know that visitor through memories, records, stories,
institutions, descendants, or cultural interpretation long after the visitor
has left.

This makes visiting another player's world consequential rather than
spectatorial.

### 8.73 Permission Can Be Revoked

A world owner or authorized administrator should eventually be able to revoke a
visitor's ability to return or restrict what they may do.

Revoking access should stop future participation.

It should not silently rewrite the history that already occurred.

If protection against unwanted consequences is needed, timeline branching,
snapshots, or scenario rules may provide safer experimentation without treating
history as though it never happened.

The exact recovery and moderation model remains open.

### 8.74 Shared Worlds Do Not Replace Single Player

Online participation should expand Living Worlds rather than make persistent
connectivity mandatory.

A player should still be able to create and inhabit a world alone.

The simulation should remain meaningful with:

- one player,
- several invited players,
- or no manifested players at all.

The world remains the central object.

Multiplayer is another way people may enter it.

### 8.75 Visiting Across Different Worlds

A persistent Ester identity may eventually participate in many independent
worlds.

A player may:

- maintain their own world,
- visit a friend's world,
- return home,
- participate in another shared scenario.

These worlds do not need to share physical history.

The same player may therefore acquire very different histories in different
worlds.

One civilization may know them as a benevolent figure.

Another may remember them as a criminal.

Another may never have encountered them.

Player identity can persist across worlds without forcing those worlds into one
shared universe.

### 8.76 Shared-World Open Questions

The following remain intentionally unresolved:

- Is a world hosted locally, remotely, or either?
- Can a world continue running while its owner is offline?
- Can visitors join asynchronously?
- How many Esters may inhabit one world concurrently?
- Can ownership be transferred?
- Can multiple players jointly own a world?
- Can permission be scoped by region or civilization?
- Can visitors use different manifestation forms?
- Can one player physically harm another manifested player?
- What happens when manifested players use incompatible powers?
- Who controls time when multiple players are present?
- Can a visitor create a branch of another player's world?
- Can a host require approval for major interventions?
- How are unwanted or abusive visitors handled?
- Can worlds ever be discoverable publicly?
- Can a world support spectators?
- Can shared worlds later support persistent communities of human players?

These are future product and architecture decisions.

They should remain possible without forcing Est to become a conventional MMO.

### 8.77 Shared-World Working Principle

The current working principle is:

**An Est world belongs to its own continuous history. Its owner may invite
other Esters into that history, grant them meaningful agency, and allow their
actions to become real parts of the world without surrendering control over
who is allowed to participate.**

---

## 9. Consequence Instead of Moral Scoring

The base sandbox should not decide whether the player is good or evil.

A player may:

- conserve an ecosystem,
- destroy it,
- save a settlement,
- abandon it,
- accelerate industrialization,
- suppress industrialization,
- preserve one species at the expense of another,
- intervene constantly,
- never intervene.

The world responds causally.

**Consequences are the primary feedback.**

Individual scenarios may impose explicit goals, constraints, scores, survival
conditions, or ethical roles.

The core simulation should not need to.

---

## 10. Earth Observatory

Earth Observatory emphasizes evidence, inspection, explanation, and real-world
systems.

The user primarily asks:

- What exists?
- What changed?
- Why?
- How are systems connected?
- What happened historically?
- What might happen under a defined scenario?

Earth Observatory may share many interaction primitives with Living Worlds.

The framing is different.

The user is primarily examining the world rather than playing toward a
particular outcome.

---

## 11. Scenario Lab

Scenario Lab emphasizes controlled experimentation.

A scenario workflow may be:

1. select a world state,
2. create a branch,
3. alter one or more assumptions or inputs,
4. advance time,
5. compare outcomes,
6. inspect causal differences.

This is where Est's timeline and branching architecture becomes a major product
feature.

### Simulation is not automatically prediction

Est should distinguish between:

- a simulation,
- a scenario,
- a forecast,
- and a validated prediction.

A modeled future should not be presented as predictive merely because the
engine can calculate it.

Predictive claims require appropriate:

- evidence,
- calibration,
- validation,
- uncertainty treatment,
- domain models,
- and intended-use constraints.

Until those standards are met, simulated futures should be described as
possible or modeled outcomes.

---

## 12. Scale of Agency

Zoom should eventually reveal more than visual detail.

It should reveal different levels of causal structure and possible agency.

### Planetary scale

Potential concerns include:

- orbital forcing,
- atmosphere,
- oceans,
- climate,
- global ecosystems,
- resources,
- civilization-scale conditions,
- major technological transitions.

### Regional scale

Potential concerns include:

- watersheds,
- ecosystems,
- agriculture,
- migration,
- settlements,
- transportation,
- resources,
- disasters.

### Settlement scale

Potential concerns include:

- infrastructure,
- institutions,
- neighborhoods,
- economic activity,
- governance,
- public systems.

### Individual scale

Potential concerns include:

- people,
- animals,
- households,
- relationships,
- work,
- health,
- survival,
- reproduction,
- movement,
- consequences of larger systems.

Different scales may require different representations.

Those representations must still describe the same authoritative world.

---

## 13. Civilization

Civilization should emerge from the simulated world rather than exist only as a
player-owned game board.

A civilization may eventually contain authoritative systems for:

- settlements,
- families,
- population,
- resources,
- agriculture,
- labor,
- trade,
- technology,
- infrastructure,
- institutions,
- culture,
- politics,
- government,
- conflict,
- diplomacy.

These systems should grow from the same causal principles already used for the
physical and biological world.

The long-term objective is not merely to create a conventional civilization
game inside Est.

It is to allow civilization to become another living system within Est.

---

## 14. Long-Term Horizon: Interplanetary Development

Interplanetary civilization is a long-term expansion of the Living Worlds
concept, not the initial product premise.

Est should first prove that one world can become meaningful enough for the
player to care about its history.

Once civilization-scale systems are mature, the same simulation hierarchy may
expand beyond one planet.

This should be a continuation of the same simulation rather than a separate
game pasted onto it.

A civilization may eventually:

- reach orbit,
- construct off-world infrastructure,
- settle moons,
- settle planets,
- develop interplanetary trade,
- create resource dependencies,
- form new political identities,
- seek independence,
- cooperate across worlds,
- conflict across worlds.

The player's conceptual space may therefore expand from:

**person -> settlement -> civilization -> planet -> planetary system**

A sandbox should be able to begin at different stages.

A player should not be forced to simulate centuries solely to access
interplanetary systems.

---

## 15. Game, Science, and Prediction

Est can support all three without treating them as identical.

### Est as science

Scientific models provide:

- structure,
- constraints,
- causal relationships,
- calibration targets,
- explanatory power.

Model assumptions should be inspectable.

Uncertainty should be represented honestly where relevant.

### Est as a game

Gameplay emerges from:

- agency,
- consequence,
- uncertainty,
- attachment,
- discovery,
- history,
- experimentation.

The simulation should not be deliberately falsified merely to guarantee a
desired gameplay outcome.

Presentation may simplify interaction without falsifying authoritative state.

### Est as a predictive platform

Predictive capability may eventually exist in particular validated domains.

It is not the default identity of every simulation Est can run.

---

## 16. Events as the Bridge Between Simulation and Experience

Events are a natural bridge between the existing simulation and the future
experience.

The simulation already produces meaningful transitions such as:

- births,
- pregnancies,
- mating,
- predation,
- deaths,
- feeding,
- migration,
- resource depletion,
- population change.

Many are currently visible mainly as aggregate metrics.

A future event representation may include:

- event type,
- simulation time,
- location,
- actors,
- immediate cause,
- relevant preceding conditions,
- direct consequences,
- links to affected state,
- camera focus,
- history entry.

Events must derive from authoritative simulation outcomes.

Renderer behavior must never create simulation facts.

---

## 17. The First Playable Est Loop

Before adding another major simulation domain, Est should prove one complete
user experience.

The first playable loop should be:

**Enter world -> notice event -> inspect cause -> intervene -> advance time ->
witness consequences -> inspect history**

This is the point at which Est stops being something the user merely watches.

It becomes something the user participates in.

The existing Phase 8 "First Intervention" milestone is a natural foundation for
this loop.

Phase 8 should therefore be considered not merely an API milestone, but a
candidate for Est's first true gameplay milestone.

### 17.1 First Playable Slice

The first playable Est experience does not need civilization, economics,
politics, technology trees, or interplanetary systems.

It needs one world that already feels alive.

A successful first playable slice should prove that the user can:

1. launch Est,
2. enter a living world,
3. immediately understand that meaningful activity is already occurring,
4. notice or be shown an important event,
5. inspect the actors and conditions behind that event,
6. make one legitimate intervention,
7. advance simulation time,
8. see consequences emerge from the simulation,
9. understand at least part of the causal chain,
10. inspect the event in world history.

The slice succeeds if the user becomes curious about what happens next.

### 17.2 First Launch Experience

A first-time launch should minimize configuration.

A possible sequence is:

1. brief Est splash,
2. restrained framing statement,
3. entry into a prepared Living World,
4. planetary view with minimal interface,
5. simulation already active or ready to advance,
6. one meaningful event surfaced to the player.

The first interaction should not be:

> Configure a simulation.

It should be:

> Something is happening in this world.

The player can learn configuration and advanced controls later.

### 17.3 World Event Presentation

Important events should be surfaced through a lightweight event layer.

For example:

> **Human killed by wolf**

Selecting the event might:

- focus the camera on the location,
- identify the human,
- identify the wolf,
- show simulation time,
- show the immediate cause,
- expose relevant preceding conditions,
- link to the actors involved,
- allow inspection of subsequent consequences.

The event presentation must not invent causal explanations.

It should summarize and navigate authoritative simulation evidence.

If the simulation only knows that a wolf attacked a human and the human died,
the interface should say that.

If the simulation also knows that the wolf was starving, prey was scarce, and
the human was nearby, the interface may expose those facts.

Narrative quality should grow with simulation evidence.

### 17.4 Event Importance

Not every simulation transition should interrupt the player.

Est may eventually distinguish between:

- routine state change,
- locally notable events,
- major world events,
- player-caused consequences,
- scenario-critical events.

Importance should be derived from simulation context and player relevance rather
than from renderer activity.

At accelerated simulation speeds, sufficiently important events may pause or
offer to pause time.

### 17.5 First Intervention Design Criteria

The first intervention should be chosen for its ability to demonstrate Est's
core loop rather than for spectacle.

It should:

- act through an existing authoritative domain boundary,
- have understandable immediate intent,
- create consequences that propagate through more than one system when
  possible,
- avoid requiring a large new simulation domain,
- be reversible through timeline branching rather than magical undo,
- produce outcomes that can differ depending on world state,
- remain meaningful whether the result is beneficial, harmful, or mixed.

The first intervention should prove:

**The player changed a condition. The world responded. History became
different.**

### 17.6 First Playable Success Criteria

The first playable milestone is successful when a new user can answer:

- What is this world?
- What is happening?
- Why should I care?
- What can I change?
- What happened because I changed it?
- Where can I see that history?

At that point Est has crossed an important boundary.

It is no longer only a simulation runtime with visualization.

It is a playable living-world experience.

---

## 18. Design Constraints

The following principles should remain true as Est develops:

1. Simulation truth remains authoritative.
2. Rendering never creates simulation facts.
3. Game mechanics act through legitimate domain inputs.
4. Player intervention does not guarantee a desired result.
5. Important consequences should be inspectable.
6. History should preserve meaningful change.
7. Branching should support both experimentation and play.
8. Aggregate and individual simulation may coexist.
9. Different scales may use different representations without creating
   contradictory worlds.
10. Scientific claims must not exceed model evidence.
11. Emergent behavior should be surfaced rather than replaced with scripted
    spectacle.
12. Complexity should be progressively revealed rather than exposed all at
    once.
13. The simulation should continue to make sense when the player does nothing.
14. The world should contain actors with goals that do not depend on player
    commands.

---

## 19. Open Design Questions

The following questions remain intentionally unresolved:

- Does Living Worlds have a default campaign?
- Does the player have an explicit fictional identity?
- Is the player ever directly acknowledged by civilizations?
- How powerful is direct sandbox intervention?
- Can the player possess or directly control an individual?
- Can players begin with a barren planet?
- Can players begin with an existing civilization?
- What does world creation expose?
- How are objectives introduced?
- Are objectives authored, emergent, optional, or all three?
- What constitutes failure?
- What constitutes victory?
- How should technology progression work?
- How should economics become authoritative?
- How should politics become authoritative?
- How should culture become authoritative?
- How should institutions emerge?
- How should civilizations recognize one another?
- How does off-world settlement begin?
- How much narrative summarization should be automatic?
- Should major events interrupt accelerated time?
- Should players be able to follow individuals or families across generations?
- Is there eventually a multiplayer or shared-world mode?
- Is there ever a canonical Est universe, or only independent simulations?

These questions should be resolved from the product thesis rather than through
isolated feature decisions.

---

## 20. Working Definition

The current working definition of Est is:

> **Est is a living-world simulation game where the player can observe a
> world from above, walk within it, choose their own role and purpose, exercise
> extraordinary influence when desired, and become part of the histories that
> emerge from the simulation itself.**

The science makes the world credible.

The simulation makes it alive.

The game gives the player a reason to care what happens next.

Earth Observatory and Scenario Lab expose the same world through scientific and
experimental lenses.

The long-term ambition is for the player's relationship with that world to grow
from individual lives and ecosystems through civilization and, eventually,
across multiple worlds.
