using Est.Simulation.Climate;
using Est.Simulation.Definitions;
using Est.Simulation.Hydrology;
using Est.Simulation.Planets;
using Est.Simulation.Population;
using Est.Simulation.Time;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Definitions;

public class SimulationDefinitionTests
{
    [Fact]
    public void Empty_HasNoConfiguredModels()
    {
        Assert.Empty(
            SimulationDefinition.Empty
                .PlanetaryEnergyBalanceModels);

        Assert.Empty(
            SimulationDefinition.Empty
                .PopulationModels);

        Assert.Empty(
            SimulationDefinition.Empty
                .HydrologyModels);
    }

    [Fact]
    public void Constructor_PreservesConfiguredModel()
    {
        var model = CreateModel();

        var definition =
            new SimulationDefinition([model]);

        var configured =
            Assert.Single(
                definition.PlanetaryEnergyBalanceModels);

        Assert.Same(model, configured);
    }

    [Fact]
    public void Constructor_RejectsDuplicateModelForPlanet()
    {
        var planetId = PlanetId.New();

        Assert.Throws<ArgumentException>(
            () => new SimulationDefinition(
            [
                CreateModel(planetId),
                CreateModel(planetId)
            ]));
    }

    [Fact]
    public void ModelDefinition_RejectsEmptyPlanetIdentity()
    {
        Assert.Throws<ArgumentException>(
            () => new PlanetaryEnergyBalanceModelDefinition(
                new PlanetId(Guid.Empty),
                CreateParameters()));
    }

    [Fact]
    public void ValidateFor_AcceptsModelForExistingPlanet()
    {
        var planetId = PlanetId.New();
        var definition = new SimulationDefinition(
            [CreateModel(planetId)]);

        var world = CreateWorld(planetId);

        definition.ValidateFor(world);
    }

    [Fact]
    public void ValidateFor_RejectsModelForUnknownPlanet()
    {
        var definition = new SimulationDefinition(
            [CreateModel(PlanetId.New())]);

        var world = CreateWorld(PlanetId.New());

        Assert.Throws<ArgumentException>(
            () => definition.ValidateFor(world));
    }

    [Fact]
    public void ValidateFor_EmptyDefinitionAcceptsEmptyWorld()
    {
        var world = new WorldState(
            WorldId.New(),
            SimulationTime.Zero);

        SimulationDefinition.Empty.ValidateFor(world);
    }

    [Fact]
    public void Constructor_PreservesConfiguredPopulationModel()
    {
        var planetId = PlanetId.New();

        var model =
            new PopulationModelDefinition(
                planetId,
                new PopulationModelParameters(
                    seed: 42));

        var definition =
            new SimulationDefinition(
                populationModels: [model]);

        var configured =
            Assert.Single(
                definition.PopulationModels);

        Assert.Same(model, configured);
    }

    [Fact]
    public void Constructor_RejectsDuplicatePopulationModelForPlanet()
    {
        var planetId = PlanetId.New();

        Assert.Throws<ArgumentException>(
            () => new SimulationDefinition(
                populationModels:
                [
                    new PopulationModelDefinition(
                        planetId,
                        new PopulationModelParameters()),
                    new PopulationModelDefinition(
                        planetId,
                        new PopulationModelParameters())
                ]));
    }

    [Fact]
    public void ValidateFor_AcceptsPopulationModelForExistingPlanet()
    {
        var planetId = PlanetId.New();

        var definition =
            new SimulationDefinition(
                populationModels:
                [
                    new PopulationModelDefinition(
                        planetId,
                        new PopulationModelParameters())
                ]);

        definition.ValidateFor(
            CreateWorld(planetId));
    }

    [Fact]
    public void ValidateFor_RejectsPopulationModelForUnknownPlanet()
    {
        var definition =
            new SimulationDefinition(
                populationModels:
                [
                    new PopulationModelDefinition(
                        PlanetId.New(),
                        new PopulationModelParameters())
                ]);

        Assert.Throws<ArgumentException>(
            () => definition.ValidateFor(
                CreateWorld(PlanetId.New())));
    }

    [Fact]
    public void Constructor_PreservesConfiguredHydrologyModel()
    {
        var planetId =
            PlanetId.New();

        var model =
            new HydrologyModelDefinition(
                planetId,
                new HydrologyModelParameters(
                    maximumIntegrationStepSeconds: 3_600,
                    maximumEvaporationRateKilogramsPerSquareMeterPerDay: 5,
                    atmosphericPrecipitationThresholdKilogramsPerSquareMeter: 18,
                    maximumPrecipitationRateKilogramsPerSquareMeterPerDay: 14,
                    soilWaterCapacityKilogramsPerSquareMeter: 175,
                    maximumInfiltrationRateKilogramsPerSquareMeterPerDay: 22,
                    maximumRunoffRateKilogramsPerSquareMeterPerDay: 28,
                    freezingTemperatureKelvin: 272.5,
                    meltingTemperatureKelvin: 274,
                    maximumFreezingRateKilogramsPerSquareMeterPerDay: 21,
                    maximumMeltingRateKilogramsPerSquareMeterPerDay: 23));

        var definition =
            new SimulationDefinition(
                hydrologyModels:
                [
                    model
                ]);

        Assert.Same(
            model,
            Assert.Single(
                definition.HydrologyModels));
    }

