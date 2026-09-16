using Est.Simulation.Planets;

namespace Est.Simulation.Grazers;

public sealed record GrazerModelDefinition
{
    public GrazerModelDefinition(
        PlanetId planetId,
        GrazerModelParameters parameters)
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

    public GrazerModelParameters Parameters { get; }
}
