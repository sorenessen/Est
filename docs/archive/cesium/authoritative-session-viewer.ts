import 'cesium/Build/Cesium/Widgets/widgets.css'
import '../style.css'

import {
  ArcType,
  Cartesian3,
  Color,
  ColorGeometryInstanceAttribute,
  GeometryInstance,
  GroundPrimitive,
  Material,
  PerInstanceColorAppearance,
  PointPrimitiveCollection,
  PolygonGeometry,
  PolygonHierarchy,
  Viewer,
} from 'cesium'

import {
  EstApi,
  type TerrainResponse,
} from '../api/est-api'
import {
  createEstTerrainPresentation,
} from './est-terrain-provider'

const queryParameters =
  new URLSearchParams(window.location.search)

const sessionId =
  queryParameters.get('session')

if (!sessionId) {
  throw new Error(
    'The Est session viewer requires a session query parameter.',
  )
}

const app =
  document.querySelector<HTMLDivElement>('#app')

if (!app) {
  throw new Error(
    'Application root was not found.',
  )
}

app.innerHTML = `
  <div
    id="cesiumContainer"
    aria-label="Est authoritative planet simulation"
  ></div>

  <section
    class="render-evaluation-panel"
    aria-label="Est simulation status"
  >
    <div class="render-evaluation-heading">
      <div class="render-evaluation-heading-copy">
        <strong>Est Observatory</strong>
        <div class="render-evaluation-mode">
          <span class="render-evaluation-mode-label">
            Mode:
          </span>
          <span>Authoritative Planet</span>
        </div>
      </div>
    </div>

    <div
      id="presentationStatus"
    >
      Loading authoritative terrain...
    </div>

    <style>
      .simulation-diagnostics {
        margin-top: 12px;
        border-top:
          1px solid rgba(255, 255, 255, 0.12);
        padding-top: 10px;
      }

      .simulation-diagnostics > summary {
        cursor: pointer;
        user-select: none;
        font-size: 11px;
        font-weight: 700;
        letter-spacing: 0.12em;
        text-transform: uppercase;
        opacity: 0.72;
      }

      .simulation-diagnostics[open] > summary {
        margin-bottom: 4px;
      }
    </style>

    <section
      id="sessionStatus"
      class="simulation-telemetry"
      aria-label="Simulation telemetry"
    >
      <div class="simulation-telemetry-empty">
        Loading simulation...
      </div>
    </section>
  </section>
`

const sessionStatus =
  document.querySelector<HTMLDivElement>(
    '#sessionStatus',
  )

const presentationStatus =
  document.querySelector<HTMLDivElement>(
    '#presentationStatus',
  )

if (
  !sessionStatus
  || !presentationStatus
) {
  throw new Error(
    'Est session status UI could not be initialized.',
  )
}

const api =
  new EstApi('/api')

const initialWorld =
  await api.getWorld(
    sessionId,
  )

const initialPlanet =
  initialWorld.planets[0]

if (!initialPlanet) {
  sessionStatus.textContent =
    'This simulation session has no planets.'

  throw new Error(
    'The Est session contains no planet to render.',
  )
}

const [
  initialSurface,
  initialTerrain,
] =
  await Promise.all([
    api.getPlanetSurface(
      sessionId,
      initialPlanet.planetId,
    ),
    api.getPlanetTerrain(
      sessionId,
      initialPlanet.planetId,
    ),
  ])

if (
  initialSurface.planetId
  !== initialPlanet.planetId
  || initialTerrain.planetId
  !== initialPlanet.planetId
) {
  throw new Error(
    'Authoritative terrain responses do not match the active planet.',
  )
}

const terrainPresentation =
  createEstTerrainPresentation(
    initialPlanet.meanRadiusMeters,
    initialSurface,
    initialTerrain,
  )

const planetEllipsoid =
  terrainPresentation.ellipsoid

function planetCartesianFromDegrees(
  longitudeDegrees: number,
  latitudeDegrees: number,
  heightMeters: number = 0,
): Cartesian3 {
  return Cartesian3.fromDegrees(
    longitudeDegrees,
    latitudeDegrees,
    heightMeters,
    planetEllipsoid,
  )
}

