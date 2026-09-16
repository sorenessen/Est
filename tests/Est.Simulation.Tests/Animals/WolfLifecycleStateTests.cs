using Est.Simulation.Animals;
using Est.Simulation.Organisms;
using Est.Simulation.Planets;

namespace Est.Simulation.Tests.Animals;

public sealed class WolfLifecycleStateTests
{
    [Fact]
    public void WolfWithoutExplicitLifecycle_GetsUnknownLifecycle()
    {
        var wolf =
            new AnimalState(
                AnimalId.New(),
                PlanetId.New(),
                AnimalSpecies.Wolf,
                latitudeDegrees: 0,
                longitudeDegrees: 0);

        Assert.NotNull(
            wolf.WolfLifecycle);

        Assert.Equal(
            WolfSex.Unknown,
            wolf.WolfLifecycle!.Sex);

        Assert.Null(
            wolf.WolfLifecycle.Pregnancy);
    }

    [Fact]
    public void Pregnancy_IsOnlyValidForFemaleWolf()
    {
        var pregnancy =
            new WolfPregnancyState(
                conceptionTimeSeconds: 100,
                fatherId: AnimalId.New());

        Assert.Throws<ArgumentException>(
            () =>
                new WolfLifecycleState(
                    WolfSex.Male,
                    pregnancy));

        Assert.Throws<ArgumentException>(
            () =>
                new WolfLifecycleState(
                    WolfSex.Unknown,
                    pregnancy));

        var female =
            new WolfLifecycleState(
                WolfSex.Female,
                pregnancy);

        Assert.Same(
            pregnancy,
            female.Pregnancy);
    }

    [Fact]
    public void AnimalState_PreservesWolfLifecycleAcrossStateChanges()
    {
        var lifecycle =
            new WolfLifecycleState(
                WolfSex.Female);

        var wolf =
            new AnimalState(
                AnimalId.New(),
                PlanetId.New(),
                AnimalSpecies.Wolf,
                latitudeDegrees: 10,
                longitudeDegrees: 20,
                wolfLifecycle:
                    lifecycle);

        var moved =
            wolf.WithState(
                latitudeDegrees: 11,
                longitudeDegrees: 21,
                energyReserve: 0.5,
                health: 0.9,
                activity:
                    AnimalActivity.Traveling);

        Assert.Same(
            lifecycle,
            moved.WolfLifecycle);
    }

    [Fact]
    public void DefaultPolicy_UsesDistinctGrowthAndReproductiveMaturity()
    {
        var parameters =
            new WolfLifecycleParameters();

        Assert.Equal(
            180 *
            WolfLifecycleParameters.SecondsPerDay,
            parameters
                .MaterialMaturityAgeSeconds);

        Assert.Equal(
            180 *
            WolfLifecycleParameters.SecondsPerDay,
            parameters
                .PackHuntingAgeSeconds);

        Assert.Equal(
            2 *
            WolfLifecycleParameters.SecondsPerYear,
            parameters
                .LifecycleTiming
                .MaturityAgeSeconds);

        Assert.Equal(
            63 *
            WolfLifecycleParameters.SecondsPerDay,
            parameters.GestationSeconds);

        Assert.Equal(
            4,
            parameters.MinimumLitterSize);

        Assert.Equal(
            6,
            parameters.MaximumLitterSize);

        Assert.True(
            parameters.SeasonalStrategies
                .HasFlag(
                    OrganismSeasonalStrategy
                        .Breeding));

        Assert.False(
            parameters.SeasonalStrategies
                .HasFlag(
                    OrganismSeasonalStrategy
                        .Hibernation));
    }

    [Fact]
    public void HuntingAge_IsIndependentFromMaterialAndReproductiveMaturity()
    {
        var parameters =
            new WolfLifecycleParameters(
                maturityAgeSeconds:
                    2 *
                    WolfLifecycleParameters
                        .SecondsPerYear,
                reproductiveAgeMinimumSeconds:
                    2 *
                    WolfLifecycleParameters
                        .SecondsPerYear,
                materialMaturityAgeSeconds:
                    180 *
                    WolfLifecycleParameters
                        .SecondsPerDay,
                packHuntingAgeSeconds:
                    210 *
                    WolfLifecycleParameters
                        .SecondsPerDay);

        Assert.Equal(
            180 *
            WolfLifecycleParameters.SecondsPerDay,
            parameters.MaterialMaturityAgeSeconds);

        Assert.Equal(
            210 *
            WolfLifecycleParameters.SecondsPerDay,
            parameters.PackHuntingAgeSeconds);

        Assert.Equal(
            2 *
            WolfLifecycleParameters.SecondsPerYear,
            parameters
                .LifecycleTiming
                .ReproductiveAgeMinimumSeconds);
    }

    [Fact]
    public void MaterialTarget_ReachesMatureBodyBeforeReproductiveMaturity()
    {
        var parameters =
            new WolfLifecycleParameters();

        var newborn =
            parameters.MaterialTargetAtAgeSeconds(
                0);

        var mature =
            parameters.MaterialTargetAtAgeSeconds(
                parameters
                    .MaterialMaturityAgeSeconds);

        Assert.Equal(
            0.45,
            newborn.LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            0.01125,
            newborn.LiveNitrogenKilograms,
            precision: 10);

        Assert.Equal(
            40,
            mature.LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            1,
            mature.LiveNitrogenKilograms,
            precision: 10);
    }
}
