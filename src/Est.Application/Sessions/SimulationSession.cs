using System.Collections.Immutable;
using Est.Simulation.Animals;
using Est.Simulation.Biogeochemistry;
using Est.Simulation.Birds;
using Est.Simulation.Causality;
using Est.Simulation.Climate;
using Est.Simulation.Definitions;
using Est.Simulation.Ecology;
using Est.Simulation.Grazers;
using Est.Simulation.Hydrology;
using Est.Simulation.Invertebrates;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Players;
using Est.Simulation.Population;
using Est.Simulation.Seasons;
using Est.Simulation.Social;
using Est.Simulation.Thermal;
using Est.Simulation.Time;
using Est.Simulation.Timelines;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Application.Sessions;

public sealed class SimulationSession
{
    private const long MaximumCausalCouplingStepSeconds =
        86_400;

    private readonly object _sync = new();
    private readonly SimulationClock _clock = new();
    private readonly IReadOnlyList<ICausalSystem> _causalSystems;
    private SimulationTimeline _timeline;

    public SimulationSession(WorldState initialWorld)
        : this(
            SimulationTimeline.Create(initialWorld),
            SimulationDefinition.Empty)
    {
    }

    public SimulationSession(
        WorldState initialWorld,
        SimulationDefinition definition)
        : this(SimulationTimeline.Create(initialWorld), definition)
    {
    }

    public SimulationSession(SimulationTimeline timeline)
        : this(timeline, SimulationDefinition.Empty)
    {
    }