function createAuthoritativeTerrainMaterial(
  terrain: TerrainResponse,
): Material {
  const elevations =
    terrain.cells.map(
      cell => cell.elevationMeters,
    )

  const minimumElevation =
    Math.min(
      ...elevations,
    )

  const maximumElevation =
    Math.max(
      ...elevations,
    )

  const elevationRange =
    Math.max(
      maximumElevation -
      minimumElevation,
      1,
    )

  const displayMinimum =
    minimumElevation -
    elevationRange * 0.05

  const displayMaximum =
    maximumElevation +
    elevationRange * 0.05

  const ramp =
    document.createElement(
      'canvas',
    )

  ramp.width = 256
  ramp.height = 1

  const context =
    ramp.getContext('2d')

  if (!context) {
    throw new Error(
      'Could not create authoritative terrain color ramp.',
    )
  }

  const gradient =
    context.createLinearGradient(
      0,
      0,
      256,
      0,
    )

  gradient.addColorStop(
    0.00,
    '#172f3f',
  )

  gradient.addColorStop(
    0.22,
    '#38594d',
  )

  gradient.addColorStop(
    0.44,
    '#647052',
  )

  gradient.addColorStop(
    0.66,
    '#83775e',
  )

  gradient.addColorStop(
    0.84,
    '#9b9486',
  )

  gradient.addColorStop(
    1.00,
    '#d9d8d2',
  )

  context.fillStyle =
    gradient

  context.fillRect(
    0,
    0,
    ramp.width,
    ramp.height,
  )

  return Material.fromType(
    Material.ElevationRampType,
    {
      image: ramp,
      minimumHeight:
        displayMinimum,
      maximumHeight:
        displayMaximum,
    },
  )
}

const viewer =
  new Viewer(
    'cesiumContainer',
    {
      ellipsoid:
        planetEllipsoid,
      terrainProvider:
        terrainPresentation
          .terrainProvider,
      baseLayer: false,
      animation: false,
      baseLayerPicker: false,
      fullscreenButton: false,
      geocoder: false,
      homeButton: false,
      infoBox: false,
      navigationHelpButton: false,
      sceneModePicker: false,
      selectionIndicator: false,
      timeline: false,
    },
  )

const populationPoints =
  viewer.scene.primitives.add(
    new PointPrimitiveCollection(),
  )

const animalPoints =
  viewer.scene.primitives.add(
    new PointPrimitiveCollection(),
  )

const bloodEffectPoints =
  viewer.scene.primitives.add(
    new PointPrimitiveCollection(),
  )

viewer.scene.backgroundColor =
  Color.BLACK

viewer.scene.globe.enableLighting =
  true

viewer.scene.globe.depthTestAgainstTerrain =
  true

viewer.scene.globe.showGroundAtmosphere =
  true

viewer.scene.globe.dynamicAtmosphereLighting =
  true

viewer.scene.globe.preloadSiblings =
  true

viewer.scene.globe.material =
  createAuthoritativeTerrainMaterial(
    initialTerrain,
  )

viewer.camera.setView({
  destination:
    planetCartesianFromDegrees(
      25,
      0,
      initialPlanet.meanRadiusMeters
      * 2.8,
    ),
})

const minimumElevation =
  Math.min(
    ...initialTerrain.cells.map(
      cell => cell.elevationMeters,
    ),
  )

const maximumElevation =
  Math.max(
    ...initialTerrain.cells.map(
      cell => cell.elevationMeters,
    ),
  )

presentationStatus.textContent =
  `Authoritative terrain · ${initialTerrain.cells.length.toLocaleString()} cells`
  + ` · radius ${(initialPlanet.meanRadiusMeters / 1_000).toLocaleString(undefined, { maximumFractionDigits: 0 })} km`
  + ` · elevation ${minimumElevation.toFixed(0)} to ${maximumElevation.toFixed(0)} m`

