using Est.Simulation.Causality;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Surface;
using Est.Simulation.Terrain;
using Est.Simulation.Worlds;

namespace Est.Simulation.Hydrology;

/// <summary>
/// Conservative first-pass planetary water-cycle integration.
///
/// Each bounded substep performs local store-to-store transfers first:
///
/// surface liquid -> atmosphere          evaporation
/// atmosphere    -> surface liquid       precipitation
/// surface liquid -> soil                infiltration
///
/// Runoff is then evaluated from one shared post-local snapshot and applied
/// simultaneously. This prevents iteration order from allowing water to
/// cascade through several cells during one integration substep.
///
/// Water stores are densities in kg/m². Runoff crosses cells as physical mass,
/// so differing cell areas cannot create or destroy water.
/// </summary>
public sealed class HydrologySystem
    : ICausalSystem
{
    private const double SecondsPerDay =
        86_400d;

    private readonly PlanetId _planetId;
    private readonly HydrologyModelParameters _parameters;

    public HydrologySystem(
        PlanetId planetId,
        HydrologyModelParameters parameters)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        ArgumentNullException.ThrowIfNull(
            parameters);

        _planetId = planetId;
        _parameters = parameters;
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
                "Hydrology integration requires terrain for the target planet.");

        var hydrology =
            world.Hydrology.FirstOrDefault(
                candidate =>
                    candidate.PlanetId ==
                    _planetId)
            ?? throw new InvalidOperationException(
                "Hydrology integration requires hydrology state for the target planet.");

        if (terrain.GridDefinition !=
            hydrology.GridDefinition)
        {
            throw new InvalidOperationException(
                "Hydrology and terrain must use the same surface grid.");
        }

        hydrology.ValidateFor(
            planet);

        terrain.ValidateFor(
            planet);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                hydrology.GridDefinition);

        var drainage =
            PlanetTerrainDrainageTopology.Derive(
                planet,
                terrain);

        var surfaceCellsById =
            grid.Cells.ToDictionary(
                cell =>
                    cell.Id);

        var initialWaterMass =
            hydrology.TotalWaterMassKilograms(
                planet);

        var current =
            hydrology;

        var totalEvaporationMass = 0d;
        var totalPrecipitationMass = 0d;
        var totalInfiltrationMass = 0d;
        var totalRunoffMass = 0d;
        var integrationSubsteps = 0;

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

            var localStores =
                new Dictionary<
                    SurfaceCellId,
                    WaterStores>(
                    current.Cells.Length);

            foreach (var cell in
                     current.Cells)
            {
                var area =
                    surfaceCellsById[
                        cell.CellId]
                    .AreaSquareMeters;

                var atmospheric =
                    cell
                        .AtmosphericWaterKilogramsPerSquareMeter;

                var surface =
                    cell
                        .SurfaceLiquidWaterKilogramsPerSquareMeter;

                var soil =
                    cell
                        .SoilWaterKilogramsPerSquareMeter;

                var snowIce =
                    cell
                        .SnowIceWaterEquivalentKilogramsPerSquareMeter;

                var evaporation =
                    Math.Min(
                        surface,
                        _parameters
                            .MaximumEvaporationRateKilogramsPerSquareMeterPerDay *
                        elapsedDays);

                surface -=
                    evaporation;

                atmospheric +=
                    evaporation;

                totalEvaporationMass +=
                    evaporation *
                    area;

                var precipitableExcess =
                    Math.Max(
                        0,
                        atmospheric -
                        _parameters
                            .AtmosphericPrecipitationThresholdKilogramsPerSquareMeter);

                var precipitation =
                    Math.Min(
                        precipitableExcess,
                        _parameters
                            .MaximumPrecipitationRateKilogramsPerSquareMeterPerDay *
                        elapsedDays);

                atmospheric -=
                    precipitation;

                surface +=
                    precipitation;

                totalPrecipitationMass +=
                    precipitation *
                    area;

                var remainingSoilCapacity =
                    Math.Max(
                        0,
                        _parameters
                            .SoilWaterCapacityKilogramsPerSquareMeter -
                        soil);

                var infiltration =
                    Math.Min(
                        surface,
                        Math.Min(
                            remainingSoilCapacity,
                            _parameters
                                .MaximumInfiltrationRateKilogramsPerSquareMeterPerDay *
                            elapsedDays));

                surface -=
                    infiltration;

                soil +=
                    infiltration;

                totalInfiltrationMass +=
                    infiltration *
                    area;

                localStores.Add(
                    cell.CellId,
                    new WaterStores(
                        atmospheric,
                        surface,
                        soil,
                        snowIce));
            }

            var runoffBySource =
                new Dictionary<
                    SurfaceCellId,
                    double>();

            var incomingRunoffMass =
                new Dictionary<
                    SurfaceCellId,
                    double>();

            foreach (var cell in
                     current.Cells)
            {
                var drainageCell =
                    drainage.GetCell(
                        cell.CellId);

                if (!drainageCell
                    .DownhillNeighborCellId
                    .HasValue)
                {
                    continue;
                }

                var stores =
                    localStores[
                        cell.CellId];

                var runoff =
                    Math.Min(
                        stores.SurfaceLiquidWaterKilogramsPerSquareMeter,
                        _parameters
                            .MaximumRunoffRateKilogramsPerSquareMeterPerDay *
                        elapsedDays);

                if (runoff <= 0)
                {
                    continue;
                }

                var sourceArea =
                    surfaceCellsById[
                        cell.CellId]
                    .AreaSquareMeters;

                var runoffMass =
                    runoff *
                    sourceArea;

                var destinationId =
                    drainageCell
                        .DownhillNeighborCellId
                        .Value;

                runoffBySource[
                    cell.CellId] =
                    runoff;

                if (incomingRunoffMass.TryGetValue(
                        destinationId,
                        out var existingMass))
                {
                    incomingRunoffMass[
                        destinationId] =
                        existingMass +
                        runoffMass;
                }
                else
                {
                    incomingRunoffMass[
                        destinationId] =
                        runoffMass;
                }

                totalRunoffMass +=
                    runoffMass;
            }

            var nextCells =
                new HydrologyCellState[
                    current.Cells.Length];

            for (var index = 0;
                 index < current.Cells.Length;
                 index++)
            {
                var previousCell =
                    current.Cells[index];

                var stores =
                    localStores[
                        previousCell.CellId];

                var surface =
                    stores
                        .SurfaceLiquidWaterKilogramsPerSquareMeter;

                if (runoffBySource.TryGetValue(
                        previousCell.CellId,
                        out var runoff))
                {
                    surface -=
                        runoff;
                }

                if (incomingRunoffMass.TryGetValue(
                        previousCell.CellId,
                        out var incomingMass))
                {
                    var destinationArea =
                        surfaceCellsById[
                            previousCell.CellId]
                        .AreaSquareMeters;

                    surface +=
                        incomingMass /
                        destinationArea;
                }

                nextCells[index] =
                    new HydrologyCellState(
                        previousCell.CellId,
                        stores
                            .AtmosphericWaterKilogramsPerSquareMeter,
                        surface,
                        stores
                            .SoilWaterKilogramsPerSquareMeter,
                        stores
                            .SnowIceWaterEquivalentKilogramsPerSquareMeter);
            }

            current =
                new PlanetHydrologyState(
                    hydrology.PlanetId,
                    hydrology.GridDefinition,
                    nextCells);

            integrationSubsteps++;
            remainingSeconds -=
                stepSeconds;
        }

        var finalWaterMass =
            current.TotalWaterMassKilograms(
                planet);

        var conservationError =
            finalWaterMass -
            initialWaterMass;

        var relativeConservationError =
            initialWaterMass == 0
                ? Math.Abs(
                    conservationError)
                : Math.Abs(
                    conservationError) /
                  initialWaterMass;

        return new SimulationChange(
            new ReplacePlanetHydrologyStateOperation(
                current),
            "planetary-hydrology",
            "Conservative water transfers redistributed atmospheric, surface, soil, and runoff stores.",
            planet.Id,
            elapsedSeconds,
            new Dictionary<string, double>
            {
                ["initialWaterMassKilograms"] =
                    initialWaterMass,
                ["finalWaterMassKilograms"] =
                    finalWaterMass,
                ["waterMassConservationErrorKilograms"] =
                    conservationError,
                ["relativeWaterMassConservationError"] =
                    relativeConservationError,
                ["evaporationMassKilograms"] =
                    totalEvaporationMass,
                ["precipitationMassKilograms"] =
                    totalPrecipitationMass,
                ["infiltrationMassKilograms"] =
                    totalInfiltrationMass,
                ["runoffMassKilograms"] =
                    totalRunoffMass,
                ["integrationSubsteps"] =
                    integrationSubsteps
            });
    }

    private readonly record struct WaterStores(
        double AtmosphericWaterKilogramsPerSquareMeter,
        double SurfaceLiquidWaterKilogramsPerSquareMeter,
        double SoilWaterKilogramsPerSquareMeter,
        double SnowIceWaterEquivalentKilogramsPerSquareMeter);
}
