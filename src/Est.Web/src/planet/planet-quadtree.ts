import {
  CUBE_FACES,
  cubeFacePoint,
  projectCubePointToUnitSphere,
  type CubeFace,
} from './cube-sphere'

export interface PlanetPatch {
  readonly face: CubeFace
  readonly level: number
  readonly x: number
  readonly y: number
}

export interface PlanetPatchBounds {
  readonly uMin: number
  readonly uMax: number
  readonly vMin: number
  readonly vMax: number
}

export function patchKey(
  patch: PlanetPatch,
): string {
  return [
    patch.face,
    patch.level,
    patch.x,
    patch.y,
  ].join(':')
}

export function patchBounds(
  patch: PlanetPatch,
): PlanetPatchBounds {
  if (
    !Number.isInteger(patch.level) ||
    patch.level < 0
  ) {
    throw new Error(
      'Planet patch level must be a non-negative integer.',
    )
  }

  const patchCount = 2 ** patch.level

  if (
    !Number.isInteger(patch.x) ||
    !Number.isInteger(patch.y) ||
    patch.x < 0 ||
    patch.y < 0 ||
    patch.x >= patchCount ||
    patch.y >= patchCount
  ) {
    throw new Error(
      'Planet patch coordinates are outside the requested level.',
    )
  }

  const size = 2 / patchCount

  return {
    uMin: -1 + patch.x * size,
    uMax: -1 + (patch.x + 1) * size,
    vMin: -1 + patch.y * size,
    vMax: -1 + (patch.y + 1) * size,
  }
}

export function subdividePatch(
  patch: PlanetPatch,
): readonly PlanetPatch[] {
  const level = patch.level + 1
  const x = patch.x * 2
  const y = patch.y * 2

  return [
    {
      face: patch.face,
      level,
      x,
      y,
    },
    {
      face: patch.face,
      level,
      x: x + 1,
      y,
    },
    {
      face: patch.face,
      level,
      x,
      y: y + 1,
    },
    {
      face: patch.face,
      level,
      x: x + 1,
      y: y + 1,
    },
  ]
}

export function patchesAtLevel(
  face: CubeFace,
  level: number,
): PlanetPatch[] {
  if (
    !Number.isInteger(level) ||
    level < 0
  ) {
    throw new Error(
      'Planet patch level must be a non-negative integer.',
    )
  }

  const count = 2 ** level
  const patches: PlanetPatch[] = []

  for (
    let y = 0;
    y < count;
    y += 1
  ) {
    for (
      let x = 0;
      x < count;
      x += 1
    ) {
      patches.push({
        face,
        level,
        x,
        y,
      })
    }
  }

  return patches
}

export interface PlanetViewPoint {
  readonly x: number
  readonly y: number
  readonly z: number
}

export interface PlanetLodOptions {
  readonly minimumLevel: number
  readonly maximumLevel: number
  readonly splitThreshold: number
}

function patchCornerDirections(
  patch: PlanetPatch,
): readonly PlanetViewPoint[] {
  const bounds = patchBounds(patch)

  return [
    [bounds.uMin, bounds.vMin],
    [bounds.uMax, bounds.vMin],
    [bounds.uMin, bounds.vMax],
    [bounds.uMax, bounds.vMax],
  ].map(([u, v]) =>
    projectCubePointToUnitSphere(
      cubeFacePoint(
        patch.face,
        u,
        v,
      ),
    ),
  )
}

export function patchCenterDirection(
  patch: PlanetPatch,
): PlanetViewPoint {
  const bounds = patchBounds(patch)

  return projectCubePointToUnitSphere(
    cubeFacePoint(
      patch.face,
      (bounds.uMin + bounds.uMax) / 2,
      (bounds.vMin + bounds.vMax) / 2,
    ),
  )
}

export function patchWorldDiameter(
  patch: PlanetPatch,
): number {
  const corners =
    patchCornerDirections(patch)

  let maximum = 0

  for (
    let a = 0;
    a < corners.length;
    a += 1
  ) {
    for (
      let b = a + 1;
      b < corners.length;
      b += 1
    ) {
      maximum = Math.max(
        maximum,
        Math.hypot(
          corners[a].x - corners[b].x,
          corners[a].y - corners[b].y,
          corners[a].z - corners[b].z,
        ),
      )
    }
  }

  return maximum
}

export interface PlanetFrustumPlane {
  readonly normal: PlanetViewPoint
  readonly d: number
}

export interface PlanetPatchBoundingSphere {
  readonly center: PlanetViewPoint
  readonly radius: number
}

