using Est.Simulation.Planets;

namespace Est.Simulation.Hydrology;

public sealed record HydrologyModelDefinition
{
    public HydrologyModelDefinition(
        PlanetId planetId,
        HydrologyModelParameters parameters)
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

    public HydrologyModelParameters Parameters { get; }
}
