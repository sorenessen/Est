const degreesToRadians =
  Math.PI / 180

const octaveDefinitions = [
  {
    wavelengthMeters: 1_200,
    amplitudeMeters: 16,
  },
  {
    wavelengthMeters: 360,
    amplitudeMeters: 5,
  },
  {
    wavelengthMeters: 90,
    amplitudeMeters: 1.5,
  },
  {
    wavelengthMeters: 24,
    amplitudeMeters: 0.35,
  },
] as const

function validateCoordinate(
  latitudeDegrees: number,
  longitudeDegrees: number,
): void {
  if (
    !Number.isFinite(
      latitudeDegrees,
    ) ||
    latitudeDegrees < -90 ||
    latitudeDegrees > 90
  ) {
    throw new RangeError(
      'Terrain presentation latitude must be finite and between -90 and 90 degrees.',
    )
  }

  if (
    !Number.isFinite(
      longitudeDegrees,
    )
  ) {
    throw new RangeError(
      'Terrain presentation longitude must be finite.',
    )
  }
}

function validateRadius(
  planetRadiusMeters: number,
): void {
  if (
    !Number.isFinite(
      planetRadiusMeters,
    ) ||
    planetRadiusMeters <= 0
  ) {
    throw new RangeError(
      'Terrain presentation planet radius must be finite and positive.',
    )
  }
}

function hashString(
  value: string,
): number {
  let hash =
    0x811c9dc5

  for (
    let index = 0;
    index < value.length;
    index += 1
  ) {
    hash ^=
      value.charCodeAt(
        index,
      )

    hash =
      Math.imul(
        hash,
        0x01000193,
      )
  }

  return hash >>> 0
}

function hashLattice(
  x: number,
  y: number,
  z: number,
  seed: number,
): number {
  let hash =
    seed ^ 0x9e3779b9

  hash ^=
    Math.imul(
      x | 0,
      0x1b873593,
    )

  hash ^=
    Math.imul(
      y | 0,
      0x85ebca6b,
    )

  hash ^=
    Math.imul(
      z | 0,
      0xc2b2ae35,
    )

  hash ^= hash >>> 16

  hash =
    Math.imul(
      hash,
      0x7feb352d,
    )

  hash ^= hash >>> 15

  hash =
    Math.imul(
      hash,
      0x846ca68b,
    )

  hash ^= hash >>> 16

  return hash >>> 0
}

function latticeValue(
  x: number,
  y: number,
  z: number,
  seed: number,
): number {
  return (
    hashLattice(
      x,
      y,
      z,
      seed,
    ) /
      0xffffffff *
      2 -
    1
  )
}

function smoothStep(
  value: number,
): number {
  return (
    value *
    value *
    (
      3 -
      2 * value
    )
  )
}

function interpolate(
  start: number,
  end: number,
  amount: number,
): number {
  return (
    start +
    (
      end -
      start
    ) *
      amount
  )
}

function sampleValueNoise3d(
  x: number,
  y: number,
  z: number,
  seed: number,
): number {
  const x0 =
    Math.floor(x)

  const y0 =
    Math.floor(y)

  const z0 =
    Math.floor(z)

  const x1 =
    x0 + 1

  const y1 =
    y0 + 1

  const z1 =
    z0 + 1

  const tx =
    smoothStep(
      x - x0,
    )

  const ty =
    smoothStep(
      y - y0,
    )

  const tz =
    smoothStep(
      z - z0,
    )

  const c000 =
    latticeValue(
      x0,
      y0,
      z0,
      seed,
    )

  const c100 =
    latticeValue(
      x1,
      y0,
      z0,
      seed,
    )

  const c010 =
    latticeValue(
      x0,
      y1,
      z0,
      seed,
    )

  const c110 =
    latticeValue(
      x1,
      y1,
      z0,
      seed,
    )

  const c001 =
    latticeValue(
      x0,
      y0,
      z1,
      seed,
    )

  const c101 =
    latticeValue(
      x1,
      y0,
      z1,
      seed,
    )

  const c011 =
    latticeValue(
      x0,
      y1,
      z1,
      seed,
    )

  const c111 =
    latticeValue(
      x1,
      y1,
      z1,
      seed,
    )

  const x00 =
    interpolate(
      c000,
      c100,
      tx,
    )

  const x10 =
    interpolate(
      c010,
      c110,
      tx,
    )

  const x01 =
    interpolate(
      c001,
      c101,
      tx,
    )

  const x11 =
    interpolate(
      c011,
      c111,
      tx,
    )

  const y0Value =
    interpolate(
      x00,
      x10,
      ty,
    )

  const y1Value =
    interpolate(
      x01,
      x11,
      ty,
    )

  return interpolate(
    y0Value,
    y1Value,
    tz,
  )
}

/**
 * Returns deterministic presentation-only terrain relief in meters.
 *
 * This does not alter authoritative terrain state. It supplies higher
 * spatial-frequency visual detail over Est's continuous authoritative
 * terrain field for local human-scale presentation.
 *
 * Sampling is performed in planet-centered 3D metric coordinates so the
 * result is continuous across longitude wrapping and does not depend on
 * renderer patch boundaries or camera position.
 */
