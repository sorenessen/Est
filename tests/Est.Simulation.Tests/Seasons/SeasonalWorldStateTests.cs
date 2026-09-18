using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Seasons;
using Est.Simulation.Time;
using Est.Simulation.Worlds;

namespace Est.Simulation.Tests.Seasons;

public class SeasonalWorldStateTests
{
    [Fact]
    public void Constructor_PreservesSeasonalState()
    {
        var planet =
            CreatePlanet();

        var seasonalState =
            new PlanetSeasonalState(
                planet.Id);

        var world =
            CreateWorld(
                [planet],
                [seasonalState]);

        Assert.Same(
            seasonalState,
            Assert.Single(
                world.SeasonalStates));
    }

    [Fact]
    public void Constructor_RejectsDuplicateSeasonalStateForPlanet()
    {
        var planet =
            CreatePlanet();

        var first =
            new PlanetSeasonalState(
                planet.Id);

        var second =
            new PlanetSeasonalState(
                planet.Id);

        Assert.Throws<ArgumentException>(
            () =>
                CreateWorld(
                    [planet],
                    [first, second]));
    }

    [Fact]
    public void Constructor_RejectsSeasonalStateForUnknownPlanet()
    {
        var planet =
            CreatePlanet();

        var seasonalState =
            new PlanetSeasonalState(
                PlanetId.New());

        Assert.Throws<ArgumentException>(
            () =>
                CreateWorld(
                    [planet],
                    [seasonalState]));
    }

    [Fact]
    public void CopyAndFork_PreserveSeasonalState()
    {
        var planet =
            CreatePlanet();

        var seasonalState =
            new PlanetSeasonalState(
                planet.Id,
                SeasonalControlMode.Override,
                overrideContext:
                    new SeasonalContext(
                        "cold",
                        0.8));

        var world =
            CreateWorld(
                [planet],
                [seasonalState]);

        var copy =
            world.Copy();

        var fork =
            world.Fork();

        Assert.Same(
            seasonalState,
            Assert.Single(
                copy.SeasonalStates));

        Assert.Same(
            seasonalState,
            Assert.Single(
                fork.SeasonalStates));
    }

    [Fact]
    public void AdvanceBy_PreservesSeasonalStateWithoutChangingContext()
    {
        var planet =
            CreatePlanet();

        var context =
            new SeasonalContext(
                "wet",
                0.25);

        var seasonalState =
            new PlanetSeasonalState(
                planet.Id,
                SeasonalControlMode.Derived,
                derivedContext:
                    context);

        var world =
            CreateWorld(
                [planet],
                [seasonalState]);

        var advanced =
            world.AdvanceBy(
                86_400);

        Assert.Equal(
            86_400,
            advanced.CurrentTime.TotalSeconds);

        var advancedSeasonalState =
            Assert.Single(
                advanced.SeasonalStates);

        Assert.Same(
            seasonalState,
            advancedSeasonalState);

        Assert.Same(
            context,
            advancedSeasonalState.EffectiveContext);
    }

    [Fact]
    public void ReplaceSeasonalStates_ReplacesCollection()
    {
        var planet =
            CreatePlanet();

        var original =
            new PlanetSeasonalState(
                planet.Id);

        var replacement =
            new PlanetSeasonalState(
                planet.Id,
                SeasonalControlMode.Override,
                overrideContext:
                    new SeasonalContext(
                        "dry"));

        var world =
            CreateWorld(
                [planet],
                [original]);

        var result =
            world.ReplaceSeasonalStates(
                [replacement]);

        Assert.Same(
            original,
            Assert.Single(
                world.SeasonalStates));

        Assert.Same(
            replacement,
            Assert.Single(
                result.SeasonalStates));
    }

    [Fact]
    public void ReplaceOperation_ReplacesOnlyTargetPlanetSeasonalState()
    {
        var firstPlanet =
            CreatePlanet();

        var secondPlanet =
            CreatePlanet();

        var firstOriginal =
            new PlanetSeasonalState(
                firstPlanet.Id);

        var secondOriginal =
            new PlanetSeasonalState(
                secondPlanet.Id);

        var replacement =
            new PlanetSeasonalState(
                firstPlanet.Id,
                SeasonalControlMode.Override,
                overrideContext:
                    new SeasonalContext(
                        "cold"));

        var world =
            CreateWorld(
                [firstPlanet, secondPlanet],
                [firstOriginal, secondOriginal]);

        var result =
            new ReplacePlanetSeasonalStateOperation(
                replacement)
            .Apply(
                world);

        Assert.Equal(
            2,
            result.SeasonalStates.Length);

        Assert.Same(
            replacement,
            result.SeasonalStates.Single(
                state =>
                    state.PlanetId ==
                    firstPlanet.Id));

        Assert.Same(
            secondOriginal,
            result.SeasonalStates.Single(
                state =>
                    state.PlanetId ==
                    secondPlanet.Id));
    }

    [Fact]
    public void ReplaceOperation_RejectsUnknownPlanet()
    {
        var world =
            CreateWorld(
                [CreatePlanet()],
                []);

        var operation =
            new ReplacePlanetSeasonalStateOperation(
                new PlanetSeasonalState(
                    PlanetId.New()));

        Assert.Throws<PlanetNotFoundException>(
            () =>
                operation.Apply(
                    world));
    }

    private static WorldState CreateWorld(
        IEnumerable<PlanetState> planets,
        IEnumerable<PlanetSeasonalState> seasonalStates)
    {
        return new WorldState(
            WorldId.New(),
            SimulationTime.Zero,
            planets,
            [],
            seasonalStates:
                seasonalStates);
    }

    private static PlanetState CreatePlanet()
    {
        return new PlanetState(
            PlanetId.New(),
            "Test",
            5.9722e24,
            6_371_000,
            new PlanetEnvironment(
                288.15,
                0.71,
                0.03,
                AtmosphereState.Vacuum));
    }
}
