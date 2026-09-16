using Est.Simulation.Surface;

namespace Est.Simulation.Biogeochemistry;

/// <summary>
/// Authoritative first-pass biogeochemical state for one planet-surface cell.
///
/// Detrital biomass tracks dead organic matter independently from its nitrogen
/// content. Plant-available nitrogen is a separate inorganic/available pool so
/// later decomposition and plant uptake can preserve explicit nitrogen balance.
/// Carbon, phosphorus, microbial biomass, fungi, and detailed soil chemistry
/// remain later extensions.
/// </summary>
public sealed record BiogeochemistryCellState
{
    public BiogeochemistryCellState(
        SurfaceCellId cellId,
        double detritalBiomassKilogramsPerSquareMeter,
        double detritalNitrogenKilogramsPerSquareMeter,
        double plantAvailableNitrogenKilogramsPerSquareMeter)
    {
        ValidateNonnegativeFinite(
            detritalBiomassKilogramsPerSquareMeter,
            nameof(detritalBiomassKilogramsPerSquareMeter));

        ValidateNonnegativeFinite(
            detritalNitrogenKilogramsPerSquareMeter,
            nameof(detritalNitrogenKilogramsPerSquareMeter));

        ValidateNonnegativeFinite(
            plantAvailableNitrogenKilogramsPerSquareMeter,
            nameof(plantAvailableNitrogenKilogramsPerSquareMeter));

        CellId =
            cellId;

        DetritalBiomassKilogramsPerSquareMeter =
            detritalBiomassKilogramsPerSquareMeter;

        DetritalNitrogenKilogramsPerSquareMeter =
            detritalNitrogenKilogramsPerSquareMeter;

        PlantAvailableNitrogenKilogramsPerSquareMeter =
            plantAvailableNitrogenKilogramsPerSquareMeter;
    }

    public SurfaceCellId CellId { get; }

    public double DetritalBiomassKilogramsPerSquareMeter
    {
        get;
    }

    public double DetritalNitrogenKilogramsPerSquareMeter
    {
        get;
    }

    public double PlantAvailableNitrogenKilogramsPerSquareMeter
    {
        get;
    }

    private static void ValidateNonnegativeFinite(
        double value,
        string parameterName)
    {
        if (!double.IsFinite(value) ||
            value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Biogeochemical pools must be finite and nonnegative.");
        }
    }
}
