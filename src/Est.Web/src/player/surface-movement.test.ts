import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  moveSurfaceCoordinate,
} from './surface-movement'

describe(
  'moveSurfaceCoordinate',
  () => {
    it(
      'moves north by physical surface distance',
      () => {
        const moved =
          moveSurfaceCoordinate(
            {
              latitudeDegrees: 0,
              longitudeDegrees: 0,
            },
            111_194.9266,
            0,
            6_371_000,
          )

        expect(
          moved.latitudeDegrees,
        ).toBeCloseTo(
          1,
          4,
        )

        expect(
          moved.longitudeDegrees,
        ).toBeCloseTo(
          0,
          8,
        )
      },
    )

    it(
      'moves east by physical surface distance',
      () => {
        const moved =
          moveSurfaceCoordinate(
            {
              latitudeDegrees: 0,
              longitudeDegrees: 0,
            },
            0,
            111_194.9266,
            6_371_000,
          )

        expect(
          moved.latitudeDegrees,
        ).toBeCloseTo(
          0,
          8,
        )

        expect(
          moved.longitudeDegrees,
        ).toBeCloseTo(
          1,
          4,
        )
      },
    )

    it(
      'wraps longitude across the dateline',
      () => {
        const moved =
          moveSurfaceCoordinate(
            {
              latitudeDegrees: 0,
              longitudeDegrees: 179.9999,
            },
            0,
            100,
            6_371_000,
          )

        expect(
          moved.longitudeDegrees,
        ).toBeLessThan(
          180,
        )

        expect(
          moved.longitudeDegrees,
        ).toBeGreaterThan(
          -180,
        )
      },
    )
  },
)
