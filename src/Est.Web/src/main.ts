import './style.css'

import {
  ArcRotateCamera,
  Color3,
  Color4,
  DirectionalLight,
  Engine,
  HemisphericLight,
  Mesh,
  Scene,
  StandardMaterial,
  Vector3,
  VertexData,
} from '@babylonjs/core'

import {
  createCubeSpherePatchGeometry,
  type CubeFace,
} from './planet/cube-sphere'

import {
  patchBounds,
  patchKey,
  selectPlanetPatches,
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
    <span>R2 · camera-driven quadtree LOD</span>
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

const sunlight =
  new DirectionalLight(
    'sunlight',
    new Vector3(
      -0.8,
      -0.35,
      0.6,
    ),
    scene,
  )

sunlight.intensity = 1.8

const ambient =
  new HemisphericLight(
    'ambient',
    new Vector3(0, 1, 0),
    scene,
  )

ambient.intensity = 0.35

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

const showFaceDiagnostics =
  new URLSearchParams(
    window.location.search,
  ).get('faces') === '1'

const segmentsPerPatch = 8

const lodOptions = {
  minimumLevel: 1,
  maximumLevel: 5,
  splitThreshold: 0.28,
} as const

const patchMeshes =
  new Map<string, Mesh>()

const diagnosticMaterials =
  new Map<
    string,
    StandardMaterial
  >()

function createPatchMaterial(
  patch: PlanetPatch,
): StandardMaterial {
  if (!showFaceDiagnostics) {
    return sharedSurface
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

function synchronizePlanetPatches(): void {
  const selected =
    selectPlanetPatches(
      {
        x: camera.position.x,
        y: camera.position.y,
        z: camera.position.z,
      },
      lodOptions,
    )

  const keys =
    selected.map(patchKey)

  const selectionSignature =
    keys.join('|')

  if (
    selectionSignature ===
    previousSelection
  ) {
    return
  }

  previousSelection =
    selectionSignature

  const selectedKeys =
    new Set(keys)

  for (
    const [
      key,
      mesh,
    ] of patchMeshes
  ) {
    if (
      selectedKeys.has(key)
    ) {
      continue
    }

    mesh.dispose()
    patchMeshes.delete(key)
  }

  for (const patch of selected) {
    const key =
      patchKey(patch)

    if (
      patchMeshes.has(key)
    ) {
      continue
    }

    patchMeshes.set(
      key,
      createPatchMesh(patch),
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
      `${selected.length} patches · L${minimum}–L${maximum}`
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
