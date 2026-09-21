export type WolfPresentationActivity =
  | 'idle'
  | 'hunting'
  | 'traveling'
  | 'attacking'
  | 'eating'

export type WolfPresentationLocomotion =
  | 'stationary'
  | 'moving'

export interface WolfPresentationState {
  activity: WolfPresentationActivity
  locomotion: WolfPresentationLocomotion
}

/**
 * Maps authoritative wolf activity plus authoritative displacement
 * observation into renderer-independent presentation state.
 *
 * This function does not infer simulation activity from animation or
 * manufacture movement. `isMoving` must come from observed authoritative
 * geographic displacement for the same stable AnimalId.
 */
export function resolveWolfPresentationState(
  authoritativeActivity: string,
  isMoving: boolean,
): WolfPresentationState {
  let activity:
    WolfPresentationActivity

  switch (
    authoritativeActivity
      .trim()
      .toLowerCase()
  ) {
    case 'idle':
      activity =
        'idle'
      break

    case 'hunting':
      activity =
        'hunting'
      break

    case 'traveling':
      activity =
        'traveling'
      break

    case 'attacking':
      activity =
        'attacking'
      break

    case 'eating':
      activity =
        'eating'
      break

    default:
      throw new Error(
        `Unsupported authoritative wolf activity: ${authoritativeActivity}`,
      )
  }

  return {
    activity,
    locomotion:
      isMoving
        ? 'moving'
        : 'stationary',
  }
}
