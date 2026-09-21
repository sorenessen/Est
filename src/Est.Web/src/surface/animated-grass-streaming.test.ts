import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  animatedGrassFragmentShader,
  animatedGrassVertexShader,
} from './animated-grass-material'

describe(
  'animated grass streaming',
  () => {
    it('uses player-relative distance fading instead of a hard cell edge', () => {
      expect(
        animatedGrassVertexShader,
      ).toContain(
        'uFadeStartMeters',
      )

      expect(
        animatedGrassVertexShader,
      ).toContain(
        'uFadeEndMeters',
      )

      expect(
        animatedGrassVertexShader,
      ).toContain(
        'grassRootWorldPosition',
      )

      expect(
        animatedGrassVertexShader,
      ).toContain(
        'grassDistanceMeters',
      )

      expect(
        animatedGrassVertexShader,
      ).toContain(
        'distanceVisibility',
      )
    })

    it('reveals individual blades progressively', () => {
      expect(
        animatedGrassVertexShader,
      ).toContain(
        'revealThreshold',
      )

      expect(
        animatedGrassVertexShader,
      ).toContain(
        'grassReveal',
      )

      expect(
        animatedGrassVertexShader,
      ).toContain(
        'positionUpdated.y *=',
      )

      expect(
        animatedGrassFragmentShader,
      ).toContain(
        'vGrassReveal',
      )

      expect(
        animatedGrassFragmentShader,
      ).toContain(
        'discard',
      )
    })

    it('breaks up the visible meadow edge by geography and blade stratum', () => {
      expect(
        animatedGrassVertexShader,
      ).toContain(
        'edgeNoise',
      )

      expect(
        animatedGrassVertexShader,
      ).toContain(
        'basalExtensionMeters',
      )

      expect(
        animatedGrassVertexShader,
      ).toContain(
        'noisyFadeStartMeters',
      )

      expect(
        animatedGrassVertexShader,
      ).toContain(
        'noisyFadeEndMeters',
      )
    })
    it('declares geographic wind position before edge-noise sampling', () => {
      const planetPositionIndex =
        animatedGrassVertexShader.indexOf(
          'vec3 planetPositionMeters',
        )

      const edgeNoiseIndex =
        animatedGrassVertexShader.indexOf(
          'float edgeNoise',
        )

      expect(
        planetPositionIndex,
      ).toBeGreaterThanOrEqual(
        0,
      )

      expect(
        edgeNoiseIndex,
      ).toBeGreaterThan(
        planetPositionIndex,
      )
    })
  },
)
