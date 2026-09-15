export interface PlanetaryDirection {
  readonly x: number
  readonly y: number
  readonly z: number
}

export interface PlanetaryLightingState {
  /**
   * Unit vector in planet-local coordinates pointing
   * from the surface toward the light source.
   */
  readonly surfaceToLightDirection:
    PlanetaryDirection

  /**
   * Minimum illumination retained on terrain that is
   * not directly lit.
   */
  readonly ambientIntensity: number

  /**
   * Lambertian contribution on directly lit terrain.
   */
  readonly diffuseIntensity: number
}

function normalizeDirection(
  direction: PlanetaryDirection,
): PlanetaryDirection {
  const length =
    Math.hypot(
      direction.x,
      direction.y,
      direction.z,
    )

  if (
    !Number.isFinite(length) ||
    length <= 0
  ) {
    throw new Error(
      'Planetary light direction must be finite and non-zero.',
    )
  }

  return {
    x: direction.x / length,
    y: direction.y / length,
    z: direction.z / length,
  }
}

export function createPlanetaryLightingState(
  surfaceToLightDirection:
    PlanetaryDirection,
  ambientIntensity = 0.30,
  diffuseIntensity = 0.70,
): PlanetaryLightingState {
  if (
    !Number.isFinite(
      ambientIntensity,
    ) ||
    ambientIntensity < 0
  ) {
    throw new Error(
      'Planetary ambient intensity must be finite and non-negative.',
    )
  }

  if (
    !Number.isFinite(
      diffuseIntensity,
    ) ||
    diffuseIntensity < 0
  ) {
    throw new Error(
      'Planetary diffuse intensity must be finite and non-negative.',
    )
  }

  return {
    surfaceToLightDirection:
      normalizeDirection(
        surfaceToLightDirection,
      ),
    ambientIntensity,
    diffuseIntensity,
  }
}

export const defaultPlanetaryLighting =
  createPlanetaryLightingState(
    {
      x: 0.8,
      y: 0.35,
      z: -0.6,
    },
  )

/**
 * Babylon DirectionalLight.direction describes the
 * direction travelled by the light rays, which is the
 * inverse of Est's surface-to-light direction.
 */
export function planetaryLightRayDirection(
  lighting:
    PlanetaryLightingState,
): PlanetaryDirection {
  return {
    x:
      -lighting
        .surfaceToLightDirection.x,
    y:
      -lighting
        .surfaceToLightDirection.y,
    z:
      -lighting
        .surfaceToLightDirection.z,
  }
}

export function evaluatePlanetaryIllumination(
  surfaceNormal: PlanetaryDirection,
  lighting:
    PlanetaryLightingState,
): number {
  const normal =
    normalizeDirection(
      surfaceNormal,
    )

  const light =
    lighting.surfaceToLightDirection

  const lambert =
    Math.max(
      normal.x * light.x +
      normal.y * light.y +
      normal.z * light.z,
      0,
    )

  return (
    lighting.ambientIntensity +
    lambert *
      lighting.diffuseIntensity
  )
}
