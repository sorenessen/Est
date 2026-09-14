namespace Est.Simulation.Surface;

public sealed record SurfaceCoordinate
{
    public SurfaceCoordinate(
        double latitudeDegrees,
        double longitudeDegrees)
    {
        if (!double.IsFinite(latitudeDegrees) ||
            latitudeDegrees < -90 ||
            latitudeDegrees > 90)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitudeDegrees),
                "Surface latitude must be finite and between -90 and 90 degrees.");
        }

        if (!double.IsFinite(longitudeDegrees) ||
            longitudeDegrees < -180 ||
            longitudeDegrees > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitudeDegrees),
                "Surface longitude must be finite and between -180 and 180 degrees.");
        }

        LatitudeDegrees = latitudeDegrees;
        LongitudeDegrees = longitudeDegrees;
    }

    public double LatitudeDegrees { get; }

    public double LongitudeDegrees { get; }
}
