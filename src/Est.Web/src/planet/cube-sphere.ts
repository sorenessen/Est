export const CUBE_FACES = [
  'positiveX',
  'negativeX',
  'positiveY',
  'negativeY',
  'positiveZ',
  'negativeZ',
] as const

export type CubeFace = (typeof CUBE_FACES)[number]

export interface Vector3Like {
  readonly x: number
  readonly y: number
  readonly z: number
}

export interface GeographicCoordinateDegrees {
  readonly latitudeDegrees: number
  readonly longitudeDegrees: number
}

export type CubeSphereRadialOffset =
  (
    direction: Vector3Like,
  ) => number

export type CubeSphereSurfaceNormal =
  (
    direction: Vector3Like,
  ) => Vector3Like

export interface CubeSphereFaceGeometry {
  readonly positions: number[]
  readonly indices: number[]
  readonly normals: number[]
}

export function cubeFacePoint(
  face: CubeFace,
  u: number,
  v: number,
): Vector3Like {
  switch (face) {
    case 'positiveX':
      return { x: 1, y: v, z: -u }

    case 'negativeX':
      return { x: -1, y: v, z: u }

    case 'positiveY':
      return { x: u, y: 1, z: -v }

    case 'negativeY':
      return { x: u, y: -1, z: v }

    case 'positiveZ':
      return { x: u, y: v, z: 1 }

    case 'negativeZ':
      return { x: -u, y: v, z: -1 }
  }
}

export function projectCubePointToUnitSphere(
  point: Vector3Like,
): Vector3Like {
  const length = Math.hypot(
    point.x,
    point.y,
    point.z,
  )

  if (!Number.isFinite(length) || length === 0) {
    throw new Error(
      'Cube-sphere projection requires a finite nonzero point.',
    )
  }

  return {
    x: point.x / length,
    y: point.y / length,
    z: point.z / length,
  }
}

export function sphereDirectionToGeographicDegrees(
  direction: Vector3Like,
): GeographicCoordinateDegrees {
  const normalized =
    projectCubePointToUnitSphere(
      direction,
    )

  const radiansToDegrees =
    180 / Math.PI

  return {
    latitudeDegrees:
      Math.asin(
        Math.max(
          -1,
          Math.min(
            1,
            normalized.z,
          ),
        ),
      ) *
      radiansToDegrees,
    longitudeDegrees:
      Math.atan2(
        normalized.y,
        normalized.x,
      ) *
      radiansToDegrees,
  }
}

function triangleFacesOutward(
  positions: readonly number[],
  a: number,
  b: number,
  c: number,
): boolean {
  const ax = positions[a * 3]
  const ay = positions[a * 3 + 1]
  const az = positions[a * 3 + 2]

  const abx = positions[b * 3] - ax
  const aby = positions[b * 3 + 1] - ay
  const abz = positions[b * 3 + 2] - az

  const acx = positions[c * 3] - ax
  const acy = positions[c * 3 + 1] - ay
  const acz = positions[c * 3 + 2] - az

  const nx = aby * acz - abz * acy
  const ny = abz * acx - abx * acz
  const nz = abx * acy - aby * acx

  const cx =
    (
      positions[a * 3] +
      positions[b * 3] +
      positions[c * 3]
    ) / 3

  const cy =
    (
      positions[a * 3 + 1] +
      positions[b * 3 + 1] +
      positions[c * 3 + 1]
    ) / 3

  const cz =
    (
      positions[a * 3 + 2] +
      positions[b * 3 + 2] +
      positions[c * 3 + 2]
    ) / 3

  return nx * cx + ny * cy + nz * cz > 0
}

function appendOutwardTriangle(
  indices: number[],
  positions: readonly number[],
  a: number,
  b: number,
  c: number,
): void {
  if (triangleFacesOutward(
    positions,
    a,
    b,
    c,
  )) {
    indices.push(a, b, c)
    return
  }

  indices.push(a, c, b)
}

export interface CubeSpherePatchStitchEdges {
  readonly left?: boolean
  readonly right?: boolean
  readonly bottom?: boolean
  readonly top?: boolean
}

function patchVertexIndex(
  row: number,
  column: number,
  stride: number,
): number {
  return row * stride + column
}

