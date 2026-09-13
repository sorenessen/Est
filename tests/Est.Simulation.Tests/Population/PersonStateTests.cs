using Est.Simulation.Planets;
using Est.Simulation.Population;

namespace Est.Simulation.Tests.Population;

public sealed class PersonStateTests
{
    [Fact]
    public void Constructor_DefaultsToHealthyIdleState()
    {
        var person = CreatePerson();

        Assert.Equal(
            1,
            person.Needs.EnergyReserve);

        Assert.Equal(
            1,
            person.Needs.Health);

        Assert.Equal(
            PersonActivity.Idle,
            person.Activity);
    }

    [Fact]
    public void MoveTo_PreservesNeedsAndActivity()
    {
        var needs =
            new PersonNeedsState(
                energyReserve: 0.35,
                health: 0.8);

        var person =
            new PersonState(
                PersonId.New(),
                PlanetId.New(),
                PersonSex.Male,
                0,
                10,
                20,
                needs: needs,
                activity: PersonActivity.Foraging);

        var moved = person.MoveTo(11, 21);

        Assert.Equal(11, moved.LatitudeDegrees);
        Assert.Equal(21, moved.LongitudeDegrees);
        Assert.Equal(needs, moved.Needs);
        Assert.Equal(
            PersonActivity.Foraging,
            moved.Activity);
    }

    [Theory]
    [InlineData(-0.01, 1)]
    [InlineData(1.01, 1)]
    [InlineData(1, -0.01)]
    [InlineData(1, 1.01)]
    public void Needs_RejectsValuesOutsideNormalizedRange(
        double energyReserve,
        double health)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new PersonNeedsState(
                    energyReserve,
                    health));
    }

    [Fact]
    public void Needs_ConsumeEnergyAsTimePasses()
    {
        var needs = new PersonNeedsState();

        var advanced =
            needs.AdvanceWithoutFood(15);

        Assert.Equal(
            0.5,
            advanced.EnergyReserve,
            precision: 10);

        Assert.Equal(
            1,
            advanced.Health,
            precision: 10);
    }

    [Fact]
    public void Needs_LoseHealthAfterEnergyIsExhausted()
    {
        var needs = new PersonNeedsState();

        var advanced =
            needs.AdvanceWithoutFood(37);

        Assert.Equal(
            0,
            advanced.EnergyReserve,
            precision: 10);

        Assert.Equal(
            0.5,
            advanced.Health,
            precision: 10);
    }

    [Fact]
    public void Needs_EventuallyReachZeroHealthWithoutFood()
    {
        var needs = new PersonNeedsState();

        var advanced =
            needs.AdvanceWithoutFood(60);

        Assert.Equal(0, advanced.EnergyReserve);
        Assert.Equal(0, advanced.Health);
    }

    private static PersonState CreatePerson()
    {
        return new PersonState(
            PersonId.New(),
            PlanetId.New(),
            PersonSex.Female,
            0,
            0,
            0);
    }
}
