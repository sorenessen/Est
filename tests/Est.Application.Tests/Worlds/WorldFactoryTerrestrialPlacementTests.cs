using Est.Application.Worlds;
using Est.Simulation.Hydrology;
using Est.Simulation.Surface;

namespace Est.Application.Tests.Worlds;

public sealed class WorldFactoryTerrestrialPlacementTests
{
    [Fact]
    public void Create_PlacesSyntheticTerrestrialOrganismsOnDryCells()
    {
        var world =
            WorldFactory.Create(
                new WorldCreationSpecification(
                [
                    new PlanetCreationSpecification(
                        "Earth",
                        5.9722e24,
                        6_371_000,
                        new PlanetEnvironmentCreationSpecification(
                            288.15,
                            0.71,
                            0.03,
                            new AtmosphereCreationSpecification(
                                101_325,
                                new Dictionary<string, double>
                                {
                                    ["N2"] = 0.78,
                                    ["O2"] = 0.21,
                                    ["Ar"] = 0.01
                                })),
                        SyntheticPopulation:
                            new SyntheticPopulationCreationSpecification(
                                FounderCount: 48,
                                Seed: 125,
                                CenterLatitudeDegrees: 0,
                                CenterLongitudeDegrees: 20,
                                SpreadDegrees: 4,
                                MinimumAgeYears: 8,
                                MaximumAgeYears: 35),
                        SyntheticAnimals:
                            new SyntheticAnimalCreationSpecification(
                                WolfCount: 8,
                                Seed: 126,
                                CenterLatitudeDegrees: 0,
                                CenterLongitudeDegrees: 25,
                                SpreadDegrees: 1),
                        GeneratedTerrain:
                            new GeneratedTerrainCreationSpecification(
                                Seed: 20260914,
                                LatitudeBandCount: 72,
                                LongitudeBandCount: 144,
                                PlateCount: 24,
                                ContinentalPlateFraction: 0.45),
                        GeneratedHydrology:
                            new GeneratedHydrologyCreationSpecification(
                                1.4e21))
                ]));

        var planet =
            Assert.Single(
                world.Planets);

        var terrain =
            Assert.Single(
                world.Terrain);

        var hydrology =
            Assert.Single(
                world.Hydrology);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                terrain.GridDefinition);

        var standingWater =
            PlanetStandingWaterState.Derive(
                planet,
                terrain,
                hydrology);

        Assert.NotEmpty(
            world.Population);

        Assert.NotEmpty(
            world.Animals);

        foreach (var person in world.Population)
        {
            var cell =
                grid.LocateCell(
                    person.LatitudeDegrees,
                    person.LongitudeDegrees);

            Assert.False(
                standingWater
                    .GetCell(
                        cell.Id)
                    .IsFlooded);
        }

        foreach (var animal in world.Animals)
        {
            var cell =
                grid.LocateCell(
                    animal.LatitudeDegrees,
                    animal.LongitudeDegrees);

            Assert.False(
                standingWater
                    .GetCell(
                        cell.Id)
                    .IsFlooded);
        }
    }
}
