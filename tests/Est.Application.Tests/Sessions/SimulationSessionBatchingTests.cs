using Est.Application.Sessions;
using Est.Application.Worlds;
using Est.Simulation.Biogeochemistry;
using Est.Simulation.Definitions;
using Est.Simulation.Grazers;
using Est.Simulation.Hydrology;
using Est.Simulation.Vegetation;

namespace Est.Application.Tests.Sessions;

public sealed class SimulationSessionBatchingTests
{
    private const long OneDaySeconds = 86_400;

    [Fact]
    public void Advance_SevenDaysMatchesSevenOneDayAdvances()
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
                                1.4e21),
                        GeneratedVegetation:
                            new GeneratedVegetationCreationSpecification(
                                2.0),
                        GeneratedGrazers:
                            new GeneratedGrazerCreationSpecification(
                                CarryingCapacityGrazersPerKilogramLiveVegetationBiomass:
                                    1e-6,
                                InitialFractionOfLocalCarryingCapacity:
                                    0.25,
                                MinimumInitialCohortMemberCount:
                                    10,
                                MaximumInitialCohortCount:
                                    256,
                                LiveBiomassKilogramsPerGrazer:
                                    250,
                                LiveNitrogenKilogramsPerGrazer:
                                    6.25),
                        GeneratedBiogeochemistry:
                            new GeneratedBiogeochemistryCreationSpecification(
                                InitialDetritalBiomassKilogramsPerSquareMeter:
                                    0,
                                InitialDetritalNitrogenKilogramsPerSquareMeter:
                                    0,
                                InitialPlantAvailableNitrogenKilogramsPerSquareMeter:
                                    0))
                ]));

        var planet =
            Assert.Single(
                world.Planets);

        var definition =
            new SimulationDefinition(
                hydrologyModels:
                [
                    new HydrologyModelDefinition(
                        planet.Id,
                        new HydrologyModelParameters())
                ],
                vegetationModels:
                [
                    new VegetationModelDefinition(
                        planet.Id,
                        new VegetationModelParameters())
                ],
                grazerModels:
                [
                    new GrazerModelDefinition(
                        planet.Id,
                        new GrazerModelParameters())
                ],
                biogeochemistryModels:
                [
                    new BiogeochemistryModelDefinition(
                        planet.Id,
                        new BiogeochemistryModelParameters())
                ]);

        var singleBatch =
            new SimulationSession(
                world,
                definition);

        var dailyBatches =
            new SimulationSession(
                world,
                definition);

        singleBatch.Advance(
            7 * OneDaySeconds);

        for (var day = 0; day < 7; day++)
        {
            dailyBatches.Advance(
                OneDaySeconds);
        }

        Assert.Equal(
            singleBatch.CurrentWorld.CurrentTime.TotalSeconds,
            dailyBatches.CurrentWorld.CurrentTime.TotalSeconds);

        var singleVegetation =
            Assert.Single(
                    singleBatch.CurrentWorld.Vegetation)
                .Cells
                .Select(
                    cell =>
                        cell.LiveBiomassKilogramsPerSquareMeter)
                .ToArray();

        var dailyVegetation =
            Assert.Single(
                    dailyBatches.CurrentWorld.Vegetation)
                .Cells
                .Select(
                    cell =>
                        cell.LiveBiomassKilogramsPerSquareMeter)
                .ToArray();

        Assert.Equal(
            singleVegetation,
            dailyVegetation);

        var singleGrazers =
            singleBatch.CurrentWorld.GrazerCohorts
                .OrderBy(
                    cohort =>
                        cohort.Id.Value)
                .Select(
                    cohort =>
                        (
                            cohort.Id,
                            cohort.MemberCount,
                            cohort.LatitudeDegrees,
                            cohort.LongitudeDegrees,
                            cohort.Material.LiveBiomassKilograms,
                            cohort.Material.LiveNitrogenKilograms
                        ))
                .ToArray();

        var dailyGrazers =
            dailyBatches.CurrentWorld.GrazerCohorts
                .OrderBy(
                    cohort =>
                        cohort.Id.Value)
                .Select(
                    cohort =>
                        (
                            cohort.Id,
                            cohort.MemberCount,
                            cohort.LatitudeDegrees,
                            cohort.LongitudeDegrees,
                            cohort.Material.LiveBiomassKilograms,
                            cohort.Material.LiveNitrogenKilograms
                        ))
                .ToArray();

        Assert.Equal(
            singleGrazers,
            dailyGrazers);

        var singleAnimals =
            singleBatch.CurrentWorld.Animals
                .OrderBy(
                    animal =>
                        animal.Id.Value)
                .Select(
                    animal =>
                        (
                            animal.Id,
                            animal.LatitudeDegrees,
                            animal.LongitudeDegrees,
                            animal.EnergyReserve,
                            animal.Health,
                            animal.Activity,
                            animal.Material.LiveBiomassKilograms,
                            animal.Material.LiveNitrogenKilograms,
                            animal.WolfLifecycle?.Pregnancy
                                ?.ConceptionTimeSeconds
                        ))
                .ToArray();

        var dailyAnimals =
            dailyBatches.CurrentWorld.Animals
                .OrderBy(
                    animal =>
                        animal.Id.Value)
                .Select(
                    animal =>
                        (
                            animal.Id,
                            animal.LatitudeDegrees,
                            animal.LongitudeDegrees,
                            animal.EnergyReserve,
                            animal.Health,
                            animal.Activity,
                            animal.Material.LiveBiomassKilograms,
                            animal.Material.LiveNitrogenKilograms,
                            animal.WolfLifecycle?.Pregnancy
                                ?.ConceptionTimeSeconds
                        ))
                .ToArray();

        Assert.Equal(
            singleAnimals,
            dailyAnimals);

        Assert.Equal(
            singleBatch.Timeline.Events.Length,
            dailyBatches.Timeline.Events.Length);

        for (var index = 0;
             index < singleBatch.Timeline.Events.Length;
             index++)
        {
            var singleEvent =
                singleBatch.Timeline.Events[index];

            var dailyEvent =
                dailyBatches.Timeline.Events[index];

            Assert.Equal(
                singleEvent.OccurredAt.TotalSeconds,
                dailyEvent.OccurredAt.TotalSeconds);

            Assert.Equal(
                singleEvent.Cause,
                dailyEvent.Cause);

            Assert.Equal(
                singleEvent.Summary,
                dailyEvent.Summary);

            Assert.Equal(
                singleEvent.AffectedPlanetId,
                dailyEvent.AffectedPlanetId);

            Assert.Equal(
                singleEvent.ElapsedSeconds,
                dailyEvent.ElapsedSeconds);

            Assert.Equal(
                singleEvent.Metrics.Count,
                dailyEvent.Metrics.Count);

            foreach (var metric in singleEvent.Metrics)
            {
                Assert.True(
                    dailyEvent.Metrics.TryGetValue(
                        metric.Key,
                        out var dailyValue),
                    $"Missing metric '{metric.Key}' " +
                    $"for event '{singleEvent.Cause}'.");

                Assert.Equal(
                    metric.Value,
                    dailyValue);
            }
        }
    }
}
