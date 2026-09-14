using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;

namespace Est.Simulation.Tests.Terrain;

public sealed class PlanetTerrainDrainageTopologyTests
{
    private const double RadiusMeters =
        6_000_000;

    [Fact]
    public void Derive_SelectsSteepestDownhillNeighbor()
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

        var source =
            grid.LocateCell(
                67.5,
                22.5);

        var east =
            grid.LocateCell(
                67.5,
                67.5);

        var south =
            grid.LocateCell(
                22.5,
                22.5);

        var elevations =
            grid.Cells.ToDictionary(
                cell => cell.Id,
                _ => 1_000d);

        elevations[source.Id] =
            1_000;

        // East has a smaller raw drop, but at this high latitude its
        // center is much closer than the southern neighbor. Drainage
        // must therefore choose physical slope, not elevation alone.
        elevations[east.Id] =
            900;

        elevations[south.Id] =
            800;

        var topology =
            PlanetTerrainDrainageTopology.Derive(
                planet,
                CreateTerrain(
                    planet,
                    definition,
                    grid,
                    elevations));

        var drainage =
            topology.GetCell(
                source.Id);

        Assert.Equal(
            east.Id,
            drainage.DownhillNeighborCellId);

        Assert.True(
            drainage.DownhillSlope > 0);

        Assert.True(
            drainage.HorizontalDistanceMeters > 0);
    }

    [Fact]
    public void Derive_LocalMinimumBecomesSink()
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

        var sink =
            grid.LocateCell(
                22.5,
                22.5);

        var elevations =
            grid.Cells.ToDictionary(
                cell => cell.Id,
                _ => 1_000d);

        elevations[sink.Id] =
            -500;

        var topology =
            PlanetTerrainDrainageTopology.Derive(
                planet,
                CreateTerrain(
                    planet,
                    definition,
                    grid,
                    elevations));

        var drainage =
            topology.GetCell(
                sink.Id);

        Assert.True(
            drainage.IsSink);

        Assert.Null(
            drainage.DownhillNeighborCellId);

        Assert.Equal(
            0,
            drainage.DownhillSlope);

        Assert.Equal(
            0,
            drainage.HorizontalDistanceMeters);
    }

    [Fact]
    public void Derive_CanDrainAcrossLongitudeWrap()
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

        var westernEdge =
            grid.LocateCell(
                22.5,
                -179.9);

        var wrappedWest =
            grid.LocateCell(
                22.5,
                179.9);

        var elevations =
            grid.Cells.ToDictionary(
                cell => cell.Id,
                _ => 2_000d);

        elevations[westernEdge.Id] =
            1_000;

        elevations[wrappedWest.Id] =
            0;

        var topology =
            PlanetTerrainDrainageTopology.Derive(
                planet,
                CreateTerrain(
                    planet,
                    definition,
                    grid,
                    elevations));

        Assert.Equal(
            wrappedWest.Id,
            topology
                .GetCell(
                    westernEdge.Id)
                .DownhillNeighborCellId);
    }

    [Fact]
    public void Derive_GeneratedTerrainCoversEveryCell()
    {
        var planet =
            CreatePlanet();

        var parameters =
            new TectonicTerrainGenerationParameters(
                SurfaceGridDefinition.LatitudeLongitude(
                    12,
                    24),
                seed: 42,
                plateCount: 12,
                continentalPlateFraction: 0.45);

        var terrain =
            TectonicTerrainGenerator.Generate(
                planet,
                parameters);

        var topology =
            PlanetTerrainDrainageTopology.Derive(
                planet,
                terrain);

        Assert.Equal(
            terrain.Cells.Length,
            topology.Cells.Length);

        Assert.Equal(
            terrain.Cells
                .Select(cell => cell.CellId)
                .OrderBy(id => id.Value),
            topology.Cells
                .Select(cell => cell.CellId)
                .OrderBy(id => id.Value));

        Assert.All(
            topology.Cells,
            cell =>
            {
                Assert.True(
                    double.IsFinite(
                        cell.DownhillSlope));

                Assert.True(
                    cell.DownhillSlope >= 0);
            });
    }

    private static PlanetTerrainState CreateTerrain(
        PlanetState planet,
        SurfaceGridDefinition definition,
        IPlanetSurfaceGrid grid,
        IReadOnlyDictionary<
            SurfaceCellId,
            double> elevations)
    {
        return new PlanetTerrainState(
            planet.Id,
            definition,
            grid.Cells.Select(
                cell =>
                    new TerrainCellState(
                        cell.Id,
                        elevations[cell.Id])));
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Drainage World",
            5.0e24,
            RadiusMeters,
            new PlanetEnvironment(
                285,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
