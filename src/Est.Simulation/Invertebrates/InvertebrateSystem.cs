using Est.Simulation.Biogeochemistry;
using Est.Simulation.Causality;
using Est.Simulation.Operations;
using Est.Simulation.Organisms;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Vegetation;
using Est.Simulation.Worlds;

namespace Est.Simulation.Invertebrates;

/// <summary>
/// First-pass causal aggregate invertebrate biomass model.
///
/// Local live vegetation establishes carrying capacity and supplies material
/// for realized invertebrate growth. Existing invertebrate biomass can reproduce
/// toward that support ceiling while baseline loss continuously removes biomass.
/// Nitrogen-bearing growth requires an authoritative plant-tissue nitrogen policy
/// and biogeochemistry state. Realized mortality material enters the detrital pool
/// when authoritative biogeochemistry is present.
/// </summary>
public sealed class InvertebrateSystem
    : ICausalSystem
{
    private const double SecondsPerDay =
        86_400d;

    private readonly PlanetId _planetId;
    private readonly InvertebrateModelParameters _parameters;
    private readonly double?
        _plantNitrogenKilogramsPerKilogramLiveBiomass;

    public InvertebrateSystem(
        PlanetId planetId,
        InvertebrateModelParameters parameters,
        double?
            plantNitrogenKilogramsPerKilogramLiveBiomass =
                null)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(
            parameters);

        if (plantNitrogenKilogramsPerKilogramLiveBiomass
                is not null &&
            (!double.IsFinite(
                plantNitrogenKilogramsPerKilogramLiveBiomass.Value) ||
             plantNitrogenKilogramsPerKilogramLiveBiomass.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    plantNitrogenKilogramsPerKilogramLiveBiomass),
                "Plant-tissue nitrogen ratio must be finite and greater than zero.");
        }

        _planetId =
            planetId;

        _parameters =
            parameters;

        _plantNitrogenKilogramsPerKilogramLiveBiomass =
            plantNitrogenKilogramsPerKilogramLiveBiomass;
    }

    public SimulationChange Evaluate(
        WorldState world,
        long elapsedSeconds)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        if (elapsedSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elapsedSeconds));
        }

        var planet =
            world.Planets.FirstOrDefault(
                candidate =>
                    candidate.Id ==
                    _planetId)
            ?? throw new InvalidOperationException(
                "The target planet does not exist in this world.");

        var vegetation =
            world.Vegetation.FirstOrDefault(
                candidate =>
                    candidate.PlanetId ==
                    _planetId)
            ?? throw new InvalidOperationException(
                "Invertebrate integration requires vegetation for the target planet.");

        var invertebrates =
            world.Invertebrates.FirstOrDefault(
                candidate =>
                    candidate.PlanetId ==
                    _planetId)
            ?? throw new InvalidOperationException(
                "Invertebrate integration requires invertebrate state for the target planet.");

        var biogeochemistry =
            world.Biogeochemistry.FirstOrDefault(
                candidate =>
                    candidate.PlanetId ==
                    _planetId);

        if (vegetation.GridDefinition !=
            invertebrates.GridDefinition)
        {
            throw new InvalidOperationException(
                "Invertebrates and vegetation must use the same surface grid.");
        }

        if (biogeochemistry is not null &&
            biogeochemistry.GridDefinition !=
            invertebrates.GridDefinition)
        {
            throw new InvalidOperationException(
                "Invertebrates and biogeochemistry must use the same surface grid.");
        }

        vegetation.ValidateFor(
            planet);

        invertebrates.ValidateFor(
            planet);

        biogeochemistry?.ValidateFor(
            planet);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                invertebrates.GridDefinition);

        var surfaceCellsById =
            grid.Cells.ToDictionary(
                cell =>
                    cell.Id);

        var vegetationBiomassByCellId =
            vegetation.Cells.ToDictionary(
                cell =>
                    cell.CellId,
                cell =>
                    cell.LiveBiomassKilogramsPerSquareMeter);

        var initialBiomassMass =
            TotalBiomassMassKilograms(
                invertebrates,
                surfaceCellsById);

        var detritalBiomassByCellId =
            biogeochemistry?.Cells.ToDictionary(
                cell =>
                    cell.CellId,
                cell =>
                    cell.DetritalBiomassKilogramsPerSquareMeter);

        var detritalNitrogenByCellId =
            biogeochemistry?.Cells.ToDictionary(
                cell =>
                    cell.CellId,
                cell =>
                    cell.DetritalNitrogenKilogramsPerSquareMeter);

        var feedingMaterialTransfers =
            new List<VegetationFeedingEvent>();

        var totalMortalityDetritalBiomassMass =
            0d;

        var totalMortalityDetritalNitrogenMass =
            0d;

        var totalAssimilatedNitrogenMass =
            0d;

        var current =
            invertebrates;

        var integrationSubsteps =
            0;

        var remainingSeconds =
            elapsedSeconds;

        while (remainingSeconds > 0)
        {
            var stepSeconds =
                Math.Min(
                    remainingSeconds,
                    _parameters
                        .MaximumIntegrationStepSeconds);

            var elapsedDays =
                stepSeconds /
                SecondsPerDay;

            var nextCells =
                new InvertebrateCellState[
                    current.Cells.Length];

            for (var index = 0;
                 index < current.Cells.Length;
                 index++)
            {
                var cell =
                    current.Cells[index];

                var biomass =
                    cell.LiveBiomassKilogramsPerSquareMeter;

                if (biomass <= 0)
                {
                    nextCells[index] =
                        cell;

                    continue;
                }

                var vegetationBiomass =
                    vegetationBiomassByCellId[
                        cell.CellId];

                var carryingCapacity =
                    vegetationBiomass *
                    _parameters
                        .CarryingCapacityKilogramsPerKilogramLiveVegetation;

                var growth =
                    0d;

                if (carryingCapacity > 0 &&
                    biomass <
                    carryingCapacity)
                {
                    var remainingCapacityFactor =
                        Math.Max(
                            0,
                            1 -
                            biomass /
                            carryingCapacity);

                    growth =
                        biomass *
                        _parameters
                            .MaximumRelativeGrowthRatePerDay *
                        remainingCapacityFactor *
                        elapsedDays;
                }

                var targetNitrogenRatio =
                    _parameters
                        .LiveNitrogenKilogramsPerKilogramLiveBiomass;

                var maximumMaterialBackedGrowth =
                    vegetationBiomass;

                if (growth > 0 &&
                    targetNitrogenRatio > 0)
                {
                    var plantNitrogenRatio =
                        _plantNitrogenKilogramsPerKilogramLiveBiomass
                        ?? throw new InvalidOperationException(
                            "Nitrogen-bearing invertebrate growth requires an authoritative plant-tissue nitrogen policy.");

                    if (biogeochemistry is null)
                    {
                        throw new InvalidOperationException(
                            "Nitrogen-bearing invertebrate growth requires authoritative biogeochemistry state for the target planet.");
                    }

                    maximumMaterialBackedGrowth =
                        Math.Min(
                            maximumMaterialBackedGrowth,
                            vegetationBiomass *
                            plantNitrogenRatio /
                            targetNitrogenRatio);
                }

                var realizedGrowth =
                    Math.Min(
                        growth,
                        Math.Min(
                            maximumMaterialBackedGrowth,
                            Math.Max(
                                0,
                                carryingCapacity -
                                biomass)));

                var assimilatedNitrogen =
                    realizedGrowth *
                    targetNitrogenRatio;

                var consumedVegetation =
                    realizedGrowth;

                if (realizedGrowth > 0 &&
                    targetNitrogenRatio > 0)
                {
                    var plantNitrogenRatio =
                        _plantNitrogenKilogramsPerKilogramLiveBiomass!.Value;

                    consumedVegetation =
                        Math.Max(
                            realizedGrowth,
                            assimilatedNitrogen /
                            plantNitrogenRatio);
                }

                vegetationBiomassByCellId[
                    cell.CellId] =
                    Math.Max(
                        0,
                        vegetationBiomass -
                        consumedVegetation);

                if (consumedVegetation > 0 &&
                    _plantNitrogenKilogramsPerKilogramLiveBiomass
                        is double feedingPlantNitrogenRatio)
                {
                    var surfaceCell =
                        surfaceCellsById[
                            cell.CellId];

                    var consumedBiomassKilograms =
                        consumedVegetation *
                        surfaceCell.AreaSquareMeters;

                    var assimilatedBiomassKilograms =
                        realizedGrowth *
                        surfaceCell.AreaSquareMeters;

                    var availablePlantNitrogenKilograms =
                        consumedBiomassKilograms *
                        feedingPlantNitrogenRatio;

                    var assimilatedNitrogenKilograms =
                        Math.Min(
                            assimilatedNitrogen *
                            surfaceCell.AreaSquareMeters,
                            availablePlantNitrogenKilograms);

                    assimilatedNitrogen =
                        assimilatedNitrogenKilograms /
                        surfaceCell.AreaSquareMeters;

                    feedingMaterialTransfers.Add(
                        new VegetationFeedingEvent(
                            cell.CellId,
                            consumedBiomassKilograms,
                            assimilatedBiomassKilograms,
                            assimilatedNitrogenKilograms));

                    totalAssimilatedNitrogenMass +=
                        assimilatedNitrogenKilograms;
                }

                var nominalLoss =
                    biomass *
                    _parameters
                        .BaselineMortalityRatePerDay *
                    elapsedDays;

                var biomassBeforeLoss =
                    biomass +
                    realizedGrowth;

                var nitrogenBeforeLoss =
                    cell.LiveNitrogenKilogramsPerSquareMeter +
                    assimilatedNitrogen;

                var realizedLoss =
                    Math.Min(
                        biomassBeforeLoss,
                        nominalLoss);

                var mortalityFraction =
                    biomassBeforeLoss <= 0
                        ? 0
                        : Math.Clamp(
                            realizedLoss /
                            biomassBeforeLoss,
                            0,
                            1);

                var realizedNitrogenLoss =
                    nitrogenBeforeLoss *
                    mortalityFraction;

                var nextBiomass =
                    Math.Max(
                        0,
                        biomassBeforeLoss -
                        realizedLoss);

                var nextNitrogen =
                    Math.Max(
                        0,
                        nitrogenBeforeLoss -
                        realizedNitrogenLoss);

                if (realizedLoss > 0)
                {
                    if (detritalBiomassByCellId is not null &&
                        detritalNitrogenByCellId is not null)
                    {
                        detritalBiomassByCellId[
                            cell.CellId] +=
                            realizedLoss;

                        detritalNitrogenByCellId[
                            cell.CellId] +=
                            realizedNitrogenLoss;

                        var surfaceCell =
                            surfaceCellsById[
                                cell.CellId];

                        totalMortalityDetritalBiomassMass +=
                            realizedLoss *
                            surfaceCell.AreaSquareMeters;

                        totalMortalityDetritalNitrogenMass +=
                            realizedNitrogenLoss *
                            surfaceCell.AreaSquareMeters;
                    }
                    else if (realizedNitrogenLoss > 0)
                    {
                        throw new InvalidOperationException(
                            "Material-bearing invertebrate mortality requires authoritative biogeochemistry state for the target planet.");
                    }
                }

                if (nextBiomass >
                        biomass &&
                    carryingCapacity > 0)
                {
                    nextBiomass =
                        Math.Min(
                            carryingCapacity,
                            nextBiomass);
                }

                nextCells[index] =
                    new InvertebrateCellState(
                        cell.CellId,
                        nextBiomass,
                        nextNitrogen);
            }

            current =
                new PlanetInvertebrateState(
                    invertebrates.PlanetId,
                    invertebrates.GridDefinition,
                    nextCells);

            integrationSubsteps++;
            remainingSeconds -=
                stepSeconds;
        }

        var finalBiomassMass =
            TotalBiomassMassKilograms(
                current,
                surfaceCellsById);

        var finalVegetation =
            new PlanetVegetationState(
                vegetation.PlanetId,
                vegetation.GridDefinition,
                vegetation.Cells.Select(
                    cell =>
                        new VegetationCellState(
                            cell.CellId,
                            vegetationBiomassByCellId[
                                cell.CellId])));

        PlanetBiogeochemistryState?
            finalBiogeochemistry =
                biogeochemistry is null
                    ? null
                    : new PlanetBiogeochemistryState(
                        biogeochemistry.PlanetId,
                        biogeochemistry.GridDefinition,
                        biogeochemistry.Cells.Select(
                            cell =>
                                new BiogeochemistryCellState(
                                    cell.CellId,
                                    detritalBiomassByCellId![
                                        cell.CellId],
                                    detritalNitrogenByCellId![
                                        cell.CellId],
                                    cell.PlantAvailableNitrogenKilogramsPerSquareMeter)));

        if (feedingMaterialTransfers.Count > 0)
        {
            var plantNitrogenRatio =
                _plantNitrogenKilogramsPerKilogramLiveBiomass
                ?? throw new InvalidOperationException(
                    "Nitrogen-coupled invertebrate feeding requires an authoritative plant-tissue nitrogen policy.");

            finalBiogeochemistry =
                VegetationFeedingMaterialTransfer
                    .ReturnConsumedNitrogen(
                        planet,
                        finalBiogeochemistry
                        ?? throw new InvalidOperationException(
                            "Nitrogen-coupled invertebrate feeding requires authoritative biogeochemistry state for the target planet."),
                        feedingMaterialTransfers,
                        plantNitrogenRatio);
        }

        var vegetationChanged =
            vegetation.Cells.Any(
                cell =>
                    vegetationBiomassByCellId[
                        cell.CellId] !=
                    cell.LiveBiomassKilogramsPerSquareMeter);

        ISimulationOperation operation;

        if (vegetationChanged)
        {
            operation =
                new ReplacePlanetInvertebrateVegetationStateOperation(
                    current,
                    finalVegetation,
                    finalBiogeochemistry);
        }
        else if (finalBiogeochemistry is null)
        {
            operation =
                new ReplacePlanetInvertebrateStateOperation(
                    current);
        }
        else
        {
            operation =
                new ReplacePlanetInvertebrateBiogeochemistryStateOperation(
                    current,
                    finalBiogeochemistry);
        }

        return new SimulationChange(
            operation,
            "planetary-invertebrates",
            "Vegetation-supported aggregate invertebrate biomass changed.",
            planet.Id,
            elapsedSeconds,
            new Dictionary<string, double>
            {
                ["initialLiveBiomassKilograms"] =
                    initialBiomassMass,
                ["finalLiveBiomassKilograms"] =
                    finalBiomassMass,
                ["biomassChangeKilograms"] =
                    finalBiomassMass -
                    initialBiomassMass,
                ["mortalityDetritalBiomassKilograms"] =
                    totalMortalityDetritalBiomassMass,
                ["mortalityDetritalNitrogenKilograms"] =
                    totalMortalityDetritalNitrogenMass,
                ["assimilatedNitrogenKilograms"] =
                    totalAssimilatedNitrogenMass,
                ["integrationSubsteps"] =
                    integrationSubsteps
            });
    }

    private static double TotalBiomassMassKilograms(
        PlanetInvertebrateState invertebrates,
        IReadOnlyDictionary<
            SurfaceCellId,
            SurfaceCell> surfaceCellsById)
    {
        var total =
            0d;

        foreach (var cell in
                 invertebrates.Cells)
        {
            total +=
                cell.LiveBiomassKilogramsPerSquareMeter *
                surfaceCellsById[
                    cell.CellId]
                .AreaSquareMeters;
        }

        return total;
    }
}
