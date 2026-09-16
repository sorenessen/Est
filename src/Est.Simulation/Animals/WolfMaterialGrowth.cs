using Est.Simulation.Organisms;

namespace Est.Simulation.Animals;

public sealed record WolfMaterialGrowthResult(
    AnimalState Wolf,
    OrganismMaterialState AssimilatedMaterial,
    OrganismMaterialState RemainingFoodMaterial);

public static class WolfMaterialGrowth
{
    public static WolfMaterialGrowthResult
        AssimilateTowardAgeTarget(
            AnimalState wolf,
            long currentTimeSeconds,
            OrganismMaterialState availableFoodMaterial,
            WolfLifecycleParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(
            wolf);

        ArgumentNullException.ThrowIfNull(
            availableFoodMaterial);

        ArgumentNullException.ThrowIfNull(
            parameters);

        if (wolf.Species != AnimalSpecies.Wolf)
        {
            throw new ArgumentException(
                "Wolf material growth requires a wolf.",
                nameof(wolf));
        }

        var ageSeconds =
            OrganismLifecycleClock.AgeSeconds(
                wolf.BirthTimeSeconds,
                currentTimeSeconds);

        var target =
            parameters.MaterialTargetAtAgeSeconds(
                ageSeconds);

        var biomassDeficit =
            Math.Max(
                0,
                target.LiveBiomassKilograms -
                wolf.Material.LiveBiomassKilograms);

        var nitrogenDeficit =
            Math.Max(
                0,
                target.LiveNitrogenKilograms -
                wolf.Material.LiveNitrogenKilograms);

        if (biomassDeficit <= 0 ||
            availableFoodMaterial.IsEmpty)
        {
            return new WolfMaterialGrowthResult(
                wolf,
                new OrganismMaterialState(0, 0),
                availableFoodMaterial);
        }

        var assimilatedFraction =
            Math.Min(
                1,
                availableFoodMaterial
                    .LiveBiomassKilograms /
                biomassDeficit);

        if (nitrogenDeficit > 0)
        {
            assimilatedFraction =
                Math.Min(
                    assimilatedFraction,
                    availableFoodMaterial
                        .LiveNitrogenKilograms /
                    nitrogenDeficit);
        }

        if (assimilatedFraction <= 0)
        {
            return new WolfMaterialGrowthResult(
                wolf,
                new OrganismMaterialState(0, 0),
                availableFoodMaterial);
        }

        var assimilated =
            new OrganismMaterialState(
                biomassDeficit *
                assimilatedFraction,
                nitrogenDeficit *
                assimilatedFraction);

        var nextMaterial =
            new OrganismMaterialState(
                wolf.Material
                    .LiveBiomassKilograms +
                assimilated
                    .LiveBiomassKilograms,
                wolf.Material
                    .LiveNitrogenKilograms +
                assimilated
                    .LiveNitrogenKilograms);

        return new WolfMaterialGrowthResult(
            wolf.WithMaterial(
                nextMaterial),
            assimilated,
            availableFoodMaterial.Subtract(
                assimilated));
    }
}
