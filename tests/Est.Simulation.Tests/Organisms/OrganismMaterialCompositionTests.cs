using Est.Simulation.Birds;
using Est.Simulation.Grazers;
using Est.Simulation.Organisms;

namespace Est.Simulation.Tests.Organisms;

public sealed class OrganismMaterialCompositionTests
{
    [Fact]
    public void ForUnits_CreatesAggregateMaterial()
    {
        var composition =
            new OrganismMaterialComposition(
                liveBiomassKilogramsPerUnit: 250,
                liveNitrogenKilogramsPerUnit: 6.25);

        var material =
            composition.ForUnits(20);

        Assert.Equal(
            5_000,
            material.LiveBiomassKilograms);

        Assert.Equal(
            125,
            material.LiveNitrogenKilograms);
    }

    [Fact]
    public void ForUnits_AllowsZeroUnits()
    {
        var composition =
            new OrganismMaterialComposition(
                liveBiomassKilogramsPerUnit: 250,
                liveNitrogenKilogramsPerUnit: 6.25);

        Assert.True(
            composition.ForUnits(0).IsEmpty);
    }

    [Fact]
    public void ForUnits_RejectsNegativeUnitCount()
    {
        var composition =
            new OrganismMaterialComposition(
                liveBiomassKilogramsPerUnit: 1,
                liveNitrogenKilogramsPerUnit: 0.025);

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                composition.ForUnits(-1));
    }

    [Fact]
    public void BirdDefaults_ExposeExplicitPhysicalComposition()
    {
        var parameters =
            new BirdModelParameters();

        Assert.Equal(
            1,
            parameters
                .MaterialPerBird
                .LiveBiomassKilogramsPerUnit);

        Assert.Equal(
            0.025,
            parameters
                .MaterialPerBird
                .LiveNitrogenKilogramsPerUnit);
    }

    [Fact]
    public void GrazerDefaults_ExposeExplicitPhysicalComposition()
    {
        var parameters =
            new GrazerModelParameters();

        Assert.Equal(
            250,
            parameters
                .MaterialPerGrazer
                .LiveBiomassKilogramsPerUnit);

        Assert.Equal(
            6.25,
            parameters
                .MaterialPerGrazer
                .LiveNitrogenKilogramsPerUnit);
    }
}
