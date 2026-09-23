export interface LocalActorPresentationPosition {
  eastMeters: number
  verticalMeters: number
  northMeters: number
}

export interface LocalActorPresentationTransition {
  start: LocalActorPresentationPosition
  target: LocalActorPresentationPosition
  startedAtMilliseconds: number
  durationMilliseconds: number
}

export interface LocalActorPresentationSample {
  position: LocalActorPresentationPosition
  progress: number
  isComplete: boolean
}

export interface LocalActorPresentationVelocity {
  eastMetersPerSecond: number
  verticalMetersPerSecond: number
  northMetersPerSecond: number
}

export interface LocalActorPresentationContinuation {
  velocity: LocalActorPresentationVelocity
  maintainVelocityDuringTransition?: boolean
}

function validateFinite(
  value: number,
  label: string,
): void {
  if (!Number.isFinite(value)) {
    throw new RangeError(
      `${label} must be finite.`,
    )
  }
}

function validatePosition(
  position: LocalActorPresentationPosition,
  label: string,
): void {
  validateFinite(
    position.eastMeters,
    `${label} east`,
  )

  validateFinite(
    position.verticalMeters,
    `${label} vertical`,
  )

  validateFinite(
    position.northMeters,
    `${label} north`,
  )
}

export function createLocalActorPresentationTransition(
  start: LocalActorPresentationPosition,
  target: LocalActorPresentationPosition,
  startedAtMilliseconds: number,
  durationMilliseconds: number,
): LocalActorPresentationTransition {
  validatePosition(
    start,
    'Local actor transition start',
  )

  validatePosition(
    target,
    'Local actor transition target',
  )

  validateFinite(
    startedAtMilliseconds,
    'Local actor transition start time',
  )

  if (
    !Number.isFinite(
      durationMilliseconds,
    ) ||
    durationMilliseconds <= 0
  ) {
    throw new RangeError(
      'Local actor transition duration must be positive and finite.',
    )
  }

  return {
    start: {
      ...start,
    },
    target: {
      ...target,
    },
    startedAtMilliseconds,
    durationMilliseconds,
  }
}

export function sampleLocalActorPresentationTransition(
  transition: LocalActorPresentationTransition,
  nowMilliseconds: number,
  continuation?: LocalActorPresentationContinuation,
): LocalActorPresentationSample {
  validateFinite(
    nowMilliseconds,
    'Local actor transition sample time',
  )

  if (continuation !== undefined) {
    validateFinite(
      continuation.velocity.eastMetersPerSecond,
      'Local actor continuation east velocity',
    )

    validateFinite(
      continuation.velocity.verticalMetersPerSecond,
      'Local actor continuation vertical velocity',
    )

    validateFinite(
      continuation.velocity.northMetersPerSecond,
      'Local actor continuation north velocity',
    )
  }

  const rawProgress =
    (
      nowMilliseconds -
      transition.startedAtMilliseconds
    ) /
    transition.durationMilliseconds

  const progress =
    Math.min(
      1,
      Math.max(
        0,
        rawProgress,
      ),
    )

  const interpolate = (
    start: number,
    target: number,
  ) =>
    start +
    (
      target -
      start
    ) *
      progress

  const interpolatedPosition = {
    eastMeters:
      interpolate(
        transition.start.eastMeters,
        transition.target.eastMeters,
      ),
    verticalMeters:
      interpolate(
        transition.start.verticalMeters,
        transition.target.verticalMeters,
      ),
    northMeters:
      interpolate(
        transition.start.northMeters,
        transition.target.northMeters,
      ),
  }

  if (continuation === undefined) {
    return {
      position:
        interpolatedPosition,
      progress,
      isComplete:
        progress >= 1,
    }
  }

  const elapsedMilliseconds =
    Math.max(
      0,
      nowMilliseconds -
      transition.startedAtMilliseconds,
    )

  const elapsedSeconds =
    elapsedMilliseconds /
    1_000

  if (
    continuation
      .maintainVelocityDuringTransition ===
    true
  ) {
    // A continuing moving actor may already have been predicted close to
    // the newly arrived authoritative target. Preserve measured velocity
    // while the interpolation corrects only the remaining prediction error.
    return {
      position: {
        eastMeters:
          interpolatedPosition.eastMeters +
          continuation.velocity
            .eastMetersPerSecond *
            elapsedSeconds,
        verticalMeters:
          interpolatedPosition.verticalMeters +
          continuation.velocity
            .verticalMetersPerSecond *
            elapsedSeconds,
        northMeters:
          interpolatedPosition.northMeters +
          continuation.velocity
            .northMetersPerSecond *
            elapsedSeconds,
      },
      progress,
      isComplete:
        false,
    }
  }

  if (rawProgress <= 1) {
    return {
      position:
        interpolatedPosition,
      progress,
      isComplete:
        false,
    }
  }

  const transitionEndMilliseconds =
    transition.startedAtMilliseconds +
    transition.durationMilliseconds

  const continuationMilliseconds =
    Math.max(
      0,
      nowMilliseconds -
      transitionEndMilliseconds,
    )

  const continuationSeconds =
    continuationMilliseconds /
    1_000

  return {
    position: {
      eastMeters:
        transition.target.eastMeters +
        continuation.velocity
          .eastMetersPerSecond *
          continuationSeconds,
      verticalMeters:
        transition.target.verticalMeters +
        continuation.velocity
          .verticalMetersPerSecond *
          continuationSeconds,
      northMeters:
        transition.target.northMeters +
        continuation.velocity
          .northMetersPerSecond *
          continuationSeconds,
    },
    progress:
      1,
    // Prediction remains active until a newer authoritative snapshot
    // replaces this transition.
    isComplete:
      false,
  }
}
