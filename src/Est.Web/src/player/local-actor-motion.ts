import {
  geographicToLocalMeters,
  type GeographicCoordinate,
} from './local-play-space'


export interface LocalActorMotionObservation {
  actorKey: string
  movedDistanceMeters: number
  headingRadians: number | null
  isMoving: boolean
}

export interface ObserveLocalActorMotionInput {
  actorKey: string
  coordinate: GeographicCoordinate
  planetRadiusMeters: number
  minimumMovementMeters?: number
}

interface TrackedLocalActorMotion {
  coordinate: GeographicCoordinate
  headingRadians: number | null
}

const defaultMinimumMovementMeters =
  0.01

/**
 * Observes successive authoritative geographic positions for stable actors.
 *
 * Heading is presentation state derived from authoritative displacement.
 * It is not simulation authority and does not create velocity, movement,
 * or position that the simulation did not provide.
 */
export class LocalActorMotionTracker {
  private readonly tracked =
    new Map<
      string,
      TrackedLocalActorMotion
    >()

  observe(
    input: ObserveLocalActorMotionInput,
  ): LocalActorMotionObservation {
    if (
      !input.actorKey
    ) {
      throw new Error(
        'Local actor motion requires a stable actor key.',
      )
    }

    if (
      !Number.isFinite(
        input.planetRadiusMeters,
      ) ||
      input.planetRadiusMeters <= 0
    ) {
      throw new RangeError(
        'Local actor motion requires a positive finite planet radius.',
      )
    }

    const minimumMovementMeters =
      input.minimumMovementMeters ??
      defaultMinimumMovementMeters

    if (
      !Number.isFinite(
        minimumMovementMeters,
      ) ||
      minimumMovementMeters < 0
    ) {
      throw new RangeError(
        'Local actor motion threshold must be finite and non-negative.',
      )
    }

    const previous =
      this.tracked.get(
        input.actorKey,
      )

    if (!previous) {
      this.tracked.set(
        input.actorKey,
        {
          coordinate: {
            ...input.coordinate,
          },
          headingRadians:
            null,
        },
      )

      return {
        actorKey:
          input.actorKey,
        movedDistanceMeters:
          0,
        headingRadians:
          null,
        isMoving:
          false,
      }
    }

    const displacement =
      geographicToLocalMeters(
        input.coordinate,
        previous.coordinate,
        input.planetRadiusMeters,
      )

    const movedDistanceMeters =
      Math.hypot(
        displacement.eastMeters,
        displacement.northMeters,
      )

    const isMoving =
      movedDistanceMeters >
      minimumMovementMeters

    const headingRadians =
      isMoving
        ? Math.atan2(
            displacement.northMeters,
            displacement.eastMeters,
          )
        : previous.headingRadians

    this.tracked.set(
      input.actorKey,
      {
        coordinate: {
          ...input.coordinate,
        },
        headingRadians,
      },
    )

    return {
      actorKey:
        input.actorKey,
      movedDistanceMeters,
      headingRadians,
      isMoving,
    }
  }

  forget(
    actorKey: string,
  ): void {
    this.tracked.delete(
      actorKey,
    )
  }

  clear(): void {
    this.tracked.clear()
  }
}
