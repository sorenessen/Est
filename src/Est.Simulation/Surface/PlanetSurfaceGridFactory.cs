using Est.Simulation.Planets;

namespace Est.Simulation.Surface;

public static class PlanetSurfaceGridFactory
{
    public static IPlanetSurfaceGrid Create(
        PlanetState planet,
        SurfaceGridDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(planet);
        ArgumentNullException.ThrowIfNull(definition);

        return definition.Kind switch
        {
            SurfaceGridKind.LatitudeLongitude
                when definition.IdentityVersion ==
                     LatLonPlanetSurfaceGrid.IdentityVersion =>
                    new LatLonPlanetSurfaceGrid(
                        planet.Id,
                        planet.MeanRadiusMeters,
                        definition.LatitudeBandCount,
                        definition.LongitudeBandCount),

            SurfaceGridKind.LatitudeLongitude =>
                throw new NotSupportedException(
                    $"Latitude/longitude surface-grid identity version {definition.IdentityVersion} is not supported."),

            _ =>
                throw new NotSupportedException(
                    $"Surface-grid kind {definition.Kind} is not supported.")
        };
    }
}