function appendPatchTriangle(
  indices: number[],
  positions: readonly number[],
  a: number,
  b: number,
  c: number,
): void {
  appendOutwardTriangle(
    indices,
    positions,
    a,
    b,
    c,
  )
}

function appendRegularPatchCell(
  indices: number[],
  positions: readonly number[],
  row: number,
  column: number,
  stride: number,
): void {
  const lowerLeft =
    patchVertexIndex(
      row,
      column,
      stride,
    )

  const lowerRight =
    patchVertexIndex(
      row,
      column + 1,
      stride,
    )

  const upperLeft =
    patchVertexIndex(
      row + 1,
      column,
      stride,
    )

  const upperRight =
    patchVertexIndex(
      row + 1,
      column + 1,
      stride,
    )

  appendPatchTriangle(
    indices,
    positions,
    lowerLeft,
    lowerRight,
    upperRight,
  )

  appendPatchTriangle(
    indices,
    positions,
    lowerLeft,
    upperRight,
    upperLeft,
  )
}

function appendHorizontalStitchPair(
  indices: number[],
  positions: readonly number[],
  boundaryRow: number,
  innerRow: number,
  column: number,
  stride: number,
  omitStartTriangle: boolean,
  omitEndTriangle: boolean,
): void {
  const boundaryStart =
    patchVertexIndex(
      boundaryRow,
      column,
      stride,
    )

  const boundaryEnd =
    patchVertexIndex(
      boundaryRow,
      column + 2,
      stride,
    )

  const innerStart =
    patchVertexIndex(
      innerRow,
      column,
      stride,
    )

  const innerMiddle =
    patchVertexIndex(
      innerRow,
      column + 1,
      stride,
    )

  const innerEnd =
    patchVertexIndex(
      innerRow,
      column + 2,
      stride,
    )

  if (!omitStartTriangle) {
    appendPatchTriangle(
      indices,
      positions,
      boundaryStart,
      innerMiddle,
      innerStart,
    )
  }

  appendPatchTriangle(
    indices,
    positions,
    boundaryStart,
    boundaryEnd,
    innerMiddle,
  )

  if (!omitEndTriangle) {
    appendPatchTriangle(
      indices,
      positions,
      boundaryEnd,
      innerEnd,
      innerMiddle,
    )
  }
}

function appendVerticalStitchPair(
  indices: number[],
  positions: readonly number[],
  boundaryColumn: number,
  innerColumn: number,
  row: number,
  stride: number,
  omitStartTriangle: boolean,
  omitEndTriangle: boolean,
): void {
  const boundaryStart =
    patchVertexIndex(
      row,
      boundaryColumn,
      stride,
    )

  const boundaryEnd =
    patchVertexIndex(
      row + 2,
      boundaryColumn,
      stride,
    )

  const innerStart =
    patchVertexIndex(
      row,
      innerColumn,
      stride,
    )

  const innerMiddle =
    patchVertexIndex(
      row + 1,
      innerColumn,
      stride,
    )

  const innerEnd =
    patchVertexIndex(
      row + 2,
      innerColumn,
      stride,
    )

  if (!omitStartTriangle) {
    appendPatchTriangle(
      indices,
      positions,
      boundaryStart,
      innerMiddle,
      innerStart,
    )
  }

  appendPatchTriangle(
    indices,
    positions,
    boundaryStart,
    boundaryEnd,
    innerMiddle,
  )

  if (!omitEndTriangle) {
    appendPatchTriangle(
      indices,
      positions,
      boundaryEnd,
      innerEnd,
      innerMiddle,
    )
  }
}

