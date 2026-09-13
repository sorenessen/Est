import 'cesium/Build/Cesium/Widgets/widgets.css'
import '../style.css'
import { EstApi } from '../api/est-api'
import {
  selectPresentationState,
  type PresentationState,
} from '../presentation/presentation-policy'
import {
  createLocalSceneViewMeasurementAdapter,
  type LocalScenePresentationFeature,
  type LocalSceneViewMeasurement,
} from './local-scene-view-measurement'

import {
  Cartesian3,
  Cartographic,
  Color,
  Cesium3DTileStyle,
  createGooglePhotorealistic3DTileset,
  createOsmBuildingsAsync,
  createWorldTerrainAsync,
  HeightReference,
  IonGeocodeProviderType,
  ImageryLayer,
  Ion,
  JulianDate,
  Material,
  PointPrimitiveCollection,
  Rectangle,
  sampleTerrainMostDetailed,
  SingleTileImageryProvider,
  TileMapServiceImageryProvider,
  UrlTemplateImageryProvider,
  Viewer,
  WebMapServiceImageryProvider,
} from 'cesium'

const token = import.meta.env.VITE_CESIUM_ION_TOKEN

if (!token) {
  throw new Error(
    'VITE_CESIUM_ION_TOKEN is required for the Cesium evaluation.',
  )
}

Ion.defaultAccessToken = token

const app = document.querySelector<HTMLDivElement>('#app')

if (!app) {
  throw new Error('Application root was not found.')
}

const appRoot = app

appRoot.innerHTML = `
  <div id="cesiumContainer" aria-label="Est Cesium globe evaluation"></div>

  <section
    id="renderEvaluationPanel"
    class="render-evaluation-panel"
  >
    <div
      id="renderEvaluationDragHandle"
      class="render-evaluation-heading"
    >
      <div class="render-evaluation-heading-copy">
        <strong>Est Rendering Evaluation</strong>
        <div class="render-evaluation-mode">
          <span class="render-evaluation-mode-label">Mode:</span>
          <span id="lookLabel">Baseline</span>
        </div>
      </div>

      <div class="render-evaluation-window-actions">
        <button
          id="renderEvaluationCollapseButton"
          type="button"
          aria-label="Collapse rendering evaluation panel"
          aria-expanded="true"
          title="Collapse panel"
        >−</button>
        <button
          id="renderEvaluationHideButton"
          type="button"
          aria-label="Hide rendering evaluation panel"
          title="Hide panel"
        >×</button>
      </div>
    </div>

    <div id="renderEvaluationBody">
      <div class="render-evaluation-actions">
        <button id="baselineButton" type="button">Baseline</button>
        <button id="photorealisticButton" type="button">Google Photo 3D</button>
        <button id="nightEarthButton" type="button">Night Earth</button>
        <button id="estCgButton" type="button">Est CG World</button>
        <button id="estButton" type="button">Terrain Study</button>
        <button id="landCoverButton" type="button">Land Cover</button>
        <button id="estSurfaceButton" type="button">Est Surface Study</button>
        <button id="estSurfaceTmsButton" type="button">Est Surface TMS</button>
        <button id="visualSurfaceButton" type="button">Est Visual Surface</button>
        <button id="continuousSurfaceButton" type="button">Est Continuous Surface</button>
        <button id="localGeometryButton" type="button">Local Geometry</button>
        <button id="terrainGeometryButton" type="button">Est Terrain + Geometry</button>
        <button id="automaticScaleButton" type="button">Automatic Scale</button>
        <button id="daylightButton" type="button" aria-pressed="false">Lighting: Real Time</button>
      </div>

      <div id="presentationStatus">Automatic scale inactive.</div>

      <section
        id="sessionStatus"
        class="simulation-telemetry"
        aria-label="Simulation telemetry"
      >
        <div class="simulation-telemetry-empty">
          No simulation session selected.
        </div>
      </section>
    </div>
  </section>

  <button
    id="renderEvaluationRestoreButton"
    class="render-evaluation-restore"
    type="button"
    aria-label="Show Est rendering evaluation panel"
    title="Show rendering evaluation panel"
    hidden
  >Est</button>
`

const terrainProvider = await createWorldTerrainAsync({
  requestVertexNormals: true,
  requestWaterMask: true,
})

const imageryLayer = ImageryLayer.fromWorldImagery({})

const landCoverProvider = new WebMapServiceImageryProvider({
  url: 'https://dmsdata.cr.usgs.gov/geoserver/mrlc_Land-Cover-Native_conus_year_data/wms',
  layers: 'Land-Cover-Native_conus_year_data',
  parameters: {
    transparent: true,
    format: 'image/png',
  },
  rectangle: Rectangle.fromDegrees(-124.5, 45.5, -120.0, 48.5),
  credit: 'USGS / MRLC Annual NLCD',
})

const nightLightsProvider = new UrlTemplateImageryProvider({
  url: 'https://gibs.earthdata.nasa.gov/wmts/epsg3857/best/VIIRS_Night_Lights/default/default/GoogleMapsCompatible_Level8/{z}/{y}/{x}.png',
  maximumLevel: 8,
  credit: 'NASA GIBS / VIIRS Black Marble',
})