function renderPopulation(
  population: Array<{
    personId: string
    planetId: string
    latitudeDegrees: number
    longitudeDegrees: number
    activity: string
    isPregnant: boolean
    energyReserve: number
    health: number
  }>,
  planetId: string,
) {
  populationPoints.removeAll()

  const visiblePopulation =
    population.filter(
      person => person.planetId === planetId,
    )

  const activityColor = (activity: string) => {
    switch (activity) {
      case 'Foraging':
        return Color.fromCssColorString('#ff9f1c')
      case 'Eating':
        return Color.fromCssColorString('#9be564')
      case 'Traveling':
        return Color.fromCssColorString('#4ddcff')
      case 'Fleeing':
        return Color.fromCssColorString('#ff4fd8')
      case 'SeekingPartner':
        return Color.fromCssColorString('#b388ff')
      case 'Mating':
        return Color.fromCssColorString('#ff6fae')
      case 'Idle':
      default:
        return Color.fromCssColorString('#ffd166')
    }
  }

  for (const person of visiblePopulation) {
    const energy =
      Math.max(0, Math.min(1, person.energyReserve))

    const health =
      Math.max(0, Math.min(1, person.health))

    const color =
      activityColor(person.activity)
        .withAlpha(0.72 + health * 0.28)

    const haloColor =
      person.isPregnant
        ? Color.fromCssColorString('#65e6c4')
            .withAlpha(0.42)
        : color.withAlpha(0.18)

    const coreSize =
      person.activity === 'Fleeing'
        ? 16
        : person.activity === 'Traveling'
          ? 14
          : 11 + energy * 3

    populationPoints.add({
      id: `${person.personId}-halo`,
      position: planetCartesianFromDegrees(
        person.longitudeDegrees,
        person.latitudeDegrees,
        95,
      ),
      pixelSize:
        coreSize + (person.isPregnant ? 11 : 8),
      color: haloColor,
      outlineColor: color.withAlpha(0),
      outlineWidth: 0,
    })

    populationPoints.add({
      id: person.personId,
      position: planetCartesianFromDegrees(
        person.longitudeDegrees,
        person.latitudeDegrees,
        100,
      ),
      pixelSize: coreSize,
      color,
      outlineColor: Color.fromCssColorString('#071018'),
      outlineWidth: 2.5,
    })
  }

  return visiblePopulation.length
}

function renderAnimals(
  animals: Array<{
    animalId: string
    planetId: string
    species: string
    latitudeDegrees: number
    longitudeDegrees: number
    energyReserve: number
    health: number
    activity: string
  }>,
  planetId: string,
) {
  animalPoints.removeAll()

  const visibleAnimals =
    animals.filter(
      animal => animal.planetId === planetId,
    )

  for (const animal of visibleAnimals) {
    const isWolf = animal.species === 'Wolf'

    animalPoints.add({
      id: `animal-${animal.animalId}`,
      position: planetCartesianFromDegrees(
        animal.longitudeDegrees,
        animal.latitudeDegrees,
        120,
      ),
      pixelSize:
        animal.activity === 'Attacking'
          ? 24
          : animal.activity === 'Traveling'
            ? 21
            : 19,
      color:
        isWolf
          ? Color.fromCssColorString('#d9e1e8')
          : Color.fromCssColorString('#c4b5fd'),
      outlineColor:
        Color.fromCssColorString('#10151b'),
      outlineWidth: 3,
    })

    animalPoints.add({
      id: `animal-${animal.animalId}-eye`,
      position: planetCartesianFromDegrees(
        animal.longitudeDegrees,
        animal.latitudeDegrees,
        125,
      ),
      pixelSize: 6,
      color: Color.fromCssColorString('#ff3344'),
      outlineColor: Color.BLACK,
      outlineWidth: 1,
    })
  }

  return visibleAnimals.length
}

