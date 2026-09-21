import type {
  Scene,
} from '@babylonjs/core'

import type {
  FaunaPresentation,
} from './fauna-presentation-contract'

import type {
  GrazerPresentationState,
} from './grazer-presentation-state'


export interface GrazerPresentation
  extends FaunaPresentation {
  setSimulationTimeSeconds(
    simulationTimeSeconds: number,
  ): void
  setState(
    state: GrazerPresentationState,
  ): void
  setGaitPhase(
    phaseRadians: number,
  ): void
}

export const grazerPresentationAssetPath =
  '/assets/animals/quaternius/ultimate-animated-animals/Deer.gltf'

/**
 * The Quaternius deer is authored facing +Z. Est fauna presentations face
 * local +X/east, so the asset-specific child rotates +Z onto +X.
 *
 * This correction belongs below the Est-owned presentation root and therefore
 * does not redefine cohort position, presentation identity, or heading.
 */
export const grazerPresentationAssetYawCorrectionRadians =
  Math.PI / 2

/**
 * The source mesh is approximately 4.40 units long and 4.27 units tall.
 * At 0.30 presentation scale it is approximately 1.32 m long and
 * 1.28 m tall in Est's local metre-space.
 */
export const grazerPresentationAssetScale =
  0.30

export const grazerPresentationAnimationNames = {
  idle: 'Idle',
  walk: 'Walk',
} as const

export type GrazerPresentationAnimationName =
  typeof grazerPresentationAnimationNames[
    keyof typeof grazerPresentationAnimationNames
  ]

export type GrazerPresentationAnimationPlayback =
  | 'simulation-time'
  | 'gait-phase'

export interface GrazerPresentationAnimationSelection {
  name: GrazerPresentationAnimationName
  playback: GrazerPresentationAnimationPlayback
}

/**
 * Grazer representatives have presentation locomotion only. Est does not
 * currently expose per-representative authoritative activity such as eating,
 * attacking, or dying.
 */
export function resolveGrazerPresentationAnimation(
  state: GrazerPresentationState,
): GrazerPresentationAnimationSelection {
  if (
    state.locomotion ===
    'moving'
  ) {
    return {
      name:
        grazerPresentationAnimationNames.walk,
      playback:
        'gait-phase',
    }
  }

  return {
    name:
      grazerPresentationAnimationNames.idle,
    playback:
      'simulation-time',
  }
}

export function resolveGrazerGaitFrame(
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
      'Grazer gait phase must be finite.',
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
      'Grazer animation frame range must be finite and ordered.',
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

export function resolveGrazerSimulationFrame(
  simulationTimeSeconds: number,
  fromFrame: number,
  toFrame: number,
  durationSeconds: number,
): number {
  if (
    !Number.isFinite(
      simulationTimeSeconds,
    )
  ) {
    throw new RangeError(
      'Grazer simulation time must be finite.',
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
      fromFrame ||
    !Number.isFinite(
      durationSeconds,
    ) ||
    durationSeconds <=
      0
  ) {
    throw new RangeError(
      'Grazer animation frame range and duration must be finite, ordered, and positive.',
    )
  }

  const wrappedSeconds =
    (
      (
        simulationTimeSeconds %
        durationSeconds
      ) +
      durationSeconds
    ) %
    durationSeconds

  const progress =
    wrappedSeconds /
    durationSeconds

  return (
    fromFrame +
    (
      toFrame -
      fromFrame
    ) *
      progress
  )
}

/**
 * Implemented in the next renderer slice after the asset policy and sampling
 * contract are validated independently.
 */
export type GrazerPresentationFactory =
  (
    scene: Scene,
    actorKey: string,
  ) => Promise<GrazerPresentation>
