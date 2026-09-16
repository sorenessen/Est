using System.Text.Json;
using Est.Persistence.Snapshots;
using Est.Simulation.Birds;
using Est.Simulation.Climate;
using Est.Simulation.Definitions;
using Est.Simulation.Ecology;
using Est.Simulation.Hydrology;
using Est.Simulation.Invertebrates;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Time;
using Est.Simulation.Timelines;
using Est.Simulation.Vegetation;

namespace Est.Persistence.Archives;

public static class TimelineArchiveSerializer
{
    public const int CurrentSchemaVersion = 10;
    private const int BirdBehaviorSchemaVersion = 10;
    private const int BirdModelSchemaVersion = 9;
    private const int InvertebrateModelSchemaVersion = 8;
    private const int VegetationForagingSchemaVersion = 7;
    private const int VegetationModelSchemaVersion = 6;
    private const int HydrologyModelSchemaVersion = 5;
    private const int ReproductiveBehaviorSchemaVersion = 4;
    private const int PopulationModelSchemaVersion = 3;
    private const int DefinitionSchemaVersion = 2;
    private const int LegacySchemaVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowDuplicateProperties = false,
            UnmappedMemberHandling =
                System.Text.Json.Serialization
                    .JsonUnmappedMemberHandling.Disallow,
            WriteIndented = true
        };

    public static string Serialize(
        SimulationTimeline timeline,
        TimelineArchiveProvenance provenance)
    {
        return Serialize(
            timeline,
            SimulationDefinition.Empty,
            provenance);
    }

    public static string Serialize(
        SimulationTimeline timeline,
        SimulationDefinition definition,
        TimelineArchiveProvenance provenance)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(provenance);

        definition.ValidateFor(timeline.CurrentWorld);

        var archive = new TimelineArchiveSnapshot
        {
            SchemaVersion = CurrentSchemaVersion,
            Provenance = new ProvenanceSnapshot
            {
                Producer = provenance.Producer,
                ProducerVersion = provenance.ProducerVersion,
                Origin = provenance.Origin
            },
            Definition = ToDefinitionSnapshot(definition),
            TimelineId = timeline.Id.Value,
            ParentTimelineId =
                timeline.ParentTimelineId?.Value,
            ParentCheckpointId =
                timeline.ParentCheckpointId,
            CurrentWorld = ToWorldElement(
                timeline.CurrentWorld),
            Checkpoints = timeline.Checkpoints
                .Select(ToCheckpointSnapshot)
                .ToArray(),
            Events = timeline.Events
                .Select(ToEventSnapshot)
                .ToArray()
        };

        return JsonSerializer.Serialize(
            archive,
            SerializerOptions);
    }

    public static TimelineArchive Deserialize(
        string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException(
                "Timeline archive JSON cannot be empty.",
                nameof(json));
        }

        var archive =
            JsonSerializer.Deserialize<TimelineArchiveSnapshot>(
                json,
                SerializerOptions)
            ?? throw new JsonException(
                "Archive JSON did not contain a timeline.");

        if (archive.SchemaVersion != LegacySchemaVersion &&
            archive.SchemaVersion != DefinitionSchemaVersion &&
            archive.SchemaVersion != PopulationModelSchemaVersion &&
            archive.SchemaVersion != ReproductiveBehaviorSchemaVersion &&
            archive.SchemaVersion != HydrologyModelSchemaVersion &&
            archive.SchemaVersion != VegetationModelSchemaVersion &&
            archive.SchemaVersion != VegetationForagingSchemaVersion &&
            archive.SchemaVersion != InvertebrateModelSchemaVersion &&
            archive.SchemaVersion != BirdModelSchemaVersion &&
            archive.SchemaVersion != CurrentSchemaVersion)
        {
            throw new NotSupportedException(
                $"Timeline archive schema version {archive.SchemaVersion} is not supported.");
        }

        if (archive.Provenance is null)
        {
            throw new JsonException(
                "Archive provenance is required.");
        }

        if (archive.Checkpoints is null ||
            archive.Checkpoints.Length == 0)
        {
            throw new JsonException(
                "Archive checkpoints are required.");
        }

        if (archive.Events is null)
        {
            throw new JsonException(
                "Archive events collection is required.");
        }

        var timelineId =
            new TimelineId(
                archive.TimelineId);

        var checkpoints = archive.Checkpoints
            .Select(snapshot =>
                FromCheckpointSnapshot(
                    timelineId,
                    snapshot))
            .ToArray();

        var events = archive.Events
            .Select(snapshot =>
                FromEventSnapshot(
                    timelineId,
                    snapshot))
            .ToArray();

        var currentWorld =
            FromWorldElement(
                archive.CurrentWorld);

        var definition =
            archive.SchemaVersion == LegacySchemaVersion
                ? SimulationDefinition.Empty
                : FromDefinitionSnapshot(
                    archive.Definition
                    ?? throw new JsonException(
                        "Archive simulation definition is required."),
                    archive.SchemaVersion);

        definition.ValidateFor(currentWorld);

        var timeline =
            SimulationTimeline.Restore(
                timelineId,
                archive.ParentTimelineId.HasValue
                    ? new TimelineId(
                        archive.ParentTimelineId.Value)
                    : null,
                archive.ParentCheckpointId,
                checkpoints[0],
                currentWorld,
                checkpoints,
                events);

        var provenance =
            new TimelineArchiveProvenance(
                archive.Provenance.Producer
                    ?? throw new JsonException(
                        "Archive producer is required."),
                archive.Provenance.ProducerVersion
                    ?? throw new JsonException(
                        "Archive producer version is required."),
                archive.Provenance.Origin
                    ?? throw new JsonException(
                        "Archive origin is required."));

        return new TimelineArchive(
            timeline,
            definition,
            provenance);
    }

    private static CheckpointSnapshot ToCheckpointSnapshot(
        SimulationCheckpoint checkpoint)
    {
        return new CheckpointSnapshot
        {
            CheckpointId = checkpoint.Id,
            World = ToWorldElement(
                checkpoint.World)
        };
    }

    private static SimulationCheckpoint
        FromCheckpointSnapshot(
            TimelineId timelineId,
            CheckpointSnapshot snapshot)
    {
        return new SimulationCheckpoint(
            snapshot.CheckpointId,
            timelineId,
            FromWorldElement(snapshot.World));
    }

    private static TimelineEventSnapshot ToEventSnapshot(
        TimelineEvent timelineEvent)
    {
        return new TimelineEventSnapshot
        {
            EventId = timelineEvent.Id,
            OccurredAtSeconds =
                timelineEvent.OccurredAt.TotalSeconds,
            Cause = timelineEvent.Cause,
            Summary = timelineEvent.Summary,
            AffectedPlanetId =
                timelineEvent.AffectedPlanetId?.Value,
            ElapsedSeconds =
                timelineEvent.ElapsedSeconds,
            Metrics = timelineEvent.Metrics
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value,
                    StringComparer.Ordinal)
        };
    }

    private static TimelineEvent FromEventSnapshot(
        TimelineId timelineId,
        TimelineEventSnapshot snapshot)
    {
        if (snapshot.Metrics is null)
        {
            throw new JsonException(
                "Timeline event metrics are required.");
        }

        return new TimelineEvent(
            snapshot.EventId,
            timelineId,
            new SimulationTime(
                snapshot.OccurredAtSeconds),
            snapshot.Cause
                ?? throw new JsonException(
                    "Timeline event cause is required."),
            snapshot.Summary
                ?? throw new JsonException(
                    "Timeline event summary is required."),
            snapshot.AffectedPlanetId.HasValue
                ? new PlanetId(
                    snapshot.AffectedPlanetId.Value)
                : null,
            snapshot.ElapsedSeconds,
            snapshot.Metrics);
    }

    private static DefinitionSnapshot ToDefinitionSnapshot(
        SimulationDefinition definition)
    {
        return new DefinitionSnapshot
        {
            PlanetaryEnergyBalanceModels =
                definition.PlanetaryEnergyBalanceModels
                    .Select(
                        model =>
                            new PlanetaryEnergyBalanceModelSnapshot
                            {
                                PlanetId = model.PlanetId.Value,
                                Parameters =
                                    new PlanetaryEnergyBalanceParametersSnapshot
                                    {
                                        StellarFluxWattsPerSquareMeter =
                                            model.Parameters
                                                .StellarFluxWattsPerSquareMeter,
                                        EffectiveLongwaveEmissivity =
                                            model.Parameters
                                                .EffectiveLongwaveEmissivity,
                                        EffectiveHeatCapacityJoulesPerSquareMeterKelvin =
                                            model.Parameters
                                                .EffectiveHeatCapacityJoulesPerSquareMeterKelvin,
                                        IceFreeAlbedo =
                                            model.Parameters.IceFreeAlbedo,
                                        IceAlbedo =
                                            model.Parameters.IceAlbedo,
                                        FullIceTemperatureKelvin =
                                            model.Parameters
                                                .FullIceTemperatureKelvin,
                                        IceFreeTemperatureKelvin =
                                            model.Parameters
                                                .IceFreeTemperatureKelvin,
                                        IceResponseTimescaleSeconds =
                                            model.Parameters
                                                .IceResponseTimescaleSeconds
                                    }
                            })
                    .ToArray(),
            PopulationModels =
                definition.PopulationModels
                    .Select(
                        model =>
                            new PopulationModelSnapshot
                            {
                                PlanetId = model.PlanetId.Value,
                                Parameters =
                                    new PopulationModelParametersSnapshot
                                    {
                                        Seed = model.Parameters.Seed,
                                        AnnualBirthRatePerEligibleFemale =
                                            model.Parameters
                                                .AnnualBirthRatePerEligibleFemale,
                                        AnnualAdultMigrationRate =
                                            model.Parameters
                                                .AnnualAdultMigrationRate,
                                        AnnualBaseMortalityRate =
                                            model.Parameters
                                                .AnnualBaseMortalityRate,
                                        AnnualElderMortalityRate =
                                            model.Parameters
                                                .AnnualElderMortalityRate,
                                        ReproductiveAgeMinimumYears =
                                            model.Parameters
                                                .ReproductiveAgeMinimumYears,
                                        ReproductiveAgeMaximumYears =
                                            model.Parameters
                                                .ReproductiveAgeMaximumYears,
                                        ElderAgeYears =
                                            model.Parameters.ElderAgeYears,
                                        LocalMigrationDegrees =
                                            model.Parameters
                                                .LocalMigrationDegrees,
                                        LongMigrationProbability =
                                            model.Parameters
                                                .LongMigrationProbability,
                                        LongMigrationDegrees =

                                            model.Parameters

                                                .LongMigrationDegrees,

                                        ConceptionProbabilityPerMatingOpportunity =

                                            model.Parameters

                                                .ConceptionProbabilityPerMatingOpportunity,

                                        GestationDays =

                                            model.Parameters.GestationDays
                                    },
                                VegetationForaging =
                                    model.VegetationForaging is null
                                        ? null
                                        : new VegetationForagingSnapshot
                                        {
                                            KilogramsLiveBiomassPerEnergyReserveUnit =
                                                model.VegetationForaging
                                                    .KilogramsLiveBiomassPerEnergyReserveUnit,
                                            MaximumHarvestKilogramsPerPersonPerDay =
                                                model.VegetationForaging
                                                    .MaximumHarvestKilogramsPerPersonPerDay
                                        }

                            })
                    .ToArray(),
            HydrologyModels =
                definition.HydrologyModels
                    .Select(
                        model =>
                            new HydrologyModelSnapshot
                            {
                                PlanetId = model.PlanetId.Value,
                                Parameters =
                                    new HydrologyModelParametersSnapshot
                                    {
                                        MaximumIntegrationStepSeconds =
                                            model.Parameters
                                                .MaximumIntegrationStepSeconds,
                                        MaximumEvaporationRateKilogramsPerSquareMeterPerDay =
                                            model.Parameters
                                                .MaximumEvaporationRateKilogramsPerSquareMeterPerDay,
                                        AtmosphericPrecipitationThresholdKilogramsPerSquareMeter =
                                            model.Parameters
                                                .AtmosphericPrecipitationThresholdKilogramsPerSquareMeter,
                                        MaximumPrecipitationRateKilogramsPerSquareMeterPerDay =
                                            model.Parameters
                                                .MaximumPrecipitationRateKilogramsPerSquareMeterPerDay,
                                        SoilWaterCapacityKilogramsPerSquareMeter =
                                            model.Parameters
                                                .SoilWaterCapacityKilogramsPerSquareMeter,
                                        MaximumInfiltrationRateKilogramsPerSquareMeterPerDay =
                                            model.Parameters
                                                .MaximumInfiltrationRateKilogramsPerSquareMeterPerDay,
                                        MaximumRunoffRateKilogramsPerSquareMeterPerDay =
                                            model.Parameters
                                                .MaximumRunoffRateKilogramsPerSquareMeterPerDay,
                                        FreezingTemperatureKelvin =
                                            model.Parameters
                                                .FreezingTemperatureKelvin,
                                        MeltingTemperatureKelvin =
                                            model.Parameters
                                                .MeltingTemperatureKelvin,
                                        MaximumFreezingRateKilogramsPerSquareMeterPerDay =
                                            model.Parameters
                                                .MaximumFreezingRateKilogramsPerSquareMeterPerDay,
                                        MaximumMeltingRateKilogramsPerSquareMeterPerDay =
                                            model.Parameters
                                                .MaximumMeltingRateKilogramsPerSquareMeterPerDay
                                    }
                            })
                    .ToArray(),
            VegetationModels =
                definition.VegetationModels
                    .Select(
                        model =>
                            new VegetationModelSnapshot
                            {
                                PlanetId =
                                    model.PlanetId.Value,
                                Parameters =
                                    new VegetationModelParametersSnapshot
                                    {
                                        MaximumIntegrationStepSeconds =
                                            model.Parameters
                                                .MaximumIntegrationStepSeconds,
                                        CarryingCapacityKilogramsPerSquareMeter =
                                            model.Parameters
                                                .CarryingCapacityKilogramsPerSquareMeter,
                                        MaximumRelativeGrowthRatePerDay =
                                            model.Parameters
                                                .MaximumRelativeGrowthRatePerDay,
                                        SoilWaterForFullProductivityKilogramsPerSquareMeter =
                                            model.Parameters
                                                .SoilWaterForFullProductivityKilogramsPerSquareMeter,
                                        MinimumGrowthTemperatureKelvin =
                                            model.Parameters
                                                .MinimumGrowthTemperatureKelvin,
                                        OptimumGrowthTemperatureKelvin =
                                            model.Parameters
                                                .OptimumGrowthTemperatureKelvin,
                                        MaximumGrowthTemperatureKelvin =
                                            model.Parameters
                                                .MaximumGrowthTemperatureKelvin,
                                        TemperatureLapseRateKelvinPerMeter =
                                            model.Parameters
                                                .TemperatureLapseRateKelvinPerMeter
                                    }
                            })
                    .ToArray(),
            InvertebrateModels =
                definition.InvertebrateModels
                    .Select(
                        model =>
                            new InvertebrateModelSnapshot
                            {
                                PlanetId =
                                    model.PlanetId.Value,
                                Parameters =
                                    new InvertebrateModelParametersSnapshot
                                    {
                                        MaximumIntegrationStepSeconds =
                                            model.Parameters
                                                .MaximumIntegrationStepSeconds,
                                        CarryingCapacityKilogramsPerKilogramLiveVegetation =
                                            model.Parameters
                                                .CarryingCapacityKilogramsPerKilogramLiveVegetation,
                                        InitialFractionOfLocalCarryingCapacity =
                                            model.Parameters
                                                .InitialFractionOfLocalCarryingCapacity,
                                        MaximumRelativeGrowthRatePerDay =
                                            model.Parameters
                                                .MaximumRelativeGrowthRatePerDay,
                                        BaselineMortalityRatePerDay =
                                            model.Parameters
                                                .BaselineMortalityRatePerDay
                                    }
                            })
                    .ToArray(),
            BirdModels =
                definition.BirdModels
                    .Select(
                        model =>
                            new BirdModelSnapshot
                            {
                                PlanetId =
                                    model.PlanetId.Value,
                                Parameters =
                                    new BirdModelParametersSnapshot
                                    {
                                        CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass =
                                            model.Parameters
                                                .CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass,
                                        InitialFractionOfLocalCarryingCapacity =
                                            model.Parameters
                                                .InitialFractionOfLocalCarryingCapacity,
                                        MinimumInitialFlockMemberCount =
                                            model.Parameters
                                                .MinimumInitialFlockMemberCount,
                                        MaximumInitialFlockCount =
                                            model.Parameters
                                                .MaximumInitialFlockCount,
                                        MaximumIntegrationStepSeconds =
                                            model.Parameters
                                                .MaximumIntegrationStepSeconds,
                                        MaximumTravelMetersPerDay =
                                            model.Parameters
                                                .MaximumTravelMetersPerDay,
                                        FoodShortageMortalityRatePerDay =
                                            model.Parameters
                                                .FoodShortageMortalityRatePerDay,
                                        WaterAbsenceMortalityRatePerDay =
                                            model.Parameters
                                                .WaterAbsenceMortalityRatePerDay,
                                        HabitatAbsenceMortalityRatePerDay =
                                            model.Parameters
                                                .HabitatAbsenceMortalityRatePerDay
                                    }
                            })
                    .ToArray()
        };
    }

    private static SimulationDefinition FromDefinitionSnapshot(
        DefinitionSnapshot snapshot,
        int schemaVersion)
    {
        if (snapshot.PlanetaryEnergyBalanceModels is null)
        {
            throw new JsonException(
                "Planetary energy-balance model collection is required.");
        }

        var energyModels =
            snapshot.PlanetaryEnergyBalanceModels
                .Select(
                    model =>
                    {
                        if (model.Parameters is null)
                        {
                            throw new JsonException(
                                "Planetary energy-balance model parameters are required.");
                        }

                        return new PlanetaryEnergyBalanceModelDefinition(
                            new PlanetId(model.PlanetId),
                            new PlanetaryEnergyBalanceParameters(
                                model.Parameters
                                    .StellarFluxWattsPerSquareMeter,
                                model.Parameters
                                    .EffectiveLongwaveEmissivity,
                                model.Parameters
                                    .EffectiveHeatCapacityJoulesPerSquareMeterKelvin,
                                model.Parameters.IceFreeAlbedo,
                                model.Parameters.IceAlbedo,
                                model.Parameters
                                    .FullIceTemperatureKelvin,
                                model.Parameters
                                    .IceFreeTemperatureKelvin,
                                model.Parameters
                                    .IceResponseTimescaleSeconds));
                    })
                .ToArray();

        PopulationModelDefinition[] populationModels;

        if (schemaVersion == DefinitionSchemaVersion)
        {
            populationModels = [];
        }
        else
        {
            if (snapshot.PopulationModels is null)
            {
                throw new JsonException(
                    "Population model collection is required.");
            }

            populationModels =
                snapshot.PopulationModels
                    .Select(
                        model =>
                        {
                            if (model.Parameters is null)
                            {
                                throw new JsonException(
                                    "Population model parameters are required.");
                            }

                            return new PopulationModelDefinition(
                                new PlanetId(model.PlanetId),
                                new PopulationModelParameters(
                                    model.Parameters.Seed,
                                    model.Parameters
                                        .AnnualBirthRatePerEligibleFemale,
                                    model.Parameters
                                        .AnnualAdultMigrationRate,
                                    model.Parameters
                                        .AnnualBaseMortalityRate,
                                    model.Parameters
                                        .AnnualElderMortalityRate,
                                    model.Parameters
                                        .ReproductiveAgeMinimumYears,
                                    model.Parameters
                                        .ReproductiveAgeMaximumYears,
                                    model.Parameters.ElderAgeYears,
                                    model.Parameters
                                        .LocalMigrationDegrees,
                                    model.Parameters
                                        .LongMigrationProbability,
                                    model.Parameters
                                        .LongMigrationDegrees,
                                    conceptionProbabilityPerMatingOpportunity:
                                        schemaVersion >=
                                            ReproductiveBehaviorSchemaVersion
                                            ? model.Parameters
                                                .ConceptionProbabilityPerMatingOpportunity
                                                ?? throw new JsonException(
                                                    "Conception probability is required.")
                                            : 0.20,
                                    gestationDays:
                                        schemaVersion >=
                                            ReproductiveBehaviorSchemaVersion
                                            ? model.Parameters
                                                .GestationDays
                                                ?? throw new JsonException(
                                                    "Gestation duration is required.")
                                            : 280),
                                    vegetationForaging:
                                        schemaVersion >=
                                            VegetationForagingSchemaVersion &&
                                        model.VegetationForaging is not null
                                            ? new VegetationForagingParameters(
                                                model.VegetationForaging
                                                    .KilogramsLiveBiomassPerEnergyReserveUnit,
                                                model.VegetationForaging
                                                    .MaximumHarvestKilogramsPerPersonPerDay)
                                            : null);
                        })
                    .ToArray();
        }

        HydrologyModelDefinition[] hydrologyModels;

        if (schemaVersion <
            HydrologyModelSchemaVersion)
        {
            hydrologyModels = [];
        }
        else
        {
            if (snapshot.HydrologyModels is null)
            {
                throw new JsonException(
                    "Hydrology model collection is required.");
            }

            hydrologyModels =
                snapshot.HydrologyModels
                    .Select(
                        model =>
                        {
                            if (model.Parameters is null)
                            {
                                throw new JsonException(
                                    "Hydrology model parameters are required.");
                            }

                            return new HydrologyModelDefinition(
                                new PlanetId(model.PlanetId),
                                new HydrologyModelParameters(
                                    model.Parameters
                                        .MaximumIntegrationStepSeconds,
                                    model.Parameters
                                        .MaximumEvaporationRateKilogramsPerSquareMeterPerDay,
                                    model.Parameters
                                        .AtmosphericPrecipitationThresholdKilogramsPerSquareMeter,
                                    model.Parameters
                                        .MaximumPrecipitationRateKilogramsPerSquareMeterPerDay,
                                    model.Parameters
                                        .SoilWaterCapacityKilogramsPerSquareMeter,
                                    model.Parameters
                                        .MaximumInfiltrationRateKilogramsPerSquareMeterPerDay,
                                    model.Parameters
                                        .MaximumRunoffRateKilogramsPerSquareMeterPerDay,
                                    model.Parameters
                                        .FreezingTemperatureKelvin,
                                    model.Parameters
                                        .MeltingTemperatureKelvin,
                                    model.Parameters
                                        .MaximumFreezingRateKilogramsPerSquareMeterPerDay,
                                    model.Parameters
                                        .MaximumMeltingRateKilogramsPerSquareMeterPerDay));
                        })
                    .ToArray();
        }

        VegetationModelDefinition[] vegetationModels;

        if (schemaVersion <
            VegetationModelSchemaVersion)
        {
            vegetationModels = [];
        }
        else
        {
            if (snapshot.VegetationModels is null)
            {
                throw new JsonException(
                    "Vegetation model collection is required.");
            }

            vegetationModels =
                snapshot.VegetationModels
                    .Select(
                        model =>
                        {
                            if (model.Parameters is null)
                            {
                                throw new JsonException(
                                    "Vegetation model parameters are required.");
                            }

                            return new VegetationModelDefinition(
                                new PlanetId(
                                    model.PlanetId),
                                new VegetationModelParameters(
                                    model.Parameters
                                        .MaximumIntegrationStepSeconds,
                                    model.Parameters
                                        .CarryingCapacityKilogramsPerSquareMeter,
                                    model.Parameters
                                        .MaximumRelativeGrowthRatePerDay,
                                    model.Parameters
                                        .SoilWaterForFullProductivityKilogramsPerSquareMeter,
                                    model.Parameters
                                        .MinimumGrowthTemperatureKelvin,
                                    model.Parameters
                                        .OptimumGrowthTemperatureKelvin,
                                    model.Parameters
                                        .MaximumGrowthTemperatureKelvin,
                                    model.Parameters
                                        .TemperatureLapseRateKelvinPerMeter));
                        })
                    .ToArray();
        }

        InvertebrateModelDefinition[] invertebrateModels;

        if (schemaVersion <
            InvertebrateModelSchemaVersion)
        {
            invertebrateModels = [];
        }
        else
        {
            if (snapshot.InvertebrateModels is null)
            {
                throw new JsonException(
                    "Invertebrate model collection is required.");
            }

            invertebrateModels =
                snapshot.InvertebrateModels
                    .Select(
                        model =>
                        {
                            if (model.Parameters is null)
                            {
                                throw new JsonException(
                                    "Invertebrate model parameters are required.");
                            }

                            return new InvertebrateModelDefinition(
                                new PlanetId(
                                    model.PlanetId),
                                new InvertebrateModelParameters(
                                    model.Parameters
                                        .MaximumIntegrationStepSeconds,
                                    model.Parameters
                                        .CarryingCapacityKilogramsPerKilogramLiveVegetation,
                                    model.Parameters
                                        .InitialFractionOfLocalCarryingCapacity,
                                    model.Parameters
                                        .MaximumRelativeGrowthRatePerDay,
                                    model.Parameters
                                        .BaselineMortalityRatePerDay));
                        })
                    .ToArray();
        }

        BirdModelDefinition[] birdModels;

        if (schemaVersion <
            BirdModelSchemaVersion)
        {
            birdModels = [];
        }
        else
        {
            if (snapshot.BirdModels is null)
            {
                throw new JsonException(
                    "Bird model collection is required.");
            }

            birdModels =
                snapshot.BirdModels
                    .Select(
                        model =>
                        {
                            if (model.Parameters is null)
                            {
                                throw new JsonException(
                                    "Bird model parameters are required.");
                            }

                            var behaviorDefaults =
                                new BirdModelParameters();

                            return new BirdModelDefinition(
                                new PlanetId(
                                    model.PlanetId),
                                new BirdModelParameters(
                                    model.Parameters
                                        .CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass,
                                    model.Parameters
                                        .InitialFractionOfLocalCarryingCapacity,
                                    model.Parameters
                                        .MinimumInitialFlockMemberCount,
                                    model.Parameters
                                        .MaximumInitialFlockCount,
                                    schemaVersion <
                                        BirdBehaviorSchemaVersion
                                        ? behaviorDefaults
                                            .MaximumIntegrationStepSeconds
                                        : model.Parameters
                                            .MaximumIntegrationStepSeconds
                                            ?? throw new JsonException(
                                                "Bird maximum integration step is required."),
                                    schemaVersion <
                                        BirdBehaviorSchemaVersion
                                        ? behaviorDefaults
                                            .MaximumTravelMetersPerDay
                                        : model.Parameters
                                            .MaximumTravelMetersPerDay
                                            ?? throw new JsonException(
                                                "Bird maximum travel distance is required."),
                                    schemaVersion <
                                        BirdBehaviorSchemaVersion
                                        ? behaviorDefaults
                                            .FoodShortageMortalityRatePerDay
                                        : model.Parameters
                                            .FoodShortageMortalityRatePerDay
                                            ?? throw new JsonException(
                                                "Bird food-shortage mortality rate is required."),
                                    schemaVersion <
                                        BirdBehaviorSchemaVersion
                                        ? behaviorDefaults
                                            .WaterAbsenceMortalityRatePerDay
                                        : model.Parameters
                                            .WaterAbsenceMortalityRatePerDay
                                            ?? throw new JsonException(
                                                "Bird water-absence mortality rate is required."),
                                    schemaVersion <
                                        BirdBehaviorSchemaVersion
                                        ? behaviorDefaults
                                            .HabitatAbsenceMortalityRatePerDay
                                        : model.Parameters
                                            .HabitatAbsenceMortalityRatePerDay
                                            ?? throw new JsonException(
                                                "Bird habitat-absence mortality rate is required.")));
                        })
                    .ToArray();
        }

        return new SimulationDefinition(
            energyModels,
            populationModels,
            hydrologyModels,
            vegetationModels,
            invertebrateModels,
            birdModels);
    }

    private static JsonElement ToWorldElement(
        Est.Simulation.Worlds.WorldState world)
    {
        using var document =
            JsonDocument.Parse(
                WorldSnapshotSerializer.Serialize(world));

        return document.RootElement.Clone();
    }

    private static Est.Simulation.Worlds.WorldState
        FromWorldElement(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException(
                "Archive world snapshot is required.");
        }

        return WorldSnapshotSerializer.Deserialize(
            element.GetRawText());
    }

    private sealed class TimelineArchiveSnapshot
    {
        public required int SchemaVersion { get; set; }

        public required ProvenanceSnapshot Provenance { get; set; }

        public DefinitionSnapshot? Definition { get; set; }

        public required Guid TimelineId { get; set; }

        public Guid? ParentTimelineId { get; set; }

        public Guid? ParentCheckpointId { get; set; }

        public required JsonElement CurrentWorld { get; set; }

        public required CheckpointSnapshot[] Checkpoints { get; set; }

        public required TimelineEventSnapshot[] Events { get; set; }
    }

    private sealed class ProvenanceSnapshot
    {
        public required string Producer { get; set; }

        public required string ProducerVersion { get; set; }

        public required string Origin { get; set; }
    }

    private sealed class DefinitionSnapshot
    {
        public required PlanetaryEnergyBalanceModelSnapshot[]
            PlanetaryEnergyBalanceModels { get; set; }

        public PopulationModelSnapshot[]? PopulationModels { get; set; }

        public HydrologyModelSnapshot[]? HydrologyModels { get; set; }

        public VegetationModelSnapshot[]? VegetationModels { get; set; }

        public InvertebrateModelSnapshot[]? InvertebrateModels { get; set; }

        public BirdModelSnapshot[]? BirdModels { get; set; }
    }

    private sealed class BirdModelSnapshot
    {
        public required Guid PlanetId { get; set; }

        public required BirdModelParametersSnapshot Parameters
        {
            get;
            set;
        }
    }

    private sealed class BirdModelParametersSnapshot
    {
        public required double
            CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass
        {
            get;
            set;
        }

        public required double
            InitialFractionOfLocalCarryingCapacity
        {
            get;
            set;
        }

        public required int MinimumInitialFlockMemberCount
        {
            get;
            set;
        }

        public required int MaximumInitialFlockCount
        {
            get;
            set;
        }

        public long? MaximumIntegrationStepSeconds
        {
            get;
            set;
        }

        public double? MaximumTravelMetersPerDay
        {
            get;
            set;
        }

        public double? FoodShortageMortalityRatePerDay
        {
            get;
            set;
        }

        public double? WaterAbsenceMortalityRatePerDay
        {
            get;
            set;
        }

        public double? HabitatAbsenceMortalityRatePerDay
        {
            get;
            set;
        }
    }

    private sealed class InvertebrateModelSnapshot
    {
        public required Guid PlanetId { get; set; }

        public required InvertebrateModelParametersSnapshot
            Parameters
        {
            get;
            set;
        }
    }

    private sealed class InvertebrateModelParametersSnapshot
    {
        public required long MaximumIntegrationStepSeconds
        {
            get;
            set;
        }

        public required double
            CarryingCapacityKilogramsPerKilogramLiveVegetation
        {
            get;
            set;
        }

        public required double
            InitialFractionOfLocalCarryingCapacity
        {
            get;
            set;
        }

        public required double MaximumRelativeGrowthRatePerDay
        {
            get;
            set;
        }

        public required double BaselineMortalityRatePerDay
        {
            get;
            set;
        }
    }

    private sealed class VegetationModelSnapshot
    {
        public required Guid PlanetId { get; set; }

        public required VegetationModelParametersSnapshot
            Parameters
        {
            get;
            set;
        }
    }

    private sealed class VegetationModelParametersSnapshot
    {
        public required long MaximumIntegrationStepSeconds
        {
            get;
            set;
        }

        public required double
            CarryingCapacityKilogramsPerSquareMeter
        {
            get;
            set;
        }

        public required double MaximumRelativeGrowthRatePerDay
        {
            get;
            set;
        }

        public required double
            SoilWaterForFullProductivityKilogramsPerSquareMeter
        {
            get;
            set;
        }

        public required double MinimumGrowthTemperatureKelvin
        {
            get;
            set;
        }

        public required double OptimumGrowthTemperatureKelvin
        {
            get;
            set;
        }

        public required double MaximumGrowthTemperatureKelvin
        {
            get;
            set;
        }

        public required double TemperatureLapseRateKelvinPerMeter
        {
            get;
            set;
        }
    }

    private sealed class HydrologyModelSnapshot
    {
        public required Guid PlanetId { get; set; }

        public required HydrologyModelParametersSnapshot
            Parameters
        {
            get;
            set;
        }
    }

    private sealed class HydrologyModelParametersSnapshot
    {
        public required long MaximumIntegrationStepSeconds { get; set; }

        public required double
            MaximumEvaporationRateKilogramsPerSquareMeterPerDay
        {
            get;
            set;
        }

        public required double
            AtmosphericPrecipitationThresholdKilogramsPerSquareMeter
        {
            get;
            set;
        }

        public required double
            MaximumPrecipitationRateKilogramsPerSquareMeterPerDay
        {
            get;
            set;
        }

        public required double
            SoilWaterCapacityKilogramsPerSquareMeter
        {
            get;
            set;
        }

        public required double
            MaximumInfiltrationRateKilogramsPerSquareMeterPerDay
        {
            get;
            set;
        }

        public required double
            MaximumRunoffRateKilogramsPerSquareMeterPerDay
        {
            get;
            set;
        }

        public required double FreezingTemperatureKelvin { get; set; }

        public required double MeltingTemperatureKelvin { get; set; }

        public required double
            MaximumFreezingRateKilogramsPerSquareMeterPerDay
        {
            get;
            set;
        }

        public required double
            MaximumMeltingRateKilogramsPerSquareMeterPerDay
        {
            get;
            set;
        }
    }

    private sealed class PopulationModelSnapshot
    {
        public required Guid PlanetId { get; set; }

        public required PopulationModelParametersSnapshot
            Parameters { get; set; }

        public VegetationForagingSnapshot? VegetationForaging
        {
            get;
            set;
        }
    }

    private sealed class VegetationForagingSnapshot
    {
        public required double
            KilogramsLiveBiomassPerEnergyReserveUnit
        {
            get;
            set;
        }

        public required double
            MaximumHarvestKilogramsPerPersonPerDay
        {
            get;
            set;
        }
    }

    private sealed class PopulationModelParametersSnapshot
    {
        public required int Seed { get; set; }

        public required double
            AnnualBirthRatePerEligibleFemale { get; set; }

        public required double AnnualAdultMigrationRate { get; set; }

        public required double AnnualBaseMortalityRate { get; set; }

        public required double AnnualElderMortalityRate { get; set; }

        public required double ReproductiveAgeMinimumYears { get; set; }

        public required double ReproductiveAgeMaximumYears { get; set; }

        public required double ElderAgeYears { get; set; }

        public required double LocalMigrationDegrees { get; set; }

        public required double LongMigrationProbability { get; set; }

        public required double LongMigrationDegrees { get; set; }

        public double? ConceptionProbabilityPerMatingOpportunity
        {
            get;
            set;
        }

        public double? GestationDays { get; set; }
    }

    private sealed class PlanetaryEnergyBalanceModelSnapshot
    {
        public required Guid PlanetId { get; set; }

        public required PlanetaryEnergyBalanceParametersSnapshot
            Parameters { get; set; }
    }

    private sealed class PlanetaryEnergyBalanceParametersSnapshot
    {
        public required double StellarFluxWattsPerSquareMeter { get; set; }

        public required double EffectiveLongwaveEmissivity { get; set; }

        public required double
            EffectiveHeatCapacityJoulesPerSquareMeterKelvin { get; set; }

        public required double IceFreeAlbedo { get; set; }

        public required double IceAlbedo { get; set; }

        public required double FullIceTemperatureKelvin { get; set; }

        public required double IceFreeTemperatureKelvin { get; set; }

        public required double IceResponseTimescaleSeconds { get; set; }
    }

    private sealed class CheckpointSnapshot
    {
        public required Guid CheckpointId { get; set; }

        public required JsonElement World { get; set; }
    }

    private sealed class TimelineEventSnapshot
    {
        public required Guid EventId { get; set; }

        public required long OccurredAtSeconds { get; set; }

        public required string Cause { get; set; }

        public required string Summary { get; set; }

        public Guid? AffectedPlanetId { get; set; }

        public required long ElapsedSeconds { get; set; }

        public required Dictionary<string, double> Metrics { get; set; }
    }
}
