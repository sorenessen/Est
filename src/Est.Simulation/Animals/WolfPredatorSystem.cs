using Est.Simulation.Causality;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Worlds;

namespace Est.Simulation.Animals;

public sealed class WolfPredatorSystem : ICausalSystem
{
    private const double TravelDegreesPerDay = 0.75;
    private const double AttackRadiusDegrees = 0.12;
    private const double EnergyUsePerDay = 0.08;
    private const double EnergyPerKill = 0.65;
    private const long MaximumIntegrationStepSeconds = 86_400;

    private readonly PlanetId _planetId;

    public WolfPredatorSystem(PlanetId planetId)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        _planetId = planetId;
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
            throw new PlanetNotFoundException(_planetId);
        }

        var population =
            world.Population
                .Where(person => person.PlanetId == _planetId)
                .OrderBy(person => person.Id.Value)
                .ToList();

        var animals =
            world.Animals
                .Where(animal => animal.PlanetId == _planetId)
                .OrderBy(animal => animal.Id.Value)
                .ToList();

        var wolfAttacks = 0;
        var successfulKills = 0;
        var predationDeaths = 0;
        var chaseSteps = 0;

        double? lastAttackLatitude = null;
        double? lastAttackLongitude = null;

        var remainingSeconds = elapsedSeconds;

        while (remainingSeconds > 0 &&
               population.Count > 0)
        {
            var stepSeconds =
                Math.Min(
                    MaximumIntegrationStepSeconds,
                    remainingSeconds);

            var elapsedDays =
                stepSeconds / 86_400d;

            for (var index = 0;
                 index < animals.Count;
                 index++)
            {
                var wolf = animals[index];

                if (wolf.Species != AnimalSpecies.Wolf ||
                    wolf.Health <= 0 ||
                    population.Count == 0)
                {
                    continue;
                }

                var energy =
                    Math.Max(
                        0,
                        wolf.EnergyReserve -
                        EnergyUsePerDay * elapsedDays);

                var target =
                    FindNearestPerson(
                        wolf,
                        population);

                if (target is null)
                {
                    animals[index] =
                        wolf.WithState(
                            wolf.LatitudeDegrees,
                            wolf.LongitudeDegrees,
                            energy,
                            wolf.Health,
                            AnimalActivity.Idle);
                    continue;
                }

                var distance =
                    DistanceDegrees(
                        wolf.LatitudeDegrees,
                        wolf.LongitudeDegrees,
                        target.LatitudeDegrees,
                        target.LongitudeDegrees);

                if (distance > AttackRadiusDegrees)
                {
                    var moved =
                        MoveToward(
                            wolf,
                            target,
                            elapsedDays);

                    animals[index] =
                        moved.WithState(
                            moved.LatitudeDegrees,
                            moved.LongitudeDegrees,
                            energy,
                            moved.Health,
                            AnimalActivity.Traveling);

                    chaseSteps++;
                    continue;
                }

                wolfAttacks++;
                successfulKills++;
                predationDeaths++;

                lastAttackLatitude =
                    target.LatitudeDegrees;
                lastAttackLongitude =
                    target.LongitudeDegrees;

                population.RemoveAll(
                    person => person.Id == target.Id);

                animals[index] =
                    wolf.WithState(
                        target.LatitudeDegrees,
                        target.LongitudeDegrees,
                        Math.Min(
                            1,
                            energy + EnergyPerKill),
                        wolf.Health,
                        AnimalActivity.Eating);
            }

            remainingSeconds -= stepSeconds;
        }

        var metrics =
            new Dictionary<string, double>
            {
                ["wolfAttacks"] = wolfAttacks,
                ["successfulKills"] = successfulKills,
                ["predationDeaths"] = predationDeaths,
                ["chaseSteps"] = chaseSteps,
                ["wolves"] =
                    animals.Count(
                        animal =>
                            animal.Species ==
                            AnimalSpecies.Wolf),
                ["survivors"] = population.Count
            };

        if (lastAttackLatitude is not null &&
            lastAttackLongitude is not null)
        {
            metrics["attackLatitude"] =
                lastAttackLatitude.Value;
            metrics["attackLongitude"] =
                lastAttackLongitude.Value;
        }

        return new SimulationChange(
            new ReplacePlanetPredatorStateOperation(
                _planetId,
                population,
                animals),
            "predation",
            successfulKills > 0
                ? "A wolf attacked and killed human prey."
                : "Wolf predation state changed.",
            _planetId,
            elapsedSeconds,
            metrics);
    }

    private static PersonState? FindNearestPerson(
        AnimalState wolf,
        IEnumerable<PersonState> population)
    {
        PersonState? nearest = null;
        var nearestDistance =
            double.PositiveInfinity;

        foreach (var person in population)
        {
            var distance =
                DistanceDegrees(
                    wolf.LatitudeDegrees,
                    wolf.LongitudeDegrees,
                    person.LatitudeDegrees,
                    person.LongitudeDegrees);

            if (distance < nearestDistance)
            {
                nearest = person;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private static AnimalState MoveToward(
        AnimalState wolf,
        PersonState target,
        double elapsedDays)
    {
        var latitudeDelta =
            target.LatitudeDegrees -
            wolf.LatitudeDegrees;

        var longitudeDelta =
            SignedLongitudeDelta(
                target.LongitudeDegrees,
                wolf.LongitudeDegrees);

        var distance =
            Math.Sqrt(
                latitudeDelta * latitudeDelta +
                longitudeDelta * longitudeDelta);

        if (distance <= 0)
        {
            return wolf;
        }

        var travelDistance =
            Math.Min(
                distance,
                TravelDegreesPerDay * elapsedDays);

        var fraction =
            travelDistance / distance;

        return wolf.WithState(
            Math.Clamp(
                wolf.LatitudeDegrees +
                latitudeDelta * fraction,
                -89.999,
                89.999),
            WrapLongitude(
                wolf.LongitudeDegrees +
                longitudeDelta * fraction),
            wolf.EnergyReserve,
            wolf.Health,
            AnimalActivity.Hunting);
    }

    private static double DistanceDegrees(
        double sourceLatitude,
        double sourceLongitude,
        double targetLatitude,
        double targetLongitude)
    {
        var latitudeDelta =
            targetLatitude - sourceLatitude;

        var longitudeDelta =
            SignedLongitudeDelta(
                targetLongitude,
                sourceLongitude);

        return Math.Sqrt(
            latitudeDelta * latitudeDelta +
            longitudeDelta * longitudeDelta);
    }

    private static double SignedLongitudeDelta(
        double target,
        double source)
    {
        return
            ((target - source + 540) % 360)
            - 180;
    }

    private static double WrapLongitude(
        double longitude)
    {
        return
            ((longitude + 540) % 360)
            - 180;
    }
}
