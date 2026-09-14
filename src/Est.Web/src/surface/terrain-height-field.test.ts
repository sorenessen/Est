import { describe, expect, it } from 'vitest'

import type {
  SurfaceCellResponse,
  SurfaceGridResponse,
  SurfaceResponse,
  TerrainResponse,
} from '../api/est-api'
import {
  createTerrainHeightField,
} from './terrain-height-field'

const grid: SurfaceGridResponse = {
  kind: 'RendererNeutralTestGrid',
  identityVersion: 1,
  latitudeBandCount: 1,
  longitudeBandCount: 4,
}

function surfaceCell(
  cellId: string,
  latitudeDegrees: number,
  longitudeDegrees: number,
): SurfaceCellResponse {
  return {
    cellId,
    centerLatitudeDegrees:
      latitudeDegrees,
    centerLongitudeDegrees:
      longitudeDegrees,
    areaSquareMeters: 1,
    boundary: [],
  }
}

function createResponses(
  points: Array<{
    id: string
    latitudeDegrees: number
    longitudeDegrees: number
    elevationMeters: number
  }>,
): {
  surface: SurfaceResponse
  terrain: TerrainResponse
} {
  return {
    surface: {
      planetId: 'planet',
      grid,
      cells:
        points.map(
          point =>
            surfaceCell(
              point.id,
              point.latitudeDegrees,
              point.longitudeDegrees,
            ),
        ),
    },
    terrain: {
      planetId: 'planet',
      grid,
      cells:
        points.map(
          point => ({
            cellId:
              point.id,
            elevationMeters:
              point.elevationMeters,
          }),
        ),
    },
  }
}

describe('createTerrainHeightField', () => {
  it('preserves the authoritative elevation at a control point', () => {
    const {
      surface,
      terrain,
    } =
      createResponses([
        {
          id: 'west',
          latitudeDegrees: 0,
          longitudeDegrees: -10,
          elevationMeters: 100,
        },
        {
          id: 'east',
          latitudeDegrees: 0,
          longitudeDegrees: 10,
          elevationMeters: 300,
        },
      ])

    const field =
      createTerrainHeightField(
        surface,
        terrain,
      )

    expect(
      field.sampleHeightMeters(
        0,
        -10,
      ),
    ).toBe(100)

    expect(
      field.sampleHeightMeters(
        0,
        10,
      ),
    ).toBe(300)
  })

  it('smoothly interpolates between nearby authoritative elevations', () => {
    const {
      surface,
      terrain,
    } =
      createResponses([
        {
          id: 'west',
          latitudeDegrees: 0,
          longitudeDegrees: -10,
          elevationMeters: 100,
        },
        {
          id: 'east',
          latitudeDegrees: 0,
          longitudeDegrees: 10,
          elevationMeters: 300,
        },
      ])

    const field =
      createTerrainHeightField(
        surface,
        terrain,
      )

    expect(
      field.sampleHeightMeters(
        0,
        0,
      ),
    ).toBeCloseTo(
      200,
      8,
    )
  })

  it('interpolates continuously across the antimeridian', () => {
    const {
      surface,
      terrain,
    } =
      createResponses([
        {
          id: 'west-dateline',
          latitudeDegrees: 0,
          longitudeDegrees: 179,
          elevationMeters: 100,
        },
        {
          id: 'east-dateline',
          latitudeDegrees: 0,
          longitudeDegrees: -179,
          elevationMeters: 300,
        },
      ])

    const field =
      createTerrainHeightField(
        surface,
        terrain,
      )

    expect(
      field.sampleHeightMeters(
        0,
        180,
      ),
    ).toBeCloseTo(
      200,
      8,
    )

    expect(
      field.sampleHeightMeters(
        0,
        -180,
      ),
    ).toBeCloseTo(
      200,
      8,
    )
  })

  it('does not depend on row or column identity', () => {
    const {
      surface,
      terrain,
    } =
      createResponses([
        {
          id: 'only',
          latitudeDegrees: 45,
          longitudeDegrees: 20,
          elevationMeters: 1_234,
        },
      ])

    const field =
      createTerrainHeightField(
        surface,
        terrain,
      )

    expect(
      field.sampleHeightMeters(
        -30,
        -120,
      ),
    ).toBe(1_234)
  })

  it('rejects mismatched surface and terrain grids', () => {
    const {
      surface,
      terrain,
    } =
      createResponses([
        {
          id: 'a',
          latitudeDegrees: 0,
          longitudeDegrees: 0,
          elevationMeters: 0,
        },
      ])

    expect(() =>
      createTerrainHeightField(
        surface,
        {
          ...terrain,
          grid: {
            ...terrain.grid,
            identityVersion: 2,
          },
        },
      ),
    ).toThrow(
      'grid definitions',
    )
  })

  it('rejects terrain cells missing from the shared surface', () => {
    const {
      surface,
      terrain,
    } =
      createResponses([
        {
          id: 'a',
          latitudeDegrees: 0,
          longitudeDegrees: 0,
          elevationMeters: 0,
        },
      ])

    expect(() =>
      createTerrainHeightField(
        surface,
        {
          ...terrain,
          cells: [
            {
              cellId: 'other',
              elevationMeters: 0,
            },
          ],
        },
      ),
    ).toThrow(
      'has no terrain state',
    )
  })

  it('rejects invalid sample coordinates', () => {
    const {
      surface,
      terrain,
    } =
      createResponses([
        {
          id: 'a',
          latitudeDegrees: 0,
          longitudeDegrees: 0,
          elevationMeters: 0,
        },
      ])

    const field =
      createTerrainHeightField(
        surface,
        terrain,
      )

    expect(() =>
      field.sampleHeightMeters(
        91,
        0,
      ),
    ).toThrow(
      'latitude',
    )

    expect(() =>
      field.sampleHeightMeters(
        0,
        Number.NaN,
      ),
    ).toThrow(
      'longitude',
    )
  })
})
