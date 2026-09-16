using Est.Simulation.Biogeochemistry;
using Est.Simulation.Definitions;
using Est.Simulation.Hydrology;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Biogeochemistry;

public sealed class BiogeochemistryModelDefinitionTests
{
    [Fact]
    public void Parameters_PreserveConfiguredPolicy()
    {
        var parameters =
            new BiogeochemistryModelParameters(
                maximumIntegrationStepSeconds: 3_600,
                maximumRelativeDecompositionRatePerDay: 0.08,
                soilWaterForFullDecompositionKilogramsPerSquareMeter: 65,
                minimumDecompositionTemperatureKelvin: 260,
                optimumDecompositionTemperatureKelvin: 295,
                maximumDecompositionTemperatureKelvin: 320,
                temperatureLapseRateKelvinPerMeter: 0.006);

        Assert.Equal(
            3_600,
            parameters.MaximumIntegrationStepSeconds);

        Assert.Equal(
            0.08,
            parameters.MaximumRelativeDecompositionRatePerDay);

        Assert.Equal(
            65,
            parameters
                .SoilWaterForFullDecompositionKilogramsPerSquareMeter);

        Assert.Equal(
            260,
            parameters.MinimumDecompositionTemperatureKelvin);

        Assert.Equal(
            295,
            parameters.OptimumDecompositionTemperatureKelvin);

        Assert.Equal(
            320,
            parameters.MaximumDecompositionTemperatureKelvin);

        Assert.Equal(
            0.006,
            parameters.TemperatureLapseRateKelvinPerMeter);
    }

    [Fact]
    public void Parameters_RejectInvalidPolicy()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new BiogeochemistryModelParameters(
                    maximumIntegrationStepSeconds: 0));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new BiogeochemistryModelParameters(
                    maximumRelativeDecompositionRatePerDay:
                        double.NaN));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new BiogeochemistryModelParameters(
                    soilWaterForFullDecompositionKilogramsPerSquareMeter:
                        0));

        Assert.Throws<ArgumentException>(
            () =>
                new BiogeochemistryModelParameters(
                    minimumDecompositionTemperatureKelvin: 295,
                    optimumDecompositionTemperatureKelvin: 290));

        Assert.Throws<ArgumentException>(
            () =>
                new BiogeochemistryModelParameters(
                    optimumDecompositionTemperatureKelvin: 315,
                    maximumDecompositionTemperatureKelvin: 310));
    }

    [Fact]
    public void ModelDefinition_RejectsEmptyPlanetIdentity()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new BiogeochemistryModelDefinition(
                    new PlanetId(
                        Guid.Empty),
                    new BiogeochemistryModelParameters()));
    }

    [Fact]
    public void Empty_HasNoConfiguredBiogeochemistryModels()
    {
        Assert.Empty(
            SimulationDefinition.Empty
                .BiogeochemistryModels);
    }

    [Fact]
    public void Constructor_PreservesConfiguredModel()
    {
        var planetId =
            PlanetId.New();

        var model =
            new BiogeochemistryModelDefinition(
                planetId,
                new BiogeochemistryModelParameters());

        var definition =
            new SimulationDefinition(
                biogeochemistryModels:
                [
                    model
                ]);

        Assert.Same(
            model,
            Assert.Single(
                definition.BiogeochemistryModels));
    }

    [Fact]
    public void Constructor_RejectsDuplicatePlanetModels()
    {
        var planetId =
            PlanetId.New();

        Assert.Throws<ArgumentException>(
            () =>
                new SimulationDefinition(
                    biogeochemistryModels:
                    [
                        new BiogeochemistryModelDefinition(
                            planetId,
                            new BiogeochemistryModelParameters()),
                        new BiogeochemistryModelDefinition(
                            planetId,
                            new BiogeochemistryModelParameters())
                    ]));
    }

    [Fact]
    public void ValidateFor_RejectsModelForUnknownPlanet()
    {
        var planet =
            CreatePlanet();

        var definition =
            new SimulationDefinition(
                biogeochemistryModels:
                [
                    new BiogeochemistryModelDefinition(
                        PlanetId.New(),
                        new BiogeochemistryModelParameters())
                ]);

        Assert.Throws<ArgumentException>(
            () =>
                definition.ValidateFor(
                    new WorldState(
                        WorldId.New(),
                        SimulationTime.Zero,
                        [planet],
                        [])));
    }

    [Fact]
    public void ValidateFor_RequiresAuthoritativeBiogeochemistryState()
    {
        var planet =
            CreatePlanet();

        var definition =
            new SimulationDefinition(
                biogeochemistryModels:
                [
                    new BiogeochemistryModelDefinition(
                        planet.Id,
                        new BiogeochemistryModelParameters())
                ]);

        Assert.Throws<ArgumentException>(
            () =>
                definition.ValidateFor(
                    new WorldState(
                        WorldId.New(),
                        SimulationTime.Zero,
                        [planet],
                        [])));
    }

    [Fact]
    public void ValidateFor_AllowsModelWithAuthoritativeBiogeochemistryState()
    {
        var planet =
            CreatePlanet();

        var gridDefinition =
            SurfaceGridDefinition.LatitudeLongitude(
                2,
                4);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                gridDefinition);

        var terrain =
            new PlanetTerrainState(
                planet.Id,
                gridDefinition,
                grid.Cells.Select(
                    cell =>
                        new TerrainCellState(
                            cell.Id,
                            0)));

        var hydrology =
            new PlanetHydrologyState(
                planet.Id,
                gridDefinition,
                grid.Cells.Select(
                    cell =>
                        new HydrologyCellState(
                            cell.Id,
                            0,
                            0,
                            100,
                            0)));

        var biogeochemistry =
            new PlanetBiogeochemistryState(
                planet.Id,
                gridDefinition,
                grid.Cells.Select(
                    cell =>
                        new BiogeochemistryCellState(
                            cell.Id,
                            1,
                            0.03,
                            0.01)));

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [],
                terrain:
                [
                    terrain
                ],
                hydrology:
                [
                    hydrology
                ],
                biogeochemistry:
                [
                    biogeochemistry
                ]);

        var definition =
            new SimulationDefinition(
                biogeochemistryModels:
                [
                    new BiogeochemistryModelDefinition(
                        planet.Id,
                        new BiogeochemistryModelParameters())
                ]);

        definition.ValidateFor(
            world);
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Biogeochemistry Model World",
            5.0e24,
            6_000_000,
            new PlanetEnvironment(
                285,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }
}
