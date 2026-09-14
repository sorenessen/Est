namespace Est.Simulation.Surface;

public sealed record SurfaceGridDefinition
{
    public SurfaceGridDefinition(
        SurfaceGridKind kind,
        int identityVersion,
        int latitudeBandCount,
        int longitudeBandCount)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind));
        }

        if (identityVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(identityVersion),
                "Surface-grid identity version must be positive.");
        }

        if (latitudeBandCount < 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitudeBandCount),
                "A surface grid requires at least two latitude bands.");
        }

        if (longitudeBandCount < 4)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitudeBandCount),
                "A surface grid requires at least four longitude bands.");
        }

        Kind = kind;
        IdentityVersion = identityVersion;
        LatitudeBandCount = latitudeBandCount;
        LongitudeBandCount = longitudeBandCount;
    }

    public SurfaceGridKind Kind { get; }

    public int IdentityVersion { get; }

    public int LatitudeBandCount { get; }

    public int LongitudeBandCount { get; }

    public static SurfaceGridDefinition
        LatitudeLongitude(
            int latitudeBandCount,
            int longitudeBandCount)
    {
        return new SurfaceGridDefinition(
            SurfaceGridKind.LatitudeLongitude,
            LatLonPlanetSurfaceGrid.IdentityVersion,
            latitudeBandCount,
            longitudeBandCount);
    }
}
