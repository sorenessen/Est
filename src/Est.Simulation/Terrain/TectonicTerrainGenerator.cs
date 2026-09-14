using Est.Simulation.Planets;
using Est.Simulation.Surface;

namespace Est.Simulation.Terrain;

/// <summary>
/// Generates deterministic tectonic-informed initial topography.
///
/// The generator deliberately models broad geological structure before
/// fine surface roughness:
///
/// - spherical tectonic domains,
/// - continental versus oceanic crust tendency,
/// - rigid plate-motion tendencies,
/// - convergent, divergent, and transform boundary effects,
/// - broad plate-interior relief,
/// - subordinate large-scale roughness.
///
/// It is not intended to be a geodynamic mantle simulation. Its output is
/// durable terrain state that later systems can use for drainage, climate,
/// erosion, soils, and ecology.
/// </summary>
public static class TectonicTerrainGenerator
{
    private const double TwoPi =
        Math.PI * 2;

    public static PlanetTerrainState Generate(
        PlanetState planet,
        TectonicTerrainGenerationParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(planet);
        ArgumentNullException.ThrowIfNull(parameters);

        var grid =
            PlanetSurfaceGridFactory.Create(
                planet,
                parameters.GridDefinition);

        if (parameters.PlateCount > grid.CellCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(parameters),
                "Tectonic plate count cannot exceed surface-cell count.");
        }

        var random =
            new DeterministicRandom(
                CreateSeed(
                    parameters.Seed));

        var plates =
            CreatePlates(
                parameters,
                random);

        var cells =
            new TerrainCellState[
                grid.CellCount];

        for (var index = 0;
             index < grid.Cells.Count;
             index++)
        {
            var surfaceCell =
                grid.Cells[index];

            var position =
                FromLatitudeLongitude(
                    surfaceCell.CenterLatitudeDegrees,
                    surfaceCell.CenterLongitudeDegrees);

            var assignment =
                FindNearestPlates(
                    position,
                    plates);

            var elevation =
                CalculateElevationMeters(
                    surfaceCell,
                    position,
                    plates[assignment.PrimaryIndex],
                    plates[assignment.SecondaryIndex],
                    assignment.PrimaryScore,
                    assignment.SecondaryScore,
                    parameters.Seed);

            cells[index] =
                new TerrainCellState(
                    surfaceCell.Id,
                    elevation);
        }

        var terrain =
            new PlanetTerrainState(
                planet.Id,
                parameters.GridDefinition,
                cells);

        terrain.ValidateFor(planet);

