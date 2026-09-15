import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  createPlanetaryLightingState,
  defaultPlanetaryLighting,
  evaluatePlanetaryIllumination,
  planetaryLightRayDirection,
} from './planetary-lighting'

describe(
  'planetary lighting',
  () => {
    it(
      'normalizes the renderer-owned direction toward the light',
      () => {
        const lighting =
          createPlanetaryLightingState(
            {
              x: 4,
              y: 0,
              z: 3,
            },
          )

        expect(
          Math.hypot(
            lighting
              .surfaceToLightDirection.x,
            lighting
              .surfaceToLightDirection.y,
            lighting
              .surfaceToLightDirection.z,
          ),
        ).toBeCloseTo(1)

        expect(
          lighting
            .surfaceToLightDirection.x,
        ).toBeCloseTo(0.8)

        expect(
          lighting
            .surfaceToLightDirection.z,
        ).toBeCloseTo(0.6)
      },
    )

    it(
      'derives Babylon light-ray direction by inversion',
      () => {
        const ray =
          planetaryLightRayDirection(
            defaultPlanetaryLighting,
          )

        expect(ray.x).toBeCloseTo(
          -defaultPlanetaryLighting
            .surfaceToLightDirection.x,
        )

        expect(ray.y).toBeCloseTo(
          -defaultPlanetaryLighting
            .surfaceToLightDirection.y,
        )

        expect(ray.z).toBeCloseTo(
          -defaultPlanetaryLighting
            .surfaceToLightDirection.z,
        )
      },
    )

    it(
      'produces a directional day side and ambient night side',
      () => {
        const lighting =
          createPlanetaryLightingState(
            {
              x: 1,
              y: 0,
              z: 0,
            },
            0.25,
            0.75,
          )

        expect(
          evaluatePlanetaryIllumination(
            {
              x: 1,
              y: 0,
              z: 0,
            },
            lighting,
          ),
        ).toBeCloseTo(1)

        expect(
          evaluatePlanetaryIllumination(
            {
              x: -1,
              y: 0,
              z: 0,
            },
            lighting,
          ),
        ).toBeCloseTo(0.25)

        expect(
          evaluatePlanetaryIllumination(
            {
              x: 0,
              y: 1,
              z: 0,
            },
            lighting,
          ),
        ).toBeCloseTo(0.25)
      },
    )

    it(
      'rejects invalid lighting state',
      () => {
        expect(
          () =>
            createPlanetaryLightingState(
              {
                x: 0,
                y: 0,
                z: 0,
              },
            ),
        ).toThrow()

        expect(
          () =>
            createPlanetaryLightingState(
              {
                x: 1,
                y: 0,
                z: 0,
              },
              -1,
              1,
            ),
        ).toThrow()
      },
    )
  },
)