export function sampleTerrainPresentationDetailMeters(
  latitudeDegrees: number,
  longitudeDegrees: number,
  planetRadiusMeters: number,
  planetIdentity: string,
): number {
  validateCoordinate(
    latitudeDegrees,
    longitudeDegrees,
  )

  validateRadius(
    planetRadiusMeters,
  )

  if (
    planetIdentity.length === 0
  ) {
    throw new Error(
      'Terrain presentation planet identity cannot be empty.',
    )
  }

  const latitudeRadians =
    latitudeDegrees *
    degreesToRadians

  const longitudeRadians =
    longitudeDegrees *
    degreesToRadians

  const cosLatitude =
    Math.cos(
      latitudeRadians,
    )

  const unitX =
    cosLatitude *
    Math.cos(
      longitudeRadians,
    )

  const unitY =
    Math.sin(
      latitudeRadians,
    )

  const unitZ =
    cosLatitude *
    Math.sin(
      longitudeRadians,
    )

  const baseSeed =
    hashString(
      planetIdentity,
    )

  let detailMeters =
    0

  for (
    let octaveIndex = 0;
    octaveIndex <
      octaveDefinitions.length;
    octaveIndex += 1
  ) {
    const octave =
      octaveDefinitions[
        octaveIndex
      ]

    const scale =
      planetRadiusMeters /
      octave.wavelengthMeters

    const octaveSeed =
      (
        baseSeed +
        Math.imul(
          octaveIndex + 1,
          0x9e3779b1,
        )
      ) >>> 0

    detailMeters +=
      sampleValueNoise3d(
        unitX * scale,
        unitY * scale,
        unitZ * scale,
        octaveSeed,
      ) *
      octave.amplitudeMeters
  }

  return detailMeters
}


export interface TerrainSurfaceAppearanceSample {
  albedoVariation: number
  earthiness: number
  microHeightMeters: number
}

function sampleTerrainSurfaceNoise(
  latitudeDegrees: number,
  longitudeDegrees: number,
  planetRadiusMeters: number,
  wavelengthMeters: number,
  seed: number,
): number {
  const latitudeRadians =
    latitudeDegrees *
    degreesToRadians

  const longitudeRadians =
    longitudeDegrees *
    degreesToRadians

  const cosLatitude =
    Math.cos(
      latitudeRadians,
    )

  const scale =
    planetRadiusMeters /
    wavelengthMeters

  return sampleValueNoise3d(
    cosLatitude *
      Math.cos(
        longitudeRadians,
      ) *
      scale,
    Math.sin(
      latitudeRadians,
    ) *
      scale,
    cosLatitude *
      Math.sin(
        longitudeRadians,
      ) *
      scale,
    seed,
  )
}

/**
 * Deterministic close-range surface appearance for embodied play.
 *
 * This is presentation state only. It supplies sub-regional visual
 * breakup and micro-normal relief without becoming authoritative
 * terrain, soil, vegetation, or geology state.
 */
export function sampleTerrainSurfaceAppearance(
  latitudeDegrees: number,
  longitudeDegrees: number,
  planetRadiusMeters: number,
  planetIdentity: string,
): TerrainSurfaceAppearanceSample {
  validateCoordinate(
    latitudeDegrees,
    longitudeDegrees,
  )

  validateRadius(
    planetRadiusMeters,
  )

  if (
    planetIdentity.length === 0
  ) {
    throw new Error(
      'Terrain surface appearance planet identity cannot be empty.',
    )
  }

  const baseSeed =
    hashString(
      `${planetIdentity}:ground`,
    )

  const broad =
    sampleTerrainSurfaceNoise(
      latitudeDegrees,
      longitudeDegrees,
      planetRadiusMeters,
      42,
      baseSeed ^
        0x42a15d31,
    )

  const medium =
    sampleTerrainSurfaceNoise(
      latitudeDegrees,
      longitudeDegrees,
      planetRadiusMeters,
      13,
      baseSeed ^
        0x71c9e247,
    )

  const small =
    sampleTerrainSurfaceNoise(
      latitudeDegrees,
      longitudeDegrees,
      planetRadiusMeters,
      4,
      baseSeed ^
        0x2d4f6a89,
    )

  const fine =
    sampleTerrainSurfaceNoise(
      latitudeDegrees,
      longitudeDegrees,
      planetRadiusMeters,
      1.8,
      baseSeed ^
        0x5ab318c7,
    )

  const albedoVariation =
    Math.max(
      -1,
      Math.min(
        1,
        broad * 0.48 +
          medium * 0.30 +
          small * 0.17 +
          fine * 0.05,
      ),
    )

  const earthiness =
    Math.max(
      0,
      Math.min(
        1,
        (
          medium * 0.55 +
          small * 0.30 +
          fine * 0.15 +
          1
        ) /
          2,
      ),
    )

  const microHeightMeters =
    broad * 0.30 +
    medium * 0.12 +
    small * 0.04 +
    fine * 0.015

  return {
    albedoVariation,
    earthiness,
    microHeightMeters,
  }
}
