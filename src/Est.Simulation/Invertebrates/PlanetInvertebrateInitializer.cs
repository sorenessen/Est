using Est.Simulation.Planets;
using Est.Simulation.Vegetation;

namespace Est.Simulation.Invertebrates;

/// <summary>
/// Builds initial aggregate invertebrate biomass from authoritative live
/// vegetation.
///
/// Vegetation is treated as an ecological-support signal rather than direct
/// consumed plant mass. This avoids assigning a trophic role to the aggregate
/// invertebrate state.
/// </summary>
public static class PlanetInvertebrateInitializer
{
    public static PlanetInvertebrateState FromVegetationSupport(
        PlanetState planet,
        PlanetVegetationState vegetation,
        InvertebrateModelParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(
            planet);

        ArgumentNullException.ThrowIfNull(
            vegetation);

        ArgumentNullException.ThrowIfNull(
            parameters);

        vegetation.ValidateFor(
            planet);

        return new PlanetInvertebrateState(
            planet.Id,
            vegetation.GridDefinition,
            vegetation.Cells.Select(
                cell =>
                    {
                        var liveBiomass =
                            cell.LiveBiomassKilogramsPerSquareMeter *
                            parameters
                                .CarryingCapacityKilogramsPerKilogramLiveVegetation *
                            parameters
                                .InitialFractionOfLocalCarryingCapacity;

                        return new InvertebrateCellState(
                            cell.CellId,
                            liveBiomass,
                            liveBiomass *
                            parameters
                                .LiveNitrogenKilogramsPerKilogramLiveBiomass);
                    }));
    }
}
