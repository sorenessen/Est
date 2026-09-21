import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  createTerrainSurfaceScatter,
} from './terrain-surface-scatter'

const earthRadiusMeters =
  6_371_000

const identity =
  'test-planet'

const toKey = (
  sample: {
    kind: string
    latitudeDegrees: number
    longitudeDegrees: number
  },
) =>
  [
    sample.kind,
    sample.latitudeDegrees
      .toFixed(
        8,
      ),
    sample.longitudeDegrees
      .toFixed(
        8,
      ),
  ].join(
    ':',
  )

describe(
  'createTerrainSurfaceScatter',
  () => {
    it('is deterministic', () => {
      const first =
        createTerrainSurfaceScatter(
          47.0379,
          -122.9007,
          earthRadiusMeters,
          identity,
          40,
        )

      const second =
        createTerrainSurfaceScatter(
          47.0379,
          -122.9007,
          earthRadiusMeters,
          identity,
          40,
        )

      expect(second).toEqual(first)
    })

    it('keeps overlapping scatter fixed when the center moves', () => {
      const latitudeOffsetDegrees =
        (
          12 /
          earthRadiusMeters
        ) *
        180 /
        Math.PI

      const first =
        createTerrainSurfaceScatter(
          47.0379,
          -122.9007,
          earthRadiusMeters,
          identity,
          40,
        )

      const second =
        createTerrainSurfaceScatter(
          47.0379 +
            latitudeOffsetDegrees,
          -122.9007,
          earthRadiusMeters,
          identity,
          40,
        )

      const firstKeys =
        new Set(
          first.map(
            toKey,
          ),
        )

      const shared =
        second.filter(
          sample =>
            firstKeys.has(
              toKey(
                sample,
              ),
            ),
        )

      expect(
        shared.length,
      ).toBeGreaterThan(
        300,
      )
    })

    it('produces grass and sparse stones', () => {
      const samples =
        createTerrainSurfaceScatter(
          47.0379,
          -122.9007,
          earthRadiusMeters,
          identity,
          50,
        )

      expect(
        samples.some(
          sample =>
            sample.kind ===
            'grass',
        ),
      ).toBe(true)

      expect(
        samples.some(
          sample =>
            sample.kind ===
            'stone',
        ),
      ).toBe(true)
    })

    it('changes with planet identity', () => {
      const first =
        createTerrainSurfaceScatter(
          47.0379,
          -122.9007,
          earthRadiusMeters,
          'planet-a',
          30,
        )

      const second =
        createTerrainSurfaceScatter(
          47.0379,
          -122.9007,
          earthRadiusMeters,
          'planet-b',
          30,
        )

      expect(second).not.toEqual(first)
    })

    it('rejects invalid inputs', () => {
      expect(
        () =>
          createTerrainSurfaceScatter(
            100,
            0,
            earthRadiusMeters,
            identity,
            30,
          ),
      ).toThrow()

      expect(
        () =>
          createTerrainSurfaceScatter(
            0,
            0,
            -1,
            identity,
            30,
          ),
      ).toThrow()

      expect(
        () =>
          createTerrainSurfaceScatter(
            0,
            0,
            earthRadiusMeters,
            '',
            30,
          ),
      ).toThrow()
    })
  },
)