function showBloodSpatter(
  latitudeDegrees: number,
  longitudeDegrees: number,
) {
  const offsets = [
    [0, 0, 22],
    [0.025, 0.012, 10],
    [-0.021, 0.017, 8],
    [0.014, -0.028, 9],
    [-0.032, -0.011, 7],
    [0.041, -0.018, 6],
    [-0.013, 0.039, 6],
  ] as const

  const burstIds: string[] = []
  const burstId =
    `blood-${Date.now()}-${Math.random()}`

  offsets.forEach(
    ([latitudeOffset, longitudeOffset, size], index) => {
      const id = `${burstId}-${index}`
      burstIds.push(id)

      bloodEffectPoints.add({
        id,
        position: planetCartesianFromDegrees(
          longitudeDegrees + longitudeOffset,
          latitudeDegrees + latitudeOffset,
          145 + index * 2,
        ),
        pixelSize: size,
        color:
          Color.fromCssColorString('#c1121f')
            .withAlpha(index === 0 ? 0.95 : 0.8),
        outlineColor:
          Color.fromCssColorString('#4a0008')
            .withAlpha(0.9),
        outlineWidth: index === 0 ? 3 : 1.5,
      })
    },
  )

  window.setTimeout(
    () => {
      for (const id of burstIds) {
        const point =
          bloodEffectPoints.get(
            bloodEffectPoints.length - 1,
          )

        if (point && point.id === id) {
          bloodEffectPoints.remove(point)
          continue
        }

        for (
          let index = bloodEffectPoints.length - 1;
          index >= 0;
          index--
        ) {
          const candidate =
            bloodEffectPoints.get(index)

          if (candidate.id === id) {
            bloodEffectPoints.remove(candidate)
            break
          }
        }
      }
    },
    1100,
  )
}

function hydrologyWaterColor(
  surfaceLiquidWaterKilogramsPerSquareMeter: number,
): Color {
  if (
    !Number.isFinite(
      surfaceLiquidWaterKilogramsPerSquareMeter,
    )
    || surfaceLiquidWaterKilogramsPerSquareMeter <= 0
  ) {
    return Color.fromBytes(
      18,
      78,
      118,
      0,
    )
  }

  const depthMeters =
    surfaceLiquidWaterKilogramsPerSquareMeter /
    1000

  const depthSignal =
    Math.min(
      1,
      Math.log10(
        1 + depthMeters,
      ) / 4,
    )

  const alpha =
    0.30 +
    0.50 *
    depthSignal

  return Color.fromBytes(
    20,
    105,
    168,
    Math.round(
      alpha *
      255,
    ),
  )
}

const simulationStepSeconds = 86_400
const simulationTickMilliseconds = 500
const timelinePollIntervalTicks = 10
const hydrologyRefreshMilliseconds = 2_500

let simulationTickInProgress = false
let simulationTickCount = 0
let activePlanetId: string | undefined
let hydrologyPlanetId: string | undefined
let hydrologyPrimitive: GroundPrimitive | undefined
let hydrologyInitializationInProgress = false
let hydrologyRefreshInProgress = false

const observedTimelineEvents = new Set<string>()

const cumulativeMetrics = {
  births: 0,
  demographicDeaths: 0,
  starvationDeaths: 0,
  feedingEvents: 0,
  travelFeedingEvents: 0,
  continuedFoodTravel: 0,
  energyConsumed: 0,
  energyRecovered: 0,
  demographicMigrations: 0,
  foodSeekingTravel: 0,
  scarcityMigrations: 0,
  noViableFoodFound: 0,
  wolfAttacks: 0,
  failedAttacks: 0,
  predationDeaths: 0,
}

const updateHydrologyPrimitive = async () => {
  if (
    !hydrologyPrimitive
    || !hydrologyPrimitive.ready
    || !hydrologyPlanetId
    || hydrologyRefreshInProgress
  ) {
    return
  }

  hydrologyRefreshInProgress = true

  try {
    const hydrology =
      await api.getPlanetHydrology(
        sessionId,
        hydrologyPlanetId,
      )

    for (const cell of hydrology.cells) {
      const attributes =
        hydrologyPrimitive
          .getGeometryInstanceAttributes(
            cell.cellId,
          )

      if (!attributes?.color) {
        continue
      }

      attributes.color =
        ColorGeometryInstanceAttribute.toValue(
          hydrologyWaterColor(
            cell.surfaceLiquidWaterKilogramsPerSquareMeter,
          ),
          attributes.color,
        )
    }
  } catch (error) {
    console.error(
      'Hydrology visualization could not be refreshed.',
      error,
    )
  } finally {
    hydrologyRefreshInProgress = false
  }
}

