using Est.Simulation.Organisms;

namespace Est.Simulation.Tests.Organisms;

public sealed class OrganismLifecycleSemanticsTests
{
    [Fact]
    public void AgeSeconds_UsesAuthoritativeSimulationTime()
    {
        Assert.Equal(
            750,
            OrganismLifecycleClock.AgeSeconds(
                birthTimeSeconds: -250,
                currentTimeSeconds: 500));
    }

    [Fact]
    public void AgeSeconds_ClampsTimeBeforeBirthToZero()
    {
        Assert.Equal(
            0,
            OrganismLifecycleClock.AgeSeconds(
                birthTimeSeconds: 500,
                currentTimeSeconds: 400));
    }

    [Fact]
    public void StageAtAgeSeconds_UsesMaturityAndSenescenceBoundaries()
    {
        var timing =
            new OrganismLifecycleTiming(
                maturityAgeSeconds: 100,
                reproductiveAgeMinimumSeconds: 120,
                reproductiveAgeMaximumSeconds: 400,
                senescenceAgeSeconds: 500);

        Assert.Equal(
            OrganismLifecycleStage.Immature,
            timing.StageAtAgeSeconds(99));

        Assert.Equal(
            OrganismLifecycleStage.Mature,
            timing.StageAtAgeSeconds(100));

        Assert.Equal(
            OrganismLifecycleStage.Mature,
            timing.StageAtAgeSeconds(499));

        Assert.Equal(
            OrganismLifecycleStage.Senescent,
            timing.StageAtAgeSeconds(500));
    }

    [Fact]
    public void ReproductiveEligibility_UsesInclusiveConfiguredWindow()
    {
        var timing =
            new OrganismLifecycleTiming(
                maturityAgeSeconds: 100,
                reproductiveAgeMinimumSeconds: 120,
                reproductiveAgeMaximumSeconds: 400);

        Assert.False(
            timing.IsReproductivelyEligibleAtAgeSeconds(
                119));

        Assert.True(
            timing.IsReproductivelyEligibleAtAgeSeconds(
                120));

        Assert.True(
            timing.IsReproductivelyEligibleAtAgeSeconds(
                400));

        Assert.False(
            timing.IsReproductivelyEligibleAtAgeSeconds(
                401));
    }

    [Fact]
    public void Constructor_RejectsInvalidLifecycleOrdering()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new OrganismLifecycleTiming(
                    maturityAgeSeconds: 100,
                    reproductiveAgeMinimumSeconds: 99));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new OrganismLifecycleTiming(
                    maturityAgeSeconds: 100,
                    reproductiveAgeMinimumSeconds: 120,
                    reproductiveAgeMaximumSeconds: 119));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new OrganismLifecycleTiming(
                    maturityAgeSeconds: 100,
                    reproductiveAgeMinimumSeconds: 120,
                    senescenceAgeSeconds: 99));
    }

    [Fact]
    public void LifecycleEvaluation_RejectsNegativeAge()
    {
        var timing =
            new OrganismLifecycleTiming(
                maturityAgeSeconds: 100,
                reproductiveAgeMinimumSeconds: 120);

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                timing.StageAtAgeSeconds(-1));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                timing
                    .IsReproductivelyEligibleAtAgeSeconds(
                        -1));
    }

    [Fact]
    public void SeasonalStrategies_AreComposableSpeciesPolicy()
    {
        var strategies =
            OrganismSeasonalStrategy.Breeding |
            OrganismSeasonalStrategy.Migration;

        Assert.True(
            strategies.HasFlag(
                OrganismSeasonalStrategy.Breeding));

        Assert.True(
            strategies.HasFlag(
                OrganismSeasonalStrategy.Migration));

        Assert.False(
            strategies.HasFlag(
                OrganismSeasonalStrategy.Hibernation));
    }
}
