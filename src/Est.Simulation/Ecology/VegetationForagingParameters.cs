namespace Est.Simulation.Ecology;

/// <summary>
/// Defines the provisional bridge between authoritative live plant biomass and
/// the normalized energy reserve used by the current person-survival slice.
///
/// This is intentionally not a mature nutrition or edible-fraction model.
/// Species composition, edible plant parts, nutritional composition, and diet
/// suitability remain later ecological concerns.
///
/// KilogramsLiveBiomassPerEnergyReserveUnit defines how much authoritative live
/// plant biomass must be removed to restore one complete normalized person
/// energy-reserve unit.
///
/// MaximumHarvestKilogramsPerPersonPerDay bounds the amount of live biomass one
/// person can remove during one simulated day.
/// </summary>
public sealed record VegetationForagingParameters
{
    public VegetationForagingParameters(
        double kilogramsLiveBiomassPerEnergyReserveUnit,
        double maximumHarvestKilogramsPerPersonPerDay)
    {
        if (!double.IsFinite(
                kilogramsLiveBiomassPerEnergyReserveUnit) ||
            kilogramsLiveBiomassPerEnergyReserveUnit <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(kilogramsLiveBiomassPerEnergyReserveUnit),
                "Live biomass per energy-reserve unit must be finite and greater than zero.");
        }

        if (!double.IsFinite(
                maximumHarvestKilogramsPerPersonPerDay) ||
            maximumHarvestKilogramsPerPersonPerDay <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumHarvestKilogramsPerPersonPerDay),
                "Maximum daily biomass harvest must be finite and greater than zero.");
        }

        KilogramsLiveBiomassPerEnergyReserveUnit =
            kilogramsLiveBiomassPerEnergyReserveUnit;

        MaximumHarvestKilogramsPerPersonPerDay =
            maximumHarvestKilogramsPerPersonPerDay;
    }

    public double KilogramsLiveBiomassPerEnergyReserveUnit { get; }

    public double MaximumHarvestKilogramsPerPersonPerDay { get; }
}
