import './style.css'

import {
  ArcRotateCamera,
  Color3,
  Color4,
  DirectionalLight,
  DynamicTexture,
  Engine,
  Mesh,
  Scene,
  StandardMaterial,
  Vector3,
  VertexData,
} from '@babylonjs/core'

import {
  sphereDirectionToGeographicDegrees,
  type CubeFace,
} from './planet/cube-sphere'

import {
  CreateIcoSphereVertexData,
} from '@babylonjs/core/Meshes/Builders/icoSphereBuilder.js'

import {
  CreatePlane,
} from '@babylonjs/core/Meshes/Builders/planeBuilder.js'

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
  createStandingWaterMesh,
} from './surface/standing-water-mesh'

import {
  defaultPlanetaryLighting,
  planetaryLightRayDirection,
} from './planet/planetary-lighting'


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
    <span id="lodStatus">building spherical terrain…</span>
    <span id="faunaStatus">fauna symbols waiting for simulation session…</span>
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
camera.minZ = 0.05
camera.maxZ = 20

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

const faunaStatus =
  document.querySelector<HTMLSpanElement>(
    '#faunaStatus',
  )

interface SphereDirection {
  readonly x: number
  readonly y: number
  readonly z: number
}

type TerrainRadialOffset =
  (direction: SphereDirection) => number

let terrainRadialOffset:
  TerrainRadialOffset | undefined

let terrainSurfaceMaterial:
  ReturnType<
    typeof createTerrainShaderMaterial
  > | undefined

// Maximum radial extent of geometry that the camera must preserve.
// The unit sphere is the fallback when no simulation session is active.
let maximumPlanetRenderRadius = 1

const wolfAdultSymbolUrl =
  new URL(
    './assets/fauna/wolf-adult.svg',
    import.meta.url,
  ).href

const wolfPupSymbolUrl =
  new URL(
    './assets/fauna/wolf-pup.svg',
    import.meta.url,
  ).href

const maleBadgeSymbolUrl =
  new URL(
    './assets/fauna/badge-male.svg',
    import.meta.url,
  ).href

const femaleBadgeSymbolUrl =
  new URL(
    './assets/fauna/badge-female.svg',
    import.meta.url,
  ).href

const pregnantBadgeSymbolUrl =
  new URL(
    './assets/fauna/badge-pregnant.svg',
    import.meta.url,
  ).href

function geographicDegreesToSphereDirection(
  latitudeDegrees: number,
  longitudeDegrees: number,
): Vector3 {
  const degreesToRadians =
    Math.PI / 180

  const latitudeRadians =
    latitudeDegrees *
    degreesToRadians

  const longitudeRadians =
    longitudeDegrees *
    degreesToRadians

  const cosLatitude =
    Math.cos(latitudeRadians)

  return new Vector3(
    cosLatitude *
      Math.cos(longitudeRadians),
    cosLatitude *
      Math.sin(longitudeRadians),
    Math.sin(latitudeRadians),
  )
}

