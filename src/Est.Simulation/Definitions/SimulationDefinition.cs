using System.Collections.Immutable;
using Est.Simulation.Birds;
using Est.Simulation.Hydrology;
using Est.Simulation.Invertebrates;
using Est.Simulation.Population;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Definitions;

public sealed record SimulationDefinition
{
    public SimulationDefinition(
        IEnumerable<PlanetaryEnergyBalanceModelDefinition>? planetaryEnergyBalanceModels = null,
        IEnumerable<PopulationModelDefinition>? populationModels = null,
        IEnumerable<HydrologyModelDefinition>? hydrologyModels = null,
        IEnumerable<VegetationModelDefinition>? vegetationModels = null,
        IEnumerable<InvertebrateModelDefinition>? invertebrateModels = null,
        IEnumerable<BirdModelDefinition>? birdModels = null)
    {
        var models = planetaryEnergyBalanceModels?
            .ToImmutableArray()
            ?? ImmutableArray<PlanetaryEnergyBalanceModelDefinition>.Empty;

        var population = populationModels?
            .ToImmutableArray()
            ?? ImmutableArray<PopulationModelDefinition>.Empty;

        var hydrology = hydrologyModels?
            .ToImmutableArray()
            ?? ImmutableArray<HydrologyModelDefinition>.Empty;

        var vegetation = vegetationModels?
            .ToImmutableArray()
            ?? ImmutableArray<VegetationModelDefinition>.Empty;

        var invertebrates = invertebrateModels?
            .ToImmutableArray()
            ?? ImmutableArray<InvertebrateModelDefinition>.Empty;

        var birds = birdModels?
            .ToImmutableArray()
            ?? ImmutableArray<BirdModelDefinition>.Empty;

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

        if (hydrology.Any(model => model is null))
        {
            throw new ArgumentException(
                "Simulation definition cannot contain null hydrology model definitions.",
                nameof(hydrologyModels));
        }

        if (vegetation.Any(model => model is null))
        {
            throw new ArgumentException(
                "Simulation definition cannot contain null vegetation model definitions.",
                nameof(vegetationModels));
        }

        if (invertebrates.Any(model => model is null))
        {
            throw new ArgumentException(
                "Simulation definition cannot contain null invertebrate model definitions.",
                nameof(invertebrateModels));
        }

        if (birds.Any(model => model is null))
        {
            throw new ArgumentException(
                "Simulation definition cannot contain null bird model definitions.",
                nameof(birdModels));
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

        if (hydrology
            .GroupBy(model => model.PlanetId)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "A planet cannot have more than one hydrology model definition.",
                nameof(hydrologyModels));
        }

        if (vegetation
            .GroupBy(model => model.PlanetId)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "A planet cannot have more than one vegetation model definition.",
                nameof(vegetationModels));
        }

        if (invertebrates
            .GroupBy(model => model.PlanetId)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "A planet cannot have more than one invertebrate model definition.",
                nameof(invertebrateModels));
        }

        if (birds
            .GroupBy(model => model.PlanetId)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "A planet cannot have more than one bird model definition.",
                nameof(birdModels));
        }

        PlanetaryEnergyBalanceModels = models;
        PopulationModels = population;
        HydrologyModels = hydrology;
        VegetationModels = vegetation;
        InvertebrateModels = invertebrates;
        BirdModels = birds;
    }

    public ImmutableArray<PlanetaryEnergyBalanceModelDefinition>
        PlanetaryEnergyBalanceModels { get; }

    public ImmutableArray<PopulationModelDefinition>
        PopulationModels { get; }

    public ImmutableArray<HydrologyModelDefinition>
        HydrologyModels { get; }

    public ImmutableArray<VegetationModelDefinition>
        VegetationModels { get; }

    public ImmutableArray<InvertebrateModelDefinition>
        InvertebrateModels { get; }

    public ImmutableArray<BirdModelDefinition>
        BirdModels { get; }

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

            if (model.VegetationForaging is not null &&
                !world.Vegetation.Any(
                    vegetation =>
                        vegetation.PlanetId ==
                        model.PlanetId))
            {
                throw new ArgumentException(
                    $"Population model for planet '{model.PlanetId.Value}' enables vegetation foraging, but the world does not contain authoritative vegetation state for that planet.",
                    nameof(world));
            }
        }

        foreach (var model in HydrologyModels)
        {
            if (!planetIds.Contains(model.PlanetId))
            {
                throw new ArgumentException(
                    $"Hydrology model targets planet '{model.PlanetId.Value}', which does not exist in the world.",
                    nameof(world));
            }
        }

        foreach (var model in VegetationModels)
        {
            if (!planetIds.Contains(model.PlanetId))
            {
                throw new ArgumentException(
                    $"Vegetation model targets planet '{model.PlanetId.Value}', which does not exist in the world.",
                    nameof(world));
            }
        }

        foreach (var model in InvertebrateModels)
        {
            if (!planetIds.Contains(model.PlanetId))
            {
                throw new ArgumentException(
                    $"Invertebrate model targets planet '{model.PlanetId.Value}', which does not exist in the world.",
                    nameof(world));
            }

            if (!world.Invertebrates.Any(
                    invertebrates =>
                        invertebrates.PlanetId ==
                        model.PlanetId))
            {
                throw new ArgumentException(
                    $"Invertebrate model for planet '{model.PlanetId.Value}' requires authoritative invertebrate state for that planet.",
                    nameof(world));
            }
        }

        foreach (var model in BirdModels)
        {
            if (!planetIds.Contains(model.PlanetId))
            {
                throw new ArgumentException(
                    $"Bird model targets planet '{model.PlanetId.Value}', which does not exist in the world.",
                    nameof(world));
            }

            if (!world.Invertebrates.Any(
                    invertebrates =>
                        invertebrates.PlanetId ==
                        model.PlanetId))
            {
                throw new ArgumentException(
                    $"Bird model for planet '{model.PlanetId.Value}' requires authoritative invertebrate state for that planet.",
                    nameof(world));
            }
        }
    }
}