    public SimulationSession(
        SimulationTimeline timeline,
        SimulationDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        ArgumentNullException.ThrowIfNull(definition);

        definition.ValidateFor(timeline.CurrentWorld);

        _timeline = timeline;
        Definition = definition;

        var seasonalSystems =
            definition.SeasonalModels
                .Select(
                    model =>
                        (ICausalSystem)
                            new DerivedSeasonalSystem(
                                model.PlanetId,
                                new CircularOrbitSeasonalProvider(
                                    model.Parameters)));

        var energyBalanceSystems =
            definition.PlanetaryEnergyBalanceModels
                .Select(
                    model =>
                        (ICausalSystem)
                            new PlanetaryEnergyBalanceSystem(
                                model.PlanetId,
                                model.Parameters));

        var regionalThermalSystems =
            definition.RegionalThermalModels
                .Select(
                    model =>
                        (ICausalSystem)
                            new RegionalThermalSystem(
                                model.PlanetId,
                                model.Parameters,
                                hasConfiguredSeasonalModel:
                                    definition.SeasonalModels.Any(
                                        seasonalModel =>
                                            seasonalModel.PlanetId ==
                                            model.PlanetId)));

        var hydrologySystems =
            definition.HydrologyModels
                .Select(
                    model =>
                        (ICausalSystem)
                            new HydrologySystem(
                                model.PlanetId,
                                model.Parameters,
                                definition.RegionalThermalModels.Any(
                                    regionalModel =>
                                        regionalModel.PlanetId ==
                                        model.PlanetId)
                                    ? HydrologyTemperatureSource
                                        .RegionalSurface
                                    : HydrologyTemperatureSource
                                        .PlanetaryCompatibility));

        var biogeochemistrySystems =
            definition.BiogeochemistryModels
                .Select(
                    model =>
                        (ICausalSystem)
                            new BiogeochemistrySystem(
                                model.PlanetId,
                                model.Parameters));

        var vegetationSystems =
            definition.VegetationModels
                .Select(
                    model =>
                        (ICausalSystem)
                            new VegetationSystem(
                                model.PlanetId,
                                model.Parameters));

        var invertebrateSystems =
            definition.InvertebrateModels
                .Select(
                    model =>
                        (ICausalSystem)
                            new InvertebrateSystem(
                                model.PlanetId,
                                model.Parameters,
                                plantNitrogenKilogramsPerKilogramLiveBiomass:
                                    definition.VegetationModels
                                        .FirstOrDefault(
                                            vegetationModel =>
                                                vegetationModel.PlanetId ==
                                                model.PlanetId)
                                        ?.Parameters
                                        .PlantNitrogenKilogramsPerKilogramLiveBiomass));

        var birdSystems =
            definition.BirdModels
                .Select(
                    model =>
                        (ICausalSystem)
                            new BirdFlockSystem(
                                model.PlanetId,
                                model.Parameters));

        var grazerSystems =
            definition.GrazerModels
                .Select(
                    model =>
                        (ICausalSystem)
                            new GrazerCohortSystem(
                                model.PlanetId,
                                model.Parameters,
                                plantNitrogenKilogramsPerKilogramLiveBiomass:
                                    definition.VegetationModels
                                        .FirstOrDefault(
                                            vegetationModel =>
                                                vegetationModel.PlanetId ==
                                                model.PlanetId)
                                        ?.Parameters
                                        .PlantNitrogenKilogramsPerKilogramLiveBiomass));

        var foragingSystems =
            definition.PopulationModels
                .Where(
                    model =>
                        model.VegetationForaging is not null)
                .Select(
                    model =>
                        (ICausalSystem)
                            new ForagingSystem(
                                model.PlanetId,
                                model.VegetationForaging!,
                                plantNitrogenKilogramsPerKilogramLiveBiomass:
                                    definition.VegetationModels
                                        .FirstOrDefault(
                                            vegetationModel =>
                                                vegetationModel.PlanetId ==
                                                model.PlanetId)
                                        ?.Parameters
                                        .PlantNitrogenKilogramsPerKilogramLiveBiomass,
                                populationParameters:
                                    model.Parameters));

        var predatorSystems =
            timeline.CurrentWorld.Animals
                .Where(
                    animal =>
                        animal.Species ==
                        AnimalSpecies.Wolf)
                .Select(animal => animal.PlanetId)
                .Distinct()
                .Select(
                    planetId =>
                        (ICausalSystem)
                            new WolfPredatorSystem(
                                planetId));

        var wolfReproductionSystems =
            timeline.CurrentWorld.Animals
                .Where(
                    animal =>
                        animal.Species ==
                        AnimalSpecies.Wolf)
                .Select(
                    animal =>
                        animal.PlanetId)
                .Distinct()
                .Select(
                    planetId =>
                        (ICausalSystem)
                            new WolfReproductionSystem(
                                planetId));

        var reproductionSystems =
            definition.PopulationModels
                .Select(
                    model =>
                        (ICausalSystem)
                            new ReproductionSystem(
                                model.PlanetId,
                                model.Parameters));

        var populationSystems =
            definition.PopulationModels
                .Select(
                    model =>
                        (ICausalSystem)
                            new PopulationSystem(
                                model.PlanetId,
                                model.Parameters));

        _causalSystems =
            seasonalSystems
                .Concat(energyBalanceSystems)
                .Concat(regionalThermalSystems)
                .Concat(hydrologySystems)
                .Concat(biogeochemistrySystems)
                .Concat(vegetationSystems)
                .Concat(invertebrateSystems)
                .Concat(birdSystems)
                .Concat(grazerSystems)
                .Concat(foragingSystems)
                .Concat(predatorSystems)
                .Concat(wolfReproductionSystems)
                .Concat(reproductionSystems)
                .Concat(populationSystems)
                .ToArray();
    }

    public SimulationDefinition Definition { get; }

    public SimulationTimeline Timeline
    {
        get
        {
            lock (_sync)
                return _timeline;
        }
    }

    public WorldState CurrentWorld => Timeline.CurrentWorld;

    public bool IsPaused
    {
        get
        {
            lock (_sync)
                return _clock.IsPaused;
        }
    }

    public void Pause()
    {
        lock (_sync)
            _clock.Pause();
    }

    public void Resume()
    {
        lock (_sync)
            _clock.Resume();
    }

