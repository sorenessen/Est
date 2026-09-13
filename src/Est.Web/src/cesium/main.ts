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
      <div id="sessionStatus">No simulation session selected.</div>
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
  }>,
  planetId: string,
) {
  populationPoints.removeAll()

  const visiblePopulation =
    population.filter(
      person => person.planetId === planetId,
    )

  for (const person of visiblePopulation) {
    populationPoints.add({
      id: person.personId,
      position: Cartesian3.fromDegrees(
        person.longitudeDegrees,
        person.latitudeDegrees,
        100,
      ),
      pixelSize: 7,
      color: Color.fromCssColorString('#ffd166'),
      outlineColor: Color.BLACK,
      outlineWidth: 1,
    })
  }

  return visiblePopulation.length
}

const queryParameters =
  new URLSearchParams(window.location.search)

const sessionId = queryParameters.get('session')

if (sessionId) {
  const api = new EstApi('/api')

  try {
    const [session, world] = await Promise.all([
      api.getSession(sessionId),
      api.getWorld(sessionId),
    ])

    const planet = world.planets[0]

    if (planet) {
      const populationCount =
        renderPopulation(
          world.population,
          planet.planetId,
        )

      sessionStatus.textContent =
        `${planet.name} · ` +
        `${planet.environment.meanSurfaceTemperatureKelvin.toFixed(2)} K · ` +
        `Population ${populationCount.toLocaleString()} · ` +
        `t=${session.currentTimeSeconds}s`
    } else {
      populationPoints.removeAll()

      sessionStatus.textContent =
        `Session ${session.sessionId} · no planets`
    }
  } catch (error) {
    console.error(error)
    sessionStatus.textContent = 'Simulation session could not be loaded.'
  }
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
