using System.Text.Json;
using Est.Simulation.Animals;
using Est.Simulation.Birds;
using Est.Simulation.Biogeochemistry;
using Est.Simulation.Grazers;
using Est.Simulation.Hydrology;
using Est.Simulation.Invertebrates;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Persistence.Snapshots;

public static class WorldSnapshotSerializer
{
    public const int CurrentSchemaVersion = 15;
    private const int LegacySchemaVersion = 1;
    private const int PopulationSchemaVersion = 2;
    private const int SurvivalSchemaVersion = 3;
    private const int FoodSchemaVersion = 4;
    private const int RenewableFoodSchemaVersion = 5;
    private const int AnimalSchemaVersion = 6;
    private const int PregnancySchemaVersion = 7;
    private const int TerrainSchemaVersion = 8;
    private const int HydrologySchemaVersion = 9;
    private const int VegetationSchemaVersion = 10;
    private const int FoodRetirementSchemaVersion = 11;
    private const int InvertebrateSchemaVersion = 12;
    private const int BirdFlockSchemaVersion = 13;
    private const int GrazerCohortSchemaVersion = 14;
    private const int BiogeochemistrySchemaVersion = 15;

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
                .ToArray(),
            Animals = world.Animals
                .Select(ToSnapshot)
                .ToArray(),
            Terrain = world.Terrain
                .Select(ToSnapshot)
                .ToArray(),
            Hydrology = world.Hydrology
                .Select(ToSnapshot)
                .ToArray(),
            Vegetation = world.Vegetation
                .Select(ToSnapshot)
                .ToArray(),
            Invertebrates = world.Invertebrates
                .Select(ToSnapshot)
                .ToArray(),
            BirdFlocks = world.BirdFlocks
                .Select(ToSnapshot)
                .ToArray(),
            GrazerCohorts = world.GrazerCohorts
                .Select(ToSnapshot)
                .ToArray(),
            Biogeochemistry = world.Biogeochemistry
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
            snapshot.SchemaVersion != SurvivalSchemaVersion &&
            snapshot.SchemaVersion != FoodSchemaVersion &&
            snapshot.SchemaVersion != RenewableFoodSchemaVersion &&
            snapshot.SchemaVersion != AnimalSchemaVersion &&
            snapshot.SchemaVersion != PregnancySchemaVersion &&
            snapshot.SchemaVersion != TerrainSchemaVersion &&
            snapshot.SchemaVersion != HydrologySchemaVersion &&
            snapshot.SchemaVersion != VegetationSchemaVersion &&
            snapshot.SchemaVersion != FoodRetirementSchemaVersion &&
            snapshot.SchemaVersion != InvertebrateSchemaVersion &&
            snapshot.SchemaVersion != BirdFlockSchemaVersion &&
            snapshot.SchemaVersion != GrazerCohortSchemaVersion &&
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

        if (snapshot.SchemaVersion >=
                FoodSchemaVersion &&
            snapshot.SchemaVersion <
                FoodRetirementSchemaVersion)
        {
            if (snapshot.FoodResources is null)
            {
                throw new JsonException(
                    "Snapshot food resources collection is required.");
            }

            var legacyPlanetIds =
                planets
                    .Select(
                        planet =>
                            planet.Id.Value)
                    .ToHashSet();

            var legacyFoodResourceIds =
                new HashSet<Guid>();

            foreach (var resource in
                     snapshot.FoodResources)
            {
                if (resource is null)
                {
                    throw new JsonException(
                        "Legacy food resources cannot contain null entries.");
                }

                if (!legacyFoodResourceIds.Add(
                        resource.FoodResourceId))
                {
                    throw new JsonException(
                        "Legacy food resources cannot contain duplicate identities.");
                }

                ValidateLegacyFoodResource(
                    resource,
                    snapshot.SchemaVersion,
                    legacyPlanetIds);
            }
        }
        else if (snapshot.SchemaVersion >=
                     FoodRetirementSchemaVersion &&
                 snapshot.FoodResources is not null)
        {
            throw new JsonException(
                "Current world snapshots cannot contain legacy food resources.");
        }

        AnimalState[] animals;

        if (snapshot.SchemaVersion <
            AnimalSchemaVersion)
        {
            animals = [];
        }
        else
        {
            if (snapshot.Animals is null)
            {
                throw new JsonException(
                    "Snapshot animals collection is required.");
            }

            animals = snapshot.Animals
                .Select(FromSnapshot)
                .ToArray();
        }