        return terrain;
    }

    private static Plate[] CreatePlates(
        TectonicTerrainGenerationParameters parameters,
        DeterministicRandom random)
    {
        var plates =
            new Plate[
                parameters.PlateCount];

        var continentalCount = 0;

        for (var index = 0;
             index < plates.Length;
             index++)
        {
            var isContinental =
                random.NextDouble() <
                parameters.ContinentalPlateFraction;

            if (isContinental)
            {
                continentalCount++;
            }

            plates[index] =
                CreatePlate(
                    random,
                    isContinental);
        }

        // For mixed-world defaults, guarantee both crustal regimes.
        // This avoids pathological all-land or all-ocean initial
        // topography from an otherwise valid deterministic seed.
        if (parameters.ContinentalPlateFraction > 0 &&
            parameters.ContinentalPlateFraction < 1)
        {
            if (continentalCount == 0)
            {
                plates[0] =
                    plates[0] with
                    {
                        IsContinental = true,
                        BaseElevationMeters =
                            CreateContinentalBaseElevation(
                                random)
                    };
            }
            else if (continentalCount == plates.Length)
            {
                plates[^1] =
                    plates[^1] with
                    {
                        IsContinental = false,
                        BaseElevationMeters =
                            CreateOceanicBaseElevation(
                                random)
                    };
            }
        }

        return plates;
    }

    private static Plate CreatePlate(
        DeterministicRandom random,
        bool isContinental)
    {
        var seedPosition =
            RandomUnitVector(
                random);

        var rotationAxis =
            RandomUnitVector(
                random);

        var angularSpeed =
            0.35 +
            random.NextDouble() *
            0.65;

        var baseElevation =
            isContinental
                ? CreateContinentalBaseElevation(
                    random)
                : CreateOceanicBaseElevation(
                    random);

        var reliefAmplitude =
            isContinental
                ? 650 +
                  random.NextDouble() * 850
                : 250 +
                  random.NextDouble() * 500;

        var latitudeFrequency =
            0.75 +
            random.NextDouble() * 1.75;

        var longitudeFrequency =
            0.75 +
            random.NextDouble() * 2.25;

        var reliefPhase =
            random.NextDouble() *
            TwoPi;

        return new Plate(
            seedPosition,
            rotationAxis,
            angularSpeed,
            isContinental,
            baseElevation,
            reliefAmplitude,
            latitudeFrequency,
            longitudeFrequency,
            reliefPhase);
    }

    private static double
        CreateContinentalBaseElevation(
            DeterministicRandom random)
    {
        return 450 +
               random.NextDouble() *
               900;
    }

    private static double
        CreateOceanicBaseElevation(
            DeterministicRandom random)
    {
        return -4_700 +
               random.NextDouble() *
               1_400;
    }

    private static PlateAssignment
        FindNearestPlates(
            Vector3d position,
            IReadOnlyList<Plate> plates)
    {
        var primaryIndex = -1;
        var secondaryIndex = -1;
        var primaryScore =
            double.NegativeInfinity;
        var secondaryScore =
            double.NegativeInfinity;

        for (var index = 0;
             index < plates.Count;
             index++)
        {
            var score =
                Vector3d.Dot(
                    position,
                    plates[index].SeedPosition);

            if (score > primaryScore)
            {
                secondaryIndex =
                    primaryIndex;

                secondaryScore =
                    primaryScore;

                primaryIndex =
                    index;

                primaryScore =
                    score;
            }
            else if (score > secondaryScore)
            {
                secondaryIndex =
                    index;

                secondaryScore =
                    score;
            }
        }

        if (primaryIndex < 0 ||
            secondaryIndex < 0)
        {
            throw new InvalidOperationException(
                "Terrain generation could not identify two tectonic domains.");
        }

        return new PlateAssignment(
            primaryIndex,
            secondaryIndex,
            primaryScore,
            secondaryScore);
    }

    private static double CalculateElevationMeters(
        SurfaceCell cell,
        Vector3d position,
        Plate primary,
        Plate secondary,
        double primaryScore,
        double secondaryScore,
        int seed)
    {
        var latitudeRadians =
            DegreesToRadians(
                cell.CenterLatitudeDegrees);

        var longitudeRadians =
            DegreesToRadians(
                cell.CenterLongitudeDegrees);

        var broadInteriorRelief =
            primary.ReliefAmplitudeMeters *
            Math.Sin(
                latitudeRadians *
                    primary.LatitudeFrequency +
                primary.ReliefPhase) *
            Math.Cos(
                longitudeRadians *
                    primary.LongitudeFrequency -
                primary.ReliefPhase * 0.5);

        var plateMargin =
            Math.Max(
                0,
                primaryScore -
                secondaryScore);

        // The two closest spherical plate seeds have nearly equal scores
        // close to their shared boundary. Exponential decay gives broad
        // mountain/rift belts without making boundaries razor thin.
        var boundaryInfluence =
            Math.Exp(
                -12 * plateMargin);

        var primaryVelocity =
            PlateVelocity(
                primary,
                position);

        var secondaryVelocity =
            PlateVelocity(
                secondary,
                position);

        var towardSecondary =
            TangentDirectionToward(
                position,
                secondary.SeedPosition);

        var relativeVelocity =
            secondaryVelocity -
            primaryVelocity;

        var separationRate =
            Vector3d.Dot(
                relativeVelocity,
                towardSecondary);

        var convergence =
            Math.Clamp(
                -separationRate / 1.5,
                0,
                1);

        var divergence =
            Math.Clamp(
                separationRate / 1.5,
                0,
                1);

        var boundaryRelief =
            CalculateBoundaryReliefMeters(
                primary,
                secondary,
                convergence,
                divergence,
                boundaryInfluence);

        var regionalRoughness =
            CalculateRegionalRoughnessMeters(
                latitudeRadians,
                longitudeRadians,
                seed);

        var elevation =
            primary.BaseElevationMeters +
            broadInteriorRelief +
            boundaryRelief +
            regionalRoughness;

        return Math.Round(
            elevation,
            3,
            MidpointRounding.AwayFromZero);
    }

    private static double
        CalculateBoundaryReliefMeters(
            Plate primary,
            Plate secondary,
            double convergence,
            double divergence,
            double boundaryInfluence)
    {
        double convergenceRelief;

        if (primary.IsContinental &&
            secondary.IsContinental)
        {
            // Continental collision produces the largest mountain belts.
            convergenceRelief =
                4_800 *
                convergence;
        }
        else if (primary.IsContinental &&
                 !secondary.IsContinental)
        {
            // Continental side of an ocean-continent subduction zone.
            convergenceRelief =
                3_600 *
                convergence;
        }
        else if (!primary.IsContinental &&
                 secondary.IsContinental)
        {
            // Oceanic side tends toward a trench.
            convergenceRelief =
                -2_000 *
                convergence;
        }
        else
        {
            // Ocean-ocean convergence can produce arcs and elevated
            // boundary structure without creating continental-scale
            // relief.
            convergenceRelief =
                1_300 *
                convergence;
        }

        double divergenceRelief;

        if (primary.IsContinental)
        {
            // Continental divergence initially expresses as rifting.
            divergenceRelief =
                -1_100 *
                divergence;
        }
        else
        {
            // Oceanic divergence creates a raised spreading ridge
            // relative to abyssal oceanic crust.
            divergenceRelief =
                1_700 *
                divergence;
        }

        return boundaryInfluence *
               (convergenceRelief +
                divergenceRelief);
    }

    private static double
        CalculateRegionalRoughnessMeters(
            double latitudeRadians,
            double longitudeRadians,
            int seed)
    {
        var phaseA =
            HashPhase(
                seed,
                0xA24BAED4963EE407UL);

        var phaseB =
            HashPhase(
                seed,
                0x9FB21C651E98DF25UL);

        var phaseC =
            HashPhase(
                seed,
                0xC13FA9A902A6328FUL);

        // Deliberately subordinate to the tectonic structure. These
        // harmonics prevent plate interiors from becoming perfectly
        // smooth while avoiding generic high-amplitude fractal noise as
        // the source of continents and mountain chains.
        return
            220 *
            Math.Sin(
                latitudeRadians * 3 +
                phaseA) *
            Math.Cos(
                longitudeRadians * 4 +
                phaseB)
            +
            120 *
            Math.Cos(
                latitudeRadians * 5 -
                phaseC) *
            Math.Sin(
                longitudeRadians * 7 +
                phaseA * 0.5);
    }

    private static Vector3d PlateVelocity(
        Plate plate,
        Vector3d position)
    {
        return Vector3d.Cross(
                   plate.RotationAxis,
                   position) *
               plate.AngularSpeed;
    }

    private static Vector3d
        TangentDirectionToward(
            Vector3d origin,
            Vector3d target)
    {
        var projection =
            target -
            origin *
            Vector3d.Dot(
                origin,
                target);

        var length =
            projection.Length;

        if (length <= 1e-12)
        {
            return Vector3d.Zero;
        }

        return projection /
               length;
    }

    private static Vector3d
        FromLatitudeLongitude(
            double latitudeDegrees,
            double longitudeDegrees)
    {
        var latitude =
            DegreesToRadians(
                latitudeDegrees);

        var longitude =
            DegreesToRadians(
                longitudeDegrees);

        var cosLatitude =
            Math.Cos(latitude);

        return new Vector3d(
            cosLatitude *
            Math.Cos(longitude),
            cosLatitude *
            Math.Sin(longitude),
            Math.Sin(latitude));
    }

    private static Vector3d RandomUnitVector(
        DeterministicRandom random)
    {
        var z =
            random.NextDouble() * 2 -
            1;

        var longitude =
            random.NextDouble() *
            TwoPi;

        var radial =
            Math.Sqrt(
                Math.Max(
                    0,
                    1 - z * z));

        return new Vector3d(
            radial * Math.Cos(longitude),
            radial * Math.Sin(longitude),
            z);
    }

    private static double DegreesToRadians(
        double degrees)
    {
        return degrees *
               Math.PI /
               180;
    }

    private static ulong CreateSeed(
        int configuredSeed)
    {
        return Mix64(
            (ulong)(uint)configuredSeed ^
            0xD1B54A32D192ED03UL);
    }

    private static double HashPhase(
        int seed,
        ulong salt)
    {
        var value =
            Mix64(
                (ulong)(uint)seed ^
                salt);

        var unit =
            (value >> 11) *
            (1.0 / (1UL << 53));

        return unit *
               TwoPi;
    }

    private static ulong Mix64(
        ulong value)
    {
        unchecked
        {
            value +=
                0x9E3779B97F4A7C15UL;

            value =
                (value ^
                 (value >> 30)) *
                0xBF58476D1CE4E5B9UL;

            value =
                (value ^
                 (value >> 27)) *
                0x94D049BB133111EBUL;

            return value ^
                   (value >> 31);
        }
    }

    private sealed record Plate(
        Vector3d SeedPosition,
        Vector3d RotationAxis,
        double AngularSpeed,
        bool IsContinental,
        double BaseElevationMeters,
        double ReliefAmplitudeMeters,
        double LatitudeFrequency,
        double LongitudeFrequency,
        double ReliefPhase);

    private readonly record struct PlateAssignment(
        int PrimaryIndex,
        int SecondaryIndex,
        double PrimaryScore,
        double SecondaryScore);

    private readonly record struct Vector3d(
        double X,
        double Y,
        double Z)
    {
        public static Vector3d Zero =>
            new(
                0,
                0,
                0);

        public double Length =>
            Math.Sqrt(
                X * X +
                Y * Y +
                Z * Z);

        public static double Dot(
            Vector3d left,
            Vector3d right)
        {
            return
                left.X * right.X +
                left.Y * right.Y +
                left.Z * right.Z;
        }

        public static Vector3d Cross(
            Vector3d left,
            Vector3d right)
        {
            return new Vector3d(
                left.Y * right.Z -
                left.Z * right.Y,
                left.Z * right.X -
                left.X * right.Z,
                left.X * right.Y -
                left.Y * right.X);
        }

        public static Vector3d operator +(
            Vector3d left,
            Vector3d right)
        {
            return new Vector3d(
                left.X + right.X,
                left.Y + right.Y,
                left.Z + right.Z);
        }

        public static Vector3d operator -(
            Vector3d left,
            Vector3d right)
        {
            return new Vector3d(
                left.X - right.X,
                left.Y - right.Y,
                left.Z - right.Z);
        }

        public static Vector3d operator *(
            Vector3d vector,
            double scalar)
        {
            return new Vector3d(
                vector.X * scalar,
                vector.Y * scalar,
                vector.Z * scalar);
        }

        public static Vector3d operator /(
            Vector3d vector,
            double scalar)
        {
            return new Vector3d(
                vector.X / scalar,
                vector.Y / scalar,
                vector.Z / scalar);
        }
    }

    private sealed class DeterministicRandom
    {
        private ulong _state;

        public DeterministicRandom(
            ulong seed)
        {
            _state =
                seed == 0
                    ? 0xA0761D6478BD642FUL
                    : seed;
        }

        public double NextDouble()
        {
            var value =
                NextUInt64() >> 11;

            return value *
                   (1.0 / (1UL << 53));
        }

        private ulong NextUInt64()
        {
            var value =
                _state;

            value ^=
                value >> 12;

            value ^=
                value << 25;

            value ^=
                value >> 27;

            _state =
                value;

            return value *
                   2685821657736338717UL;
        }
    }
}
