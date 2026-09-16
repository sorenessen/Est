using Est.Simulation.Biogeochemistry;
using Est.Simulation.Causality;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Worlds;

namespace Est.Simulation.Vegetation;

/// <summary>
/// First-pass causal plant biomass productivity model.
///
/// Existing biomass grows toward carrying capacity when soil water and
/// temperature are suitable. Elevation modifies the current authoritative
/// planetary mean temperature through a configurable lapse rate.
///
/// When plant tissue nitrogen policy is enabled, growth also requires
/// authoritative plant-available nitrogen and consumes it atomically with
/// biomass production.
///
/// Zero biomass remains zero. Colonization, seed dispersal, mortality, and
/// species structure remain later ecological systems.
/// </summary>
public sealed class VegetationSystem
    : ICausalSystem
{
    private const double SecondsPerDay =
        86_400d;

    private readonly PlanetId _planetId;
    private readonly VegetationModelParameters _parameters;

    public VegetationSystem(
        PlanetId planetId,
        VegetationModelParameters parameters)
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
                "Vegetation integration requires terrain for the target planet.");

        var hydrology =
            world.Hydrology.FirstOrDefault(
                candidate =>
                    candidate.PlanetId ==
                    _planetId)
            ?? throw new InvalidOperationException(
                "Vegetation integration requires hydrology for the target planet.");

        var vegetation =
            world.Vegetation.FirstOrDefault(
                candidate =>
                    candidate.PlanetId ==
                    _planetId)
            ?? throw new InvalidOperationException(
                "Vegetation integration requires vegetation state for the target planet.");

        var plantNitrogenRatio =
            _parameters
                .PlantNitrogenKilogramsPerKilogramLiveBiomass;

        var biogeochemistry =
            plantNitrogenRatio is null
                ? null
                : world.Biogeochemistry.FirstOrDefault(
                    candidate =>
                        candidate.PlanetId ==
                        _planetId)
                  ?? throw new InvalidOperationException(
                      "Nitrogen-coupled vegetation integration requires authoritative biogeochemistry state for the target planet.");

        if (terrain.GridDefinition !=
                hydrology.GridDefinition ||
            terrain.GridDefinition !=
                vegetation.GridDefinition ||
            (biogeochemistry is not null &&
             terrain.GridDefinition !=
                biogeochemistry.GridDefinition))
        {
            throw new InvalidOperationException(
                "Vegetation, hydrology, terrain, and enabled biogeochemistry must use the same surface grid.");
        }

        terrain.ValidateFor(
            planet);

        hydrology.ValidateFor(
            planet);

        vegetation.ValidateFor(
            planet);

        biogeochemistry?.ValidateFor(
            planet);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                vegetation.GridDefinition);

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

        var biogeochemistryIndexByCellId =
            biogeochemistry?.Cells
                .Select(
                    (cell, index) =>
                        new
                        {
                            cell.CellId,
                            Index = index
                        })
                .ToDictionary(
                    entry =>
                        entry.CellId,
                    entry =>
                        entry.Index);

        var initialBiomassMass =
            TotalBiomassMassKilograms(
                vegetation,
                surfaceCellsById);

        var initialAvailableNitrogenMass =
            biogeochemistry is null
                ? 0
                : TotalAvailableNitrogenMassKilograms(
                    biogeochemistry,
                    surfaceCellsById);

        var current =
            vegetation;

        var currentBiogeochemistry =
            biogeochemistry;

        var totalNitrogenUptakeMass =
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
                new VegetationCellState[
                    current.Cells.Length];

            var nextBiogeochemistryCells =
                currentBiogeochemistry?.Cells
                    .ToArray();

            for (var index = 0;
                 index < current.Cells.Length;
                 index++)
            {
                var cell =
                    current.Cells[index];

                var biomass =
                    cell.LiveBiomassKilogramsPerSquareMeter;

                if (biomass <= 0 ||
                    biomass >=
                    _parameters
                        .CarryingCapacityKilogramsPerSquareMeter)
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
                            .SoilWaterForFullProductivityKilogramsPerSquareMeter,
                        0,
                        1);

                var carryingCapacityFactor =
                    Math.Max(
                        0,
                        1 -
                        biomass /
                        _parameters
                            .CarryingCapacityKilogramsPerSquareMeter);

                var potentialGrowth =
                    biomass *
                    _parameters
                        .MaximumRelativeGrowthRatePerDay *
                    temperatureFactor *
                    soilWaterFactor *
                    carryingCapacityFactor *
                    elapsedDays;

                var growth =
                    Math.Min(
                        potentialGrowth,
                        _parameters
                            .CarryingCapacityKilogramsPerSquareMeter -
                        biomass);

                var nitrogenUptake =
                    0d;

                if (plantNitrogenRatio is not null)
                {
                    var biogeochemistryIndex =
                        biogeochemistryIndexByCellId![
                            cell.CellId];

                    var biogeochemistryCell =
                        currentBiogeochemistry!.Cells[
                            biogeochemistryIndex];

                    var maximumGrowthFromNitrogen =
                        biogeochemistryCell
                            .PlantAvailableNitrogenKilogramsPerSquareMeter /
                        plantNitrogenRatio.Value;

                    growth =
                        Math.Min(
                            growth,
                            maximumGrowthFromNitrogen);

                    nitrogenUptake =
                        growth *
                        plantNitrogenRatio.Value;

                    nextBiogeochemistryCells![
                        biogeochemistryIndex] =
                        new BiogeochemistryCellState(
                            biogeochemistryCell.CellId,
                            biogeochemistryCell
                                .DetritalBiomassKilogramsPerSquareMeter,
                            biogeochemistryCell
                                .DetritalNitrogenKilogramsPerSquareMeter,
                            Math.Max(
                                0,
                                biogeochemistryCell
                                    .PlantAvailableNitrogenKilogramsPerSquareMeter -
                                nitrogenUptake));

                    totalNitrogenUptakeMass +=
                        nitrogenUptake *
                        surfaceCellsById[
                            cell.CellId]
                        .AreaSquareMeters;
                }

                var nextBiomass =
                    Math.Min(
                        _parameters
                            .CarryingCapacityKilogramsPerSquareMeter,
                        biomass +
                        growth);

                nextCells[index] =
                    new VegetationCellState(
                        cell.CellId,
                        nextBiomass);
            }

            current =
                new PlanetVegetationState(
                    vegetation.PlanetId,
                    vegetation.GridDefinition,
                    nextCells);

            if (currentBiogeochemistry is not null)
            {
                currentBiogeochemistry =
                    new PlanetBiogeochemistryState(
                        currentBiogeochemistry.PlanetId,
                        currentBiogeochemistry.GridDefinition,
                        nextBiogeochemistryCells!);
            }

            integrationSubsteps++;
            remainingSeconds -=
                stepSeconds;
        }

        var finalBiomassMass =
            TotalBiomassMassKilograms(
                current,
                surfaceCellsById);

        var biomassGrowth =
            finalBiomassMass -
            initialBiomassMass;

        var finalAvailableNitrogenMass =
            currentBiogeochemistry is null
                ? 0
                : TotalAvailableNitrogenMassKilograms(
                    currentBiogeochemistry,
                    surfaceCellsById);

        var operation =
            currentBiogeochemistry is null
                ? (ISimulationOperation)
                    new ReplacePlanetVegetationStateOperation(
                        current)
                : new ReplacePlanetVegetationBiogeochemistryStateOperation(
                    current,
                    currentBiogeochemistry);

        return new SimulationChange(
            operation,
            "planetary-vegetation",
            plantNitrogenRatio is null
                ? "Terrain, water availability, and climate changed live plant biomass."
                : "Terrain, water availability, climate, and available nitrogen changed live plant biomass.",
            planet.Id,
            elapsedSeconds,
            new Dictionary<string, double>
            {
                ["initialLiveBiomassKilograms"] =
                    initialBiomassMass,
                ["finalLiveBiomassKilograms"] =
                    finalBiomassMass,
                ["biomassGrowthKilograms"] =
                    biomassGrowth,
                ["initialAvailableNitrogenMassKilograms"] =
                    initialAvailableNitrogenMass,
                ["finalAvailableNitrogenMassKilograms"] =
                    finalAvailableNitrogenMass,
                ["nitrogenUptakeMassKilograms"] =
                    totalNitrogenUptakeMass,
                ["integrationSubsteps"] =
                    integrationSubsteps
            });
    }

    private double CalculateTemperatureFactor(
        double temperatureKelvin)
    {
        if (temperatureKelvin <=
                _parameters
                    .MinimumGrowthTemperatureKelvin ||
            temperatureKelvin >=
                _parameters
                    .MaximumGrowthTemperatureKelvin)
        {
            return 0;
        }

        if (temperatureKelvin <=
            _parameters
                .OptimumGrowthTemperatureKelvin)
        {
            return
                (temperatureKelvin -
                 _parameters
                     .MinimumGrowthTemperatureKelvin) /
                (_parameters
                     .OptimumGrowthTemperatureKelvin -
                 _parameters
                     .MinimumGrowthTemperatureKelvin);
        }

        return
            (_parameters
                 .MaximumGrowthTemperatureKelvin -
             temperatureKelvin) /
            (_parameters
                 .MaximumGrowthTemperatureKelvin -
             _parameters
                 .OptimumGrowthTemperatureKelvin);
    }

    private static double TotalBiomassMassKilograms(
        PlanetVegetationState vegetation,
        IReadOnlyDictionary<
            SurfaceCellId,
            SurfaceCell> surfaceCellsById)
    {
        var total =
            0d;

        foreach (var cell in
                 vegetation.Cells)
        {
            total +=
                cell.LiveBiomassKilogramsPerSquareMeter *
                surfaceCellsById[
                    cell.CellId]
                .AreaSquareMeters;
        }

        return total;
    }

    private static double TotalAvailableNitrogenMassKilograms(
        PlanetBiogeochemistryState biogeochemistry,
        IReadOnlyDictionary<
            SurfaceCellId,
            SurfaceCell> surfaceCellsById)
    {
        var total =
            0d;

        foreach (var cell in
                 biogeochemistry.Cells)
        {
            total +=
                cell.PlantAvailableNitrogenKilogramsPerSquareMeter *
                surfaceCellsById[
                    cell.CellId]
                .AreaSquareMeters;
        }

        return total;
    }
}
