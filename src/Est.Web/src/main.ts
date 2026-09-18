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
  Texture,
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

// Terrain and standing water render in group 0.
// Preserve their depth buffer for presentation layers so objects on
// the far hemisphere cannot draw through the planet.
scene.setRenderingAutoClearDepthStencil(
  1,
  false,
)

scene.setRenderingAutoClearDepthStencil(
  2,
  false,
)

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


const reportPlanetStartupStage = (
  stage: string,
): void => {
  console.log(
    `[Est Babylon] ${stage}`,
  )

  if (terrainStatus) {
    terrainStatus.textContent =
      stage
  }
}

window.addEventListener(
  'error',
  event => {
    const message =
      event.error instanceof Error
        ? `${event.error.name}: ${event.error.message}`
        : event.message

    reportPlanetStartupStage(
      `ERROR · ${message}`,
    )

    console.error(
      '[Est Babylon] uncaught error',
      event.error ?? event.message,
    )
  },
)

window.addEventListener(
  'unhandledrejection',
  event => {
    const reason =
      event.reason instanceof Error
        ? `${event.reason.name}: ${event.reason.message}`
        : String(event.reason)

    reportPlanetStartupStage(
      `ERROR · ${reason}`,
    )

    console.error(
      '[Est Babylon] unhandled rejection',
      event.reason,
    )
  },
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

let vegetationCoverageData:
  Float32Array | undefined

let vegetationCoverageWidth = 1
let vegetationCoverageHeight = 1

// Maximum radial extent of geometry that the camera must preserve.
// The unit sphere is the fallback when no simulation session is active.
let maximumPlanetRenderRadius = 1

const humanManSpriteUrl =
  '/assets/sprites/humans/man.png'

const humanWomanSpriteUrl =
  '/assets/sprites/humans/woman.png'

const humanBoySpriteUrl =
  '/assets/sprites/humans/boy.png'

const humanGirlSpriteUrl =
  '/assets/sprites/humans/girl.png'

const maleWolfSpriteUrl =
  '/assets/sprites/wolves/male-wolf.png'

const femaleWolfSpriteUrl =
  '/assets/sprites/wolves/female-wolf.png'

const wolfPupSpriteUrl =
  '/assets/sprites/wolves/wolf-pup.png'

const birdFlockSpriteUrl =
  '/assets/sprites/birds/birds.png'

const grazerCohortSpriteUrl =
  '/assets/sprites/grazers/grazers.png'

const invertebrateSpriteUrl =
  '/assets/sprites/insects/insects.png'

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
    Math.cos(
      latitudeRadians,
    )

  return new Vector3(
    cosLatitude *
      Math.cos(
        longitudeRadians,
      ),
    cosLatitude *
      Math.sin(
        longitudeRadians,
      ),
    Math.sin(
      latitudeRadians,
    ),
  )
}

