using Est.Simulation.Planets;

namespace Est.Simulation.Birds;

public sealed record BirdModelDefinition
{
    public BirdModelDefinition(
        PlanetId planetId,
        BirdModelParameters parameters)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(
            parameters);

        PlanetId =
            planetId;

        Parameters =
            parameters;
    }

    public PlanetId PlanetId { get; }

    public BirdModelParameters Parameters { get; }
}
