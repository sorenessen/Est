using Est.Simulation.Animals;
using Est.Simulation.Birds;
using Est.Simulation.Grazers;
using Est.Simulation.Organisms;
using Est.Simulation.Planets;
using Est.Simulation.Population;

namespace Est.Simulation.Tests.Organisms;

public sealed class OrganismMaterialAttachmentTests
{
    [Fact]
    public void PersonState_PreservesMaterialAcrossStateChanges()
    {
        var material =
            new OrganismMaterialState(72, 1.8);

        var person =
            new PersonState(
                new PersonId(Guid.NewGuid()),
                PlanetId.New(),
                PersonSex.Female,
                birthTimeSeconds: 0,
                latitudeDegrees: 10,
                longitudeDegrees: 20,
                material: material);

        var moved =
            person.MoveTo(11, 21);

        var survived =
            person.WithSurvivalState(
                new PersonNeedsState(
                    energyReserve: 0.5,
                    health: 0.9),
                PersonActivity.Foraging);

        Assert.Same(material, person.Material);
        Assert.Same(material, moved.Material);
        Assert.Same(material, survived.Material);
    }

    [Fact]
    public void AnimalState_PreservesMaterialAcrossStateChanges()
    {
        var material =
            new OrganismMaterialState(40, 1);

        var animal =
            new AnimalState(
                new AnimalId(Guid.NewGuid()),
                PlanetId.New(),
                AnimalSpecies.Wolf,
                latitudeDegrees: 10,
                longitudeDegrees: 20,
                material: material);

        var changed =
            animal.WithState(
                latitudeDegrees: 11,
                longitudeDegrees: 21,
                energyReserve: 0.4,
                health: 0.8,
                activity: AnimalActivity.Traveling);

        Assert.Same(material, animal.Material);
        Assert.Same(material, changed.Material);
    }

    [Fact]
    public void BirdFlockState_CarriesAggregateMaterial()
    {
        var material =
            new OrganismMaterialState(250, 6);

        var flock =
            new BirdFlockState(
                new BirdFlockId(Guid.NewGuid()),
                PlanetId.New(),
                memberCount: 50,
                latitudeDegrees: 10,
                longitudeDegrees: 20,
                material: material);

        Assert.Same(material, flock.Material);
    }

    [Fact]
    public void GrazerCohortState_CarriesAggregateMaterial()
    {
        var material =
            new OrganismMaterialState(5_000, 125);

        var cohort =
            new GrazerCohortState(
                new GrazerCohortId(Guid.NewGuid()),
                PlanetId.New(),
                memberCount: 25,
                latitudeDegrees: 10,
                longitudeDegrees: 20,
                material: material);

        Assert.Same(material, cohort.Material);
    }

    [Fact]
    public void BirdFlockState_SurvivalReducesAggregateMaterialProportionally()
    {
        var flock =
            new BirdFlockState(
                new BirdFlockId(Guid.NewGuid()),
                PlanetId.New(),
                memberCount: 100,
                latitudeDegrees: 10,
                longitudeDegrees: 20,
                material:
                    new OrganismMaterialState(
                        liveBiomassKilograms: 50,
                        liveNitrogenKilograms: 1.25));

        var survivors =
            flock.WithSurvivalState(
                survivingMemberCount: 40,
                latitudeDegrees: 11,
                longitudeDegrees: 21);

        Assert.Equal(
            40,
            survivors.MemberCount);

        Assert.Equal(
            20,
            survivors.Material.LiveBiomassKilograms);

        Assert.Equal(
            0.5,
            survivors.Material.LiveNitrogenKilograms);
    }

    [Fact]
    public void GrazerCohortState_SurvivalReducesAggregateMaterialProportionally()
    {
        var cohort =
            new GrazerCohortState(
                new GrazerCohortId(Guid.NewGuid()),
                PlanetId.New(),
                memberCount: 25,
                latitudeDegrees: 10,
                longitudeDegrees: 20,
                material:
                    new OrganismMaterialState(
                        liveBiomassKilograms: 5_000,
                        liveNitrogenKilograms: 125));

        var survivors =
            cohort.WithSurvivalState(
                survivingMemberCount: 20,
                latitudeDegrees: 11,
                longitudeDegrees: 21);

        Assert.Equal(
            20,
            survivors.MemberCount);

        Assert.Equal(
            4_000,
            survivors.Material.LiveBiomassKilograms);

        Assert.Equal(
            100,
            survivors.Material.LiveNitrogenKilograms);
    }

    [Fact]
    public void ExistingConstructors_DefaultToEmptyTransitionalMaterial()
    {
        var planetId =
            PlanetId.New();

        var person =
            new PersonState(
                new PersonId(Guid.NewGuid()),
                planetId,
                PersonSex.Male,
                0,
                0,
                0);

        var animal =
            new AnimalState(
                new AnimalId(Guid.NewGuid()),
                planetId,
                AnimalSpecies.Wolf,
                0,
                0);

        var flock =
            new BirdFlockState(
                new BirdFlockId(Guid.NewGuid()),
                planetId,
                10,
                0,
                0);

        var cohort =
            new GrazerCohortState(
                new GrazerCohortId(Guid.NewGuid()),
                planetId,
                10,
                0,
                0);

        Assert.True(person.Material.IsEmpty);
        Assert.True(animal.Material.IsEmpty);
        Assert.True(flock.Material.IsEmpty);
        Assert.True(cohort.Material.IsEmpty);
    }
}
