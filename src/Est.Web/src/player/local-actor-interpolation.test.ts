import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  createLocalActorPresentationTransition,
  sampleLocalActorPresentationTransition,
} from './local-actor-interpolation'


describe(
  'local actor presentation interpolation',
  () => {
    const start = {
      eastMeters: 1,
      verticalMeters: 2,
      northMeters: 3,
    }

    const target = {
      eastMeters: 5,
      verticalMeters: 6,
      northMeters: 11,
    }

    it(
      'begins exactly at the previous presentation position',
      () => {
        const transition =
          createLocalActorPresentationTransition(
            start,
            target,
            100,
            400,
          )

        expect(
          sampleLocalActorPresentationTransition(
            transition,
            100,
          ),
        ).toEqual({
          position: start,
          progress: 0,
          isComplete: false,
        })
      },
    )

    it(
      'interpolates between known presentation endpoints',
      () => {
        const transition =
          createLocalActorPresentationTransition(
            start,
            target,
            100,
            400,
          )

        expect(
          sampleLocalActorPresentationTransition(
            transition,
            300,
          ),
        ).toEqual({
          position: {
            eastMeters: 3,
            verticalMeters: 4,
            northMeters: 7,
          },
          progress: 0.5,
          isComplete: false,
        })
      },
    )

    it(
      'lands exactly on the authoritative presentation target',
      () => {
        const transition =
          createLocalActorPresentationTransition(
            start,
            target,
            100,
            400,
          )

        expect(
          sampleLocalActorPresentationTransition(
            transition,
            500,
          ),
        ).toEqual({
          position: target,
          progress: 1,
          isComplete: true,
        })
      },
    )

    it(
      'clamps late samples to the authoritative target',
      () => {
        const transition =
          createLocalActorPresentationTransition(
            start,
            target,
            100,
            400,
          )

        expect(
          sampleLocalActorPresentationTransition(
            transition,
            5_000,
          ).position,
        ).toEqual(
          target,
        )
      },
    )

    it(
      'maintains authoritative velocity while correcting a moving prediction',
      () => {
        const transition =
          createLocalActorPresentationTransition(
            start,
            target,
            100,
            400,
          )

        const continuation = {
          velocity: {
            eastMetersPerSecond:
              2,
            verticalMetersPerSecond:
              -1,
            northMetersPerSecond:
              4,
          },
          maintainVelocityDuringTransition:
            true,
        }

        expect(
          sampleLocalActorPresentationTransition(
            transition,
            300,
            continuation,
          ),
        ).toEqual({
          position: {
            eastMeters:
              3.4,
            verticalMeters:
              3.8,
            northMeters:
              7.8,
          },
          progress:
            0.5,
          isComplete:
            false,
        })

        expect(
          sampleLocalActorPresentationTransition(
            transition,
            1_000,
            continuation,
          ),
        ).toEqual({
          position: {
            eastMeters:
              6.8,
            verticalMeters:
              5.1,
            northMeters:
              14.6,
          },
          progress:
            1,
          isComplete:
            false,
        })
      },
    )

    it(
      'continues at authoritative velocity until a newer snapshot replaces it',
      () => {
        const transition =
          createLocalActorPresentationTransition(
            start,
            target,
            100,
            400,
          )

        const continuation = {
          velocity: {
            eastMetersPerSecond:
              2,
            verticalMetersPerSecond:
              -1,
            northMetersPerSecond:
              4,
          },
        }

        expect(
          sampleLocalActorPresentationTransition(
            transition,
            1_000,
            continuation,
          ),
        ).toEqual({
          position: {
            eastMeters:
              6,
            verticalMeters:
              5.5,
            northMeters:
              13,
          },
          progress:
            1,
          isComplete:
            false,
        })

        expect(
          sampleLocalActorPresentationTransition(
            transition,
            5_000,
            continuation,
          ),
        ).toEqual({
          position: {
            eastMeters:
              14,
            verticalMeters:
              1.5,
            northMeters:
              29,
          },
          progress:
            1,
          isComplete:
            false,
        })
      },
    )

    it(
      'rejects non-positive transition duration',
      () => {
        expect(
          () =>
            createLocalActorPresentationTransition(
              start,
              target,
              0,
              0,
            ),
        ).toThrow(
          'Local actor transition duration must be positive and finite.',
        )
      },
    )
  },
)
