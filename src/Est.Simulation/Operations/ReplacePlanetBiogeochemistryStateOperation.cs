using Est.Simulation.Biogeochemistry;
using Est.Simulation.Planets;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

public sealed record ReplacePlanetBiogeochemistryStateOperation
    : ISimulationOperation
{
    public ReplacePlanetBiogeochemistryStateOperation(
        PlanetBiogeochemistryState biogeochemistry)
    {
        ArgumentNullException.ThrowIfNull(
            biogeochemistry);

        Biogeochemistry =
            biogeochemistry;
    }

    public PlanetBiogeochemistryState Biogeochemistry { get; }

    public WorldState Apply(
        WorldState world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        if (!world.Planets.Any(
                planet =>
                    planet.Id ==
                    Biogeochemistry.PlanetId))
        {
            throw new PlanetNotFoundException(
                Biogeochemistry.PlanetId);
        }

        var preserved =
            world.Biogeochemistry.Where(
                candidate =>
                    candidate.PlanetId !=
                    Biogeochemistry.PlanetId);

        return world.ReplaceBiogeochemistry(
            preserved.Append(
                Biogeochemistry));
    }
}
