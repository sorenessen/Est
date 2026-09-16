using Est.Simulation.Animals;
using Est.Simulation.Organisms;
using Est.Simulation.Planets;

namespace Est.Simulation.Tests.Animals;

public sealed class WolfMaterialGrowthTests
{
    [Fact]
    public void Assimilation_GrowsOnlyToCurrentAgeTarget()
    {
        var parameters =
            new WolfLifecycleParameters();

        var ageSeconds =
            30 *
            WolfLifecycleParameters.SecondsPerDay;

        var wolf =
            new AnimalState(
                AnimalId.New(),
                PlanetId.New(),
                AnimalSpecies.Wolf,
                latitudeDegrees: 0,
                longitudeDegrees: 0,
                material:
                    parameters.NewbornMaterial
                        .ForUnits(1),
                birthTimeSeconds:
                    -ageSeconds);

        var food =
            new OrganismMaterialState(
                250,
                6.25);

        var result =
            WolfMaterialGrowth
                .AssimilateTowardAgeTarget(
                    wolf,
                    currentTimeSeconds: 0,
                    food,
                    parameters);

        var expected =
            parameters
                .MaterialTargetAtAgeSeconds(
                    ageSeconds);

        Assert.Equal(
            expected.LiveBiomassKilograms,
            result.Wolf.Material
                .LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            expected.LiveNitrogenKilograms,
            result.Wolf.Material
                .LiveNitrogenKilograms,
            precision: 10);

        Assert.Equal(
            food.LiveBiomassKilograms,
            result.AssimilatedMaterial
                    .LiveBiomassKilograms +
                result.RemainingFoodMaterial
                    .LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            food.LiveNitrogenKilograms,
            result.AssimilatedMaterial
                    .LiveNitrogenKilograms +
                result.RemainingFoodMaterial
                    .LiveNitrogenKilograms,
            precision: 10);
    }

    [Fact]
    public void Assimilation_DoesNotCreateGrowthWithoutAvailableNitrogen()
    {
        var parameters =
            new WolfLifecycleParameters();

        var wolf =
            new AnimalState(
                AnimalId.New(),
                PlanetId.New(),
                AnimalSpecies.Wolf,
                latitudeDegrees: 0,
                longitudeDegrees: 0,
                material:
                    parameters.NewbornMaterial
                        .ForUnits(1),
                birthTimeSeconds:
                    -30 *
                    WolfLifecycleParameters
                        .SecondsPerDay);

        var food =
            new OrganismMaterialState(
                10,
                0);

        var result =
            WolfMaterialGrowth
                .AssimilateTowardAgeTarget(
                    wolf,
                    currentTimeSeconds: 0,
                    food,
                    parameters);

        Assert.Equal(
            wolf.Material,
            result.Wolf.Material);

        Assert.True(
            result.AssimilatedMaterial.IsEmpty);

        Assert.Equal(
            food,
            result.RemainingFoodMaterial);
    }

    [Fact]
    public void Assimilation_AllowsAdultRecoveryButNeverExceedsMatureTarget()
    {
        var parameters =
            new WolfLifecycleParameters();

        var planetId =
            PlanetId.New();

        var underweightAdult =
            new AnimalState(
                AnimalId.New(),
                planetId,
                AnimalSpecies.Wolf,
                latitudeDegrees: 0,
                longitudeDegrees: 0,
                material:
                    new OrganismMaterialState(
                        liveBiomassKilograms: 38,
                        liveNitrogenKilograms: 0.95),
                birthTimeSeconds:
                    -4 *
                    WolfLifecycleParameters
                        .SecondsPerYear);

        var food =
            new OrganismMaterialState(
                liveBiomassKilograms: 250,
                liveNitrogenKilograms: 6.25);

        var result =
            WolfMaterialGrowth
                .AssimilateTowardAgeTarget(
                    underweightAdult,
                    currentTimeSeconds: 0,
                    food,
                    parameters);

        Assert.Equal(
            parameters.MatureMaterial
                .LiveBiomassKilogramsPerUnit,
            result.Wolf.Material
                .LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            parameters.MatureMaterial
                .LiveNitrogenKilogramsPerUnit,
            result.Wolf.Material
                .LiveNitrogenKilograms,
            precision: 10);

        Assert.Equal(
            2,
            result.AssimilatedMaterial
                .LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            0.05,
            result.AssimilatedMaterial
                .LiveNitrogenKilograms,
            precision: 10);

        Assert.Equal(
            food.LiveBiomassKilograms,
            result.AssimilatedMaterial
                    .LiveBiomassKilograms +
                result.RemainingFoodMaterial
                    .LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            food.LiveNitrogenKilograms,
            result.AssimilatedMaterial
                    .LiveNitrogenKilograms +
                result.RemainingFoodMaterial
                    .LiveNitrogenKilograms,
            precision: 10);
    }
}