const initializeHydrologyVisualization = async (
  planetId: string,
) => {
  if (
    hydrologyInitializationInProgress
    || hydrologyPrimitive
  ) {
    return
  }

  hydrologyInitializationInProgress = true

  try {
    const [
      surface,
      hydrology,
    ] =
      await Promise.all([
        api.getPlanetSurface(
          sessionId,
          planetId,
        ),
        api.getPlanetHydrology(
          sessionId,
          planetId,
        ),
      ])

    if (
      surface.planetId !== planetId
      || hydrology.planetId !== planetId
    ) {
      throw new Error(
        'Surface and hydrology responses do not match the active planet.',
      )
    }

    if (
      surface.grid.kind !== hydrology.grid.kind
      || surface.grid.identityVersion !==
         hydrology.grid.identityVersion
      || surface.grid.latitudeBandCount !==
         hydrology.grid.latitudeBandCount
      || surface.grid.longitudeBandCount !==
         hydrology.grid.longitudeBandCount
    ) {
      throw new Error(
        'Surface and hydrology grid definitions do not match.',
      )
    }

    const hydrologyByCellId =
      new Map(
        hydrology.cells.map(
          cell => [
            cell.cellId,
            cell,
          ] as const,
        ),
      )

    const geometryInstances =
      surface.cells.map(
        cell => {
          if (cell.boundary.length < 3) {
            throw new Error(
              `Surface cell ${cell.cellId} has no drawable boundary.`,
            )
          }

          const hydrologyCell =
            hydrologyByCellId.get(
              cell.cellId,
            )

          if (!hydrologyCell) {
            throw new Error(
              `Surface cell ${cell.cellId} has no hydrology state.`,
            )
          }

          const positions =
            cell.boundary.map(
              coordinate =>
                planetCartesianFromDegrees(
                  coordinate.longitudeDegrees,
                  coordinate.latitudeDegrees,
                ),
            )

          return new GeometryInstance({
            id: cell.cellId,
            geometry:
              new PolygonGeometry({
                  ellipsoid: planetEllipsoid,
                polygonHierarchy:
                  new PolygonHierarchy(
                    positions,
                  ),
                vertexFormat:
                  PerInstanceColorAppearance
                    .FLAT_VERTEX_FORMAT,
                arcType: ArcType.RHUMB,
              }),
            attributes: {
              color:
                ColorGeometryInstanceAttribute
                  .fromColor(
                    hydrologyWaterColor(
                      hydrologyCell
                        .surfaceLiquidWaterKilogramsPerSquareMeter,
                    ),
                  ),
            },
          })
        },
      )

    hydrologyPrimitive =
      viewer.scene.primitives.add(
        new GroundPrimitive({
          geometryInstances,
          appearance:
            new PerInstanceColorAppearance({
              flat: true,
              translucent: true,
              closed: false,
            }),
          allowPicking: false,
          releaseGeometryInstances: true,
          asynchronous: true,
        }),
      )

    hydrologyPlanetId = planetId
  } catch (error) {
    console.info(
      'Authoritative hydrology visualization is unavailable for this session.',
      error,
    )
  } finally {
    hydrologyInitializationInProgress = false
  }
}

