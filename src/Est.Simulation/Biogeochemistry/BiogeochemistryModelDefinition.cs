using Est.Simulation.Planets;

namespace Est.Simulation.Biogeochemistry;

public sealed record BiogeochemistryModelDefinition
{
    public BiogeochemistryModelDefinition(
        PlanetId planetId,
        BiogeochemistryModelParameters parameters)
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

    public BiogeochemistryModelParameters Parameters { get; }
}
