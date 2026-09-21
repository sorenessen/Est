export type TerrainSurfaceScatterKind =
  | 'grass'
  | 'stone'

interface TerrainSurfaceScatterBase {
  kind: TerrainSurfaceScatterKind
  latitudeDegrees: number
  longitudeDegrees: number
  scaleX: number
  scaleY: number
  scaleZ: number
  yawRadians: number
}

export interface TerrainGrassScatterSample
  extends TerrainSurfaceScatterBase {
  kind: 'grass'
  variation: number
  dryness: number
  stiffness: number
  seedHead: number
}

export interface TerrainStoneScatterSample
  extends TerrainSurfaceScatterBase {
  kind: 'stone'
}

export type TerrainSurfaceScatterSample =
  | TerrainGrassScatterSample
  | TerrainStoneScatterSample

const degreesToRadians =
  Math.PI /
  180

const radiansToDegrees =
  180 /
  Math.PI

const twoPi =
  Math.PI *
  2

function hashIdentity(
  identity: string,
): number {
  let hash =
    2166136261

  for (
    let index = 0;
    index < identity.length;
    index += 1
  ) {
    hash ^=
      identity.charCodeAt(
        index,
      )

    hash =
      Math.imul(
        hash,
        16777619,
      )
  }

  return hash >>> 0
}

function mixCell(
  seed: number,
  row: number,
  column: number,
  salt: number,
): number {
  let hash =
    seed ^
    Math.imul(
      row,
      374761393,
    ) ^
    Math.imul(
      column,
      668265263,
    ) ^
    salt

  hash =
    Math.imul(
      hash ^
        (
          hash >>>
          13
        ),
      1274126177,
    )

  hash ^=
    hash >>>
    16

  return hash >>> 0
}

function unitValue(
  value: number,
): number {
  return (
    value /
    4294967295
  )
}

function clamp01(
  value: number,
): number {
  return Math.max(
    0,
    Math.min(
      1,
      value,
    ),
  )
}

function wrapPositiveRadians(
  value: number,
): number {
  return (
    (
      value %
      twoPi
    ) +
    twoPi
  ) %
    twoPi
}

function wrapSignedRadians(
  value: number,
): number {
  return (
    wrapPositiveRadians(
      value +
      Math.PI,
    ) -
    Math.PI
  )
}

function wrapIndex(
  value: number,
  count: number,
): number {
  return (
    (
      value %
      count
    ) +
    count
  ) %
    count
}

function planetField(
  latitudeRadians: number,
  longitudeRadians: number,
  planetRadiusMeters: number,
  seed: number,
  scaleMeters: number,
  phaseSalt: number,
): number {
  const cosineLatitude =
    Math.cos(
      latitudeRadians,
    )

  const x =
    cosineLatitude *
    Math.cos(
      longitudeRadians,
    ) *
    planetRadiusMeters /
    scaleMeters

  const y =
    Math.sin(
      latitudeRadians,
    ) *
    planetRadiusMeters /
    scaleMeters

  const z =
    cosineLatitude *
    Math.sin(
      longitudeRadians,
    ) *
    planetRadiusMeters /
    scaleMeters

  const phaseA =
    unitValue(
      mixCell(
        seed,
        11,
        29,
        phaseSalt,
      ),
    ) *
    twoPi

  const phaseB =
    unitValue(
      mixCell(
        seed,
        37,
        17,
        phaseSalt ^
          0x51f15e31,
      ),
    ) *
    twoPi

  const phaseC =
    unitValue(
      mixCell(
        seed,
        53,
        41,
        phaseSalt ^
          0x21c73b49,
      ),
    ) *
    twoPi

  const signal =
    Math.sin(
      x +
      phaseA,
    ) *
      0.46 +
    Math.sin(
      y *
        1.31 +
      z *
        0.79 +
      phaseB,
    ) *
      0.31 +
    Math.cos(
      (
        x -
        z
      ) *
        1.67 +
      y *
        0.28 +
      phaseC,
    ) *
      0.23

  return clamp01(
    0.5 +
    signal *
      0.5,
  )
}

function surfaceDensity(
  latitudeRadians: number,
  longitudeRadians: number,
  planetRadiusMeters: number,
  seed: number,
): number {
  const broad =
    planetField(
      latitudeRadians,
      longitudeRadians,
      planetRadiusMeters,
      seed,
      8.0,
      0x41d7a821,
    )

  const local =
    planetField(
      latitudeRadians,
      longitudeRadians,
      planetRadiusMeters,
      seed,
      3.1,
      0x6a42b91f,
    )

  const densitySignal =
    broad *
      0.68 +
    local *
      0.32

  return (
    0.58 +
    densitySignal *
      densitySignal *
      0.30
  )
}

