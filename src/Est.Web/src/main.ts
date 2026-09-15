import './style.css'

import {
  ArcRotateCamera,
  Color3,
  Color4,
  DirectionalLight,
  Engine,
  Mesh,
  Scene,
  StandardMaterial,
  Vector3,
  VertexData,
} from '@babylonjs/core'

import {
  createCubeSpherePatchGeometry,
  sphereDirectionToGeographicDegrees,
  type CubeFace,
  type CubeSphereRadialOffset,
  type CubeSphereSurfaceNormal,
} from './planet/cube-sphere'

import {
  EstApi,
} from './api/est-api'

import {
  createTerrainHeightField,
} from './surface/terrain-height-field'

import {
  createTerrainShaderMaterial,
} from './surface/terrain-material'

import {
  defaultPlanetaryLighting,
  planetaryLightRayDirection,
} from './planet/planetary-lighting'

import {
  findPlanetPatchStitchEdges,
  patchBounds,
  patchKey,
  selectBalancedPlanetPatches,
  type PlanetPatch,
} from './planet/planet-quadtree'

const app =
  document.querySelector<HTMLDivElement>(
    '#app',
  )

if (!app) {
  throw new Error(
    'Application root was not found.',
  )
}

app.innerHTML = `
  <canvas
    id="renderCanvas"
    aria-label="Est planetary renderer"
  ></canvas>

  <div class="planet-foundation-status">
    <strong>Est Planet Renderer</strong>
    <span>R4 · procedural terrain materials</span>
    <span id="terrainStatus">preparing terrain…</span>
    <span id="lodStatus">selecting patches…</span>
    <span>drag to orbit · wheel to zoom</span>
  </div>
`

const canvas =
  document.querySelector<HTMLCanvasElement>(
    '#renderCanvas',
  )

if (!canvas) {
  throw new Error(
    'Render canvas was not created.',
  )
}

const engine = new Engine(
  canvas,
  true,
  {
    preserveDrawingBuffer: true,
    stencil: true,
  },
)

const scene = new Scene(engine)

scene.clearColor =
  new Color4(
    0.002,
    0.004,
    0.009,
    1,
  )

const camera =
  new ArcRotateCamera(
    'planet-camera',
    Math.PI * 1.25,
    Math.PI * 0.40,
    3.2,
    Vector3.Zero(),
    scene,
  )

camera.attachControl(
  canvas,
  true,
)

camera.lowerRadiusLimit = 1.18
camera.upperRadiusLimit = 12
camera.wheelPrecision = 35
camera.panningSensibility = 0
camera.inertia = 0.82
camera.minZ = 0.01

const planetaryLighting =
  defaultPlanetaryLighting

const sunlightRayDirection =
  planetaryLightRayDirection(
    planetaryLighting,
  )

const sunlight =
  new DirectionalLight(
    'sunlight',
    new Vector3(
      sunlightRayDirection.x,
      sunlightRayDirection.y,
      sunlightRayDirection.z,
    ),
    scene,
  )

// The production terrain shader uses the same Est-owned
// planetary light state directly. This Babylon light remains
// useful for diagnostic StandardMaterials.
sunlight.intensity = 1.0

const sharedSurface =
  new StandardMaterial(
    'cube-sphere-surface',
    scene,
  )

sharedSurface.diffuseColor =
  new Color3(
    0.12,
    0.36,
    0.58,
  )

sharedSurface.emissiveColor =
  new Color3(
    0.006,
    0.018,
    0.03,
  )

sharedSurface.specularColor =
  Color3.Black()

const diagnosticFaceColors:
  Record<CubeFace, Color3> = {
    positiveX: new Color3(
      0.50,
      0.20,
      0.18,
    ),
    negativeX: new Color3(
      0.18,
      0.42,
      0.60,
    ),
    positiveY: new Color3(
      0.24,
      0.50,
      0.24,
    ),
    negativeY: new Color3(
      0.54,
      0.42,
      0.16,
    ),
    positiveZ: new Color3(
      0.34,
      0.24,
      0.56,
    ),
    negativeZ: new Color3(
      0.18,
      0.48,
      0.48,
    ),
  }

const queryParameters =
  new URLSearchParams(
    window.location.search,
  )

const showFaceDiagnostics =
  queryParameters.get('faces') === '1'

const sessionId =
  queryParameters.get('session')

const terrainStatus =
  document.querySelector<HTMLSpanElement>(
    '#terrainStatus',
  )

let terrainRadialOffset:
  CubeSphereRadialOffset | undefined

