using Est.Simulation.Planets;

namespace Est.Simulation.Seasons;

/// <summary>
/// Authoritative seasonal-control state for one planet.
///
/// Derived and override context remain separate so an explicit override never
/// masquerades as naturally derived seasonality.
/// </summary>
public sealed record PlanetSeasonalState
{
    public PlanetSeasonalState(
        PlanetId planetId,
        SeasonalControlMode controlMode = SeasonalControlMode.Disabled,
        SeasonalContext? derivedContext = null,
        SeasonalContext? overrideContext = null)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        switch (controlMode)
        {
            case SeasonalControlMode.Disabled:
                if (derivedContext is not null ||
                    overrideContext is not null)
                {
                    throw new ArgumentException(
                        "Disabled seasonal control cannot contain derived or override context.");
                }

                break;

            case SeasonalControlMode.Derived:
                if (derivedContext is null)
                {
                    throw new ArgumentException(
                        "Derived seasonal control requires derived context.",
                        nameof(derivedContext));
                }

                if (overrideContext is not null)
                {
                    throw new ArgumentException(
                        "Derived seasonal control cannot contain override context.",
                        nameof(overrideContext));
                }

                break;

            case SeasonalControlMode.Override:
                if (overrideContext is null)
                {
                    throw new ArgumentException(
                        "Override seasonal control requires override context.",
                        nameof(overrideContext));
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(controlMode),
                    controlMode,
                    "Seasonal control mode is not supported.");
        }

        PlanetId = planetId;
        ControlMode = controlMode;
        DerivedContext = derivedContext;
        OverrideContext = overrideContext;
    }

    public PlanetId PlanetId { get; }

    public SeasonalControlMode ControlMode { get; }

    public SeasonalContext? DerivedContext { get; }

    public SeasonalContext? OverrideContext { get; }

    public SeasonalContext? EffectiveContext =>
        ControlMode switch
        {
            SeasonalControlMode.Disabled => null,
            SeasonalControlMode.Derived => DerivedContext,
            SeasonalControlMode.Override => OverrideContext,
            _ => throw new InvalidOperationException(
                $"Unsupported seasonal control mode '{ControlMode}'.")
        };
}
