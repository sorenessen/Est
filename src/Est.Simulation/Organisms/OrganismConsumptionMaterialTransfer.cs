using Est.Simulation.Biogeochemistry;
using Est.Simulation.Planets;
using Est.Simulation.Surface;

namespace Est.Simulation.Organisms;

/// <summary>
/// One authoritative consumption of material-bearing organism prey.
///
/// At the current coarse feeding resolution, consumed organic biomass is
/// treated as metabolized out of the tracked organic-biomass pool. Its
/// tracked nitrogen is returned to the authoritative plant-available
/// nitrogen pool of the surface cell where consumption occurred.
/// </summary>
public sealed record OrganismConsumptionEvent
{
    public OrganismConsumptionEvent(
        double latitudeDegrees,
        double longitudeDegrees,
        OrganismMaterialState material)
    {
        ArgumentNullException.ThrowIfNull(material);

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

        LatitudeDegrees = latitudeDegrees;
        LongitudeDegrees = longitudeDegrees;
        Material = material;
    }

    public double LatitudeDegrees { get; }

    public double LongitudeDegrees { get; }

    public OrganismMaterialState Material { get; }
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
            IEnumerable<OrganismConsumptionEvent> consumptionEvents)
    {
        ArgumentNullException.ThrowIfNull(planet);
        ArgumentNullException.ThrowIfNull(biogeochemistry);
        ArgumentNullException.ThrowIfNull(consumptionEvents);

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

        foreach (var consumptionEvent in consumptionEvents)
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
                    .Material
                    .LiveNitrogenKilograms /
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