const viewer = new Viewer('cesiumContainer', {
  terrainProvider,
  baseLayer: imageryLayer,
  animation: false,
  baseLayerPicker: false,
  fullscreenButton: false,
  geocoder: IonGeocodeProviderType.GOOGLE,
  homeButton: false,
  infoBox: false,
  navigationHelpButton: false,
  sceneModePicker: false,
  selectionIndicator: false,
  timeline: false,
})

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

const nightLightsLayer =
  viewer.imageryLayers.addImageryProvider(
    nightLightsProvider,
  )

nightLightsLayer.show = false
nightLightsLayer.brightness = 1.15
nightLightsLayer.contrast = 1.15
nightLightsLayer.saturation = 1.1

const globalBuildings =
  await createOsmBuildingsAsync({
    enableShowOutline: false,
  })

globalBuildings.maximumScreenSpaceError = 8
globalBuildings.cacheBytes = 1024 * 1024 * 1024
globalBuildings.maximumCacheOverflowBytes = 512 * 1024 * 1024
globalBuildings.progressiveResolutionHeightFraction = 0.3

viewer.scene.primitives.add(globalBuildings)

const photorealisticTiles =
  await createGooglePhotorealistic3DTileset({
    onlyUsingWithGoogleGeocoder: true,
  })

photorealisticTiles.show = false
viewer.scene.primitives.add(photorealisticTiles)

viewer.scene.globe.preloadSiblings = true

viewer.scene.backgroundColor = Color.BLACK
viewer.scene.globe.enableLighting = true
viewer.scene.globe.depthTestAgainstTerrain = true
viewer.scene.globe.showGroundAtmosphere = true
viewer.scene.globe.dynamicAtmosphereLighting = true

viewer.camera.setView({
  destination: Cartesian3.fromDegrees(
    25,
    0,
    18_000_000,
  ),
})

function requireElement<T extends HTMLElement>(
  selector: string,
): T {
  const element = document.querySelector<T>(selector)

  if (!element) {
    throw new Error(`Required element was not found: ${selector}`)
  }

  return element
}

const baselineButton =
  requireElement<HTMLButtonElement>('#baselineButton')

const photorealisticButton =
  requireElement<HTMLButtonElement>('#photorealisticButton')

const nightEarthButton =
  requireElement<HTMLButtonElement>('#nightEarthButton')

const estCgButton =
  requireElement<HTMLButtonElement>('#estCgButton')

const estButton =
  requireElement<HTMLButtonElement>('#estButton')

const landCoverButton =
  requireElement<HTMLButtonElement>('#landCoverButton')

const estSurfaceButton =
  requireElement<HTMLButtonElement>('#estSurfaceButton')

const estSurfaceTmsButton =
  requireElement<HTMLButtonElement>('#estSurfaceTmsButton')

const visualSurfaceButton =
  requireElement<HTMLButtonElement>('#visualSurfaceButton')

const continuousSurfaceButton =
  requireElement<HTMLButtonElement>('#continuousSurfaceButton')

const localGeometryButton =
  requireElement<HTMLButtonElement>('#localGeometryButton')

const terrainGeometryButton =
  requireElement<HTMLButtonElement>('#terrainGeometryButton')

const automaticScaleButton =
  requireElement<HTMLButtonElement>('#automaticScaleButton')

const daylightButton =
  requireElement<HTMLButtonElement>('#daylightButton')

const lookLabel =
  requireElement<HTMLSpanElement>('#lookLabel')

const presentationStatus =
  requireElement<HTMLDivElement>('#presentationStatus')

const sessionStatus =
  requireElement<HTMLDivElement>('#sessionStatus')

const renderEvaluationPanel =
  requireElement<HTMLElement>('#renderEvaluationPanel')

const renderEvaluationDragHandle =
  requireElement<HTMLDivElement>('#renderEvaluationDragHandle')

const renderEvaluationBody =
  requireElement<HTMLDivElement>('#renderEvaluationBody')

const renderEvaluationCollapseButton =
  requireElement<HTMLButtonElement>(
    '#renderEvaluationCollapseButton',
  )

const renderEvaluationHideButton =
  requireElement<HTMLButtonElement>(
    '#renderEvaluationHideButton',
  )

const renderEvaluationRestoreButton =
  requireElement<HTMLButtonElement>(
    '#renderEvaluationRestoreButton',
  )

const panelViewportMargin = 8

function clampEvaluationPanelToViewport(): void {
  if (renderEvaluationPanel.hidden) {
    return
  }

  const panelRect = renderEvaluationPanel.getBoundingClientRect()
  const appRect = appRoot.getBoundingClientRect()

  const maxLeft = Math.max(
    panelViewportMargin,
    appRect.width - panelRect.width - panelViewportMargin,
  )

  const maxTop = Math.max(
    panelViewportMargin,
    appRect.height - panelRect.height - panelViewportMargin,
  )

  const currentLeft =
    Number.parseFloat(renderEvaluationPanel.style.left)
    || panelRect.left - appRect.left

  const currentTop =
    Number.parseFloat(renderEvaluationPanel.style.top)
    || panelRect.top - appRect.top

  renderEvaluationPanel.style.left =
    `${Math.min(Math.max(currentLeft, panelViewportMargin), maxLeft)}px`

  renderEvaluationPanel.style.top =
    `${Math.min(Math.max(currentTop, panelViewportMargin), maxTop)}px`
}

