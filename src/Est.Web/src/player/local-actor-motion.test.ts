import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  LocalActorMotionTracker,
} from './local-actor-motion'

import {
  moveSurfaceCoordinate,
} from './surface-movement'


const earthRadiusMeters =
  6_371_000

const origin = {
  latitudeDegrees:
    47,
  longitudeDegrees:
    -122,
}


describe(
  'LocalActorMotionTracker',
  () => {
    it(
      'does not invent heading before authoritative movement is observed',
      () => {
        const tracker =
          new LocalActorMotionTracker()

        const observation =
          tracker.observe({
            actorKey:
              'animal:wolf-a',
            coordinate:
              origin,
            planetRadiusMeters:
              earthRadiusMeters,
          })

        expect(
          observation,
        ).toEqual({
          actorKey:
            'animal:wolf-a',
          movedDistanceMeters:
            0,
          headingRadians:
            null,
          isMoving:
            false,
        })
      },
    )

    it(
      'derives eastward heading from authoritative displacement',
      () => {
        const tracker =
          new LocalActorMotionTracker()

        tracker.observe({
          actorKey:
            'animal:wolf-a',
          coordinate:
            origin,
          planetRadiusMeters:
            earthRadiusMeters,
        })

        const east =
          moveSurfaceCoordinate(
            origin,
            0,
            5,
            earthRadiusMeters,
          )

        const observation =
          tracker.observe({
            actorKey:
              'animal:wolf-a',
            coordinate:
              east,
            planetRadiusMeters:
              earthRadiusMeters,
          })

        expect(
          observation.isMoving,
        ).toBe(
          true,
        )

        expect(
          observation.movedDistanceMeters,
        ).toBeCloseTo(
          5,
          3,
        )

        expect(
          observation.headingRadians,
        ).toBeCloseTo(
          0,
          6,
        )
      },
    )

    it(
      'derives northward heading from authoritative displacement',
      () => {
        const tracker =
          new LocalActorMotionTracker()

        tracker.observe({
          actorKey:
            'animal:wolf-a',
          coordinate:
            origin,
          planetRadiusMeters:
            earthRadiusMeters,
        })

        const north =
          moveSurfaceCoordinate(
            origin,
            5,
            0,
            earthRadiusMeters,
          )

        const observation =
          tracker.observe({
            actorKey:
              'animal:wolf-a',
            coordinate:
              north,
            planetRadiusMeters:
              earthRadiusMeters,
          })

        expect(
          observation.headingRadians,
        ).toBeCloseTo(
          Math.PI / 2,
          6,
        )
      },
    )

    it(
      'preserves the last valid heading while the authoritative actor is stationary',
      () => {
        const tracker =
          new LocalActorMotionTracker()

        tracker.observe({
          actorKey:
            'animal:wolf-a',
          coordinate:
            origin,
          planetRadiusMeters:
            earthRadiusMeters,
        })

        const north =
          moveSurfaceCoordinate(
            origin,
            8,
            0,
            earthRadiusMeters,
          )

        tracker.observe({
          actorKey:
            'animal:wolf-a',
          coordinate:
            north,
          planetRadiusMeters:
            earthRadiusMeters,
        })

        const stationary =
          tracker.observe({
            actorKey:
              'animal:wolf-a',
            coordinate:
              north,
            planetRadiusMeters:
              earthRadiusMeters,
          })

        expect(
          stationary.isMoving,
        ).toBe(
          false,
        )

        expect(
          stationary.movedDistanceMeters,
        ).toBeCloseTo(
          0,
          6,
        )

        expect(
          stationary.headingRadians,
        ).toBeCloseTo(
          Math.PI / 2,
          6,
        )
      },
    )

    it(
      'tracks stable actor identities independently',
      () => {
        const tracker =
          new LocalActorMotionTracker()

        tracker.observe({
          actorKey:
            'animal:wolf-a',
          coordinate:
            origin,
          planetRadiusMeters:
            earthRadiusMeters,
        })

        tracker.observe({
          actorKey:
            'animal:wolf-b',
          coordinate:
            origin,
          planetRadiusMeters:
            earthRadiusMeters,
        })

        const east =
          moveSurfaceCoordinate(
            origin,
            0,
            4,
            earthRadiusMeters,
          )

        const north =
          moveSurfaceCoordinate(
            origin,
            4,
            0,
            earthRadiusMeters,
          )

        const wolfA =
          tracker.observe({
            actorKey:
              'animal:wolf-a',
            coordinate:
              east,
            planetRadiusMeters:
              earthRadiusMeters,
          })

        const wolfB =
          tracker.observe({
            actorKey:
              'animal:wolf-b',
            coordinate:
              north,
            planetRadiusMeters:
              earthRadiusMeters,
          })

        expect(
          wolfA.headingRadians,
        ).toBeCloseTo(
          0,
          6,
        )

        expect(
          wolfB.headingRadians,
        ).toBeCloseTo(
          Math.PI / 2,
          6,
        )
      },
    )

    it(
      'forgets motion history when an actor leaves local presentation',
      () => {
        const tracker =
          new LocalActorMotionTracker()

        tracker.observe({
          actorKey:
            'animal:wolf-a',
          coordinate:
            origin,
          planetRadiusMeters:
            earthRadiusMeters,
        })

        const east =
          moveSurfaceCoordinate(
            origin,
            0,
            4,
            earthRadiusMeters,
          )

        tracker.observe({
          actorKey:
            'animal:wolf-a',
          coordinate:
            east,
          planetRadiusMeters:
            earthRadiusMeters,
        })

        tracker.forget(
          'animal:wolf-a',
        )

        const observation =
          tracker.observe({
            actorKey:
              'animal:wolf-a',
            coordinate:
              east,
            planetRadiusMeters:
              earthRadiusMeters,
          })

        expect(
          observation.headingRadians,
        ).toBeNull()

        expect(
          observation.isMoving,
        ).toBe(
          false,
        )
      },
    )
  },
)
