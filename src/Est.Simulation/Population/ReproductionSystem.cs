using Est.Simulation.Causality;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Worlds;

namespace Est.Simulation.Population;

public sealed class ReproductionSystem : ICausalSystem
{
    private const double SecondsPerYear = 31_536_000d;
    private const double PartnerSearchRadiusDegrees = 5d;
    private const double MatingDistanceDegrees = 0.12d;
    private const double TravelDegreesPerDay = 0.25d;
    private const double MinimumEnergyReserve = 0.6d;
    private const double MinimumHealth = 0.6d;

    private readonly PlanetId _planetId;
    private readonly PopulationModelParameters _parameters;

    public ReproductionSystem(
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

        var population = world.Population
            .Where(person => person.PlanetId == _planetId)
            .OrderBy(person => person.Id.Value)
            .ToArray();

        var currentTime =
            world.CurrentTime.TotalSeconds;

        var males = population
            .Where(
                person =>
                    person.Sex == PersonSex.Male &&
                    IsEligible(
                        person,
                        currentTime,
                        requireFemaleMaximumAge: false))
            .ToArray();

        var random =
            new DeterministicRandom(
                CreateStepSeed(
                    _parameters.Seed,
                    currentTime,
                    elapsedSeconds));

        var nextPopulation =
            new List<PersonState>(
                population.Length);

        var births =
            new List<PersonState>();

        var partnerSeeking = 0;
        var matingEvents = 0;
        var eligibleFemales = 0;
        var noPartnerFound = 0;

        foreach (var person in population)
        {
            if (person.Sex != PersonSex.Female ||
                !IsEligible(
                    person,
                    currentTime,
                    requireFemaleMaximumAge: true))
            {
                nextPopulation.Add(person);
                continue;
            }

            eligibleFemales++;

            var partner =
                FindNearestPartner(
                    person,
                    males);

            if (partner is null)
            {
                noPartnerFound++;
                nextPopulation.Add(person);
                continue;
            }

            var distance =
                DistanceDegrees(
                    person.LatitudeDegrees,
                    person.LongitudeDegrees,
                    partner.LatitudeDegrees,
                    partner.LongitudeDegrees);

            if (distance > MatingDistanceDegrees)
            {
                var moved =
                    MoveToward(
                        person,
                        partner,
                        elapsedSeconds);

                nextPopulation.Add(
                    moved.WithSurvivalState(
                        moved.Needs,
                        PersonActivity.SeekingPartner));

                partnerSeeking++;
                continue;
            }

            var mating =
                person.WithSurvivalState(
                    person.Needs,
                    PersonActivity.Mating);

            nextPopulation.Add(mating);
            matingEvents++;

            if (Occurs(
                    _parameters
                        .AnnualBirthRatePerEligibleFemale,
                    elapsedSeconds / SecondsPerYear,
                    random))
            {
                births.Add(
                    CreateChild(
                        mating,
                        currentTime + elapsedSeconds,
                        random));
            }
        }

        nextPopulation.AddRange(births);

        var operation =
            new ReplacePlanetPopulationOperation(
                _planetId,
                nextPopulation);

        return new SimulationChange(
            operation,
            "reproduction",
            "Partner seeking, mating, and births changed planetary population.",
            _planetId,
            elapsedSeconds,
            new Dictionary<string, double>
            {
                ["eligibleFemales"] = eligibleFemales,
                ["partnerSeeking"] = partnerSeeking,
                ["matingEvents"] = matingEvents,
                ["noPartnerFound"] = noPartnerFound,
                ["births"] = births.Count
            });
    }

