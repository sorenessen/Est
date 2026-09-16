using System.Security.Cryptography;
using Est.Simulation.Planets;
using Est.Simulation.Surface;

namespace Est.Simulation.Birds;

public readonly record struct BirdFlockId(Guid Value)
{
    public static BirdFlockId New()
    {
        return new BirdFlockId(
            Guid.NewGuid());
    }

    public static BirdFlockId CreateDeterministic(
        PlanetId planetId,
        SurfaceCellId surfaceCellId)
    {
        if (planetId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Planet identity cannot be empty.",
                nameof(planetId));
        }

        Span<byte> source =
            stackalloc byte[32];

        planetId.Value.TryWriteBytes(
            source[..16]);

        surfaceCellId.Value.TryWriteBytes(
            source[16..]);

        Span<byte> hash =
            stackalloc byte[32];

        SHA256.HashData(
            source,
            hash);

        return new BirdFlockId(
            new Guid(
                hash[..16]));
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
