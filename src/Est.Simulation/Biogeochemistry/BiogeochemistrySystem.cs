using Est.Simulation.Causality;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Worlds;

namespace Est.Simulation.Biogeochemistry;

/// <summary>
/// First-pass causal decomposition and nitrogen-mineralization model.
///
/// Detrital biomass decomposes when soil moisture and local temperature are
/// suitable. The same fraction of detrital nitrogen is transferred into the
/// plant-available nitrogen pool, preserving tracked nitrogen mass exactly
/// apart from floating-point integration error.
///
/// Carbon dioxide, microbial immobilization, phosphorus, and detailed soil
/// chemistry remain later extensions.
/// </summary>
public sealed class BiogeochemistrySystem
    : ICausalSystem
{
    private const double SecondsPerDay =
        86_400d;

    private readonly PlanetId _planetId;
    private readonly BiogeochemistryModelParameters _parameters;

    public BiogeochemistrySystem(
        PlanetId planetId,
        BiogeochemistryModelParameters parameters)
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

        var terrain =
            world.Terrain.FirstOrDefault(
                candidate =>
                    candidate.PlanetId ==
                    _planetId)
            ?? throw new InvalidOperationException(
                "Biogeochemistry integration requires terrain for the target planet.");

        var hydrology =
            world.Hydrology.FirstOrDefault(
                candidate =>
                    candidate.PlanetId ==
                    _planetId)
            ?? throw new InvalidOperationException(
                "Biogeochemistry integration requires hydrology for the target planet.");

        var biogeochemistry =
            world.Biogeochemistry.FirstOrDefault(
                candidate =>
                    candidate.PlanetId ==
                    _planetId)
            ?? throw new InvalidOperationException(
                "Biogeochemistry integration requires authoritative biogeochemistry state for the target planet.");

        if (terrain.GridDefinition !=
                hydrology.GridDefinition ||
            terrain.GridDefinition !=
                biogeochemistry.GridDefinition)
        {
            throw new InvalidOperationException(
                "Biogeochemistry, hydrology, and terrain must use the same surface grid.");
        }

        terrain.ValidateFor(
            planet);

        hydrology.ValidateFor(
            planet);

        biogeochemistry.ValidateFor(
            planet);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                biogeochemistry.GridDefinition);

        var surfaceCellsById =
            grid.Cells.ToDictionary(
                cell =>
                    cell.Id);

        var terrainByCellId =
            terrain.Cells.ToDictionary(
                cell =>
                    cell.CellId);

        var hydrologyByCellId =
            hydrology.Cells.ToDictionary(
                cell =>
                    cell.CellId);

        var initialDetritalBiomassMass =
            TotalMassKilograms(
                biogeochemistry,
                surfaceCellsById,
                cell =>
                    cell.DetritalBiomassKilogramsPerSquareMeter);

        var initialDetritalNitrogenMass =
            TotalMassKilograms(
                biogeochemistry,
                surfaceCellsById,
                cell =>
                    cell.DetritalNitrogenKilogramsPerSquareMeter);

        var initialAvailableNitrogenMass =
            TotalMassKilograms(
                biogeochemistry,
                surfaceCellsById,
                cell =>
                    cell.PlantAvailableNitrogenKilogramsPerSquareMeter);

        var initialTrackedNitrogenMass =
            initialDetritalNitrogenMass +
            initialAvailableNitrogenMass;

        var current =
            biogeochemistry;

        var totalDecomposedBiomassMass =
            0d;

        var totalTransferredNitrogenMass =
            0d;

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
                new BiogeochemistryCellState[
                    current.Cells.Length];

            for (var index = 0;
                 index < current.Cells.Length;
                 index++)
            {
                var cell =
                    current.Cells[index];

                var detritalBiomass =
                    cell
                        .DetritalBiomassKilogramsPerSquareMeter;

                if (detritalBiomass <= 0)
                {
                    nextCells[index] =
                        cell;

                    continue;
                }

                var terrainCell =
                    terrainByCellId[
                        cell.CellId];

                var hydrologyCell =
                    hydrologyByCellId[
                        cell.CellId];

                var localTemperatureKelvin =
                    Math.Max(
                        0,
                        planet.Environment
                            .MeanSurfaceTemperatureKelvin -
                        terrainCell.ElevationMeters *
                        _parameters
                            .TemperatureLapseRateKelvinPerMeter);

                var temperatureFactor =
                    CalculateTemperatureFactor(
                        localTemperatureKelvin);

                var soilWaterFactor =
                    Math.Clamp(
                        hydrologyCell
                            .SoilWaterKilogramsPerSquareMeter /
                        _parameters
                            .SoilWaterForFullDecompositionKilogramsPerSquareMeter,
                        0,
                        1);

                var decomposedBiomass =
                    Math.Min(
                        detritalBiomass,
                        detritalBiomass *
                        _parameters
                            .MaximumRelativeDecompositionRatePerDay *
                        temperatureFactor *
                        soilWaterFactor *
                        elapsedDays);

                if (decomposedBiomass <= 0)
                {
                    nextCells[index] =
                        cell;

                    continue;
                }

                var decompositionFraction =
                    decomposedBiomass /
                    detritalBiomass;

                var transferredNitrogen =
                    Math.Min(
                        cell
                            .DetritalNitrogenKilogramsPerSquareMeter,
                        cell
                            .DetritalNitrogenKilogramsPerSquareMeter *
                        decompositionFraction);

                var nextDetritalBiomass =
                    Math.Max(
                        0,
                        detritalBiomass -
                        decomposedBiomass);

                var nextDetritalNitrogen =
                    Math.Max(
                        0,
                        cell
                            .DetritalNitrogenKilogramsPerSquareMeter -
                        transferredNitrogen);

                var nextAvailableNitrogen =
                    cell
                        .PlantAvailableNitrogenKilogramsPerSquareMeter +
                    transferredNitrogen;

                nextCells[index] =
                    new BiogeochemistryCellState(
                        cell.CellId,
                        nextDetritalBiomass,
                        nextDetritalNitrogen,
                        nextAvailableNitrogen);

                var area =
                    surfaceCellsById[
                        cell.CellId]
                    .AreaSquareMeters;

                totalDecomposedBiomassMass +=
                    decomposedBiomass *
                    area;

                totalTransferredNitrogenMass +=
                    transferredNitrogen *
                    area;
            }

            current =
                new PlanetBiogeochemistryState(
                    biogeochemistry.PlanetId,
                    biogeochemistry.GridDefinition,
                    nextCells);

            integrationSubsteps++;
            remainingSeconds -=
                stepSeconds;
        }

        var finalDetritalBiomassMass =
            TotalMassKilograms(
                current,
                surfaceCellsById,
                cell =>
                    cell.DetritalBiomassKilogramsPerSquareMeter);

        var finalDetritalNitrogenMass =
            TotalMassKilograms(
                current,
                surfaceCellsById,
                cell =>
                    cell.DetritalNitrogenKilogramsPerSquareMeter);

        var finalAvailableNitrogenMass =
            TotalMassKilograms(
                current,
                surfaceCellsById,
                cell =>
                    cell.PlantAvailableNitrogenKilogramsPerSquareMeter);

        var finalTrackedNitrogenMass =
            finalDetritalNitrogenMass +
            finalAvailableNitrogenMass;

        var nitrogenConservationError =
            finalTrackedNitrogenMass -
            initialTrackedNitrogenMass;

        var relativeNitrogenConservationError =
            initialTrackedNitrogenMass == 0
                ? Math.Abs(
                    nitrogenConservationError)
                : Math.Abs(
                    nitrogenConservationError) /
                  initialTrackedNitrogenMass;

        return new SimulationChange(
            new ReplacePlanetBiogeochemistryStateOperation(
                current),
            "planetary-biogeochemistry",
            "Moisture and temperature constrained decomposition transferred detrital nitrogen into the plant-available pool.",
            planet.Id,
            elapsedSeconds,
            new Dictionary<string, double>
            {
                ["initialDetritalBiomassMassKilograms"] =
                    initialDetritalBiomassMass,
                ["finalDetritalBiomassMassKilograms"] =
                    finalDetritalBiomassMass,
                ["decomposedBiomassMassKilograms"] =
                    totalDecomposedBiomassMass,
                ["initialDetritalNitrogenMassKilograms"] =
                    initialDetritalNitrogenMass,
                ["finalDetritalNitrogenMassKilograms"] =
                    finalDetritalNitrogenMass,
                ["initialAvailableNitrogenMassKilograms"] =
                    initialAvailableNitrogenMass,
                ["finalAvailableNitrogenMassKilograms"] =
                    finalAvailableNitrogenMass,
                ["transferredNitrogenMassKilograms"] =
                    totalTransferredNitrogenMass,
                ["initialTrackedNitrogenMassKilograms"] =
                    initialTrackedNitrogenMass,
                ["finalTrackedNitrogenMassKilograms"] =
                    finalTrackedNitrogenMass,
                ["nitrogenMassConservationErrorKilograms"] =
                    nitrogenConservationError,
                ["relativeNitrogenMassConservationError"] =
                    relativeNitrogenConservationError,
                ["integrationSubsteps"] =
                    integrationSubsteps
            });
    }

    private double CalculateTemperatureFactor(
        double temperatureKelvin)
    {
        if (temperatureKelvin <=
                _parameters
                    .MinimumDecompositionTemperatureKelvin ||
            temperatureKelvin >=
                _parameters
                    .MaximumDecompositionTemperatureKelvin)
        {
            return 0;
        }

        if (temperatureKelvin <=
            _parameters
                .OptimumDecompositionTemperatureKelvin)
        {
            return
                (temperatureKelvin -
                 _parameters
                     .MinimumDecompositionTemperatureKelvin) /
                (_parameters
                     .OptimumDecompositionTemperatureKelvin -
                 _parameters
                     .MinimumDecompositionTemperatureKelvin);
        }

        return
            (_parameters
                 .MaximumDecompositionTemperatureKelvin -
             temperatureKelvin) /
            (_parameters
                 .MaximumDecompositionTemperatureKelvin -
             _parameters
                 .OptimumDecompositionTemperatureKelvin);
    }

    private static double TotalMassKilograms(
        PlanetBiogeochemistryState biogeochemistry,
        IReadOnlyDictionary<
            SurfaceCellId,
            SurfaceCell> surfaceCellsById,
        Func<
            BiogeochemistryCellState,
            double> kilogramsPerSquareMeter)
    {
        var total =
            0d;

        foreach (var cell in
                 biogeochemistry.Cells)
        {
            total +=
                kilogramsPerSquareMeter(
                    cell) *
                surfaceCellsById[
                    cell.CellId]
                .AreaSquareMeters;
        }

        return total;
    }
}
