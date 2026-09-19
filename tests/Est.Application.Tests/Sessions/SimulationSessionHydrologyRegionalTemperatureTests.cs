using Est.Application.Sessions;
using Est.Simulation.Climate;
using Est.Simulation.Definitions;
using Est.Simulation.Hydrology;
using Est.Simulation.Planets;
using Est.Simulation.Seasons;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Thermal;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Application.Tests.Sessions;

public sealed class SimulationSessionHydrologyRegionalTemperatureTests
{
    private const long OneDaySeconds =
        86_400;

    [Fact]
    public void Advance_RegionalThermalAuthorityFeedsCellLocalTemperatureToHydrologyAfterThermalEvaluation()
    {
        var setup =
            CreateWorld(
                compatibilityTemperatureKelvin:
                    273.5,
                regionalSurfaceTemperatures:
                    (index, _) =>
                        index switch
                        {
                            0 => 270,
                            1 => 277,
                            _ => 273.5
                        });

        var definition =
            new SimulationDefinition(
                hydrologyModels:
                [
                    new HydrologyModelDefinition(
                        setup.Planet.Id,
                        CreateHydrologyParameters())
                ],
                regionalThermalModels:
                [
                    new RegionalThermalModelDefinition(
                        setup.Planet.Id,
                        CreateThermalParameters())
                ]);

        var session =
            new SimulationSession(
                setup.World,
                definition);

        session.Advance(
            OneDaySeconds);

        Assert.Equal(
            2,
            session.Timeline.Events.Length);

        Assert.Equal(
            "regional-thermal",
            session.Timeline.Events[0]
                .Cause);

        Assert.Equal(
            "planetary-hydrology",
            session.Timeline.Events[1]
                .Cause);

        var changedHydrology =
            Assert.Single(
                session.CurrentWorld.Hydrology);

        Assert.Equal(
            15,
            changedHydrology
                .GetCell(
                    setup.Grid.Cells[0].Id)
                .SnowIceWaterEquivalentKilogramsPerSquareMeter,
            10);

        Assert.Equal(
            6,
            changedHydrology
                .GetCell(
                    setup.Grid.Cells[1].Id)
                .SnowIceWaterEquivalentKilogramsPerSquareMeter,
            10);

        Assert.Equal(
            10,
            changedHydrology
                .GetCell(
                    setup.Grid.Cells[2].Id)
                .SnowIceWaterEquivalentKilogramsPerSquareMeter,
            10);

        var changedPlanet =
            Assert.Single(
                session.CurrentWorld.Planets);

        Assert.Equal(
            273.5,
            changedPlanet.Environment
                .MeanSurfaceTemperatureKelvin,
            10);

        var changedThermal =
            Assert.Single(
                session.CurrentWorld.RegionalThermal);

        Assert.Equal(
            setup.Thermal.PlanetId,
            changedThermal.PlanetId);

        Assert.Equal(
            setup.Thermal.GridDefinition,
            changedThermal.GridDefinition);

        foreach (var expectedCell in
                 setup.Thermal.Cells)
        {
            var actualCell =
                changedThermal.GetCell(
                    expectedCell.CellId);

            Assert.Equal(
                expectedCell.SurfaceTemperatureKelvin,
                actualCell.SurfaceTemperatureKelvin,
                10);

            Assert.Equal(
                expectedCell.AtmosphericTemperatureKelvin,
                actualCell.AtmosphericTemperatureKelvin,
                10);
        }
    }

    [Fact]
    public void Advance_DormantRegionalThermalStateDoesNotActivateRegionalHydrologyTemperature()
    {
        var setup =
            CreateWorld(
                compatibilityTemperatureKelvin:
                    270,
                regionalSurfaceTemperatures:
                    (_, _) =>
                        277);

        var definition =
            new SimulationDefinition(
                hydrologyModels:
                [
                    new HydrologyModelDefinition(
                        setup.Planet.Id,
                        CreateHydrologyParameters())
                ]);

        var session =
            new SimulationSession(
                setup.World,
                definition);

        session.Advance(
            OneDaySeconds);

        var hydrologyEvent =
            Assert.Single(
                session.Timeline.Events);

        Assert.Equal(
            "planetary-hydrology",
            hydrologyEvent.Cause);

        Assert.Equal(
            15,
            Assert.Single(
                    session.CurrentWorld.Hydrology)
                .GetCell(
                    setup.Grid.Cells[0].Id)
                .SnowIceWaterEquivalentKilogramsPerSquareMeter,
            10);

        Assert.Equal(
            setup.Thermal,
            Assert.Single(
                session.CurrentWorld.RegionalThermal));
    }

