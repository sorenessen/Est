using Est.Simulation.Operations;
using Est.Simulation.Causality;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Social;
using Est.Simulation.Time;
using Est.Simulation.Timelines;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Timelines;

public class SimulationTimelineTests
{
    [Fact]
    public void Create_EstablishesInitialCheckpoint()
    {
        var world = new WorldState(
            WorldId.New(),
            new SimulationTime(100));

        var timeline = SimulationTimeline.Create(world);

        Assert.NotEqual(Guid.Empty, timeline.Id.Value);
        Assert.Equal(
            timeline.Id,
            timeline.InitialCheckpoint.TimelineId);

        Assert.Equal(
            world.Id,
            timeline.CurrentWorld.Id);

        Assert.Equal(
            100,
            timeline.CurrentWorld.CurrentTime.TotalSeconds);

        Assert.Single(timeline.Checkpoints);
        Assert.Same(
            timeline.InitialCheckpoint,
            timeline.Checkpoints[0]);
    }

    [Fact]
    public void Create_DoesNotShareMutableWorldContainer()
    {
        var world = new WorldState(
            WorldId.New(),
            SimulationTime.Zero);

        var timeline = SimulationTimeline.Create(world);

        Assert.NotSame(world, timeline.CurrentWorld);
        Assert.NotSame(
            world,
            timeline.InitialCheckpoint.World);
    }
    [Fact]
    public void RecordStep_AppendsEventAndUpdatesCurrentWorld()
    {
        var world = new WorldState(
            WorldId.New(),
            new SimulationTime(100));

        var timeline =
            SimulationTimeline.Create(world);

        var result = CreateStepResult(
            world,
            60);

        var updated =
            timeline.RecordStep(result);

        Assert.Equal(
            160,
            updated.CurrentWorld.CurrentTime.TotalSeconds);

        var timelineEvent =
            Assert.Single(updated.Events);

        Assert.Equal(
            updated.Id,
            timelineEvent.TimelineId);

        Assert.Equal(
            160,
            timelineEvent.OccurredAt.TotalSeconds);

        Assert.Equal(
            "test-step",
            timelineEvent.Cause);
    }

    [Fact]
    public void RecordStep_DoesNotModifySourceTimeline()
    {
        var world = new WorldState(
            WorldId.New(),
            new SimulationTime(100));

        var timeline =
            SimulationTimeline.Create(world);

        var updated =
            timeline.RecordStep(
                CreateStepResult(
                    world,
                    60));

        Assert.Empty(timeline.Events);

        Assert.Equal(
            100,
            timeline.CurrentWorld.CurrentTime.TotalSeconds);

        Assert.Single(updated.Events);

        Assert.Equal(
            160,
            updated.CurrentWorld.CurrentTime.TotalSeconds);
    }

    [Fact]
    public void RecordStep_RejectsDifferentWorld()
    {
        var timeline =
            SimulationTimeline.Create(
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero));

        var otherWorld =
            new WorldState(
                WorldId.New(),
                new SimulationTime(60));

        var result =
            new Est.Simulation.Causality.SimulationStepResult(
                otherWorld,
                new Est.Simulation.Causality.SimulationChange(
                    new Est.Simulation.Operations.AdvanceTimeOperation(0),
                    "test-step",
                    "Test step.",
                    null,
                    60));

