using Est.Simulation.Hydrology;
using Est.Simulation.Planets;
using Est.Simulation.Terrain;

namespace Est.Simulation.Vegetation;

/// <summary>
/// Builds initial authoritative plant biomass from the established physical
/// surface state.
///
/// Flooded cells begin without terrestrial live biomass. Dry cells receive
/// the requested initial live biomass uniformly. Subsequent productivity is
/// owned by the causal vegetation system.
/// </summary>
public static class PlanetVegetationInitializer
{
    public static PlanetVegetationState FromDrySurfaceBiomass(
        PlanetState planet,
        PlanetTerrainState terrain,
        PlanetHydrologyState hydrology,
        double initialLiveBiomassKilogramsPerSquareMeter)
    {
        ArgumentNullException.ThrowIfNull(
            planet);

        ArgumentNullException.ThrowIfNull(
            terrain);

        ArgumentNullException.ThrowIfNull(
            hydrology);

        if (!double.IsFinite(
                initialLiveBiomassKilogramsPerSquareMeter) ||
            initialLiveBiomassKilogramsPerSquareMeter < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    initialLiveBiomassKilogramsPerSquareMeter),
                "Initial live plant biomass must be finite and nonnegative.");
        }

        terrain.ValidateFor(
            planet);

        hydrology.ValidateFor(
            planet);

        if (terrain.GridDefinition !=
            hydrology.GridDefinition)
        {
            throw new ArgumentException(
                "Generated vegetation requires terrain and hydrology on the same surface grid.");
        }

        var standingWater =
            PlanetStandingWaterState.Derive(
                planet,
                terrain,
                hydrology);

        return new PlanetVegetationState(
            planet.Id,
            terrain.GridDefinition,
            standingWater.Cells.Select(
                cell =>
                    new VegetationCellState(
                        cell.CellId,
                        cell.IsFlooded
                            ? 0
                            : initialLiveBiomassKilogramsPerSquareMeter)));
    }
}
