import type {
  FaunaPresentation,
} from './fauna-presentation-contract'

import type {
  WolfPresentationState,
} from './wolf-presentation-state'


export interface WolfPresentation
  extends FaunaPresentation {
  setState(
    state: WolfPresentationState,
  ): void
  setGaitPhase(
    phaseRadians: number,
  ): void
}

export const wolfPresentationAssetPath =
  '/assets/animals/quaternius/ultimate-animated-animals/Wolf.gltf'

export const wolfPresentationAnimationNames = {
  attack: 'Attack',
  eating: 'Eating',
  huntingIdle: 'Idle_2_HeadLow',
  idle: 'Idle',
  walk: 'Walk',
} as const

export type WolfPresentationAnimationName =
  typeof wolfPresentationAnimationNames[
    keyof typeof wolfPresentationAnimationNames
  ]

export type WolfPresentationAnimationPlayback =
  | 'loop'
  | 'gait-phase'

export interface WolfPresentationAnimationSelection {
  name: WolfPresentationAnimationName
  playback: WolfPresentationAnimationPlayback
}

/**
 * Maps renderer-independent wolf presentation state onto the current
 * Quaternius asset.
 *
 * Authoritative displacement takes precedence for the single non-additive
 * rig: any moving wolf uses the in-place Walk clip. The authoritative
 * activity itself is not changed. Once displacement stops, the stationary
 * presentation returns to the activity-specific clip.
 */
export function resolveWolfPresentationAnimation(
  state: WolfPresentationState,
): WolfPresentationAnimationSelection {
  if (
    state.locomotion ===
    'moving'
  ) {
    return {
      name:
        wolfPresentationAnimationNames.walk,
      playback:
        'gait-phase',
    }
  }

  switch (state.activity) {
    case 'idle':
    case 'traveling':
      return {
        name:
          wolfPresentationAnimationNames.idle,
        playback:
          'loop',
      }

    case 'hunting':
      return {
        name:
          wolfPresentationAnimationNames
            .huntingIdle,
        playback:
          'loop',
      }

    case 'attacking':
      return {
        name:
          wolfPresentationAnimationNames.attack,
        playback:
          'loop',
      }

    case 'eating':
      return {
        name:
          wolfPresentationAnimationNames.eating,
        playback:
          'loop',
      }
  }
}

/**
 * Converts Est's periodic gait phase into a Babylon animation frame.
 *
 * A complete 2π cycle wraps back to the source frame. This keeps the
 * imported in-place gait subordinate to Est's bounded authoritative
 * interpolation rather than allowing animation time to advance movement.
 */
export function resolveWolfGaitFrame(
  phaseRadians: number,
  fromFrame: number,
  toFrame: number,
): number {
  if (
    !Number.isFinite(
      phaseRadians,
    )
  ) {
    throw new RangeError(
      'Wolf gait phase must be finite.',
    )
  }

  if (
    !Number.isFinite(
      fromFrame,
    ) ||
    !Number.isFinite(
      toFrame,
    ) ||
    toFrame <
      fromFrame
  ) {
    throw new RangeError(
      'Wolf animation frame range must be finite and ordered.',
    )
  }

  const cycleRadians =
    Math.PI *
    2

  const wrappedPhase =
    (
      (
        phaseRadians %
        cycleRadians
      ) +
      cycleRadians
    ) %
    cycleRadians

  const progress =
    wrappedPhase /
    cycleRadians

  return (
    fromFrame +
    (
      toFrame -
      fromFrame
    ) *
      progress
  )
}
