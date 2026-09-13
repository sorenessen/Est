using System.Text.Json;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Persistence.Snapshots;

public static class WorldSnapshotSerializer
{
    public const int CurrentSchemaVersion = 3;
    private const int LegacySchemaVersion = 1;
    private const int PopulationSchemaVersion = 2;

    private static readonly JsonSerializerOptions SerializerOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowDuplicateProperties = false,
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
            WriteIndented = true
        };

    public static string Serialize(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var snapshot = new WorldSnapshot
        {
            SchemaVersion = CurrentSchemaVersion,
            WorldId = world.Id.Value,
            CurrentTimeSeconds = world.CurrentTime.TotalSeconds,
            Planets = world.Planets
                .Select(ToSnapshot)
                .ToArray(),
            Population = world.Population
                .Select(ToSnapshot)
                .ToArray()
        };

        return JsonSerializer.Serialize(
            snapshot,
            SerializerOptions);
    }

    public static WorldState Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException(
                "Snapshot JSON cannot be empty.",
                nameof(json));
        }

        var snapshot = JsonSerializer.Deserialize<WorldSnapshot>(
            json,
            SerializerOptions)
            ?? throw new JsonException(
                "Snapshot JSON did not contain a world.");

        if (snapshot.SchemaVersion != LegacySchemaVersion &&
            snapshot.SchemaVersion != PopulationSchemaVersion &&
            snapshot.SchemaVersion != CurrentSchemaVersion)
        {
            throw new NotSupportedException(
                $"Snapshot schema version {snapshot.SchemaVersion} is not supported.");
        }

        if (snapshot.Planets is null)
        {
            throw new JsonException(
                "Snapshot planets collection is required.");
        }

        var planets = snapshot.Planets
            .Select(FromSnapshot)
            .ToArray();

        PersonState[] population;

        if (snapshot.SchemaVersion == LegacySchemaVersion)
        {
            population = [];
        }
        else
        {
            if (snapshot.Population is null)
            {
                throw new JsonException(
                    "Snapshot population collection is required.");
            }

            population = snapshot.Population
                .Select(
                    person =>
                        FromSnapshot(
                            person,
                            snapshot.SchemaVersion))
                .ToArray();
        }

        return new WorldState(
            new WorldId(snapshot.WorldId),
            new SimulationTime(snapshot.CurrentTimeSeconds),
            planets,
            population);
    }

    private static PersonSnapshot ToSnapshot(PersonState person)
    {
        return new PersonSnapshot
        {
            PersonId = person.Id.Value,
            PlanetId = person.PlanetId.Value,
            Sex = person.Sex,
            BirthTimeSeconds = person.BirthTimeSeconds,
            LatitudeDegrees = person.LatitudeDegrees,
            LongitudeDegrees = person.LongitudeDegrees,
            ParentId = person.ParentId?.Value,
            EnergyReserve = person.Needs.EnergyReserve,
            Health = person.Needs.Health,
            Activity = person.Activity
        };
    }

    private static PersonState FromSnapshot(
        PersonSnapshot snapshot,
        int schemaVersion)
    {
        PersonNeedsState needs;
        PersonActivity activity;

        if (schemaVersion >= CurrentSchemaVersion)
        {
            if (!snapshot.EnergyReserve.HasValue)
            {
                throw new JsonException(
                    "Person energy reserve is required.");
            }

            if (!snapshot.Health.HasValue)
            {
                throw new JsonException(
                    "Person health is required.");
            }

            if (!snapshot.Activity.HasValue)
            {
                throw new JsonException(
                    "Person activity is required.");
            }

            needs =
                new PersonNeedsState(
                    snapshot.EnergyReserve.Value,
                    snapshot.Health.Value);

            activity = snapshot.Activity.Value;
        }
        else
        {
            needs = new PersonNeedsState();
            activity = PersonActivity.Idle;
        }

        return new PersonState(
            new PersonId(snapshot.PersonId),
            new PlanetId(snapshot.PlanetId),
            snapshot.Sex,
            snapshot.BirthTimeSeconds,
            snapshot.LatitudeDegrees,
            snapshot.LongitudeDegrees,
            snapshot.ParentId.HasValue
                ? new PersonId(snapshot.ParentId.Value)
                : null,
            needs,
            activity);
    }

    private static PlanetSnapshot ToSnapshot(PlanetState planet)
    {
        return new PlanetSnapshot
        {
            PlanetId = planet.Id.Value,
            Name = planet.Name,
            MassKilograms = planet.MassKilograms,
            MeanRadiusMeters = planet.MeanRadiusMeters,
            Environment = new PlanetEnvironmentSnapshot
            {
                MeanSurfaceTemperatureKelvin =
                    planet.Environment.MeanSurfaceTemperatureKelvin,
                SurfaceWaterFraction =
                    planet.Environment.SurfaceWaterFraction,
                IceCoverageFraction =
                    planet.Environment.IceCoverageFraction,
                Atmosphere = new AtmosphereSnapshot
                {
                    SurfacePressurePascals =
                        planet.Environment.Atmosphere.SurfacePressurePascals,
                    CompositionByMoleFraction =
                        planet.Environment.Atmosphere
                            .CompositionByMoleFraction
                            .ToDictionary(
                                pair => pair.Key,
                                pair => pair.Value,
                                StringComparer.Ordinal)
                }
            }
        };
    }

    private static PlanetState FromSnapshot(PlanetSnapshot snapshot)
    {
        if (snapshot.Environment is null)
        {
            throw new JsonException(
                "Planet environment is required.");
        }

        if (snapshot.Environment.Atmosphere is null)
        {
            throw new JsonException(
                "Planet atmosphere is required.");
        }

        if (snapshot.Environment.Atmosphere.CompositionByMoleFraction is null)
        {
            throw new JsonException(
                "Atmospheric composition is required.");
        }

        var atmosphere = new AtmosphereState(
            snapshot.Environment.Atmosphere.SurfacePressurePascals,
            snapshot.Environment.Atmosphere.CompositionByMoleFraction);

        var environment = new PlanetEnvironment(
            snapshot.Environment.MeanSurfaceTemperatureKelvin,
            snapshot.Environment.SurfaceWaterFraction,
            snapshot.Environment.IceCoverageFraction,
            atmosphere);

        return new PlanetState(
            new PlanetId(snapshot.PlanetId),
            snapshot.Name
                ?? throw new JsonException(
                    "Planet name is required."),
            snapshot.MassKilograms,
            snapshot.MeanRadiusMeters,
            environment);
    }

    private sealed class WorldSnapshot
    {
        public required int SchemaVersion { get; set; }
        public required Guid WorldId { get; set; }
        public required long CurrentTimeSeconds { get; set; }
        public required PlanetSnapshot[] Planets { get; set; }
        public PersonSnapshot[]? Population { get; set; }
    }

    private sealed class PersonSnapshot
    {
        public required Guid PersonId { get; set; }
        public required Guid PlanetId { get; set; }
        public required PersonSex Sex { get; set; }
        public required long BirthTimeSeconds { get; set; }
        public required double LatitudeDegrees { get; set; }
        public required double LongitudeDegrees { get; set; }
        public Guid? ParentId { get; set; }
        public double? EnergyReserve { get; set; }
        public double? Health { get; set; }
        public PersonActivity? Activity { get; set; }
    }

    private sealed class PlanetSnapshot
    {
        public required Guid PlanetId { get; set; }
        public required string Name { get; set; }
        public required double MassKilograms { get; set; }
        public required double MeanRadiusMeters { get; set; }
        public required PlanetEnvironmentSnapshot Environment { get; set; }
    }

    private sealed class PlanetEnvironmentSnapshot
    {
        public required double MeanSurfaceTemperatureKelvin { get; set; }
        public required double SurfaceWaterFraction { get; set; }
        public required double IceCoverageFraction { get; set; }
        public required AtmosphereSnapshot Atmosphere { get; set; }
    }

    private sealed class AtmosphereSnapshot
    {
        public required double SurfacePressurePascals { get; set; }
        public required Dictionary<string, double> CompositionByMoleFraction
        {
            get;
            set;
        }
    }
}
