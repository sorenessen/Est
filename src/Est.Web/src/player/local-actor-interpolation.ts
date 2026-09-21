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
): LocalActorPresentationSample {
  validateFinite(
    nowMilliseconds,
    'Local actor transition sample time',
  )

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

  return {
    position: {
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
    },
    progress,
    isComplete:
      progress >= 1,
  }
}
