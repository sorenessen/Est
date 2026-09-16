namespace Est.Simulation.Organisms;

/// <summary>
/// Shared lifecycle-time calculations based on authoritative simulation time.
///
/// The common substrate intentionally uses seconds rather than assuming an
/// Earth-specific calendar or orbital year.
/// </summary>
public static class OrganismLifecycleClock
{
    public static long AgeSeconds(
        long birthTimeSeconds,
        long currentTimeSeconds)
    {
        if (currentTimeSeconds <=
            birthTimeSeconds)
        {
            return 0;
        }

        return checked(
            currentTimeSeconds -
            birthTimeSeconds);
    }
}
