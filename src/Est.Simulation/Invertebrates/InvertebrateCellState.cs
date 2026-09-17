using Est.Simulation.Surface;

namespace Est.Simulation.Invertebrates;

/// <summary>
/// Authoritative live aggregate invertebrate biomass for one planet-surface
/// cell.
///
/// This first-pass state deliberately does not encode species, individuals,
/// age structure, dead biomass, or trophic role.
/// </summary>
public sealed record InvertebrateCellState
{
    public InvertebrateCellState(
        SurfaceCellId cellId,
        double liveBiomassKilogramsPerSquareMeter,
        double liveNitrogenKilogramsPerSquareMeter = 0)
    {
        if (!double.IsFinite(
                liveBiomassKilogramsPerSquareMeter) ||
            liveBiomassKilogramsPerSquareMeter < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(liveBiomassKilogramsPerSquareMeter),
                "Live invertebrate biomass must be finite and nonnegative.");
        }

        if (!double.IsFinite(
                liveNitrogenKilogramsPerSquareMeter) ||
            liveNitrogenKilogramsPerSquareMeter < 0 ||
            liveNitrogenKilogramsPerSquareMeter >
                liveBiomassKilogramsPerSquareMeter)
        {
            throw new ArgumentOutOfRangeException(
                nameof(liveNitrogenKilogramsPerSquareMeter),
                "Live invertebrate nitrogen must be finite, nonnegative, and cannot exceed live biomass.");
        }

        CellId =
            cellId;

        LiveBiomassKilogramsPerSquareMeter =
            liveBiomassKilogramsPerSquareMeter;

        LiveNitrogenKilogramsPerSquareMeter =
            liveNitrogenKilogramsPerSquareMeter;
    }

    public SurfaceCellId CellId { get; }

    public double LiveBiomassKilogramsPerSquareMeter
    {
        get;
    }

    public double LiveNitrogenKilogramsPerSquareMeter
    {
        get;
    }
}
