import type {
  SurfaceGridResponse,
  SurfaceResponse,
  TerrainResponse,
} from '../api/est-api'

const degreesToRadians = Math.PI / 180
const bucketSizeDegrees = 10
const latitudeBucketCount = 18
const longitudeBucketCount = 36
const interpolationNeighborCount = 12
const exactPointDistanceSquared = 1e-20

type TerrainControlPoint = {
  x: number
  y: number
  z: number
  elevationMeters: number
}

export interface TerrainHeightField {
  sampleHeightMeters(
    latitudeDegrees: number,
    longitudeDegrees: number,
  ): number
}

function sameGrid(
  left: SurfaceGridResponse,
  right: SurfaceGridResponse,
): boolean {
  return (
    left.kind === right.kind
    && left.identityVersion === right.identityVersion
    && left.latitudeBandCount === right.latitudeBandCount
    && left.longitudeBandCount === right.longitudeBandCount
  )
}

function normalizeLongitudeDegrees(
  longitudeDegrees: number,
): number {
  let normalized =
    longitudeDegrees % 360

  if (normalized < -180) {
    normalized += 360
  }

  if (normalized >= 180) {
    normalized -= 360
  }

  return normalized
}

function latitudeBucketIndex(
  latitudeDegrees: number,
): number {
  return Math.min(
    latitudeBucketCount - 1,
    Math.max(
      0,
      Math.floor(
        (latitudeDegrees + 90) /
        bucketSizeDegrees,
      ),
    ),
  )
}

function longitudeBucketIndex(
  longitudeDegrees: number,
): number {
  return Math.min(
    longitudeBucketCount - 1,
    Math.max(
      0,
      Math.floor(
        (
          normalizeLongitudeDegrees(
            longitudeDegrees,
          ) + 180
        ) /
        bucketSizeDegrees,
      ),
    ),
  )
}

function wrapLongitudeBucket(
  index: number,
): number {
  const wrapped =
    index % longitudeBucketCount

  return wrapped < 0
    ? wrapped + longitudeBucketCount
    : wrapped
}

function bucketKey(
  latitudeIndex: number,
  longitudeIndex: number,
): number {
  return (
    latitudeIndex *
    longitudeBucketCount +
    longitudeIndex
  )
}

function unitVector(
  latitudeDegrees: number,
  longitudeDegrees: number,
): {
  x: number
  y: number
  z: number
} {
  const latitudeRadians =
    latitudeDegrees *
    degreesToRadians

  const longitudeRadians =
    normalizeLongitudeDegrees(
      longitudeDegrees,
    ) *
    degreesToRadians

  const cosLatitude =
    Math.cos(
      latitudeRadians,
    )

  return {
    x:
      cosLatitude *
      Math.cos(
        longitudeRadians,
      ),
    y:
      cosLatitude *
      Math.sin(
        longitudeRadians,
      ),
    z:
      Math.sin(
        latitudeRadians,
      ),
  }
}

function validateCoordinate(
  latitudeDegrees: number,
  longitudeDegrees: number,
): void {
  if (
    !Number.isFinite(
      latitudeDegrees,
    )
    || latitudeDegrees < -90
    || latitudeDegrees > 90
  ) {
    throw new RangeError(
      'Terrain sample latitude must be finite and between -90 and 90 degrees.',
    )
  }

  if (
    !Number.isFinite(
      longitudeDegrees,
    )
  ) {
    throw new RangeError(
      'Terrain sample longitude must be finite.',
    )
  }
}

type TerrainDistanceSample = {
  point: TerrainControlPoint
  distanceSquared: number
}

function findNearestControlPoints(
  searchPoints:
    readonly TerrainControlPoint[],
  sample: {
    readonly x: number
    readonly y: number
    readonly z: number
  },
): TerrainDistanceSample[] {
  const nearest:
    TerrainDistanceSample[] = []

  for (const point of searchPoints) {
    const dx =
      sample.x - point.x

    const dy =
      sample.y - point.y

    const dz =
      sample.z - point.z

    const candidate:
      TerrainDistanceSample = {
        point,
        distanceSquared:
          dx * dx +
          dy * dy +
          dz * dz,
      }

    if (
      nearest.length <
      interpolationNeighborCount
    ) {
      nearest.push(candidate)
      continue
    }

    let farthestIndex = 0

    for (
      let index = 1;
      index < nearest.length;
      index += 1
    ) {
      if (
        nearest[index]
          .distanceSquared >
        nearest[farthestIndex]
          .distanceSquared
      ) {
        farthestIndex = index
      }
    }

    if (
      candidate.distanceSquared <
      nearest[farthestIndex]
        .distanceSquared
    ) {
      nearest[farthestIndex] =
        candidate
    }
  }

  nearest.sort(
    (left, right) =>
      left.distanceSquared -
      right.distanceSquared,
  )

  return nearest
}

