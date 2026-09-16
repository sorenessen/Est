using Est.Simulation.Animals;
using Est.Simulation.Biogeochemistry;
using Est.Simulation.Birds;
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

namespace Est.Application.Worlds;

public static class WorldFactory
{
    private const double SecondsPerYear = 31_536_000;

    public static WorldState Create(
        WorldCreationSpecification specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(specification.Planets);

        var planets = specification.Planets
            .Select(CreatePlanet)
            .ToArray();

        var population =
            specification.Planets
                .SelectMany(
                    (planetSpecification, index) =>
                        CreatePopulation(
                            planets[index],
                            planetSpecification
                                .SyntheticPopulation))
                .ToArray();

        var animals =
            specification.Planets
                .SelectMany(
                    (planetSpecification, index) =>
                        CreateAnimals(
                            planets[index],
                            planetSpecification
                                .SyntheticAnimals))
                .ToArray();

        var terrain =
            specification.Planets
                .Select(
                    (planetSpecification, index) =>
                        CreateTerrain(
                            planets[index],
                            planetSpecification
                                .GeneratedTerrain))
                .Where(
                    item =>
                        item is not null)
                .Select(
                    item =>
                        item!)
                .ToArray();

        var terrainByPlanetId =
            terrain.ToDictionary(
                item =>
                    item.PlanetId);

        var hydrology =
            specification.Planets
                .Select(
                    (planetSpecification, index) =>
                        CreateHydrology(
                            planets[index],
                            terrainByPlanetId.GetValueOrDefault(
                                planets[index].Id),
                            planetSpecification
                                .GeneratedHydrology))
                .Where(
                    item =>
                        item is not null)
                .Select(
                    item =>
                        item!)
                .ToArray();

        var hydrologyByPlanetId =
            hydrology.ToDictionary(
                item =>
                    item.PlanetId);

        var biogeochemistry =
            specification.Planets
                .Select(
                    (planetSpecification, index) =>
                        CreateBiogeochemistry(
                            planets[index],
                            terrainByPlanetId.GetValueOrDefault(
                                planets[index].Id),
                            hydrologyByPlanetId.GetValueOrDefault(
                                planets[index].Id),
                            planetSpecification
                                .GeneratedBiogeochemistry))
                .Where(
                    item =>
                        item is not null)
                .Select(
                    item =>
                        item!)
                .ToArray();

        var vegetation =
            specification.Planets
                .Select(
                    (planetSpecification, index) =>
                        CreateVegetation(
                            planets[index],
                            terrainByPlanetId.GetValueOrDefault(
                                planets[index].Id),
                            hydrologyByPlanetId.GetValueOrDefault(
                                planets[index].Id),
                            planetSpecification
                                .GeneratedVegetation))
                .Where(
                    item =>
                        item is not null)
                .Select(
                    item =>
                        item!)
                .ToArray();

        var vegetationByPlanetId =
            vegetation.ToDictionary(
                item =>
                    item.PlanetId);

        var grazerCohorts =
            specification.Planets
                .SelectMany(
                    (planetSpecification, index) =>
                        CreateGrazerCohorts(
                            planets[index],
                            vegetationByPlanetId.GetValueOrDefault(
                                planets[index].Id),
                            planetSpecification
                                .GeneratedGrazers))
                .ToArray();

        var invertebrates =
            specification.Planets
                .Select(
                    (planetSpecification, index) =>
                        CreateInvertebrates(
                            planets[index],
                            vegetationByPlanetId.GetValueOrDefault(
                                planets[index].Id),
                            planetSpecification
                                .GeneratedInvertebrates))
                .Where(
                    item =>
                        item is not null)
                .Select(
                    item =>
                        item!)
                .ToArray();

        var invertebratesByPlanetId =
            invertebrates.ToDictionary(
                item =>
                    item.PlanetId);

        var birdFlocks =
            specification.Planets
                .SelectMany(
                    (planetSpecification, index) =>
                        CreateBirdFlocks(
                            planets[index],
                            invertebratesByPlanetId.GetValueOrDefault(
                                planets[index].Id),
                            planetSpecification
                                .GeneratedBirds))
                .ToArray();

        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
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

    private static PlanetState CreatePlanet(
        PlanetCreationSpecification specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(specification.Environment);
        ArgumentNullException.ThrowIfNull(
            specification.Environment.Atmosphere);

        var environment = specification.Environment;
        var atmosphere = environment.Atmosphere;

        return new PlanetState(
            PlanetId.New(),
            specification.Name,
            specification.MassKilograms,
            specification.MeanRadiusMeters,
            new PlanetEnvironment(
                environment.MeanSurfaceTemperatureKelvin,
                environment.SurfaceWaterFraction,
                environment.IceCoverageFraction,
                new AtmosphereState(
                    atmosphere.SurfacePressurePascals,
                    atmosphere.CompositionByMoleFraction)));
    }

    private static PlanetTerrainState? CreateTerrain(
        PlanetState planet,
        GeneratedTerrainCreationSpecification? specification)
    {
        if (specification is null)
        {
            return null;
        }

        var gridDefinition =
            SurfaceGridDefinition.LatitudeLongitude(
                specification.LatitudeBandCount,
                specification.LongitudeBandCount);

        var parameters =
            new TectonicTerrainGenerationParameters(
                gridDefinition,
                specification.Seed,
                specification.PlateCount,
                specification.ContinentalPlateFraction);

        return TectonicTerrainGenerator.Generate(
            planet,
            parameters);
    }

    private static PlanetHydrologyState? CreateHydrology(
        PlanetState planet,
        PlanetTerrainState? terrain,
        GeneratedHydrologyCreationSpecification? specification)
    {
        if (specification is null)
        {
            return null;
        }

        if (terrain is null)
        {
            throw new ArgumentException(
                "Generated hydrology requires generated terrain.",
                nameof(specification));
        }

        if (!double.IsFinite(
                specification
                    .SurfaceLiquidWaterInventoryKilograms) ||
            specification
                .SurfaceLiquidWaterInventoryKilograms <
            0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    specification
                        .SurfaceLiquidWaterInventoryKilograms),
                "Surface-liquid water inventory must be finite and nonnegative.");
        }

