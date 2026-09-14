namespace Est.Simulation.Surface;

public sealed record SurfaceCell
{
    public SurfaceCell(
        SurfaceCellId id,
        double centerLatitudeDegrees,
        double centerLongitudeDegrees,
        double areaSquareMeters)
    {
        if (!double.IsFinite(centerLatitudeDegrees) ||
            centerLatitudeDegrees < -90 ||
            centerLatitudeDegrees > 90)
        {
            throw new ArgumentOutOfRangeException(
                nameof(centerLatitudeDegrees),
                "Surface-cell latitude must be finite and between -90 and 90 degrees.");
        }

        if (!double.IsFinite(centerLongitudeDegrees) ||
            centerLongitudeDegrees < -180 ||
            centerLongitudeDegrees >= 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(centerLongitudeDegrees),
                "Surface-cell longitude must be finite and in [-180, 180).");
        }

        if (!double.IsFinite(areaSquareMeters) ||
            areaSquareMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(areaSquareMeters),
                "Surface-cell area must be finite and positive.");
        }

        Id = id;
        CenterLatitudeDegrees = centerLatitudeDegrees;
        CenterLongitudeDegrees = centerLongitudeDegrees;
        AreaSquareMeters = areaSquareMeters;
    }

    public SurfaceCellId Id { get; }

    public double CenterLatitudeDegrees { get; }

    public double CenterLongitudeDegrees { get; }

    public double AreaSquareMeters { get; }
}
