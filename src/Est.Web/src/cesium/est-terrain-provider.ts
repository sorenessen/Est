import {
  CustomHeightmapTerrainProvider,
  Ellipsoid,
  GeographicTilingScheme,
  Math as CesiumMath,
} from 'cesium'

import type {
  SurfaceResponse,
  TerrainResponse,
} from '../api/est-api'
import {
  createTerrainHeightField,
  type TerrainHeightField,
} from '../surface/terrain-height-field'

export const estTerrainTileWidth = 33
export const estTerrainTileHeight = 33

export interface EstTerrainPresentation {
  ellipsoid: Ellipsoid
  tilingScheme: GeographicTilingScheme
  terrainProvider: CustomHeightmapTerrainProvider
  heightField: TerrainHeightField
}

function validateTileDimension(
  value: number,
  name: string,
): void {
  if (
    !Number.isInteger(value)
    || value < 2
  ) {
    throw new RangeError(
      `${name} must be an integer greater than or equal to 2.`,
    )
  }
}

export function buildEstTerrainHeightmapTile(
  tilingScheme: GeographicTilingScheme,
  heightField: TerrainHeightField,
  x: number,
  y: number,
  level: number,
  width: number = estTerrainTileWidth,
  height: number = estTerrainTileHeight,
): Float32Array {
  validateTileDimension(
    width,
    'Terrain tile width',
  )

  validateTileDimension(
    height,
    'Terrain tile height',
  )

  if (
    !Number.isInteger(x)
    || x < 0
    || !Number.isInteger(y)
    || y < 0
    || !Number.isInteger(level)
    || level < 0
  ) {
    throw new RangeError(
      'Terrain tile coordinates and level must be non-negative integers.',
    )
  }

  const xTileCount =
    tilingScheme.getNumberOfXTilesAtLevel(
      level,
    )

  const yTileCount =
    tilingScheme.getNumberOfYTilesAtLevel(
      level,
    )

  if (
    x >= xTileCount
    || y >= yTileCount
  ) {
    throw new RangeError(
      'Terrain tile coordinate is outside the tiling scheme.',
    )
  }

  const rectangle =
    tilingScheme.tileXYToRectangle(
      x,
      y,
      level,
    )

  const northDegrees =
    CesiumMath.toDegrees(
      rectangle.north,
    )

  const southDegrees =
    CesiumMath.toDegrees(
      rectangle.south,
    )

  const westDegrees =
    CesiumMath.toDegrees(
      rectangle.west,
    )

  const eastDegrees =
    CesiumMath.toDegrees(
      rectangle.east,
    )

  const latitudeSpan =
    northDegrees -
    southDegrees

  const longitudeSpan =
    eastDegrees -
    westDegrees

  const buffer =
    new Float32Array(
      width *
      height,
    )

  for (
    let row = 0;
    row < height;
    row += 1
  ) {
    const latitudeDegrees =
      northDegrees -
      latitudeSpan *
      row /
      (height - 1)

    for (
      let column = 0;
      column < width;
      column += 1
    ) {
      const longitudeDegrees =
        westDegrees +
        longitudeSpan *
        column /
        (width - 1)

      buffer[
        row *
        width +
        column
      ] =
        heightField.sampleHeightMeters(
          latitudeDegrees,
          longitudeDegrees,
        )
    }
  }

  return buffer
}

export function createEstTerrainPresentation(
  meanRadiusMeters: number,
  surface: SurfaceResponse,
  terrain: TerrainResponse,
): EstTerrainPresentation {
  if (
    !Number.isFinite(
      meanRadiusMeters,
    )
    || meanRadiusMeters <= 0
  ) {
    throw new RangeError(
      'Planet mean radius must be finite and greater than zero.',
    )
  }

  const ellipsoid =
    new Ellipsoid(
      meanRadiusMeters,
      meanRadiusMeters,
      meanRadiusMeters,
    )

  const tilingScheme =
    new GeographicTilingScheme({
      ellipsoid,
      numberOfLevelZeroTilesX: 2,
      numberOfLevelZeroTilesY: 1,
    })

  const heightField =
    createTerrainHeightField(
      surface,
      terrain,
    )

  const terrainProvider =
    new CustomHeightmapTerrainProvider({
      width:
        estTerrainTileWidth,
      height:
        estTerrainTileHeight,
      tilingScheme,
      callback:
        (
          x,
          y,
          level,
        ) =>
          buildEstTerrainHeightmapTile(
            tilingScheme,
            heightField,
            x,
            y,
            level,
          ),
    })

  return {
    ellipsoid,
    tilingScheme,
    terrainProvider,
    heightField,
  }
}
