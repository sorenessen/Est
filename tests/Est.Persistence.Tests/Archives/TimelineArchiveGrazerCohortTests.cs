using Est.Persistence.Archives;
using Est.Simulation.Causality;
using Est.Simulation.Definitions;
using Est.Simulation.Grazers;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Time;
using Est.Simulation.Timelines;
using Est.Simulation.Worlds;

namespace Est.Persistence.Tests.Archives;

public sealed class TimelineArchiveGrazerCohortTests
{
    [Fact]
    public void RoundTrip_PreservesGrazerCohortsInCurrentWorldAndCheckpoints()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Grazer Timeline World",
                5.0e24,
                6_000_000,
                new PlanetEnvironment(
                    288,
                    0.60,
                    0.05,
                    AtmosphereState.Vacuum));

        var cohort =
            new GrazerCohortState(
                GrazerCohortId.New(),
                planet.Id,
                400,
                35,
                -120);

        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(
                    100),
                [planet],
                [],
                grazerCohorts:
                [
                    cohort
                ]);

        var timeline =
            SimulationTimeline.Create(
                world);

        var step =
            new SimulationStepResult(
                world.AdvanceBy(
                    60),
                new SimulationChange(
                    new AdvanceTimeOperation(
                        0),
                    "grazer-cohort-test-step",
                    "Grazer cohort persistence test.",
                    planet.Id,
                    60));

        timeline =
            timeline
                .RecordStep(
                    step)
                .CreateCheckpoint();

        var archive =
            TimelineArchiveSerializer.Deserialize(
                TimelineArchiveSerializer.Serialize(
                    timeline,
                    SimulationDefinition.Empty,
                    new TimelineArchiveProvenance(
                        "Est.Tests",
                        "1.0",
                        "test")));

        Assert.Equal(
            cohort,
            Assert.Single(
                archive.Timeline
                    .CurrentWorld
                    .GrazerCohorts));

        Assert.All(
            archive.Timeline.Checkpoints,
            checkpoint =>
                Assert.Equal(
                    cohort,
                    Assert.Single(
                        checkpoint.World
                            .GrazerCohorts)));
    }
}