export function createCubeSpherePatchGeometry(
  face: CubeFace,
  segments: number,
  uMin: number,
  uMax: number,
  vMin: number,
  vMax: number,
  stitchEdges: CubeSpherePatchStitchEdges = {},
  radialOffset?: CubeSphereRadialOffset,
  normalAtDirection?: CubeSphereSurfaceNormal,
): CubeSphereFaceGeometry {
  if (
    !Number.isInteger(segments) ||
    segments < 1
  ) {
    throw new Error(
      'Cube-sphere segments must be a positive integer.',
    )
  }

  const hasStitchedEdge =
    stitchEdges.left === true ||
    stitchEdges.right === true ||
    stitchEdges.bottom === true ||
    stitchEdges.top === true

  if (hasStitchedEdge && segments % 2 !== 0) {
    throw new Error(
      'Cube-sphere stitched patch edges require an even segment count.',
    )
  }

  const positions: number[] = []
  const normals: number[] = []
  const indices: number[] = []

  const stride = segments + 1

  for (
    let row = 0;
    row <= segments;
    row += 1
  ) {
    const v =
      vMin +
      (row / segments) *
        (vMax - vMin)

    for (
      let column = 0;
      column <= segments;
      column += 1
    ) {
      const u =
        uMin +
        (column / segments) *
          (uMax - uMin)

      const cubePoint =
        cubeFacePoint(face, u, v)

      const spherePoint =
        projectCubePointToUnitSphere(
          cubePoint,
        )

      const offset =
        radialOffset
          ? radialOffset(
              spherePoint,
            )
          : 0

      if (
        !Number.isFinite(offset) ||
        1 + offset <= 0
      ) {
        throw new Error(
          'Cube-sphere radial offset must be finite and preserve a positive radius.',
        )
      }

      const radius =
        1 + offset

      positions.push(
        spherePoint.x * radius,
        spherePoint.y * radius,
        spherePoint.z * radius,
      )

      let surfaceNormal =
        normalAtDirection
          ? projectCubePointToUnitSphere(
              normalAtDirection(
                spherePoint,
              ),
            )
          : spherePoint

      if (
        surfaceNormal.x *
          spherePoint.x +
        surfaceNormal.y *
          spherePoint.y +
        surfaceNormal.z *
          spherePoint.z <
        0
      ) {
        surfaceNormal = {
          x: -surfaceNormal.x,
          y: -surfaceNormal.y,
          z: -surfaceNormal.z,
        }
      }

      normals.push(
        surfaceNormal.x,
        surfaceNormal.y,
        surfaceNormal.z,
      )
    }
  }

  const firstRegularRow =
    stitchEdges.bottom
      ? 1
      : 0

  const lastRegularRow =
    segments -
    (
      stitchEdges.top
        ? 1
        : 0
    )

  const firstRegularColumn =
    stitchEdges.left
      ? 1
      : 0

  const lastRegularColumn =
    segments -
    (
      stitchEdges.right
        ? 1
        : 0
    )

  for (
    let row = firstRegularRow;
    row < lastRegularRow;
    row += 1
  ) {
    for (
      let column = firstRegularColumn;
      column < lastRegularColumn;
      column += 1
    ) {
      appendRegularPatchCell(
        indices,
        positions,
        row,
        column,
        stride,
      )
    }
  }

  if (stitchEdges.bottom) {
    for (
      let column = 0;
      column < segments;
      column += 2
    ) {
      appendHorizontalStitchPair(
        indices,
        positions,
        0,
        1,
        column,
        stride,
        stitchEdges.left === true &&
          column === 0,
        stitchEdges.right === true &&
          column + 2 === segments,
      )
    }
  }

  if (stitchEdges.top) {
    for (
      let column = 0;
      column < segments;
      column += 2
    ) {
      appendHorizontalStitchPair(
        indices,
        positions,
        segments,
        segments - 1,
        column,
        stride,
        stitchEdges.left === true &&
          column === 0,
        stitchEdges.right === true &&
          column + 2 === segments,
      )
    }
  }

  if (stitchEdges.left) {
    for (
      let row = 0;
      row < segments;
      row += 2
    ) {
      appendVerticalStitchPair(
        indices,
        positions,
        0,
        1,
        row,
        stride,
        stitchEdges.bottom === true &&
          row === 0,
        stitchEdges.top === true &&
          row + 2 === segments,
      )
    }
  }

  if (stitchEdges.right) {
    for (
      let row = 0;
      row < segments;
      row += 2
    ) {
      appendVerticalStitchPair(
        indices,
        positions,
        segments,
        segments - 1,
        row,
        stride,
        stitchEdges.bottom === true &&
          row === 0,
        stitchEdges.top === true &&
          row + 2 === segments,
      )
    }
  }

  return {
    positions,
    indices,
    normals,
  }
}

export function createCubeSphereFaceGeometry(
  face: CubeFace,
  segments: number,
): CubeSphereFaceGeometry {
  return createCubeSpherePatchGeometry(
    face,
    segments,
    -1,
    1,
    -1,
    1,
  )
}
