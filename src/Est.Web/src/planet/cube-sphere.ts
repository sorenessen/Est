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

export function createCubeSpherePatchGeometry(
  face: CubeFace,
  segments: number,
  uMin: number,
  uMax: number,
  vMin: number,
  vMax: number,
): CubeSphereFaceGeometry {
  if (
    !Number.isInteger(segments) ||
    segments < 1
  ) {
    throw new Error(
      'Cube-sphere segments must be a positive integer.',
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

      positions.push(
        spherePoint.x,
        spherePoint.y,
        spherePoint.z,
      )

      // R1 is an exact unit sphere, so its analytic
      // surface normal is the normalized position.
      // This also guarantees identical normals where
      // independently generated cube faces meet.
      normals.push(
        spherePoint.x,
        spherePoint.y,
        spherePoint.z,
      )
    }
  }

  for (
    let row = 0;
    row < segments;
    row += 1
  ) {
    for (
      let column = 0;
      column < segments;
      column += 1
    ) {
      const lowerLeft =
        row * stride + column

      const lowerRight =
        lowerLeft + 1

      const upperLeft =
        lowerLeft + stride

      const upperRight =
        upperLeft + 1

      appendOutwardTriangle(
        indices,
        positions,
        lowerLeft,
        lowerRight,
        upperRight,
      )

      appendOutwardTriangle(
        indices,
        positions,
        lowerLeft,
        upperRight,
        upperLeft,
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
