using Est.Simulation.Planets;

namespace Est.Simulation.Seasons;

public sealed record CircularOrbitSeasonalModelDefinition
{
    public CircularOrbitSeasonalModelDefinition(
        PlanetId planetId,
        CircularOrbitSeasonalParameters parameters)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(
            parameters);

        PlanetId = planetId;
        Parameters = parameters;
    }

    public PlanetId PlanetId { get; }

    public CircularOrbitSeasonalParameters Parameters { get; }
}