    [Fact]
    public void Advance_PlanetaryEnergyBalanceAuthorityFeedsUpdatedCompatibilityTemperatureToHydrology()
    {
        var setup =
            CreateWorld(
                compatibilityTemperatureKelvin:
                    274,
                regionalSurfaceTemperatures:
                    (_, _) =>
                        278);

        var definition =
            new SimulationDefinition(
                planetaryEnergyBalanceModels:
                [
                    new PlanetaryEnergyBalanceModelDefinition(
                        setup.Planet.Id,
                        new PlanetaryEnergyBalanceParameters(
                            0,
                            0.61,
                            1.0e7,
                            0.30,
                            0.60,
                            240,
                            270,
                            31_536_000))
                ],
                hydrologyModels:
                [
                    new HydrologyModelDefinition(
                        setup.Planet.Id,
                        CreateHydrologyParameters())
                ]);

        var session =
            new SimulationSession(
                setup.World,
                definition);

        session.Advance(
            OneDaySeconds);

        Assert.Equal(
            2,
            session.Timeline.Events.Length);

        Assert.Equal(
            "planetary-energy-balance",
            session.Timeline.Events[0]
                .Cause);

        Assert.Equal(
            "planetary-hydrology",
            session.Timeline.Events[1]
                .Cause);

        var changedPlanet =
            Assert.Single(
                session.CurrentWorld.Planets);

        Assert.True(
            changedPlanet.Environment
                .MeanSurfaceTemperatureKelvin <
            273);

        var changedHydrology =
            Assert.Single(
                session.CurrentWorld.Hydrology);

        Assert.Equal(
            15,
            changedHydrology
                .GetCell(
                    setup.Grid.Cells[0].Id)
                .SnowIceWaterEquivalentKilogramsPerSquareMeter,
            10);

        Assert.Equal(
            setup.Thermal,
            Assert.Single(
                session.CurrentWorld.RegionalThermal));
    }

    private static HydrologyModelParameters
        CreateHydrologyParameters()
    {
        return new HydrologyModelParameters(
            maximumIntegrationStepSeconds:
                OneDaySeconds,
            maximumEvaporationRateKilogramsPerSquareMeterPerDay:
                0,
            atmosphericPrecipitationThresholdKilogramsPerSquareMeter:
                20,
            maximumPrecipitationRateKilogramsPerSquareMeterPerDay:
                0,
            soilWaterCapacityKilogramsPerSquareMeter:
                150,
            maximumInfiltrationRateKilogramsPerSquareMeterPerDay:
                0,
            maximumRunoffRateKilogramsPerSquareMeterPerDay:
                0,
            freezingTemperatureKelvin:
                273,
            meltingTemperatureKelvin:
                274,
            maximumFreezingRateKilogramsPerSquareMeterPerDay:
                5,
            maximumMeltingRateKilogramsPerSquareMeterPerDay:
                4);
    }

    private static RegionalThermalModelParameters
        CreateThermalParameters()
    {
        return new RegionalThermalModelParameters(
            stellarFluxWattsPerSquareMeter:
                0,
            atmosphericShortwaveVerticalOpticalDepth:
                0,
            atmosphericShortwaveSingleScatteringAlbedo:
                0,
            atmosphericShortwaveDownwardScatteringFraction:
                0,
            surfaceShortwaveAlbedo:
                0,
            surfaceLongwaveEmissivity:
                0,
            atmosphericLongwaveEmissivity:
                0,
            surfaceEffectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                1.0e8,
            atmosphericEffectiveArealHeatCapacityJoulesPerSquareMeterKelvin:
                1.0e7,
            maximumIntegrationStepSeconds:
                OneDaySeconds);
    }

    private static TestWorld CreateWorld(
        double compatibilityTemperatureKelvin,
        Func<int, SurfaceCellId, double>
            regionalSurfaceTemperatures)
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Session Regional Hydrology World",
                5.0e24,
                6_000_000,
                new PlanetEnvironment(
                    compatibilityTemperatureKelvin,
                    0.60,
                    0.05,
                    AtmosphereState.Vacuum));

        var definition =
            SurfaceGridDefinition
                .LatitudeLongitude(
                    2,
                    4);

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
                            0)));

        var hydrology =
            new PlanetHydrologyState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    cell =>
                        new HydrologyCellState(
                            cell.Id,
                            atmosphericWaterKilogramsPerSquareMeter:
                                0,
                            surfaceLiquidWaterKilogramsPerSquareMeter:
                                10,
                            soilWaterKilogramsPerSquareMeter:
                                0,
                            snowIceWaterEquivalentKilogramsPerSquareMeter:
                                10)));

        var thermal =
            new PlanetRegionalThermalState(
                planet.Id,
                definition,
                grid.Cells.Select(
                    (cell, index) =>
                        new RegionalThermalCellState(
                            cell.Id,
                            regionalSurfaceTemperatures(
                                index,
                                cell.Id),
                            atmosphericTemperatureKelvin:
                                200)));

        var seasonalState =
            new PlanetSeasonalState(
                planet.Id,
                SeasonalControlMode.Override,
                overrideContext:
                    new SeasonalContext(
                        "fixed",
                        subsolarLatitudeDegrees:
                            0));

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [],
                terrain:
                    [terrain],
                hydrology:
                    [hydrology],
                seasonalStates:
                    [seasonalState],
                regionalThermal:
                    [thermal]);

        return new TestWorld(
            planet,
            grid,
            thermal,
            world);
    }

    private sealed record TestWorld(
        PlanetState Planet,
        IPlanetSurfaceGrid Grid,
        PlanetRegionalThermalState Thermal,
        WorldState World);
}