function createSpriteMaterial(
  name: string,
  url: string,
): StandardMaterial {
  const material =
    new StandardMaterial(
      name,
      scene,
    )

  const texture =
    new Texture(
      url,
      scene,
    )

  texture.hasAlpha =
    true

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

const humanManSpriteMaterial =
  createSpriteMaterial(
    'human-man-sprite-material',
    humanManSpriteUrl,
  )

const humanWomanSpriteMaterial =
  createSpriteMaterial(
    'human-woman-sprite-material',
    humanWomanSpriteUrl,
  )

const humanBoySpriteMaterial =
  createSpriteMaterial(
    'human-boy-sprite-material',
    humanBoySpriteUrl,
  )

const humanGirlSpriteMaterial =
  createSpriteMaterial(
    'human-girl-sprite-material',
    humanGirlSpriteUrl,
  )

const maleWolfSpriteMaterial =
  createSpriteMaterial(
    'male-wolf-sprite-material',
    maleWolfSpriteUrl,
  )

const femaleWolfSpriteMaterial =
  createSpriteMaterial(
    'female-wolf-sprite-material',
    femaleWolfSpriteUrl,
  )

const wolfPupSpriteMaterial =
  createSpriteMaterial(
    'wolf-pup-sprite-material',
    wolfPupSpriteUrl,
  )

const birdFlockSpriteMaterial =
  createSpriteMaterial(
    'bird-flock-sprite-material',
    birdFlockSpriteUrl,
  )

const grazerCohortSpriteMaterial =
  createSpriteMaterial(
    'grazer-cohort-sprite-material',
    grazerCohortSpriteUrl,
  )

const invertebrateSpriteMaterial =
  createSpriteMaterial(
    'invertebrate-sprite-material',
    invertebrateSpriteUrl,
  )

if (sessionId) {
  reportPlanetStartupStage(
    'loading simulation world…',
  )

  const api =
    new EstApi('/api')

  const world =
    await api.getWorld(
      sessionId,
    )

  reportPlanetStartupStage(
    'world loaded · resolving planet…',
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

  reportPlanetStartupStage(
    'loading surface + terrain + standing water…',
  )

  const [
    surface,
    terrain,
    standingWater,
    vegetation,
    invertebrates,
    birdFlocks,
    grazerCohorts,
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
      api.getPlanetVegetation(
        sessionId,
        planet.planetId,
      ),
      api.getPlanetInvertebrates(
        sessionId,
        planet.planetId,
      ),
      api.getPlanetBirdFlocks(
        sessionId,
        planet.planetId,
      ),
      api.getPlanetGrazerCohorts(
        sessionId,
        planet.planetId,
      ),
    ])

  reportPlanetStartupStage(
    'planet data loaded · validating responses…',
  )

  if (
    surface.planetId !==
      planet.planetId ||
    terrain.planetId !==
      planet.planetId ||
    standingWater.planetId !==
      planet.planetId ||
    (
      vegetation !== null &&
      vegetation.planetId !==
        planet.planetId
    ) ||
    (
      invertebrates !== null &&
      invertebrates.planetId !==
        planet.planetId
    ) ||
    (
      birdFlocks !== null &&
      birdFlocks.planetId !==
        planet.planetId
    ) ||
    (
      grazerCohorts !== null &&
      grazerCohorts.planetId !==
        planet.planetId
    )
  ) {
    throw new Error(
      'Authoritative planetary responses do not match the active planet.',
    )
  }

  reportPlanetStartupStage(
    'building terrain height field…',
  )

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

  reportPlanetStartupStage(
    'building standing-water mesh…',
  )

  createStandingWaterMesh(
    scene,
    {
      meanRadiusMeters,
      surface,
      standingWater,
      sphereSubdivisions: 64,
    },
  )

  const vegetationCoverageGridWidth =
    vegetation?.grid.longitudeBandCount ??
    1

  const vegetationCoverageGridHeight =
    vegetation?.grid.latitudeBandCount ??
    1

  const vegetationCoverageGrid =
    new Float32Array(
      vegetationCoverageGridWidth *
        vegetationCoverageGridHeight,
    )

  if (vegetation !== null) {
    if (
      vegetation.grid.latitudeBandCount !==
        surface.grid.latitudeBandCount ||
      vegetation.grid.longitudeBandCount !==
        surface.grid.longitudeBandCount
    ) {
      throw new Error(
        'Vegetation and surface grids must match.',
      )
    }

    const surfaceCellsById =
      new Map(
        surface.cells.map(
          cell => [
            cell.cellId,
            cell,
          ] as const,
        ),
      )

    const maximumBiomass =
      vegetation.cells.reduce(
        (maximum, cell) =>
          Math.max(
            maximum,
            cell.liveBiomassKilogramsPerSquareMeter,
          ),
        0,
      )

    const assignedCoverageCells =
      new Set<number>()

    for (const cell of vegetation.cells) {
      const surfaceCell =
        surfaceCellsById.get(
          cell.surfaceCellId,
        )

      if (!surfaceCell) {
        throw new Error(
          `Vegetation cell ${cell.surfaceCellId} has no matching surface cell.`,
        )
      }

      const row =
        Math.min(
          vegetationCoverageGridHeight - 1,
          Math.max(
            0,
            Math.floor(
              (
                (
                  surfaceCell.centerLatitudeDegrees +
                  90
                ) /
                180
              ) *
                vegetationCoverageGridHeight,
            ),
          ),
        )

      const normalizedLongitude =
        (
          (
            surfaceCell.centerLongitudeDegrees +
            180
          ) %
            360 +
          360
        ) %
        360

      const column =
        Math.min(
          vegetationCoverageGridWidth - 1,
          Math.max(
            0,
            Math.floor(
              (
                normalizedLongitude /
                360
              ) *
                vegetationCoverageGridWidth,
            ),
          ),
        )

      const coverageIndex =
        row *
          vegetationCoverageGridWidth +
        column

      if (
        assignedCoverageCells.has(
          coverageIndex,
        )
      ) {
        throw new Error(
          `Multiple vegetation cells map to coverage cell ${coverageIndex}.`,
        )
      }

      assignedCoverageCells.add(
        coverageIndex,
      )

      const biomass =
        cell.liveBiomassKilogramsPerSquareMeter

      vegetationCoverageGrid[
        coverageIndex
      ] =
        biomass <= 0 ||
        maximumBiomass <= 0
          ? 0
          : Math.min(
              1,
              biomass /
                maximumBiomass,
            )
    }

    if (
      assignedCoverageCells.size !==
      vegetation.cells.length
    ) {
      throw new Error(
        'Vegetation coverage grid did not receive every authoritative cell.',
      )
    }
  }

  vegetationCoverageData =
    vegetationCoverageGrid

  vegetationCoverageWidth =
    vegetationCoverageGridWidth

  vegetationCoverageHeight =
    vegetationCoverageGridHeight

  reportPlanetStartupStage(
    'creating terrain material…',
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

  const secondsPerYear =
    31_536_000

  const humanAdultDisplayAgeSeconds =
    18 * secondsPerYear

  const wolfJuvenileDisplayAgeSeconds =
    180 * 86_400

  const livingSymbolRadialLift =
    0.006

  const visiblePopulation =
    world.population.filter(
      person =>
        person.planetId ===
        planet.planetId,
    )

  const visibleWolves =
    world.animals.filter(
      animal =>
        animal.planetId ===
          planet.planetId &&
        animal.species === 'Wolf',
    )

  const visibleBirdFlocks =
    birdFlocks?.flocks ?? []

  const visibleGrazerCohorts =
    grazerCohorts?.cohorts ?? []

  const surfaceCellById =
    new Map(
      surface.cells.map(
        cell => [
          cell.cellId,
          cell,
        ] as const,
      ),
    )

  const activeVegetationCells =
    vegetation === null
      ? []
      : [...vegetation.cells]
          .filter(
            cell =>
              cell.liveBiomassKilogramsPerSquareMeter >
              0,
          )
          .sort(
            (left, right) =>
              right.liveBiomassKilogramsPerSquareMeter -
              left.liveBiomassKilogramsPerSquareMeter,
          )

  const activeInvertebrateCells =
    invertebrates === null
      ? []
      : [...invertebrates.cells]
          .filter(
            cell =>
              cell.liveBiomassKilogramsPerSquareMeter >
              0,
          )
          .sort(
            (left, right) =>
              right.liveBiomassKilogramsPerSquareMeter -
              left.liveBiomassKilogramsPerSquareMeter,
          )

  const selectSpatiallyDistributedInvertebrateCells = (
    cells: typeof activeInvertebrateCells,
    maximumCount: number,
  ): typeof activeInvertebrateCells => {
    const candidates: Array<{
      cell:
        (typeof activeInvertebrateCells)[number]
      direction: Vector3
    }> = []

    for (const cell of cells) {
      const surfaceCell =
        surfaceCellById.get(
          cell.surfaceCellId,
        )

      if (!surfaceCell) {
        continue
      }

      candidates.push({
        cell,
        direction:
          geographicDegreesToSphereDirection(
            surfaceCell.centerLatitudeDegrees,
            surfaceCell.centerLongitudeDegrees,
          ),
      })
    }

    candidates.sort(
      (left, right) => {
        const biomassDifference =
          right.cell
            .liveBiomassKilogramsPerSquareMeter -
          left.cell
            .liveBiomassKilogramsPerSquareMeter

        if (biomassDifference !== 0) {
          return biomassDifference
        }

        return left.cell.surfaceCellId
          .localeCompare(
            right.cell.surfaceCellId,
          )
      },
    )

    if (
      candidates.length <=
      maximumCount
    ) {
      return candidates.map(
        candidate =>
          candidate.cell,
      )
    }

    const selected = [
      candidates[0],
    ]

    const remaining =
      candidates
        .slice(1)
        .map(
          candidate => ({
            ...candidate,
            minimumDistance:
              1 -
              Vector3.Dot(
                candidates[0].direction,
                candidate.direction,
              ),
          }),
        )

    const tolerance =
      1e-12

    while (
      selected.length <
        maximumCount &&
      remaining.length >
        0
    ) {
      let bestIndex =
        0

      for (
        let index = 1;
        index <
        remaining.length;
        index++
      ) {
        const candidate =
          remaining[index]

        const currentBest =
          remaining[bestIndex]

        if (
          candidate.minimumDistance >
          currentBest.minimumDistance +
            tolerance
        ) {
          bestIndex =
            index

          continue
        }

        if (
          Math.abs(
            candidate.minimumDistance -
              currentBest.minimumDistance,
          ) <= tolerance
        ) {
          const biomassDifference =
            candidate.cell
              .liveBiomassKilogramsPerSquareMeter -
            currentBest.cell
              .liveBiomassKilogramsPerSquareMeter

          if (
            biomassDifference >
            0 ||
            (
              biomassDifference ===
                0 &&
              candidate.cell
                .surfaceCellId <
                currentBest.cell
                  .surfaceCellId
            )
          ) {
            bestIndex =
              index
          }
        }
      }

      const next =
        remaining[
          bestIndex
        ]

      selected.push(
        next,
      )

      remaining.splice(
        bestIndex,
        1,
      )

      for (
        const candidate of
        remaining
      ) {
        const distance =
          1 -
          Vector3.Dot(
            next.direction,
            candidate.direction,
          )

        candidate.minimumDistance =
          Math.min(
            candidate.minimumDistance,
            distance,
          )
      }
    }

    return selected.map(
      candidate =>
        candidate.cell,
    )
  }

  const visibleInvertebrateCells =
    selectSpatiallyDistributedInvertebrateCells(
      activeInvertebrateCells,
      64,
    )

  const createBillboardPlane = (
    name: string,
    material: StandardMaterial,
    latitudeDegrees: number,
    longitudeDegrees: number,
    size: number,
    radialLift =
      livingSymbolRadialLift,
  ): Mesh => {
    const direction =
      geographicDegreesToSphereDirection(
        latitudeDegrees,
        longitudeDegrees,
      )

    const terrainOffset =
      terrainRadialOffset?.(
        direction,
      ) ?? 0

    const position =
      direction.scale(
        1 +
          terrainOffset +
          radialLift,
      )

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

  for (const person of visiblePopulation) {
    const ageSeconds =
      Math.max(
        0,
        world.currentTimeSeconds -
          person.birthTimeSeconds,
      )

    const isChild =
      ageSeconds <
      humanAdultDisplayAgeSeconds

    const material =
      isChild
        ? person.sex === 'Female'
          ? humanGirlSpriteMaterial
          : humanBoySpriteMaterial
        : person.sex === 'Female'
          ? humanWomanSpriteMaterial
          : humanManSpriteMaterial

    createBillboardPlane(
      `human-${person.personId}`,
      material,
      person.latitudeDegrees,
      person.longitudeDegrees,
      isChild
        ? 0.026
        : 0.032,
    )
  }

  for (const wolf of visibleWolves) {
    const ageSeconds =
      Math.max(
        0,
        world.currentTimeSeconds -
          wolf.birthTimeSeconds,
      )

    const isJuvenile =
      ageSeconds <
      wolfJuvenileDisplayAgeSeconds

    const material =
      isJuvenile
        ? wolfPupSpriteMaterial
        : wolf.sex === 'Female'
          ? femaleWolfSpriteMaterial
          : maleWolfSpriteMaterial

    createBillboardPlane(
      `wolf-${wolf.animalId}`,
      material,
      wolf.latitudeDegrees,
      wolf.longitudeDegrees,
      isJuvenile
        ? 0.028
        : 0.036,
    )
  }

  for (const flock of visibleBirdFlocks) {
    const scale =
      Math.min(
        0.055,
        0.030 +
          Math.log10(
            flock.memberCount + 1,
          ) *
            0.004,
      )

    createBillboardPlane(
      `bird-flock-${flock.flockId}`,
      birdFlockSpriteMaterial,
      flock.latitudeDegrees,
      flock.longitudeDegrees,
      scale,
      0.012,
    )
  }

  interface GrazerPresentationCluster {
    memberCount: number
    cohortCount: number
    weightedDirection: Vector3
  }

  const grazerLatitudeClusterDegrees =
    45

  const grazerLongitudeClusterDegrees =
    60

  const grazerClusterMap =
    new Map<
      string,
      GrazerPresentationCluster
    >()

  for (
    const cohort of visibleGrazerCohorts
  ) {
    const normalizedLongitude =
      (
        (
          cohort.longitudeDegrees +
          180
        ) %
          360 +
        360
      ) %
        360 -
      180

    const latitudeBin =
      Math.min(
        3,
        Math.max(
          0,
          Math.floor(
            (
              cohort.latitudeDegrees +
              90
            ) /
              grazerLatitudeClusterDegrees,
          ),
        ),
      )

    const longitudeBin =
      Math.min(
        5,
        Math.max(
          0,
          Math.floor(
            (
              normalizedLongitude +
              180
            ) /
              grazerLongitudeClusterDegrees,
          ),
        ),
      )

    const key =
      `${latitudeBin}:${longitudeBin}`

    const direction =
      geographicDegreesToSphereDirection(
        cohort.latitudeDegrees,
        cohort.longitudeDegrees,
      )

    const weightedDirection =
      direction.scale(
        cohort.memberCount,
      )

    const existing =
      grazerClusterMap.get(
        key,
      )

    if (existing) {
      existing.memberCount +=
        cohort.memberCount

      existing.cohortCount +=
        1

      existing.weightedDirection
        .addInPlace(
          weightedDirection,
        )
    } else {
      grazerClusterMap.set(
        key,
        {
          memberCount:
            cohort.memberCount,
          cohortCount: 1,
          weightedDirection,
        },
      )
    }
  }

  const grazerPresentationClusters =
    Array.from(
      grazerClusterMap.entries(),
    )
      .map(
        ([key, cluster]) => {
          const direction =
            cluster.weightedDirection
              .normalize()

          const coordinate =
            sphereDirectionToGeographicDegrees(
              direction,
            )

          return {
            key,
            memberCount:
              cluster.memberCount,
            cohortCount:
              cluster.cohortCount,
            latitudeDegrees:
              coordinate.latitudeDegrees,
            longitudeDegrees:
              coordinate.longitudeDegrees,
          }
        },
      )
      .sort(
        (left, right) =>
          right.memberCount -
          left.memberCount,
      )

  const totalGrazerMembers =
    visibleGrazerCohorts.reduce(
      (total, cohort) =>
        total +
        cohort.memberCount,
      0,
    )

  const formatLivingCount = (
    value: number,
  ): string => {
    if (value >= 1_000_000) {
      return (
        `${(
          value /
          1_000_000
        ).toFixed(1)}m`
      )
    }

    if (value >= 1_000) {
      return (
        `${(
          value /
          1_000
        ).toFixed(1)}k`
      )
    }

    return value.toLocaleString()
  }

  for (
    const cluster of
      grazerPresentationClusters
  ) {
    const scale =
      Math.min(
        0.068,
        0.046 +
          Math.log10(
            cluster.memberCount + 1,
          ) *
            0.0025,
      )

    createBillboardPlane(
      `grazer-cluster-${cluster.key}`,
      grazerCohortSpriteMaterial,
      cluster.latitudeDegrees,
      cluster.longitudeDegrees,
      scale,
    )
  }

  let renderedInvertebrateCells =
    0

  for (
    const cell of
      visibleInvertebrateCells
  ) {
    const surfaceCell =
      surfaceCellById.get(
        cell.surfaceCellId,
      )

    if (!surfaceCell) {
      continue
    }

    const scale =
      Math.min(
        0.040,
        0.020 +
          Math.log10(
            1 +
              cell.liveBiomassKilogramsPerSquareMeter *
                1_000,
          ) *
            0.004,
      )

    createBillboardPlane(
      `invertebrates-${cell.surfaceCellId}`,
      invertebrateSpriteMaterial,
      surfaceCell.centerLatitudeDegrees,
      surfaceCell.centerLongitudeDegrees,
      scale,
      0.009,
    )

    renderedInvertebrateCells +=
      1
  }

  const faunaFocusRequested =
    new URLSearchParams(
      window.location.search,
    ).get('focus') === 'fauna'

  if (faunaFocusRequested) {
    const target =
      visibleWolves[0] ??
      visiblePopulation[0] ??
      visibleGrazerCohorts[0] ??
      visibleBirdFlocks[0]

    if (
      target &&
      'latitudeDegrees' in target &&
      'longitudeDegrees' in target
    ) {
      const focusDirection =
        geographicDegreesToSphereDirection(
          target.latitudeDegrees,
          target.longitudeDegrees,
        )

      camera.setPosition(
        focusDirection.scale(
          3.2,
        ),
      )
    }
  }

  if (faunaStatus) {
    const juvenileWolfCount =
      visibleWolves.filter(
        wolf =>
          Math.max(
            0,
            world.currentTimeSeconds -
              wolf.birthTimeSeconds,
          ) <
          wolfJuvenileDisplayAgeSeconds,
      ).length

    faunaStatus.textContent =
      `living world · ${visiblePopulation.length} humans · ${visibleWolves.length} wolves (${juvenileWolfCount} pups) · ${visibleBirdFlocks.length} bird flocks · ${grazerPresentationClusters.length} grazer markers representing ${formatLivingCount(totalGrazerMembers)} grazers / ${visibleGrazerCohorts.length} cohorts · ${activeVegetationCells.length} active vegetation cells · ${renderedInvertebrateCells} spatial insect samples / ${activeInvertebrateCells.length} active cells${faunaFocusRequested ? ' · FAUNA FOCUS' : ''}`
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

  reportPlanetStartupStage(
    'building Babylon planet sphere…',
  )

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


const planetVegetationCoverage:
  number[] = []

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

  const coordinate =
    sphereDirectionToGeographicDegrees(
      direction,
    )

  const vegetationRow =
    Math.min(
      vegetationCoverageHeight - 1,
      Math.max(
        0,
        Math.floor(
          (
            (
              coordinate.latitudeDegrees +
              90
            ) /
            180
          ) *
            vegetationCoverageHeight,
        ),
      ),
    )

  const normalizedVegetationLongitude =
    (
      (
        coordinate.longitudeDegrees +
        180
      ) %
        360 +
      360
    ) %
    360

  const vegetationColumn =
    Math.min(
      vegetationCoverageWidth - 1,
      Math.max(
        0,
        Math.floor(
          (
            normalizedVegetationLongitude /
            360
          ) *
            vegetationCoverageWidth,
        ),
      ),
    )

  const vegetationIndex =
    vegetationRow *
      vegetationCoverageWidth +
    vegetationColumn

  planetVegetationCoverage.push(
    vegetationCoverageData?.[
      vegetationIndex
    ] ?? 0,
  )

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


planetMesh.setVerticesData(
  'vegetationCoverage',
  planetVegetationCoverage,
  false,
  1,
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
