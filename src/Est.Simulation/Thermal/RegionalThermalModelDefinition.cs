using Est.Simulation.Planets;

namespace Est.Simulation.Thermal;

public sealed record RegionalThermalModelDefinition
{
    public RegionalThermalModelDefinition(
        PlanetId planetId,
        RegionalThermalModelParameters parameters)
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

    public RegionalThermalModelParameters Parameters { get; }
}
