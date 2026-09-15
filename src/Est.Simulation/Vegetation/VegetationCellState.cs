using Est.Simulation.Surface;

namespace Est.Simulation.Vegetation;

/// <summary>
/// Authoritative live plant biomass for one planet-surface cell.
///
/// Biomass is represented as kilograms of living plant matter per square
/// meter of surface area. Species structure, dead biomass, nutrients, and
/// edible fractions remain separate future concerns.
/// </summary>
public sealed record VegetationCellState
{
    public VegetationCellState(
        SurfaceCellId cellId,
        double liveBiomassKilogramsPerSquareMeter)
    {
        if (!double.IsFinite(
                liveBiomassKilogramsPerSquareMeter) ||
            liveBiomassKilogramsPerSquareMeter < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(liveBiomassKilogramsPerSquareMeter),
                "Live plant biomass must be finite and nonnegative.");
        }

        CellId =
            cellId;

        LiveBiomassKilogramsPerSquareMeter =
            liveBiomassKilogramsPerSquareMeter;
    }

    public SurfaceCellId CellId { get; }

    public double LiveBiomassKilogramsPerSquareMeter
    {
        get;
    }
}
