using Est.Simulation.Biogeochemistry;
using Est.Simulation.Causality;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Worlds;

namespace Est.Simulation.Invertebrates;

/// <summary>
/// First-pass causal aggregate invertebrate biomass model.
///
/// Local live vegetation establishes carrying capacity. Existing invertebrate
/// biomass can reproduce toward that support ceiling while baseline loss
/// continuously removes biomass. When authoritative biogeochemistry is present,
/// realized mortality biomass enters the detrital pool. Vegetation is not
/// directly consumed by this aggregate model.
/// </summary>
public sealed class InvertebrateSystem
    : ICausalSystem
{
    private const double SecondsPerDay =
        86_400d;

    private readonly PlanetId _planetId;
    private readonly InvertebrateModelParameters _parameters;

    public InvertebrateSystem(
        PlanetId planetId,
        InvertebrateModelParameters parameters)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(
            parameters);

        _planetId =
            planetId;

        _parameters =
            parameters;
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

        var vegetationByCellId =
            vegetation.Cells.ToDictionary(
                cell =>
                    cell.CellId);

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

        var totalMortalityDetritalBiomassMass =
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
                    vegetationByCellId[
                        cell.CellId]
                    .LiveBiomassKilogramsPerSquareMeter;

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

                var nominalLoss =
                    biomass *
                    _parameters
                        .BaselineMortalityRatePerDay *
                    elapsedDays;

                var realizedLoss =
                    Math.Min(
                        biomass +
                        growth,
                        nominalLoss);

                var nextBiomass =
                    Math.Max(
                        0,
                        biomass +
                        growth -
                        realizedLoss);

                if (detritalBiomassByCellId is not null &&
                    realizedLoss > 0)
                {
                    detritalBiomassByCellId[
                        cell.CellId] +=
                        realizedLoss;

                    totalMortalityDetritalBiomassMass +=
                        realizedLoss *
                        surfaceCellsById[
                            cell.CellId]
                        .AreaSquareMeters;
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
                        nextBiomass);
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

        var finalBiogeochemistry =
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
                                cell.DetritalNitrogenKilogramsPerSquareMeter,
                                cell.PlantAvailableNitrogenKilogramsPerSquareMeter)));

        var operation =
            finalBiogeochemistry is null
                ? (ISimulationOperation)
                    new ReplacePlanetInvertebrateStateOperation(
                        current)
                : new ReplacePlanetInvertebrateBiogeochemistryStateOperation(
                    current,
                    finalBiogeochemistry);

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
