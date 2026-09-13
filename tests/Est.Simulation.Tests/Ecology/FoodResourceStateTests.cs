using Est.Simulation.Ecology;
using Est.Simulation.Planets;

namespace Est.Simulation.Tests.Ecology;

public sealed class FoodResourceStateTests
{
    [Fact]
    public void Consume_ReducesAvailableEnergy()
    {
        var resource =
            new FoodResourceState(
                FoodResourceId.New(),
                PlanetId.New(),
                10,
                20,
                100);

        var consumed =
            resource.Consume(25);

        Assert.Equal(
            100,
            resource.AvailableEnergy);

        Assert.Equal(
            75,
            consumed.AvailableEnergy);
    }

    [Fact]
    public void Consume_AllowsResourceToBeExhausted()
    {
        var resource =
            new FoodResourceState(
                FoodResourceId.New(),
                PlanetId.New(),
                10,
                20,
                100);

        var consumed =
            resource.Consume(100);

        Assert.Equal(
            0,
            consumed.AvailableEnergy);
    }

    [Fact]
    public void Consume_RejectsMoreThanAvailable()
    {
        var resource =
            new FoodResourceState(
                FoodResourceId.New(),
                PlanetId.New(),
                10,
                20,
                100);

        Assert.Throws<InvalidOperationException>(
            () => resource.Consume(101));
    }

    [Fact]
    public void Constructor_DefaultsCapacityToInitialEnergy()
    {
        var resource =
            new FoodResourceState(
                FoodResourceId.New(),
                PlanetId.New(),
                10,
                20,
                100);

        Assert.Equal(
            100,
            resource.CapacityEnergy);

        Assert.Equal(
            0,
            resource.RecoveryEnergyPerDay);
    }

    [Fact]
    public void Recover_IncreasesAvailableEnergy()
    {
        var resource =
            new FoodResourceState(
                FoodResourceId.New(),
                PlanetId.New(),
                10,
                20,
                availableEnergy: 25,
                capacityEnergy: 100,
                recoveryEnergyPerDay: 10);

        var recovered =
            resource.Recover(2);

        Assert.Equal(
            45,
            recovered.AvailableEnergy);

        Assert.Equal(
            25,
            resource.AvailableEnergy);
    }

    [Fact]
    public void Recover_DoesNotExceedCapacity()
    {
        var resource =
            new FoodResourceState(
                FoodResourceId.New(),
                PlanetId.New(),
                10,
                20,
                availableEnergy: 95,
                capacityEnergy: 100,
                recoveryEnergyPerDay: 10);

        var recovered =
            resource.Recover(2);

        Assert.Equal(
            100,
            recovered.AvailableEnergy);
    }

    [Fact]
    public void Consume_PreservesCapacityAndRecoveryRate()
    {
        var resource =
            new FoodResourceState(
                FoodResourceId.New(),
                PlanetId.New(),
                10,
                20,
                availableEnergy: 100,
                capacityEnergy: 100,
                recoveryEnergyPerDay: 5);

        var consumed =
            resource.Consume(25);

        Assert.Equal(
            100,
            consumed.CapacityEnergy);

        Assert.Equal(
            5,
            consumed.RecoveryEnergyPerDay);
    }

    [Fact]
    public void Constructor_RejectsCapacityBelowAvailableEnergy()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new FoodResourceState(
                    FoodResourceId.New(),
                    PlanetId.New(),
                    0,
                    0,
                    availableEnergy: 100,
                    capacityEnergy: 50));
    }

    [Fact]
    public void Constructor_RejectsNegativeRecoveryRate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new FoodResourceState(
                    FoodResourceId.New(),
                    PlanetId.New(),
                    0,
                    0,
                    availableEnergy: 100,
                    capacityEnergy: 100,
                    recoveryEnergyPerDay: -1));
    }

    [Theory]
    [InlineData(-90.1, 0)]
    [InlineData(90.1, 0)]
    [InlineData(0, -180.1)]
    [InlineData(0, 180.1)]
    public void Constructor_RejectsInvalidLocation(
        double latitude,
        double longitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new FoodResourceState(
                    FoodResourceId.New(),
                    PlanetId.New(),
                    latitude,
                    longitude,
                    100));
    }

    [Fact]
    public void Constructor_RejectsNegativeAvailableEnergy()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new FoodResourceState(
                    FoodResourceId.New(),
                    PlanetId.New(),
                    0,
                    0,
                    -1));
    }
}