let panelDragPointerId: number | undefined
let panelDragStartPointerX = 0
let panelDragStartPointerY = 0
let panelDragStartLeft = 0
let panelDragStartTop = 0

renderEvaluationDragHandle.addEventListener(
  'pointerdown',
  (event) => {
    if (
      event.target instanceof Element
      && event.target.closest('button')
    ) {
      return
    }

    const panelRect =
      renderEvaluationPanel.getBoundingClientRect()
    const appRect = appRoot.getBoundingClientRect()

    panelDragPointerId = event.pointerId
    panelDragStartPointerX = event.clientX
    panelDragStartPointerY = event.clientY
    panelDragStartLeft = panelRect.left - appRect.left
    panelDragStartTop = panelRect.top - appRect.top

    renderEvaluationDragHandle.setPointerCapture(
      event.pointerId,
    )

    renderEvaluationPanel.classList.add('is-dragging')
    event.preventDefault()
  },
)

renderEvaluationDragHandle.addEventListener(
  'pointermove',
  (event) => {
    if (panelDragPointerId !== event.pointerId) {
      return
    }

    const panelRect =
      renderEvaluationPanel.getBoundingClientRect()
    const appRect = appRoot.getBoundingClientRect()

    const maxLeft = Math.max(
      panelViewportMargin,
      appRect.width - panelRect.width - panelViewportMargin,
    )

    const maxTop = Math.max(
      panelViewportMargin,
      appRect.height - panelRect.height - panelViewportMargin,
    )

    const requestedLeft =
      panelDragStartLeft
      + event.clientX
      - panelDragStartPointerX

    const requestedTop =
      panelDragStartTop
      + event.clientY
      - panelDragStartPointerY

    renderEvaluationPanel.style.left =
      `${Math.min(
        Math.max(requestedLeft, panelViewportMargin),
        maxLeft,
      )}px`

    renderEvaluationPanel.style.top =
      `${Math.min(
        Math.max(requestedTop, panelViewportMargin),
        maxTop,
      )}px`
  },
)

function finishEvaluationPanelDrag(event: PointerEvent): void {
  if (panelDragPointerId !== event.pointerId) {
    return
  }

  if (
    renderEvaluationDragHandle.hasPointerCapture(
      event.pointerId,
    )
  ) {
    renderEvaluationDragHandle.releasePointerCapture(
      event.pointerId,
    )
  }

  panelDragPointerId = undefined
  renderEvaluationPanel.classList.remove('is-dragging')
}

renderEvaluationDragHandle.addEventListener(
  'pointerup',
  finishEvaluationPanelDrag,
)

renderEvaluationDragHandle.addEventListener(
  'pointercancel',
  finishEvaluationPanelDrag,
)

renderEvaluationCollapseButton.addEventListener(
  'click',
  () => {
    const collapsed =
      !renderEvaluationPanel.classList.contains('is-collapsed')

    renderEvaluationPanel.classList.toggle(
      'is-collapsed',
      collapsed,
    )

    renderEvaluationBody.hidden = collapsed

    renderEvaluationCollapseButton.textContent =
      collapsed ? '+' : '−'

    renderEvaluationCollapseButton.setAttribute(
      'aria-expanded',
      String(!collapsed),
    )

    renderEvaluationCollapseButton.setAttribute(
      'aria-label',
      collapsed
        ? 'Expand rendering evaluation panel'
        : 'Collapse rendering evaluation panel',
    )

    renderEvaluationCollapseButton.title =
      collapsed ? 'Expand panel' : 'Collapse panel'

    requestAnimationFrame(clampEvaluationPanelToViewport)
  },
)

renderEvaluationHideButton.addEventListener(
  'click',
  () => {
    renderEvaluationPanel.hidden = true
    renderEvaluationRestoreButton.hidden = false
  },
)

renderEvaluationRestoreButton.addEventListener(
  'click',
  () => {
    renderEvaluationRestoreButton.hidden = true
    renderEvaluationPanel.hidden = false
    requestAnimationFrame(clampEvaluationPanelToViewport)
  },
)

window.addEventListener(
  'resize',
  () => requestAnimationFrame(clampEvaluationPanelToViewport),
)

const evaluationPanelResizeObserver = new ResizeObserver(
  () => requestAnimationFrame(clampEvaluationPanelToViewport),
)

evaluationPanelResizeObserver.observe(renderEvaluationPanel)