export function patchBoundingSphere(
  patch: PlanetPatch,
): PlanetPatchBoundingSphere {
  const center =
    patchCenterDirection(patch)

  const radius =
    Math.max(
      ...patchCornerDirections(
        patch,
      ).map(
        (corner) =>
          Math.hypot(
            corner.x - center.x,
            corner.y - center.y,
            corner.z - center.z,
          ),
      ),
    )

  return {
    center,
    radius,
  }
}

export function patchIntersectsFrustum(
  patch: PlanetPatch,
  planes: readonly PlanetFrustumPlane[],
): boolean {
  const sphere =
    patchBoundingSphere(patch)

  for (const plane of planes) {
    const normalLength =
      Math.hypot(
        plane.normal.x,
        plane.normal.y,
        plane.normal.z,
      )

    if (
      !Number.isFinite(
        normalLength,
      ) ||
      normalLength === 0 ||
      !Number.isFinite(plane.d)
    ) {
      throw new Error(
        'Planet frustum plane is invalid.',
      )
    }

    const signedDistance =
      plane.normal.x *
        sphere.center.x +
      plane.normal.y *
        sphere.center.y +
      plane.normal.z *
        sphere.center.z +
      plane.d

    if (
      signedDistance <
      -sphere.radius *
        normalLength
    ) {
      return false
    }
  }

  return true
}

export function filterPlanetPatchesByFrustum(
  patches: readonly PlanetPatch[],
  planes: readonly PlanetFrustumPlane[],
): PlanetPatch[] {
  return patches.filter(
    (patch) =>
      patchIntersectsFrustum(
        patch,
        planes,
      ),
  )
}

export function patchIntersectsHorizon(
  patch: PlanetPatch,
  camera: PlanetViewPoint,
): boolean {
  if (
    !Number.isFinite(camera.x) ||
    !Number.isFinite(camera.y) ||
    !Number.isFinite(camera.z)
  ) {
    throw new Error(
      'Planet horizon camera position is invalid.',
    )
  }

  const cameraDistance =
    Math.hypot(
      camera.x,
      camera.y,
      camera.z,
    )

  if (cameraDistance <= 1) {
    return true
  }

  const sphere =
    patchBoundingSphere(patch)

  const maximumCameraDot =
    camera.x *
      sphere.center.x +
    camera.y *
      sphere.center.y +
    camera.z *
      sphere.center.z +
    cameraDistance *
      sphere.radius

  return maximumCameraDot >= 1
}

export function filterPlanetPatchesByHorizon(
  patches: readonly PlanetPatch[],
  camera: PlanetViewPoint,
): PlanetPatch[] {
  return patches.filter(
    (patch) =>
      patchIntersectsHorizon(
        patch,
        camera,
      ),
  )
}

function distance(
  a: PlanetViewPoint,
  b: PlanetViewPoint,
): number {
  return Math.hypot(
    a.x - b.x,
    a.y - b.y,
    a.z - b.z,
  )
}

function shouldSplitPatch(
  patch: PlanetPatch,
  camera: PlanetViewPoint,
  options: PlanetLodOptions,
): boolean {
  if (
    patch.level <
    options.minimumLevel
  ) {
    return true
  }

  if (
    patch.level >=
    options.maximumLevel
  ) {
    return false
  }

  const center =
    patchCenterDirection(patch)

  const cameraDistance =
    Math.max(
      distance(camera, center),
      0.0001,
    )

  const projectedSize =
    patchWorldDiameter(patch) /
    cameraDistance

  return (
    projectedSize >
    options.splitThreshold
  )
}

export function selectPlanetPatches(
  camera: PlanetViewPoint,
  options: PlanetLodOptions,
): PlanetPatch[] {
  if (
    !Number.isInteger(
      options.minimumLevel,
    ) ||
    options.minimumLevel < 0 ||
    !Number.isInteger(
      options.maximumLevel,
    ) ||
    options.maximumLevel <
      options.minimumLevel ||
    !Number.isFinite(
      options.splitThreshold,
    ) ||
    options.splitThreshold <= 0
  ) {
    throw new Error(
      'Planet LOD options are invalid.',
    )
  }

  const selected: PlanetPatch[] = []

  function visit(
    patch: PlanetPatch,
  ): void {
    if (
      shouldSplitPatch(
        patch,
        camera,
        options,
      )
    ) {
      for (
        const child of
          subdividePatch(patch)
      ) {
        visit(child)
      }

      return
    }

    selected.push(patch)
  }

  for (const face of CUBE_FACES) {
    visit({
      face,
      level: 0,
      x: 0,
      y: 0,
    })
  }

  return selected
}

export type PlanetPatchEdge =
  | 'left'
  | 'right'
  | 'bottom'
  | 'top'

