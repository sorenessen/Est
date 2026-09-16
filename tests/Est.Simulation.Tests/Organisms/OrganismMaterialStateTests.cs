using Est.Simulation.Organisms;

namespace Est.Simulation.Tests.Organisms;

public sealed class OrganismMaterialStateTests
{
    [Fact]
    public void Constructor_StoresAuthoritativeMaterial()
    {
        var material =
            new OrganismMaterialState(
                liveBiomassKilograms: 72.5,
                liveNitrogenKilograms: 1.8);

        Assert.Equal(
            72.5,
            material.LiveBiomassKilograms);

        Assert.Equal(
            1.8,
            material.LiveNitrogenKilograms);

        Assert.False(material.IsEmpty);
    }

    [Fact]
    public void Constructor_AllowsEmptyMaterial()
    {
        var material =
            new OrganismMaterialState(
                liveBiomassKilograms: 0,
                liveNitrogenKilograms: 0);

        Assert.True(material.IsEmpty);
    }

    [Fact]
    public void Constructor_RejectsInvalidBiomass()
    {
        foreach (var value in new[]
                 {
                     -0.001,
                     double.NaN,
                     double.PositiveInfinity
                 })
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    new OrganismMaterialState(
                        liveBiomassKilograms: value,
                        liveNitrogenKilograms: 0));
        }
    }

    [Fact]
    public void Constructor_RejectsInvalidNitrogen()
    {
        foreach (var value in new[]
                 {
                     -0.001,
                     double.NaN,
                     double.PositiveInfinity
                 })
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    new OrganismMaterialState(
                        liveBiomassKilograms: 10,
                        liveNitrogenKilograms: value));
        }
    }

    [Fact]
    public void Constructor_RejectsNitrogenGreaterThanBiomass()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new OrganismMaterialState(
                    liveBiomassKilograms: 10,
                    liveNitrogenKilograms: 10.001));
    }

    [Fact]
    public void RetainFraction_ReducesBiomassAndNitrogenProportionally()
    {
        var material =
            new OrganismMaterialState(
                liveBiomassKilograms: 80,
                liveNitrogenKilograms: 2);

        var retained =
            material.RetainFraction(0.25);

        Assert.Equal(
            20,
            retained.LiveBiomassKilograms);

        Assert.Equal(
            0.5,
            retained.LiveNitrogenKilograms);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(1.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void RetainFraction_RejectsInvalidFraction(
        double retainedFraction)
    {
        var material =
            new OrganismMaterialState(
                liveBiomassKilograms: 80,
                liveNitrogenKilograms: 2);

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                material.RetainFraction(
                    retainedFraction));
    }

    [Fact]
    public void Subtract_RemovesExactAvailableMaterial()
    {
        var adult =
            new OrganismMaterialState(
                liveBiomassKilograms: 70,
                liveNitrogenKilograms: 1.75);

        var newborn =
            new OrganismMaterialState(
                liveBiomassKilograms: 3.5,
                liveNitrogenKilograms: 0.0875);

        var remaining =
            adult.Subtract(newborn);

        Assert.Equal(
            66.5,
            remaining.LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            1.6625,
            remaining.LiveNitrogenKilograms,
            precision: 10);
    }

    [Fact]
    public void Subtract_RejectsRemovingUnavailableMaterial()
    {
        var available =
            new OrganismMaterialState(
                liveBiomassKilograms: 1,
                liveNitrogenKilograms: 0.02);

        Assert.Throws<InvalidOperationException>(
            () =>
                available.Subtract(
                    new OrganismMaterialState(
                        liveBiomassKilograms: 1.1,
                        liveNitrogenKilograms: 0.02)));
    }

}
