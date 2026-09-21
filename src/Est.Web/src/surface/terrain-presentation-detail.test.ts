import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  sampleTerrainPresentationDetailMeters,
  sampleTerrainSurfaceAppearance,
} from './terrain-presentation-detail'

const earthRadiusMeters =
  6_371_000

const planetIdentity =
  'test-planet'

describe(
  'sampleTerrainPresentationDetailMeters',
  () => {
    it('is deterministic for the same planet and coordinate', () => {
      const first =
        sampleTerrainPresentationDetailMeters(
          47.0379,
          -122.9007,
          earthRadiusMeters,
          planetIdentity,
        )

      const second =
        sampleTerrainPresentationDetailMeters(
          47.0379,
          -122.9007,
          earthRadiusMeters,
          planetIdentity,
        )

      expect(second).toBe(first)
    })

    it('uses planet identity to distinguish presentation terrain', () => {
      const first =
        sampleTerrainPresentationDetailMeters(
          47.0379,
          -122.9007,
          earthRadiusMeters,
          'planet-a',
        )

      const second =
        sampleTerrainPresentationDetailMeters(
          47.0379,
          -122.9007,
          earthRadiusMeters,
          'planet-b',
        )

      expect(second).not.toBe(first)
    })

    it('is continuous across the antimeridian', () => {
      const west =
        sampleTerrainPresentationDetailMeters(
          12.5,
          180,
          earthRadiusMeters,
          planetIdentity,
        )

      const east =
        sampleTerrainPresentationDetailMeters(
          12.5,
          -180,
          earthRadiusMeters,
          planetIdentity,
        )

      expect(east).toBeCloseTo(
        west,
        8,
      )
    })

    it('changes continuously over walking-scale distances', () => {
      const origin =
        sampleTerrainPresentationDetailMeters(
          47,
          -122,
          earthRadiusMeters,
          planetIdentity,
        )

      const oneMeterNorth =
        sampleTerrainPresentationDetailMeters(
          47 +
            (
              1 /
              earthRadiusMeters
            ) *
              180 /
              Math.PI,
          -122,
          earthRadiusMeters,
          planetIdentity,
        )

      expect(
        Math.abs(
          oneMeterNorth -
          origin,
        ),
      ).toBeLessThan(1)
    })

    it('keeps residual detail subordinate to regional terrain', () => {
      for (
        const coordinate of [
          [0, 0],
          [20, 45],
          [-35, 120],
          [68, -30],
        ] as const
      ) {
        const detail =
          sampleTerrainPresentationDetailMeters(
            coordinate[0],
            coordinate[1],
            earthRadiusMeters,
            planetIdentity,
          )

        expect(
          Math.abs(detail),
        ).toBeLessThanOrEqual(
          22.85,
        )
      }
    })

    it('rejects an invalid planet radius', () => {
      expect(() =>
        sampleTerrainPresentationDetailMeters(
          0,
          0,
          0,
          planetIdentity,
        ),
      ).toThrow(
        'radius',
      )
    })

    it('rejects an empty planet identity', () => {
      expect(() =>
        sampleTerrainPresentationDetailMeters(
          0,
          0,
          earthRadiusMeters,
          '',
        ),
      ).toThrow(
        'identity',
      )
    })
  },
)


describe(
  'sampleTerrainSurfaceAppearance',
  () => {
    it('is deterministic', () => {
      const first =
        sampleTerrainSurfaceAppearance(
          47.0379,
          -122.9007,
          earthRadiusMeters,
          planetIdentity,
        )

      const second =
        sampleTerrainSurfaceAppearance(
          47.0379,
          -122.9007,
          earthRadiusMeters,
          planetIdentity,
        )

      expect(second).toEqual(first)
    })

    it('is continuous across the antimeridian', () => {
      const west =
        sampleTerrainSurfaceAppearance(
          12.5,
          180,
          earthRadiusMeters,
          planetIdentity,
        )

      const east =
        sampleTerrainSurfaceAppearance(
          12.5,
          -180,
          earthRadiusMeters,
          planetIdentity,
        )

      expect(
        east.albedoVariation,
      ).toBeCloseTo(
        west.albedoVariation,
        8,
      )

      expect(
        east.microHeightMeters,
      ).toBeCloseTo(
        west.microHeightMeters,
        8,
      )
    })

    it('keeps the micro-height residual bounded', () => {
      for (
        const coordinate of [
          [0, 0],
          [20, 45],
          [-35, 120],
          [68, -30],
        ] as const
      ) {
        const sample =
          sampleTerrainSurfaceAppearance(
            coordinate[0],
            coordinate[1],
            earthRadiusMeters,
            planetIdentity,
          )

        expect(
          Math.abs(
            sample.microHeightMeters,
          ),
        ).toBeLessThanOrEqual(
          0.475,
        )
      }
    })

    it('varies across walking-scale distance', () => {
      const first =
        sampleTerrainSurfaceAppearance(
          47,
          -122,
          earthRadiusMeters,
          planetIdentity,
        )

      const second =
        sampleTerrainSurfaceAppearance(
          47 +
            (
              3 /
              earthRadiusMeters
            ) *
              180 /
              Math.PI,
          -122,
          earthRadiusMeters,
          planetIdentity,
        )

      expect(second).not.toEqual(first)
    })
  },
)
