using Est.Simulation.Causality;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Worlds;

namespace Est.Simulation.Population;

public sealed class PopulationSystem : ICausalSystem
{
    private const double SecondsPerYear = 31_536_000d;

    private readonly PlanetId _planetId;
    private readonly PopulationModelParameters _parameters;

    public PopulationSystem(
        PlanetId planetId,
        PopulationModelParameters parameters)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(parameters);

        _planetId = planetId;
        _parameters = parameters;
    }

    public SimulationChange Evaluate(
        WorldState world,
        long elapsedSeconds)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (elapsedSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elapsedSeconds));
        }

        if (!world.Planets.Any(
                planet => planet.Id == _planetId))
        {
            throw new InvalidOperationException(
                "The target planet does not exist in this world.");
        }

        var existing = world.Population
            .Where(person => person.PlanetId == _planetId)
            .OrderBy(person => person.Id.Value)
            .ToArray();

        var years = elapsedSeconds / SecondsPerYear;

        var random = new DeterministicRandom(
            CreateStepSeed(
                _parameters.Seed,
                world.CurrentTime.TotalSeconds,
                elapsedSeconds));

        var survivors = new List<PersonState>(
            existing.Length);

        var deaths = 0;
        var migrations = 0;

        foreach (var person in existing)
        {
            var age = person.AgeYears(
                world.CurrentTime.TotalSeconds);

            var mortalityRate =
                age >= _parameters.ElderAgeYears
                    ? _parameters.AnnualElderMortalityRate
                    : _parameters.AnnualBaseMortalityRate;

            if (Occurs(
                    mortalityRate,
                    years,
                    random))
            {
                deaths++;
                continue;
            }

            var moved = person;

            if (age >=
                    _parameters.ReproductiveAgeMinimumYears &&
                Occurs(
                    _parameters.AnnualAdultMigrationRate,
                    years,
                    random))
            {
                moved = Migrate(person, random);
                migrations++;
            }

            survivors.Add(moved);
        }

        var hasAdultMale = survivors.Any(
            person =>
                person.Sex == PersonSex.Male &&
                person.AgeYears(
                    world.CurrentTime.TotalSeconds)
                >= _parameters
                    .ReproductiveAgeMinimumYears);

        var births = new List<PersonState>();

        if (hasAdultMale && elapsedSeconds > 0)
        {
            foreach (var mother in survivors)
            {
                if (mother.Sex != PersonSex.Female)
                {
                    continue;
                }

                var age = mother.AgeYears(
                    world.CurrentTime.TotalSeconds);

                if (age <
                        _parameters
                            .ReproductiveAgeMinimumYears ||
                    age >
                        _parameters
                            .ReproductiveAgeMaximumYears)
                {
                    continue;
                }

                if (!Occurs(
                        _parameters
                            .AnnualBirthRatePerEligibleFemale,
                        years,
                        random))
                {
                    continue;
                }

                births.Add(
                    CreateChild(
                        mother,
                        world.CurrentTime.TotalSeconds
                            + elapsedSeconds,
                        random));
            }
        }

        var nextPopulation = survivors
            .Concat(births)
            .ToArray();

        var operation =
            new ReplacePlanetPopulationOperation(
                _planetId,
                nextPopulation);

        return new SimulationChange(
            operation,
            "population-dynamics",
            "Birth, death, and migration changed planetary population.",
            _planetId,
            elapsedSeconds,
            new Dictionary<string, double>
            {
                ["previousPopulation"] = existing.Length,
                ["births"] = births.Count,
                ["deaths"] = deaths,
                ["migrations"] = migrations,
                ["newPopulation"] = nextPopulation.Length
            });
    }

    private PersonState Migrate(
        PersonState person,
        DeterministicRandom random)
    {
        var maximumDistance =
            random.NextDouble() <
                _parameters.LongMigrationProbability
                ? _parameters.LongMigrationDegrees
                : _parameters.LocalMigrationDegrees;

        var distance =
            random.NextDouble() * maximumDistance;

        var angle =
            random.NextDouble() * Math.PI * 2;

        var latitude =
            Math.Clamp(
                person.LatitudeDegrees
                    + Math.Cos(angle) * distance,
                -89.999,
                89.999);

        var longitude =
            WrapLongitude(
                person.LongitudeDegrees
                    + Math.Sin(angle) * distance);

        return person.MoveTo(
            latitude,
            longitude);
    }

    private PersonState CreateChild(
        PersonState mother,
        long birthTimeSeconds,
        DeterministicRandom random)
    {
        var distance =
            random.NextDouble()
            * Math.Min(
                0.25,
                _parameters.LocalMigrationDegrees);

        var angle =
            random.NextDouble()
            * Math.PI * 2;

        var latitude =
            Math.Clamp(
                mother.LatitudeDegrees
                    + Math.Cos(angle) * distance,
                -89.999,
                89.999);

        var longitude =
            WrapLongitude(
                mother.LongitudeDegrees
                    + Math.Sin(angle) * distance);

        return new PersonState(
            new PersonId(random.NextGuid()),
            mother.PlanetId,
            random.NextDouble() < 0.5
                ? PersonSex.Female
                : PersonSex.Male,
            birthTimeSeconds,
            latitude,
            longitude,
            mother.Id);
    }

    private static bool Occurs(
        double annualRate,
        double elapsedYears,
        DeterministicRandom random)
    {
        if (annualRate <= 0 ||
            elapsedYears <= 0)
        {
            return false;
        }

        var probability =
            1 - Math.Exp(
                -annualRate * elapsedYears);

        return random.NextDouble() <
            Math.Clamp(probability, 0, 1);
    }

    private static ulong CreateStepSeed(
        int configuredSeed,
        long currentTimeSeconds,
        long elapsedSeconds)
    {
        unchecked
        {
            var seed =
                (ulong)(uint)configuredSeed
                ^ ((ulong)currentTimeSeconds
                    * 0x9E3779B97F4A7C15UL)
                ^ ((ulong)elapsedSeconds
                    * 0xBF58476D1CE4E5B9UL);

            return seed == 0
                ? 0xA0761D6478BD642FUL
                : seed;
        }
    }

    private static double WrapLongitude(
        double longitude)
    {
        var wrapped =
            ((longitude + 180) % 360 + 360)
            % 360 - 180;

        return wrapped;
    }

    private sealed class DeterministicRandom
    {
        private ulong _state;

        public DeterministicRandom(ulong seed)
        {
            _state = seed;
        }

        public double NextDouble()
        {
            var value = NextUInt64() >> 11;

            return value
                * (1.0 / (1UL << 53));
        }

        public Guid NextGuid()
        {
            var bytes = new byte[16];

            BitConverter
                .GetBytes(NextUInt64())
                .CopyTo(bytes, 0);

            BitConverter
                .GetBytes(NextUInt64())
                .CopyTo(bytes, 8);

            return new Guid(bytes);
        }

        private ulong NextUInt64()
        {
            var value = _state;

            value ^= value >> 12;
            value ^= value << 25;
            value ^= value >> 27;

            _state = value;

            return value
                * 2685821657736338717UL;
        }
    }
}
