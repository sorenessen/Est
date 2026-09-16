using Est.Simulation.Causality;
using Est.Simulation.Grazers;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Worlds;

namespace Est.Simulation.Animals;

public sealed class WolfPredatorSystem : ICausalSystem
{
    private const double TravelDegreesPerDay = 0.75;
    private const double HumanFleeDegreesPerDay = 0.25;

    private const double HumanDetectionRadiusDegrees = 1;
    private const double GrazerDetectionRadiusDegrees = 2;
    private const double HumanDefenseRadiusDegrees = 0.35;
    private const double PackSupportRadiusDegrees = 0.75;
    private const double AttackRadiusDegrees = 0.12;

    private const double HuntEnergyThreshold = 0.55;

    // Humans are emergency/high-risk targets, not normal prey.
    private const double HumanAttackEnergyThreshold = 0.12;

    private const double EnergyUsePerDay = 0.08;
    private const double EnergyPerGrazerKill = 0.45;
    private const double EnergyPerHumanKill = 0.65;

    // Starvation remains gradual when neither normal grazer prey
    // nor emergency human prey is available. A wolf can survive
    // for a while after exhausting stored energy, but not indefinitely.
    private const double StarvationHealthLossPerDay =
        1d / 21d;

    private const double HumanInjuryHealthLoss = 0.20;
    private const double WolfInjuryHealthLoss = 0.35;

    private const double HumanDefenderMinimumAgeYears = 14;
    private const double HumanDefenderMinimumHealth = 0.25;

    // Even a starving wolf does not roll a human attack every day.
    private const long HumanEncounterCycleSeconds =
        14 * 86_400;

