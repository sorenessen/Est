import type { CubeFace } from './cube-sphere'

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
