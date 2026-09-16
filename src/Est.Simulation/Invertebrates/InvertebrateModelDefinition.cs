using Est.Simulation.Planets;

namespace Est.Simulation.Invertebrates;

public sealed record InvertebrateModelDefinition
{
    public InvertebrateModelDefinition(
        PlanetId planetId,
        InvertebrateModelParameters parameters)
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

    public InvertebrateModelParameters Parameters { get; }
}
