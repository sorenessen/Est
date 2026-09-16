using Est.Simulation.Organisms;

namespace Est.Simulation.Animals;

public sealed record WolfLifecycleParameters
{
    public const long SecondsPerDay =
        86_400;

    public const long SecondsPerYear =
        31_536_000;

    public WolfLifecycleParameters(
        long maturityAgeSeconds =
            2 * SecondsPerYear,
        long reproductiveAgeMinimumSeconds =
            2 * SecondsPerYear,
        long gestationSeconds =
            63 * SecondsPerDay,
        int minimumLitterSize = 4,
        int maximumLitterSize = 6,
        long materialMaturityAgeSeconds =
            180 * SecondsPerDay,
        double newbornLiveBiomassKilograms =
            0.45,
        double newbornLiveNitrogenKilograms =
            0.01125,
        double matureLiveBiomassKilograms =
            40,
        double matureLiveNitrogenKilograms =
            1)
    {
        if (gestationSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gestationSeconds));
        }

        if (minimumLitterSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumLitterSize));
        }

        if (maximumLitterSize <
            minimumLitterSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumLitterSize));
        }

        if (materialMaturityAgeSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(materialMaturityAgeSeconds));
        }

        var newbornMaterial =
            new OrganismMaterialComposition(
                newbornLiveBiomassKilograms,
                newbornLiveNitrogenKilograms);

        var matureMaterial =
            new OrganismMaterialComposition(
                matureLiveBiomassKilograms,
                matureLiveNitrogenKilograms);

        if (matureMaterial
                .LiveBiomassKilogramsPerUnit <
            newbornMaterial
                .LiveBiomassKilogramsPerUnit ||
            matureMaterial
                .LiveNitrogenKilogramsPerUnit <
            newbornMaterial
                .LiveNitrogenKilogramsPerUnit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(matureLiveBiomassKilograms),
                "Mature wolf material must not be less than newborn material.");
        }

        LifecycleTiming =
            new OrganismLifecycleTiming(
                maturityAgeSeconds,
                reproductiveAgeMinimumSeconds);

        GestationSeconds =
            gestationSeconds;

        MinimumLitterSize =
            minimumLitterSize;

        MaximumLitterSize =
            maximumLitterSize;

        MaterialMaturityAgeSeconds =
            materialMaturityAgeSeconds;

        NewbornMaterial =
            newbornMaterial;

        MatureMaterial =
            matureMaterial;
    }

    public OrganismLifecycleTiming
        LifecycleTiming { get; }

    public long GestationSeconds { get; }

    public int MinimumLitterSize { get; }

    public int MaximumLitterSize { get; }

    public long MaterialMaturityAgeSeconds
    {
        get;
    }

    public OrganismMaterialComposition
        NewbornMaterial { get; }

    public OrganismMaterialComposition
        MatureMaterial { get; }

    public OrganismSeasonalStrategy
        SeasonalStrategies =>
            OrganismSeasonalStrategy.Breeding;

    public OrganismMaterialState
        MaterialTargetAtAgeSeconds(
            long ageSeconds)
    {
        if (ageSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ageSeconds));
        }

        var maturityFraction =
            Math.Clamp(
                ageSeconds /
                (double)MaterialMaturityAgeSeconds,
                0,
                1);

        return new OrganismMaterialState(
            NewbornMaterial
                .LiveBiomassKilogramsPerUnit +
            (MatureMaterial
                 .LiveBiomassKilogramsPerUnit -
             NewbornMaterial
                 .LiveBiomassKilogramsPerUnit) *
            maturityFraction,
            NewbornMaterial
                .LiveNitrogenKilogramsPerUnit +
            (MatureMaterial
                 .LiveNitrogenKilogramsPerUnit -
             NewbornMaterial
                 .LiveNitrogenKilogramsPerUnit) *
            maturityFraction);
    }
}
