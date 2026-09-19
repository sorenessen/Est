using Est.Simulation.Planets;
using Est.Simulation.Thermal;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

/// <summary>
/// Atomically replaces one planet's regional thermal state and projects the
/// corresponding area-weighted surface temperature into the compatibility
/// planetary mean.
///
/// Surface water, coarse ice coverage, and atmospheric state are preserved.
/// </summary>
public sealed record
    ReplacePlanetRegionalThermalStateAndEnvironmentOperation
    : ISimulationOperation
{
    public ReplacePlanetRegionalThermalStateAndEnvironmentOperation(
        PlanetRegionalThermalState regionalThermal,
        double meanSurfaceTemperatureKelvin)
    {
        ArgumentNullException.ThrowIfNull(
            regionalThermal);

        if (!double.IsFinite(
                meanSurfaceTemperatureKelvin) ||
            meanSurfaceTemperatureKelvin < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(meanSurfaceTemperatureKelvin),
                "Compatibility mean surface temperature must be finite and at or above absolute zero.");
        }

        RegionalThermal =
            regionalThermal;

        MeanSurfaceTemperatureKelvin =
            meanSurfaceTemperatureKelvin;
    }

    public PlanetRegionalThermalState RegionalThermal { get; }

    public double MeanSurfaceTemperatureKelvin { get; }

    public WorldState Apply(
        WorldState world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        var planet =
            world.Planets.FirstOrDefault(
                candidate =>
                    candidate.Id ==
                    RegionalThermal.PlanetId)
            ?? throw new PlanetNotFoundException(
                RegionalThermal.PlanetId);

        var environment =
            new PlanetEnvironment(
                MeanSurfaceTemperatureKelvin,
                planet.Environment
                    .SurfaceWaterFraction,
                planet.Environment
                    .IceCoverageFraction,
                planet.Environment
                    .Atmosphere);

        var replacementPlanet =
            new PlanetState(
                planet.Id,
                planet.Name,
                planet.MassKilograms,
                planet.MeanRadiusMeters,
                environment);

        // ReplacePlanet itself preserves every non-planet subsystem. The final
        // ReplaceRegionalThermal call reconstructs and validates the complete
        // WorldState before this single operation returns it.
        var withProjectedEnvironment =
            world.ReplacePlanet(
                replacementPlanet);

        var preservedRegionalThermal =
            withProjectedEnvironment
                .RegionalThermal
                .Where(
                    candidate =>
                        candidate.PlanetId !=
                        RegionalThermal.PlanetId);

        return withProjectedEnvironment
            .ReplaceRegionalThermal(
                preservedRegionalThermal
                    .Append(
                        RegionalThermal));
    }
}