    [Fact]
    public void Constructor_RejectsDuplicateHydrologyModelForPlanet()
    {
        var planetId =
            PlanetId.New();

        Assert.Throws<ArgumentException>(
            () =>
                new SimulationDefinition(
                    hydrologyModels:
                    [
                        new HydrologyModelDefinition(
                            planetId,
                            new HydrologyModelParameters()),
                        new HydrologyModelDefinition(
                            planetId,
                            new HydrologyModelParameters())
                    ]));
    }

    [Fact]
    public void ValidateFor_RejectsHydrologyModelForUnknownPlanet()
    {
        var definition =
            new SimulationDefinition(
                hydrologyModels:
                [
                    new HydrologyModelDefinition(
                        PlanetId.New(),
                        new HydrologyModelParameters())
                ]);

        Assert.Throws<ArgumentException>(
            () =>
                definition.ValidateFor(
                    CreateWorld(
                        PlanetId.New())));
    }

    [Fact]
    public void HydrologyParameters_RejectInvalidPolicy()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new HydrologyModelParameters(
                    maximumIntegrationStepSeconds: 0));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new HydrologyModelParameters(
                    maximumRunoffRateKilogramsPerSquareMeterPerDay:
                        double.NaN));

        Assert.Throws<ArgumentException>(
            () =>
                new HydrologyModelParameters(
                    freezingTemperatureKelvin: 275,
                    meltingTemperatureKelvin: 274));
    }

    [Fact]
    public void Empty_HasNoConfiguredVegetationModels()
    {
        Assert.Empty(
            SimulationDefinition.Empty
                .VegetationModels);
    }

    [Fact]
    public void Constructor_PreservesConfiguredVegetationModel()
    {
        var planetId =
            PlanetId.New();

        var model =
            new VegetationModelDefinition(
                planetId,
                new VegetationModelParameters(
                    maximumIntegrationStepSeconds:
                        3_600,
                    carryingCapacityKilogramsPerSquareMeter:
                        7,
                    maximumRelativeGrowthRatePerDay:
                        0.2,
                    soilWaterForFullProductivityKilogramsPerSquareMeter:
                        60,
                    minimumGrowthTemperatureKelvin:
                        270,
                    optimumGrowthTemperatureKelvin:
                        292,
                    maximumGrowthTemperatureKelvin:
                        315,
                    temperatureLapseRateKelvinPerMeter:
                        0.006));

        var definition =
            new SimulationDefinition(
                vegetationModels:
                [
                    model
                ]);

        Assert.Same(
            model,
            Assert.Single(
                definition.VegetationModels));
    }

    [Fact]
    public void Constructor_RejectsDuplicateVegetationModelForPlanet()
    {
        var planetId =
            PlanetId.New();

        Assert.Throws<ArgumentException>(
            () =>
                new SimulationDefinition(
                    vegetationModels:
                    [
                        new VegetationModelDefinition(
                            planetId,
                            new VegetationModelParameters()),
                        new VegetationModelDefinition(
                            planetId,
                            new VegetationModelParameters())
                    ]));
    }

    [Fact]
    public void ValidateFor_RejectsVegetationModelForUnknownPlanet()
    {
        var definition =
            new SimulationDefinition(
                vegetationModels:
                [
                    new VegetationModelDefinition(
                        PlanetId.New(),
                        new VegetationModelParameters())
                ]);

        Assert.Throws<ArgumentException>(
            () =>
                definition.ValidateFor(
                    CreateWorld(
                        PlanetId.New())));
    }

    private static WorldState CreateWorld(PlanetId planetId)
    {
        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            [
                new PlanetState(
                    planetId,
                    "Test Planet",
                    5.9722e24,
                    6_371_000,
                    new PlanetEnvironment(
                        288.15,
                        0.71,
                        0.03,
                        AtmosphereState.Vacuum))
            ]);
    }

    private static PlanetaryEnergyBalanceModelDefinition CreateModel(
        PlanetId? planetId = null)
    {
        return new PlanetaryEnergyBalanceModelDefinition(
            planetId ?? PlanetId.New(),
            CreateParameters());
    }

    private static PlanetaryEnergyBalanceParameters CreateParameters()
    {
        return new PlanetaryEnergyBalanceParameters(
            1361,
            0.61,
            1.0e8,
            0.30,
            0.60,
            263.15,
            273.15,
            31_536_000);
    }
}
