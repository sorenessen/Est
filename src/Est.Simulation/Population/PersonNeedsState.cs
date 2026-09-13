namespace Est.Simulation.Population;

public sealed record PersonNeedsState
{
    public PersonNeedsState(
        double energyReserve = 1,
        double health = 1)
    {
        ValidateNormalized(
            energyReserve,
            nameof(energyReserve));

        ValidateNormalized(
            health,
            nameof(health));

        EnergyReserve = energyReserve;
        Health = health;
    }

    public double EnergyReserve { get; }

    public double Health { get; }

    public PersonNeedsState WithEnergyReserve(
        double energyReserve)
    {
        return new PersonNeedsState(
            energyReserve,
            Health);
    }

    public PersonNeedsState WithHealth(
        double health)
    {
        return new PersonNeedsState(
            EnergyReserve,
            health);
    }

    public PersonNeedsState AdvanceWithoutFood(
        double elapsedDays)
    {
        if (!double.IsFinite(elapsedDays) ||
            elapsedDays < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elapsedDays));
        }

        const double energyConsumedPerDay = 1d / 30d;
        const double starvationHealthLossPerDay =
            1d / 14d;

        var requiredEnergy =
            elapsedDays * energyConsumedPerDay;

        var remainingEnergy =
            Math.Max(
                0,
                EnergyReserve - requiredEnergy);

        var starvationDays =
            requiredEnergy <= EnergyReserve
                ? 0
                : (requiredEnergy - EnergyReserve)
                    / energyConsumedPerDay;

        var remainingHealth =
            Math.Max(
                0,
                Health -
                starvationDays
                    * starvationHealthLossPerDay);

        return new PersonNeedsState(
            remainingEnergy,
            remainingHealth);
    }

    private static void ValidateNormalized(
        double value,
        string parameterName)
    {
        if (!double.IsFinite(value) ||
            value < 0 ||
            value > 1)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Value must be finite and within [0, 1].");
        }
    }
}
