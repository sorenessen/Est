using Est.Simulation.Surface;
using Est.Simulation.Vegetation;

namespace Est.Simulation.Tests.Vegetation;

public sealed class VegetationCellStateTests
{
    [Fact]
    public void Constructor_StoresLiveBiomass()
    {
        var cellId =
            new SurfaceCellId(
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000042"));

        var state =
            new VegetationCellState(
                cellId,
                3.5);

        Assert.Equal(
            cellId,
            state.CellId);

        Assert.Equal(
            3.5,
            state.LiveBiomassKilogramsPerSquareMeter);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Constructor_RejectsInvalidLiveBiomass(
        double liveBiomassKilogramsPerSquareMeter)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new VegetationCellState(
                    new SurfaceCellId(
                        Guid.Parse(
                            "00000000-0000-0000-0000-000000000042")),
                    liveBiomassKilogramsPerSquareMeter));
    }
}
