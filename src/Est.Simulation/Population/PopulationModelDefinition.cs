using Est.Simulation.Ecology;
using Est.Simulation.Planets;

namespace Est.Simulation.Population;

public sealed record PopulationModelDefinition
{
    public PopulationModelDefinition(
        PlanetId planetId,
        PopulationModelParameters parameters,
        VegetationForagingParameters? vegetationForaging = null)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(parameters);

        PlanetId = planetId;
        Parameters = parameters;
        VegetationForaging = vegetationForaging;
    }

    public PlanetId PlanetId { get; }

    public PopulationModelParameters Parameters { get; }

    /// <summary>
    /// Optional explicit policy enabling consumption of authoritative
    /// vegetation by this population model.
    ///
    /// When absent, the existing synthetic food-resource foraging path remains
    /// active for backward compatibility.
    /// </summary>
    public VegetationForagingParameters? VegetationForaging { get; }
}
