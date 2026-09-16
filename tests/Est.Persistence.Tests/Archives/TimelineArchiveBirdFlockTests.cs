using Est.Persistence.Archives;
using Est.Simulation.Birds;
using Est.Simulation.Causality;
using Est.Simulation.Definitions;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Time;
using Est.Simulation.Timelines;
using Est.Simulation.Worlds;

namespace Est.Persistence.Tests.Archives;

public sealed class TimelineArchiveBirdFlockTests
{
    [Fact]
    public void RoundTrip_PreservesBirdFlocksInCurrentWorldAndCheckpoints()
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Bird Timeline World",
                5.0e24,
                6_000_000,
                new PlanetEnvironment(
                    288,
                    0.60,
                    0.05,
                    AtmosphereState.Vacuum));

        var flock =
            new BirdFlockState(
                BirdFlockId.New(),
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
                birdFlocks:
                [
                    flock
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
                    "bird-flock-test-step",
                    "Bird flock persistence test.",
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
            flock,
            Assert.Single(
                archive.Timeline
                    .CurrentWorld
                    .BirdFlocks));

        Assert.All(
            archive.Timeline.Checkpoints,
            checkpoint =>
                Assert.Equal(
                    flock,
                    Assert.Single(
                        checkpoint.World
                            .BirdFlocks)));
    }
}