function renderPopulation(
  population: Array<{
    personId: string
    planetId: string
    latitudeDegrees: number
    longitudeDegrees: number
    activity: string
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

    const coreSize =
      person.activity === 'Traveling'
        ? 14
        : 11 + energy * 3

    populationPoints.add({
      id: `${person.personId}-halo`,
      position: Cartesian3.fromDegrees(
        person.longitudeDegrees,
        person.latitudeDegrees,
        95,
      ),
      pixelSize: coreSize + 8,
      color: color.withAlpha(0.18),
      outlineColor: color.withAlpha(0),
      outlineWidth: 0,
    })

    populationPoints.add({
      id: person.personId,
      position: Cartesian3.fromDegrees(
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
      position: Cartesian3.fromDegrees(
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
      position: Cartesian3.fromDegrees(
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
        position: Cartesian3.fromDegrees(
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

const queryParameters =
  new URLSearchParams(window.location.search)

const sessionId = queryParameters.get('session')

if (sessionId) {
  const api = new EstApi('/api')

  const simulationStepSeconds = 86_400
  const simulationTickMilliseconds = 500
  const timelinePollIntervalTicks = 10

  let simulationTickInProgress = false
  let simulationTickCount = 0

  const observedTimelineEvents = new Set<string>()

  const cumulativeMetrics = {
    births: 0,
    demographicDeaths: 0,
    starvationDeaths: 0,
    demographicMigrations: 0,
    scarcityMigrations: 0,
    wolfAttacks: 0,
    predationDeaths: 0,
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
        cumulativeMetrics.births +=
          event.metrics.births ?? 0

        cumulativeMetrics.demographicDeaths +=
          event.metrics.deaths ?? 0

        cumulativeMetrics.demographicMigrations +=
          event.metrics.migrations ?? 0
      }

      if (event.cause === 'foraging') {
        cumulativeMetrics.starvationDeaths +=
          event.metrics.starvationDeaths ?? 0

        cumulativeMetrics.scarcityMigrations +=
          event.metrics.scarcityMigrations ?? 0
      }

      if (event.cause === 'predation') {
        cumulativeMetrics.wolfAttacks +=
          event.metrics.wolfAttacks ?? 0

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
                <span class="simulation-metric-label">Predation deaths</span>
                <strong>−${cumulativeMetrics.predationDeaths.toFixed(0)}</strong>
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
                  Food migrations
                </span>
                <strong>${cumulativeMetrics.scarcityMigrations}</strong>
              </div>
            </div>
          </div>
        `
      } else {
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

  window.setInterval(
    () => {
      void refreshSimulation(true)
    },
    simulationTickMilliseconds,
  )
}

const ramp = document.createElement('canvas')
ramp.width = 256
ramp.height = 1

const context = ramp.getContext('2d')

if (!context) {
  throw new Error('Could not create terrain color ramp.')
}

const gradient = context.createLinearGradient(0, 0, 256, 0)

gradient.addColorStop(0.00, '#092b46')
gradient.addColorStop(0.18, '#14506a')
gradient.addColorStop(0.28, '#287d8e')
gradient.addColorStop(0.30, '#d1c6a2')
gradient.addColorStop(0.36, '#52734c')
gradient.addColorStop(0.48, '#71835a')
gradient.addColorStop(0.62, '#a79b77')
gradient.addColorStop(0.78, '#817e78')
gradient.addColorStop(0.91, '#c4c5c0')
gradient.addColorStop(1.00, '#f2f3f0')

context.fillStyle = gradient
context.fillRect(0, 0, ramp.width, ramp.height)

const terrainMaterial = Material.fromType(
  Material.ElevationRampType,
  {
    image: ramp,
    minimumHeight: -1000,
    maximumHeight: 6000,
  },
)

const landCoverLayer = viewer.imageryLayers.addImageryProvider(
  landCoverProvider,
)
landCoverLayer.show = false

const surfaceManifestResponse = await fetch(
  '/evaluation/nlcd-2025/surface-manifest.json',
)

if (!surfaceManifestResponse.ok) {
  throw new Error(
    `Est surface manifest could not be loaded: ${surfaceManifestResponse.status}`,
  )
}

const surfaceManifest = await surfaceManifestResponse.json() as {
  geographicPreview: {
    bounds: {
      west: number
      south: number
      east: number
      north: number
    }
  }
}

const surfaceBounds = surfaceManifest.geographicPreview.bounds

const surfaceRectangle = Rectangle.fromDegrees(
  surfaceBounds.west,
  surfaceBounds.south,
  surfaceBounds.east,
  surfaceBounds.north,
)

const surfaceProvider = await SingleTileImageryProvider.fromUrl(
  '/evaluation/nlcd-2025/surface-preview-geographic.png',
  {
    rectangle: surfaceRectangle,
    credit: 'USGS Annual NLCD 2025 / Est Surface Study',
  },
)

const surfaceLayer = viewer.imageryLayers.addImageryProvider(
  surfaceProvider,
)
surfaceLayer.show = false

const surfaceTmsProvider =
  await TileMapServiceImageryProvider.fromUrl(
    '/evaluation/nlcd-2025/surface-tms/',
    {
      credit: 'USGS Annual NLCD 2025 / Est Surface TMS Study',
    },
  )

const surfaceTmsLayer =
  viewer.imageryLayers.addImageryProvider(
    surfaceTmsProvider,
  )

surfaceTmsLayer.show = false

const visualSurfaceTmsProvider =
  await TileMapServiceImageryProvider.fromUrl(
    '/evaluation/nlcd-2025/visual-surface-tms/',
    {
      credit: 'USGS Annual NLCD 2025 / USGS 3DEP / Est Visual Surface Study',
    },
  )

const visualSurfaceTmsLayer =
  viewer.imageryLayers.addImageryProvider(
    visualSurfaceTmsProvider,
  )

visualSurfaceTmsLayer.show = false

const continuousSurfaceTmsProvider =
  await TileMapServiceImageryProvider.fromUrl(
    '/evaluation/nlcd-2025/continuous-surface-tms/',
    {
      credit: 'Copernicus Sentinel-2 / Est Continuous Visual Surface Study',
    },
  )

const continuousSurfaceTmsLayer =
  viewer.imageryLayers.addImageryProvider(
    continuousSurfaceTmsProvider,
  )

continuousSurfaceTmsLayer.show = false

type LocalSceneFeature = {
  type: 'Feature'
  properties: {
    id: string
    name?: string
    heightMeters: number
    heightSource: string
  }
  geometry: {
    type: 'Polygon'
    coordinates: number[][][]
  }
}

type LocalSceneFeatureCollection = {
  type: 'FeatureCollection'
  estLocalSceneVersion: number
  features: LocalSceneFeature[]
}

const localSceneEvaluations = {
  longmire: {
    name: 'Longmire',
    url: '/evaluation/local-scene/rainier-longmire-buildings.geojson',
    longitude: -121.8112,
    latitude: 46.7495,
    cameraHeight: 1800,
  },
  olympia: {
    name: 'Olympia',
    url: '/evaluation/local-scene/olympia-capitol-buildings.geojson',
    longitude: -122.90484,
    latitude: 47.03576,
    cameraHeight: 850,
  },
} as const

const requestedLocalScene =
  queryParameters.get('localScene') ?? 'longmire'

if (!(requestedLocalScene in localSceneEvaluations)) {
  throw new Error(
    `Unsupported local-scene evaluation: ${requestedLocalScene}`,
  )
}

const localSceneEvaluation =
  localSceneEvaluations[
    requestedLocalScene as keyof typeof localSceneEvaluations
  ]

const localSceneResponse = await fetch(
  localSceneEvaluation.url,
)

if (!localSceneResponse.ok) {
  throw new Error(
    `Could not load ${localSceneEvaluation.name} local-scene evaluation: ${localSceneResponse.status}`,
  )
}

const localScene =
  await localSceneResponse.json() as LocalSceneFeatureCollection

if (
  localScene.type !== 'FeatureCollection'
  || localScene.estLocalSceneVersion !== 1
) {
  throw new Error('Unsupported Est local-scene evaluation asset.')
}

type PendingLocalScenePresentationFeature = {
  id: string
  heightMeters: number
  terrainPositions: Cartographic[]
}

const localScenePresentationFeatures: LocalScenePresentationFeature[] = []

const pendingLocalScenePresentationFeatures:
  PendingLocalScenePresentationFeature[] = []

const localGeometryEntities = localScene.features.map((feature) => {
  if (
    feature.geometry.type !== 'Polygon'
    || !Number.isFinite(feature.properties.heightMeters)
    || feature.properties.heightMeters <= 0
  ) {
    throw new Error(
      `Invalid Est local-scene building feature: ${feature.properties.id}`,
    )
  }

  const ring = feature.geometry.coordinates[0]

  if (!ring || ring.length < 4) {
    throw new Error(
      `Est local-scene building has no usable exterior ring: ${feature.properties.id}`,
    )
  }

  const positions = ring
    .slice(0, -1)
    .map(([longitude, latitude]) =>
      Cartesian3.fromDegrees(longitude, latitude),
    )

  pendingLocalScenePresentationFeatures.push({
    id: feature.properties.id,
    heightMeters: feature.properties.heightMeters,
    terrainPositions: ring
      .slice(0, -1)
      .map(([longitude, latitude]) =>
        Cartographic.fromDegrees(longitude, latitude),
      ),
  })

  const entity = viewer.entities.add({
    id: feature.properties.id,
    name: feature.properties.name,
    show: false,
    polygon: {
      hierarchy: positions,
      height: 0,
      heightReference: HeightReference.RELATIVE_TO_GROUND,
      extrudedHeight: feature.properties.heightMeters,
      extrudedHeightReference: HeightReference.RELATIVE_TO_GROUND,
      material: Color.fromBytes(196, 190, 176, 235),
      outline: false,
    },
  })

  return entity
})

const localSceneTerrainSamples =
  pendingLocalScenePresentationFeatures.flatMap(
    (feature) => feature.terrainPositions,
  )

await sampleTerrainMostDetailed(
  terrainProvider,
  localSceneTerrainSamples,
)

for (const feature of pendingLocalScenePresentationFeatures) {
  const basePositions: Cartesian3[] = []
  const roofPositions: Cartesian3[] = []

  for (const terrainPosition of feature.terrainPositions) {
    if (!Number.isFinite(terrainPosition.height)) {
      throw new Error(
        `Could not resolve terrain height for local-scene feature: ${feature.id}`,
      )
    }

    const terrainHeight = terrainPosition.height

    basePositions.push(
      Cartesian3.fromRadians(
        terrainPosition.longitude,
        terrainPosition.latitude,
        terrainHeight,
      ),
    )

    roofPositions.push(
      Cartesian3.fromRadians(
        terrainPosition.longitude,
        terrainPosition.latitude,
        terrainHeight + feature.heightMeters,
      ),
    )
  }

  localScenePresentationFeatures.push({
    id: feature.id,
    measurementPositions: [
      ...basePositions,
      ...roofPositions,
    ],
  })
}

const localSceneViewMeasurement =
  createLocalSceneViewMeasurementAdapter(
    viewer,
    localScenePresentationFeatures,
  )

function setLocalGeometryVisible(visible: boolean): void {
  for (const entity of localGeometryEntities) {
    entity.show = visible
  }
}

let automaticScaleEnabled = false

let automaticPresentationState: PresentationState = {
  localStructuresVisible: false,
}

let automaticPolicyCheckCount = 0
let automaticRepresentationTransitionCount = 0

let lastPolicyEvaluationHeightMeters: number | undefined
let lastPolicyEvaluationDistanceMeters: number | undefined

function formatMeters(value: number): string {
  return Math.round(value).toLocaleString()
}

function updateAutomaticPresentationStatus(
  measurement: LocalSceneViewMeasurement,
): void {
  if (!automaticScaleEnabled) {
    return
  }

  const {
    cameraHeightMeters,
    distanceToLocalSceneMeters,
    centerViewSurfaceDistanceMeters,
    viewSurfaceSampleDistancesMeters,
    localSceneVisibleFeatureCount,
    localSceneFeatureBoxCoverage,
    localSceneFeatureBoxUnionCoverage,
  } = measurement

  const representation =
    automaticPresentationState.localStructuresVisible
      ? 'regional + local structures'
      : 'regional only'

  const lastEvaluation =
    lastPolicyEvaluationHeightMeters === undefined
    || lastPolicyEvaluationDistanceMeters === undefined
      ? 'none'
      : `${formatMeters(lastPolicyEvaluationHeightMeters)} m high / ${formatMeters(lastPolicyEvaluationDistanceMeters)} m away`

  const centerViewSurfaceDistanceLabel =
    centerViewSurfaceDistanceMeters === undefined
      ? 'none'
      : `${formatMeters(centerViewSurfaceDistanceMeters)} m`

  const viewSurfaceSampleLabels =
    viewSurfaceSampleDistancesMeters.map((distance) =>
      distance === undefined
        ? 'none'
        : `${formatMeters(distance)} m`,
    )

  const viewSurfaceHitCount =
    viewSurfaceSampleDistancesMeters.filter(
      (distance) => distance !== undefined,
    ).length

  const viewSurfaceSamplesLabel =
    `${viewSurfaceHitCount}/${viewSurfaceSampleDistancesMeters.length}`
    + ` [${viewSurfaceSampleLabels.join(', ')}]`

  const featureBoxCoverageLabel =
    (localSceneFeatureBoxCoverage * 100).toFixed(1)

  const featureBoxUnionCoverageLabel =
    (localSceneFeatureBoxUnionCoverage * 100).toFixed(1)

  presentationStatus.textContent =
    `Live: ${formatMeters(cameraHeightMeters)} m high / ${formatMeters(distanceToLocalSceneMeters)} m away / center surface ${centerViewSurfaceDistanceLabel}`
    + ` · Surface samples C/L/R/U/D ${viewSurfaceSamplesLabel}`
    + ` · ${localSceneEvaluation.name}: visible features ${localSceneVisibleFeatureCount}/${localScenePresentationFeatures.length} / feature boxes ${featureBoxCoverageLabel}% aggregate / ${featureBoxUnionCoverageLabel}% union`
    + ` · Last evaluation: ${lastEvaluation}`
    + ` · Decision: ${representation}`
    + ` · Checks: ${automaticPolicyCheckCount}`
    + ` · Transitions: ${automaticRepresentationTransitionCount}`
}

function disableAutomaticScale(): void {
  automaticScaleEnabled = false
  presentationStatus.textContent = 'Automatic scale inactive.'
}

function applyAutomaticPresentation(): void {
  if (!automaticScaleEnabled) {
    return
  }

  const measurement =
    localSceneViewMeasurement.measure()

  const {
    cameraHeightMeters,
    distanceToLocalSceneMeters,
  } = measurement

  const previousPresentationState =
    automaticPresentationState

  automaticPresentationState = selectPresentationState(
    {
      cameraHeightMeters,
      localRepresentationScreenSignificance:
        measurement.localSceneFeatureBoxUnionCoverage,
    },
    previousPresentationState,
  )

  automaticPolicyCheckCount += 1
  lastPolicyEvaluationHeightMeters = cameraHeightMeters
  lastPolicyEvaluationDistanceMeters =
    distanceToLocalSceneMeters

  if (
    automaticPresentationState.localStructuresVisible
    !== previousPresentationState.localStructuresVisible
  ) {
    automaticRepresentationTransitionCount += 1
  }

  setLocalGeometryVisible(
    automaticPresentationState.localStructuresVisible,
  )

  lookLabel.textContent = 'Automatic Scale'
  updateAutomaticPresentationStatus(measurement)
}

function enableAutomaticScale(): void {
  viewer.scene.globe.enableLighting = true
  automaticScaleEnabled = true
  automaticPolicyCheckCount = 0
  automaticRepresentationTransitionCount = 0
  lastPolicyEvaluationHeightMeters = undefined
  lastPolicyEvaluationDistanceMeters = undefined

  viewer.scene.globe.material = undefined
  imageryLayer.show = true
  landCoverLayer.show = false
  surfaceLayer.show = false
  surfaceTmsLayer.show = false
  visualSurfaceTmsLayer.show = false
  continuousSurfaceTmsLayer.show = false

  viewer.scene.globe.lambertDiffuseMultiplier = 1
  viewer.scene.globe.atmosphereLightIntensity = 10

  imageryLayer.brightness = 1
  imageryLayer.contrast = 1
  imageryLayer.saturation = 1
  imageryLayer.gamma = 1

  applyAutomaticPresentation()
}

const inspectionDaylightTime =
  JulianDate.fromIso8601('2026-06-21T20:00:00Z')

let inspectionDaylightEnabled = false

function applyInspectionLighting(): void {
  inspectionDaylightEnabled = !inspectionDaylightEnabled

  viewer.clock.currentTime = inspectionDaylightEnabled
    ? inspectionDaylightTime.clone()
    : JulianDate.now()

  daylightButton.textContent = inspectionDaylightEnabled
    ? 'Lighting: Inspection Daylight'
    : 'Lighting: Real Time'

  daylightButton.setAttribute(
    'aria-pressed',
    String(inspectionDaylightEnabled),
  )
}

function applyBaseline(): void {
  viewer.scene.globe.enableLighting = true
  disableAutomaticScale()
  setLocalGeometryVisible(false)
  photorealisticTiles.show = false
  globalBuildings.show = true
  globalBuildings.style = undefined
  nightLightsLayer.show = false
  viewer.scene.globe.show = true
  viewer.scene.globe.material = undefined
  imageryLayer.show = true
  landCoverLayer.show = false
  surfaceLayer.show = false
  surfaceTmsLayer.show = false
  visualSurfaceTmsLayer.show = false
  continuousSurfaceTmsLayer.show = false

  viewer.scene.globe.lambertDiffuseMultiplier = 1
  viewer.scene.globe.atmosphereLightIntensity = 10

  imageryLayer.brightness = 1
  imageryLayer.contrast = 1
  imageryLayer.saturation = 1
  imageryLayer.gamma = 1

  lookLabel.textContent = 'Baseline'
}

function applyPhotorealistic(): void {
  disableAutomaticScale()
  setLocalGeometryVisible(false)

  photorealisticTiles.show = true
  globalBuildings.show = false
  nightLightsLayer.show = false
  viewer.scene.globe.show = false

  lookLabel.textContent = 'Google Photorealistic 3D'
}

function applyNightEarth(): void {
  disableAutomaticScale()
  setLocalGeometryVisible(false)

  photorealisticTiles.show = false
  globalBuildings.show = false

  viewer.scene.globe.show = true
  viewer.scene.globe.material = undefined

  imageryLayer.show = false
  landCoverLayer.show = false
  surfaceLayer.show = false
  surfaceTmsLayer.show = false
  visualSurfaceTmsLayer.show = false
  continuousSurfaceTmsLayer.show = false
  nightLightsLayer.show = true

  viewer.scene.globe.enableLighting = false
  viewer.scene.globe.lambertDiffuseMultiplier = 1
  viewer.scene.globe.atmosphereLightIntensity = 6

  lookLabel.textContent = 'NASA VIIRS Night Earth'
}

function applyEstCgWorld(): void {
  viewer.scene.globe.enableLighting = true
  disableAutomaticScale()
  setLocalGeometryVisible(false)

  photorealisticTiles.show = false
  globalBuildings.show = true
  nightLightsLayer.show = false
  globalBuildings.style = new Cesium3DTileStyle({
    color: "color('#d8cbb7')",
  })

  viewer.scene.globe.show = true
  viewer.scene.globe.material = terrainMaterial

  imageryLayer.show = false
  landCoverLayer.show = false
  surfaceLayer.show = false
  surfaceTmsLayer.show = false
  visualSurfaceTmsLayer.show = false
  continuousSurfaceTmsLayer.show = false

  viewer.scene.globe.lambertDiffuseMultiplier = 1.1
  viewer.scene.globe.atmosphereLightIntensity = 12

  lookLabel.textContent = 'Est CG World'
}

function applyEstLook(): void {
  viewer.scene.globe.enableLighting = true
  disableAutomaticScale()
  setLocalGeometryVisible(false)
  landCoverLayer.show = false
  surfaceLayer.show = false
  surfaceTmsLayer.show = false
  visualSurfaceTmsLayer.show = false
  continuousSurfaceTmsLayer.show = false
  imageryLayer.show = false
  viewer.scene.globe.material = terrainMaterial

  viewer.scene.globe.lambertDiffuseMultiplier = 1.15
  viewer.scene.globe.atmosphereLightIntensity = 12

  lookLabel.textContent = 'Terrain Study'
}

function applyLandCover(): void {
  viewer.scene.globe.enableLighting = true
  disableAutomaticScale()
  setLocalGeometryVisible(false)
  viewer.scene.globe.material = undefined
  imageryLayer.show = false
  landCoverLayer.show = true
  surfaceLayer.show = false
  surfaceTmsLayer.show = false
  visualSurfaceTmsLayer.show = false
  continuousSurfaceTmsLayer.show = false

  viewer.scene.globe.lambertDiffuseMultiplier = 1
  viewer.scene.globe.atmosphereLightIntensity = 10

  lookLabel.textContent = 'USGS Land Cover'

}

function applyEstSurface(): void {
  viewer.scene.globe.enableLighting = true
  disableAutomaticScale()
  setLocalGeometryVisible(false)
  viewer.scene.globe.material = undefined
  imageryLayer.show = false
  landCoverLayer.show = false
  surfaceLayer.show = true
  surfaceTmsLayer.show = false
  visualSurfaceTmsLayer.show = false
  continuousSurfaceTmsLayer.show = false

  viewer.scene.globe.lambertDiffuseMultiplier = 1
  viewer.scene.globe.atmosphereLightIntensity = 10

  lookLabel.textContent = 'Est Surface Study'

}

function applyEstSurfaceTms(): void {
  viewer.scene.globe.enableLighting = true
  disableAutomaticScale()
  setLocalGeometryVisible(false)
  viewer.scene.globe.material = undefined
  imageryLayer.show = false
  landCoverLayer.show = false
  surfaceLayer.show = false
  surfaceTmsLayer.show = true
  visualSurfaceTmsLayer.show = false
  continuousSurfaceTmsLayer.show = false

  viewer.scene.globe.lambertDiffuseMultiplier = 1
  viewer.scene.globe.atmosphereLightIntensity = 10

  lookLabel.textContent = 'Est Surface TMS'

}

function applyVisualSurface(): void {
  viewer.scene.globe.enableLighting = true
  disableAutomaticScale()
  setLocalGeometryVisible(false)
  viewer.scene.globe.material = undefined
  imageryLayer.show = false
  landCoverLayer.show = false
  surfaceLayer.show = false
  surfaceTmsLayer.show = false
  visualSurfaceTmsLayer.show = true
  continuousSurfaceTmsLayer.show = false

  viewer.scene.globe.lambertDiffuseMultiplier = 1
  viewer.scene.globe.atmosphereLightIntensity = 10

  lookLabel.textContent = 'Est Visual Surface'
}

function applyContinuousSurface(): void {
  viewer.scene.globe.enableLighting = true
  disableAutomaticScale()
  setLocalGeometryVisible(false)
  viewer.scene.globe.material = undefined
  imageryLayer.show = false
  landCoverLayer.show = false
  surfaceLayer.show = false
  surfaceTmsLayer.show = false
  visualSurfaceTmsLayer.show = false
  continuousSurfaceTmsLayer.show = true

  viewer.scene.globe.lambertDiffuseMultiplier = 1
  viewer.scene.globe.atmosphereLightIntensity = 10

  lookLabel.textContent = 'Est Continuous Surface'
}

function flyToLocalSceneEvaluation(): void {
  viewer.camera.flyTo({
    destination: Cartesian3.fromDegrees(
      localSceneEvaluation.longitude,
      localSceneEvaluation.latitude,
      localSceneEvaluation.cameraHeight,
    ),
    duration: 1.5,
  })
}

function applyLocalGeometry(): void {
  viewer.scene.globe.enableLighting = true
  disableAutomaticScale()
  viewer.scene.globe.material = undefined
  imageryLayer.show = true
  landCoverLayer.show = false
  surfaceLayer.show = false
  surfaceTmsLayer.show = false
  visualSurfaceTmsLayer.show = false
  continuousSurfaceTmsLayer.show = false
  setLocalGeometryVisible(true)

  viewer.scene.globe.lambertDiffuseMultiplier = 1
  viewer.scene.globe.atmosphereLightIntensity = 10

  imageryLayer.brightness = 1
  imageryLayer.contrast = 1
  imageryLayer.saturation = 1
  imageryLayer.gamma = 1

  lookLabel.textContent = `Est Local Geometry · ${localSceneEvaluation.name}`

  flyToLocalSceneEvaluation()
}

function applyTerrainGeometry(): void {
  viewer.scene.globe.enableLighting = true
  disableAutomaticScale()
  imageryLayer.show = false
  landCoverLayer.show = false
  surfaceLayer.show = false
  surfaceTmsLayer.show = false
  visualSurfaceTmsLayer.show = false
  continuousSurfaceTmsLayer.show = false
  viewer.scene.globe.material = terrainMaterial
  setLocalGeometryVisible(true)

  viewer.scene.globe.lambertDiffuseMultiplier = 1.15
  viewer.scene.globe.atmosphereLightIntensity = 12

  lookLabel.textContent = `Est Terrain + Local Geometry · ${localSceneEvaluation.name}`

  flyToLocalSceneEvaluation()
}

estSurfaceButton.addEventListener('click', applyEstSurface)
estSurfaceTmsButton.addEventListener(
  'click',
  applyEstSurfaceTms,
)
visualSurfaceButton.addEventListener(
  'click',
  applyVisualSurface,
)
continuousSurfaceButton.addEventListener(
  'click',
  applyContinuousSurface,
)
localGeometryButton.addEventListener(
  'click',
  applyLocalGeometry,
)
terrainGeometryButton.addEventListener(
  'click',
  applyTerrainGeometry,
)
automaticScaleButton.addEventListener(
  'click',
  enableAutomaticScale,
)
viewer.scene.postRender.addEventListener(
  applyAutomaticPresentation,
)
daylightButton.addEventListener(
  'click',
  applyInspectionLighting,
)

baselineButton.addEventListener('click', applyBaseline)
photorealisticButton.addEventListener(
  'click',
  applyPhotorealistic,
)
nightEarthButton.addEventListener(
  'click',
  applyNightEarth,
)
estCgButton.addEventListener(
  'click',
  applyEstCgWorld,
)
estButton.addEventListener('click', applyEstLook)
landCoverButton.addEventListener('click', applyLandCover)

applyBaseline()
