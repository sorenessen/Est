import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  humanPresentationAnimationAssetPath,
  humanPresentationAssetPaths,
  humanPresentationIdleAnimationName,
  humanPresentationWalkAnimationName,
  resolveHumanBodyVariant,
  resolveHumanGaitFrame,
  resolveHumanPresentationRotationY,
} from './human-presentation'

describe(
  'human presentation',
  () => {
    it(
      'maps authoritative female sex to the female presentation body',
      () => {
        expect(
          resolveHumanBodyVariant(
            'Female',
          ),
        ).toBe(
          'female',
        )
      },
    )

    it(
      'maps authoritative male sex to the male presentation body',
      () => {
        expect(
          resolveHumanBodyVariant(
            'Male',
          ),
        ).toBe(
          'male',
        )
      },
    )

    it(
      'does not invent a body mapping for an unknown authoritative value',
      () => {
        expect(
          resolveHumanBodyVariant(
            'Unknown',
          ),
        ).toBeNull()
      },
    )

    it(
      'uses Est-hosted glTF presentation assets',
      () => {
        expect(
          humanPresentationAssetPaths
            .female,
        ).toMatch(
          /^\/assets\/characters\/.+\.gltf$/,
        )

        expect(
          humanPresentationAssetPaths
            .male,
        ).toMatch(
          /^\/assets\/characters\/.+\.gltf$/,
        )
      },
    )

    it(
      'uses the non-root-motion animation library',
      () => {
        expect(
          humanPresentationAnimationAssetPath,
        ).toBe(
          '/assets/characters/quaternius/universal-animation-library/UAL1_Standard.glb',
        )

        expect(
          humanPresentationAnimationAssetPath,
        ).not.toContain(
          '_RM',
        )
      },
    )

    it(
      'uses the neutral standing idle for the initial human presentation',
      () => {
        expect(
          humanPresentationIdleAnimationName,
        ).toBe(
          'Idle_Loop',
        )
      },
    )

    it(
      'uses the in-place walk animation for observed locomotion',
      () => {
        expect(
          humanPresentationWalkAnimationName,
        ).toBe(
          'Walk_Loop',
        )
      },
    )

    it(
      'maps Est heading onto Babylon Y rotation',
      () => {
        expect(
          resolveHumanPresentationRotationY(
            0,
          ),
        ).toBeCloseTo(
          0,
        )

        expect(
          resolveHumanPresentationRotationY(
            Math.PI / 2,
          ),
        ).toBeCloseTo(
          -Math.PI / 2,
        )

        expect(
          () =>
            resolveHumanPresentationRotationY(
              Number.NaN,
            ),
        ).toThrow(
          'Human presentation heading must be finite.',
        )
      },
    )

    it(
      'maps periodic gait phase onto the walk clip frame range',
      () => {
        expect(
          resolveHumanGaitFrame(
            0,
            10,
            50,
          ),
        ).toBe(
          10,
        )

        expect(
          resolveHumanGaitFrame(
            Math.PI,
            10,
            50,
          ),
        ).toBe(
          30,
        )

        expect(
          resolveHumanGaitFrame(
            Math.PI * 2,
            10,
            50,
          ),
        ).toBe(
          10,
        )
      },
    )
  },
)