        Assert.Throws<InvalidOperationException>(
            () => timeline.RecordStep(result));
    }

    private static Est.Simulation.Causality.SimulationStepResult
        CreateStepResult(
            WorldState world,
            long elapsedSeconds)
    {
        return new Est.Simulation.Causality.SimulationStepResult(
            world.AdvanceBy(elapsedSeconds),
            new Est.Simulation.Causality.SimulationChange(
                new Est.Simulation.Operations.AdvanceTimeOperation(0),
                "test-step",
                "Test step.",
                null,
                elapsedSeconds));
    }

    [Fact]
    public void CreateCheckpoint_AppendsCheckpointWithoutChangingHistory()
    {
        var world = new WorldState(
            WorldId.New(),
            new SimulationTime(100));

        var timeline =
            SimulationTimeline.Create(world);

        var updated =
            timeline.CreateCheckpoint();

        Assert.Single(timeline.Checkpoints);
        Assert.Equal(2, updated.Checkpoints.Length);

        Assert.Equal(
            timeline.Id,
            updated.Id);

        Assert.Equal(
            100,
            updated.Checkpoints[1]
                .Time
                .TotalSeconds);
    }

    [Fact]
    public void ForkFromCheckpoint_CreatesChildTimeline()
    {
        var world = new WorldState(
            WorldId.New(),
            new SimulationTime(100));

        var parent =
            SimulationTimeline.Create(world);

        var child =
            parent.ForkFromCheckpoint(
                parent.InitialCheckpoint.Id);

        Assert.NotEqual(
            parent.Id,
            child.Id);

        Assert.Equal(
            parent.Id,
            child.ParentTimelineId);

        Assert.Equal(
            parent.InitialCheckpoint.Id,
            child.ParentCheckpointId);
    }

    [Fact]
    public void ForkFromCheckpoint_CreatesDistinctWorldIdentity()
    {
        var world = new WorldState(
            WorldId.New(),
            new SimulationTime(100));

        var parent =
            SimulationTimeline.Create(world);

        var child =
            parent.ForkFromCheckpoint(
                parent.InitialCheckpoint.Id);

        Assert.NotEqual(
            parent.CurrentWorld.Id,
            child.CurrentWorld.Id);

        Assert.Equal(
            parent.InitialCheckpoint.World.CurrentTime,
            child.CurrentWorld.CurrentTime);
    }

    [Fact]
    public void ForkFromCheckpoint_DoesNotModifyParentTimeline()
    {
        var world = new WorldState(
            WorldId.New(),
            new SimulationTime(100));

        var parent =
            SimulationTimeline.Create(world);

        var child =
            parent.ForkFromCheckpoint(
                parent.InitialCheckpoint.Id);

        Assert.Null(parent.ParentTimelineId);
        Assert.Null(parent.ParentCheckpointId);
        Assert.Empty(parent.Events);
        Assert.Single(parent.Checkpoints);

        Assert.NotEqual(
            parent.Id,
            child.Id);
    }

    [Fact]
    public void ForkFromCheckpoint_RejectsUnknownCheckpoint()
    {
        var timeline =
            SimulationTimeline.Create(
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero));

        Assert.Throws<InvalidOperationException>(
            () => timeline.ForkFromCheckpoint(
                Guid.NewGuid()));
    }

    [Fact]
    public void ResetToInitial_CreatesChildAtInitialState()
    {
        var original =
            new WorldState(
                WorldId.New(),
                new SimulationTime(100));

        var timeline =
            SimulationTimeline.Create(original);

        var advanced =
            timeline.RecordStep(
                CreateStepResult(
                    original,
                    60));

        var reset =
            advanced.ResetToInitial();

        Assert.Equal(
            100,
            reset.CurrentWorld
                .CurrentTime
                .TotalSeconds);

        Assert.Equal(
            advanced.Id,
            reset.ParentTimelineId);

        Assert.Equal(
            advanced.InitialCheckpoint.Id,
            reset.ParentCheckpointId);

        Assert.NotEqual(
            advanced.CurrentWorld.Id,
            reset.CurrentWorld.Id);
    }

    [Fact]
    public void ForkFromLaterCheckpoint_StartsAtCheckpointState()
    {
        var original =
            new WorldState(
                WorldId.New(),
                new SimulationTime(100));

        var timeline =
            SimulationTimeline.Create(original);

        var advanced =
            timeline.RecordStep(
                CreateStepResult(
                    original,
                    60));

        var checkpointed =
            advanced.CreateCheckpoint();

        var laterCheckpoint =
            checkpointed.Checkpoints[1];

        var child =
            checkpointed.ForkFromCheckpoint(
                laterCheckpoint.Id);

        Assert.Equal(
            160,
            child.CurrentWorld
                .CurrentTime
                .TotalSeconds);

        Assert.Equal(
            laterCheckpoint.Id,
            child.ParentCheckpointId);
    }

    [Fact]
    public void ForkFromEarlierCheckpoint_DoesNotInheritParentFuture()
    {
        var original =
            new WorldState(
                WorldId.New(),
                new SimulationTime(100));

        var timeline =
            SimulationTimeline.Create(original);

        var advanced =
            timeline.RecordStep(
                CreateStepResult(
                    original,
                    60));

        var checkpointed =
            advanced.CreateCheckpoint();

        var advancedAgain =
            checkpointed.RecordStep(
                CreateStepResult(
                    checkpointed.CurrentWorld,
                    60));

        var child =
            advancedAgain.ForkFromCheckpoint(
                advancedAgain.InitialCheckpoint.Id);

        Assert.Equal(
            100,
            child.CurrentWorld
                .CurrentTime
                .TotalSeconds);

        Assert.Empty(child.Events);
        Assert.Single(child.Checkpoints);

        Assert.Equal(
            2,
            advancedAgain.Events.Length);

        Assert.Equal(
            2,
            advancedAgain.Checkpoints.Length);
    }


    [Fact]
    public void CreateCheckpoint_PreservesHistoricalSocialRecognition()
    {
        var (world, person) =
            CreateSocialWorld();

        var actor =
            SocialActorIdentity.ForEster(
                EsterId.New());

        var timeline =
            SimulationTimeline.Create(
                world);

        var recognizedWorld =
            new RecordPersonSocialEncounterOperation(
                person.Id,
                actor)
            .Apply(
                timeline.CurrentWorld);

        var checkpointed =
            timeline
                .RecordStep(
                    CreateStepResult(
                        recognizedWorld,
                        60))
                .CreateCheckpoint();

        var initialPerson =
            Assert.Single(
                checkpointed
                    .InitialCheckpoint
                    .World
                    .Population);

        var laterPerson =
            Assert.Single(
                checkpointed
                    .Checkpoints[1]
                    .World
                    .Population);

        Assert.False(
            initialPerson.SocialState
                .HasEncountered(actor));

        Assert.True(
            laterPerson.SocialState
                .HasEncountered(actor));
    }

    [Fact]
    public void ForkFromCheckpoint_InheritsSocialStateAndDivergesIndependently()
    {
        var inheritedActor =
            SocialActorIdentity.ForEster(
                EsterId.New());

        var parentOnlyActor =
            SocialActorIdentity.ForEster(
                EsterId.New());

        var childOnlyActor =
            SocialActorIdentity.ForEster(
                EsterId.New());

        var inheritedState =
            new PersonSocialState()
                .RecordEncounter(
                    inheritedActor,
                    encounterTimeSeconds: 50);

        var (world, person) =
            CreateSocialWorld(
                inheritedState);

        var timeline =
            SimulationTimeline.Create(
                world);

        var parentChangedWorld =
            new RecordPersonSocialEncounterOperation(
                person.Id,
                parentOnlyActor)
            .Apply(
                timeline.CurrentWorld);

        var parent =
            timeline.RecordStep(
                CreateStepResult(
                    parentChangedWorld,
                    60));

        var child =
            parent.ForkFromCheckpoint(
                parent.InitialCheckpoint.Id);

        var parentPerson =
            Assert.Single(
                parent.CurrentWorld.Population);

        var childPerson =
            Assert.Single(
                child.CurrentWorld.Population);

        Assert.True(
            childPerson.SocialState
                .HasEncountered(
                    inheritedActor));

        Assert.False(
            childPerson.SocialState
                .HasEncountered(
                    parentOnlyActor));

        Assert.True(
            parentPerson.SocialState
                .HasEncountered(
                    inheritedActor));

        Assert.True(
            parentPerson.SocialState
                .HasEncountered(
                    parentOnlyActor));

        var changedChildWorld =
            new RecordPersonSocialEncounterOperation(
                person.Id,
                childOnlyActor)
            .Apply(
                child.CurrentWorld);

        var changedChildPerson =
            Assert.Single(
                changedChildWorld.Population);

        Assert.True(
            changedChildPerson.SocialState
                .HasEncountered(
                    childOnlyActor));

        Assert.False(
            childPerson.SocialState
                .HasEncountered(
                    childOnlyActor));

        Assert.False(
            parentPerson.SocialState
                .HasEncountered(
                    childOnlyActor));

        Assert.NotEqual(
            parent.CurrentWorld.Id,
            child.CurrentWorld.Id);
    }

    private static (
        WorldState World,
        PersonState Person)
        CreateSocialWorld(
            PersonSocialState? socialState = null)
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Social Timeline World",
                5.9722e24,
                6_371_000,
                new PlanetEnvironment(
                    288.15,
                    0.71,
                    0.03,
                    AtmosphereState.Vacuum));

        var person =
            new PersonState(
                PersonId.New(),
                planet.Id,
                PersonSex.Female,
                birthTimeSeconds: 0,
                latitudeDegrees: 10,
                longitudeDegrees: 20,
                socialState: socialState);

        return (
            new WorldState(
                WorldId.New(),
                new SimulationTime(100),
                [planet],
                [person]),
            person);
    }

    [Fact]
    public void RecordStep_MultipleChangesPreserveOrderAndAdvanceTimeOnce()
    {
        var world = new WorldState(
            WorldId.New(),
            new SimulationTime(100));

        var timeline = SimulationTimeline.Create(world);

        var first = new SimulationChange(
            new AdvanceTimeOperation(0),
            "first-system",
            "First causal change.",
            null,
            60);

        var second = new SimulationChange(
            new AdvanceTimeOperation(0),
            "second-system",
            "Second causal change.",
            null,
            60);

        var result = new SimulationStepResult(
            world.AdvanceBy(60),
            60,
            [first, second]);

        var recorded = timeline.RecordStep(result);

        Assert.Equal(
            160,
            recorded.CurrentWorld.CurrentTime.TotalSeconds);

        Assert.Equal(2, recorded.Events.Length);

        Assert.Equal("first-system", recorded.Events[0].Cause);
        Assert.Equal("second-system", recorded.Events[1].Cause);

        Assert.All(
            recorded.Events,
            timelineEvent =>
                Assert.Equal(
                    160,
                    timelineEvent.OccurredAt.TotalSeconds));

        Assert.Equal(
            100,
            timeline.CurrentWorld.CurrentTime.TotalSeconds);

        Assert.Empty(timeline.Events);
    }


}
