using Est.Simulation.Grazers;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Vegetation;

namespace Est.Simulation.Tests.Grazers;

public sealed class PlanetGrazerCohortInitializerTests
{
    [Fact]
    public void FromVegetationSupport_SelectsStrongestCellsDeterministically()
    {
        var planet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                2,
                4);

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
                            index + 1)));

        var parameters =
            new GrazerModelParameters(
                carryingCapacityGrazersPerKilogramLiveVegetationBiomass:
                    0.001,
                initialFractionOfLocalCarryingCapacity:
                    0.5,
                minimumInitialCohortMemberCount:
                    1,
                maximumInitialCohortCount:
                    2);

        var first =
            PlanetGrazerCohortInitializer
                .FromVegetationSupport(
                    planet,
                    vegetation,
                    parameters);

        var second =
            PlanetGrazerCohortInitializer
                .FromVegetationSupport(
                    planet,
                    vegetation,
                    parameters);

        Assert.Equal(
            2,
            first.Count);

        Assert.Equal(
            first,
            second);

        var expectedTotalMemberCount =
            (long)Math.Floor(
                grid.Cells.Sum(
                    cell =>
                        vegetation
                            .GetCell(
                                cell.Id)
                            .LiveBiomassKilogramsPerSquareMeter *
                        cell.AreaSquareMeters *
                        0.001 *
                        0.5));

        Assert.Equal(
            expectedTotalMemberCount,
            first.Sum(
                cohort =>
                    (long)cohort.MemberCount));

        var strongestCell =
            grid.Cells[^1];

        Assert.Equal(
            strongestCell.CenterLatitudeDegrees,
            first[0].LatitudeDegrees);

        Assert.Equal(
            strongestCell.CenterLongitudeDegrees,
            first[0].LongitudeDegrees);

        Assert.Equal(
            GrazerCohortId.CreateDeterministic(
                planet.Id,
                strongestCell.Id),
            first[0].Id);
    }

    [Fact]
    public void FromVegetationSupport_WithNoSupportCreatesNoCohorts()
    {
        var planet =
            CreatePlanet();

        var definition =
            SurfaceGridDefinition.LatitudeLongitude(
                2,
                4);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                definition);

        var vegetation =
            new PlanetVegetationState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new VegetationCellState(
                            cell.Id,
                            0)));

        var cohorts =
            PlanetGrazerCohortInitializer
                .FromVegetationSupport(
                    planet,
                    vegetation,
                    new GrazerModelParameters());

        Assert.Empty(
            cohorts);
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Grazer Initializer World",
            5.0e20,
            1_000,
            new PlanetEnvironment(
                285,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
