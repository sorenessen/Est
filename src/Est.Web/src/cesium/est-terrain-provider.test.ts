import {
  Ellipsoid,
  GeographicTilingScheme,
} from 'cesium'
import {
  describe,
  expect,
  it,
} from 'vitest'

import type {
  SurfaceGridResponse,
  SurfaceResponse,
  TerrainResponse,
} from '../api/est-api'
import type {
  TerrainHeightField,
} from '../surface/terrain-height-field'
import {
  buildEstTerrainHeightmapTile,
  createEstTerrainPresentation,
  estTerrainTileHeight,
  estTerrainTileWidth,
} from './est-terrain-provider'

function coordinateField():
  TerrainHeightField {
  return {
    sampleHeightMeters(
      latitudeDegrees,
      longitudeDegrees,
    ): number {
      return (
        latitudeDegrees *
        1_000 +
        longitudeDegrees
      )
    },
  }
}

function unitSphereTilingScheme():
  GeographicTilingScheme {
  return new GeographicTilingScheme({
    ellipsoid:
      new Ellipsoid(
        1,
        1,
        1,
      ),
    numberOfLevelZeroTilesX: 2,
    numberOfLevelZeroTilesY: 1,
  })
}

describe('buildEstTerrainHeightmapTile', () => {
  it('writes rows north to south and columns west to east', () => {
    const buffer =
      buildEstTerrainHeightmapTile(
        unitSphereTilingScheme(),
        coordinateField(),
        0,
        0,
        0,
        3,
        3,
      )

    expect(
      Array.from(buffer),
    ).toEqual([
      89_820,
      89_910,
      90_000,

      -180,
      -90,
      0,

      -90_180,
      -90_090,
      -90_000,
    ])
  })

  it('shares identical samples across east-west tile edges', () => {
    const tilingScheme =
      unitSphereTilingScheme()

    const left =
      buildEstTerrainHeightmapTile(
        tilingScheme,
        coordinateField(),
        0,
        0,
        1,
        5,
        5,
      )

    const right =
      buildEstTerrainHeightmapTile(
        tilingScheme,
        coordinateField(),
        1,
        0,
        1,
        5,
        5,
      )

    for (
      let row = 0;
      row < 5;
      row += 1
    ) {
      expect(
        left[
          row * 5 +
          4
        ],
      ).toBe(
        right[
          row * 5
        ],
      )
    }
  })

  it('shares identical samples across north-south tile edges', () => {
    const tilingScheme =
      unitSphereTilingScheme()

    const north =
      buildEstTerrainHeightmapTile(
        tilingScheme,
        coordinateField(),
        0,
        0,
        1,
        5,
        5,
      )

    const south =
      buildEstTerrainHeightmapTile(
        tilingScheme,
        coordinateField(),
        0,
        1,
        1,
        5,
        5,
      )

    for (
      let column = 0;
      column < 5;
      column += 1
    ) {
      expect(
        north[
          4 * 5 +
          column
        ],
      ).toBe(
        south[
          column
        ],
      )
    }
  })

  it('rejects invalid tile coordinates and dimensions', () => {
    const tilingScheme =
      unitSphereTilingScheme()

    expect(() =>
      buildEstTerrainHeightmapTile(
        tilingScheme,
        coordinateField(),
        -1,
        0,
        0,
      ),
    ).toThrow(
      'non-negative integers',
    )

    expect(() =>
      buildEstTerrainHeightmapTile(
        tilingScheme,
        coordinateField(),
        2,
        0,
        0,
      ),
    ).toThrow(
      'outside the tiling scheme',
    )

    expect(() =>
      buildEstTerrainHeightmapTile(
        tilingScheme,
        coordinateField(),
        0,
        0,
        0,
        1,
        33,
      ),
    ).toThrow(
      'width',
    )
  })
})

const grid:
  SurfaceGridResponse = {
    kind: 'PresentationTestGrid',
    identityVersion: 1,
    latitudeBandCount: 1,
    longitudeBandCount: 1,
  }

const surface:
  SurfaceResponse = {
    planetId: 'planet',
    grid,
    cells: [
      {
        cellId: 'cell',
        centerLatitudeDegrees: 0,
        centerLongitudeDegrees: 0,
        areaSquareMeters: 1,
        boundary: [],
      },
    ],
  }

const terrain:
  TerrainResponse = {
    planetId: 'planet',
    grid,
    cells: [
      {
        cellId: 'cell',
        elevationMeters: 250,
      },
    ],
  }

describe('createEstTerrainPresentation', () => {
  it('uses the authoritative planet radius for a spherical Cesium ellipsoid', () => {
    const radius =
      4_321_000

    const presentation =
      createEstTerrainPresentation(
        radius,
        surface,
        terrain,
      )

    expect(
      presentation.ellipsoid.radii.x,
    ).toBe(radius)

    expect(
      presentation.ellipsoid.radii.y,
    ).toBe(radius)

    expect(
      presentation.ellipsoid.radii.z,
    ).toBe(radius)

    expect(
      presentation
        .tilingScheme
        .ellipsoid,
    ).toBe(
      presentation.ellipsoid,
    )
  })

  it('creates shared-edge 33 by 33 heightmap tiles', () => {
    const presentation =
      createEstTerrainPresentation(
        6_000_000,
        surface,
        terrain,
      )

    expect(
      presentation
        .terrainProvider
        .width,
    ).toBe(
      estTerrainTileWidth,
    )

    expect(
      presentation
        .terrainProvider
        .height,
    ).toBe(
      estTerrainTileHeight,
    )

    expect(
      estTerrainTileWidth,
    ).toBe(33)

    expect(
      estTerrainTileHeight,
    ).toBe(33)
  })

  it('rejects a non-positive planet radius', () => {
    expect(() =>
      createEstTerrainPresentation(
        0,
        surface,
        terrain,
      ),
    ).toThrow(
      'mean radius',
    )
  })
})