function surfaceDryness(
  latitudeRadians: number,
  longitudeRadians: number,
  planetRadiusMeters: number,
  seed: number,
): number {
  const broad =
    planetField(
      latitudeRadians,
      longitudeRadians,
      planetRadiusMeters,
      seed,
      13.0,
      0x725c19ab,
    )

  const fine =
    planetField(
      latitudeRadians,
      longitudeRadians,
      planetRadiusMeters,
      seed,
      4.6,
      0x2da98431,
    )

  return clamp01(
    0.08 +
    broad *
      0.48 +
    fine *
      0.22,
  )
}

/**
 * Deterministic presentation-only near-field vegetation.
 *
 * Individual grass samples represent individual blades rather than
 * repeated grass props. World-anchored geographic cells keep them
 * stable while the local renderer reanchors.
 */
export function createTerrainSurfaceScatter(
  centerLatitudeDegrees: number,
  centerLongitudeDegrees: number,
  planetRadiusMeters: number,
  planetIdentity: string,
  radiusMeters: number,
  spacingMeters = 0.62,
): TerrainSurfaceScatterSample[] {
  if (
    !Number.isFinite(
      centerLatitudeDegrees,
    ) ||
    centerLatitudeDegrees < -90 ||
    centerLatitudeDegrees > 90
  ) {
    throw new Error(
      'Terrain scatter latitude must be finite and between -90 and 90.',
    )
  }

  if (
    !Number.isFinite(
      centerLongitudeDegrees,
    )
  ) {
    throw new Error(
      'Terrain scatter longitude must be finite.',
    )
  }

  if (
    !Number.isFinite(
      planetRadiusMeters,
    ) ||
    planetRadiusMeters <= 0
  ) {
    throw new Error(
      'Terrain scatter planet radius must be positive and finite.',
    )
  }

  if (
    !Number.isFinite(
      radiusMeters,
    ) ||
    radiusMeters <= 0
  ) {
    throw new Error(
      'Terrain scatter radius must be positive and finite.',
    )
  }

  if (
    !Number.isFinite(
      spacingMeters,
    ) ||
    spacingMeters <= 0
  ) {
    throw new Error(
      'Terrain scatter spacing must be positive and finite.',
    )
  }

  if (
    planetIdentity.length === 0
  ) {
    throw new Error(
      'Terrain scatter planet identity cannot be empty.',
    )
  }

  const seed =
    hashIdentity(
      `${planetIdentity}:wild-grass-v3`,
    )

  const centerLatitudeRadians =
    centerLatitudeDegrees *
    degreesToRadians

  const centerLongitudeRadians =
    centerLongitudeDegrees *
    degreesToRadians

  const latitudeRowCount =
    Math.max(
      1,
      Math.round(
        Math.PI *
        planetRadiusMeters /
        spacingMeters,
      ),
    )

  const latitudeStep =
    Math.PI /
    latitudeRowCount

  const centerRow =
    Math.min(
      latitudeRowCount - 1,
      Math.max(
        0,
        Math.floor(
          (
            centerLatitudeRadians +
            Math.PI /
              2
          ) /
          latitudeStep,
        ),
      ),
    )

  const rowRadius =
    Math.ceil(
      radiusMeters /
      (
        planetRadiusMeters *
        latitudeStep
      ),
    ) +
    2

  const samples:
    TerrainSurfaceScatterSample[] =
      []

  for (
    let rowOffset =
      -rowRadius;
    rowOffset <=
      rowRadius;
    rowOffset += 1
  ) {
    const row =
      centerRow +
      rowOffset

    if (
      row < 0 ||
      row >=
        latitudeRowCount
    ) {
      continue
    }

    const rowLatitude =
      -Math.PI /
        2 +
      (
        row +
        0.5
      ) *
        latitudeStep

    const rowCosine =
      Math.max(
        0.02,
        Math.abs(
          Math.cos(
            rowLatitude,
          ),
        ),
      )

    const longitudeColumnCount =
      Math.max(
        1,
        Math.round(
          twoPi *
          planetRadiusMeters *
          rowCosine /
          spacingMeters,
        ),
      )

    const longitudeStep =
      twoPi /
      longitudeColumnCount

    const centerColumn =
      Math.floor(
        wrapPositiveRadians(
          centerLongitudeRadians,
        ) /
        longitudeStep,
      )

    const columnRadius =
      Math.ceil(
        radiusMeters /
        (
          planetRadiusMeters *
          rowCosine *
          longitudeStep
        ),
      ) +
      2

    const visitedColumns =
      new Set<number>()

    for (
      let columnOffset =
        -columnRadius;
      columnOffset <=
        columnRadius;
      columnOffset += 1
    ) {
      const column =
        wrapIndex(
          centerColumn +
            columnOffset,
          longitudeColumnCount,
        )

      if (
        visitedColumns.has(
          column,
        )
      ) {
        continue
      }

      visitedColumns.add(
        column,
      )

      const latitudeJitter =
        (
          unitValue(
            mixCell(
              seed,
              row,
              column,
              0x2b5f13a7,
            ),
          ) -
          0.5
        ) *
        latitudeStep *
        0.88

      const longitudeJitter =
        (
          unitValue(
            mixCell(
              seed,
              row,
              column,
              0x6d1c84f3,
            ),
          ) -
          0.5
        ) *
        longitudeStep *
        0.88

      const latitudeRadians =
        Math.max(
          -Math.PI /
            2 +
            1e-9,
          Math.min(
            Math.PI /
              2 -
              1e-9,
            rowLatitude +
              latitudeJitter,
          ),
        )

      const longitudeRadians =
        wrapSignedRadians(
          (
            column +
            0.5
          ) *
            longitudeStep +
          longitudeJitter,
        )

      const northMeters =
        (
          latitudeRadians -
          centerLatitudeRadians
        ) *
        planetRadiusMeters

      const longitudeDelta =
        wrapSignedRadians(
          longitudeRadians -
          centerLongitudeRadians,
        )

      const eastMeters =
        longitudeDelta *
        planetRadiusMeters *
        Math.cos(
          (
            latitudeRadians +
            centerLatitudeRadians
          ) /
            2,
        )

      if (
        Math.hypot(
          eastMeters,
          northMeters,
        ) >
        radiusMeters
      ) {
        continue
      }

      const presence =
        unitValue(
          mixCell(
            seed,
            row,
            column,
            0x41c64e6d,
          ),
        )

      const localDensity =
        surfaceDensity(
          latitudeRadians,
          longitudeRadians,
          planetRadiusMeters,
          seed,
        )

      if (
        presence >
        localDensity
      ) {
        continue
      }

      const kindSelector =
        unitValue(
          mixCell(
            seed,
            row,
            column,
            0x19a7dc53,
          ),
        )

      const yawRadians =
        unitValue(
          mixCell(
            seed,
            row,
            column,
            0x5e73ac91,
          ),
        ) *
        twoPi

      const sizeA =
        unitValue(
          mixCell(
            seed,
            row,
            column,
            0x7f4a2d11,
          ),
        )

      const sizeB =
        unitValue(
          mixCell(
            seed,
            row,
            column,
            0x35bd9017,
          ),
        )

      const sizeC =
        unitValue(
          mixCell(
            seed,
            row,
            column,
            0x68d72c4b,
          ),
        )

      if (
        kindSelector <
        0.0007
      ) {
        samples.push({
          kind:
            'stone',
          latitudeDegrees:
            latitudeRadians *
            radiansToDegrees,
          longitudeDegrees:
            longitudeRadians *
            radiansToDegrees,
          scaleX:
            0.10 +
            sizeA *
              0.18,
          scaleY:
            0.035 +
            sizeB *
              0.055,
          scaleZ:
            0.09 +
            sizeC *
              0.20,
          yawRadians,
        })

        continue
      }

      const variation =
        unitValue(
          mixCell(
            seed,
            row,
            column,
            0x59f31487,
          ),
        )

      const stiffness =
        unitValue(
          mixCell(
            seed,
            row,
            column,
            0x176a9d43,
          ),
        )

      const seedSelector =
        unitValue(
          mixCell(
            seed,
            row,
            column,
            0x3bd45129,
          ),
        )

      const localDryness =
        clamp01(
          surfaceDryness(
            latitudeRadians,
            longitudeRadians,
            planetRadiusMeters,
            seed,
          ) *
            0.78 +
          unitValue(
            mixCell(
              seed,
              row,
              column,
              0x61bd28f7,
            ),
          ) *
            0.22,
        )

      const tallSignal =
        Math.pow(
          sizeC,
          2.2,
        )

      const heightMeters =
        0.48 +
        sizeB *
          0.24 +
        tallSignal *
          0.12

      const seedHead =
        seedSelector >
        0.91
          ? clamp01(
              (
                seedSelector -
                0.91
              ) /
              0.09
            )
          : 0

      samples.push({
        kind:
          'grass',
        latitudeDegrees:
          latitudeRadians *
          radiansToDegrees,
        longitudeDegrees:
          longitudeRadians *
          radiansToDegrees,
        scaleX:
          0.82 +
          sizeA *
            0.34,
        scaleY:
          heightMeters +
          seedHead *
            0.10,
        scaleZ:
          0.82 +
          sizeC *
            0.34,
        yawRadians,
        variation,
        dryness:
          localDryness,
        stiffness:
          0.12 +
          stiffness *
            0.78,
        seedHead,
      })
    }
  }

  return samples
}
