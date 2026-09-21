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
  snapshotTimeSeconds: number
  minimumMovementMeters?: number
}

interface TrackedLocalActorMotion {
  coordinate: GeographicCoordinate
  snapshotTimeSeconds: number
  observation: LocalActorMotionObservation
}

const defaultMinimumMovementMeters =
  0.01

/**
 * Observes successive authoritative geographic snapshots for stable actors.
 *
 * Re-projecting the same authoritative snapshot returns the same motion
 * observation. Renderer updates caused by Ester movement, camera movement,
 * terrain refresh, or another presentation concern therefore cannot turn
 * one authoritative displacement into a later false stationary observation.
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
    if (!input.actorKey) {
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

    if (
      !Number.isFinite(
        input.snapshotTimeSeconds,
      )
    ) {
      throw new RangeError(
        'Local actor motion snapshot time must be finite.',
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

    if (
      previous &&
      input.snapshotTimeSeconds <
        previous.snapshotTimeSeconds
    ) {
      throw new RangeError(
        'Local actor motion snapshots must be observed in non-decreasing time order.',
      )
    }

    if (
      previous &&
      input.snapshotTimeSeconds ===
        previous.snapshotTimeSeconds
    ) {
      return previous.observation
    }

    if (!previous) {
      const observation = {
        actorKey:
          input.actorKey,
        movedDistanceMeters:
          0,
        headingRadians:
          null,
        isMoving:
          false,
      }

      this.tracked.set(
        input.actorKey,
        {
          coordinate: {
            ...input.coordinate,
          },
          snapshotTimeSeconds:
            input.snapshotTimeSeconds,
          observation,
        },
      )

      return observation
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
        : previous.observation
            .headingRadians

    const observation = {
      actorKey:
        input.actorKey,
      movedDistanceMeters,
      headingRadians,
      isMoving,
    }

    this.tracked.set(
      input.actorKey,
      {
        coordinate: {
          ...input.coordinate,
        },
        snapshotTimeSeconds:
          input.snapshotTimeSeconds,
        observation,
      },
    )

    return observation
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
