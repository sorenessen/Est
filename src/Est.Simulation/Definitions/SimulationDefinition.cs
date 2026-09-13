using System.Collections.Immutable;
using Est.Simulation.Population;
using Est.Simulation.Worlds;

namespace Est.Simulation.Definitions;

public sealed record SimulationDefinition
{
    public SimulationDefinition(
        IEnumerable<PlanetaryEnergyBalanceModelDefinition>? planetaryEnergyBalanceModels = null,
        IEnumerable<PopulationModelDefinition>? populationModels = null)
    {
        var models = planetaryEnergyBalanceModels?
            .ToImmutableArray()
            ?? ImmutableArray<PlanetaryEnergyBalanceModelDefinition>.Empty;

        var population = populationModels?
            .ToImmutableArray()
            ?? ImmutableArray<PopulationModelDefinition>.Empty;

        if (models.Any(model => model is null))
        {
            throw new ArgumentException(
                "Simulation definition cannot contain null model definitions.",
                nameof(planetaryEnergyBalanceModels));
        }

        if (population.Any(model => model is null))
        {
            throw new ArgumentException(
                "Simulation definition cannot contain null population model definitions.",
                nameof(populationModels));
        }

        if (models
            .GroupBy(model => model.PlanetId)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "A planet cannot have more than one planetary energy-balance model definition.",
                nameof(planetaryEnergyBalanceModels));
        }

        if (population
            .GroupBy(model => model.PlanetId)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "A planet cannot have more than one population model definition.",
                nameof(populationModels));
        }

        PlanetaryEnergyBalanceModels = models;
        PopulationModels = population;
    }

    public ImmutableArray<PlanetaryEnergyBalanceModelDefinition>
        PlanetaryEnergyBalanceModels { get; }

    public ImmutableArray<PopulationModelDefinition>
        PopulationModels { get; }

    public static SimulationDefinition Empty { get; } = new();

    public void ValidateFor(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var planetIds = world.Planets
            .Select(planet => planet.Id)
            .ToHashSet();

        foreach (var model in PlanetaryEnergyBalanceModels)
        {
            if (!planetIds.Contains(model.PlanetId))
            {
                throw new ArgumentException(
                    $"Energy-balance model targets planet '{model.PlanetId.Value}', which does not exist in the world.",
                    nameof(world));
            }
        }

        foreach (var model in PopulationModels)
        {
            if (!planetIds.Contains(model.PlanetId))
            {
                throw new ArgumentException(
                    $"Population model targets planet '{model.PlanetId.Value}', which does not exist in the world.",
                    nameof(world));
            }
        }
    }
}