export function createTerrainHeightField(
  surface: SurfaceResponse,
  terrain: TerrainResponse,
): TerrainHeightField {
  if (
    surface.planetId !==
    terrain.planetId
  ) {
    throw new Error(
      'Surface and terrain belong to different planets.',
    )
  }

  if (
    !sameGrid(
      surface.grid,
      terrain.grid,
    )
  ) {
    throw new Error(
      'Surface and terrain grid definitions do not match.',
    )
  }

  if (
    surface.cells.length === 0
  ) {
    throw new Error(
      'Terrain height field requires at least one surface cell.',
    )
  }

  if (
    surface.cells.length !==
    terrain.cells.length
  ) {
    throw new Error(
      'Surface and terrain must contain the same cells.',
    )
  }

  const terrainByCellId =
    new Map(
      terrain.cells.map(
        cell => [
          cell.cellId,
          cell,
        ] as const,
      ),
    )

  if (
    terrainByCellId.size !==
    terrain.cells.length
  ) {
    throw new Error(
      'Terrain contains duplicate cell identities.',
    )
  }

  const buckets =
    new Map<
      number,
      TerrainControlPoint[]
    >()

  const allPoints:
    TerrainControlPoint[] = []

  for (const cell of surface.cells) {
    const terrainCell =
      terrainByCellId.get(
        cell.cellId,
      )

    if (!terrainCell) {
      throw new Error(
        `Surface cell ${cell.cellId} has no terrain state.`,
      )
    }

    if (
      !Number.isFinite(
        terrainCell.elevationMeters,
      )
    ) {
      throw new Error(
        `Terrain cell ${cell.cellId} has a non-finite elevation.`,
      )
    }

    validateCoordinate(
      cell.centerLatitudeDegrees,
      cell.centerLongitudeDegrees,
    )

    const vector =
      unitVector(
        cell.centerLatitudeDegrees,
        cell.centerLongitudeDegrees,
      )

    const point:
      TerrainControlPoint = {
        ...vector,
        elevationMeters:
          terrainCell.elevationMeters,
      }

    allPoints.push(
      point,
    )

    const key =
      bucketKey(
        latitudeBucketIndex(
          cell.centerLatitudeDegrees,
        ),
        longitudeBucketIndex(
          cell.centerLongitudeDegrees,
        ),
      )

    const bucket =
      buckets.get(
        key,
      )

    if (bucket) {
      bucket.push(
        point,
      )
    } else {
      buckets.set(
        key,
        [point],
      )
    }
  }

  const sampleCache =
    new Map<string, number>()

  return {
    sampleHeightMeters(
      latitudeDegrees: number,
      longitudeDegrees: number,
    ): number {
      validateCoordinate(
        latitudeDegrees,
        longitudeDegrees,
      )

      const normalizedLongitude =
        normalizeLongitudeDegrees(
          longitudeDegrees,
        )

      const cacheKey =
        `${latitudeDegrees}:${normalizedLongitude}`

      const cached =
        sampleCache.get(cacheKey)

      if (cached !== undefined) {
        return cached
      }

      const sample =
        unitVector(
          latitudeDegrees,
          longitudeDegrees,
        )

      const latitudeIndex =
        latitudeBucketIndex(
          latitudeDegrees,
        )

      const longitudeIndex =
        longitudeBucketIndex(
          longitudeDegrees,
        )

      const candidates:
        TerrainControlPoint[] = []

      const latitudeStart =
        Math.max(
          0,
          latitudeIndex - 1,
        )

      const latitudeEnd =
        Math.min(
          latitudeBucketCount - 1,
          latitudeIndex + 1,
        )

      if (
        Math.abs(
          latitudeDegrees,
        ) >= 80
      ) {
        for (
          let candidateLatitude =
            latitudeStart;
          candidateLatitude <=
            latitudeEnd;
          candidateLatitude += 1
        ) {
          for (
            let candidateLongitude = 0;
            candidateLongitude <
              longitudeBucketCount;
            candidateLongitude += 1
          ) {
            const bucket =
              buckets.get(
                bucketKey(
                  candidateLatitude,
                  candidateLongitude,
                ),
              )

            if (bucket) {
              candidates.push(
                ...bucket,
              )
            }
          }
        }
      } else {
        for (
          let candidateLatitude =
            latitudeStart;
          candidateLatitude <=
            latitudeEnd;
          candidateLatitude += 1
        ) {
          for (
            let longitudeOffset = -1;
            longitudeOffset <= 1;
            longitudeOffset += 1
          ) {
            const bucket =
              buckets.get(
                bucketKey(
                  candidateLatitude,
                  wrapLongitudeBucket(
                    longitudeIndex +
                    longitudeOffset,
                  ),
                ),
              )

            if (bucket) {
              candidates.push(
                ...bucket,
              )
            }
          }
        }
      }

      const searchPoints =
        candidates.length >=
        interpolationNeighborCount
          ? candidates
          : allPoints

      const nearest =
        findNearestControlPoints(
          searchPoints,
          sample,
        )

      const exact =
        nearest[0]

      if (
        exact
        && exact.distanceSquared <=
          exactPointDistanceSquared
      ) {
        const elevation =
          exact
            .point
            .elevationMeters

        sampleCache.set(
          cacheKey,
          elevation,
        )

        return elevation
      }

      let weightedElevation = 0
      let totalWeight = 0

      for (const candidate of nearest) {
        const weight =
          1 /
          Math.max(
            candidate.distanceSquared,
            1e-12,
          )

        weightedElevation +=
          candidate
            .point
            .elevationMeters *
          weight

        totalWeight +=
          weight
      }

      const elevation =
        weightedElevation /
        totalWeight

      sampleCache.set(
        cacheKey,
        elevation,
      )

      return elevation
    },
  }
}