function createFaunaSymbolMaterial(
  name: string,
  _url: string,
): StandardMaterial {
  const material =
    new StandardMaterial(
      name,
      scene,
    )

  const texture =
    new DynamicTexture(
      `${name}-texture`,
      {
        width: 128,
        height: 128,
      },
      scene,
      false,
    )

  const context =
    texture.getContext()

  const isAdultWolf =
    name ===
    'wolf-adult-symbol-material'

  const isPup =
    name ===
    'wolf-pup-symbol-material'

  const isFemaleBadge =
    name ===
    'wolf-female-badge-material'

  const isMaleBadge =
    name ===
    'wolf-male-badge-material'

  const isPregnantBadge =
    name ===
    'wolf-pregnant-badge-material'

  if (
    isAdultWolf ||
    isPup
  ) {
    context.clearRect(
      0,
      0,
      128,
      128,
    )

    context.fillStyle =
      isPup
        ? '#dbeafe'
        : '#ffffff'

    /*
     * High-contrast symbolic wolf head.
     * This deliberately uses no external image/SVG decoding.
     */
    context.beginPath()

    context.moveTo(
      24,
      40,
    )

    context.lineTo(
      36,
      10,
    )

    context.lineTo(
      53,
      34,
    )

    context.lineTo(
      75,
      34,
    )

    context.lineTo(
      92,
      10,
    )

    context.lineTo(
      104,
      40,
    )

    context.lineTo(
      94,
      86,
    )

    context.lineTo(
      64,
      116,
    )

    context.lineTo(
      34,
      86,
    )

    context.closePath()
    context.fill()

    context.fillStyle =
      '#111827'

    context.beginPath()

    context.arc(
      49,
      61,
      5,
      0,
      Math.PI * 2,
    )

    context.arc(
      79,
      61,
      5,
      0,
      Math.PI * 2,
    )

    context.fill()

    context.beginPath()

    context.moveTo(
      55,
      87,
    )

    context.lineTo(
      73,
      87,
    )

    context.lineTo(
      64,
      99,
    )

    context.closePath()
    context.fill()
  } else {
    context.clearRect(
      0,
      0,
      128,
      128,
    )

    context.fillStyle =
      isFemaleBadge
        ? '#db2777'
        : isMaleBadge
          ? '#2563eb'
          : isPregnantBadge
            ? '#d97706'
            : '#475569'

    context.beginPath()

    context.arc(
      64,
      64,
      54,
      0,
      Math.PI * 2,
    )

    context.fill()

    context.strokeStyle =
      '#ffffff'

    context.lineWidth =
      8

    context.stroke()

    context.fillStyle =
      '#ffffff'

    context.font =
      'bold 82px sans-serif'

    context.textAlign =
      'center'

    context.textBaseline =
      'middle'

    context.fillText(
      isFemaleBadge
        ? 'F'
        : isMaleBadge
          ? 'M'
          : isPregnantBadge
            ? 'P'
            : '?',
      64,
      68,
    )
  }

  texture.hasAlpha =
    true

  texture.update()

  material.diffuseTexture =
    texture

  material.emissiveTexture =
    texture

  material.useAlphaFromDiffuseTexture =
    true

  material.emissiveColor =
    Color3.White()

  material.specularColor =
    Color3.Black()

  material.disableLighting =
    true

  material.backFaceCulling =
    false

  return material
}

const wolfAdultSymbolMaterial =
  createFaunaSymbolMaterial(
    'wolf-adult-symbol-material',
    wolfAdultSymbolUrl,
  )

const wolfPupSymbolMaterial =
  createFaunaSymbolMaterial(
    'wolf-pup-symbol-material',
    wolfPupSymbolUrl,
  )

const maleBadgeSymbolMaterial =
  createFaunaSymbolMaterial(
    'wolf-male-badge-material',
    maleBadgeSymbolUrl,
  )

const femaleBadgeSymbolMaterial =
  createFaunaSymbolMaterial(
    'wolf-female-badge-material',
    femaleBadgeSymbolUrl,
  )

