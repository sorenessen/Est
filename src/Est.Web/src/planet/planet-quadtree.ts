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
