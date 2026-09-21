import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  humanPresentationAnimationAssetPath,
  humanPresentationAssetPaths,
  humanPresentationIdleAnimationName,
  resolveHumanBodyVariant,
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
  },
)
