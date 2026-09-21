import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  resolveWolfGaitFrame,
  resolveWolfPresentationAnimation,
  wolfPresentationAnimationNames,
  wolfPresentationAssetPath,
} from './wolf-presentation'


describe(
  'wolf presentation asset policy',
  () => {
    it(
      'uses the Est-hosted Quaternius wolf asset',
      () => {
        expect(
          wolfPresentationAssetPath,
        ).toBe(
          '/assets/animals/quaternius/ultimate-animated-animals/Wolf.gltf',
        )
      },
    )

    it.each([
      'idle',
      'hunting',
      'traveling',
      'attacking',
      'eating',
    ] as const)(
      'uses the in-place Walk clip while %s activity is moving',
      activity => {
        expect(
          resolveWolfPresentationAnimation({
            activity,
            locomotion:
              'moving',
          }),
        ).toEqual({
          name:
            wolfPresentationAnimationNames.walk,
          playback:
            'gait-phase',
        })
      },
    )

    it.each([
      [
        'idle',
        wolfPresentationAnimationNames.idle,
      ],
      [
        'traveling',
        wolfPresentationAnimationNames.idle,
      ],
      [
        'hunting',
        wolfPresentationAnimationNames
          .huntingIdle,
      ],
      [
        'attacking',
        wolfPresentationAnimationNames.attack,
      ],
      [
        'eating',
        wolfPresentationAnimationNames.eating,
      ],
    ] as const)(
      'maps stationary %s activity to %s',
      (
        activity,
        expectedName,
      ) => {
        expect(
          resolveWolfPresentationAnimation({
            activity,
            locomotion:
              'stationary',
          }),
        ).toEqual({
          name:
            expectedName,
          playback:
            'loop',
        })
      },
    )

    it(
      'maps gait phase deterministically across the clip frame range',
      () => {
        expect(
          resolveWolfGaitFrame(
            0,
            10,
            40,
          ),
        ).toBe(
          10,
        )

        expect(
          resolveWolfGaitFrame(
            Math.PI,
            10,
            40,
          ),
        ).toBe(
          25,
        )

        expect(
          resolveWolfGaitFrame(
            Math.PI *
              2,
            10,
            40,
          ),
        ).toBe(
          10,
        )
      },
    )

    it(
      'wraps negative gait phase without changing the cycle',
      () => {
        expect(
          resolveWolfGaitFrame(
            -Math.PI,
            10,
            40,
          ),
        ).toBe(
          25,
        )
      },
    )

    it(
      'rejects invalid gait phase and frame ranges',
      () => {
        expect(
          () =>
            resolveWolfGaitFrame(
              Number.NaN,
              0,
              10,
            ),
        ).toThrow(
          'Wolf gait phase must be finite.',
        )

        expect(
          () =>
            resolveWolfGaitFrame(
              0,
              10,
              5,
            ),
        ).toThrow(
          'Wolf animation frame range must be finite and ordered.',
        )
      },
    )
  },
)
