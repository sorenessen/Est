using Est.Simulation.Biogeochemistry;
using Est.Simulation.Surface;

namespace Est.Simulation.Tests.Biogeochemistry;

public sealed class BiogeochemistryCellStateTests
{
    [Fact]
    public void Constructor_PreservesPools()
    {
        var cellId =
            new SurfaceCellId(
                Guid.NewGuid());

        var state =
            new BiogeochemistryCellState(
                cellId,
                1.25,
                0.04,
                0.015);

        Assert.Equal(
            cellId,
            state.CellId);

        Assert.Equal(
            1.25,
            state.DetritalBiomassKilogramsPerSquareMeter);

        Assert.Equal(
            0.04,
            state.DetritalNitrogenKilogramsPerSquareMeter);

        Assert.Equal(
            0.015,
            state.PlantAvailableNitrogenKilogramsPerSquareMeter);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Constructor_RejectsInvalidDetritalBiomass(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new BiogeochemistryCellState(
                    new SurfaceCellId(
                        Guid.NewGuid()),
                    value,
                    0,
                    0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Constructor_RejectsInvalidDetritalNitrogen(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new BiogeochemistryCellState(
                    new SurfaceCellId(
                        Guid.NewGuid()),
                    0,
                    value,
                    0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Constructor_RejectsInvalidAvailableNitrogen(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new BiogeochemistryCellState(
                    new SurfaceCellId(
                        Guid.NewGuid()),
                    0,
                    0,
                    value));
    }
}
