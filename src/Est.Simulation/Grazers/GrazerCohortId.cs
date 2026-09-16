using System.Security.Cryptography;
using Est.Simulation.Planets;
using Est.Simulation.Surface;

namespace Est.Simulation.Grazers;

public readonly record struct GrazerCohortId(Guid Value)
{
    public static GrazerCohortId New()
    {
        return new GrazerCohortId(
            Guid.NewGuid());
    }

    public static GrazerCohortId CreateDeterministic(
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

        return new GrazerCohortId(
            new Guid(
                hash[..16]));
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
