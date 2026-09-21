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

        expect(
          tracker.observe({
            actorKey:
              'animal:wolf-a',
            coordinate:
              origin,
            planetRadiusMeters:
              earthRadiusMeters,
            snapshotTimeSeconds:
              0,
          }),
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
          snapshotTimeSeconds:
            0,
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
            snapshotTimeSeconds:
              1,
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
          snapshotTimeSeconds:
            0,
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
            snapshotTimeSeconds:
              1,
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
      'preserves one movement observation while the same authoritative snapshot is reprojected',
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
          snapshotTimeSeconds:
            0,
        })

        const east =
          moveSurfaceCoordinate(
            origin,
            0,
            4,
            earthRadiusMeters,
          )

        const moved =
          tracker.observe({
            actorKey:
              'animal:wolf-a',
            coordinate:
              east,
            planetRadiusMeters:
              earthRadiusMeters,
            snapshotTimeSeconds:
              1,
          })

        const reprojected =
          tracker.observe({
            actorKey:
              'animal:wolf-a',
            coordinate:
              east,
            planetRadiusMeters:
              earthRadiusMeters,
            snapshotTimeSeconds:
              1,
          })

        expect(
          reprojected,
        ).toEqual(
          moved,
        )

        expect(
          reprojected.isMoving,
        ).toBe(
          true,
        )
      },
    )

    it(
      'preserves the last valid heading on a later stationary authoritative snapshot',
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
          snapshotTimeSeconds:
            0,
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
          snapshotTimeSeconds:
            1,
        })

        const stationary =
          tracker.observe({
            actorKey:
              'animal:wolf-a',
            coordinate:
              north,
            planetRadiusMeters:
              earthRadiusMeters,
            snapshotTimeSeconds:
              2,
          })

        expect(
          stationary.isMoving,
        ).toBe(
          false,
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

        for (
          const actorKey of [
            'animal:wolf-a',
            'animal:wolf-b',
          ]
        ) {
          tracker.observe({
            actorKey,
            coordinate:
              origin,
            planetRadiusMeters:
              earthRadiusMeters,
            snapshotTimeSeconds:
              0,
          })
        }

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
            snapshotTimeSeconds:
              1,
          })

        const wolfB =
          tracker.observe({
            actorKey:
              'animal:wolf-b',
            coordinate:
              north,
            planetRadiusMeters:
              earthRadiusMeters,
            snapshotTimeSeconds:
              1,
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
          snapshotTimeSeconds:
            0,
        })

        tracker.forget(
          'animal:wolf-a',
        )

        const observation =
          tracker.observe({
            actorKey:
              'animal:wolf-a',
            coordinate:
              origin,
            planetRadiusMeters:
              earthRadiusMeters,
            snapshotTimeSeconds:
              1,
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

    it(
      'rejects an authoritative snapshot that moves backward in simulation time',
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
          snapshotTimeSeconds:
            5,
        })

        expect(
          () =>
            tracker.observe({
              actorKey:
                'animal:wolf-a',
              coordinate:
                origin,
              planetRadiusMeters:
                earthRadiusMeters,
              snapshotTimeSeconds:
                4,
            }),
        ).toThrow(
          'Local actor motion snapshots must be observed in non-decreasing time order.',
        )
      },
    )
  },
)