    public SimulationTimeline Advance(long seconds)
    {
        if (seconds < 0)
            throw new ArgumentOutOfRangeException(nameof(seconds));

        lock (_sync)
        {
            return AdvanceCore(seconds);
        }
    }

    public SimulationTimeline Tick(long seconds)
    {
        if (seconds < 0)
            throw new ArgumentOutOfRangeException(nameof(seconds));

        lock (_sync)
        {
            if (_clock.IsPaused)
                return _timeline;

            return AdvanceCore(seconds);
        }
    }

    public SimulationTimeline ReplacePlanetEnvironment(
        PlanetId planetId,
        PlanetEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        lock (_sync)
        {
            var operation =
                new ReplacePlanetEnvironmentOperation(
                    planetId,
                    environment);

            var world =
                SimulationOperationExecutor.Apply(
                    _timeline.CurrentWorld,
                    operation);

            var change =
                new SimulationChange(
                    operation,
                    "User intervention",
                    "Replaced planetary environment.",
                    planetId,
                    0);

            _timeline =
                _timeline.RecordStep(
                    new SimulationStepResult(
                        world,
                        change));

            return _timeline;
        }
    }

    public SimulationTimeline OverridePlanetSeasonalState(
        PlanetId planetId,
        SeasonalContext overrideContext)
    {
        ArgumentNullException.ThrowIfNull(
            overrideContext);

        lock (_sync)
        {
            var existingState =
                _timeline.CurrentWorld.SeasonalStates
                    .FirstOrDefault(
                        state =>
                            state.PlanetId ==
                            planetId);

            var seasonalState =
                new PlanetSeasonalState(
                    planetId,
                    SeasonalControlMode.Override,
                    derivedContext:
                        existingState?.DerivedContext,
                    overrideContext:
                        overrideContext);

            var operation =
                new ReplacePlanetSeasonalStateOperation(
                    seasonalState);

            var world =
                SimulationOperationExecutor.Apply(
                    _timeline.CurrentWorld,
                    operation);

            var change =
                new SimulationChange(
                    operation,
                    "User intervention",
                    "Overrode planetary seasonal state.",
                    planetId,
                    0);

            _timeline =
                _timeline.RecordStep(
                    new SimulationStepResult(
                        world,
                        change));

            return _timeline;
        }
    }

    public PersonSocialContactState? GetPersonSocialContact(
        PersonId personId,
        SocialActorIdentity actor)
    {
        if (personId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Person identity cannot be empty.",
                nameof(personId));
        }

        if (actor.Value == Guid.Empty ||
            !Enum.IsDefined(
                typeof(SocialActorKind),
                actor.Kind))
        {
            throw new ArgumentException(
                "Social actor identity must be valid and nonempty.",
                nameof(actor));
        }

