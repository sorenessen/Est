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

export interface TerrainSurfaceSample {
  readonly elevationMeters: number
  readonly gradientMetersPerUnit: {
    readonly x: number
    readonly y: number
    readonly z: number
  }
}

export interface TerrainHeightField {
  sampleHeightMeters(
    latitudeDegrees: number,
    longitudeDegrees: number,
  ): number
}

export interface TerrainSurfaceHeightField
  extends TerrainHeightField {
  sampleSurfaceMeters(
    latitudeDegrees: number,
    longitudeDegrees: number,
  ): TerrainSurfaceSample
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

function dotTerrainVector(
  left: {
    readonly x: number
    readonly y: number
    readonly z: number
  },
  right: {
    readonly x: number
    readonly y: number
    readonly z: number
  },
): number {
  return (
    left.x * right.x +
    left.y * right.y +
    left.z * right.z
  )
}

function normalizeTerrainVector(
  vector: {
    readonly x: number
    readonly y: number
    readonly z: number
  },
): {
  x: number
  y: number
  z: number
} {
  const length =
    Math.hypot(
      vector.x,
      vector.y,
      vector.z,
    )

  if (
    !Number.isFinite(length) ||
    length === 0
  ) {
    throw new Error(
      'Terrain tangent basis could not be computed.',
    )
  }

  return {
    x: vector.x / length,
    y: vector.y / length,
    z: vector.z / length,
  }
}

function terrainTangentBasis(
  direction: {
    readonly x: number
    readonly y: number
    readonly z: number
  },
): readonly [
  {
    readonly x: number
    readonly y: number
    readonly z: number
  },
  {
    readonly x: number
    readonly y: number
    readonly z: number
  },
] {
  const ax =
    Math.abs(direction.x)

  const ay =
    Math.abs(direction.y)

  const az =
    Math.abs(direction.z)

  const reference =
    ax <= ay && ax <= az
      ? { x: 1, y: 0, z: 0 }
      : ay <= az
        ? { x: 0, y: 1, z: 0 }
        : { x: 0, y: 0, z: 1 }

  const first =
    normalizeTerrainVector({
      x:
        reference.y * direction.z -
        reference.z * direction.y,
      y:
        reference.z * direction.x -
        reference.x * direction.z,
      z:
        reference.x * direction.y -
        reference.y * direction.x,
    })

  const second =
    normalizeTerrainVector({
      x:
        direction.y * first.z -
        direction.z * first.y,
      y:
        direction.z * first.x -
        direction.x * first.z,
      z:
        direction.x * first.y -
        direction.y * first.x,
    })

  return [
    first,
    second,
  ]
}

function interpolateElevationMeters(
  nearest:
    readonly TerrainDistanceSample[],
): number {
  const exact =
    nearest[0]

  if (
    exact &&
    exact.distanceSquared <=
      exactPointDistanceSquared
  ) {
    return exact
      .point
      .elevationMeters
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

  return (
    weightedElevation /
    totalWeight
  )
}

function estimateTerrainGradientMetersPerUnit(
  sample: {
    readonly x: number
    readonly y: number
    readonly z: number
  },
  nearest:
    readonly TerrainDistanceSample[],
  elevationMeters: number,
): {
  x: number
  y: number
  z: number
} {
  const [
    firstTangent,
    secondTangent,
  ] =
    terrainTangentBasis(sample)

  let xx = 0
  let xy = 0
  let yy = 0
  let xz = 0
  let yz = 0

  for (const candidate of nearest) {
    if (
      candidate.distanceSquared <=
      exactPointDistanceSquared
    ) {
      continue
    }

    const localX =
      dotTerrainVector(
        candidate.point,
        firstTangent,
      )

    const localY =
      dotTerrainVector(
        candidate.point,
        secondTangent,
      )

    const localElevation =
      candidate
        .point
        .elevationMeters -
      elevationMeters

    const weight =
      1 /
      Math.max(
        candidate.distanceSquared,
        1e-12,
      )

    xx +=
      weight *
      localX *
      localX

    xy +=
      weight *
      localX *
      localY

    yy +=
      weight *
      localY *
      localY

    xz +=
      weight *
      localX *
      localElevation

    yz +=
      weight *
      localY *
      localElevation
  }

  const determinant =
    xx * yy -
    xy * xy

  if (
    !Number.isFinite(determinant) ||
    Math.abs(determinant) <
      1e-18
  ) {
    return {
      x: 0,
      y: 0,
      z: 0,
    }
  }

  const firstSlope =
    (
      xz * yy -
      yz * xy
    ) /
    determinant

  const secondSlope =
    (
      yz * xx -
      xz * xy
    ) /
    determinant

  return {
    x:
      firstTangent.x *
        firstSlope +
      secondTangent.x *
        secondSlope,
    y:
      firstTangent.y *
        firstSlope +
      secondTangent.y *
        secondSlope,
    z:
      firstTangent.z *
        firstSlope +
      secondTangent.z *
        secondSlope,
  }
}

export function createTerrainHeightField(
  surface: SurfaceResponse,
  terrain: TerrainResponse,
): TerrainSurfaceHeightField {
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
    new Map<
      string,
      TerrainSurfaceSample
    >()

  function sampleSurfaceMeters(
    latitudeDegrees: number,
    longitudeDegrees: number,
  ): TerrainSurfaceSample {
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

    const elevationMeters =
      interpolateElevationMeters(
        nearest,
      )

    const result:
      TerrainSurfaceSample = {
        elevationMeters,
        gradientMetersPerUnit:
          estimateTerrainGradientMetersPerUnit(
            sample,
            nearest,
            elevationMeters,
          ),
      }

    sampleCache.set(
      cacheKey,
      result,
    )

    return result
  }

  return {
    sampleHeightMeters(
      latitudeDegrees: number,
      longitudeDegrees: number,
    ): number {
      return sampleSurfaceMeters(
        latitudeDegrees,
        longitudeDegrees,
      ).elevationMeters
    },

    sampleSurfaceMeters,
  }
}