        return PlanetHydrologyInitializer
            .FromSurfaceLiquidWaterInventory(
                planet,
                terrain,
                specification
                    .SurfaceLiquidWaterInventoryKilograms);
    }

    private static PlanetBiogeochemistryState? CreateBiogeochemistry(
        PlanetState planet,
        PlanetTerrainState? terrain,
        PlanetHydrologyState? hydrology,
        GeneratedBiogeochemistryCreationSpecification? specification)
    {
        if (specification is null)
        {
            return null;
        }

        if (terrain is null)
        {
            throw new ArgumentException(
                "Generated biogeochemistry requires generated terrain.",
                nameof(specification));
        }

        if (hydrology is null)
        {
            throw new ArgumentException(
                "Generated biogeochemistry requires generated hydrology.",
                nameof(specification));
        }

        if (terrain.GridDefinition !=
            hydrology.GridDefinition)
        {
            throw new ArgumentException(
                "Generated biogeochemistry requires terrain and hydrology on the same surface grid.",
                nameof(specification));
        }

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                terrain.GridDefinition);

        return new PlanetBiogeochemistryState(
            planet.Id,
            terrain.GridDefinition,
            grid.Cells.Select(
                cell =>
                    new BiogeochemistryCellState(
                        cell.Id,
                        specification
                            .InitialDetritalBiomassKilogramsPerSquareMeter,
                        specification
                            .InitialDetritalNitrogenKilogramsPerSquareMeter,
                        specification
                            .InitialPlantAvailableNitrogenKilogramsPerSquareMeter)));
    }

    private static PlanetVegetationState? CreateVegetation(
        PlanetState planet,
        PlanetTerrainState? terrain,
        PlanetHydrologyState? hydrology,
        GeneratedVegetationCreationSpecification? specification)
    {
        if (specification is null)
        {
            return null;
        }

        if (terrain is null)
        {
            throw new ArgumentException(
                "Generated vegetation requires generated terrain.",
                nameof(specification));
        }

        if (hydrology is null)
        {
            throw new ArgumentException(
                "Generated vegetation requires generated hydrology.",
                nameof(specification));
        }

        return PlanetVegetationInitializer
            .FromDrySurfaceBiomass(
                planet,
                terrain,
                hydrology,
                specification
                    .InitialLiveBiomassKilogramsPerSquareMeter);
    }

    private static IEnumerable<GrazerCohortState> CreateGrazerCohorts(
        PlanetState planet,
        PlanetVegetationState? vegetation,
        GeneratedGrazerCreationSpecification? specification)
    {
        if (specification is null)
        {
            return [];
        }

        if (vegetation is null)
        {
            throw new ArgumentException(
                "Generated grazers require generated vegetation.",
                nameof(specification));
        }

        var parameters =
            new GrazerModelParameters(
                carryingCapacityGrazersPerKilogramLiveVegetationBiomass:
                    specification
                        .CarryingCapacityGrazersPerKilogramLiveVegetationBiomass,
                initialFractionOfLocalCarryingCapacity:
                    specification
                        .InitialFractionOfLocalCarryingCapacity,
                minimumInitialCohortMemberCount:
                    specification
                        .MinimumInitialCohortMemberCount,
                maximumInitialCohortCount:
                    specification
                        .MaximumInitialCohortCount);

        return PlanetGrazerCohortInitializer
            .FromVegetationSupport(
                planet,
                vegetation,
                parameters);
    }

    private static PlanetInvertebrateState? CreateInvertebrates(
        PlanetState planet,
        PlanetVegetationState? vegetation,
        GeneratedInvertebrateCreationSpecification? specification)
    {
        if (specification is null)
        {
            return null;
        }

        if (vegetation is null)
        {
            throw new ArgumentException(
                "Generated invertebrates require generated vegetation.",
                nameof(specification));
        }

        var parameters =
            new InvertebrateModelParameters(
                carryingCapacityKilogramsPerKilogramLiveVegetation:
                    specification
                        .CarryingCapacityKilogramsPerKilogramLiveVegetation,
                initialFractionOfLocalCarryingCapacity:
                    specification
                        .InitialFractionOfLocalCarryingCapacity);

        return PlanetInvertebrateInitializer
            .FromVegetationSupport(
                planet,
                vegetation,
                parameters);
    }

    private static IEnumerable<BirdFlockState> CreateBirdFlocks(
        PlanetState planet,
        PlanetInvertebrateState? invertebrates,
        GeneratedBirdCreationSpecification? specification)
    {
        if (specification is null)
        {
            return [];
        }

        if (invertebrates is null)
        {
            throw new ArgumentException(
                "Generated birds require generated invertebrates.",
                nameof(specification));
        }

        var parameters =
            new BirdModelParameters(
                carryingCapacityBirdsPerKilogramLiveInvertebrateBiomass:
                    specification
                        .CarryingCapacityBirdsPerKilogramLiveInvertebrateBiomass,
                initialFractionOfLocalCarryingCapacity:
                    specification
                        .InitialFractionOfLocalCarryingCapacity,
                minimumInitialFlockMemberCount:
                    specification
                        .MinimumInitialFlockMemberCount,
                maximumInitialFlockCount:
                    specification
                        .MaximumInitialFlockCount);

        return PlanetBirdFlockInitializer
            .FromInvertebrateSupport(
                planet,
                invertebrates,
                parameters);
    }

    private static IEnumerable<AnimalState> CreateAnimals(
        PlanetState planet,
        SyntheticAnimalCreationSpecification? specification)
    {
        if (specification is null)
        {
            return [];
        }

        ValidateAnimalSpecification(specification);

        var random =
            new Random(specification.Seed);

        var animals =
            new AnimalState[specification.WolfCount];

        for (var index = 0;
             index < animals.Length;
             index++)
        {
            var radius =
                Math.Sqrt(random.NextDouble()) *
                specification.SpreadDegrees;

            var angle =
                random.NextDouble() *
                Math.PI *
                2;

            var latitude =
                Math.Clamp(
                    specification.CenterLatitudeDegrees +
                    Math.Sin(angle) * radius,
                    -90,
                    90);

            var longitude =
                NormalizeLongitude(
                    specification.CenterLongitudeDegrees +
                    Math.Cos(angle) * radius);

            var idBytes = new byte[16];
            random.NextBytes(idBytes);

            animals[index] =
                new AnimalState(
                    new AnimalId(
                        new Guid(idBytes)),
                    planet.Id,
                    AnimalSpecies.Wolf,
                    latitude,
                    longitude,
                    energyReserve: 0.35,
                    health: 1,
                    activity: AnimalActivity.Hunting);
        }

        return animals;
    }

    private static IEnumerable<PersonState> CreatePopulation(
        PlanetState planet,
        SyntheticPopulationCreationSpecification? specification)
    {
        if (specification is null)
        {
            return [];
        }

        ValidatePopulationSpecification(specification);

        var random =
            new Random(specification.Seed);

        var population =
            new PersonState[specification.FounderCount];

        for (var index = 0;
             index < population.Length;
             index++)
        {
            var radius =
                Math.Sqrt(random.NextDouble()) *
                specification.SpreadDegrees;

            var angle =
                random.NextDouble() *
                Math.PI *
                2;

            var latitude =
                Math.Clamp(
                    specification.CenterLatitudeDegrees +
                    Math.Sin(angle) * radius,
                    -90,
                    90);

            var longitude =
                NormalizeLongitude(
                    specification.CenterLongitudeDegrees +
                    Math.Cos(angle) * radius);

            var ageYears =
                specification.MinimumAgeYears +
                random.NextDouble() *
                (specification.MaximumAgeYears -
                 specification.MinimumAgeYears);

            var birthTimeSeconds =
                -checked(
                    (long)Math.Round(
                        ageYears *
                        SecondsPerYear));

            population[index] =
                new PersonState(
                    new PersonId(Guid.NewGuid()),
                    planet.Id,
                    index % 2 == 0
                        ? PersonSex.Female
                        : PersonSex.Male,
                    birthTimeSeconds,
                    latitude,
                    longitude);
        }

        return population;
    }

    private static void ValidateAnimalSpecification(
        SyntheticAnimalCreationSpecification specification)
    {
        if (specification.WolfCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(specification.WolfCount));
        }

        if (!double.IsFinite(
                specification.CenterLatitudeDegrees) ||
            specification.CenterLatitudeDegrees < -90 ||
            specification.CenterLatitudeDegrees > 90)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    specification.CenterLatitudeDegrees));
        }

        if (!double.IsFinite(
                specification.CenterLongitudeDegrees) ||
            specification.CenterLongitudeDegrees < -180 ||
            specification.CenterLongitudeDegrees > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    specification.CenterLongitudeDegrees));
        }

        if (!double.IsFinite(specification.SpreadDegrees) ||
            specification.SpreadDegrees < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(specification.SpreadDegrees));
        }
    }

    private static void ValidatePopulationSpecification(
        SyntheticPopulationCreationSpecification specification)
    {
        if (specification.FounderCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(specification.FounderCount));
        }

        if (!double.IsFinite(
                specification.CenterLatitudeDegrees) ||
            specification.CenterLatitudeDegrees < -90 ||
            specification.CenterLatitudeDegrees > 90)
        {
            throw new ArgumentOutOfRangeException(
                nameof(specification.CenterLatitudeDegrees));
        }

        if (!double.IsFinite(
                specification.CenterLongitudeDegrees) ||
            specification.CenterLongitudeDegrees < -180 ||
            specification.CenterLongitudeDegrees > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(specification.CenterLongitudeDegrees));
        }

        if (!double.IsFinite(specification.SpreadDegrees) ||
            specification.SpreadDegrees < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(specification.SpreadDegrees));
        }

        if (!double.IsFinite(
                specification.MinimumAgeYears) ||
            specification.MinimumAgeYears < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(specification.MinimumAgeYears));
        }

        if (!double.IsFinite(
                specification.MaximumAgeYears) ||
            specification.MaximumAgeYears <
            specification.MinimumAgeYears)
        {
            throw new ArgumentOutOfRangeException(
                nameof(specification.MaximumAgeYears));
        }
    }

    private static double NormalizeLongitude(
        double longitudeDegrees)
    {
        var normalized =
            (longitudeDegrees + 180) % 360;

        if (normalized < 0)
        {
            normalized += 360;
        }

        return normalized - 180;
    }
}
