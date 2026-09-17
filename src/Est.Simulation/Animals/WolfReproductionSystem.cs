using System.Security.Cryptography;
using Est.Simulation.Causality;
using Est.Simulation.Operations;
using Est.Simulation.Organisms;
using Est.Simulation.Planets;
using Est.Simulation.Worlds;

namespace Est.Simulation.Animals;

public sealed class WolfReproductionSystem
    : ICausalSystem
{
    private const double
        MatingDistanceDegrees = 0.75;

    private readonly PlanetId _planetId;

    private readonly WolfLifecycleParameters
        _parameters;

    public WolfReproductionSystem(
        PlanetId planetId,
        WolfLifecycleParameters?
            parameters = null)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        _planetId =
            planetId;

        _parameters =
            parameters ??
            new WolfLifecycleParameters();
    }

    public SimulationChange Evaluate(
        WorldState world,
        long elapsedSeconds)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        if (elapsedSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elapsedSeconds));
        }

        if (!world.Planets.Any(
                planet =>
                    planet.Id == _planetId))
        {
            throw new PlanetNotFoundException(
                _planetId);
        }

        var currentTimeSeconds =
            world.CurrentTime.TotalSeconds;

        var endTimeSeconds =
            checked(
                currentTimeSeconds +
                elapsedSeconds);

        var animals =
            world.Animals
                .Where(
                    animal =>
                        animal.PlanetId ==
                        _planetId)
                .OrderBy(
                    animal =>
                        animal.Id.Value)
                .ToList();

        var births =
            new List<AnimalState>();

        var eligibleFemales = 0;
        var matingOpportunities = 0;
        var conceptions = 0;
        var pregnantFemales = 0;
        var noPartnerFound = 0;
        var litters = 0;
        var pregnancyLossesMaterialInsufficient =
            0;

        var newbornBiomassKilograms =
            0d;

        var newbornNitrogenKilograms =
            0d;

        var nursedPups = 0;

        var nursingBiomassKilograms =
            0d;

        var nursingNitrogenKilograms =
            0d;

        for (var index = 0;
             index < animals.Count;
             index++)
        {
            var wolf =
                animals[index];

            if (wolf.Species !=
                    AnimalSpecies.Wolf ||
                wolf.Health <= 0 ||
                wolf.WolfLifecycle is null)
            {
                continue;
            }

            var lifecycle =
                wolf.WolfLifecycle;

            if (lifecycle.Sex !=
                WolfSex.Female)
            {
                continue;
            }

            if (lifecycle.Pregnancy is not null)
            {
                pregnantFemales++;

                var pregnancy =
                    lifecycle.Pregnancy;

                var dueTimeSeconds =
                    checked(
                        pregnancy
                            .ConceptionTimeSeconds +
                        _parameters
                            .GestationSeconds);

                if (dueTimeSeconds >
                    endTimeSeconds)
                {
                    continue;
                }

                var litterSize =
                    DetermineLitterSize(
                        wolf.Id,
                        pregnancy.FatherId,
                        dueTimeSeconds);

                var litterMaterial =
                    _parameters
                        .NewbornMaterial
                        .ForUnits(
                            litterSize);

                if (!CanProvideMaterial(
                        wolf.Material,
                        litterMaterial))
                {
                    animals[index] =
                        wolf.WithWolfLifecycle(
                            lifecycle
                                .WithoutPregnancy());

                    pregnancyLossesMaterialInsufficient++;

                    continue;
                }

                var mother =
                    wolf
                        .WithMaterial(
                            wolf.Material.Subtract(
                                litterMaterial))
                        .WithWolfLifecycle(
                            lifecycle
                                .WithoutPregnancy());

                animals[index] =
                    mother;

                var newbornMaterial =
                    _parameters
                        .NewbornMaterial
                        .ForUnits(1);

                for (var pupIndex = 0;
                     pupIndex < litterSize;
                     pupIndex++)
                {
                    births.Add(
                        CreatePup(
                            mother,
                            pregnancy.FatherId,
                            dueTimeSeconds,
                            pupIndex,
                            newbornMaterial));
                }

                litters++;

                newbornBiomassKilograms +=
                    litterMaterial
                        .LiveBiomassKilograms;

                newbornNitrogenKilograms +=
                    litterMaterial
                        .LiveNitrogenKilograms;

                continue;
            }

            if (!IsEligibleFemaleByEnd(
                    wolf,
                    endTimeSeconds))
            {
                continue;
            }

            eligibleFemales++;

            if (!TryFindConception(
                    wolf,
                    animals,
                    currentTimeSeconds,
                    elapsedSeconds,
                    out var conceptionTimeSeconds,
                    out var father,
                    out var hadEligibleOpportunity))
            {
                if (hadEligibleOpportunity)
                {
                    matingOpportunities++;
                    noPartnerFound++;
                }

                continue;
            }

            matingOpportunities++;

            animals[index] =
                wolf.WithWolfLifecycle(
                    lifecycle.WithPregnancy(
                        new WolfPregnancyState(
                            conceptionTimeSeconds,
                            father!.Id)));

            conceptions++;
        }

        if (births.Count > 0)
        {
            foreach (var pup in births)
            {
                if (animals.Any(
                        animal =>
                            animal.Id ==
                            pup.Id))
                {
                    throw new InvalidOperationException(
                        "Deterministic wolf birth produced a duplicate animal identity.");
                }

                animals.Add(
                    pup);
            }
        }

        ProvisionNursingPups(
            animals,
            endTimeSeconds,
            _parameters,
            out nursedPups,
            out nursingBiomassKilograms,
            out nursingNitrogenKilograms);

        var operation =
            new ReplacePlanetAnimalsOperation(
                _planetId,
                animals);

        return new SimulationChange(
            operation,
            "wolf-reproduction",
            "Wolf mating, conception, gestation, and material-conserving litter births changed planetary animals.",
            _planetId,
            elapsedSeconds,
            new Dictionary<string, double>
            {
                ["eligibleFemales"] =
                    eligibleFemales,
                ["matingOpportunities"] =
                    matingOpportunities,
                ["conceptions"] =
                    conceptions,
                ["pregnantFemales"] =
                    pregnantFemales,
                ["noPartnerFound"] =
                    noPartnerFound,
                ["litters"] =
                    litters,
                ["births"] =
                    births.Count,
                ["pregnancyLossesMaterialInsufficient"] =
                    pregnancyLossesMaterialInsufficient,
                ["newbornBiomassKilograms"] =
                    newbornBiomassKilograms,
                ["newbornNitrogenKilograms"] =
                    newbornNitrogenKilograms,
                ["nursedPups"] =
                    nursedPups,
                ["nursingBiomassKilograms"] =
                    nursingBiomassKilograms,
                ["nursingNitrogenKilograms"] =
                    nursingNitrogenKilograms
            });
    }

    private static void ProvisionNursingPups(
        List<AnimalState> animals,
        long currentTimeSeconds,
        WolfLifecycleParameters parameters,
        out int nursedPups,
        out double nursingBiomassKilograms,
        out double nursingNitrogenKilograms)
    {
        nursedPups = 0;
        nursingBiomassKilograms = 0;
        nursingNitrogenKilograms = 0;

        var dependentPupIds =
            animals
                .Where(
                    animal =>
                        animal.Species ==
                            AnimalSpecies.Wolf &&
                        animal.Health > 0 &&
                        animal.ParentId is not null &&
                        animal.BirthTimeSeconds <=
                            currentTimeSeconds &&
                        currentTimeSeconds -
                            animal.BirthTimeSeconds <=
                            parameters.NursingAgeSeconds)
                .OrderBy(
                    animal =>
                        animal.Id.Value)
                .Select(
                    animal =>
                        animal.Id)
                .ToArray();

        foreach (var pupId in dependentPupIds)
        {
            var pupIndex =
                animals.FindIndex(
                    animal =>
                        animal.Id ==
                        pupId);

            if (pupIndex < 0)
            {
                continue;
            }

            var pup =
                animals[pupIndex];

            if (pup.ParentId is null)
            {
                continue;
            }

            var motherIndex =
                animals.FindIndex(
                    animal =>
                        animal.Id ==
                            pup.ParentId.Value &&
                        animal.Species ==
                            AnimalSpecies.Wolf &&
                        animal.Health > 0 &&
                        animal.WolfLifecycle?.Sex ==
                            WolfSex.Female);

            if (motherIndex < 0)
            {
                continue;
            }

            var mother =
                animals[motherIndex];

            var growth =
                WolfMaterialGrowth
                    .AssimilateTowardAgeTarget(
                        pup,
                        currentTimeSeconds,
                        mother.Material,
                        parameters);

            if (growth
                    .AssimilatedMaterial
                    .IsEmpty)
            {
                continue;
            }

            animals[motherIndex] =
                mother.WithMaterial(
                    growth.RemainingFoodMaterial);

            animals[pupIndex] =
                growth.Wolf.WithState(
                    growth.Wolf.LatitudeDegrees,
                    growth.Wolf.LongitudeDegrees,
                    energyReserve: 1,
                    growth.Wolf.Health,
                    AnimalActivity.Eating);

            nursedPups++;

            nursingBiomassKilograms +=
                growth
                    .AssimilatedMaterial
                    .LiveBiomassKilograms;

            nursingNitrogenKilograms +=
                growth
                    .AssimilatedMaterial
                    .LiveNitrogenKilograms;
        }
    }

    private bool TryFindConception(
        AnimalState mother,
        IReadOnlyList<AnimalState> animals,
        long currentTimeSeconds,
        long elapsedSeconds,
        out long conceptionTimeSeconds,
        out AnimalState? father,
        out bool hadEligibleOpportunity)
    {
        conceptionTimeSeconds =
            0;

        father =
            null;

        hadEligibleOpportunity =
            false;

        if (elapsedSeconds <= 0)
        {
            return false;
        }

        var endTimeSeconds =
            checked(
                currentTimeSeconds +
                elapsedSeconds);

        var opportunityTimeSeconds =
            GetFirstAnnualOpportunity(
                mother.Id,
                currentTimeSeconds);

        while (opportunityTimeSeconds <=
               endTimeSeconds)
        {
            if (IsReproductivelyEligible(
                    mother,
                    WolfSex.Female,
                    opportunityTimeSeconds))
            {
                hadEligibleOpportunity =
                    true;

                var partner =
                    FindNearestEligibleMale(
                        mother,
                        animals,
                        opportunityTimeSeconds);

                if (partner is not null)
                {
                    conceptionTimeSeconds =
                        opportunityTimeSeconds;

                    father =
                        partner;

                    return true;
                }
            }

            if (opportunityTimeSeconds >
                long.MaxValue -
                WolfLifecycleParameters
                    .SecondsPerYear)
            {
                break;
            }

            opportunityTimeSeconds +=
                WolfLifecycleParameters
                    .SecondsPerYear;
        }

        return false;
    }

    private bool IsEligibleFemaleByEnd(
        AnimalState wolf,
        long endTimeSeconds)
    {
        return IsReproductivelyEligible(
            wolf,
            WolfSex.Female,
            endTimeSeconds);
    }

    private bool IsReproductivelyEligible(
        AnimalState wolf,
        WolfSex requiredSex,
        long currentTimeSeconds)
    {
        if (wolf.Species !=
                AnimalSpecies.Wolf ||
            wolf.Health <= 0 ||
            wolf.EnergyReserve <= 0 ||
            wolf.WolfLifecycle is null ||
            wolf.WolfLifecycle.Sex !=
                requiredSex)
        {
            return false;
        }

        var ageSeconds =
            OrganismLifecycleClock.AgeSeconds(
                wolf.BirthTimeSeconds,
                currentTimeSeconds);

        return ageSeconds >=
               _parameters
                   .LifecycleTiming
                   .ReproductiveAgeMinimumSeconds;
    }

    private AnimalState?
        FindNearestEligibleMale(
            AnimalState female,
            IEnumerable<AnimalState> animals,
            long currentTimeSeconds)
    {
        return animals
            .Where(
                candidate =>
                    candidate.Id !=
                        female.Id &&
                    IsReproductivelyEligible(
                        candidate,
                        WolfSex.Male,
                        currentTimeSeconds))
            .Select(
                candidate =>
                    new
                    {
                        Wolf = candidate,
                        Distance =
                            DistanceDegrees(
                                female
                                    .LatitudeDegrees,
                                female
                                    .LongitudeDegrees,
                                candidate
                                    .LatitudeDegrees,
                                candidate
                                    .LongitudeDegrees)
                    })
            .Where(
                candidate =>
                    candidate.Distance <=
                    MatingDistanceDegrees)
            .OrderBy(
                candidate =>
                    candidate.Distance)
            .ThenBy(
                candidate =>
                    candidate.Wolf.Id.Value)
            .Select(
                candidate =>
                    candidate.Wolf)
            .FirstOrDefault();
    }

    private static long
        GetFirstAnnualOpportunity(
            AnimalId motherId,
            long currentTimeSeconds)
    {
        var phaseSeconds =
            (long)(
                HashAnimalId(
                    motherId) %
                (ulong)
                WolfLifecycleParameters
                    .SecondsPerYear);

        if (currentTimeSeconds <
            phaseSeconds)
        {
            return phaseSeconds;
        }

        var completedCycles =
            (currentTimeSeconds -
             phaseSeconds) /
            WolfLifecycleParameters
                .SecondsPerYear;

        return checked(
            phaseSeconds +
            checked(
                (completedCycles + 1) *
                WolfLifecycleParameters
                    .SecondsPerYear));
    }

    private int DetermineLitterSize(
        AnimalId motherId,
        AnimalId fatherId,
        long birthTimeSeconds)
    {
        var range =
            _parameters.MaximumLitterSize -
            _parameters.MinimumLitterSize +
            1;

        var value =
            HashBirth(
                motherId,
                fatherId,
                birthTimeSeconds,
                salt: 0);

        return
            _parameters.MinimumLitterSize +
            (int)(
                value %
                (ulong)range);
    }

    private AnimalState CreatePup(
        AnimalState mother,
        AnimalId fatherId,
        long birthTimeSeconds,
        int litterIndex,
        OrganismMaterialState newbornMaterial)
    {
        var id =
            CreatePupId(
                mother.Id,
                fatherId,
                birthTimeSeconds,
                litterIndex);

        var sex =
            (HashBirth(
                 mother.Id,
                 fatherId,
                 birthTimeSeconds,
                 litterIndex + 1) &
             1UL) == 0
                ? WolfSex.Female
                : WolfSex.Male;

        return new AnimalState(
            id,
            mother.PlanetId,
            AnimalSpecies.Wolf,
            mother.LatitudeDegrees,
            mother.LongitudeDegrees,
            energyReserve: 1,
            health: 1,
            activity:
                AnimalActivity.Idle,
            material:
                newbornMaterial,
            birthTimeSeconds:
                birthTimeSeconds,
            parentId:
                mother.Id,
            wolfLifecycle:
                new WolfLifecycleState(
                    sex));
    }

    private static AnimalId CreatePupId(
        AnimalId motherId,
        AnimalId fatherId,
        long birthTimeSeconds,
        int litterIndex)
    {
        Span<byte> input =
            stackalloc byte[44];

        motherId.Value.TryWriteBytes(
            input[..16]);

        fatherId.Value.TryWriteBytes(
            input.Slice(
                16,
                16));

        BitConverter.TryWriteBytes(
            input.Slice(
                32,
                8),
            birthTimeSeconds);

        BitConverter.TryWriteBytes(
            input.Slice(
                40,
                4),
            litterIndex);

        Span<byte> digest =
            stackalloc byte[32];

        SHA256.HashData(
            input,
            digest);

        return new AnimalId(
            new Guid(
                digest[..16]));
    }

    private static ulong HashAnimalId(
        AnimalId animalId)
    {
        const ulong offsetBasis =
            14_695_981_039_346_656_037UL;

        const ulong prime =
            1_099_511_628_211UL;

        var hash =
            offsetBasis;

        Span<byte> bytes =
            stackalloc byte[16];

        animalId.Value.TryWriteBytes(
            bytes);

        foreach (var value in bytes)
        {
            hash =
                (hash ^ value) *
                prime;
        }

        return hash;
    }

    private static ulong HashBirth(
        AnimalId motherId,
        AnimalId fatherId,
        long birthTimeSeconds,
        int salt)
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

        Span<byte> motherBytes =
            stackalloc byte[16];

        motherId.Value.TryWriteBytes(
            motherBytes);

        foreach (var value in
                 motherBytes)
        {
            hash =
                Mix(
                    hash,
                    value);
        }

        Span<byte> fatherBytes =
            stackalloc byte[16];

        fatherId.Value.TryWriteBytes(
            fatherBytes);

        foreach (var value in
                 fatherBytes)
        {
            hash =
                Mix(
                    hash,
                    value);
        }

        foreach (var value in
                 BitConverter.GetBytes(
                     birthTimeSeconds))
        {
            hash =
                Mix(
                    hash,
                    value);
        }

        foreach (var value in
                 BitConverter.GetBytes(
                     salt))
        {
            hash =
                Mix(
                    hash,
                    value);
        }

        return hash;
    }

    private static bool CanProvideMaterial(
        OrganismMaterialState availableMaterial,
        OrganismMaterialState requiredMaterial)
    {
        return
            availableMaterial
                .LiveBiomassKilograms >=
            requiredMaterial
                .LiveBiomassKilograms &&
            availableMaterial
                .LiveNitrogenKilograms >=
            requiredMaterial
                .LiveNitrogenKilograms;
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
            latitudeDelta *
            latitudeDelta +
            longitudeDelta *
            longitudeDelta);
    }

    private static double
        WrappedLongitudeDelta(
            double from,
            double to)
    {
        var delta =
            to - from;

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
}