    private const long MaximumIntegrationStepSeconds =
        86_400;

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
            throw new PlanetNotFoundException(
                _planetId);
        }

        var population =
            world.Population
                .Where(
                    person =>
                        person.PlanetId == _planetId)
                .OrderBy(
                    person => person.Id.Value)
                .ToList();

        var animals =
            world.Animals
                .Where(
                    animal =>
                        animal.PlanetId == _planetId)
                .OrderBy(
                    animal => animal.Id.Value)
                .ToList();

        var grazerCohorts =
            world.GrazerCohorts
                .Where(
                    cohort =>
                        cohort.PlanetId == _planetId)
                .OrderBy(
                    cohort =>
                        cohort.Id.Value)
                .ToList();

        var wolfAttacks = 0;
        var successfulKills = 0;
        var failedAttacks = 0;
        var predationDeaths = 0;
        var chaseSteps = 0;
        var fleeSteps = 0;

        var humanEncounterOpportunities = 0;
        var avoidedEncounters = 0;
        var repelledEncounters = 0;
        var humanInjuries = 0;
        var wolfInjuries = 0;
        var wolfDeaths = 0;
        var wolfStarvationDeaths = 0;
        var packEncounters = 0;

        var grazerHunts = 0;
        var grazerKills = 0;
        var grazerChaseSteps = 0;
        var grazerPackHunts = 0;

        double? lastAttackLatitude = null;
        double? lastAttackLongitude = null;

        var remainingSeconds =
            elapsedSeconds;

        while (remainingSeconds > 0 &&
               animals.Any(
                   animal =>
                       animal.Species ==
                       AnimalSpecies.Wolf &&
                       animal.Health > 0))
        {
            var stepSeconds =
                Math.Min(
                    MaximumIntegrationStepSeconds,
                    remainingSeconds);

            var elapsedDays =
                stepSeconds / 86_400d;

            var stepStartTimeSeconds =
                checked(
                    world.CurrentTime.TotalSeconds +
                    (elapsedSeconds -
                     remainingSeconds));

            var stepEndTimeSeconds =
                checked(
                    stepStartTimeSeconds +
                    stepSeconds);

            wolfStarvationDeaths +=
                ConsumeWolfEnergy(
                    animals,
                    elapsedDays);

            var processedWolves =
                new HashSet<AnimalId>();

            for (var index = 0;
                 index < animals.Count;
                 index++)
            {
                var wolf =
                    animals[index];

                if (wolf.Species !=
                        AnimalSpecies.Wolf ||
                    wolf.Health <= 0 ||
                    processedWolves.Contains(
                        wolf.Id))
                {
                    continue;
                }

                if (wolf.EnergyReserve <=
                    HuntEnergyThreshold)
                {
                    var grazerTarget =
                        FindNearestDetectedGrazer(
                            wolf,
                            grazerCohorts);

                    if (grazerTarget is not null)
                    {
                        var huntingPack =
                            FindNearbyHuntingWolves(
                                wolf,
                                animals);

                        foreach (var packWolf in
                                 huntingPack)
                        {
                            processedWolves.Add(
                                packWolf.Id);
                        }

                        if (huntingPack.Count > 1)
                        {
                            grazerPackHunts++;
                        }

                        var grazerDistance =
                            DistanceDegrees(
                                wolf.LatitudeDegrees,
                                wolf.LongitudeDegrees,
                                grazerTarget
                                    .LatitudeDegrees,
                                grazerTarget
                                    .LongitudeDegrees);

                        if (grazerDistance >
                            AttackRadiusDegrees)
                        {
                            PursueGrazerWithPack(
                                animals,
                                huntingPack,
                                grazerTarget,
                                elapsedDays);

                            grazerChaseSteps++;

                            var leadWolf =
                                FindAnimal(
                                    animals,
                                    wolf.Id);

                            if (leadWolf is null)
                            {
                                continue;
                            }

                            grazerDistance =
                                DistanceDegrees(
                                    leadWolf
                                        .LatitudeDegrees,
                                    leadWolf
                                        .LongitudeDegrees,
                                    grazerTarget
                                        .LatitudeDegrees,
                                    grazerTarget
                                        .LongitudeDegrees);

                            if (grazerDistance >
                                AttackRadiusDegrees)
                            {
                                continue;
                            }
                        }

                        grazerHunts++;

                        if (ConsumeOneGrazer(
                                grazerCohorts,
                                grazerTarget.Id))
                        {
                            grazerKills++;

                            FeedPack(
                                animals,
                                huntingPack,
                                EnergyPerGrazerKill);
                        }

                        continue;
                    }
                }

                if (wolf.EnergyReserve >
                    HumanAttackEnergyThreshold)
                {
                    animals[index] =
                        wolf.WithState(
                            wolf.LatitudeDegrees,
                            wolf.LongitudeDegrees,
                            wolf.EnergyReserve,
                            wolf.Health,
                            wolf.EnergyReserve >
                                HuntEnergyThreshold
                                ? AnimalActivity.Idle
                                : AnimalActivity.Hunting);

                    continue;
                }

                if (!IsHumanEncounterOpportunity(
                        wolf.Id,
                        stepStartTimeSeconds,
                        stepEndTimeSeconds))
                {
                    animals[index] =
                        wolf.WithState(
                            wolf.LatitudeDegrees,
                            wolf.LongitudeDegrees,
                            wolf.EnergyReserve,
                            wolf.Health,
                            AnimalActivity.Hunting);

                    continue;
                }

                humanEncounterOpportunities++;

                var target =
                    FindNearestDetectedPerson(
                        wolf,
                        population);

                if (target is null)
                {
                    animals[index] =
                        wolf.WithState(
                            wolf.LatitudeDegrees,
                            wolf.LongitudeDegrees,
                            wolf.EnergyReserve,
                            wolf.Health,
                            AnimalActivity.Hunting);

                    continue;
                }

                var pack =
                    FindNearbyDesperateWolves(
                        wolf,
                        animals);

                foreach (var packWolf in pack)
                {
                    processedWolves.Add(
                        packWolf.Id);
                }

                if (pack.Count > 1)
                {
                    packEncounters++;
                }

                var defenders =
                    FindHumanDefenders(
                        target,
                        population,
                        stepEndTimeSeconds);

                var distance =
                    DistanceDegrees(
                        wolf.LatitudeDegrees,
                        wolf.LongitudeDegrees,
                        target.LatitudeDegrees,
                        target.LongitudeDegrees);

                if (!ShouldEscalateHumanEncounter(
                        wolf,
                        target,
                        pack,
                        defenders,
                        distance,
                        stepEndTimeSeconds))
                {
                    avoidedEncounters++;

                    RetreatPackFromHuman(
                        animals,
                        pack,
                        target,
                        elapsedDays);

                    continue;
                }

                if (distance >
                    AttackRadiusDegrees)
                {
                    var fleeingTarget =
                        MovePersonAway(
                            target,
                            wolf,
                            elapsedDays);

                    ReplacePerson(
                        population,
                        fleeingTarget);

                    target = fleeingTarget;
                    fleeSteps++;

                    PursueWithPack(
                        animals,
                        pack,
                        target,
                        elapsedDays);

                    chaseSteps++;

                    var leadWolf =
                        FindAnimal(
                            animals,
                            wolf.Id);

                    if (leadWolf is null)
                    {
                        continue;
                    }

                    distance =
                        DistanceDegrees(
                            leadWolf.LatitudeDegrees,
                            leadWolf.LongitudeDegrees,
                            target.LatitudeDegrees,
                            target.LongitudeDegrees);

                    if (distance >
                        AttackRadiusDegrees)
                    {
                        continue;
                    }
                }

                wolfAttacks++;

                lastAttackLatitude =
                    target.LatitudeDegrees;

                lastAttackLongitude =
                    target.LongitudeDegrees;

                var wolfStrength =
                    CalculateWolfStrength(
                        pack);

                var humanStrength =
                    CalculateHumanStrength(
                        defenders,
                        target);

                var totalStrength =
                    wolfStrength +
                    humanStrength;

                var wolfShare =
                    totalStrength <= 0
                        ? 0.5
                        : wolfStrength /
                          totalStrength;

                var humanDeathProbability =
                    Math.Clamp(
                        0.02 +
                        0.18 *
                        wolfShare *
                        wolfShare,
                        0.02,
                        0.20);

                var humanDied =
                    DeterministicUnit(
                        stepEndTimeSeconds,
                        wolf.Id,
                        target.Id,
                        salt: 21) <
                    humanDeathProbability;

                var humanInjuryProbability =
                    Math.Clamp(
                        0.12 +
                        0.28 * wolfShare,
                        0.12,
                        0.40);

                var humanInjured =
                    !humanDied &&
                    DeterministicUnit(
                        stepEndTimeSeconds,
                        wolf.Id,
                        target.Id,
                        salt: 22) <
                    humanInjuryProbability;

                // Close contact with an adult human is dangerous
                // to a lone wolf even when the person loses.
                var wolfInjuryProbability =
                    Math.Clamp(
                        0.20 +
                        0.55 *
                        (1 - wolfShare),
                        0.20,
                        0.75);

                var wolfInjured =
                    defenders.Count > 0 &&
                    (
                        defenders.Count >=
                            pack.Count ||
                        DeterministicUnit(
                            stepEndTimeSeconds,
                            wolf.Id,
                            target.Id,
                            salt: 23) <
                        wolfInjuryProbability
                    );

                if (humanDied)
                {
                    successfulKills++;
                    predationDeaths++;

                    population.RemoveAll(
                        person =>
                            person.Id ==
                            target.Id);
                }
                else
                {
                    failedAttacks++;
                    repelledEncounters++;

                    var survivingTarget =
                        target;

                    if (humanInjured)
                    {
                        humanInjuries++;

                        survivingTarget =
                            survivingTarget
                                .WithSurvivalState(
                                    survivingTarget
                                        .Needs
                                        .WithHealth(
                                            Math.Max(
                                                0,
                                                survivingTarget
                                                    .Needs
                                                    .Health -
                                                HumanInjuryHealthLoss)),
                                    PersonActivity.Fleeing);
                    }

                    survivingTarget =
                        MovePersonAway(
                            survivingTarget,
                            wolf,
                            elapsedDays);

                    ReplacePerson(
                        population,
                        survivingTarget);
                }

                if (wolfInjured)
                {
                    wolfInjuries++;

                    var currentWolf =
                        FindAnimal(
                            animals,
                            wolf.Id);

                    if (currentWolf is not null)
                    {
                        var injuredHealth =
                            Math.Max(
                                0,
                                currentWolf.Health -
                                WolfInjuryHealthLoss);

                        var wolfDeathProbability =
                            Math.Clamp(
                                0.05 +
                                0.20 *
                                (1 - wolfShare),
                                0.05,
                                0.25);

                        if (injuredHealth <= 0 ||
                            DeterministicUnit(
                                stepEndTimeSeconds,
                                wolf.Id,
                                target.Id,
                                salt: 24) <
                            wolfDeathProbability)
                        {
                            injuredHealth = 0;
                            wolfDeaths++;
                        }

                        ReplaceAnimal(
                            animals,
                            currentWolf.WithState(
                                currentWolf
                                    .LatitudeDegrees,
                                currentWolf
                                    .LongitudeDegrees,
                                currentWolf
                                    .EnergyReserve,
                                injuredHealth,
                                injuredHealth <= 0
                                    ? AnimalActivity.Idle
                                    : AnimalActivity.Traveling));
                    }
                }

                if (humanDied)
                {
                    FeedPack(
                        animals,
                        pack,
                        EnergyPerHumanKill);
                }
                else
                {
                    RetreatPackFromHuman(
                        animals,
                        pack,
                        target,
                        elapsedDays);
                }
            }

            animals.RemoveAll(
                animal =>
                    animal.Health <= 0);

            remainingSeconds -=
                stepSeconds;
        }

        var metrics =
            new Dictionary<string, double>
            {
                ["wolfAttacks"] =
                    wolfAttacks,
                ["successfulKills"] =
                    successfulKills,
                ["failedAttacks"] =
                    failedAttacks,
                ["predationDeaths"] =
                    predationDeaths,
                ["chaseSteps"] =
                    chaseSteps,
                ["fleeSteps"] =
                    fleeSteps,
                ["humanEncounterOpportunities"] =
                    humanEncounterOpportunities,
                ["avoidedEncounters"] =
                    avoidedEncounters,
                ["repelledEncounters"] =
                    repelledEncounters,
                ["humanInjuries"] =
                    humanInjuries,
                ["wolfInjuries"] =
                    wolfInjuries,
                ["wolfDeaths"] =
                    wolfDeaths,
                ["wolfStarvationDeaths"] =
                    wolfStarvationDeaths,
                ["packEncounters"] =
                    packEncounters,
                ["grazerHunts"] =
                    grazerHunts,
                ["grazerKills"] =
                    grazerKills,
                ["grazerChaseSteps"] =
                    grazerChaseSteps,
                ["grazerPackHunts"] =
                    grazerPackHunts,
                ["grazerMembers"] =
                    grazerCohorts.Sum(
                        cohort =>
                            cohort.MemberCount),
                ["wolves"] =
                    animals.Count(
                        animal =>
                            animal.Species ==
                            AnimalSpecies.Wolf),
                ["survivors"] =
                    population.Count
            };

        if (lastAttackLatitude is not null &&
            lastAttackLongitude is not null)
        {
            metrics["attackLatitude"] =
                lastAttackLatitude.Value;

            metrics["attackLongitude"] =
                lastAttackLongitude.Value;
        }

        var summary =
            predationDeaths > 0
                ? "A wolf-human encounter caused a human death."
                : grazerKills > 0
                    ? "Wolves killed terrestrial grazer prey."
                    : wolfDeaths > 0
                        ? "Humans killed a wolf during a defensive encounter."
                        : wolfStarvationDeaths > 0
                            ? "A wolf died from starvation."
                            : wolfAttacks > 0
                                ? "Humans survived and repelled a wolf attack."
                                : avoidedEncounters > 0
                                    ? "Wolves avoided a risky human encounter."
                                    : "Wolf activity changed.";

        return new SimulationChange(
            new ReplacePlanetPredatorStateOperation(
                _planetId,
                population,
                animals,
                grazerCohorts),
            "predation",
            summary,
            _planetId,
            elapsedSeconds,
            metrics);
    }

    private static int ConsumeWolfEnergy(
        List<AnimalState> animals,
        double elapsedDays)
    {
        var starvationDeaths = 0;

        for (var index = 0;
             index < animals.Count;
             index++)
        {
            var animal =
                animals[index];

            if (animal.Species !=
                    AnimalSpecies.Wolf ||
                animal.Health <= 0)
            {
                continue;
            }

            var requiredEnergy =
                EnergyUsePerDay *
                elapsedDays;

            var remainingEnergy =
                Math.Max(
                    0,
                    animal.EnergyReserve -
                    requiredEnergy);

            var starvationDays =
                requiredEnergy <=
                    animal.EnergyReserve
                    ? 0
                    : (requiredEnergy -
                       animal.EnergyReserve) /
                      EnergyUsePerDay;

            var remainingHealth =
                Math.Max(
                    0,
                    animal.Health -
                    starvationDays *
                    StarvationHealthLossPerDay);

            if (animal.Health > 0 &&
                remainingHealth <= 0)
            {
                starvationDeaths++;
            }

            animals[index] =
                animal.WithState(
                    animal.LatitudeDegrees,
                    animal.LongitudeDegrees,
                    remainingEnergy,
                    remainingHealth,
                    remainingHealth <= 0
                        ? AnimalActivity.Idle
                        : animal.Activity);
        }

        return starvationDeaths;
    }

    private static PersonState?
        FindNearestDetectedPerson(
            AnimalState wolf,
            IEnumerable<PersonState> population)
    {
        PersonState? nearest = null;

        var nearestDistance =
            HumanDetectionRadiusDegrees;

        foreach (var person in population)
        {
            var distance =
                DistanceDegrees(
                    wolf.LatitudeDegrees,
                    wolf.LongitudeDegrees,
                    person.LatitudeDegrees,
                    person.LongitudeDegrees);

            if (distance >
                nearestDistance)
            {
                continue;
            }

            nearest =
                person;

            nearestDistance =
                distance;
        }

        return nearest;
    }

    private static GrazerCohortState?
        FindNearestDetectedGrazer(
            AnimalState wolf,
            IEnumerable<GrazerCohortState>
                grazerCohorts)
    {
        GrazerCohortState? nearest = null;

        var nearestDistance =
            GrazerDetectionRadiusDegrees;

        foreach (var cohort in grazerCohorts)
        {
            var distance =
                DistanceDegrees(
                    wolf.LatitudeDegrees,
                    wolf.LongitudeDegrees,
                    cohort.LatitudeDegrees,
                    cohort.LongitudeDegrees);

            if (distance >=
                nearestDistance)
            {
                continue;
            }

            nearest =
                cohort;

            nearestDistance =
                distance;
        }

        return nearest;
    }

    private static List<AnimalState>
        FindNearbyHuntingWolves(
            AnimalState wolf,
            IEnumerable<AnimalState> animals)
    {
        return animals
            .Where(
                candidate =>
                    candidate.Species ==
                        AnimalSpecies.Wolf &&
                    candidate.Health > 0 &&
                    candidate.EnergyReserve <=
                        HuntEnergyThreshold &&
                    DistanceDegrees(
                        wolf.LatitudeDegrees,
                        wolf.LongitudeDegrees,
                        candidate.LatitudeDegrees,
                        candidate.LongitudeDegrees) <=
                        PackSupportRadiusDegrees)
            .OrderBy(
                candidate =>
                    candidate.Id.Value)
            .ToList();
    }

    private static List<AnimalState>
        FindNearbyDesperateWolves(
            AnimalState wolf,
            IEnumerable<AnimalState> animals)
    {
        return animals
            .Where(
                candidate =>
                    candidate.Species ==
                        AnimalSpecies.Wolf &&
                    candidate.Health > 0 &&
                    candidate.EnergyReserve <=
                        HumanAttackEnergyThreshold &&
                    DistanceDegrees(
                        wolf.LatitudeDegrees,
                        wolf.LongitudeDegrees,
                        candidate.LatitudeDegrees,
                        candidate.LongitudeDegrees) <=
                        PackSupportRadiusDegrees)
            .OrderBy(
                candidate =>
                    candidate.Id.Value)
            .ToList();
    }

    private static List<PersonState>
        FindHumanDefenders(
            PersonState target,
            IEnumerable<PersonState> population,
            long currentTimeSeconds)
    {
        return population
            .Where(
                person =>
                    person.Needs.Health >=
                        HumanDefenderMinimumHealth &&
                    person.AgeYears(
                        currentTimeSeconds) >=
                        HumanDefenderMinimumAgeYears &&
                    DistanceDegrees(
                        target.LatitudeDegrees,
                        target.LongitudeDegrees,
                        person.LatitudeDegrees,
                        person.LongitudeDegrees) <=
                        HumanDefenseRadiusDegrees)
            .OrderBy(
                person =>
                    person.Id.Value)
            .ToList();
    }

    private static bool
        ShouldEscalateHumanEncounter(
            AnimalState wolf,
            PersonState target,
            IReadOnlyList<AnimalState> pack,
            IReadOnlyList<PersonState> defenders,
            double distance,
            long encounterTimeSeconds)
    {
        var wolfCount =
            Math.Max(
                1,
                pack.Count);

        var defenderCount =
            defenders.Count;

        // Two nearby capable humans are enough to make a
        // lone wolf avoid the encounter.
        if (defenderCount >=
            wolfCount * 2)
        {
            return false;
        }

        // A coordinated numerical wolf advantage can force
        // an encounter against an isolated human.
        if (wolfCount >=
                Math.Max(
                    1,
                    defenderCount) * 2)
        {
            return true;
        }

        // A literally starving wolf already at contact
        // distance may take a desperate risk.
        if (wolf.EnergyReserve <= 0.01 &&
            distance <= AttackRadiusDegrees)
        {
            return true;
        }

        var wolfStrength =
            CalculateWolfStrength(
                pack);

        var humanStrength =
            CalculateHumanStrength(
                defenders,
                target);

        var totalStrength =
            wolfStrength +
            humanStrength;

        var wolfShare =
            totalStrength <= 0
                ? 0.5
                : wolfStrength /
                  totalStrength;

        var hunger =
            1 -
            Math.Clamp(
                wolf.EnergyReserve,
                0,
                1);

        var escalationProbability =
            Math.Clamp(
                0.02 +
                0.10 * wolfShare +
                0.06 * hunger,
                0.02,
                0.18);

        return
            DeterministicUnit(
                encounterTimeSeconds,
                wolf.Id,
                target.Id,
                salt: 11) <
            escalationProbability;
    }

    private static double
        CalculateWolfStrength(
            IReadOnlyList<AnimalState> pack)
    {
        if (pack.Count == 0)
        {
            return 0.1;
        }

        return pack.Sum(
            wolf =>
                Math.Max(
                    0.1,
                    wolf.Health) *
                (
                    0.8 +
                    0.4 *
                    (1 -
                     wolf.EnergyReserve)
                ));
    }

    private static double
        CalculateHumanStrength(
            IReadOnlyList<PersonState> defenders,
            PersonState target)
    {
        if (defenders.Count == 0)
        {
            return
                Math.Max(
                    0.1,
                    target.Needs.Health *
                    0.35);
        }

        return defenders.Sum(
            person =>
                Math.Max(
                    0.1,
                    person.Needs.Health) *
                (
                    0.75 +
                    0.25 *
                    person.Needs.EnergyReserve
                ));
    }

    private static bool
        IsHumanEncounterOpportunity(
            AnimalId wolfId,
            long startTimeSeconds,
            long endTimeSeconds)
    {
        if (endTimeSeconds <=
            startTimeSeconds)
        {
            return false;
        }

        var phaseSeconds =
            GetWolfEncounterPhaseSeconds(
                wolfId);

        long opportunityTimeSeconds;

        if (startTimeSeconds <
            phaseSeconds)
        {
            opportunityTimeSeconds =
                phaseSeconds;
        }
        else
        {
            var completedCycles =
                (startTimeSeconds -
                 phaseSeconds) /
                HumanEncounterCycleSeconds;

            opportunityTimeSeconds =
                checked(
                    phaseSeconds +
                    checked(
                        (completedCycles + 1) *
                        HumanEncounterCycleSeconds));
        }

        return
            opportunityTimeSeconds <=
            endTimeSeconds;
    }

    private static long
        GetWolfEncounterPhaseSeconds(
            AnimalId wolfId)
    {
        const ulong offsetBasis =
            14_695_981_039_346_656_037UL;

        const ulong prime =
            1_099_511_628_211UL;

        var hash =
            offsetBasis;

        Span<byte> bytes =
            stackalloc byte[16];

        wolfId.Value.TryWriteBytes(
            bytes);

        foreach (var value in bytes)
        {
            hash =
                (hash ^ value) *
                prime;
        }

        return
            (long)(
                hash %
                (ulong)
                    HumanEncounterCycleSeconds);
    }

    private static double DeterministicUnit(
        long timeSeconds,
        AnimalId wolfId,
        PersonId personId,
        byte salt)
    {
        const ulong offsetBasis =
            14_695_981_039_346_656_037UL;

        var hash =
            offsetBasis;

        static ulong Mix(
            ulong current,
            byte value)
        {
            const ulong localPrime =
                1_099_511_628_211UL;

            return
                (current ^ value) *
                localPrime;
        }

        hash =
            Mix(
                hash,
                salt);

        foreach (var value in
                 BitConverter.GetBytes(
                     timeSeconds))
        {
            hash =
                Mix(
                    hash,
                    value);
        }

        Span<byte> wolfBytes =
            stackalloc byte[16];

        wolfId.Value.TryWriteBytes(
            wolfBytes);

        foreach (var value in wolfBytes)
        {
            hash =
                Mix(
                    hash,
                    value);
        }

        Span<byte> personBytes =
            stackalloc byte[16];

        personId.Value.TryWriteBytes(
            personBytes);

        foreach (var value in personBytes)
        {
            hash =
                Mix(
                    hash,
                    value);
        }

        return
            (hash >> 11) *
            (1d / (1UL << 53));
    }

    private static void PursueGrazerWithPack(
        List<AnimalState> animals,
        IEnumerable<AnimalState> pack,
        GrazerCohortState target,
        double elapsedDays)
    {
        foreach (var member in pack)
        {
            var current =
                FindAnimal(
                    animals,
                    member.Id);

            if (current is null ||
                current.Health <= 0)
            {
                continue;
            }

            var moved =
                MoveTowardGrazer(
                    current,
                    target,
                    elapsedDays);

            ReplaceAnimal(
                animals,
                moved.WithState(
                    moved.LatitudeDegrees,
                    moved.LongitudeDegrees,
                    current.EnergyReserve,
                    current.Health,
                    AnimalActivity.Traveling));
        }
    }

    private static void PursueWithPack(
        List<AnimalState> animals,
        IEnumerable<AnimalState> pack,
        PersonState target,
        double elapsedDays)
    {
        foreach (var member in pack)
        {
            var current =
                FindAnimal(
                    animals,
                    member.Id);

            if (current is null ||
                current.Health <= 0)
            {
                continue;
            }

            var moved =
                MoveToward(
                    current,
                    target,
                    elapsedDays);

            ReplaceAnimal(
                animals,
                moved.WithState(
                    moved.LatitudeDegrees,
                    moved.LongitudeDegrees,
                    current.EnergyReserve,
                    current.Health,
                    AnimalActivity.Traveling));
        }
    }

    private static void RetreatPackFromHuman(
        List<AnimalState> animals,
        IEnumerable<AnimalState> pack,
        PersonState target,
        double elapsedDays)
    {
        foreach (var member in pack)
        {
            var current =
                FindAnimal(
                    animals,
                    member.Id);

            if (current is null ||
                current.Health <= 0)
            {
                continue;
            }

            var moved =
                MoveWolfAway(
                    current,
                    target,
                    elapsedDays);

            ReplaceAnimal(
                animals,
                moved.WithState(
                    moved.LatitudeDegrees,
                    moved.LongitudeDegrees,
                    current.EnergyReserve,
                    current.Health,
                    AnimalActivity.Traveling));
        }
    }

    private static void FeedPack(
        List<AnimalState> animals,
        IReadOnlyList<AnimalState> pack,
        double energyFromKill)
    {
        if (pack.Count == 0)
        {
            return;
        }

        var energyShare =
            energyFromKill /
            pack.Count;

        foreach (var member in pack)
        {
            var current =
                FindAnimal(
                    animals,
                    member.Id);

            if (current is null ||
                current.Health <= 0)
            {
                continue;
            }

            ReplaceAnimal(
                animals,
                current.WithState(
                    current.LatitudeDegrees,
                    current.LongitudeDegrees,
                    Math.Min(
                        1,
                        current.EnergyReserve +
                        energyShare),
                    current.Health,
                    AnimalActivity.Eating));
        }
    }

    private static bool ConsumeOneGrazer(
        List<GrazerCohortState> grazerCohorts,
        GrazerCohortId id)
    {
        var index =
            grazerCohorts.FindIndex(
                cohort =>
                    cohort.Id == id);

        if (index < 0)
        {
            return false;
        }

        var cohort =
            grazerCohorts[index];

        if (cohort.MemberCount == 1)
        {
            grazerCohorts.RemoveAt(
                index);

            return true;
        }

        grazerCohorts[index] =
            cohort.WithSurvivalState(
                cohort.MemberCount - 1,
                cohort.LatitudeDegrees,
                cohort.LongitudeDegrees);

        return true;
    }

    private static AnimalState?
        FindAnimal(
            IEnumerable<AnimalState> animals,
            AnimalId id)
    {
        return animals.FirstOrDefault(
            animal =>
                animal.Id == id);
    }

    private static void ReplaceAnimal(
        List<AnimalState> animals,
        AnimalState replacement)
    {
        var index =
            animals.FindIndex(
                animal =>
                    animal.Id ==
                    replacement.Id);

        if (index >= 0)
        {
            animals[index] =
                replacement;
        }
    }

    private static void ReplacePerson(
        List<PersonState> population,
        PersonState replacement)
    {
        var index =
            population.FindIndex(
                person =>
                    person.Id ==
                    replacement.Id);

        if (index >= 0)
        {
            population[index] =
                replacement;
        }
    }

    private static PersonState MovePersonAway(
        PersonState person,
        AnimalState wolf,
        double elapsedDays)
    {
        var latitudeDelta =
            person.LatitudeDegrees -
            wolf.LatitudeDegrees;

        var longitudeDelta =
            SignedLongitudeDelta(
                person.LongitudeDegrees,
                wolf.LongitudeDegrees);

        var distance =
            Math.Sqrt(
                latitudeDelta *
                latitudeDelta +
                longitudeDelta *
                longitudeDelta);

        if (distance <= 0)
        {
            latitudeDelta = 1;
            longitudeDelta = 0;
            distance = 1;
        }

        var fleeDistance =
            HumanFleeDegreesPerDay *
            elapsedDays;

        var fraction =
            fleeDistance /
            distance;

        var latitude =
            Math.Clamp(
                person.LatitudeDegrees +
                latitudeDelta *
                fraction,
                -89.999,
                89.999);

        var longitude =
            WrapLongitude(
                person.LongitudeDegrees +
                longitudeDelta *
                fraction);

        return person
            .MoveTo(
                latitude,
                longitude)
            .WithSurvivalState(
                person.Needs,
                PersonActivity.Fleeing);
    }

    private static AnimalState MoveWolfAway(
        AnimalState wolf,
        PersonState human,
        double elapsedDays)
    {
        var latitudeDelta =
            wolf.LatitudeDegrees -
            human.LatitudeDegrees;

        var longitudeDelta =
            SignedLongitudeDelta(
                wolf.LongitudeDegrees,
                human.LongitudeDegrees);

        var distance =
            Math.Sqrt(
                latitudeDelta *
                latitudeDelta +
                longitudeDelta *
                longitudeDelta);

        if (distance <= 0)
        {
            latitudeDelta = 1;
            longitudeDelta = 0;
            distance = 1;
        }

        var retreatDistance =
            TravelDegreesPerDay *
            elapsedDays;

        var fraction =
            retreatDistance /
            distance;

        return wolf.WithState(
            Math.Clamp(
                wolf.LatitudeDegrees +
                latitudeDelta *
                fraction,
                -89.999,
                89.999),
            WrapLongitude(
                wolf.LongitudeDegrees +
                longitudeDelta *
                fraction),
            wolf.EnergyReserve,
            wolf.Health,
            AnimalActivity.Traveling);
    }

    private static AnimalState MoveTowardGrazer(
        AnimalState wolf,
        GrazerCohortState target,
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
                latitudeDelta *
                latitudeDelta +
                longitudeDelta *
                longitudeDelta);

        if (distance <= 0)
        {
            return wolf;
        }

        var travelDistance =
            Math.Min(
                distance,
                TravelDegreesPerDay *
                elapsedDays);

        var fraction =
            travelDistance /
            distance;

        return wolf.WithState(
            Math.Clamp(
                wolf.LatitudeDegrees +
                latitudeDelta *
                fraction,
                -89.999,
                89.999),
            WrapLongitude(
                wolf.LongitudeDegrees +
                longitudeDelta *
                fraction),
            wolf.EnergyReserve,
            wolf.Health,
            AnimalActivity.Hunting);
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
                latitudeDelta *
                latitudeDelta +
                longitudeDelta *
                longitudeDelta);

        if (distance <= 0)
        {
            return wolf;
        }

        var travelDistance =
            Math.Min(
                distance,
                TravelDegreesPerDay *
                elapsedDays);

        var fraction =
            travelDistance /
            distance;

        return wolf.WithState(
            Math.Clamp(
                wolf.LatitudeDegrees +
                latitudeDelta *
                fraction,
                -89.999,
                89.999),
            WrapLongitude(
                wolf.LongitudeDegrees +
                longitudeDelta *
                fraction),
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
            targetLatitude -
            sourceLatitude;

        var longitudeDelta =
            SignedLongitudeDelta(
                targetLongitude,
                sourceLongitude);

        return
            Math.Sqrt(
                latitudeDelta *
                latitudeDelta +
                longitudeDelta *
                longitudeDelta);
    }

    private static double SignedLongitudeDelta(
        double target,
        double source)
    {
        return
            ((target -
              source +
              540) %
             360) -
            180;
    }

    private static double WrapLongitude(
        double longitude)
    {
        return
            ((longitude + 540) % 360) -
            180;
    }
}
