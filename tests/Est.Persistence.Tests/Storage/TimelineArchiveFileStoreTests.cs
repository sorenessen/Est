using System.Text.Json;
using Est.Persistence.Archives;
using Est.Persistence.Storage;
using Est.Simulation.Causality;
using Est.Simulation.Operations;
using Est.Simulation.Time;
using Est.Simulation.Timelines;
using Est.Simulation.Worlds;

namespace Est.Persistence.Tests.Storage;

public class TimelineArchiveFileStoreTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsTimelineArchive()
    {
        var path = CreateTempPath();

        try
        {
            var store =
                new TimelineArchiveFileStore();

            var timeline =
                CreateTimelineWithHistory();

            var provenance =
                CreateProvenance();

            store.Save(
                path,
                timeline,
                provenance);

            var restored =
                store.Load(path);

            Assert.Equal(
                timeline.Id,
                restored.Timeline.Id);

            Assert.Equal(
                timeline.CurrentWorld.Id,
                restored.Timeline.CurrentWorld.Id);

            Assert.Equal(
                timeline.CurrentWorld.CurrentTime,
                restored.Timeline.CurrentWorld.CurrentTime);

            Assert.Equal(
                provenance,
                restored.Provenance);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void Save_CreatesParentDirectory()
    {
        var root =
            Path.Combine(
                Path.GetTempPath(),
                "Est.Tests",
                Guid.NewGuid().ToString("N"));

        var path =
            Path.Combine(
                root,
                "nested",
                "timeline.json");

        try
        {
            var store =
                new TimelineArchiveFileStore();

            store.Save(
                path,
                CreateTimelineWithHistory(),
                CreateProvenance());

            Assert.True(
                File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(
                    root,
                    recursive: true);
            }
        }
    }

    [Fact]
    public void Save_ReplacesExistingArchive()
    {
        var path = CreateTempPath();

        try
        {
            var store =
                new TimelineArchiveFileStore();

            var first =
                CreateTimelineWithHistory();

            var second =
                SimulationTimeline.Create(
                    new WorldState(
                        WorldId.New(),
                        new SimulationTime(999)));

            store.Save(
                path,
                first,
                CreateProvenance());

            store.Save(
                path,
                second,
                CreateProvenance());

            var restored =
                store.Load(path);

            Assert.Equal(
                second.Id,
                restored.Timeline.Id);

            Assert.Equal(
                999,
                restored.Timeline.CurrentWorld
                    .CurrentTime
                    .TotalSeconds);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SaveNew_RejectsExistingArchiveWithoutChangingIt()
    {
        var path = CreateTempPath();

        try
        {
            var store =
                new TimelineArchiveFileStore();

            var first =
                CreateTimelineWithHistory();

            var second =
                SimulationTimeline.Create(
                    new WorldState(
                        WorldId.New(),
                        new SimulationTime(999)));

            store.SaveNew(
                path,
                first,
                CreateProvenance());

            var originalJson =
                File.ReadAllText(path);

            Assert.Throws<IOException>(
                () => store.SaveNew(
                    path,
                    second,
                    CreateProvenance()));

            Assert.Equal(
                originalJson,
                File.ReadAllText(path));

            var restored =
                store.Load(path);

            Assert.Equal(
                first.Id,
                restored.Timeline.Id);

            var directory =
                Path.GetDirectoryName(path)!;

            Assert.Empty(
                Directory.GetFiles(
                    directory,
                    Path.GetFileName(path) + ".*.tmp"));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void Load_RejectsCorruptArchive()
    {
        var path = CreateTempPath();

        try
        {
            File.WriteAllText(
                path,
                "{ not valid json");

            var store =
                new TimelineArchiveFileStore();

            Assert.Throws<JsonException>(
                () => store.Load(path));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void Save_CleansUpTemporaryFile_WhenReplacementFails()
    {
        var path = CreateTempPath();

        try
        {
            Directory.CreateDirectory(path);

            var store =
                new TimelineArchiveFileStore();

            Assert.Throws<IOException>(
                () => store.Save(
                    path,
                    CreateTimelineWithHistory(),
                    CreateProvenance()));

            Assert.True(Directory.Exists(path));

            var directory =
                Path.GetDirectoryName(path)!;

            Assert.Empty(
                Directory.GetFiles(
                    directory,
                    "timeline.json.*.tmp"));
        }
        finally
        {
            var directory =
                Path.GetDirectoryName(path);

            if (!string.IsNullOrWhiteSpace(directory) &&
                Directory.Exists(directory))
            {
                Directory.Delete(
                    directory,
                    recursive: true);
            }
        }
    }

    [Fact]
    public void Load_RejectsUnsupportedArchiveVersion()
    {
        var path = CreateTempPath();

        try
        {
            var store =
                new TimelineArchiveFileStore();

            var json =
                TimelineArchiveSerializer.Serialize(
                    CreateTimelineWithHistory(),
                    CreateProvenance());

            var root =
                System.Text.Json.Nodes.JsonNode
                    .Parse(json)?
                    .AsObject()
                ?? throw new InvalidOperationException(
                    "Serialized archive did not contain a JSON object.");

            root["schemaVersion"] = 999;

            File.WriteAllText(
                path,
                root.ToJsonString());

            Assert.Throws<NotSupportedException>(
                () => store.Load(path));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void Save_RejectsEmptyPath()
    {
        var store =
            new TimelineArchiveFileStore();

        Assert.Throws<ArgumentException>(
            () => store.Save(
                " ",
                CreateTimelineWithHistory(),
                CreateProvenance()));
    }

    [Fact]
    public void Load_RejectsEmptyPath()
    {
        var store =
            new TimelineArchiveFileStore();

        Assert.Throws<ArgumentException>(
            () => store.Load(" "));
    }

    private static string CreateTempPath()
    {
        var directory =
            Path.Combine(
                Path.GetTempPath(),
                "Est.Tests",
                Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);

        return Path.Combine(
            directory,
            "timeline.json");
    }

    private static void DeleteIfExists(
        string path)
    {
        var directory =
            Path.GetDirectoryName(path);

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        if (!string.IsNullOrWhiteSpace(directory) &&
            Directory.Exists(directory))
        {
            Directory.Delete(
                directory,
                recursive: true);
        }
    }

    private static TimelineArchiveProvenance
        CreateProvenance()
    {
        return new TimelineArchiveProvenance(
            "Est",
            "0.1.0-alpha",
            "simulation");
    }

    private static SimulationTimeline
        CreateTimelineWithHistory()
    {
        var world =
            new WorldState(
                WorldId.New(),
                new SimulationTime(100));

        var timeline =
            SimulationTimeline.Create(world);

        var result =
            new SimulationStepResult(
                world.AdvanceBy(60),
                new SimulationChange(
                    new AdvanceTimeOperation(0),
                    "test-step",
                    "Test timeline event.",
                    null,
                    60));

        return timeline
            .RecordStep(result)
            .CreateCheckpoint();
    }
}
