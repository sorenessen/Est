namespace Est.Simulation.Animals;

/// <summary>
/// Defines the provisional bridge between authoritative consumed prey biomass
/// and the normalized energy reserve used by the current wolf-survival model.
///
/// KilogramsMetabolizedBiomassPerEnergyReserveUnit defines how much consumed
/// prey biomass must be metabolized to restore one complete normalized wolf
/// energy-reserve unit.
///
/// This intentionally remains separate from lifecycle material growth.
/// Biomass retained as new wolf tissue is not also available for maintenance
/// energy.
/// </summary>
public sealed record WolfFeedingParameters
{
    public WolfFeedingParameters(
        double kilogramsMetabolizedBiomassPerEnergyReserveUnit =
            40)
    {
        if (!double.IsFinite(
                kilogramsMetabolizedBiomassPerEnergyReserveUnit) ||
            kilogramsMetabolizedBiomassPerEnergyReserveUnit <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    kilogramsMetabolizedBiomassPerEnergyReserveUnit),
                "Metabolized biomass per energy-reserve unit must be finite and greater than zero.");
        }

        KilogramsMetabolizedBiomassPerEnergyReserveUnit =
            kilogramsMetabolizedBiomassPerEnergyReserveUnit;
    }

    public double
        KilogramsMetabolizedBiomassPerEnergyReserveUnit
    {
        get;
    }

    public double EnergyReserveFromMetabolizedBiomass(
        double metabolizedBiomassKilograms)
    {
        if (!double.IsFinite(
                metabolizedBiomassKilograms) ||
            metabolizedBiomassKilograms < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    metabolizedBiomassKilograms),
                "Metabolized biomass must be finite and non-negative.");
        }

        return metabolizedBiomassKilograms /
            KilogramsMetabolizedBiomassPerEnergyReserveUnit;
    }
}
