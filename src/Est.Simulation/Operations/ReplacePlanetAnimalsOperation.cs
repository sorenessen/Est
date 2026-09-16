using Est.Simulation.Animals;
using Est.Simulation.Planets;
using Est.Simulation.Worlds;

namespace Est.Simulation.Operations;

public sealed record ReplacePlanetAnimalsOperation
    : ISimulationOperation
{
    public ReplacePlanetAnimalsOperation(
        PlanetId planetId,
        IEnumerable<AnimalState> animals)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(
            animals);

        PlanetId =
            planetId;

        Animals =
            animals.ToArray();

        if (Animals.Any(
                animal =>
                    animal.PlanetId != planetId))
        {
            throw new ArgumentException(
                "Animals contain an animal assigned to another planet.",
                nameof(animals));
        }
    }

    public PlanetId PlanetId { get; }

    public IReadOnlyList<AnimalState> Animals
    {
        get;
    }

    public WorldState Apply(
        WorldState world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        if (!world.Planets.Any(
                planet =>
                    planet.Id == PlanetId))
        {
            throw new PlanetNotFoundException(
                PlanetId);
        }

        var preservedAnimals =
            world.Animals.Where(
                animal =>
                    animal.PlanetId !=
                    PlanetId);

        return world.ReplaceAnimals(
            preservedAnimals.Concat(
                Animals));
    }
}
