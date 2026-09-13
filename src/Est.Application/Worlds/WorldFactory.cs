using Est.Simulation.Animals;
using Est.Simulation.Ecology;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Time;
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

        var foodResources =
            specification.Planets
                .SelectMany(
                    (planetSpecification, index) =>
                        CreateFoodResources(
                            planets[index],
                            planetSpecification
                                .SyntheticFood))
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

        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            planets,
            population,
            foodResources,
            animals);
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

    private static IEnumerable<FoodResourceState>
        CreateFoodResources(
            PlanetState planet,
            SyntheticFoodCreationSpecification? specification)
    {
        if (specification is null)
        {
            return [];
        }

        ValidateFoodSpecification(specification);

        var random =
            new Random(specification.Seed);

        var resources =
            new FoodResourceState[
                specification.PatchCount];

        for (var index = 0;
             index < resources.Length;
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

            resources[index] =
                new FoodResourceState(
                    FoodResourceId.New(),
                    planet.Id,
                    latitude,
                    longitude,
                    specification.EnergyPerPatch,
                    capacityEnergy:
                        specification.EnergyPerPatch,
                    recoveryEnergyPerDay:
                        specification.RecoveryEnergyPerDay);
        }

        return resources;
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

    private static void ValidateFoodSpecification(
        SyntheticFoodCreationSpecification specification)
    {
        if (specification.PatchCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(specification.PatchCount));
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

        if (!double.IsFinite(
                specification.SpreadDegrees) ||
            specification.SpreadDegrees < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(specification.SpreadDegrees));
        }

        if (!double.IsFinite(
                specification.EnergyPerPatch) ||
            specification.EnergyPerPatch < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(specification.EnergyPerPatch));
        }

        if (!double.IsFinite(
                specification.RecoveryEnergyPerDay) ||
            specification.RecoveryEnergyPerDay < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    specification.RecoveryEnergyPerDay));
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