const refreshTimelineMetrics = async () => {
  const timeline =
    await api.getTimeline(sessionId)

  for (const event of timeline.events) {
    if (observedTimelineEvents.has(event.eventId)) {
      continue
    }

    observedTimelineEvents.add(event.eventId)

    if (event.cause === 'population-dynamics') {
      cumulativeMetrics.demographicDeaths +=
        event.metrics.deaths ?? 0

      cumulativeMetrics.demographicMigrations +=
        event.metrics.migrations ?? 0
    }

    if (event.cause === 'reproduction') {
      cumulativeMetrics.births +=
        event.metrics.births ?? 0
    }

    if (event.cause === 'foraging') {
      cumulativeMetrics.starvationDeaths +=
        event.metrics.starvationDeaths ?? 0

      cumulativeMetrics.feedingEvents +=
        event.metrics.feedingEvents ?? 0

      cumulativeMetrics.travelFeedingEvents +=
        event.metrics.travelFeedingEvents ?? 0

      cumulativeMetrics.continuedFoodTravel +=
        event.metrics.continuedFoodTravel ?? 0

      cumulativeMetrics.energyConsumed +=
        event.metrics.energyConsumed ?? 0

      cumulativeMetrics.energyRecovered +=
        event.metrics.energyRecovered ?? 0

      cumulativeMetrics.foodSeekingTravel +=
        event.metrics.foodSeekingTravel ?? 0

      cumulativeMetrics.scarcityMigrations +=
        event.metrics.scarcityMigrations ?? 0

      cumulativeMetrics.noViableFoodFound +=
        event.metrics.noViableFoodFound ?? 0
    }

    if (event.cause === 'predation') {
      cumulativeMetrics.wolfAttacks +=
        event.metrics.wolfAttacks ?? 0

      cumulativeMetrics.failedAttacks +=
        event.metrics.failedAttacks ?? 0

      cumulativeMetrics.predationDeaths +=
        event.metrics.predationDeaths ?? 0

      const attackLatitude =
        event.metrics.attackLatitude
      const attackLongitude =
        event.metrics.attackLongitude

      if (
        Number.isFinite(attackLatitude) &&
        Number.isFinite(attackLongitude) &&
        (event.metrics.successfulKills ?? 0) > 0
      ) {
        showBloodSpatter(
          attackLatitude,
          attackLongitude,
        )
      }
    }
  }
}

