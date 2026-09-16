using Est.Simulation.Biogeochemistry;
using Est.Simulation.Planets;
using Est.Simulation.Surface;

namespace Est.Simulation.Organisms;

/// <summary>
/// One authoritative consumption of material-bearing organism prey.
///
/// Consumed prey material can either be retained as new consumer tissue or
/// metabolized out of the tracked organic-biomass pool. Consumed nitrogen
/// retained in consumer tissue is not returned to soil; the remainder returns
/// to the authoritative plant-available nitrogen pool of the surface cell where
/// consumption occurred.
/// </summary>
public sealed record OrganismConsumptionEvent
{
    public OrganismConsumptionEvent(
        double latitudeDegrees,
        double longitudeDegrees,
        OrganismMaterialState material,
        double assimilatedBiomassKilograms = 0,
        double assimilatedNitrogenKilograms = 0)
    {
        ArgumentNullException.ThrowIfNull(
            material);

        if (!double.IsFinite(latitudeDegrees) ||
            latitudeDegrees < -90 ||
            latitudeDegrees > 90)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitudeDegrees));
        }

        if (!double.IsFinite(longitudeDegrees) ||
            longitudeDegrees < -180 ||
            longitudeDegrees > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitudeDegrees));
        }

        if (!double.IsFinite(
                assimilatedBiomassKilograms) ||
            assimilatedBiomassKilograms < 0 ||
            assimilatedBiomassKilograms >
                material.LiveBiomassKilograms)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    assimilatedBiomassKilograms),
                "Assimilated biomass must be finite, non-negative, and cannot exceed consumed prey biomass.");
        }

        if (!double.IsFinite(
                assimilatedNitrogenKilograms) ||
            assimilatedNitrogenKilograms < 0 ||
            assimilatedNitrogenKilograms >
                material.LiveNitrogenKilograms ||
            assimilatedNitrogenKilograms >
                assimilatedBiomassKilograms)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    assimilatedNitrogenKilograms),
                "Assimilated nitrogen must be finite, non-negative, and cannot exceed consumed prey nitrogen or assimilated biomass.");
        }

        LatitudeDegrees =
            latitudeDegrees;

        LongitudeDegrees =
            longitudeDegrees;

        Material =
            material;

        AssimilatedBiomassKilograms =
            assimilatedBiomassKilograms;

        AssimilatedNitrogenKilograms =
            assimilatedNitrogenKilograms;
    }

    public double LatitudeDegrees { get; }

    public double LongitudeDegrees { get; }

    public OrganismMaterialState Material { get; }

    public double AssimilatedBiomassKilograms
    {
        get;
    }

    public double AssimilatedNitrogenKilograms
    {
        get;
    }

    public double RespiredBiomassKilograms =>
        Math.Max(
            0,
            Material.LiveBiomassKilograms -
            AssimilatedBiomassKilograms);

    public double ReturnedNitrogenKilograms =>
        Math.Max(
            0,
            Material.LiveNitrogenKilograms -
            AssimilatedNitrogenKilograms);
}

/// <summary>
/// Shared physical feeding semantics for consumed organism prey.
/// </summary>
public static class OrganismConsumptionMaterialTransfer
{
    public static PlanetBiogeochemistryState
        ReturnConsumedNitrogen(
            PlanetState planet,
            PlanetBiogeochemistryState biogeochemistry,
            IEnumerable<OrganismConsumptionEvent>
                consumptionEvents)
    {
        ArgumentNullException.ThrowIfNull(
            planet);

        ArgumentNullException.ThrowIfNull(
            biogeochemistry);

        ArgumentNullException.ThrowIfNull(
            consumptionEvents);

        biogeochemistry.ValidateFor(
            planet);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                biogeochemistry.GridDefinition);

        var availableNitrogenByCellId =
            biogeochemistry.Cells.ToDictionary(
                cell =>
                    cell.CellId,
                cell =>
                    cell
                        .PlantAvailableNitrogenKilogramsPerSquareMeter);

        foreach (var consumptionEvent in
                 consumptionEvents)
        {
            if (consumptionEvent is null)
            {
                throw new ArgumentException(
                    "Organism consumption events cannot contain null entries.",
                    nameof(consumptionEvents));
            }

            if (consumptionEvent.Material.IsEmpty)
            {
                continue;
            }

            var sourceCell =
                grid.LocateCell(
                    consumptionEvent.LatitudeDegrees,
                    consumptionEvent.LongitudeDegrees);

            availableNitrogenByCellId[
                sourceCell.Id] +=
                consumptionEvent
                    .ReturnedNitrogenKilograms /
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
