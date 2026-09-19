using Est.Simulation.Planets;
using Est.Simulation.Thermal;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

public sealed record ReplacePlanetRegionalThermalStateOperation
    : ISimulationOperation
{
    public ReplacePlanetRegionalThermalStateOperation(
        PlanetRegionalThermalState regionalThermal)
    {
        ArgumentNullException.ThrowIfNull(
            regionalThermal);

        RegionalThermal = regionalThermal;
    }

    public PlanetRegionalThermalState RegionalThermal { get; }

    public WorldState Apply(
        WorldState world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        if (!world.Planets.Any(
                planet =>
                    planet.Id ==
                    RegionalThermal.PlanetId))
        {
            throw new PlanetNotFoundException(
                RegionalThermal.PlanetId);
        }

        var preserved =
            world.RegionalThermal.Where(
                candidate =>
                    candidate.PlanetId !=
                    RegionalThermal.PlanetId);

        return world.ReplaceRegionalThermal(
            preserved.Append(
                RegionalThermal));
    }
}