        PlanetTerrainState[] terrain;

        if (snapshot.SchemaVersion <
            TerrainSchemaVersion)
        {
            terrain = [];
        }
        else
        {
            if (snapshot.Terrain is null)
            {
                throw new JsonException(
                    "Snapshot terrain collection is required.");
            }

            terrain = snapshot.Terrain
                .Select(FromSnapshot)
                .ToArray();
        }

        PlanetHydrologyState[] hydrology;

        if (snapshot.SchemaVersion <
            HydrologySchemaVersion)
        {
            hydrology = [];
        }
        else
        {
            if (snapshot.Hydrology is null)
            {
                throw new JsonException(
                    "Snapshot hydrology collection is required.");
            }

            hydrology = snapshot.Hydrology
                .Select(FromSnapshot)
                .ToArray();
        }

        PlanetVegetationState[] vegetation;

        if (snapshot.SchemaVersion <
            VegetationSchemaVersion)
        {
            vegetation = [];
        }
        else
        {
            if (snapshot.Vegetation is null)
            {
                throw new JsonException(
                    "Snapshot vegetation collection is required.");
            }

            vegetation = snapshot.Vegetation
                .Select(FromSnapshot)
                .ToArray();
        }

        PlanetInvertebrateState[] invertebrates;

        if (snapshot.SchemaVersion <
            InvertebrateSchemaVersion)
        {
            invertebrates = [];
        }
        else
        {
            if (snapshot.Invertebrates is null)
            {
                throw new JsonException(
                    "Snapshot invertebrate collection is required.");
            }

            invertebrates = snapshot.Invertebrates
                .Select(FromSnapshot)
                .ToArray();
        }

        BirdFlockState[] birdFlocks;

        if (snapshot.SchemaVersion <
            BirdFlockSchemaVersion)
        {
            birdFlocks = [];
        }
        else
        {
            if (snapshot.BirdFlocks is null)
            {
                throw new JsonException(
                    "Snapshot bird flock collection is required.");
            }

            birdFlocks = snapshot.BirdFlocks
                .Select(FromSnapshot)
                .ToArray();
        }

        GrazerCohortState[] grazerCohorts;

        if (snapshot.SchemaVersion <
            GrazerCohortSchemaVersion)
        {
            grazerCohorts = [];
        }
        else
        {
            if (snapshot.GrazerCohorts is null)
            {
                throw new JsonException(
                    "Snapshot grazer cohort collection is required.");
            }

            grazerCohorts = snapshot.GrazerCohorts
                .Select(FromSnapshot)
                .ToArray();
        }

        PlanetBiogeochemistryState[] biogeochemistry;

        if (snapshot.SchemaVersion <
            BiogeochemistrySchemaVersion)
        {
            biogeochemistry = [];
        }
        else
        {
            if (snapshot.Biogeochemistry is null)
            {
                throw new JsonException(
                    "Snapshot biogeochemistry collection is required.");
            }

            biogeochemistry = snapshot.Biogeochemistry
                .Select(FromSnapshot)
                .ToArray();
        }

