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
  CUBE_FACES,
  createCubeSphereFaceGeometry,
  type CubeFace,
} from './planet/cube-sphere'

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
    <span>R1 · six-face cube-sphere</span>
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

function createFaceMaterial(
  face: CubeFace,
): StandardMaterial {
  if (!showFaceDiagnostics) {
    return sharedSurface
  }

  const material =
    new StandardMaterial(
      `cube-sphere-${face}-material`,
      scene,
    )

  material.diffuseColor =
    diagnosticFaceColors[face]

  material.emissiveColor =
    diagnosticFaceColors[
      face
    ].scale(0.035)

  material.specularColor =
    Color3.Black()

  return material
}

const segmentsPerFace = 32

for (const face of CUBE_FACES) {
  const geometry =
    createCubeSphereFaceGeometry(
      face,
      segmentsPerFace,
    )

  const mesh =
    new Mesh(
      `cube-sphere-${face}`,
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
    createFaceMaterial(face)

  mesh.isPickable = false
}

engine.runRenderLoop(() => {
  scene.render()
})

window.addEventListener(
  'resize',
  () => {
    engine.resize()
  },
)
