using Est.Simulation.Biogeochemistry;
using Est.Simulation.Planets;
using Est.Simulation.Surface;

namespace Est.Simulation.Organisms;

/// <summary>
/// One authoritative removal of live vegetation biomass by feeding.
///
/// Consumed biomass can either be retained as new consumer tissue or
/// metabolized out of the tracked organic-biomass pool. Plant-tissue nitrogen
/// retained in consumer tissue is not returned to soil; the remainder returns
/// to the authoritative plant-available nitrogen pool of the source cell.
/// </summary>
public sealed record VegetationFeedingEvent
{
    public VegetationFeedingEvent(
        SurfaceCellId sourceCellId,
        double consumedBiomassKilograms,
        double assimilatedBiomassKilograms = 0,
        double assimilatedNitrogenKilograms = 0)
    {
        if (!double.IsFinite(consumedBiomassKilograms) ||
            consumedBiomassKilograms < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(consumedBiomassKilograms),
                "Consumed vegetation biomass must be finite and non-negative.");
        }

        if (!double.IsFinite(assimilatedBiomassKilograms) ||
            assimilatedBiomassKilograms < 0 ||
            assimilatedBiomassKilograms >
                consumedBiomassKilograms)
        {
            throw new ArgumentOutOfRangeException(
                nameof(assimilatedBiomassKilograms),
                "Assimilated biomass must be finite, non-negative, and cannot exceed consumed biomass.");
        }

        if (!double.IsFinite(assimilatedNitrogenKilograms) ||
            assimilatedNitrogenKilograms < 0 ||
            assimilatedNitrogenKilograms >
                assimilatedBiomassKilograms)
        {
            throw new ArgumentOutOfRangeException(
                nameof(assimilatedNitrogenKilograms),
                "Assimilated nitrogen must be finite, non-negative, and cannot exceed assimilated biomass.");
        }

        SourceCellId = sourceCellId;
        ConsumedBiomassKilograms =
            consumedBiomassKilograms;
        AssimilatedBiomassKilograms =
            assimilatedBiomassKilograms;
        AssimilatedNitrogenKilograms =
            assimilatedNitrogenKilograms;
    }

    public SurfaceCellId SourceCellId { get; }

    public double ConsumedBiomassKilograms { get; }

    public double AssimilatedBiomassKilograms { get; }

    public double AssimilatedNitrogenKilograms { get; }
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

            var consumedNitrogenKilograms =
                feedingEvent.ConsumedBiomassKilograms *
                plantNitrogenKilogramsPerKilogramLiveBiomass;

            if (feedingEvent.AssimilatedNitrogenKilograms >
                consumedNitrogenKilograms)
            {
                throw new InvalidOperationException(
                    "Vegetation feeding cannot assimilate more nitrogen than the consumed plant tissue contains.");
            }

            var returnedNitrogenKilograms =
                consumedNitrogenKilograms -
                feedingEvent.AssimilatedNitrogenKilograms;

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
