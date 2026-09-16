using Est.Simulation.Population;

namespace Est.Simulation.Tests.Population;

public sealed class PopulationModelParametersTests
{
    [Theory]
    [InlineData(0, 3.5, 0.0875)]
    [InlineData(9, 36.75, 0.91875)]
    [InlineData(18, 70, 1.75)]
    [InlineData(30, 70, 1.75)]
    public void MaterialTargetAtAgeYears_InterpolatesFromNewbornToMature(
        double ageYears,
        double expectedBiomassKilograms,
        double expectedNitrogenKilograms)
    {
        var parameters =
            new PopulationModelParameters();

        var target =
            parameters.MaterialTargetAtAgeYears(
                ageYears);

        Assert.Equal(
            expectedBiomassKilograms,
            target.LiveBiomassKilograms,
            precision: 10);

        Assert.Equal(
            expectedNitrogenKilograms,
            target.LiveNitrogenKilograms,
            precision: 10);
    }

    [Fact]
    public void Constructor_RejectsMatureMaterialBelowNewbornMaterial()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new PopulationModelParameters(
                    newbornLiveBiomassKilograms: 3.5,
                    newbornLiveNitrogenKilograms: 0.0875,
                    matureLiveBiomassKilograms: 3,
                    matureLiveNitrogenKilograms: 0.0875));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new PopulationModelParameters(
                    newbornLiveBiomassKilograms: 3.5,
                    newbornLiveNitrogenKilograms: 0.0875,
                    matureLiveBiomassKilograms: 70,
                    matureLiveNitrogenKilograms: 0.08));
    }

    [Fact]
    public void MaterialTargetAtAgeYears_RejectsInvalidAge()
    {
        var parameters =
            new PopulationModelParameters();

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                parameters.MaterialTargetAtAgeYears(
                    -0.001));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                parameters.MaterialTargetAtAgeYears(
                    double.NaN));
    }
}
