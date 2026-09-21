export type GrazerPresentationLocomotion =
  | 'stationary'
  | 'moving'

export interface GrazerPresentationState {
  locomotion: GrazerPresentationLocomotion
}

/**
 * Grazer representatives are deterministic local presentation refinements of
 * authoritative aggregate cohorts. They are not individually simulated
 * animals and therefore do not own authoritative activity state.
 *
 * Presentation may nevertheless interpret observed displacement of one stable
 * representative key as locomotion. This does not assert that a particular
 * authoritative grazer individual followed that walking-scale path.
 */
export function resolveGrazerPresentationState(
  hasObservedDisplacement: boolean,
): GrazerPresentationState {
  return {
    locomotion:
      hasObservedDisplacement
        ? 'moving'
        : 'stationary',
  }
}
