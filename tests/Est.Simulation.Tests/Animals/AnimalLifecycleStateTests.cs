using Est.Simulation.Animals;
using Est.Simulation.Planets;

namespace Est.Simulation.Tests.Animals;

public sealed class AnimalLifecycleStateTests
{
    private const long OneYearSeconds =
        31_536_000;

    [Fact]
    public void AnimalState_AgeAndParentSurviveStateChanges()
    {
        var parentId =
            AnimalId.New();

        var animal =
            new AnimalState(
                AnimalId.New(),
                PlanetId.New(),
                AnimalSpecies.Wolf,
                latitudeDegrees: 10,
                longitudeDegrees: 20,
                birthTimeSeconds:
                    -3 * OneYearSeconds,
                parentId:
                    parentId);

        Assert.Equal(
            3,
            animal.AgeYears(0),
            precision: 10);

        var changed =
            animal.WithState(
                latitudeDegrees: 11,
                longitudeDegrees: 21,
                energyReserve: 0.4,
                health: 0.8,
                activity:
                    AnimalActivity.Traveling);

        Assert.Equal(
            animal.BirthTimeSeconds,
            changed.BirthTimeSeconds);

        Assert.Equal(
            parentId,
            changed.ParentId);

        Assert.Equal(
            3,
            changed.AgeYears(0),
            precision: 10);
    }

    [Fact]
    public void AnimalState_AgeClampsBeforeBirth()
    {
        var animal =
            new AnimalState(
                AnimalId.New(),
                PlanetId.New(),
                AnimalSpecies.Wolf,
                latitudeDegrees: 0,
                longitudeDegrees: 0,
                birthTimeSeconds:
                    OneYearSeconds);

        Assert.Equal(
            0,
            animal.AgeYears(0));
    }
}