let terrainNormalAtDirection:
  CubeSphereSurfaceNormal | undefined

let terrainSurfaceMaterial:
  ReturnType<
    typeof createTerrainShaderMaterial
  > | undefined

if (sessionId) {
  const api =
    new EstApi('/api')

  const world =
    await api.getWorld(
      sessionId,
    )

  const planet =
    world.planets[0]

  if (!planet) {
    throw new Error(
      'The Est session contains no planet to render.',
    )
  }

  if (
    !Number.isFinite(
      planet.meanRadiusMeters,
    ) ||
    planet.meanRadiusMeters <= 0
  ) {
    throw new Error(
      'The active planet has an invalid mean radius.',
    )
  }

  const [
    surface,
    terrain,
  ] =
    await Promise.all([
      api.getPlanetSurface(
        sessionId,
        planet.planetId,
      ),
      api.getPlanetTerrain(
        sessionId,
        planet.planetId,
      ),
    ])

  if (
    surface.planetId !==
      planet.planetId ||
    terrain.planetId !==
      planet.planetId
  ) {
    throw new Error(
      'Authoritative terrain responses do not match the active planet.',
    )
  }

  const heightField =
    createTerrainHeightField(
      surface,
      terrain,
    )

  const elevations =
    terrain.cells.map(
      cell =>
        cell.elevationMeters,
    )

  const minimumElevationMeters =
    Math.min(...elevations)

  const maximumElevationMeters =
    Math.max(...elevations)

  const meanRadiusMeters =
    planet.meanRadiusMeters

  terrainSurfaceMaterial =
    createTerrainShaderMaterial(
      scene,
      {
        meanRadiusMeters,
        minimumElevationMeters,
        maximumElevationMeters,
        lighting:
          planetaryLighting,
      },
    )

  const minimumRenderRadius =
    1 +
    minimumElevationMeters /
      meanRadiusMeters

  const maximumRenderRadius =
    1 +
    maximumElevationMeters /
      meanRadiusMeters

  if (
    !Number.isFinite(
      minimumRenderRadius,
    ) ||
    minimumRenderRadius <= 0 ||
    !Number.isFinite(
      maximumRenderRadius,
    ) ||
    maximumRenderRadius <
      minimumRenderRadius
  ) {
    throw new Error(
      'Authoritative terrain produces invalid planetary radius bounds.',
    )
  }

  terrainRadialOffset =
    direction => {
      const coordinate =
        sphereDirectionToGeographicDegrees(
          direction,
        )

      return (
        heightField.sampleHeightMeters(
          coordinate.latitudeDegrees,
          coordinate.longitudeDegrees,
        ) /
        meanRadiusMeters
      )
    }

  terrainNormalAtDirection =
    direction => {
      const coordinate =
        sphereDirectionToGeographicDegrees(
          direction,
        )

      const sample =
        heightField.sampleSurfaceMeters(
          coordinate.latitudeDegrees,
          coordinate.longitudeDegrees,
        )

      const renderRadius =
        1 +
        sample.elevationMeters /
          meanRadiusMeters

      const gradientScale =
        meanRadiusMeters *
        renderRadius

      return {
        x:
          direction.x -
          sample.gradientMetersPerUnit.x /
            gradientScale,
        y:
          direction.y -
          sample.gradientMetersPerUnit.y /
            gradientScale,
        z:
          direction.z -
          sample.gradientMetersPerUnit.z /
            gradientScale,
      }
    }

  if (terrainStatus) {
    terrainStatus.textContent =
      `authoritative terrain · ${terrain.cells.length.toLocaleString()} cells · ${minimumElevationMeters.toFixed(0)} to ${maximumElevationMeters.toFixed(0)} m`
  }
} else if (terrainStatus) {
  terrainStatus.textContent =
    'unit sphere · no simulation session selected'
}

const segmentsPerPatch = 8

const lodOptions = {
  minimumLevel: 1,
  maximumLevel: 5,
  splitThreshold: 0.28,
} as const

const patchMeshes =
  new Map<string, Mesh>()

const patchMeshStitches =
  new Map<string, string>()

const diagnosticMaterials =
  new Map<
    string,
    StandardMaterial
  >()

