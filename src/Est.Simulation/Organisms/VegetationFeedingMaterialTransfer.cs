using Est.Simulation.Biogeochemistry;
using Est.Simulation.Planets;
using Est.Simulation.Surface;

namespace Est.Simulation.Organisms;

/// <summary>
/// One authoritative removal of live vegetation biomass by feeding.
/// The consumed organic biomass is treated as metabolized out of the tracked
/// organic-biomass pool. Its implied plant-tissue nitrogen is returned to the
/// authoritative plant-available nitrogen pool of the source surface cell.
/// </summary>
public sealed record VegetationFeedingEvent
{
    public VegetationFeedingEvent(
        SurfaceCellId sourceCellId,
        double consumedBiomassKilograms)
    {
        if (!double.IsFinite(consumedBiomassKilograms) ||
            consumedBiomassKilograms < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(consumedBiomassKilograms),
                "Consumed vegetation biomass must be finite and non-negative.");
        }

        SourceCellId = sourceCellId;
        ConsumedBiomassKilograms =
            consumedBiomassKilograms;
    }

    public SurfaceCellId SourceCellId { get; }

    public double ConsumedBiomassKilograms { get; }
}

/// <summary>
/// Shared physical feeding semantics for vegetation-backed consumers.
/// </summary>
public static class VegetationFeedingMaterialTransfer
{
    public static PlanetBiogeochemistryState
        ReturnConsumedNitrogen(
            PlanetState planet,
            PlanetBiogeochemistryState biogeochemistry,
            IEnumerable<VegetationFeedingEvent> feedingEvents,
            double plantNitrogenKilogramsPerKilogramLiveBiomass)
    {
        ArgumentNullException.ThrowIfNull(planet);
        ArgumentNullException.ThrowIfNull(biogeochemistry);
        ArgumentNullException.ThrowIfNull(feedingEvents);

        if (!double.IsFinite(
                plantNitrogenKilogramsPerKilogramLiveBiomass) ||
            plantNitrogenKilogramsPerKilogramLiveBiomass <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    plantNitrogenKilogramsPerKilogramLiveBiomass),
                "Plant-tissue nitrogen ratio must be finite and greater than zero.");
        }

        biogeochemistry.ValidateFor(
            planet);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                biogeochemistry.GridDefinition);

        var surfaceCellsById =
            grid.Cells.ToDictionary(
                cell =>
                    cell.Id);

        var availableNitrogenByCellId =
            biogeochemistry.Cells.ToDictionary(
                cell =>
                    cell.CellId,
                cell =>
                    cell
                        .PlantAvailableNitrogenKilogramsPerSquareMeter);

        foreach (var feedingEvent in feedingEvents)
        {
            if (feedingEvent is null)
            {
                throw new ArgumentException(
                    "Vegetation feeding events cannot contain null entries.",
                    nameof(feedingEvents));
            }

            if (feedingEvent.ConsumedBiomassKilograms == 0)
            {
                continue;
            }

            if (!surfaceCellsById.TryGetValue(
                    feedingEvent.SourceCellId,
                    out var sourceCell))
            {
                throw new ArgumentException(
                    "Vegetation feeding event references a surface cell outside the authoritative biogeochemistry grid.",
                    nameof(feedingEvents));
            }

            var returnedNitrogenKilograms =
                feedingEvent.ConsumedBiomassKilograms *
                plantNitrogenKilogramsPerKilogramLiveBiomass;

            availableNitrogenByCellId[
                sourceCell.Id] +=
                returnedNitrogenKilograms /
                sourceCell.AreaSquareMeters;
        }

        return new PlanetBiogeochemistryState(
            biogeochemistry.PlanetId,
            biogeochemistry.GridDefinition,
            biogeochemistry.Cells.Select(
                cell =>
                    new BiogeochemistryCellState(
                        cell.CellId,
                        cell.DetritalBiomassKilogramsPerSquareMeter,
                        cell.DetritalNitrogenKilogramsPerSquareMeter,
                        availableNitrogenByCellId[
                            cell.CellId])));
    }
}
