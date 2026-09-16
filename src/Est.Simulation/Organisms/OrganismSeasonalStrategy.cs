namespace Est.Simulation.Organisms;

/// <summary>
/// Species-policy vocabulary for lifecycle strategies whose activation may
/// depend on season, climate, resources, or other ecological signals.
///
/// Flags allow a species to support more than one strategy without implying
/// that every strategy is simultaneously active.
/// </summary>
[Flags]
public enum OrganismSeasonalStrategy
{
    None = 0,
    Breeding = 1 << 0,
    Migration = 1 << 1,
    Torpor = 1 << 2,
    Dormancy = 1 << 3,
    Hibernation = 1 << 4
}
