using Est.Simulation.Causality;
using Est.Simulation.Operations;
using Est.Simulation.Planets;
using Est.Simulation.Seasons;
using Est.Simulation.Solar;
using Est.Simulation.Surface;
using Est.Simulation.Worlds;

namespace Est.Simulation.Thermal;

/// <summary>
/// First causal regional radiative thermal model.
///
/// The model composes the accepted solar, atmospheric shortwave, surface
/// shortwave, graybody longwave, regional radiative-budget, and thermal
/// reservoir foundations. It does not add horizontal heat transport or
/// nonradiative surface-atmosphere exchange.
/// </summary>
public sealed class RegionalThermalSystem
    : ICausalSystem
{
    private readonly PlanetId _planetId;
    private readonly RegionalThermalModelParameters _parameters;
    private readonly bool _hasConfiguredSeasonalModel;

    public RegionalThermalSystem(
        PlanetId planetId,
        RegionalThermalModelParameters parameters,
        bool hasConfiguredSeasonalModel)
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

        _hasConfiguredSeasonalModel =
            hasConfiguredSeasonalModel;
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
                "Regional thermal integration requires terrain for the target planet.");

        var initialRegionalThermal =
            world.RegionalThermal.FirstOrDefault(
                candidate =>
                    candidate.PlanetId ==
                    _planetId)
            ?? throw new InvalidOperationException(
                "Regional thermal integration requires authoritative regional thermal state for the target planet.");

        if (terrain.GridDefinition !=
            initialRegionalThermal.GridDefinition)
        {
            throw new InvalidOperationException(
                "Regional thermal state and terrain must use the same surface grid.");
        }

        terrain.ValidateFor(
            planet);

        initialRegionalThermal.ValidateFor(
            planet);

        var subsolarLatitudeDegrees =
            ResolveSubsolarLatitudeDegrees(
                world);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                initialRegionalThermal
                    .GridDefinition);

        var initialMeanSurfaceTemperatureKelvin =
            CalculateAreaWeightedSurfaceMean(
                grid,
                initialRegionalThermal);

        var current =
            initialRegionalThermal;

        var remainingSeconds =
            elapsedSeconds;

        var integrationSubsteps =
            0;

        while (remainingSeconds > 0)
        {
            var stepSeconds =
                Math.Min(
                    remainingSeconds,
                    _parameters
                        .MaximumIntegrationStepSeconds);

            var currentCellsById =
                current.Cells.ToDictionary(
                    cell =>
                        cell.CellId);

            var nextCells =
                new RegionalThermalCellState[
                    grid.CellCount];

            for (var index = 0;
                 index < grid.CellCount;
                 index++)
            {
                var surfaceCell =
                    grid.Cells[index];

                var thermalCell =
                    currentCellsById[
                        surfaceCell.Id];

                var atmosphericShortwave =
                    AtmosphericShortwaveEnergyPartitionCalculator
                        .CalculateDailyMean(
                            surfaceCell
                                .CenterLatitudeDegrees,
                            subsolarLatitudeDegrees,
                            _parameters
                                .AtmosphericShortwaveVerticalOpticalDepth,
                            _parameters
                                .AtmosphericShortwaveSingleScatteringAlbedo,
                            _parameters
                                .AtmosphericShortwaveDownwardScatteringFraction);

                var surfaceShortwave =
                    SurfaceShortwaveEnergyPartitionCalculator
                        .Calculate(
                            atmosphericShortwave
                                .TotalSurfaceDownwellingInsolationFactor,
                            _parameters
                                .SurfaceShortwaveAlbedo);

                var atmosphericAbsorbedShortwaveFlux =
                    _parameters
                        .StellarFluxWattsPerSquareMeter
                    *
                    atmosphericShortwave
                        .AtmosphericAbsorbedInsolationFactor;

                var surfaceAbsorbedShortwaveFlux =
                    _parameters
                        .StellarFluxWattsPerSquareMeter
                    *
                    surfaceShortwave
                        .AbsorbedSurfaceShortwaveFactor;

                var budget =
                    RegionalRadiativeEnergyBudgetCalculator
                        .Calculate(
                            surfaceAbsorbedShortwaveFlux,
                            atmosphericAbsorbedShortwaveFlux,
                            thermalCell
                                .SurfaceTemperatureKelvin,
                            thermalCell
                                .AtmosphericTemperatureKelvin,
                            _parameters
                                .SurfaceLongwaveEmissivity,
                            _parameters
                                .AtmosphericLongwaveEmissivity);

                var surfaceResponse =
                    ThermalReservoirCalculator
                        .Calculate(
                            thermalCell
                                .SurfaceTemperatureKelvin,
                            _parameters
                                .SurfaceEffectiveArealHeatCapacityJoulesPerSquareMeterKelvin,
                            budget
                                .SurfaceNetRadiativeFluxWattsPerSquareMeter,
                            stepSeconds);

                var atmosphericResponse =
                    ThermalReservoirCalculator
                        .Calculate(
                            thermalCell
                                .AtmosphericTemperatureKelvin,
                            _parameters
                                .AtmosphericEffectiveArealHeatCapacityJoulesPerSquareMeterKelvin,
                            budget
                                .AtmosphericNetRadiativeFluxWattsPerSquareMeter,
                            stepSeconds);

                nextCells[index] =
                    new RegionalThermalCellState(
                        surfaceCell.Id,
                        surfaceResponse
                            .FinalTemperatureKelvin,
                        atmosphericResponse
                            .FinalTemperatureKelvin);
            }

            current =
                new PlanetRegionalThermalState(
                    _planetId,
                    current.GridDefinition,
                    nextCells);

            remainingSeconds -=
                stepSeconds;

            integrationSubsteps++;
        }

        var finalMeanSurfaceTemperatureKelvin =
            CalculateAreaWeightedSurfaceMean(
                grid,
                current);

        var operation =
            new ReplacePlanetRegionalThermalStateAndEnvironmentOperation(
                current,
                finalMeanSurfaceTemperatureKelvin);

        return new SimulationChange(
            operation,
            "regional-thermal",
            "Regional radiative forcing changed surface and atmospheric-column temperatures.",
            _planetId,
            elapsedSeconds,
            new Dictionary<string, double>
            {
                ["subsolarLatitudeDegrees"] =
                    subsolarLatitudeDegrees,
                ["integrationSubsteps"] =
                    integrationSubsteps,
                ["previousRegionalMeanSurfaceTemperatureKelvin"] =
                    initialMeanSurfaceTemperatureKelvin,
                ["newRegionalMeanSurfaceTemperatureKelvin"] =
                    finalMeanSurfaceTemperatureKelvin
            });
    }

    private double ResolveSubsolarLatitudeDegrees(
        WorldState world)
    {
        var seasonalState =
            world.SeasonalStates.FirstOrDefault(
                state =>
                    state.PlanetId ==
                    _planetId)
            ?? throw new InvalidOperationException(
                "Regional thermal integration requires a physical subsolar-latitude source.");

        if (seasonalState.ControlMode ==
                SeasonalControlMode.Override &&
            seasonalState.OverrideContext?
                .SubsolarLatitudeDegrees is double
                overrideSubsolarLatitudeDegrees)
        {
            return overrideSubsolarLatitudeDegrees;
        }

        if (_hasConfiguredSeasonalModel &&
            seasonalState.ControlMode ==
                SeasonalControlMode.Derived &&
            seasonalState.DerivedContext?
                .SubsolarLatitudeDegrees is double
                derivedSubsolarLatitudeDegrees)
        {
            return derivedSubsolarLatitudeDegrees;
        }

        if (_hasConfiguredSeasonalModel &&
            seasonalState.ControlMode ==
                SeasonalControlMode.Override &&
            seasonalState.DerivedContext?
                .SubsolarLatitudeDegrees is double
                maintainedDerivedSubsolarLatitudeDegrees)
        {
            return maintainedDerivedSubsolarLatitudeDegrees;
        }

        throw new InvalidOperationException(
            "Regional thermal integration requires an explicit override subsolar latitude or actively maintained derived astronomy.");
    }

    private static double CalculateAreaWeightedSurfaceMean(
        IPlanetSurfaceGrid grid,
        PlanetRegionalThermalState regionalThermal)
    {
        var thermalByCellId =
            regionalThermal.Cells.ToDictionary(
                cell =>
                    cell.CellId);

        var weightedTemperatureSum =
            0d;

        var totalArea =
            0d;

        foreach (var surfaceCell in grid.Cells)
        {
            var thermalCell =
                thermalByCellId[
                    surfaceCell.Id];

            weightedTemperatureSum +=
                thermalCell
                    .SurfaceTemperatureKelvin
                *
                surfaceCell.AreaSquareMeters;

            totalArea +=
                surfaceCell.AreaSquareMeters;
        }

        var mean =
            weightedTemperatureSum /
            totalArea;

        if (!double.IsFinite(mean) ||
            mean < 0)
        {
            throw new InvalidOperationException(
                "Regional thermal state produced an invalid area-weighted surface temperature.");
        }

        return mean;
    }
}
