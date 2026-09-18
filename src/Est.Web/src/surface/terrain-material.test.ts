import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  terrainFragmentShader,
  terrainVertexShader,
  validateTerrainMaterialOptions,
} from './terrain-material'

describe(
  'terrain shader material',
  () => {
    it(
      'accepts a finite physical planet and ordered terrain range',
      () => {
        expect(
          () =>
            validateTerrainMaterialOptions(
              {
                meanRadiusMeters:
                  6_371_000,
                minimumElevationMeters:
                  -6690,
                maximumElevationMeters:
                  4731,
              },
            ),
        ).not.toThrow()
      },
    )

    it(
      'rejects invalid physical terrain parameters',
      () => {
        expect(
          () =>
            validateTerrainMaterialOptions(
              {
                meanRadiusMeters: 0,
                minimumElevationMeters:
                  -1,
                maximumElevationMeters:
                  1,
              },
            ),
        ).toThrow()

        expect(
          () =>
            validateTerrainMaterialOptions(
              {
                meanRadiusMeters: 1,
                minimumElevationMeters:
                  10,
                maximumElevationMeters:
                  -10,
              },
            ),
        ).toThrow()
      },
    )

    it(
      'uses seamless planet-space position and terrain normals rather than UV mapping',
      () => {
        expect(
          terrainVertexShader,
        ).toContain(
          'vPlanetPosition = position',
        )

        expect(
          terrainFragmentShader,
        ).toContain(
          'normalize(',
        )

        expect(
          terrainFragmentShader,
        ).toContain(
          'vec3 warpedDirection',
        )

        expect(
          terrainFragmentShader,
        ).toContain(
          'ridges *= ridges',
        )

        expect(
          terrainFragmentShader,
        ).toContain(
          'direction * 960.0',
        )

        expect(
          terrainFragmentShader,
        ).toContain(
          'vTerrainNormal',
        )

        expect(
          terrainFragmentShader,
        ).toContain(
          'vegetationCoverage',
        )


        expect(
          terrainVertexShader,
        ).toContain(
          'attribute float vegetationCoverage',
        )

        expect(
          terrainFragmentShader,
        ).toContain(
          'vVegetationCoverage',
        )

        expect(
          terrainFragmentShader,
        ).not.toContain(
          'uv',
        )
      },
    )
  },
)