export interface PlanetPatchNeighborPair {
  readonly first: PlanetPatch
  readonly second: PlanetPatch
}

interface CubeFaceCoordinates {
  readonly face: CubeFace
  readonly u: number
  readonly v: number
}

interface PatchCoverage {
  readonly resolution: number
  readonly cells: Map<
    CubeFace,
    Array<
      PlanetPatch | undefined
    >
  >
}

const PATCH_EDGES:
  readonly PlanetPatchEdge[] = [
    'left',
    'right',
    'bottom',
    'top',
  ]

export function sphereDirectionToCubeFaceCoordinates(
  point: PlanetViewPoint,
): CubeFaceCoordinates {
  const length =
    Math.hypot(
      point.x,
      point.y,
      point.z,
    )

  if (
    !Number.isFinite(length) ||
    length === 0
  ) {
    throw new Error(
      'Cube-face lookup requires a finite non-zero direction.',
    )
  }

  const x = point.x / length
  const y = point.y / length
  const z = point.z / length

  const ax = Math.abs(x)
  const ay = Math.abs(y)
  const az = Math.abs(z)

  if (
    ax >= ay &&
    ax >= az
  ) {
    if (x >= 0) {
      return {
        face: 'positiveX',
        u: -z / ax,
        v: y / ax,
      }
    }

    return {
      face: 'negativeX',
      u: z / ax,
      v: y / ax,
    }
  }

  if (
    ay >= ax &&
    ay >= az
  ) {
    if (y >= 0) {
      return {
        face: 'positiveY',
        u: x / ay,
        v: -z / ay,
      }
    }

    return {
      face: 'negativeY',
      u: x / ay,
      v: z / ay,
    }
  }

  if (z >= 0) {
    return {
      face: 'positiveZ',
      u: x / az,
      v: y / az,
    }
  }

  return {
    face: 'negativeZ',
    u: -x / az,
    v: y / az,
  }
}

function buildPatchCoverage(
  patches: readonly PlanetPatch[],
  maximumLevel: number,
): PatchCoverage {
  if (
    !Number.isInteger(maximumLevel) ||
    maximumLevel < 0
  ) {
    throw new Error(
      'Planet coverage level must be a non-negative integer.',
    )
  }

  const resolution =
    2 ** maximumLevel

  const cells =
    new Map<
      CubeFace,
      Array<
        PlanetPatch | undefined
      >
    >()

  for (const face of CUBE_FACES) {
    cells.set(
      face,
      new Array<
        PlanetPatch | undefined
      >(
        resolution *
          resolution,
      ),
    )
  }

  for (const patch of patches) {
    if (
      patch.level >
      maximumLevel
    ) {
      throw new Error(
        'Planet patch exceeds the coverage level.',
      )
    }

    const scale =
      2 **
      (
        maximumLevel -
        patch.level
      )

    const startX =
      patch.x * scale

    const startY =
      patch.y * scale

    const faceCells =
      cells.get(patch.face)

    if (!faceCells) {
      throw new Error(
        'Planet patch face is not available.',
      )
    }

    for (
      let y = startY;
      y < startY + scale;
      y += 1
    ) {
      for (
        let x = startX;
        x < startX + scale;
        x += 1
      ) {
        const index =
          y * resolution + x

        const existing =
          faceCells[index]

        if (
          existing &&
          patchKey(existing) !==
            patchKey(patch)
        ) {
          throw new Error(
            'Planet patches overlap.',
          )
        }

        faceCells[index] =
          patch
      }
    }
  }

  return {
    resolution,
    cells,
  }
}

function finestCellCoordinate(
  value: number,
  resolution: number,
): number {
  const coordinate =
    Math.floor(
      ((value + 1) / 2) *
        resolution,
    )

  return Math.max(
    0,
    Math.min(
      resolution - 1,
      coordinate,
    ),
  )
}

function neighborSample(
  patch: PlanetPatch,
  edge: PlanetPatchEdge,
  t: number,
): CubeFaceCoordinates {
  const bounds =
    patchBounds(patch)

  const epsilon = 1e-7

  let u =
    bounds.uMin +
    (bounds.uMax -
      bounds.uMin) *
      t

  let v =
    bounds.vMin +
    (bounds.vMax -
      bounds.vMin) *
      t

  switch (edge) {
    case 'left':
      u =
        bounds.uMin -
        epsilon
      break

    case 'right':
      u =
        bounds.uMax +
        epsilon
      break

    case 'bottom':
      v =
        bounds.vMin -
        epsilon
      break

    case 'top':
      v =
        bounds.vMax +
        epsilon
      break
  }

  const direction =
    projectCubePointToUnitSphere(
      cubeFacePoint(
        patch.face,
        u,
        v,
      ),
    )

  return (
    sphereDirectionToCubeFaceCoordinates(
      direction,
    )
  )
}

