namespace Est.Simulation.Organisms;

/// <summary>
/// Shared coarse lifecycle stages for material-bearing organisms.
///
/// Species may maintain richer lifecycle state, but these stages provide common
/// semantics for maturity-sensitive behavior without requiring a shared
/// behavioral representation.
/// </summary>
public enum OrganismLifecycleStage
{
    Immature,
    Mature,
    Senescent
}
