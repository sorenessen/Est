using Est.Simulation.Climate;
using Est.Simulation.Definitions;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Thermal;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Thermal;

public sealed class RegionalThermalModelDefinitionTests
{
    [Fact]
    public void Empty_HasNoRegionalThermalModels()
    {
        Assert.Empty(
            SimulationDefinition.Empty
                .RegionalThermalModels);
    }

    [Fact]
    public void Constructor_PreservesRegionalThermalModel()
    {
        var model =
            new RegionalThermalModelDefinition(
                PlanetId.New(),
                CreateParameters());

        var definition =
            new SimulationDefinition(
                regionalThermalModels:
                [
                    model
                ]);

        Assert.Same(
            model,
            Assert.Single(
                definition.RegionalThermalModels));
    }

    [Fact]
    public void Constructor_RejectsDuplicateRegionalThermalModelForPlanet()
    {
        var planetId =
            PlanetId.New();

        Assert.Throws<ArgumentException>(
            () =>
                new SimulationDefinition(
                    regionalThermalModels:
                    [
                        new RegionalThermalModelDefinition(
                            planetId,
                            CreateParameters()),
                        new RegionalThermalModelDefinition(
                            planetId,
                            CreateParameters())
                    ]));
    }

    [Fact]
    public void Constructor_RejectsCompetingThermalAuthorityForPlanet()
    {
        var planetId =
            PlanetId.New();

        Assert.Throws<ArgumentException>(
            () =>
                new SimulationDefinition(
                    planetaryEnergyBalanceModels:
                    [
                        new PlanetaryEnergyBalanceModelDefinition(
                            planetId,
                            new PlanetaryEnergyBalanceParameters(
                                1361,
                                0.61,
                                1.0e8,
                                0.30,
                                0.60,
                                260,
                                285,
                                31_536_000))
                    ],
                    regionalThermalModels:
                    [
                        new RegionalThermalModelDefinition(
                            planetId,
                            CreateParameters())
                    ]));
    }

    [Fact]
    public void Constructor_AllowsDifferentPlanetsToUseDifferentThermalAuthorities()
    {
        var ebmPlanetId =
            PlanetId.New();

        var regionalPlanetId =
            PlanetId.New();

        var definition =
            new SimulationDefinition(
                planetaryEnergyBalanceModels:
                [
                    new PlanetaryEnergyBalanceModelDefinition(
                        ebmPlanetId,
                        new PlanetaryEnergyBalanceParameters(
                            1361,
                            0.61,
                            1.0e8,
                            0.30,
                            0.60,
                            260,
                            285,
                            31_536_000))
                ],
                regionalThermalModels:
                [
                    new RegionalThermalModelDefinition(
                        regionalPlanetId,
                        CreateParameters())
                ]);

        Assert.Equal(
            ebmPlanetId,
            Assert.Single(
                definition
                    .PlanetaryEnergyBalanceModels)
                .PlanetId);

        Assert.Equal(
            regionalPlanetId,
            Assert.Single(
                definition
                    .RegionalThermalModels)
                .PlanetId);
    }

