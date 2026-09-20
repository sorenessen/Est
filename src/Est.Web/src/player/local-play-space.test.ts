import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  geographicToLocalMeters,
} from './local-play-space'

import {
  moveSurfaceCoordinate,
} from './surface-movement'

const earthRadiusMeters =
  6_371_000

describe(
  'geographicToLocalMeters',
  () => {
    it(
      'maps the play-space origin to zero meters',
      () => {
        const origin = {
          latitudeDegrees: 47,
          longitudeDegrees: -122,
        }

        expect(
          geographicToLocalMeters(
            origin,
            origin,
            earthRadiusMeters,
          ),
        ).toEqual({
          eastMeters: 0,
          northMeters: 0,
        })
      },
    )

    it(
      'preserves a nearby authoritative eastward displacement in meters',
      () => {
        const origin = {
          latitudeDegrees: 47,
          longitudeDegrees: -122,
        }

        const moved =
          moveSurfaceCoordinate(
            origin,
            0,
            12,
            earthRadiusMeters,
          )

        const local =
          geographicToLocalMeters(
            moved,
            origin,
            earthRadiusMeters,
          )

        expect(
          local.eastMeters,
        ).toBeCloseTo(
          12,
          3,
        )

        expect(
          local.northMeters,
        ).toBeCloseTo(
          0,
          3,
        )
      },
    )

    it(
      'preserves a nearby authoritative northward displacement in meters',
      () => {
        const origin = {
          latitudeDegrees: 47,
          longitudeDegrees: -122,
        }

        const moved =
          moveSurfaceCoordinate(
            origin,
            12,
            0,
            earthRadiusMeters,
          )

        const local =
          geographicToLocalMeters(
            moved,
            origin,
            earthRadiusMeters,
          )

        expect(
          local.northMeters,
        ).toBeCloseTo(
          12,
          3,
        )

        expect(
          local.eastMeters,
        ).toBeCloseTo(
          0,
          3,
        )
      },
    )

    it(
      'uses the short longitude path across the dateline',
      () => {
        const origin = {
          latitudeDegrees: 0,
          longitudeDegrees: 179.99995,
        }

        const moved =
          moveSurfaceCoordinate(
            origin,
            0,
            12,
            earthRadiusMeters,
          )

        const local =
          geographicToLocalMeters(
            moved,
            origin,
            earthRadiusMeters,
          )

        expect(
          local.eastMeters,
        ).toBeCloseTo(
          12,
          3,
        )
      },
    )

    it(
      'rejects an invalid planetary radius',
      () => {
        expect(
          () =>
            geographicToLocalMeters(
              {
                latitudeDegrees: 0,
                longitudeDegrees: 0,
              },
              {
                latitudeDegrees: 0,
                longitudeDegrees: 0,
              },
              0,
            ),
        ).toThrow(
          'Planet radius must be a positive finite number.',
        )
      },
    )
  },
)
