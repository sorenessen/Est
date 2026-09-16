using Est.Simulation.Biogeochemistry;
using Est.Simulation.Planets;
using Est.Simulation.Surface;

namespace Est.Simulation.Organisms;

public sealed record OrganismMortalityDeposit
{
    public OrganismMortalityDeposit(
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

    public static OrganismMortalityDeposit FromRemovedFraction(
        double latitudeDegrees,
        double longitudeDegrees,
        OrganismMaterialState sourceMaterial,
        double removedFraction)
    {
        ArgumentNullException.ThrowIfNull(
            sourceMaterial);

        return new OrganismMortalityDeposit(
            latitudeDegrees,
            longitudeDegrees,
            sourceMaterial.RetainFraction(
                removedFraction));
    }
}

/// <summary>
/// Shared physical death semantics for material-bearing organism
/// representations. Dead live biomass and nitrogen are deposited into the
/// authoritative detrital pools of the surface cell where death occurred.
/// </summary>
public static class OrganismMortalityDetritusTransfer
{
    public static PlanetBiogeochemistryState Apply(
        PlanetState planet,
        PlanetBiogeochemistryState biogeochemistry,
        IEnumerable<OrganismMortalityDeposit> deposits)
    {
        ArgumentNullException.ThrowIfNull(planet);
        ArgumentNullException.ThrowIfNull(biogeochemistry);
        ArgumentNullException.ThrowIfNull(deposits);

        biogeochemistry.ValidateFor(
            planet);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                biogeochemistry.GridDefinition);

        var biomassByCellId =
            biogeochemistry.Cells.ToDictionary(
                cell =>
                    cell.CellId,
                cell =>
                    cell.DetritalBiomassKilogramsPerSquareMeter);

        var nitrogenByCellId =
            biogeochemistry.Cells.ToDictionary(
                cell =>
                    cell.CellId,
                cell =>
                    cell.DetritalNitrogenKilogramsPerSquareMeter);

        foreach (var deposit in deposits)
        {
            if (deposit is null)
            {
                throw new ArgumentException(
                    "Mortality deposits cannot contain null entries.",
                    nameof(deposits));
            }

            if (deposit.Material.IsEmpty)
            {
                continue;
            }

            var cell =
                grid.LocateCell(
                    deposit.LatitudeDegrees,
                    deposit.LongitudeDegrees);

            biomassByCellId[
                cell.Id] +=
                deposit.Material.LiveBiomassKilograms /
                cell.AreaSquareMeters;

            nitrogenByCellId[
                cell.Id] +=
                deposit.Material.LiveNitrogenKilograms /
                cell.AreaSquareMeters;
        }

        return new PlanetBiogeochemistryState(
            biogeochemistry.PlanetId,
            biogeochemistry.GridDefinition,
            biogeochemistry.Cells.Select(
                cell =>
                    new BiogeochemistryCellState(
                        cell.CellId,
                        biomassByCellId[
                            cell.CellId],
                        nitrogenByCellId[
                            cell.CellId],
                        cell.PlantAvailableNitrogenKilogramsPerSquareMeter)));
    }
}
