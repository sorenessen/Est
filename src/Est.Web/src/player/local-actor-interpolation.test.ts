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