    [Fact]
    public void ModelDefinition_RejectsEmptyPlanetIdentity()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new RegionalThermalModelDefinition(
                    new PlanetId(
                        Guid.Empty),
                    CreateParameters()));
    }

    [Fact]
    public void ValidateFor_AcceptsRegionalModelWithRegionalState()
    {
        var setup =
            CreateWorld(
                includeRegionalThermal: true);

        var definition =
            new SimulationDefinition(
                regionalThermalModels:
                [
                    new RegionalThermalModelDefinition(
                        setup.Planet.Id,
                        CreateParameters())
                ]);

        definition.ValidateFor(
            setup.World);
    }

    [Fact]
    public void ValidateFor_RejectsRegionalModelWithoutRegionalState()
    {
        var setup =
            CreateWorld(
                includeRegionalThermal: false);

        var definition =
            new SimulationDefinition(
                regionalThermalModels:
                [
                    new RegionalThermalModelDefinition(
                        setup.Planet.Id,
                        CreateParameters())
                ]);

        Assert.Throws<ArgumentException>(
            () =>
                definition.ValidateFor(
                    setup.World));
    }

    [Fact]
    public void ValidateFor_RejectsRegionalModelForUnknownPlanet()
    {
        var setup =
            CreateWorld(
                includeRegionalThermal: false);

        var definition =
            new SimulationDefinition(
                regionalThermalModels:
                [
                    new RegionalThermalModelDefinition(
                        PlanetId.New(),
                        CreateParameters())
                ]);

        Assert.Throws<ArgumentException>(
            () =>
                definition.ValidateFor(
                    setup.World));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Parameters_RejectInvalidStellarFlux(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateParameters(
                    stellarFlux: value));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Parameters_RejectInvalidOpticalDepth(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateParameters(
                    opticalDepth: value));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Parameters_RejectInvalidOpticalFractions(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateParameters(
                    singleScatteringAlbedo:
                        value));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateParameters(
                    downwardScatteringFraction:
                        value));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateParameters(
                    surfaceAlbedo:
                        value));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateParameters(
                    surfaceLongwaveEmissivity:
                        value));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateParameters(
                    atmosphericLongwaveEmissivity:
                        value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Parameters_RejectInvalidHeatCapacities(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateParameters(
                    surfaceHeatCapacity:
                        value));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateParameters(
                    atmosphericHeatCapacity:
                        value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Parameters_RejectInvalidMaximumIntegrationStep(
        long value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateParameters(
                    maximumIntegrationStepSeconds:
                        value));
    }

    [Fact]
    public void Parameters_AcceptPhysicalBoundaryValues()
    {
        var parameters =
            CreateParameters(
                stellarFlux: 0,
                opticalDepth: 0,
                singleScatteringAlbedo: 0,
                downwardScatteringFraction: 1,
                surfaceAlbedo: 0,
                surfaceLongwaveEmissivity: 1,
                atmosphericLongwaveEmissivity: 0);

        Assert.Equal(
            0,
            parameters.StellarFluxWattsPerSquareMeter);

        Assert.Equal(
            0,
            parameters
                .AtmosphericShortwaveVerticalOpticalDepth);

        Assert.Equal(
            1,
            parameters
                .AtmosphericShortwaveDownwardScatteringFraction);
    }

    private static RegionalThermalModelParameters
        CreateParameters(
            double stellarFlux = 1361,
            double opticalDepth = 0.2,
            double singleScatteringAlbedo = 0.4,
            double downwardScatteringFraction = 0.5,
            double surfaceAlbedo = 0.3,
            double surfaceLongwaveEmissivity = 0.95,
            double atmosphericLongwaveEmissivity = 0.75,
            double surfaceHeatCapacity = 1.0e8,
            double atmosphericHeatCapacity = 1.0e7,
            long maximumIntegrationStepSeconds = 3_600)
    {
        return new RegionalThermalModelParameters(
            stellarFlux,
            opticalDepth,
            singleScatteringAlbedo,
            downwardScatteringFraction,
            surfaceAlbedo,
            surfaceLongwaveEmissivity,
            atmosphericLongwaveEmissivity,
            surfaceHeatCapacity,
            atmosphericHeatCapacity,
            maximumIntegrationStepSeconds);
    }

    private static TestWorld CreateWorld(
        bool includeRegionalThermal)
    {
        var planet =
            new PlanetState(
                PlanetId.New(),
                "Regional Thermal Policy World",
                5.0e24,
                6_000_000,
                new PlanetEnvironment(
                    285,
                    0.60,
                    0.05,
                    AtmosphereState.Vacuum));

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
                    (cell, index) =>
                        new TerrainCellState(
                            cell.Id,
                            index)));

        var regionalThermal =
            includeRegionalThermal
                ? new[]
                {
                    PlanetRegionalThermalInitializer
                        .FromPlanetaryMeanSurfaceTemperature(
                            planet,
                            terrain)
                }
                : [];

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [],
                terrain: [terrain],
                regionalThermal:
                    regionalThermal);

        return new TestWorld(
            planet,
            world);
    }

    private sealed record TestWorld(
        PlanetState Planet,
        WorldState World);
}
