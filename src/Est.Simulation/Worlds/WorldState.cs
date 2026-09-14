using System.Collections.Immutable;
using Est.Simulation.Animals;
using Est.Simulation.Ecology;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Time;
using Est.Simulation.Terrain;

namespace Est.Simulation.Worlds;

public sealed record WorldState
{
    public WorldState(WorldId id, SimulationTime currentTime)
        : this(id, currentTime, [], [])
    {
    }

    public WorldState(
        WorldId id,
        SimulationTime currentTime,
        IEnumerable<PlanetState> planets)
        : this(id, currentTime, planets, [])
    {
    }

    public WorldState(
        WorldId id,
        SimulationTime currentTime,
        IEnumerable<PlanetState> planets,
        IEnumerable<PersonState> population)
        : this(
            id,
            currentTime,
            planets,
            population,
            [])
    {
    }

    public WorldState(
        WorldId id,
        SimulationTime currentTime,
        IEnumerable<PlanetState> planets,
        IEnumerable<PersonState> population,
        IEnumerable<FoodResourceState> foodResources,
        IEnumerable<AnimalState>? animals = null,
        IEnumerable<PlanetTerrainState>? terrain = null)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "World identity cannot be empty.",
                nameof(id));
        }

        ArgumentNullException.ThrowIfNull(planets);
        ArgumentNullException.ThrowIfNull(population);
        ArgumentNullException.ThrowIfNull(foodResources);

        var planetArray = planets.ToImmutableArray();
        var populationArray = population.ToImmutableArray();
        var foodResourceArray =
            foodResources.ToImmutableArray();
        var animalArray =
            (animals ?? []).ToImmutableArray();
        var terrainArray =
            (terrain ?? []).ToImmutableArray();

        if (planetArray.Any(planet => planet is null))
        {
            throw new ArgumentException(
                "World planets cannot contain null entries.",
                nameof(planets));
        }

        if (planetArray
            .GroupBy(planet => planet.Id)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "World cannot contain duplicate planet identities.",
                nameof(planets));
        }

        if (populationArray.Any(person => person is null))
        {
            throw new ArgumentException(
                "World population cannot contain null entries.",
                nameof(population));
        }

        if (foodResourceArray.Any(resource => resource is null))
        {
            throw new ArgumentException(
                "World food resources cannot contain null entries.",
                nameof(foodResources));
        }

        if (animalArray.Any(animal => animal is null))
        {
            throw new ArgumentException(
                "World animals cannot contain null entries.",
                nameof(animals));
        }

        if (terrainArray.Any(item => item is null))
        {
            throw new ArgumentException(
                "World terrain cannot contain null entries.",
                nameof(terrain));
        }

        if (terrainArray
            .GroupBy(item => item.PlanetId)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "World cannot contain more than one terrain state for a planet.",
                nameof(terrain));
        }

        if (animalArray
            .GroupBy(animal => animal.Id)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "World cannot contain duplicate animal identities.",
                nameof(animals));
        }

        if (foodResourceArray
            .GroupBy(resource => resource.Id)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "World cannot contain duplicate food resource identities.",
                nameof(foodResources));
        }

        if (populationArray
            .GroupBy(person => person.Id)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "World cannot contain duplicate person identities.",
                nameof(population));
        }

        var planetIds = planetArray
            .Select(planet => planet.Id)
            .ToHashSet();

        if (populationArray.Any(
                person =>
                    !planetIds.Contains(person.PlanetId)))
        {
            throw new ArgumentException(
                "Every person must belong to a planet in the world.",
                nameof(population));
        }

        if (animalArray.Any(
                animal =>
                    !planetIds.Contains(animal.PlanetId)))
        {
            throw new ArgumentException(
                "Every animal must belong to a planet in the world.",
                nameof(animals));
        }

        if (foodResourceArray.Any(
                resource =>
                    !planetIds.Contains(resource.PlanetId)))
        {
            throw new ArgumentException(
                "Every food resource must belong to a planet in the world.",
                nameof(foodResources));
        }

        foreach (var terrainState in terrainArray)
        {
            var planet =
                planetArray.SingleOrDefault(
                    candidate =>
                        candidate.Id ==
                        terrainState.PlanetId);

            if (planet is null)
            {
                throw new ArgumentException(
                    "Every terrain state must belong to a planet in the world.",
                    nameof(terrain));
            }

            terrainState.ValidateFor(
                planet);
        }

        Id = id;
        CurrentTime = currentTime;
        Planets = planetArray;
        Population = populationArray;
        FoodResources = foodResourceArray;
        Animals = animalArray;
        Terrain = terrainArray;
    }

    public WorldId Id { get; private init; }

    public SimulationTime CurrentTime { get; private init; }

    public ImmutableArray<PlanetState> Planets { get; private init; }

    public ImmutableArray<PersonState> Population { get; private init; }

    public ImmutableArray<FoodResourceState> FoodResources
    {
        get;
        private init;
    }

    public ImmutableArray<AnimalState> Animals
    {
        get;
        private init;
    }

    public ImmutableArray<PlanetTerrainState> Terrain
    {
        get;
        private init;
    }

    public WorldState AdvanceBy(long seconds)
    {
        return this with
        {
            CurrentTime = CurrentTime.AdvanceBy(seconds)
        };
    }

    public WorldState Copy()
    {
        return new WorldState(
            Id,
            CurrentTime,
            Planets,
            Population,
            FoodResources,
            Animals,
            Terrain);
    }

    public WorldState Fork()
    {
        return new WorldState(
            WorldId.New(),
            CurrentTime,
            Planets,
            Population,
            FoodResources,
            Animals,
            Terrain);
    }

    public WorldState ReplacePopulation(
        IEnumerable<PersonState> population)
    {
        ArgumentNullException.ThrowIfNull(population);

        return new WorldState(
            Id,
            CurrentTime,
            Planets,
            population,
            FoodResources,
            Animals,
            Terrain);
    }

    public WorldState ReplaceFoodResources(
        IEnumerable<FoodResourceState> foodResources)
    {
        ArgumentNullException.ThrowIfNull(foodResources);

        return new WorldState(
            Id,
            CurrentTime,
            Planets,
            Population,
            foodResources,
            Animals,
            Terrain);
    }

    public WorldState ReplaceAnimals(
        IEnumerable<AnimalState> animals)
    {
        ArgumentNullException.ThrowIfNull(animals);

        return new WorldState(
            Id,
            CurrentTime,
            Planets,
            Population,
            FoodResources,
            animals,
            Terrain);
    }

    public WorldState ReplaceTerrain(
        IEnumerable<PlanetTerrainState> terrain)
    {
        ArgumentNullException.ThrowIfNull(terrain);

        return new WorldState(
            Id,
            CurrentTime,
            Planets,
            Population,
            FoodResources,
            Animals,
            terrain);
    }

    public WorldState AddPlanet(PlanetState planet)
    {
        ArgumentNullException.ThrowIfNull(planet);

        if (Planets.Any(existing => existing.Id == planet.Id))
        {
            throw new InvalidOperationException(
                "A planet with this identity already exists in the world.");
        }

        return this with
        {
            Planets = Planets.Add(planet)
        };
    }

    public WorldState ReplacePlanet(PlanetState planet)
    {
        ArgumentNullException.ThrowIfNull(planet);

        for (var index = 0; index < Planets.Length; index++)
        {
            if (Planets[index].Id == planet.Id)
            {
                return this with
                {
                    Planets = Planets.SetItem(index, planet)
                };
            }
        }

        throw new InvalidOperationException(
            "The planet does not exist in this world.");
    }
}
