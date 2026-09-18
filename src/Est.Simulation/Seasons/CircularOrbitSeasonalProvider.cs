using Est.Simulation.Planets;
using Est.Simulation.Worlds;

namespace Est.Simulation.Seasons;

/// <summary>
/// Derives seasonal context from a configurable circular orbit.
///
/// This provider intentionally does not model eccentricity, periapsis,
/// calendars, or general orbital mechanics.
/// </summary>
public sealed class CircularOrbitSeasonalProvider
    : ISeasonalProvider
{
    private const string PhaseId =
        "circular-orbit";

    private readonly CircularOrbitSeasonalParameters
        _parameters;

    public CircularOrbitSeasonalProvider(
        CircularOrbitSeasonalParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(
            parameters);

        _parameters = parameters;
    }

    public SeasonalContext Derive(
        WorldState world,
        PlanetId planetId)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        if (!world.Planets.Any(
                planet =>
                    planet.Id == planetId))
        {
            throw new InvalidOperationException(
                "The target planet does not exist in this world.");
        }

        var elapsedCycles =
            world.CurrentTime.TotalSeconds
            / _parameters.OrbitalPeriodSeconds;

        var cycleFraction =
            (_parameters.CycleFractionAtTimeZero +
             elapsedCycles)
            % 1.0;

        var orbitalLongitudeRadians =
            cycleFraction *
            2.0 *
            Math.PI;

        var obliquityRadians =
            _parameters.AxialTiltDegrees *
            Math.PI /
            180.0;

        var subsolarLatitudeRadians =
            Math.Asin(
                Math.Sin(obliquityRadians) *
                Math.Sin(orbitalLongitudeRadians));

        var subsolarLatitudeDegrees =
            subsolarLatitudeRadians *
            180.0 /
            Math.PI;

        return new SeasonalContext(
            PhaseId,
            cycleFraction,
            subsolarLatitudeDegrees);
    }
}
