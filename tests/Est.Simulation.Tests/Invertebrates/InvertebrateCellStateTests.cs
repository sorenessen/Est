using Est.Simulation.Invertebrates;
using Est.Simulation.Surface;

namespace Est.Simulation.Tests.Invertebrates;

public sealed class InvertebrateCellStateTests
{
    [Fact]
    public void Constructor_PreservesBiomassAndNitrogen()
    {
        var cellId =
            new SurfaceCellId(Guid.NewGuid());

        var state =
            new InvertebrateCellState(
                cellId,
                0.25,
                0.0125);

        Assert.Equal(
            cellId,
            state.CellId);

        Assert.Equal(
            0.25,
            state.LiveBiomassKilogramsPerSquareMeter);

        Assert.Equal(
            0.0125,
            state.LiveNitrogenKilogramsPerSquareMeter);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(0.26)]
    public void Constructor_RejectsInvalidNitrogen(
        double nitrogen)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new InvertebrateCellState(
                    new SurfaceCellId(Guid.NewGuid()),
                    0.25,
                    nitrogen));
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
