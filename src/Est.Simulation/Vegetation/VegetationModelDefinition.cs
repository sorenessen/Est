using Est.Simulation.Planets;

namespace Est.Simulation.Vegetation;

public sealed record VegetationModelDefinition
{
    public VegetationModelDefinition(
        PlanetId planetId,
        VegetationModelParameters parameters)
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

    public VegetationModelParameters Parameters { get; }
}
