using Est.Simulation.Grazers;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Grazers;

public sealed class WorldGrazerCohortStateTests
{
    [Fact]
    public void World_StoresGrazerCohortsAndPreservesThemAcrossCopyAndFork()
    {
        var planet =
            CreatePlanet(
                "Grazer World");

        var first =
            CreateCohort(
                planet,
                100,
                10,
                20);

        var second =
            CreateCohort(
                planet,
                250,
                -15,
                45);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [planet],
                [],
                grazerCohorts:
                [
                    first,
                    second
                ]);

        Assert.Equal(
            2,
            world.GrazerCohorts.Length);

        Assert.Equal(
            world.GrazerCohorts,
            world.Copy().GrazerCohorts);

        Assert.Equal(
            world.GrazerCohorts,
            world.Fork().GrazerCohorts);
    }

    [Fact]
    public void World_RejectsDuplicateGrazerCohortIdentity()
    {
        var planet =
            CreatePlanet(
                "Duplicate World");

        var id =
            GrazerCohortId.New();

        var first =
            new GrazerCohortState(
                id,
                planet.Id,
                10,
                0,
                0);

        var second =
            new GrazerCohortState(
                id,
                planet.Id,
                20,
                1,
                1);

        Assert.Throws<ArgumentException>(
            () =>
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet],
                    [],
                    grazerCohorts:
                    [
                        first,
                        second
                    ]));
    }

    [Fact]
    public void World_RejectsGrazerCohortWithoutPlanet()
    {
        var planet =
            CreatePlanet(
                "Known World");

        var otherPlanet =
            CreatePlanet(
                "Other World");

        var cohort =
            CreateCohort(
                otherPlanet,
                10,
                0,
                0);

        Assert.Throws<ArgumentException>(
            () =>
                new WorldState(
                    WorldId.New(),
                    SimulationTime.Zero,
                    [planet],
                    [],
                    grazerCohorts:
                    [
                        cohort
                    ]));
    }

    [Fact]
    public void ReplacePlanetGrazerCohorts_ReplacesOnlyTargetPlanet()
    {
        var firstPlanet =
            CreatePlanet(
                "First");

        var secondPlanet =
            CreatePlanet(
                "Second");

        var oldFirst =
            CreateCohort(
                firstPlanet,
                10,
                0,
                0);

        var preservedSecond =
            CreateCohort(
                secondPlanet,
                20,
                5,
                5);

        var replacement =
            CreateCohort(
                firstPlanet,
                30,
                10,
                10);

        var world =
            new WorldState(
                WorldId.New(),
                SimulationTime.Zero,
                [
                    firstPlanet,
                    secondPlanet
                ],
                [],
                grazerCohorts:
                [
                    oldFirst,
                    preservedSecond
                ]);

        var changed =
            new ReplacePlanetGrazerCohortsOperation(
                firstPlanet.Id,
                [replacement])
            .Apply(
                world);

        Assert.DoesNotContain(
            oldFirst,
            changed.GrazerCohorts);

        Assert.Contains(
            replacement,
            changed.GrazerCohorts);

        Assert.Contains(
            preservedSecond,
            changed.GrazerCohorts);

        Assert.Equal(
            2,
            changed.GrazerCohorts.Length);
    }

    private static PlanetState CreatePlanet(
        string name)
    {
        return new PlanetState(
            PlanetId.New(),
            name,
            5.0e24,
            6_000_000,
            new PlanetEnvironment(
                288,
                0.60,
                0.05,
                AtmosphereState.Vacuum));
    }

    private static GrazerCohortState CreateCohort(
        PlanetState planet,
        int memberCount,
        double latitude,
        double longitude)
    {
        return new GrazerCohortState(
            GrazerCohortId.New(),
            planet.Id,
            memberCount,
            latitude,
            longitude);
    }
}
