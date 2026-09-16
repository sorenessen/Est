using Est.Simulation.Organisms;

namespace Est.Simulation.Population;

public sealed record PopulationModelParameters
{
    public PopulationModelParameters(
        int seed = 1,
        double annualBirthRatePerEligibleFemale = 0.12,
        double annualAdultMigrationRate = 0.03,
        double annualBaseMortalityRate = 0.004,
        double annualElderMortalityRate = 0.08,
        double reproductiveAgeMinimumYears = 18,
        double reproductiveAgeMaximumYears = 40,
        double elderAgeYears = 70,
        double localMigrationDegrees = 1.5,
        double longMigrationProbability = 0.08,
        double longMigrationDegrees = 15,
        double conceptionProbabilityPerMatingOpportunity = 0.20,
        double gestationDays = 280,
        double newbornLiveBiomassKilograms = 3.5,
        double newbornLiveNitrogenKilograms = 0.0875,
        double matureLiveBiomassKilograms = 70,
        double matureLiveNitrogenKilograms = 1.75)
    {
        ValidateProbability(
            annualAdultMigrationRate,
            nameof(annualAdultMigrationRate));

        ValidateProbability(
            longMigrationProbability,
            nameof(longMigrationProbability));

        ValidateProbability(
            conceptionProbabilityPerMatingOpportunity,
            nameof(conceptionProbabilityPerMatingOpportunity));

        if (!double.IsFinite(annualBirthRatePerEligibleFemale) ||
            annualBirthRatePerEligibleFemale < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(annualBirthRatePerEligibleFemale));
        }

        if (!double.IsFinite(annualBaseMortalityRate) ||
            annualBaseMortalityRate < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(annualBaseMortalityRate));
        }

        if (!double.IsFinite(annualElderMortalityRate) ||
            annualElderMortalityRate < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(annualElderMortalityRate));
        }

        if (!double.IsFinite(reproductiveAgeMinimumYears) ||
            reproductiveAgeMinimumYears < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reproductiveAgeMinimumYears));
        }

        if (!double.IsFinite(reproductiveAgeMaximumYears) ||
            reproductiveAgeMaximumYears <
            reproductiveAgeMinimumYears)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reproductiveAgeMaximumYears));
        }

        if (!double.IsFinite(elderAgeYears) ||
            elderAgeYears < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elderAgeYears));
        }

        if (!double.IsFinite(localMigrationDegrees) ||
            localMigrationDegrees < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(localMigrationDegrees));
        }

        if (!double.IsFinite(longMigrationDegrees) ||
            longMigrationDegrees < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longMigrationDegrees));
        }

        if (!double.IsFinite(gestationDays) ||
            gestationDays <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gestationDays),
                "Gestation duration must be finite and greater than zero.");
        }

        var newbornMaterial =
            new OrganismMaterialComposition(
                newbornLiveBiomassKilograms,
                newbornLiveNitrogenKilograms);

        var matureMaterial =
            new OrganismMaterialComposition(
                matureLiveBiomassKilograms,
                matureLiveNitrogenKilograms);

        if (matureMaterial.LiveBiomassKilogramsPerUnit <
                newbornMaterial.LiveBiomassKilogramsPerUnit ||
            matureMaterial.LiveNitrogenKilogramsPerUnit <
                newbornMaterial.LiveNitrogenKilogramsPerUnit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(matureLiveBiomassKilograms),
                "Mature human material must not be less than newborn material.");
        }

        Seed = seed;
        AnnualBirthRatePerEligibleFemale =
            annualBirthRatePerEligibleFemale;
        AnnualAdultMigrationRate =
            annualAdultMigrationRate;
        AnnualBaseMortalityRate =
            annualBaseMortalityRate;
        AnnualElderMortalityRate =
            annualElderMortalityRate;
        ReproductiveAgeMinimumYears =
            reproductiveAgeMinimumYears;
        ReproductiveAgeMaximumYears =
            reproductiveAgeMaximumYears;
        ElderAgeYears = elderAgeYears;
        LocalMigrationDegrees = localMigrationDegrees;
        LongMigrationProbability =
            longMigrationProbability;
        LongMigrationDegrees = longMigrationDegrees;
        ConceptionProbabilityPerMatingOpportunity =
            conceptionProbabilityPerMatingOpportunity;
        GestationDays = gestationDays;
        NewbornMaterial = newbornMaterial;
        MatureMaterial = matureMaterial;
    }

    public int Seed { get; }

    public double AnnualBirthRatePerEligibleFemale { get; }

    public double AnnualAdultMigrationRate { get; }

    public double AnnualBaseMortalityRate { get; }

    public double AnnualElderMortalityRate { get; }

    public double ReproductiveAgeMinimumYears { get; }

    public double ReproductiveAgeMaximumYears { get; }

    public double ElderAgeYears { get; }

    public double LocalMigrationDegrees { get; }

    public double LongMigrationProbability { get; }

    public double LongMigrationDegrees { get; }

    public double ConceptionProbabilityPerMatingOpportunity { get; }

    public double GestationDays { get; }

    public OrganismMaterialComposition NewbornMaterial { get; }

    public OrganismMaterialComposition MatureMaterial { get; }

    public OrganismMaterialState MaterialTargetAtAgeYears(
        double ageYears)
    {
        if (!double.IsFinite(ageYears) ||
            ageYears < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ageYears),
                "Age must be finite and non-negative.");
        }

        var maturityFraction =
            ReproductiveAgeMinimumYears <= 0
                ? 1
                : Math.Clamp(
                    ageYears /
                    ReproductiveAgeMinimumYears,
                    0,
                    1);

        return new OrganismMaterialState(
            NewbornMaterial.LiveBiomassKilogramsPerUnit +
            (MatureMaterial.LiveBiomassKilogramsPerUnit -
             NewbornMaterial.LiveBiomassKilogramsPerUnit) *
            maturityFraction,
            NewbornMaterial.LiveNitrogenKilogramsPerUnit +
            (MatureMaterial.LiveNitrogenKilogramsPerUnit -
             NewbornMaterial.LiveNitrogenKilogramsPerUnit) *
            maturityFraction);
    }

    private static void ValidateProbability(
        double value,
        string parameterName)
    {
        if (!double.IsFinite(value) ||
            value < 0 ||
            value > 1)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Probability must be finite and within [0, 1].");
        }
    }
}