const refreshSimulation = async (
  advance: boolean,
) => {
  if (simulationTickInProgress) {
    return
  }

  simulationTickInProgress = true

  try {
    const session =
      advance
        ? await api.advanceSession(
            sessionId,
            simulationStepSeconds,
          )
        : await api.getSession(sessionId)

    const world =
      await api.getWorld(sessionId)

    if (
      simulationTickCount % timelinePollIntervalTicks === 0
    ) {
      await refreshTimelineMetrics()
    }

    simulationTickCount++

    const planet = world.planets[0]

    if (planet) {
      activePlanetId =
        planet.planetId

      const population =
        world.population.filter(
          person => person.planetId === planet.planetId,
        )

      const populationCount =
        renderPopulation(
          world.population,
          planet.planetId,
        )

      const animalCount =
        renderAnimals(
          world.animals,
          planet.planetId,
        )

      const activityCounts =
        population.reduce(
          (counts, person) => {
            counts[person.activity] =
              (counts[person.activity] ?? 0) + 1

            return counts
          },
          {} as Record<string, number>,
        )

      const pregnantCount =
        population.filter(
          person => person.isPregnant,
        ).length

      const averageEnergy =
        population.length === 0
          ? 0
          : population.reduce(
              (sum, person) =>
                sum + person.energyReserve,
              0,
            ) / population.length

      const averageHealth =
        population.length === 0
          ? 0
          : population.reduce(
              (sum, person) =>
                sum + person.health,
              0,
            ) / population.length

      const simulatedDays =
        session.currentTimeSeconds / 86_400

      sessionStatus.innerHTML = `
        <div class="simulation-telemetry-header">
          <div>
            <div class="simulation-telemetry-kicker">
              ${planet.name}
            </div>
            <div class="simulation-telemetry-population">
              ${populationCount.toLocaleString()}
            </div>
            <div class="simulation-telemetry-caption">
              Population
            </div>
          </div>

          <div class="simulation-telemetry-day">
            <div class="simulation-telemetry-kicker">
              Simulation
            </div>
            <div class="simulation-telemetry-day-value">
              Day ${simulatedDays.toFixed(0)}
            </div>
          </div>
        </div>

        <div class="simulation-telemetry-section">
          <div class="simulation-telemetry-section-title">
            Activity
          </div>

          <div class="simulation-telemetry-grid activity-grid">
            <div class="simulation-metric">
              <span class="activity-dot activity-idle"></span>
              <span class="simulation-metric-label">Idle</span>
              <strong>${activityCounts.Idle ?? 0}</strong>
            </div>

            <div class="simulation-metric">
              <span class="activity-dot activity-foraging"></span>
              <span class="simulation-metric-label">Foraging</span>
              <strong>${activityCounts.Foraging ?? 0}</strong>
            </div>

            <div class="simulation-metric">
              <span class="activity-dot activity-eating"></span>
              <span class="simulation-metric-label">Eating</span>
              <strong>${activityCounts.Eating ?? 0}</strong>
            </div>

            <div class="simulation-metric">
              <span class="activity-dot activity-traveling"></span>
              <span class="simulation-metric-label">Traveling</span>
              <strong>${activityCounts.Traveling ?? 0}</strong>
            </div>

            <div class="simulation-metric">
              <span
                class="activity-dot"
                style="background:#ff4fd8;box-shadow:0 0 8px #ff4fd8"
              ></span>
              <span class="simulation-metric-label">Fleeing</span>
              <strong>${activityCounts.Fleeing ?? 0}</strong>
            </div>

            <div class="simulation-metric">
              <span
                class="activity-dot"
                style="background:#b388ff;box-shadow:0 0 8px #b388ff"
              ></span>
              <span class="simulation-metric-label">Seeking partner</span>
              <strong>${activityCounts.SeekingPartner ?? 0}</strong>
            </div>

            <div class="simulation-metric">
              <span
                class="activity-dot"
                style="background:#ff6fae;box-shadow:0 0 8px #ff6fae"
              ></span>
              <span class="simulation-metric-label">Mating</span>
              <strong>${activityCounts.Mating ?? 0}</strong>
            </div>

            <div class="simulation-metric">
              <span
                class="activity-dot"
                style="background:#65e6c4;box-shadow:0 0 8px #65e6c4"
              ></span>
              <span class="simulation-metric-label">Pregnant</span>
              <strong>${pregnantCount}</strong>
            </div>
          </div>
        </div>

        <div class="simulation-telemetry-section">
          <div class="simulation-telemetry-section-title">
            Condition
          </div>

          <div class="simulation-condition-grid">
            <div class="simulation-condition">
              <span>Energy</span>
              <strong>${averageEnergy.toFixed(2)}</strong>
              <div class="simulation-meter">
                <div
                  class="simulation-meter-fill"
                  style="width: ${Math.max(
                    0,
                    Math.min(100, averageEnergy * 100),
                  )}%"
                ></div>
              </div>
            </div>

            <div class="simulation-condition">
              <span>Health</span>
              <strong>${averageHealth.toFixed(2)}</strong>
              <div class="simulation-meter">
                <div
                  class="simulation-meter-fill"
                  style="width: ${Math.max(
                    0,
                    Math.min(100, averageHealth * 100),
                  )}%"
                ></div>
              </div>
            </div>
          </div>
        </div>

        <div class="simulation-telemetry-section">
          <div class="simulation-telemetry-section-title">
            Population change
          </div>

          <div class="simulation-telemetry-grid">
            <div class="simulation-metric">
              <span class="simulation-metric-label">Births</span>
              <strong>+${cumulativeMetrics.births}</strong>
            </div>

            <div class="simulation-metric">
              <span class="simulation-metric-label">
                Natural deaths
              </span>
              <strong>−${cumulativeMetrics.demographicDeaths}</strong>
            </div>

            <div class="simulation-metric">
              <span class="simulation-metric-label">
                Starvation deaths
              </span>
              <strong>−${cumulativeMetrics.starvationDeaths}</strong>
            </div>
          </div>
        </div>

        <div class="simulation-telemetry-section">
          <div class="simulation-telemetry-section-title">
            Predation
          </div>

          <div class="simulation-telemetry-grid">
            <div class="simulation-metric">
              <span class="simulation-metric-label">Wolves</span>
              <strong>${animalCount}</strong>
            </div>

            <div class="simulation-metric">
              <span class="simulation-metric-label">Wolf attacks</span>
              <strong>${cumulativeMetrics.wolfAttacks.toFixed(0)}</strong>
            </div>

            <div class="simulation-metric">
              <span class="simulation-metric-label">Failed attacks</span>
              <strong>${cumulativeMetrics.failedAttacks.toFixed(0)}</strong>
            </div>

            <div class="simulation-metric">
              <span class="simulation-metric-label">Predation deaths</span>
              <strong>−${cumulativeMetrics.predationDeaths.toFixed(0)}</strong>
            </div>
          </div>
        </div>

        <details class="simulation-diagnostics">
          <summary>Diagnostics</summary>

        <div class="simulation-telemetry-section">
          <div class="simulation-telemetry-section-title">
            Ecology
          </div>

          <div class="simulation-telemetry-grid">
            <div class="simulation-metric">
              <span class="simulation-metric-label">
                Energy consumed
              </span>
              <strong>${cumulativeMetrics.energyConsumed.toFixed(1)}</strong>
            </div>

            <div class="simulation-metric">
              <span class="simulation-metric-label">
                Energy recovered
              </span>
              <strong>${cumulativeMetrics.energyRecovered.toFixed(1)}</strong>
            </div>

            <div class="simulation-metric">
              <span class="simulation-metric-label">
                Energy / feeding
              </span>
              <strong>${(
                cumulativeMetrics.feedingEvents > 0
                  ? cumulativeMetrics.energyConsumed /
                    cumulativeMetrics.feedingEvents
                  : 0
              ).toFixed(3)}</strong>
            </div>

            <div class="simulation-metric">
              <span class="simulation-metric-label">
                Feeding events
              </span>
              <strong>${cumulativeMetrics.feedingEvents}</strong>
            </div>
          </div>
        </div>

        <div class="simulation-telemetry-section">
          <div class="simulation-telemetry-section-title">
            Movement
          </div>

          <div class="simulation-telemetry-grid">
            <div class="simulation-metric">
              <span class="simulation-metric-label">
                Random moves
              </span>
              <strong>${cumulativeMetrics.demographicMigrations}</strong>
            </div>

            <div class="simulation-metric">
              <span class="simulation-metric-label">
                Food-seeking steps
              </span>
              <strong>${cumulativeMetrics.foodSeekingTravel}</strong>
            </div>

            <div class="simulation-metric">
              <span class="simulation-metric-label">
                Continued travel
              </span>
              <strong>${cumulativeMetrics.continuedFoodTravel}</strong>
            </div>

            <div class="simulation-metric">
              <span class="simulation-metric-label">
                Travel feedings
              </span>
              <strong>${cumulativeMetrics.travelFeedingEvents}</strong>
            </div>

            <div class="simulation-metric">
              <span class="simulation-metric-label">
                Scarcity migrations
              </span>
              <strong>${cumulativeMetrics.scarcityMigrations}</strong>
            </div>

            <div class="simulation-metric">
              <span class="simulation-metric-label">
                No viable food
              </span>
              <strong>${cumulativeMetrics.noViableFoodFound}</strong>
            </div>
          </div>
        </div>
        </details>
      `
    } else {
      activePlanetId = undefined

      populationPoints.removeAll()
      animalPoints.removeAll()
      bloodEffectPoints.removeAll()

      sessionStatus.textContent =
        `Session ${session.sessionId} · no planets`
    }
  } catch (error) {
    console.error(error)
    sessionStatus.textContent =
      'Simulation session could not be updated.'
  } finally {
    simulationTickInProgress = false
  }
}

await refreshSimulation(false)

if (activePlanetId) {
  void initializeHydrologyVisualization(
    activePlanetId,
  )
}

window.setInterval(
  () => {
    void refreshSimulation(true)
  },
  simulationTickMilliseconds,
)

window.setInterval(
  () => {
    void updateHydrologyPrimitive()
  },
  hydrologyRefreshMilliseconds,
)
