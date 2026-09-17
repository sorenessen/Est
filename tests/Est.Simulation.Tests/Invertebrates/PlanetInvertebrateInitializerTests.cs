using Est.Simulation.Invertebrates;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Vegetation;

namespace Est.Simulation.Tests.Invertebrates;

public sealed class PlanetInvertebrateInitializerTests
{
    [Fact]
    public void FromVegetationSupport_SeedsFractionOfLocalCapacity()
    {
        var planet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                4,
                8);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        var vegetation =
            new PlanetVegetationState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    (cell, index) =>
                        new VegetationCellState(
                            cell.Id,
                            index % 2 == 0
                                ? 2
                                : 0.5)));

        var parameters =
            new InvertebrateModelParameters(
                carryingCapacityKilogramsPerKilogramLiveVegetation:
                    0.04,
                initialFractionOfLocalCarryingCapacity:
                    0.25,
                liveNitrogenKilogramsPerKilogramLiveBiomass:
                    0.025);

        var invertebrates =
            PlanetInvertebrateInitializer
                .FromVegetationSupport(
                    planet,
                    vegetation,
                    parameters);

        Assert.Equal(
            vegetation.GridDefinition,
            invertebrates.GridDefinition);

        foreach (var cell in
                 vegetation.Cells)
        {
            var expected =
                cell.LiveBiomassKilogramsPerSquareMeter *
                0.04 *
                0.25;

            var invertebrateCell =
                invertebrates.GetCell(
                    cell.CellId);

            Assert.Equal(
                expected,
                invertebrateCell
                    .LiveBiomassKilogramsPerSquareMeter,
                12);

            Assert.Equal(
                expected *
                0.025,
                invertebrateCell
                    .LiveNitrogenKilogramsPerSquareMeter,
                12);
        }
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Invertebrate World",
            5.0e24,
            6_000_000,
            new PlanetEnvironment(
                285,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