    private bool IsEligible(
        PersonState person,
        long currentTimeSeconds,
        bool requireFemaleMaximumAge)
    {
        if (person.Activity == PersonActivity.Fleeing ||
            person.Needs.EnergyReserve < MinimumEnergyReserve ||
            person.Needs.Health < MinimumHealth)
        {
            return false;
        }

        var age =
            person.AgeYears(currentTimeSeconds);

        if (age <
            _parameters.ReproductiveAgeMinimumYears)
        {
            return false;
        }

        if (requireFemaleMaximumAge &&
            age >
            _parameters.ReproductiveAgeMaximumYears)
        {
            return false;
        }

        return true;
    }

    private static PersonState? FindNearestPartner(
        PersonState person,
        IEnumerable<PersonState> candidates)
    {
        PersonState? nearest = null;
        var nearestDistance = double.MaxValue;

        foreach (var candidate in candidates)
        {
            var distance =
                DistanceDegrees(
                    person.LatitudeDegrees,
                    person.LongitudeDegrees,
                    candidate.LatitudeDegrees,
                    candidate.LongitudeDegrees);

            if (distance >
                    PartnerSearchRadiusDegrees ||
                distance >= nearestDistance)
            {
                continue;
            }

            nearest = candidate;
            nearestDistance = distance;
        }

        return nearest;
    }

    private static PersonState MoveToward(
        PersonState person,
        PersonState partner,
        long elapsedSeconds)
    {
        var latitudeDelta =
            partner.LatitudeDegrees -
            person.LatitudeDegrees;

        var longitudeDelta =
            WrappedLongitudeDelta(
                person.LongitudeDegrees,
                partner.LongitudeDegrees);

        var distance =
            Math.Sqrt(
                latitudeDelta * latitudeDelta +
                longitudeDelta * longitudeDelta);

        if (distance <= 0)
        {
            return person;
        }

        var elapsedDays =
            elapsedSeconds / 86_400d;

        var travelDistance =
            Math.Min(
                distance,
                TravelDegreesPerDay *
                elapsedDays);

        var scale =
            travelDistance / distance;

        var latitude =
            Math.Clamp(
                person.LatitudeDegrees +
                latitudeDelta * scale,
                -89.999,
                89.999);

        var longitude =
            WrapLongitude(
                person.LongitudeDegrees +
                longitudeDelta * scale);

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
            random.NextDouble() *
            Math.Min(
                0.25,
                _parameters.LocalMigrationDegrees);

        var angle =
            random.NextDouble() *
            Math.PI *
            2;

        var latitude =
            Math.Clamp(
                mother.LatitudeDegrees +
                Math.Cos(angle) * distance,
                -89.999,
                89.999);

        var longitude =
            WrapLongitude(
                mother.LongitudeDegrees +
                Math.Sin(angle) * distance);

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

    private static double DistanceDegrees(
        double firstLatitude,
        double firstLongitude,
        double secondLatitude,
        double secondLongitude)
    {
        var latitudeDelta =
            secondLatitude -
            firstLatitude;

        var longitudeDelta =
            WrappedLongitudeDelta(
                firstLongitude,
                secondLongitude);

        return Math.Sqrt(
            latitudeDelta * latitudeDelta +
            longitudeDelta * longitudeDelta);
    }

    private static double WrappedLongitudeDelta(
        double from,
        double to)
    {
        var delta = to - from;

        if (delta > 180)
        {
            delta -= 360;
        }
        else if (delta < -180)
        {
            delta += 360;
        }

        return delta;
    }

    private static double WrapLongitude(
        double longitude)
    {
        return
            ((longitude + 180) % 360 + 360)
            % 360 - 180;
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
            1 -
            Math.Exp(
                -annualRate *
                elapsedYears);

        return random.NextDouble() <
            Math.Clamp(
                probability,
                0,
                1);
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
                    * 0xBF58476D1CE4E5B9UL)
                ^ 0x94D049BB133111EBUL;

            return seed == 0
                ? 0xA0761D6478BD642FUL
                : seed;
        }
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
            var value =
                NextUInt64() >> 11;

            return value *
                (1.0 / (1UL << 53));
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

            return value *
                2685821657736338717UL;
        }
    }
}
