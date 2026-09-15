using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;

namespace Est.Simulation.Tests.Terrain;

public sealed class PlanetTerrainBasinTopologyTests
{
    private const double RadiusMeters =
        6_000_000;

    [Fact]
    public void Derive_FlatTerrainDoesNotCreateRetainedBasins()
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

        var terrain =
            new PlanetTerrainState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new TerrainCellState(
                            cell.Id,
                            1_000)));

        var drainage =
            PlanetTerrainDrainageTopology.Derive(
                planet,
                terrain);

        var basins =
            PlanetTerrainBasinTopology.Derive(
                planet,
                terrain,
                drainage);

        Assert.Empty(
            basins.Basins);
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Basin World",
            5.0e24,
            RadiusMeters,
            new PlanetEnvironment(
                285,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
