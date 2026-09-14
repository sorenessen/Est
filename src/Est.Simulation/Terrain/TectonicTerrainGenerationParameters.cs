using Est.Simulation.Surface;

namespace Est.Simulation.Terrain;

/// <summary>
/// Parameters for deterministic initial planet-scale topography.
///
/// This is an initial-condition generator, not a live plate-tectonics
/// simulation. Generated terrain becomes authoritative world state and is
/// persisted rather than regenerated during normal simulation.
/// </summary>
public sealed record TectonicTerrainGenerationParameters
{
    public TectonicTerrainGenerationParameters(
        SurfaceGridDefinition gridDefinition,
        int seed,
        int plateCount = 12,
        double continentalPlateFraction = 0.45)
    {
        ArgumentNullException.ThrowIfNull(gridDefinition);

        if (plateCount < 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(plateCount),
                "Terrain generation requires at least two tectonic plates.");
        }

        if (!double.IsFinite(continentalPlateFraction) ||
            continentalPlateFraction < 0 ||
            continentalPlateFraction > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(continentalPlateFraction),
                "Continental plate fraction must be between zero and one.");
        }

        GridDefinition = gridDefinition;
        Seed = seed;
        PlateCount = plateCount;
        ContinentalPlateFraction =
            continentalPlateFraction;
    }

    public SurfaceGridDefinition GridDefinition { get; }

    public int Seed { get; }

    public int PlateCount { get; }

    public double ContinentalPlateFraction { get; }
}