        return new WorldState(
            new WorldId(snapshot.WorldId),
            new SimulationTime(snapshot.CurrentTimeSeconds),
            planets,
            population,
            animals,
            terrain,
            hydrology,
            vegetation,
            invertebrates,
            birdFlocks,
            grazerCohorts,
            biogeochemistry);
    }

    private static BirdFlockSnapshot ToSnapshot(
        BirdFlockState flock)
    {
        return new BirdFlockSnapshot
        {
            BirdFlockId =
                flock.Id.Value,
            PlanetId =
                flock.PlanetId.Value,
            MemberCount =
                flock.MemberCount,
            LatitudeDegrees =
                flock.LatitudeDegrees,
            LongitudeDegrees =
                flock.LongitudeDegrees
        };
    }

    private static BirdFlockState FromSnapshot(
        BirdFlockSnapshot snapshot)
    {
        return new BirdFlockState(
            new BirdFlockId(
                snapshot.BirdFlockId),
            new PlanetId(
                snapshot.PlanetId),
            snapshot.MemberCount,
            snapshot.LatitudeDegrees,
            snapshot.LongitudeDegrees);
    }

    private static GrazerCohortSnapshot ToSnapshot(
        GrazerCohortState cohort)
    {
        return new GrazerCohortSnapshot
        {
            GrazerCohortId =
                cohort.Id.Value,
            PlanetId =
                cohort.PlanetId.Value,
            MemberCount =
                cohort.MemberCount,
            LatitudeDegrees =
                cohort.LatitudeDegrees,
            LongitudeDegrees =
                cohort.LongitudeDegrees
        };
    }

    private static GrazerCohortState FromSnapshot(
        GrazerCohortSnapshot snapshot)
    {
        return new GrazerCohortState(
            new GrazerCohortId(
                snapshot.GrazerCohortId),
            new PlanetId(
                snapshot.PlanetId),
            snapshot.MemberCount,
            snapshot.LatitudeDegrees,
            snapshot.LongitudeDegrees);
    }

    private static PlanetBiogeochemistrySnapshot ToSnapshot(
        PlanetBiogeochemistryState biogeochemistry)
    {
        return new PlanetBiogeochemistrySnapshot
        {
            PlanetId =
                biogeochemistry.PlanetId.Value,
            GridDefinition =
                new SurfaceGridDefinitionSnapshot
                {
                    Kind =
                        biogeochemistry.GridDefinition.Kind,
                    IdentityVersion =
                        biogeochemistry.GridDefinition.IdentityVersion,
                    LatitudeBandCount =
                        biogeochemistry.GridDefinition.LatitudeBandCount,
                    LongitudeBandCount =
                        biogeochemistry.GridDefinition.LongitudeBandCount
                },
            Cells = biogeochemistry.Cells
                .Select(
                    cell =>
                        new BiogeochemistryCellSnapshot
                        {
                            SurfaceCellId =
                                cell.CellId.Value,
                            DetritalBiomassKilogramsPerSquareMeter =
                                cell.DetritalBiomassKilogramsPerSquareMeter,
                            DetritalNitrogenKilogramsPerSquareMeter =
                                cell.DetritalNitrogenKilogramsPerSquareMeter,
                            PlantAvailableNitrogenKilogramsPerSquareMeter =
                                cell.PlantAvailableNitrogenKilogramsPerSquareMeter
                        })
                .ToArray()
        };
    }

    private static PlanetBiogeochemistryState FromSnapshot(
        PlanetBiogeochemistrySnapshot snapshot)
    {
        if (snapshot.GridDefinition is null)
        {
            throw new JsonException(
                "Biogeochemistry surface-grid definition is required.");
        }

        if (snapshot.Cells is null)
        {
            throw new JsonException(
                "Biogeochemistry cells collection is required.");
        }

        var gridDefinition =
            new SurfaceGridDefinition(
                snapshot.GridDefinition.Kind,
                snapshot.GridDefinition.IdentityVersion,
                snapshot.GridDefinition.LatitudeBandCount,
                snapshot.GridDefinition.LongitudeBandCount);

        return new PlanetBiogeochemistryState(
            new PlanetId(
                snapshot.PlanetId),
            gridDefinition,
            snapshot.Cells.Select(
                cell =>
                    new BiogeochemistryCellState(
                        new SurfaceCellId(
                            cell.SurfaceCellId),
                        cell.DetritalBiomassKilogramsPerSquareMeter,
                        cell.DetritalNitrogenKilogramsPerSquareMeter,
                        cell.PlantAvailableNitrogenKilogramsPerSquareMeter)));
    }

    private static PlanetInvertebrateSnapshot ToSnapshot(
        PlanetInvertebrateState invertebrates)
    {
        return new PlanetInvertebrateSnapshot
        {
            PlanetId = invertebrates.PlanetId.Value,
            GridDefinition =
                new SurfaceGridDefinitionSnapshot
                {
                    Kind =
                        invertebrates.GridDefinition.Kind,
                    IdentityVersion =
                        invertebrates.GridDefinition.IdentityVersion,
                    LatitudeBandCount =
                        invertebrates.GridDefinition.LatitudeBandCount,
                    LongitudeBandCount =
                        invertebrates.GridDefinition.LongitudeBandCount
                },
            Cells = invertebrates.Cells
                .Select(
                    cell =>
                        new InvertebrateCellSnapshot
                        {
                            SurfaceCellId =
                                cell.CellId.Value,
                            LiveBiomassKilogramsPerSquareMeter =
                                cell.LiveBiomassKilogramsPerSquareMeter
                        })
                .ToArray()
        };
    }

    private static PlanetInvertebrateState FromSnapshot(
        PlanetInvertebrateSnapshot snapshot)
    {
        if (snapshot.GridDefinition is null)
        {
            throw new JsonException(
                "Invertebrate surface-grid definition is required.");
        }

        if (snapshot.Cells is null)
        {
            throw new JsonException(
                "Invertebrate cells collection is required.");
        }

        var gridDefinition =
            new SurfaceGridDefinition(
                snapshot.GridDefinition.Kind,
                snapshot.GridDefinition.IdentityVersion,
                snapshot.GridDefinition.LatitudeBandCount,
                snapshot.GridDefinition.LongitudeBandCount);

        return new PlanetInvertebrateState(
            new PlanetId(snapshot.PlanetId),
            gridDefinition,
            snapshot.Cells.Select(
                cell =>
                    new InvertebrateCellState(
                        new SurfaceCellId(
                            cell.SurfaceCellId),
                        cell.LiveBiomassKilogramsPerSquareMeter)));
    }

    private static PlanetVegetationSnapshot ToSnapshot(
        PlanetVegetationState vegetation)
    {
        return new PlanetVegetationSnapshot
        {
            PlanetId = vegetation.PlanetId.Value,
            GridDefinition =
                new SurfaceGridDefinitionSnapshot
                {
                    Kind =
                        vegetation.GridDefinition.Kind,
                    IdentityVersion =
                        vegetation.GridDefinition.IdentityVersion,
                    LatitudeBandCount =
                        vegetation.GridDefinition.LatitudeBandCount,
                    LongitudeBandCount =
                        vegetation.GridDefinition.LongitudeBandCount
                },
            Cells = vegetation.Cells
                .Select(
                    cell =>
                        new VegetationCellSnapshot
                        {
                            SurfaceCellId =
                                cell.CellId.Value,
                            LiveBiomassKilogramsPerSquareMeter =
                                cell.LiveBiomassKilogramsPerSquareMeter
                        })
                .ToArray()
        };
    }

    private static PlanetVegetationState FromSnapshot(
        PlanetVegetationSnapshot snapshot)
    {
        if (snapshot.GridDefinition is null)
        {
            throw new JsonException(
                "Vegetation surface-grid definition is required.");
        }

        if (snapshot.Cells is null)
        {
            throw new JsonException(
                "Vegetation cells collection is required.");
        }

        var gridDefinition =
            new SurfaceGridDefinition(
                snapshot.GridDefinition.Kind,
                snapshot.GridDefinition.IdentityVersion,
                snapshot.GridDefinition.LatitudeBandCount,
                snapshot.GridDefinition.LongitudeBandCount);

        return new PlanetVegetationState(
            new PlanetId(snapshot.PlanetId),
            gridDefinition,
            snapshot.Cells.Select(
                cell =>
                    new VegetationCellState(
                        new SurfaceCellId(
                            cell.SurfaceCellId),
                        cell.LiveBiomassKilogramsPerSquareMeter)));
    }

    private static PlanetHydrologySnapshot ToSnapshot(
        PlanetHydrologyState hydrology)
    {
        return new PlanetHydrologySnapshot
        {
            PlanetId = hydrology.PlanetId.Value,
            GridDefinition =
                new SurfaceGridDefinitionSnapshot
                {
                    Kind =
                        hydrology.GridDefinition.Kind,
                    IdentityVersion =
                        hydrology.GridDefinition.IdentityVersion,
                    LatitudeBandCount =
                        hydrology.GridDefinition.LatitudeBandCount,
                    LongitudeBandCount =
                        hydrology.GridDefinition.LongitudeBandCount
                },
            Cells = hydrology.Cells
                .Select(
                    cell =>
                        new HydrologyCellSnapshot
                        {
                            SurfaceCellId =
                                cell.CellId.Value,
                            AtmosphericWaterKilogramsPerSquareMeter =
                                cell.AtmosphericWaterKilogramsPerSquareMeter,
                            SurfaceLiquidWaterKilogramsPerSquareMeter =
                                cell.SurfaceLiquidWaterKilogramsPerSquareMeter,
                            SoilWaterKilogramsPerSquareMeter =
                                cell.SoilWaterKilogramsPerSquareMeter,
                            SnowIceWaterEquivalentKilogramsPerSquareMeter =
                                cell.SnowIceWaterEquivalentKilogramsPerSquareMeter
                        })
                .ToArray()
        };
    }

    private static PlanetHydrologyState FromSnapshot(
        PlanetHydrologySnapshot snapshot)
    {
        if (snapshot.GridDefinition is null)
        {
            throw new JsonException(
                "Hydrology surface-grid definition is required.");
        }

        if (snapshot.Cells is null)
        {
            throw new JsonException(
                "Hydrology cells collection is required.");
        }

        var gridDefinition =
            new SurfaceGridDefinition(
                snapshot.GridDefinition.Kind,
                snapshot.GridDefinition.IdentityVersion,
                snapshot.GridDefinition.LatitudeBandCount,
                snapshot.GridDefinition.LongitudeBandCount);

        return new PlanetHydrologyState(
            new PlanetId(snapshot.PlanetId),
            gridDefinition,
            snapshot.Cells.Select(
                cell =>
                    new HydrologyCellState(
                        new SurfaceCellId(
                            cell.SurfaceCellId),
                        cell.AtmosphericWaterKilogramsPerSquareMeter,
                        cell.SurfaceLiquidWaterKilogramsPerSquareMeter,
                        cell.SoilWaterKilogramsPerSquareMeter,
                        cell.SnowIceWaterEquivalentKilogramsPerSquareMeter)));
    }

    private static PlanetTerrainSnapshot ToSnapshot(
        PlanetTerrainState terrain)
    {
        return new PlanetTerrainSnapshot
        {
            PlanetId = terrain.PlanetId.Value,
            GridDefinition =
                new SurfaceGridDefinitionSnapshot
                {
                    Kind =
                        terrain.GridDefinition.Kind,
                    IdentityVersion =
                        terrain.GridDefinition.IdentityVersion,
                    LatitudeBandCount =
                        terrain.GridDefinition.LatitudeBandCount,
                    LongitudeBandCount =
                        terrain.GridDefinition.LongitudeBandCount
                },
            Cells = terrain.Cells
                .Select(
                    cell =>
                        new TerrainCellSnapshot
                        {
                            SurfaceCellId =
                                cell.CellId.Value,
                            ElevationMeters =
                                cell.ElevationMeters
                        })
                .ToArray()
        };
    }

    private static PlanetTerrainState FromSnapshot(
        PlanetTerrainSnapshot snapshot)
    {
        if (snapshot.GridDefinition is null)
        {
            throw new JsonException(
                "Terrain surface-grid definition is required.");
        }

        if (snapshot.Cells is null)
        {
            throw new JsonException(
                "Terrain cells collection is required.");
        }

        var gridDefinition =
            new SurfaceGridDefinition(
                snapshot.GridDefinition.Kind,
                snapshot.GridDefinition.IdentityVersion,
                snapshot.GridDefinition.LatitudeBandCount,
                snapshot.GridDefinition.LongitudeBandCount);

        return new PlanetTerrainState(
            new PlanetId(snapshot.PlanetId),
            gridDefinition,
            snapshot.Cells.Select(
                cell =>
                    new TerrainCellState(
                        new SurfaceCellId(
                            cell.SurfaceCellId),
                        cell.ElevationMeters)));
    }

    private static AnimalSnapshot ToSnapshot(
        AnimalState animal)
    {
        return new AnimalSnapshot
        {
            AnimalId = animal.Id.Value,
            PlanetId = animal.PlanetId.Value,
            Species = animal.Species,
            LatitudeDegrees = animal.LatitudeDegrees,
            LongitudeDegrees = animal.LongitudeDegrees,
            EnergyReserve = animal.EnergyReserve,
            Health = animal.Health,
            Activity = animal.Activity
        };
    }

    private static AnimalState FromSnapshot(
        AnimalSnapshot snapshot)
    {
        return new AnimalState(
            new AnimalId(snapshot.AnimalId),
            new PlanetId(snapshot.PlanetId),
            snapshot.Species,
            snapshot.LatitudeDegrees,
            snapshot.LongitudeDegrees,
            snapshot.EnergyReserve,
            snapshot.Health,
            snapshot.Activity);
    }

    private static void ValidateLegacyFoodResource(
        FoodResourceSnapshot snapshot,
        int schemaVersion,
        IReadOnlySet<Guid> planetIds)
    {
        if (snapshot.FoodResourceId == Guid.Empty)
        {
            throw new JsonException(
                "Legacy food resource identity cannot be empty.");
        }

        if (snapshot.PlanetId == Guid.Empty ||
            !planetIds.Contains(snapshot.PlanetId))
        {
            throw new JsonException(
                "Legacy food resource planet is invalid.");
        }

        if (!double.IsFinite(snapshot.LatitudeDegrees) ||
            snapshot.LatitudeDegrees < -90 ||
            snapshot.LatitudeDegrees > 90)
        {
            throw new JsonException(
                "Legacy food resource latitude is invalid.");
        }

        if (!double.IsFinite(snapshot.LongitudeDegrees) ||
            snapshot.LongitudeDegrees < -180 ||
            snapshot.LongitudeDegrees > 180)
        {
            throw new JsonException(
                "Legacy food resource longitude is invalid.");
        }

        if (!double.IsFinite(snapshot.AvailableEnergy) ||
            snapshot.AvailableEnergy < 0)
        {
            throw new JsonException(
                "Legacy food resource available energy is invalid.");
        }

        if (schemaVersion >=
            RenewableFoodSchemaVersion)
        {
            if (!snapshot.CapacityEnergy.HasValue)
            {
                throw new JsonException(
                    "Food resource capacity energy is required.");
            }

            if (!snapshot.RecoveryEnergyPerDay.HasValue)
            {
                throw new JsonException(
                    "Food resource recovery energy per day is required.");
            }

            if (!double.IsFinite(
                    snapshot.CapacityEnergy.Value) ||
                snapshot.CapacityEnergy.Value <
                    snapshot.AvailableEnergy)
            {
                throw new JsonException(
                    "Legacy food resource capacity energy is invalid.");
            }

            if (!double.IsFinite(
                    snapshot.RecoveryEnergyPerDay.Value) ||
                snapshot.RecoveryEnergyPerDay.Value < 0)
            {
                throw new JsonException(
                    "Legacy food resource recovery rate is invalid.");
            }
        }
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
            Activity = person.Activity,
            PregnancyConceptionTimeSeconds =
                person.Pregnancy?.ConceptionTimeSeconds,
            PregnancyFatherId =
                person.Pregnancy?.FatherId.Value
        };
    }

    private static PersonState FromSnapshot(
        PersonSnapshot snapshot,
        int schemaVersion)
    {
        PersonNeedsState needs;
        PersonActivity activity;
        PregnancyState? pregnancy;

        if (schemaVersion >= SurvivalSchemaVersion)
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

        if (schemaVersion >= PregnancySchemaVersion)
        {
            if (snapshot.PregnancyConceptionTimeSeconds.HasValue !=
                snapshot.PregnancyFatherId.HasValue)
            {
                throw new JsonException(
                    "Pregnancy conception time and father identity must both be present or both be absent.");
            }

            pregnancy =
                snapshot.PregnancyConceptionTimeSeconds.HasValue
                    ? new PregnancyState(
                        snapshot.PregnancyConceptionTimeSeconds.Value,
                        new PersonId(
                            snapshot.PregnancyFatherId!.Value))
                    : null;
        }
        else
        {
            pregnancy = null;
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
            activity,
            pregnancy);
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

        [System.Text.Json.Serialization.JsonIgnore(
            Condition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public FoodResourceSnapshot[]? FoodResources { get; set; }

        public AnimalSnapshot[]? Animals { get; set; }
        public PlanetTerrainSnapshot[]? Terrain { get; set; }
        public PlanetHydrologySnapshot[]? Hydrology { get; set; }
        public PlanetVegetationSnapshot[]? Vegetation { get; set; }
        public PlanetInvertebrateSnapshot[]? Invertebrates { get; set; }
        public BirdFlockSnapshot[]? BirdFlocks { get; set; }
        public GrazerCohortSnapshot[]? GrazerCohorts { get; set; }
        public PlanetBiogeochemistrySnapshot[]? Biogeochemistry { get; set; }
    }

    private sealed class PlanetBiogeochemistrySnapshot
    {
        public required Guid PlanetId { get; set; }

        public required SurfaceGridDefinitionSnapshot
            GridDefinition
        {
            get;
            set;
        }

        public required BiogeochemistryCellSnapshot[] Cells
        {
            get;
            set;
        }
    }

    private sealed class BiogeochemistryCellSnapshot
    {
        public required Guid SurfaceCellId { get; set; }

        public required double
            DetritalBiomassKilogramsPerSquareMeter
        {
            get;
            set;
        }

        public required double
            DetritalNitrogenKilogramsPerSquareMeter
        {
            get;
            set;
        }

        public required double
            PlantAvailableNitrogenKilogramsPerSquareMeter
        {
            get;
            set;
        }
    }

    private sealed class GrazerCohortSnapshot
    {
        public required Guid GrazerCohortId { get; set; }

        public required Guid PlanetId { get; set; }

        public required int MemberCount { get; set; }

        public required double LatitudeDegrees { get; set; }

        public required double LongitudeDegrees { get; set; }
    }

    private sealed class BirdFlockSnapshot
    {
        public required Guid BirdFlockId { get; set; }

        public required Guid PlanetId { get; set; }

        public required int MemberCount { get; set; }

        public required double LatitudeDegrees { get; set; }

        public required double LongitudeDegrees { get; set; }
    }

    private sealed class PlanetInvertebrateSnapshot
    {
        public required Guid PlanetId { get; set; }

        public required SurfaceGridDefinitionSnapshot
            GridDefinition
        {
            get;
            set;
        }

        public required InvertebrateCellSnapshot[] Cells
        {
            get;
            set;
        }
    }

    private sealed class InvertebrateCellSnapshot
    {
        public required Guid SurfaceCellId { get; set; }

        public required double
            LiveBiomassKilogramsPerSquareMeter
        {
            get;
            set;
        }
    }

    private sealed class PlanetVegetationSnapshot
    {
        public required Guid PlanetId { get; set; }

        public required SurfaceGridDefinitionSnapshot
            GridDefinition
        {
            get;
            set;
        }

        public required VegetationCellSnapshot[] Cells
        {
            get;
            set;
        }
    }

    private sealed class VegetationCellSnapshot
    {
        public required Guid SurfaceCellId { get; set; }

        public required double
            LiveBiomassKilogramsPerSquareMeter
        {
            get;
            set;
        }
    }

    private sealed class PlanetHydrologySnapshot
    {
        public required Guid PlanetId { get; set; }

        public required SurfaceGridDefinitionSnapshot
            GridDefinition
        {
            get;
            set;
        }

        public required HydrologyCellSnapshot[] Cells
        {
            get;
            set;
        }
    }

    private sealed class HydrologyCellSnapshot
    {
        public required Guid SurfaceCellId { get; set; }

        public required double
            AtmosphericWaterKilogramsPerSquareMeter
        {
            get;
            set;
        }

        public required double
            SurfaceLiquidWaterKilogramsPerSquareMeter
        {
            get;
            set;
        }

        public required double
            SoilWaterKilogramsPerSquareMeter
        {
            get;
            set;
        }

        public required double
            SnowIceWaterEquivalentKilogramsPerSquareMeter
        {
            get;
            set;
        }
    }

    private sealed class PlanetTerrainSnapshot
    {
        public required Guid PlanetId { get; set; }

        public required SurfaceGridDefinitionSnapshot
            GridDefinition
        {
            get;
            set;
        }

        public required TerrainCellSnapshot[] Cells
        {
            get;
            set;
        }
    }

    private sealed class SurfaceGridDefinitionSnapshot
    {
        public required SurfaceGridKind Kind { get; set; }
        public required int IdentityVersion { get; set; }
        public required int LatitudeBandCount { get; set; }
        public required int LongitudeBandCount { get; set; }
    }

    private sealed class TerrainCellSnapshot
    {
        public required Guid SurfaceCellId { get; set; }
        public required double ElevationMeters { get; set; }
    }

    private sealed class AnimalSnapshot
    {
        public required Guid AnimalId { get; set; }
        public required Guid PlanetId { get; set; }
        public required AnimalSpecies Species { get; set; }
        public required double LatitudeDegrees { get; set; }
        public required double LongitudeDegrees { get; set; }
        public required double EnergyReserve { get; set; }
        public required double Health { get; set; }
        public required AnimalActivity Activity { get; set; }
    }

    private sealed class FoodResourceSnapshot
    {
        public required Guid FoodResourceId { get; set; }
        public required Guid PlanetId { get; set; }
        public required double LatitudeDegrees { get; set; }
        public required double LongitudeDegrees { get; set; }
        public required double AvailableEnergy { get; set; }
        public double? CapacityEnergy { get; set; }
        public double? RecoveryEnergyPerDay { get; set; }
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
        public long? PregnancyConceptionTimeSeconds { get; set; }
        public Guid? PregnancyFatherId { get; set; }
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
