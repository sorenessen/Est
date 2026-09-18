import {
  Color3,
  FresnelParameters,
  Mesh,
  Scene,
  StandardMaterial,
} from '@babylonjs/core'

import {
  CreateIcoSphereVertexData,
} from '@babylonjs/core/Meshes/Builders/icoSphereBuilder.js'

import type {
  StandingWaterResponse,
  SurfaceResponse,
} from '../api/est-api'

export interface StandingWaterMeshOptions {
  meanRadiusMeters: number
  surface: SurfaceResponse
  standingWater: StandingWaterResponse
  sphereSubdivisions?: number
}

export interface StandingWaterMeshResult {
  mesh: Mesh | undefined
  floodedTriangleCount: number
  waterBodyCount: number
}

function gridsMatch(
  surface: SurfaceResponse,
  standingWater: StandingWaterResponse,
): boolean {
  return (
    surface.grid.kind ===
      standingWater.grid.kind &&
    surface.grid.identityVersion ===
      standingWater.grid.identityVersion &&
    surface.grid.latitudeBandCount ===
      standingWater.grid.latitudeBandCount &&
    surface.grid.longitudeBandCount ===
      standingWater.grid.longitudeBandCount
  )
}

function readOceanSurfaceElevationMeters(
  standingWater: StandingWaterResponse,
): number | undefined {
  let oceanSurfaceElevationMeters:
    number | undefined

  for (
    const cell
    of standingWater.cells
  ) {
    if (
      cell.kind !== 'Ocean' ||
      cell.waterDepthMeters <= 0
    ) {
      continue
    }

    const elevation =
      cell.waterSurfaceElevationMeters

    if (
      !Number.isFinite(
        elevation,
      )
    ) {
      throw new Error(
        'Ocean standing-water state contains an invalid surface elevation.',
      )
    }

    if (
      oceanSurfaceElevationMeters ===
      undefined
    ) {
      oceanSurfaceElevationMeters =
        elevation

      continue
    }

    const comparisonScale =
      Math.max(
        1,
        Math.abs(
          oceanSurfaceElevationMeters,
        ),
        Math.abs(
          elevation,
        ),
      )

    const toleranceMeters =
      Math.max(
        0.000001,
        comparisonScale *
          0.000000000001,
      )

    if (
      Math.abs(
        elevation -
          oceanSurfaceElevationMeters,
      ) >
      toleranceMeters
    ) {
      throw new Error(
        'Ocean standing-water cells do not share one authoritative surface elevation.',
      )
    }
  }

  return oceanSurfaceElevationMeters
}

export function createStandingWaterMesh(
  scene: Scene,
  options: StandingWaterMeshOptions,
): StandingWaterMeshResult {
  const {
    meanRadiusMeters,
    surface,
    standingWater,
  } =
    options

  const sphereSubdivisions =
    options.sphereSubdivisions ??
    64

  if (
    !Number.isFinite(
      meanRadiusMeters,
    ) ||
    meanRadiusMeters <= 0
  ) {
    throw new Error(
      'Standing water requires a valid planetary mean radius.',
    )
  }

  if (
    surface.planetId !==
    standingWater.planetId
  ) {
    throw new Error(
      'Standing-water state does not match the active surface.',
    )
  }

  if (
    !gridsMatch(
      surface,
      standingWater,
    )
  ) {
    throw new Error(
      'Standing-water state does not match the active surface grid.',
    )
  }

  const oceanSurfaceElevationMeters =
    readOceanSurfaceElevationMeters(
      standingWater,
    )

  if (
    oceanSurfaceElevationMeters ===
    undefined
  ) {
    return {
      mesh: undefined,
      floodedTriangleCount: 0,
      waterBodyCount:
        standingWater
          .waterBodies.length,
    }
  }

  const oceanRadius =
    1 +
    oceanSurfaceElevationMeters /
      meanRadiusMeters

  if (
    !Number.isFinite(
      oceanRadius,
    ) ||
    oceanRadius <= 0
  ) {
    throw new Error(
      'Ocean standing-water state produced an invalid render radius.',
    )
  }

  /*
   * One continuous renderer-resolution ocean shell.
   *
   * Authoritative displaced terrain determines visible shoreline coverage by
   * naturally occluding the shell wherever terrain rises above sea level.
   */
  const waterVertexData =
    CreateIcoSphereVertexData({
      radius:
        oceanRadius,

      subdivisions:
        sphereSubdivisions,

      flat: false,
    })

  const sourcePositions =
    waterVertexData.positions

  const sourceNormals =
    waterVertexData.normals

  const sourceIndices =
    waterVertexData.indices

  if (
    sourcePositions ===
      undefined ||
    sourcePositions ===
      null ||
    sourceNormals ===
      undefined ||
    sourceNormals ===
      null ||
    sourceIndices ===
      undefined ||
    sourceIndices ===
      null
  ) {
    throw new Error(
      'Babylon did not produce complete ocean-shell sphere data.',
    )
  }

  if (
    sourceIndices.length %
      3 !==
    0
  ) {
    throw new Error(
      'Ocean-shell sphere contains incomplete triangle data.',
    )
  }

  const floodedTriangleCount =
    sourceIndices.length /
    3

  const mesh =
    new Mesh(
      'est-standing-water',
      scene,
    )

  waterVertexData.applyToMesh(
    mesh,
    false,
  )

  const material =
    new StandardMaterial(
      'est-standing-water-material',
      scene,
    )

  material.diffuseColor =
    new Color3(
      0.025,
      0.180,
      0.320,
    )

  /*
   * Disable direct specular entirely. The previous point highlight
   * read as an artificial circular hotspot on the ocean surface.
   */
  material.specularColor =
    new Color3(
      0.0,
      0.0,
      0.0,
    )

  material.emissiveColor =
    new Color3(
      0.012,
      0.060,
      0.105,
    )

  /*
   * Give the ocean a restrained grazing-angle lift so the spherical
   * surface reads as curved without introducing another light hotspot.
   */
  const emissiveFresnel =
    new FresnelParameters()

  emissiveFresnel.leftColor =
    new Color3(
      0.030,
      0.160,
      0.280,
    )

  emissiveFresnel.rightColor =
    new Color3(
      0.008,
      0.040,
      0.070,
    )

  emissiveFresnel.bias =
    0.02

  emissiveFresnel.power =
    3.0

  material.emissiveFresnelParameters =
    emissiveFresnel

  material.alpha = 1
  material.backFaceCulling = false

  mesh.material =
    material

  mesh.isPickable = false

  mesh.freezeWorldMatrix()

  return {
    mesh,
    floodedTriangleCount,
    waterBodyCount:
      standingWater
        .waterBodies.length,
  }
}
