using Est.Simulation.Animals;
using Est.Simulation.Causality;
using Est.Simulation.Climate;
using Est.Simulation.Definitions;
using Est.Simulation.Ecology;
using Est.Simulation.Hydrology;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Time;
using Est.Simulation.Timelines;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Application.Sessions;

public sealed class SimulationSession
{
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

        var energyBalanceSystems =
            definition.PlanetaryEnergyBalanceModels
                .Select(
                    model =>
                        (ICausalSystem)
                            new PlanetaryEnergyBalanceSystem(
                                model.PlanetId,
                                model.Parameters));

        var hydrologySystems =
            definition.HydrologyModels
                .Select(
                    model =>
                        (ICausalSystem)
                            new HydrologySystem(
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

        var foragingSystems =
            definition.PopulationModels
                .Select(
                    model =>
                        (ICausalSystem)
                            new ForagingSystem(
                                model.PlanetId));

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
            energyBalanceSystems
                .Concat(hydrologySystems)
                .Concat(vegetationSystems)
                .Concat(foragingSystems)
                .Concat(predatorSystems)
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

    private SimulationTimeline AdvanceCore(
        long seconds)
    {
        var result =
            SimulationStepRunner.Step(
                _timeline.CurrentWorld,
                seconds,
                _causalSystems);

        _timeline =
            _timeline.RecordStep(result);

        return _timeline;
    }
}
