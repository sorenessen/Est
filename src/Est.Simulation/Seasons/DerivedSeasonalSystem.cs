using Est.Simulation.Causality;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Worlds;

namespace Est.Simulation.Seasons;

public sealed class DerivedSeasonalSystem
    : ICausalSystem
{
    private readonly PlanetId _planetId;
    private readonly ISeasonalProvider _provider;

    public DerivedSeasonalSystem(
        PlanetId planetId,
        ISeasonalProvider provider)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(
            provider);

        _planetId = planetId;
        _provider = provider;
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

        var projectedWorld =
            world.AdvanceBy(
                elapsedSeconds);

        var derivedContext =
            _provider.Derive(
                projectedWorld,
                _planetId);

        var existingState =
            world.SeasonalStates
                .FirstOrDefault(
                    state =>
                        state.PlanetId ==
                        _planetId);

        var seasonalState =
            existingState?.ControlMode ==
                SeasonalControlMode.Override
                ? new PlanetSeasonalState(
                    _planetId,
                    SeasonalControlMode.Override,
                    derivedContext,
                    existingState.OverrideContext)
                : new PlanetSeasonalState(
                    _planetId,
                    SeasonalControlMode.Derived,
                    derivedContext);

        var metrics =
            new Dictionary<string, double>();

        if (derivedContext.CycleFraction.HasValue)
        {
            metrics["cycleFraction"] =
                derivedContext.CycleFraction.Value;
        }

        if (derivedContext
                .SubsolarLatitudeDegrees
                .HasValue)
        {
            metrics["subsolarLatitudeDegrees"] =
                derivedContext
                    .SubsolarLatitudeDegrees
                    .Value;
        }

        return new SimulationChange(
            new ReplacePlanetSeasonalStateOperation(
                seasonalState),
            "derived-seasonality",
            "Derived planetary seasonal context from authoritative simulation time.",
            _planetId,
            elapsedSeconds,
            metrics);
    }
}
