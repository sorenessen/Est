namespace Est.Simulation.Animals;

public sealed record WolfLifecycleState
{
    public WolfLifecycleState(
        WolfSex sex,
        WolfPregnancyState? pregnancy = null)
    {
        if (pregnancy is not null &&
            sex != WolfSex.Female)
        {
            throw new ArgumentException(
                "Only a female wolf can carry a pregnancy.",
                nameof(pregnancy));
        }

        Sex = sex;
        Pregnancy = pregnancy;
    }

    public WolfSex Sex { get; }

    public WolfPregnancyState? Pregnancy { get; }

    public WolfLifecycleState WithPregnancy(
        WolfPregnancyState pregnancy)
    {
        ArgumentNullException.ThrowIfNull(
            pregnancy);

        return new WolfLifecycleState(
            Sex,
            pregnancy);
    }

    public WolfLifecycleState WithoutPregnancy()
    {
        return new WolfLifecycleState(
            Sex);
    }
}
