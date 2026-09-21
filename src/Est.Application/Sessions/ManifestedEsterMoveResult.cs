using System.Collections.Immutable;
using Est.Simulation.Players;
using Est.Simulation.Population;
using Est.Simulation.Timelines;

namespace Est.Application.Sessions;

public sealed record ManifestedEsterMoveResult(
    SimulationTimeline Timeline,
    ManifestedEsterState Manifestation,
    ImmutableArray<PersonEncounterResult> Encounters);

public sealed record PersonEncounterResult(
    PersonId PersonId,
    bool RecognizedBeforeEncounter,
    long EncounterCountBefore,
    long EncounterCountAfter);
