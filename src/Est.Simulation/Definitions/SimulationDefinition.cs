using System.Collections.Immutable;
using Est.Simulation.Birds;
using Est.Simulation.Biogeochemistry;
using Est.Simulation.Grazers;
using Est.Simulation.Hydrology;
using Est.Simulation.Invertebrates;
using Est.Simulation.Population;
using Est.Simulation.Seasons;
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
        IEnumerable<BirdModelDefinition>? birdModels = null,
        IEnumerable<GrazerModelDefinition>? grazerModels = null,
        IEnumerable<BiogeochemistryModelDefinition>? biogeochemistryModels = null,
        IEnumerable<CircularOrbitSeasonalModelDefinition>? seasonalModels = null)
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

        var grazers = grazerModels?
            .ToImmutableArray()
            ?? ImmutableArray<GrazerModelDefinition>.Empty;

        var biogeochemistry = biogeochemistryModels?
            .ToImmutableArray()
            ?? ImmutableArray<BiogeochemistryModelDefinition>.Empty;

        var seasonal = seasonalModels?
            .ToImmutableArray()
            ?? ImmutableArray<CircularOrbitSeasonalModelDefinition>.Empty;

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

        if (grazers.Any(model => model is null))
        {
            throw new ArgumentException(
                "Simulation definition cannot contain null grazer model definitions.",
                nameof(grazerModels));
        }

        if (biogeochemistry.Any(model => model is null))
        {
            throw new ArgumentException(
                "Simulation definition cannot contain null biogeochemistry model definitions.",
                nameof(biogeochemistryModels));
        }

        if (seasonal.Any(model => model is null))
        {
            throw new ArgumentException(
                "Simulation definition cannot contain null seasonal model definitions.",
                nameof(seasonalModels));
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

        if (grazers
            .GroupBy(model => model.PlanetId)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "A planet cannot have more than one grazer model definition.",
                nameof(grazerModels));
        }

        if (biogeochemistry
            .GroupBy(model => model.PlanetId)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "A planet cannot have more than one biogeochemistry model definition.",
                nameof(biogeochemistryModels));
        }

        if (seasonal
            .GroupBy(model => model.PlanetId)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "A planet cannot have more than one seasonal model definition.",
                nameof(seasonalModels));
        }

        PlanetaryEnergyBalanceModels = models;
        PopulationModels = population;
        HydrologyModels = hydrology;
        VegetationModels = vegetation;
        InvertebrateModels = invertebrates;
        BirdModels = birds;
        GrazerModels = grazers;
        BiogeochemistryModels = biogeochemistry;
        SeasonalModels = seasonal;
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

    public ImmutableArray<GrazerModelDefinition>
        GrazerModels { get; }

    public ImmutableArray<BiogeochemistryModelDefinition>
        BiogeochemistryModels { get; }

    public ImmutableArray<CircularOrbitSeasonalModelDefinition>
        SeasonalModels { get; }

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

            if (model.Parameters
                    .PlantNitrogenKilogramsPerKilogramLiveBiomass
                is not null &&
                !world.Biogeochemistry.Any(
                    state =>
                        state.PlanetId ==
                        model.PlanetId))
            {
                throw new ArgumentException(
                    $"Nitrogen-coupled vegetation model for planet '{model.PlanetId.Value}' requires authoritative biogeochemistry state for that planet.",
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

        foreach (var model in GrazerModels)
        {
            if (!planetIds.Contains(model.PlanetId))
            {
                throw new ArgumentException(
                    $"Grazer model targets planet '{model.PlanetId.Value}', which does not exist in the world.",
                    nameof(world));
            }

            if (!world.Vegetation.Any(
                    vegetation =>
                        vegetation.PlanetId ==
                        model.PlanetId))
            {
                throw new ArgumentException(
                    $"Grazer model for planet '{model.PlanetId.Value}' requires authoritative vegetation state for that planet.",
                    nameof(world));
            }
        }

        foreach (var model in BiogeochemistryModels)
        {
            if (!planetIds.Contains(model.PlanetId))
            {
                throw new ArgumentException(
                    $"Biogeochemistry model targets planet '{model.PlanetId.Value}', which does not exist in the world.",
                    nameof(world));
            }

            if (!world.Biogeochemistry.Any(
                    state =>
                        state.PlanetId ==
                        model.PlanetId))
            {
                throw new ArgumentException(
                    $"Biogeochemistry model for planet '{model.PlanetId.Value}' requires authoritative biogeochemistry state for that planet.",
                    nameof(world));
            }
        }

        foreach (var model in SeasonalModels)
        {
            if (!planetIds.Contains(model.PlanetId))
            {
                throw new ArgumentException(
                    $"Seasonal model targets planet '{model.PlanetId.Value}', which does not exist in the world.",
                    nameof(world));
            }
        }
    }
}