        lock (_sync)
        {
            var person =
                _timeline.CurrentWorld.Population
                    .FirstOrDefault(
                        candidate =>
                            candidate.Id == personId)
                ?? throw new InvalidOperationException(
                    "The target person does not exist in this world.");

            return person.SocialState.GetContact(
                actor);
        }
    }

    public ManifestedEsterState? GetManifestedEster(
        EsterId esterId)
    {
        if (esterId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Ester identity cannot be empty.",
                nameof(esterId));
        }

        lock (_sync)
        {
            return _timeline.CurrentWorld
                .ManifestedEsters
                .FirstOrDefault(
                    candidate =>
                        candidate.EsterId ==
                        esterId);
        }
    }

    public SimulationTimeline ManifestEster(
        EsterId esterId,
        PlanetId planetId,
        double latitudeDegrees,
        double longitudeDegrees)
    {
        lock (_sync)
        {
            var operation =
                new ManifestEsterOperation(
                    esterId,
                    planetId,
                    latitudeDegrees,
                    longitudeDegrees);

            var world =
                SimulationOperationExecutor.Apply(
                    _timeline.CurrentWorld,
                    operation);

            var change =
                new SimulationChange(
                    operation,
                    "User intervention",
                    "Manifested Ester in the world.",
                    planetId,
                    0);

            _timeline =
                _timeline.RecordStep(
                    new SimulationStepResult(
                        world,
                        change));

            return _timeline;
        }
    }

    public SimulationTimeline MoveManifestedEster(
        EsterId esterId,
        double latitudeDegrees,
        double longitudeDegrees)
    {
        return MoveManifestedEsterWithEncounters(
            esterId,
            latitudeDegrees,
            longitudeDegrees)
            .Timeline;
    }

    public ManifestedEsterMoveResult
        MoveManifestedEsterWithEncounters(
            EsterId esterId,
            double latitudeDegrees,
            double longitudeDegrees)
    {
        const double personEncounterRadiusMeters =
            2.5;

        lock (_sync)
        {
            var sourceWorld =
                _timeline.CurrentWorld;

            var sourceManifestation =
                sourceWorld.ManifestedEsters
                    .FirstOrDefault(
                        candidate =>
                            candidate.EsterId ==
                            esterId)
                ?? throw new InvalidOperationException(
                    "The Ester is not manifested in this world.");

            var operation =
                new MoveManifestedEsterOperation(
                    esterId,
                    latitudeDegrees,
                    longitudeDegrees);

            var world =
                SimulationOperationExecutor.Apply(
                    sourceWorld,
                    operation);

            var manifested =
                world.ManifestedEsters.Single(
                    candidate =>
                        candidate.EsterId ==
                        esterId);

            var change =
                new SimulationChange(
                    operation,
                    "User intervention",
                    "Moved manifested Ester.",
                    manifested.PlanetId,
                    0);

            _timeline =
                _timeline.RecordStep(
                    new SimulationStepResult(
                        world,
                        change));

            var planet =
                world.Planets.Single(
                    candidate =>
                        candidate.Id ==
                        manifested.PlanetId);

            var actor =
                SocialActorIdentity.ForEster(
                    esterId);

            var encounters =
                ImmutableArray.CreateBuilder<
                    PersonEncounterResult>();

            foreach (
                var person in
                world.Population.Where(
                    candidate =>
                        candidate.PlanetId ==
                        manifested.PlanetId)
            )
            {
                var sourceDistanceMeters =
                    SurfaceDistanceMeters(
                        sourceManifestation
                            .LatitudeDegrees,
                        sourceManifestation
                            .LongitudeDegrees,
                        person.LatitudeDegrees,
                        person.LongitudeDegrees,
                        planet.MeanRadiusMeters);

                var movedDistanceMeters =
                    SurfaceDistanceMeters(
                        manifested.LatitudeDegrees,
                        manifested.LongitudeDegrees,
                        person.LatitudeDegrees,
                        person.LongitudeDegrees,
                        planet.MeanRadiusMeters);

                var enteredEncounterRange =
                    sourceDistanceMeters >
                        personEncounterRadiusMeters &&
                    movedDistanceMeters <=
                        personEncounterRadiusMeters;

                if (!enteredEncounterRange)
                {
                    continue;
                }

                var currentPerson =
                    _timeline.CurrentWorld.Population
                        .Single(
                            candidate =>
                                candidate.Id ==
                                person.Id);

                var contactBefore =
                    currentPerson.SocialState
                        .GetContact(
                            actor);

                var recognizedBeforeEncounter =
                    contactBefore is not null;

                var encounterCountBefore =
                    contactBefore
                        ?.EncounterCount ??
                    0;

                var encounterOperation =
                    new RecordPersonSocialEncounterOperation(
                        person.Id,
                        actor);

                var encounteredWorld =
                    SimulationOperationExecutor.Apply(
                        _timeline.CurrentWorld,
                        encounterOperation);

                var contactAfter =
                    encounteredWorld.Population
                        .Single(
                            candidate =>
                                candidate.Id ==
                                person.Id)
                        .SocialState
                        .GetContact(
                            actor)
                    ?? throw new InvalidOperationException(
                        "Encounter operation did not record the social contact.");

                var encounterChange =
                    new SimulationChange(
                        encounterOperation,
                        "Gameplay encounter",
                        "Manifested Ester entered person encounter range.",
                        manifested.PlanetId,
                        0);

                _timeline =
                    _timeline.RecordStep(
                        new SimulationStepResult(
                            encounteredWorld,
                            encounterChange));

                encounters.Add(
                    new PersonEncounterResult(
                        person.Id,
                        recognizedBeforeEncounter,
                        encounterCountBefore,
                        contactAfter
                            .EncounterCount));
            }

            return new ManifestedEsterMoveResult(
                _timeline,
                manifested,
                encounters.ToImmutable());
        }
    }

    private static double SurfaceDistanceMeters(
        double sourceLatitudeDegrees,
        double sourceLongitudeDegrees,
        double targetLatitudeDegrees,
        double targetLongitudeDegrees,
        double planetRadiusMeters)
    {
        if (!double.IsFinite(planetRadiusMeters) ||
            planetRadiusMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(planetRadiusMeters),
                "Planet radius must be finite and positive.");
        }

        const double degreesToRadians =
            Math.PI / 180d;

        var sourceLatitude =
            sourceLatitudeDegrees *
            degreesToRadians;

        var targetLatitude =
            targetLatitudeDegrees *
            degreesToRadians;

        var latitudeDelta =
            (
                targetLatitudeDegrees -
                sourceLatitudeDegrees
            ) *
            degreesToRadians;

        var longitudeDelta =
            (
                targetLongitudeDegrees -
                sourceLongitudeDegrees
            ) *
            degreesToRadians;

        var haversine =
            Math.Pow(
                Math.Sin(
                    latitudeDelta / 2),
                2) +
            Math.Cos(
                sourceLatitude) *
            Math.Cos(
                targetLatitude) *
            Math.Pow(
                Math.Sin(
                    longitudeDelta / 2),
                2);

        var angularDistance =
            2 *
            Math.Asin(
                Math.Min(
                    1,
                    Math.Sqrt(
                        Math.Max(
                            0,
                            haversine))));

        return angularDistance *
               planetRadiusMeters;
    }

    public SimulationTimeline RecordPersonSocialEncounter(
        PersonId personId,
        SocialActorIdentity actor)
    {
        lock (_sync)
        {
            var operation =
                new RecordPersonSocialEncounterOperation(
                    personId,
                    actor);

            var world =
                SimulationOperationExecutor.Apply(
                    _timeline.CurrentWorld,
                    operation);

            var change =
                new SimulationChange(
                    operation,
                    "User intervention",
                    "Recorded person social encounter.",
                    affectedPlanetId: null,
                    elapsedSeconds: 0);

            _timeline =
                _timeline.RecordStep(
                    new SimulationStepResult(
                        world,
                        change));

            return _timeline;
        }
    }

    private SimulationTimeline AdvanceCore(
        long seconds)
    {
        if (seconds == 0)
        {
            var zeroDurationResult =
                SimulationStepRunner.Step(
                    _timeline.CurrentWorld,
                    0,
                    _causalSystems);

            _timeline =
                _timeline.RecordStep(
                    zeroDurationResult);

            return _timeline;
        }

        var remainingSeconds =
            seconds;

        while (remainingSeconds > 0)
        {
            var stepSeconds =
                Math.Min(
                    MaximumCausalCouplingStepSeconds,
                    remainingSeconds);

            var result =
                SimulationStepRunner.Step(
                    _timeline.CurrentWorld,
                    stepSeconds,
                    _causalSystems);

            _timeline =
                _timeline.RecordStep(
                    result);

            remainingSeconds -=
                stepSeconds;
        }

        return _timeline;
    }
}
