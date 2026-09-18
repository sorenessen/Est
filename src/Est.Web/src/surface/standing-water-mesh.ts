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
  surface: SurfaceResponse,
  standingWater: StandingWaterResponse,
): number | undefined {
  const surfaceAreaByCellId =
    new Map(
      surface.cells.map(
        cell => [
          cell.cellId,
          cell.areaSquareMeters,
        ] as const,
      ),
    )

  let weightedElevationMeters =
    0

  let totalAreaSquareMeters =
    0

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

    const areaSquareMeters =
      surfaceAreaByCellId.get(
        cell.cellId,
      )

    if (
      areaSquareMeters === undefined ||
      !Number.isFinite(
        areaSquareMeters,
      ) ||
      areaSquareMeters <= 0
    ) {
      throw new Error(
        'Ocean standing-water state has no valid authoritative surface area.',
      )
    }

    weightedElevationMeters +=
      elevation *
      areaSquareMeters

    totalAreaSquareMeters +=
      areaSquareMeters
  }

  if (
    totalAreaSquareMeters <= 0
  ) {
    return undefined
  }

  const presentationElevationMeters =
    weightedElevationMeters /
    totalAreaSquareMeters

  if (
    !Number.isFinite(
      presentationElevationMeters,
    )
  ) {
    throw new Error(
      'Ocean standing-water state produced an invalid presentation elevation.',
    )
  }

  /*
   * Authoritative hydrology is cell-local and does not require every cell
   * in a connected ocean to have an identical instantaneous surface
   * elevation. Babylon presents that spatially varying state as one
   * continuous renderer-resolution shell, so use the surface-area-weighted
   * authoritative ocean elevation as the shell level.
   */
  return presentationElevationMeters
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
      surface,
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