function neighborsAlongEdge(
  patch: PlanetPatch,
  edge: PlanetPatchEdge,
  coverage: PatchCoverage,
  maximumLevel: number,
): PlanetPatch[] {
  const sampleCount =
    2 **
    (
      maximumLevel -
      patch.level
    )

  const neighbors =
    new Map<
      string,
      PlanetPatch
    >()

  for (
    let index = 0;
    index < sampleCount;
    index += 1
  ) {
    const t =
      (index + 0.5) /
      sampleCount

    const location =
      neighborSample(
        patch,
        edge,
        t,
      )

    const x =
      finestCellCoordinate(
        location.u,
        coverage.resolution,
      )

    const y =
      finestCellCoordinate(
        location.v,
        coverage.resolution,
      )

    const faceCells =
      coverage.cells.get(
        location.face,
      )

    const neighbor =
      faceCells?.[
        y *
          coverage.resolution +
        x
      ]

    if (
      neighbor &&
      patchKey(neighbor) !==
        patchKey(patch)
    ) {
      neighbors.set(
        patchKey(neighbor),
        neighbor,
      )
    }
  }

  return [
    ...neighbors.values(),
  ]
}

export function findPlanetPatchStitchEdges(
  patches: readonly PlanetPatch[],
  maximumLevel: number,
): Map<
  string,
  Record<
    PlanetPatchEdge,
    boolean
  >
> {
  const coverage =
    buildPatchCoverage(
      patches,
      maximumLevel,
    )

  const result =
    new Map<
      string,
      Record<
        PlanetPatchEdge,
        boolean
      >
    >()

  for (const patch of patches) {
    const edges: Record<
      PlanetPatchEdge,
      boolean
    > = {
      left: false,
      right: false,
      bottom: false,
      top: false,
    }

    for (const edge of PATCH_EDGES) {
      edges[edge] =
        neighborsAlongEdge(
          patch,
          edge,
          coverage,
          maximumLevel,
        ).some(
          (neighbor) =>
            neighbor.level <
            patch.level,
        )
    }

    result.set(
      patchKey(patch),
      edges,
    )
  }

  return result
}

export function findPlanetPatchNeighbors(
  patches: readonly PlanetPatch[],
  maximumLevel: number,
): PlanetPatchNeighborPair[] {
  const coverage =
    buildPatchCoverage(
      patches,
      maximumLevel,
    )

  const pairs =
    new Map<
      string,
      PlanetPatchNeighborPair
    >()

  for (const patch of patches) {
    for (const edge of PATCH_EDGES) {
      for (
        const neighbor of
          neighborsAlongEdge(
            patch,
            edge,
            coverage,
            maximumLevel,
          )
      ) {
        const firstKey =
          patchKey(patch)

        const secondKey =
          patchKey(neighbor)

        const pairKey =
          firstKey < secondKey
            ? `${firstKey}|${secondKey}`
            : `${secondKey}|${firstKey}`

        if (!pairs.has(pairKey)) {
          pairs.set(
            pairKey,
            {
              first: patch,
              second: neighbor,
            },
          )
        }
      }
    }
  }

  return [
    ...pairs.values(),
  ]
}

export function balancePlanetPatches(
  patches: readonly PlanetPatch[],
  maximumLevel: number,
): PlanetPatch[] {
  let balanced = [
    ...patches,
  ]

  while (true) {
    const coverage =
      buildPatchCoverage(
        balanced,
        maximumLevel,
      )

    const splitKeys =
      new Set<string>()

    for (const patch of balanced) {
      if (
        patch.level >=
        maximumLevel
      ) {
        continue
      }

      for (
        const edge of
          PATCH_EDGES
      ) {
        const neighbors =
          neighborsAlongEdge(
            patch,
            edge,
            coverage,
            maximumLevel,
          )

        if (
          neighbors.some(
            (neighbor) =>
              neighbor.level >
              patch.level + 1,
          )
        ) {
          splitKeys.add(
            patchKey(patch),
          )

          break
        }
      }
    }

    if (
      splitKeys.size === 0
    ) {
      return balanced
    }

    balanced =
      balanced.flatMap(
        (patch) =>
          splitKeys.has(
            patchKey(patch),
          )
            ? subdividePatch(
                patch,
              )
            : [patch],
      )
  }
}

export function selectBalancedPlanetPatches(
  camera: PlanetViewPoint,
  options: PlanetLodOptions,
): PlanetPatch[] {
  return balancePlanetPatches(
    selectPlanetPatches(
      camera,
      options,
    ),
    options.maximumLevel,
  )
}