const pregnantBadgeSymbolMaterial =
  createFaunaSymbolMaterial(
    'wolf-pregnant-badge-material',
    pregnantBadgeSymbolUrl,
  )

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
    standingWater,
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
      api.getPlanetStandingWater(
        sessionId,
        planet.planetId,
      ),
    ])

  if (
    surface.planetId !==
      planet.planetId ||
    terrain.planetId !==
      planet.planetId ||
    standingWater.planetId !==
      planet.planetId
  ) {
    throw new Error(
      'Authoritative planetary responses do not match the active planet.',
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

  createStandingWaterMesh(
    scene,
    {
      meanRadiusMeters,
      surface,
      standingWater,
      sphereSubdivisions: 64,
    },
  )

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

  maximumPlanetRenderRadius =
    maximumRenderRadius

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

  const wolfJuvenileDisplayAgeSeconds =
    180 * 86_400

  const wolfSymbolRadialLift =
    0.006

  const visibleWolves =
    world.animals.filter(
      animal =>
        animal.planetId ===
          planet.planetId &&
        animal.species === 'Wolf',
    )

  const faunaFocusRequested =
    new URLSearchParams(
      window.location.search,
    ).get('focus') === 'fauna'

  if (
    faunaFocusRequested &&
    visibleWolves.length > 0
  ) {
    const focusWolf =
      visibleWolves[0]

    const focusDirection =
      geographicDegreesToSphereDirection(
        focusWolf.latitudeDegrees,
        focusWolf.longitudeDegrees,
      )

    camera.setPosition(
      focusDirection.scale(
        3.2,
      ),
    )
  }

  const createBillboardPlane = (
    name: string,
    material: StandardMaterial,
    position: Vector3,
    size: number,
  ): Mesh => {
    const plane =
      CreatePlane(
        name,
        {
          width: size,
          height: size,
          sideOrientation:
            Mesh.DOUBLESIDE,
        },
        scene,
      )

    plane.position =
      position

    plane.material =
      material

    plane.billboardMode =
      Mesh.BILLBOARDMODE_ALL

    plane.isPickable =
      false

    plane.renderingGroupId =
      2

    return plane
  }

  for (const wolf of visibleWolves) {
    const direction =
      geographicDegreesToSphereDirection(
        wolf.latitudeDegrees,
        wolf.longitudeDegrees,
      )

    const terrainOffset =
      terrainRadialOffset(
        direction,
      )

    const symbolRadius =
      1 +
      terrainOffset +
      wolfSymbolRadialLift

    const position =
      direction.scale(
        symbolRadius,
      )

    const ageSeconds =
      Math.max(
        0,
        world.currentTimeSeconds -
          wolf.birthTimeSeconds,
      )

    const isJuvenile =
      ageSeconds <
      wolfJuvenileDisplayAgeSeconds

    const wolfSize =
      isJuvenile
        ? 0.022
        : 0.030

    createBillboardPlane(
      `wolf-${wolf.animalId}`,
      isJuvenile
        ? wolfPupSymbolMaterial
        : wolfAdultSymbolMaterial,
      position,
      wolfSize,
    )

    const longitudeRadians =
      wolf.longitudeDegrees *
      Math.PI / 180

    const latitudeRadians =
      wolf.latitudeDegrees *
      Math.PI / 180

    const east =
      new Vector3(
        -Math.sin(
          longitudeRadians,
        ),
        Math.cos(
          longitudeRadians,
        ),
        0,
      )

    const north =
      new Vector3(
        -Math.sin(
          latitudeRadians,
        ) *
          Math.cos(
            longitudeRadians,
          ),
        -Math.sin(
          latitudeRadians,
        ) *
          Math.sin(
            longitudeRadians,
          ),
        Math.cos(
          latitudeRadians,
        ),
      )

    const badgeRadius =
      symbolRadius +
      0.0015

    const badgeBase =
      direction.scale(
        badgeRadius,
      )

    const badgeOffset =
      isJuvenile
        ? 0.018
        : 0.024

    const sexMaterial =
      wolf.sex === 'Male'
        ? maleBadgeSymbolMaterial
        : wolf.sex === 'Female'
          ? femaleBadgeSymbolMaterial
          : undefined

    if (sexMaterial) {
      const sexPosition =
        badgeBase
          .add(
            east.scale(
              -badgeOffset,
            ),
          )
          .add(
            north.scale(
              badgeOffset,
            ),
          )

      createBillboardPlane(
        `wolf-${wolf.animalId}-sex`,
        sexMaterial,
        sexPosition,
        isJuvenile
          ? 0.010
          : 0.012,
      )
    }

    if (wolf.isPregnant) {
      const pregnancyPosition =
        badgeBase
          .add(
            east.scale(
              badgeOffset,
            ),
          )
          .add(
            north.scale(
              badgeOffset,
            ),
          )

      createBillboardPlane(
        `wolf-${wolf.animalId}-pregnant`,
        pregnantBadgeSymbolMaterial,
        pregnancyPosition,
        0.013,
      )
    }
  }

  if (faunaStatus) {
    const adultWolfCount =
      visibleWolves.filter(
        wolf =>
          Math.max(
            0,
            world.currentTimeSeconds -
              wolf.birthTimeSeconds,
          ) >=
          wolfJuvenileDisplayAgeSeconds,
      ).length

    const juvenileWolfCount =
      visibleWolves.length -
      adultWolfCount

    const pregnantWolfCount =
      visibleWolves.filter(
        wolf =>
          wolf.isPregnant,
      ).length

    faunaStatus.textContent =
      `wolves · ${visibleWolves.length} visible · ${adultWolfCount} adult · ${juvenileWolfCount} pup · ${pregnantWolfCount} pregnant${faunaFocusRequested ? ' · FAUNA FOCUS' : ''}`
  }

  if (terrainStatus) {
    terrainStatus.textContent =
      `authoritative terrain · ${terrain.cells.length.toLocaleString()} cells · ${minimumElevationMeters.toFixed(0)} to ${maximumElevationMeters.toFixed(0)} m`
  }
} else {
  if (terrainStatus) {
    terrainStatus.textContent =
      'unit sphere · no simulation session selected'
  }

  if (faunaStatus) {
    faunaStatus.textContent =
      'fauna symbols · no simulation session selected'
  }
}

// One immutable spherical planet.
//
// There are no render patches, cube faces, quadtree selections,
// topology changes, or camera-driven geometry updates here.
//
// The icosphere is generated once. Authoritative terrain is sampled
// once for each spherical vertex and baked into that fixed geometry.
const sphereSubdivisions = 64

const planetVertexData =
  CreateIcoSphereVertexData({
    radius: 1,
    subdivisions: sphereSubdivisions,
    flat: false,
  })

const sourcePositions =
  planetVertexData.positions

const planetIndices =
  planetVertexData.indices

if (
  !sourcePositions ||
  !planetIndices
) {
  throw new Error(
    'Babylon did not produce complete icosphere vertex data.',
  )
}

const planetPositions =
  Array.from(sourcePositions)

for (
  let index = 0;
  index < planetPositions.length;
  index += 3
) {
  const x =
    planetPositions[index]

  const y =
    planetPositions[index + 1]

  const z =
    planetPositions[index + 2]

  const length =
    Math.hypot(x, y, z)

  if (
    !Number.isFinite(length) ||
    length <= 0
  ) {
    throw new Error(
      'Icosphere contains an invalid vertex direction.',
    )
  }

  const direction: SphereDirection = {
    x: x / length,
    y: y / length,
    z: z / length,
  }

  const radialOffset =
    terrainRadialOffset?.(
      direction,
    ) ?? 0

  const radius =
    1 + radialOffset

  planetPositions[index] =
    direction.x * radius

  planetPositions[index + 1] =
    direction.y * radius

  planetPositions[index + 2] =
    direction.z * radius
}

const planetNormals =
  new Array<number>(
    planetPositions.length,
  ).fill(0)

VertexData.ComputeNormals(
  planetPositions,
  planetIndices,
  planetNormals,
)

planetVertexData.positions =
  planetPositions

planetVertexData.normals =
  planetNormals

const planetMesh =
  new Mesh(
    'est-planet-sphere',
    scene,
  )

planetVertexData.applyToMesh(
  planetMesh,
  false,
)

planetMesh.material =
  terrainSurfaceMaterial ??
  sharedSurface

planetMesh.isPickable = false

const lodStatus =
  document.querySelector<HTMLSpanElement>(
    '#lodStatus',
  )

if (lodStatus) {
  lodStatus.textContent =
    `1 immutable icosphere · ${sphereSubdivisions} subdivisions · no LOD`
}

// These older diagnostic values no longer affect rendering.
// Keep them temporarily until the diagnostic UI is cleaned up.
void showFaceDiagnostics
void diagnosticFaceColors

function updateCameraDepthRange(): void {
  const nearestPlanetDistance =
    Math.max(
      0,
      camera.radius -
        maximumPlanetRenderRadius,
    )

  // Keep the near plane comfortably in front of the planet while allowing it
  // to move outward as the camera retreats. This preserves depth precision
  // for shallow terrain/water separation at planetary viewing distances.
  camera.minZ =
    Math.max(
      0.05,
      nearestPlanetDistance *
        0.5,
    )

  // The farthest visible point is on the opposite limb of the planet.
  // A small margin keeps the bound safely outside all current geometry.
  camera.maxZ =
    camera.radius +
    maximumPlanetRenderRadius +
    0.5
}

engine.runRenderLoop(() => {
  updateCameraDepthRange()
  scene.render()
})

window.addEventListener(
  'resize',
  () => {
    engine.resize()
  },
)
