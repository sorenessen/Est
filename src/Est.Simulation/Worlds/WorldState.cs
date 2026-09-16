using System.Collections.Immutable;
using Est.Simulation.Animals;
using Est.Simulation.Birds;
using Est.Simulation.Grazers;
using Est.Simulation.Hydrology;
using Est.Simulation.Invertebrates;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Time;
using Est.Simulation.Terrain;
using Est.Simulation.Vegetation;

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
            null,
            null,
            null,
            null,
            null,
            null,
            null)
    {
    }

    public WorldState(
        WorldId id,
        SimulationTime currentTime,
        IEnumerable<PlanetState> planets,
        IEnumerable<PersonState> population,
        IEnumerable<AnimalState>? animals = null,
        IEnumerable<PlanetTerrainState>? terrain = null,
        IEnumerable<PlanetHydrologyState>? hydrology = null,
        IEnumerable<PlanetVegetationState>? vegetation = null,
        IEnumerable<PlanetInvertebrateState>? invertebrates = null,
        IEnumerable<BirdFlockState>? birdFlocks = null,
        IEnumerable<GrazerCohortState>? grazerCohorts = null)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "World identity cannot be empty.",
                nameof(id));
        }

        ArgumentNullException.ThrowIfNull(planets);
        ArgumentNullException.ThrowIfNull(population);

        var planetArray = planets.ToImmutableArray();
        var populationArray = population.ToImmutableArray();
        var animalArray =
            (animals ?? []).ToImmutableArray();
        var terrainArray =
            (terrain ?? []).ToImmutableArray();

        var hydrologyArray =
            (hydrology ?? []).ToImmutableArray();

        var vegetationArray =
            (vegetation ?? []).ToImmutableArray();

        var invertebrateArray =
            (invertebrates ?? []).ToImmutableArray();

        var birdFlockArray =
            (birdFlocks ?? []).ToImmutableArray();

        var grazerCohortArray =
            (grazerCohorts ?? []).ToImmutableArray();

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

        if (hydrologyArray.Any(item => item is null))
        {
            throw new ArgumentException(
                "World hydrology cannot contain null entries.",
                nameof(hydrology));
        }

        if (hydrologyArray
            .GroupBy(item => item.PlanetId)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "World cannot contain more than one hydrology state for a planet.",
                nameof(hydrology));
        }

        if (vegetationArray.Any(
                item =>
                    item is null))
        {
            throw new ArgumentException(
                "World vegetation cannot contain null entries.",
                nameof(vegetation));
        }

        if (vegetationArray
            .GroupBy(
                item =>
                    item.PlanetId)
            .Any(
                group =>
                    group.Count() > 1))
        {
            throw new ArgumentException(
                "World cannot contain more than one vegetation state for a planet.",
                nameof(vegetation));
        }

        if (invertebrateArray.Any(
                item =>
                    item is null))
        {
            throw new ArgumentException(
                "World invertebrates cannot contain null entries.",
                nameof(invertebrates));
        }

        if (invertebrateArray
            .GroupBy(
                item =>
                    item.PlanetId)
            .Any(
                group =>
                    group.Count() > 1))
        {
            throw new ArgumentException(
                "World cannot contain more than one invertebrate state for a planet.",
                nameof(invertebrates));
        }

        if (birdFlockArray.Any(
                flock =>
                    flock is null))
        {
            throw new ArgumentException(
                "World bird flocks cannot contain null entries.",
                nameof(birdFlocks));
        }

        if (birdFlockArray
            .GroupBy(
                flock =>
                    flock.Id)
            .Any(
                group =>
                    group.Count() > 1))
        {
            throw new ArgumentException(
                "World cannot contain duplicate bird flock identities.",
                nameof(birdFlocks));
        }

        if (grazerCohortArray.Any(
                cohort =>
                    cohort is null))
        {
            throw new ArgumentException(
                "World grazer cohorts cannot contain null entries.",
                nameof(grazerCohorts));
        }

        if (grazerCohortArray
            .GroupBy(
                cohort =>
                    cohort.Id)
            .Any(
                group =>
                    group.Count() > 1))
        {
            throw new ArgumentException(
                "World cannot contain duplicate grazer cohort identities.",
                nameof(grazerCohorts));
        }

        if (animalArray
            .GroupBy(animal => animal.Id)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "World cannot contain duplicate animal identities.",
                nameof(animals));
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

        if (birdFlockArray.Any(
                flock =>
                    !planetIds.Contains(
                        flock.PlanetId)))
        {
            throw new ArgumentException(
                "Every bird flock must belong to a planet in the world.",
                nameof(birdFlocks));
        }

        if (grazerCohortArray.Any(
                cohort =>
                    !planetIds.Contains(
                        cohort.PlanetId)))
        {
            throw new ArgumentException(
                "Every grazer cohort must belong to a planet in the world.",
                nameof(grazerCohorts));
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

        foreach (var hydrologyState in hydrologyArray)
        {
            var planet =
                planetArray.SingleOrDefault(
                    candidate =>
                        candidate.Id ==
                        hydrologyState.PlanetId);

            if (planet is null)
            {
                throw new ArgumentException(
                    "Every hydrology state must belong to a planet in the world.",
                    nameof(hydrology));
            }

            var terrainState =
                terrainArray.SingleOrDefault(
                    candidate =>
                        candidate.PlanetId ==
                        hydrologyState.PlanetId);

            if (terrainState is null)
            {
                throw new ArgumentException(
                    "Hydrology requires terrain for the same planet.",
                    nameof(hydrology));
            }

            if (hydrologyState.GridDefinition !=
                terrainState.GridDefinition)
            {
                throw new ArgumentException(
                    "Hydrology and terrain must use the same surface-grid definition.",
                    nameof(hydrology));
            }

            hydrologyState.ValidateFor(
                planet);
        }

        foreach (var vegetationState in vegetationArray)
        {
            var planet =
                planetArray.SingleOrDefault(
                    candidate =>
                        candidate.Id ==
                        vegetationState.PlanetId);

            if (planet is null)
            {
                throw new ArgumentException(
                    "Every vegetation state must belong to a planet in the world.",
                    nameof(vegetation));
            }

            var hydrologyState =
                hydrologyArray.SingleOrDefault(
                    candidate =>
                        candidate.PlanetId ==
                        vegetationState.PlanetId);

            if (hydrologyState is null)
            {
                throw new ArgumentException(
                    "Vegetation requires hydrology for the same planet.",
                    nameof(vegetation));
            }

            var terrainState =
                terrainArray.SingleOrDefault(
                    candidate =>
                        candidate.PlanetId ==
                        vegetationState.PlanetId);

            if (terrainState is null)
            {
                throw new ArgumentException(
                    "Vegetation requires terrain for the same planet.",
                    nameof(vegetation));
            }

            if (vegetationState.GridDefinition !=
                    hydrologyState.GridDefinition ||
                vegetationState.GridDefinition !=
                    terrainState.GridDefinition)
            {
                throw new ArgumentException(
                    "Vegetation, hydrology, and terrain must use the same surface-grid definition.",
                    nameof(vegetation));
            }

            vegetationState.ValidateFor(
                planet);
        }


        foreach (var invertebrateState in invertebrateArray)
        {
            var planet =
                planetArray.SingleOrDefault(
                    candidate =>
                        candidate.Id ==
                        invertebrateState.PlanetId);

            if (planet is null)
            {
                throw new ArgumentException(
                    "Every invertebrate state must belong to a planet in the world.",
                    nameof(invertebrates));
            }

            var vegetationState =
                vegetationArray.SingleOrDefault(
                    candidate =>
                        candidate.PlanetId ==
                        invertebrateState.PlanetId);

            if (vegetationState is null)
            {
                throw new ArgumentException(
                    "Invertebrates require vegetation for the same planet.",
                    nameof(invertebrates));
            }

            if (invertebrateState.GridDefinition !=
                vegetationState.GridDefinition)
            {
                throw new ArgumentException(
                    "Invertebrates and vegetation must use the same surface-grid definition.",
                    nameof(invertebrates));
            }

            invertebrateState.ValidateFor(
                planet);
        }

        Id = id;
        CurrentTime = currentTime;
        Planets = planetArray;
        Population = populationArray;
        Animals = animalArray;
        Terrain = terrainArray;
        Hydrology = hydrologyArray;
        Vegetation = vegetationArray;
        Invertebrates = invertebrateArray;
        BirdFlocks = birdFlockArray;
        GrazerCohorts = grazerCohortArray;
    }

    public WorldId Id { get; private init; }

    public SimulationTime CurrentTime { get; private init; }

    public ImmutableArray<PlanetState> Planets { get; private init; }

    public ImmutableArray<PersonState> Population { get; private init; }

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

    public ImmutableArray<PlanetHydrologyState> Hydrology
    {
        get;
        private init;
    }

    public ImmutableArray<PlanetVegetationState> Vegetation
    {
        get;
        private init;
    }

    public ImmutableArray<PlanetInvertebrateState> Invertebrates
    {
        get;
        private init;
    }

    public ImmutableArray<BirdFlockState> BirdFlocks
    {
        get;
        private init;
    }

    public ImmutableArray<GrazerCohortState> GrazerCohorts
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
            Animals,
            Terrain,
            Hydrology,
            Vegetation,
            Invertebrates,
            BirdFlocks,
            GrazerCohorts);
    }

    public WorldState Fork()
    {
        return new WorldState(
            WorldId.New(),
            CurrentTime,
            Planets,
            Population,
            Animals,
            Terrain,
            Hydrology,
            Vegetation,
            Invertebrates,
            BirdFlocks,
            GrazerCohorts);
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
            Animals,
            Terrain,
            Hydrology,
            Vegetation,
            Invertebrates,
            BirdFlocks,
            GrazerCohorts);
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
            animals,
            Terrain,
            Hydrology,
            Vegetation,
            Invertebrates,
            BirdFlocks,
            GrazerCohorts);
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
            Animals,
            terrain,
            Hydrology,
            Vegetation,
            Invertebrates,
            BirdFlocks,
            GrazerCohorts);
    }

    public WorldState ReplaceHydrology(
        IEnumerable<PlanetHydrologyState> hydrology)
    {
        ArgumentNullException.ThrowIfNull(
            hydrology);

        return new WorldState(
            Id,
            CurrentTime,
            Planets,
            Population,
            Animals,
            Terrain,
            hydrology,
            Vegetation,
            Invertebrates,
            BirdFlocks,
            GrazerCohorts);
    }

    public WorldState ReplaceVegetation(
        IEnumerable<PlanetVegetationState> vegetation)
    {
        ArgumentNullException.ThrowIfNull(
            vegetation);

        return new WorldState(
            Id,
            CurrentTime,
            Planets,
            Population,
            Animals,
            Terrain,
            Hydrology,
            vegetation,
            Invertebrates,
            BirdFlocks,
            GrazerCohorts);
    }

    public WorldState ReplaceInvertebrates(
        IEnumerable<PlanetInvertebrateState> invertebrates)
    {
        ArgumentNullException.ThrowIfNull(
            invertebrates);

        return new WorldState(
            Id,
            CurrentTime,
            Planets,
            Population,
            Animals,
            Terrain,
            Hydrology,
            Vegetation,
            invertebrates,
            BirdFlocks,
            GrazerCohorts);
    }

    public WorldState ReplaceBirdFlocks(
        IEnumerable<BirdFlockState> birdFlocks)
    {
        ArgumentNullException.ThrowIfNull(
            birdFlocks);

        return new WorldState(
            Id,
            CurrentTime,
            Planets,
            Population,
            Animals,
            Terrain,
            Hydrology,
            Vegetation,
            Invertebrates,
            birdFlocks,
            GrazerCohorts);
    }

    public WorldState ReplaceGrazerCohorts(
        IEnumerable<GrazerCohortState> grazerCohorts)
    {
        ArgumentNullException.ThrowIfNull(
            grazerCohorts);

        return new WorldState(
            Id,
            CurrentTime,
            Planets,
            Population,
            Animals,
            Terrain,
            Hydrology,
            Vegetation,
            Invertebrates,
            BirdFlocks,
            grazerCohorts);
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
