using Est.Simulation.Invertebrates;
using Est.Simulation.Surface;

namespace Est.Simulation.Tests.Invertebrates;

public sealed class InvertebrateCellStateTests
{
    [Fact]
    public void Constructor_PreservesBiomass()
    {
        var cellId =
            new SurfaceCellId(Guid.NewGuid());

        var state =
            new InvertebrateCellState(
                cellId,
                0.25);

        Assert.Equal(
            cellId,
            state.CellId);

        Assert.Equal(
            0.25,
            state.LiveBiomassKilogramsPerSquareMeter);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Constructor_RejectsInvalidBiomass(
        double biomass)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new InvertebrateCellState(
                    new SurfaceCellId(Guid.NewGuid()),
                    biomass));
    }
}