function createPatchMaterial(
  patch: PlanetPatch,
) {
  if (!showFaceDiagnostics) {
    return (
      terrainSurfaceMaterial ??
      sharedSurface
    )
  }

  const parity =
    (patch.x + patch.y) % 2

  const materialKey = [
    patch.face,
    patch.level,
    parity,
  ].join(':')

  const existing =
    diagnosticMaterials.get(
      materialKey,
    )

  if (existing) {
    return existing
  }

  const base =
    diagnosticFaceColors[
      patch.face
    ]

  const levelFactor =
    0.62 +
    patch.level * 0.075

  const checkerFactor =
    parity === 0
      ? 0.86
      : 1.08

  const material =
    new StandardMaterial(
      `cube-sphere-${materialKey}-material`,
      scene,
    )

  material.diffuseColor =
    base.scale(
      Math.min(
        1.15,
        levelFactor *
          checkerFactor,
      ),
    )

  material.emissiveColor =
    base.scale(0.025)

  material.specularColor =
    Color3.Black()

  diagnosticMaterials.set(
    materialKey,
    material,
  )

  return material
}

function createPatchMesh(
  patch: PlanetPatch,
  stitchEdges: Record<
    'left' | 'right' | 'bottom' | 'top',
    boolean
  >,
): Mesh {
  const bounds =
    patchBounds(patch)

  const geometry =
    createCubeSpherePatchGeometry(
      patch.face,
      segmentsPerPatch,
      bounds.uMin,
      bounds.uMax,
      bounds.vMin,
      bounds.vMax,
      stitchEdges,
      terrainRadialOffset,
      terrainNormalAtDirection,
    )

  const mesh =
    new Mesh(
      `cube-sphere-${patchKey(patch)}`,
      scene,
    )

  const vertexData =
    new VertexData()

  vertexData.positions =
    geometry.positions

  vertexData.indices =
    geometry.indices

  vertexData.normals =
    geometry.normals

  vertexData.applyToMesh(
    mesh,
    false,
  )

  mesh.material =
    createPatchMaterial(patch)

  mesh.isPickable = false

  return mesh
}

const lodStatus =
  document.querySelector<HTMLSpanElement>(
    '#lodStatus',
  )

let previousSelection = ''

let stitchEdgesByPatch =
  new Map<
    string,
    Record<
      'left' | 'right' | 'bottom' | 'top',
      boolean
    >
  >()

function synchronizePlanetPatches(): void {
  const cameraPoint = {
    x: camera.position.x,
    y: camera.position.y,
    z: camera.position.z,
  }

  const selected =
    selectBalancedPlanetPatches(
      cameraPoint,
      lodOptions,
    )

  const keys =
    selected.map(patchKey)

  const selectionSignature =
    keys.join('|')

  const selectionChanged =
    selectionSignature !==
    previousSelection

  if (selectionChanged) {
    previousSelection =
      selectionSignature

    stitchEdgesByPatch =
      findPlanetPatchStitchEdges(
        selected,
        lodOptions.maximumLevel,
      )
  }

  if (!selectionChanged) {
    return
  }

  // Babylon performs native per-mesh frustum culling.
  //
  // Keep the complete balanced patch selection resident here rather than
  // deleting patches through renderer-owned visibility approximations.
  const visible =
    selected

  const visibleKeys =
    keys

  const visibleKeySet =
    new Set(visibleKeys)

  for (
    const [
      key,
      mesh,
    ] of patchMeshes
  ) {
    if (
      visibleKeySet.has(key)
    ) {
      continue
    }

    mesh.dispose()
    patchMeshes.delete(key)
    patchMeshStitches.delete(key)
  }

  for (const patch of visible) {
    const key =
      patchKey(patch)

    const stitchEdges =
      stitchEdgesByPatch.get(key) ?? {
        left: false,
        right: false,
        bottom: false,
        top: false,
      }

    const stitchSignature =
      `${+stitchEdges.left}${+stitchEdges.right}${+stitchEdges.bottom}${+stitchEdges.top}`

    const existingMesh =
      patchMeshes.get(key)

    if (
      existingMesh &&
      patchMeshStitches.get(key) ===
        stitchSignature
    ) {
      continue
    }

    existingMesh?.dispose()

    patchMeshes.set(
      key,
      createPatchMesh(
        patch,
        stitchEdges,
      ),
    )

    patchMeshStitches.set(
      key,
      stitchSignature,
    )
  }

  if (lodStatus) {
    const levels =
      selected.map(
        (patch) =>
          patch.level,
      )

    const minimum =
      Math.min(...levels)

    const maximum =
      Math.max(...levels)

    lodStatus.textContent =
      `${selected.length} selected · Babylon native frustum · L${minimum}–L${maximum}`
  }
}

synchronizePlanetPatches()

engine.runRenderLoop(() => {
  synchronizePlanetPatches()
  scene.render()
})

window.addEventListener(
  'resize',
  () => {
    engine.resize()
  },
)
