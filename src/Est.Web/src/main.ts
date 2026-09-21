import './style.css'

import {
  ArcRotateCamera,
  Color3,
  Color4,
  DirectionalLight,
  DynamicTexture,
  Engine,
  HemisphericLight,
  Mesh,
  Scene,
  StandardMaterial,
  Texture,
  Vector3,
  VertexBuffer,
  VertexData,
} from '@babylonjs/core'

import '@babylonjs/core/Meshes/thinInstanceMesh.js'

import {
  sphereDirectionToGeographicDegrees,
  type CubeFace,
} from './planet/cube-sphere'

import {
  CreateIcoSphereVertexData,
} from '@babylonjs/core/Meshes/Builders/icoSphereBuilder.js'

import {
  CreateDisc,
} from '@babylonjs/core/Meshes/Builders/discBuilder.js'

import {
  CreateCapsule,
} from '@babylonjs/core/Meshes/Builders/capsuleBuilder.js'

import {
  CreateCylinder,
} from '@babylonjs/core/Meshes/Builders/cylinderBuilder.js'

import {
  CreateSphere,
} from '@babylonjs/core/Meshes/Builders/sphereBuilder.js'

import {
  CreateGround,
} from '@babylonjs/core/Meshes/Builders/groundBuilder.js'

import {
  EstApi,
  type ManifestedEsterResponse,
} from './api/est-api'

import {
  resolveEsterIdentity,
} from './player/ester-identity'
import {
  createEsterCapsuleMaterial,
  resolveEsterCapsuleSkin,
} from './player/ester-capsule-skin'

import {
  moveSurfaceCoordinate,
} from './player/surface-movement'

import {
  geographicToLocalMeters,
} from './player/local-play-space'

import {
  projectLocalIndividualActors,
} from './player/local-actor-projection'

import {
  LocalActorMotionTracker,
} from './player/local-actor-motion'

import {
  createLocalActorPresentationTransition,
  sampleLocalActorPresentationTransition,
  type LocalActorPresentationPosition,
  type LocalActorPresentationTransition,
} from './player/local-actor-interpolation'

import {
  resolveWolfPresentationState,
  type WolfPresentationState,
} from './player/wolf-presentation-state'

import {
  projectLocalGrazers,
} from './player/local-grazer-projection'

import {
  resolveGrazerPresentationState,
  type GrazerPresentationState,
} from './player/grazer-presentation-state'

import {
  createAnimatedGrazerPresentation,
  type GrazerPresentation,
} from './player/grazer-presentation'

import {
  createAnimatedWolfPresentation,
  type WolfPresentation,
} from './player/wolf-presentation'

import {
  createHumanPresentation,
  type HumanPresentation,
} from './player/human-presentation'
import {
  formatPersonEncounterStatus,
} from './player/person-encounter-presentation'

import {
  createTerrainHeightField,
} from './surface/terrain-height-field'

import {
  sampleTerrainPresentationDetailMeters,
  sampleTerrainSurfaceAppearance,
} from './surface/terrain-presentation-detail'
import {
  createTerrainDetailMapPixels,
} from './surface/terrain-detail-map'
import {
  createTerrainSurfaceScatter,
} from './surface/terrain-surface-scatter'
import {
  createAnimatedGrassMaterial,
  setAnimatedGrassAnchor,
} from './surface/animated-grass-material'

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

  <aside
    id="livingMarkerLegend"
    class="living-marker-legend"
    aria-label="Living world marker legend"
    hidden
  >
    <div class="living-marker-legend-heading">
      <strong>Marker legend</strong>
      <span>Color shows current state or presentation category</span>
    </div>

    <div class="living-marker-legend-section">
      <strong>Humans · live activity</strong>

      <div
        id="livingMarkerHumanActivities"
        class="living-marker-human-activities"
      >
        <span class="living-marker-legend-waiting">
          Waiting for live population state…
        </span>
      </div>

      <div class="living-marker-legend-encoding">
        Color encodes current activity only.
        Sex is identified on hover, not by color.
        Counts are the current live simulation state.
      </div>
    </div>

    <div class="living-marker-legend-section">
      <strong>Other living markers</strong>

      <div class="living-marker-legend-other">
        <div class="living-marker-legend-other-row">
          <span class="legend-marker legend-wolf"></span>
          <span>
            <strong>Wolf</strong>
            <small>
              White marker with red center.
              Traveling and attacking are emphasized by size;
              hover shows exact activity.
            </small>
          </span>
        </div>

        <div class="living-marker-legend-other-row">
          <span class="legend-marker legend-bird"></span>
          <span>
            <strong>Bird flock</strong>
            <small>
              Blue marker; size reflects flock membership.
            </small>
          </span>
        </div>

        <div class="living-marker-legend-other-row">
          <span class="legend-marker legend-grazer"></span>
          <span>
            <strong>Grazer group</strong>
            <small>
              Orange marker; size reflects represented grazers.
            </small>
          </span>
        </div>

        <div class="living-marker-legend-other-row">
          <span class="legend-marker legend-invertebrate"></span>
          <span>
            <strong>Invertebrate sample</strong>
            <small>
              Green marker; size reflects sampled live biomass.
            </small>
          </span>
        </div>
      </div>
    </div>

    <div class="living-marker-legend-note">
      Hover any marker for its authoritative instance,
      status, condition, population, or biomass details.
    </div>
  </aside>

  <div
    id="livingMarkerTooltip"
    class="living-marker-tooltip"
    role="tooltip"
    hidden
  >
    <strong id="livingMarkerTooltipTitle"></strong>
    <span id="livingMarkerTooltipDetail"></span>
  </div>

  <div
    id="panelVisibilityTip"
    class="panel-visibility-tip"
    role="status"
    aria-live="polite"
    hidden
  ></div>

  <section
    id="observerPanel"
    class="babylon-observer-panel"
    aria-label="Est observatory"
  >
    <div
      id="observerPanelDragHandle"
      class="babylon-observer-heading"
    >
      <div class="babylon-observer-heading-copy">
        <strong>Est Observatory</strong>
        <span>Live authoritative simulation</span>
      </div>

      <div class="render-evaluation-window-actions">
        <button
          id="observerPanelLegendButton"
          type="button"
          aria-label="Show marker legend"
          aria-expanded="false"
          title="Marker legend"
        >Legend</button>

        <button
          id="observerPanelMinimizeButton"
          type="button"
          aria-label="Minimize observatory"
          title="Minimize observatory"
        >Minimize</button>
      </div>
    </div>

    <div class="babylon-observer-view-switch">
      <button
        id="viewModeButton"
        type="button"
        aria-label="Enter embodied world view"
        title="Enter embodied world view"
      >Enter World</button>

      <button
        id="faunaProofStepButton"
        type="button"
        aria-label="Advance authoritative simulation by one second"
        title="Development proof: advance authoritative simulation by one second"
        hidden
      >Step Fauna +1s</button>
    </div>

    <div id="observerPanelBody" class="babylon-observer-body">
      <section
        id="sessionStatus"
        class="simulation-telemetry babylon-simulation-telemetry"
        aria-label="Simulation telemetry"
      >
        <div class="simulation-telemetry-empty">
          Loading live simulation telemetry…
        </div>
      </section>

      <details
        class="simulation-diagnostics babylon-observer-diagnostics"
      >
        <summary>Diagnostics</summary>

        <div class="babylon-diagnostics-grid">
          <div>
            <span>Renderer</span>
            <strong>R4 · Babylon immutable sphere</strong>
          </div>

          <div>
            <span>Terrain</span>
            <strong id="terrainStatus">preparing terrain…</strong>
          </div>

          <div>
            <span>Geometry</span>
            <strong id="lodStatus">building spherical terrain…</strong>
          </div>

          <div>
            <span>Living presentation</span>
            <strong id="faunaStatus">
              fauna symbols waiting for simulation session…
            </strong>
          </div>

          <div>
            <span>Navigation</span>
            <strong>drag to orbit · wheel to zoom</strong>
          </div>
        </div>
      </details>
    </div>
  </section>

  <button
    id="observerPanelRestoreButton"
    class="render-evaluation-restore babylon-observer-restore"
    type="button"
    aria-label="Show Est observatory"
    title="Show observatory"
    hidden
  >Observatory</button>
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

const livingMarkerLegend =
  document.querySelector<HTMLElement>(
    '#livingMarkerLegend',
  )

const observerPanelLegendButton =
  document.querySelector<HTMLButtonElement>(
    '#observerPanelLegendButton',
  )

const livingMarkerHumanActivities =
  document.querySelector<HTMLElement>(
    '#livingMarkerHumanActivities',
  )

const livingMarkerTooltip =
  document.querySelector<HTMLDivElement>(
    '#livingMarkerTooltip',
  )

const livingMarkerTooltipTitle =
  document.querySelector<HTMLElement>(
    '#livingMarkerTooltipTitle',
  )

const livingMarkerTooltipDetail =
  document.querySelector<HTMLElement>(
    '#livingMarkerTooltipDetail',
  )

if (
  !livingMarkerLegend ||
  !observerPanelLegendButton ||
  !livingMarkerHumanActivities ||
  !livingMarkerTooltip ||
  !livingMarkerTooltipTitle ||
  !livingMarkerTooltipDetail
) {
  throw new Error(
    'Living marker tooltip was not created.',
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

// Local embodied play uses a tangent-plane coordinate system:
// X = east, Y = up, Z = north. The planetary sunlight vector is
// expressed in the planet's global Cartesian frame, so it must be
// transformed before it can correctly light the local play surface.
const playSkyLight =
  new HemisphericLight(
    'play-sky-light',
    new Vector3(
      0,
      1,
      0,
    ),
    scene,
  )

playSkyLight.intensity =
  0

playSkyLight.diffuse =
  new Color3(
    0.82,
    0.90,
    1.0,
  )

playSkyLight.groundColor =
  new Color3(
    0.28,
    0.24,
    0.19,
  )

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

const requestedView =
  queryParameters.get('view')

if (
  requestedView !== null &&
  requestedView !== 'observatory' &&
  requestedView !== 'embodied'
) {
  throw new Error(
    `Unsupported Est view '${requestedView}'.`,
  )
}

// `play=1` remains a compatibility alias while development links migrate
// to the explicit presentation-view contract.
const playMode =
  requestedView === 'embodied' ||
  (
    requestedView === null &&
    queryParameters.get('play') === '1'
  )

const viewMode:
  'observatory' | 'embodied' =
    playMode
      ? 'embodied'
      : 'observatory'

const faunaFocusRequested =
  queryParameters.get('focus') === 'fauna'

playSkyLight.intensity =
  playMode
    ? 0.72
    : 0

const terrainStatus =
  document.querySelector<HTMLSpanElement>(
    '#terrainStatus',
  )

const faunaStatus =
  document.querySelector<HTMLSpanElement>(
    '#faunaStatus',
  )

const sessionStatus =
  document.querySelector<HTMLElement>(
    '#sessionStatus',
  )

const observerPanel =
  document.querySelector<HTMLElement>(
    '#observerPanel',
  )

const viewModeButton =
  document.querySelector<HTMLButtonElement>(
    '#viewModeButton',
  )

const faunaProofStepButton =
  document.querySelector<HTMLButtonElement>(
    '#faunaProofStepButton',
  )

const observerPanelDragHandle =
  document.querySelector<HTMLElement>(
    '#observerPanelDragHandle',
  )

const observerPanelMinimizeButton =
  document.querySelector<HTMLButtonElement>(
    '#observerPanelMinimizeButton',
  )

const observerPanelRestoreButton =
  document.querySelector<HTMLButtonElement>(
    '#observerPanelRestoreButton',
  )

const panelVisibilityTip =
  document.querySelector<HTMLDivElement>(
    '#panelVisibilityTip',
  )

if (
  !observerPanel ||
  !viewModeButton ||
  !faunaProofStepButton ||
  !observerPanelDragHandle ||
  !observerPanelMinimizeButton ||
  !observerPanelRestoreButton ||
  !panelVisibilityTip
) {
  throw new Error(
    'Observatory panel controls were not created.',
  )
}

viewModeButton.textContent =
  viewMode === 'embodied'
    ? 'Return to Observatory'
    : 'Enter World'

viewModeButton.setAttribute(
  'aria-label',
  viewMode === 'embodied'
    ? 'Return to observatory view'
    : 'Enter embodied world view',
)

viewModeButton.title =
  viewMode === 'embodied'
    ? 'Return to observatory view'
    : 'Enter embodied world view'

viewModeButton.disabled =
  sessionId === null

faunaProofStepButton.hidden =
  !(
    playMode &&
    faunaFocusRequested &&
    sessionId !== null
  )

viewModeButton.addEventListener(
  'click',
  () => {
    if (sessionId === null) {
      return
    }

    const nextUrl =
      new URL(
        window.location.href,
      )

    nextUrl.searchParams.set(
      'session',
      sessionId,
    )

    nextUrl.searchParams.set(
      'view',
      viewMode === 'embodied'
        ? 'observatory'
        : 'embodied',
    )

    // Canonical links now use `view`; do not propagate the compatibility
    // alias when switching presentation modes.
    nextUrl.searchParams.delete(
      'play',
    )

    window.location.assign(
      nextUrl.toString(),
    )
  },
)

const observerPanelViewportMargin =
  8

const clampObserverPanelToViewport = (): void => {
  if (observerPanel.hidden) {
    return
  }

  const panelRect =
    observerPanel.getBoundingClientRect()

  const appRect =
    app.getBoundingClientRect()

  const maxLeft =
    Math.max(
      observerPanelViewportMargin,
      appRect.width -
        panelRect.width -
        observerPanelViewportMargin,
    )

  const maxTop =
    Math.max(
      observerPanelViewportMargin,
      appRect.height -
        panelRect.height -
        observerPanelViewportMargin,
    )

  const currentLeft =
    panelRect.left -
    appRect.left

  const currentTop =
    panelRect.top -
    appRect.top

  observerPanel.style.right =
    'auto'

  observerPanel.style.left =
    `${Math.min(
      Math.max(
        currentLeft,
        observerPanelViewportMargin,
      ),
      maxLeft,
    )}px`

  observerPanel.style.top =
    `${Math.min(
      Math.max(
        currentTop,
        observerPanelViewportMargin,
      ),
      maxTop,
    )}px`
}

const livingMarkerLegendGap =
  8

const positionLivingMarkerLegend = (): void => {
  if (
    livingMarkerLegend.hidden ||
    observerPanel.hidden
  ) {
    return
  }

  const panelRect =
    observerPanel.getBoundingClientRect()

  const legendRect =
    livingMarkerLegend.getBoundingClientRect()

  const viewportMargin =
    observerPanelViewportMargin

  const availableLeft =
    panelRect.left -
    viewportMargin

  const availableRight =
    window.innerWidth -
    panelRect.right -
    viewportMargin

  let side: 'left' | 'right'

  if (
    availableLeft >=
    legendRect.width +
      livingMarkerLegendGap
  ) {
    side =
      'left'
  } else if (
    availableRight >=
    legendRect.width +
      livingMarkerLegendGap
  ) {
    side =
      'right'
  } else {
    side =
      availableLeft >= availableRight
        ? 'left'
        : 'right'
  }

  const desiredLeft =
    side === 'left'
      ? panelRect.left -
        legendRect.width -
        livingMarkerLegendGap
      : panelRect.right +
        livingMarkerLegendGap

  const maxLeft =
    Math.max(
      viewportMargin,
      window.innerWidth -
        legendRect.width -
        viewportMargin,
    )

  const maxTop =
    Math.max(
      viewportMargin,
      window.innerHeight -
        legendRect.height -
        viewportMargin,
    )

  livingMarkerLegend.style.left =
    `${Math.min(
      Math.max(
        desiredLeft,
        viewportMargin,
      ),
      maxLeft,
    )}px`

  livingMarkerLegend.style.top =
    `${Math.min(
      Math.max(
        panelRect.top,
        viewportMargin,
      ),
      maxTop,
    )}px`

  livingMarkerLegend.dataset.side =
    side
}

const closeLivingMarkerLegend = (): void => {
  livingMarkerLegend.hidden =
    true

  observerPanelLegendButton.setAttribute(
    'aria-expanded',
    'false',
  )
}

observerPanelLegendButton.addEventListener(
  'click',
  () => {
    const willShow =
      livingMarkerLegend.hidden

    if (!willShow) {
      closeLivingMarkerLegend()
      return
    }

    livingMarkerLegend.hidden =
      false

    observerPanelLegendButton.setAttribute(
      'aria-expanded',
      'true',
    )

    requestAnimationFrame(
      positionLivingMarkerLegend,
    )
  },
)

const humanLegendEntries =
  [
    ['Idle', 'Idle', 'legend-human-idle'],
    ['Foraging', 'Foraging', 'legend-human-foraging'],
    ['Eating', 'Eating', 'legend-human-eating'],
    ['Traveling', 'Traveling', 'legend-human-traveling'],
    ['Fleeing', 'Fleeing', 'legend-human-fleeing'],
    ['SeekingPartner', 'Seeking partner', 'legend-human-seeking'],
    ['Mating', 'Mating', 'legend-human-mating'],
  ] as const

const renderHumanMarkerLegend = (
  activityCounts: Readonly<Record<string, number>>,
  pregnantPeople: number,
): void => {
  const activityRows =
    humanLegendEntries.map(
      ([activity, label, markerClass]) => {
        const count =
          activityCounts[activity] ?? 0

        return `
          <div
            class="living-marker-legend-live-row${count === 0 ? ' is-inactive' : ''}"
          >
            <span
              class="legend-marker ${markerClass}"
              aria-hidden="true"
            ></span>
            <span>${label}</span>
            <strong>${count.toLocaleString()}</strong>
          </div>
        `
      },
    ).join('')

  livingMarkerHumanActivities.innerHTML =
    `
      ${activityRows}

      <div
        class="living-marker-legend-live-row${pregnantPeople === 0 ? ' is-inactive' : ''}"
      >
        <span
          class="legend-marker legend-pregnancy"
          aria-hidden="true"
        ></span>
        <span>Pregnancy halo</span>
        <strong>${pregnantPeople.toLocaleString()}</strong>
      </div>
    `

  if (!livingMarkerLegend.hidden) {
    requestAnimationFrame(
      positionLivingMarkerLegend,
    )
  }
}

let observerPanelDragPointerId:
  number | undefined

let observerPanelDragStartPointerX =
  0

let observerPanelDragStartPointerY =
  0

let observerPanelDragStartLeft =
  0

let observerPanelDragStartTop =
  0

observerPanelDragHandle.addEventListener(
  'pointerdown',
  event => {
    if (
      event.target instanceof Element &&
      event.target.closest('button')
    ) {
      return
    }

    const panelRect =
      observerPanel.getBoundingClientRect()

    const appRect =
      app.getBoundingClientRect()

    observerPanelDragPointerId =
      event.pointerId

    observerPanelDragStartPointerX =
      event.clientX

    observerPanelDragStartPointerY =
      event.clientY

    observerPanelDragStartLeft =
      panelRect.left -
      appRect.left

    observerPanelDragStartTop =
      panelRect.top -
      appRect.top

    observerPanel.style.right =
      'auto'

    observerPanel.style.left =
      `${observerPanelDragStartLeft}px`

    observerPanelDragHandle.setPointerCapture(
      event.pointerId,
    )

    observerPanel.classList.add(
      'is-dragging',
    )

    event.preventDefault()
  },
)

observerPanelDragHandle.addEventListener(
  'pointermove',
  event => {
    if (
      observerPanelDragPointerId !==
      event.pointerId
    ) {
      return
    }

    const panelRect =
      observerPanel.getBoundingClientRect()

    const appRect =
      app.getBoundingClientRect()

    const maxLeft =
      Math.max(
        observerPanelViewportMargin,
        appRect.width -
          panelRect.width -
          observerPanelViewportMargin,
      )

    const maxTop =
      Math.max(
        observerPanelViewportMargin,
        appRect.height -
          panelRect.height -
          observerPanelViewportMargin,
      )

    const requestedLeft =
      observerPanelDragStartLeft +
      event.clientX -
      observerPanelDragStartPointerX

    const requestedTop =
      observerPanelDragStartTop +
      event.clientY -
      observerPanelDragStartPointerY

    observerPanel.style.left =
      `${Math.min(
        Math.max(
          requestedLeft,
          observerPanelViewportMargin,
        ),
        maxLeft,
      )}px`

    observerPanel.style.top =
      `${Math.min(
        Math.max(
          requestedTop,
          observerPanelViewportMargin,
        ),
        maxTop,
      )}px`

    positionLivingMarkerLegend()
  },
)

const finishObserverPanelDrag = (
  event: PointerEvent,
): void => {
  if (
    observerPanelDragPointerId !==
    event.pointerId
  ) {
    return
  }

  if (
    observerPanelDragHandle
      .hasPointerCapture(
        event.pointerId,
      )
  ) {
    observerPanelDragHandle
      .releasePointerCapture(
        event.pointerId,
      )
  }

  observerPanelDragPointerId =
    undefined

  observerPanel.classList.remove(
    'is-dragging',
  )
}

observerPanelDragHandle.addEventListener(
  'pointerup',
  finishObserverPanelDrag,
)

observerPanelDragHandle.addEventListener(
  'pointercancel',
  finishObserverPanelDrag,
)

const minimizeObserverPanel = (): void => {
  closeLivingMarkerLegend()

  const panelRect =
    observerPanel.getBoundingClientRect()

  const appRect =
    app.getBoundingClientRect()

  const left =
    panelRect.left -
    appRect.left

  const top =
    panelRect.top -
    appRect.top

  observerPanelRestoreButton.style.right =
    'auto'

  observerPanelRestoreButton.style.left =
    `${left}px`

  observerPanelRestoreButton.style.top =
    `${top}px`

  observerPanel.hidden =
    true

  observerPanelRestoreButton.hidden =
    false
}

const restoreObserverPanel = (): void => {
  const restoreRect =
    observerPanelRestoreButton
      .getBoundingClientRect()

  const appRect =
    app.getBoundingClientRect()

  const anchorLeft =
    restoreRect.left -
    appRect.left

  const anchorRight =
    restoreRect.right -
    appRect.left

  const anchorTop =
    restoreRect.top -
    appRect.top

  observerPanelRestoreButton.hidden =
    true

  observerPanel.style.right =
    'auto'

  observerPanel.style.left =
    `${anchorLeft}px`

  observerPanel.style.top =
    `${anchorTop}px`

  observerPanel.hidden =
    false

  const panelRect =
    observerPanel.getBoundingClientRect()

  const availableRight =
    appRect.width -
    anchorLeft

  const availableLeft =
    anchorRight

  let requestedLeft =
    anchorLeft

  if (
    availableRight <
      panelRect.width &&
    availableLeft >=
      panelRect.width
  ) {
    // The pill is near the right edge:
    // open the Observatory leftward from it.
    requestedLeft =
      anchorRight -
      panelRect.width
  } else if (
    availableRight <
    panelRect.width
  ) {
    requestedLeft =
      anchorLeft -
      panelRect.width / 2
  }

  const maxLeft =
    Math.max(
      0,
      appRect.width -
        panelRect.width,
    )

  const maxTop =
    Math.max(
      0,
      appRect.height -
        panelRect.height,
    )

  observerPanel.style.left =
    `${Math.min(
      Math.max(
        requestedLeft,
        0,
      ),
      maxLeft,
    )}px`

  observerPanel.style.top =
    `${Math.min(
      Math.max(
        anchorTop,
        0,
      ),
      maxTop,
    )}px`

  requestAnimationFrame(
    positionLivingMarkerLegend,
  )
}

observerPanelMinimizeButton.addEventListener(
  'click',
  minimizeObserverPanel,
)

let observerRestoreDragPointerId:
  number | undefined

let observerRestoreDragStartX =
  0

let observerRestoreDragStartLeft =
  0

let observerRestoreDragged =
  false

const observerRestoreDragThreshold =
  4

observerPanelRestoreButton.addEventListener(
  'pointerdown',
  event => {
    const restoreRect =
      observerPanelRestoreButton
        .getBoundingClientRect()

    const appRect =
      app.getBoundingClientRect()

    observerRestoreDragPointerId =
      event.pointerId

    observerRestoreDragStartX =
      event.clientX

    observerRestoreDragStartLeft =
      restoreRect.left -
      appRect.left

    observerRestoreDragged =
      false

    observerPanelRestoreButton.style.right =
      'auto'

    observerPanelRestoreButton.style.left =
      `${observerRestoreDragStartLeft}px`

    observerPanelRestoreButton
      .setPointerCapture(
        event.pointerId,
      )

    observerPanelRestoreButton
      .classList.add(
        'is-dragging',
      )
  },
)

observerPanelRestoreButton.addEventListener(
  'pointermove',
  event => {
    if (
      observerRestoreDragPointerId !==
      event.pointerId
    ) {
      return
    }

    const deltaX =
      event.clientX -
      observerRestoreDragStartX

    if (
      Math.abs(deltaX) >=
      observerRestoreDragThreshold
    ) {
      observerRestoreDragged =
        true
    }

    const appRect =
      app.getBoundingClientRect()

    const restoreRect =
      observerPanelRestoreButton
        .getBoundingClientRect()

    const maxLeft =
      Math.max(
        0,
        appRect.width -
          restoreRect.width,
      )

    const requestedLeft =
      observerRestoreDragStartLeft +
      deltaX

    observerPanelRestoreButton.style.left =
      `${Math.min(
        Math.max(
          requestedLeft,
          0,
        ),
        maxLeft,
      )}px`

    observerPanelRestoreButton.style.right =
      'auto'

    if (observerRestoreDragged) {
      event.preventDefault()
    }
  },
)

const finishObserverRestoreDrag = (
  event: PointerEvent,
): void => {
  if (
    observerRestoreDragPointerId !==
    event.pointerId
  ) {
    return
  }

  if (
    observerPanelRestoreButton
      .hasPointerCapture(
        event.pointerId,
      )
  ) {
    observerPanelRestoreButton
      .releasePointerCapture(
        event.pointerId,
      )
  }

  observerRestoreDragPointerId =
    undefined

  observerPanelRestoreButton
    .classList.remove(
      'is-dragging',
    )
}

observerPanelRestoreButton.addEventListener(
  'pointerup',
  finishObserverRestoreDrag,
)

observerPanelRestoreButton.addEventListener(
  'pointercancel',
  finishObserverRestoreDrag,
)

observerPanelRestoreButton.addEventListener(
  'click',
  event => {
    if (observerRestoreDragged) {
      observerRestoreDragged =
        false

      event.preventDefault()
      return
    }

    restoreObserverPanel()
  },
)

let panelVisibilityTipTimer:
  number | undefined

let legendWasOpenBeforeCleanView =
  false

const dismissPanelVisibilityTip = (): void => {
  if (
    panelVisibilityTipTimer !==
    undefined
  ) {
    window.clearTimeout(
      panelVisibilityTipTimer,
    )

    panelVisibilityTipTimer =
      undefined
  }

  panelVisibilityTip.classList.remove(
    'is-visible',
  )

  panelVisibilityTip.hidden =
    true
}

const showPanelVisibilityTip = (
  message: string,
): void => {
  dismissPanelVisibilityTip()

  panelVisibilityTip.textContent =
    message

  panelVisibilityTip.hidden =
    false

  // Restart the transition even if the same
  // hint has just been displayed.
  void panelVisibilityTip.offsetWidth

  panelVisibilityTip.classList.add(
    'is-visible',
  )

  panelVisibilityTipTimer =
    window.setTimeout(
      () => {
        panelVisibilityTip.classList.remove(
          'is-visible',
        )

        panelVisibilityTip.hidden =
          true

        panelVisibilityTipTimer =
          undefined
      },
      5_000,
    )
}

const hideAllPanels = (): void => {
  legendWasOpenBeforeCleanView =
    !livingMarkerLegend.hidden

  closeLivingMarkerLegend()

  livingMarkerTooltip.hidden =
    true

  canvas.style.cursor =
    ''

  if (sessionStatus) {
    delete sessionStatus.dataset
      .livingHover
  }

  app.classList.add(
    'is-clean-viewer',
  )

  showPanelVisibilityTip(
    "Press 'Esc' to bring back panels",
  )
}

const restoreAllPanels = (): void => {
  app.classList.remove(
    'is-clean-viewer',
  )

  if (
    legendWasOpenBeforeCleanView &&
    !observerPanel.hidden
  ) {
    livingMarkerLegend.hidden =
      false

    observerPanelLegendButton.setAttribute(
      'aria-expanded',
      'true',
    )

    requestAnimationFrame(
      positionLivingMarkerLegend,
    )
  }

  showPanelVisibilityTip(
    "Press 'Esc' to hide all panels",
  )

  if (!observerPanel.hidden) {
    requestAnimationFrame(
      () => {
        clampObserverPanelToViewport()
        positionLivingMarkerLegend()
      },
    )
  }
}

window.addEventListener(
  'keydown',
  event => {
    if (event.key !== 'Escape') {
      return
    }

    event.preventDefault()

    if (
      app.classList.contains(
        'is-clean-viewer',
      )
    ) {
      restoreAllPanels()
    } else {
      hideAllPanels()
    }
  },
)

requestAnimationFrame(
  () => {
    showPanelVisibilityTip(
      "Press 'Esc' to hide all panels",
    )
  },
)

window.addEventListener(
  'resize',
  () =>
    requestAnimationFrame(
      () => {
        clampObserverPanelToViewport()
        positionLivingMarkerLegend()
      },
    ),
)

const observerPanelResizeObserver =
  new ResizeObserver(
    () =>
      requestAnimationFrame(
        () => {
          clampObserverPanelToViewport()
          positionLivingMarkerLegend()
        },
      ),
  )

observerPanelResizeObserver.observe(
  observerPanel,
)

requestAnimationFrame(
  clampObserverPanelToViewport,
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

let activePlanetMeanRadiusMeters:
  number | undefined

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

function createMarkerMaterial(
  name: string,
  color: Color3,
): StandardMaterial {
  const material =
    new StandardMaterial(
      name,
      scene,
    )

  material.diffuseColor =
    color

  material.emissiveColor =
    color

  material.specularColor =
    Color3.Black()

  material.disableLighting =
    true

  material.backFaceCulling =
    false

  return material
}

type LivingMarkerKind =
  | 'ester'
  | 'humans'
  | 'wolves'
  | 'birds'
  | 'grazers'
  | 'invertebrates'

interface LivingMarkerMetadata {
  kind: LivingMarkerKind
  title: string
  detail: string
}

const humanMarkerMaterials = {
  Idle: createMarkerMaterial(
    'human-idle-marker-material',
    Color3.FromHexString('#ffcc00'),
  ),
  Foraging: createMarkerMaterial(
    'human-foraging-marker-material',
    Color3.FromHexString('#ff7a00'),
  ),
  Eating: createMarkerMaterial(
    'human-eating-marker-material',
    Color3.FromHexString('#21d66f'),
  ),
  Traveling: createMarkerMaterial(
    'human-traveling-marker-material',
    Color3.FromHexString('#00c8ff'),
  ),
  Fleeing: createMarkerMaterial(
    'human-fleeing-marker-material',
    Color3.FromHexString('#ff2d9b'),
  ),
  SeekingPartner: createMarkerMaterial(
    'human-seeking-partner-marker-material',
    Color3.FromHexString('#7c4dff'),
  ),
  Mating: createMarkerMaterial(
    'human-mating-marker-material',
    Color3.FromHexString('#ff3d7f'),
  ),
} as const

const pregnancyMarkerMaterial =
  createMarkerMaterial(
    'pregnancy-marker-material',
    Color3.FromHexString('#00e5c0'),
  )

const wolfMarkerMaterial =
  createMarkerMaterial(
    'wolf-marker-material',
    Color3.FromHexString('#f2f2f2'),
  )

const wolfEyeMarkerMaterial =
  createMarkerMaterial(
    'wolf-eye-marker-material',
    Color3.FromHexString('#ff1f3d'),
  )

const birdMarkerMaterial =
  createMarkerMaterial(
    'bird-marker-material',
    Color3.FromHexString('#009dff'),
  )

const grazerMarkerMaterial =
  createMarkerMaterial(
    'grazer-marker-material',
    Color3.FromHexString('#d97706'),
  )

const invertebrateMarkerMaterial =
  createMarkerMaterial(
    'invertebrate-marker-material',
    Color3.FromHexString('#22c55e'),
  )

const esterMarkerMaterial =
  createMarkerMaterial(
    'ester-marker-material',
    Color3.FromHexString('#ffffff'),
  )

if (sessionId) {
  reportPlanetStartupStage(
    'loading simulation world…',
  )

  const api =
    new EstApi('/api')

  const esterId =
    playMode
      ? resolveEsterIdentity(
          window.localStorage,
          () =>
            crypto.randomUUID(),
        )
      : null

  // Temporary embodiment presentation only. Both capsule skins will be
  // retired when playable Ester receives its proper human avatar.
  const esterCapsuleSkin =
    resolveEsterCapsuleSkin(
      window.location.search,
    )

  let manifestedEster:
    ManifestedEsterResponse | null =
      null

  const observedTimelineEvents =
    new Set<string>()

  const cumulativeMetrics = {
    births: 0,
    demographicDeaths: 0,
    starvationDeaths: 0,
    feedingEvents: 0,
    travelFeedingEvents: 0,
    continuedFoodTravel: 0,
    demographicMigrations: 0,
    foodSeekingTravel: 0,
    scarcityMigrations: 0,
    noViableFoodFound: 0,
    wolfAttacks: 0,
    failedAttacks: 0,
    predationDeaths: 0,
    grazerKills: 0,
    wolfBirths: 0,
  }

  let world =
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
    playMode &&
    esterId !== null
  ) {
    manifestedEster =
      await api.getManifestedEster(
        sessionId,
        esterId,
      )

    if (
      manifestedEster === null
    ) {
      const nearbyPerson =
        world.population.find(
          person =>
            person.planetId ===
            planet.planetId,
        )

      const nearbyWolf =
        faunaFocusRequested
          ? world.animals.find(
              animal =>
                animal.planetId ===
                  planet.planetId &&
                animal.species ===
                  'Wolf',
            )
          : undefined

      const spawnAnchor =
        nearbyWolf ??
        nearbyPerson

      const spawnBase = {
        latitudeDegrees:
          spawnAnchor
            ?.latitudeDegrees ??
          0,
        longitudeDegrees:
          spawnAnchor
            ?.longitudeDegrees ??
          0,
      }

      const spawn =
        spawnAnchor
          ? moveSurfaceCoordinate(
              spawnBase,
              0,
              12,
              planet.meanRadiusMeters,
            )
          : spawnBase

      manifestedEster =
        await api.manifestEster(
          sessionId,
          esterId,
          planet.planetId,
          spawn.latitudeDegrees,
          spawn.longitudeDegrees,
        )
    }

    if (
      manifestedEster.planetId !==
      planet.planetId
    ) {
      throw new Error(
        'The persisted Ester is manifested on a planet that is not currently rendered.',
      )
    }
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

  let [
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

  activePlanetMeanRadiusMeters =
    meanRadiusMeters

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

  const updateVegetationCoverageState = () => {
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

  }

  updateVegetationCoverageState()

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

  const focusManifestedEster = () => {
    if (
      !playMode ||
      manifestedEster === null
    ) {
      return
    }

    camera.upVector =
      Vector3.Up()

    camera.lowerRadiusLimit =
      4

    camera.upperRadiusLimit =
      40

    camera.minZ =
      0.05

    camera.maxZ =
      500

    camera.setTarget(
      new Vector3(
        0,
        0.9,
        0,
      ),
    )

    camera.setPosition(
      new Vector3(
        6,
        4,
        7.5,
      ),
    )
  }

  let livingPresentationMeshes: Mesh[] = []
  let faunaFocusApplied = false

  const renderLivingWorld = () => {
    for (const mesh of livingPresentationMeshes) {
      mesh.dispose()
    }

    livingPresentationMeshes = []

    if (playMode) {
      return
    }

    const wolfJuvenileDisplayAgeSeconds =
      180 * 86_400

    const metersToPlanetRadius =
      1 /
      planet.meanRadiusMeters

    const livingSymbolRadialLift =
      playMode
        ? 1.2 *
          metersToPlanetRadius
        : 0.006

    const playableEsterDiameter =
      1.8 *
      metersToPlanetRadius

    const playableHumanDiameter =
      1.6 *
      metersToPlanetRadius

    const playablePregnancyHaloAddition =
      0.45 *
      metersToPlanetRadius

    const visiblePopulation =
      world.population.filter(
        person =>
          person.planetId ===
          planet.planetId,
      )

    const visibleWolves =
      playMode
        ? []
        : world.animals.filter(
            animal =>
              animal.planetId ===
                planet.planetId &&
              animal.species === 'Wolf',
          )

    const visibleBirdFlocks =
      playMode
        ? []
        : birdFlocks?.flocks ?? []

    const visibleGrazerCohorts =
      playMode
        ? []
        : grazerCohorts?.cohorts ?? []

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
      playMode
        ? []
        : selectSpatiallyDistributedInvertebrateCells(
            activeInvertebrateCells,
            64,
          )

    const createBillboardMarker = (
      name: string,
      material: StandardMaterial,
      latitudeDegrees: number,
      longitudeDegrees: number,
      diameter: number,
      metadata: LivingMarkerMetadata,
      radialLift =
        livingSymbolRadialLift,
      visibility = 1,
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

      const marker =
        CreateDisc(
          name,
          {
            radius:
              diameter / 2,
            tessellation: 24,
            sideOrientation:
              Mesh.DOUBLESIDE,
          },
          scene,
        )

      marker.position =
        position

      marker.material =
        material

      marker.visibility =
        visibility

      marker.billboardMode =
        Mesh.BILLBOARDMODE_ALL

      marker.isPickable =
        true

      marker.metadata = {
        livingMarker: metadata,
      }

      marker.renderingGroupId =
        2

      livingPresentationMeshes.push(
        marker,
      )

      return marker
    }

    const formatLivingAge = (
      birthTimeSeconds: number,
    ): string => {
      const ageSeconds =
        Math.max(
          0,
          world.currentTimeSeconds -
            birthTimeSeconds,
        )

      const ageYears =
        ageSeconds /
        31_536_000

      if (ageYears >= 1) {
        return `${ageYears.toFixed(
          ageYears >= 10
            ? 0
            : 1,
        )} years`
      }

      return `${Math.floor(
        ageSeconds / 86_400,
      )} days`
    }

    if (
      playMode &&
      manifestedEster !== null &&
      manifestedEster.planetId ===
        planet.planetId
    ) {
      createBillboardMarker(
        `ester-${manifestedEster.esterId}`,
        esterMarkerMaterial,
        manifestedEster.latitudeDegrees,
        manifestedEster.longitudeDegrees,
        playableEsterDiameter,
        {
          kind: 'ester',
          title: 'Ester',
          detail:
            `${manifestedEster.latitudeDegrees.toFixed(6)}°, ${manifestedEster.longitudeDegrees.toFixed(6)}° · WASD to walk`,
        },
        livingSymbolRadialLift,
        1,
      )
    }

    for (const person of visiblePopulation) {
      const energy =
        Math.max(
          0,
          Math.min(
            1,
            person.energyReserve,
          ),
        )

      const health =
        Math.max(
          0,
          Math.min(
            1,
            person.health,
          ),
        )

      const material =
        humanMarkerMaterials[
          person.activity as
            keyof typeof humanMarkerMaterials
        ] ??
        humanMarkerMaterials.Idle

      const coreDiameter =
        playMode
          ? playableHumanDiameter
          : person.activity === 'Fleeing'
            ? 0.020
            : person.activity === 'Traveling'
              ? 0.018
              : 0.014 +
                energy * 0.0035

      if (person.isPregnant) {
        createBillboardMarker(
          `human-${person.personId}-pregnancy-halo`,
          pregnancyMarkerMaterial,
          person.latitudeDegrees,
          person.longitudeDegrees,
          coreDiameter +
            (
              playMode
                ? playablePregnancyHaloAddition
                : 0.012
            ),
          {
            kind: 'humans',
            title:
              `Human · ${person.sex}`,
            detail:
              `${formatLivingAge(person.birthTimeSeconds)} · Status: ${person.activity} · Health ${Math.round(health * 100)}% · Energy ${Math.round(energy * 100)}% · Pregnant`,
          },
          livingSymbolRadialLift,
          0.42,
        )
      }

      createBillboardMarker(
        `human-${person.personId}`,
        material,
        person.latitudeDegrees,
        person.longitudeDegrees,
        coreDiameter,
        {
          kind: 'humans',
          title:
            `Human · ${person.sex}`,
          detail:
            `${formatLivingAge(person.birthTimeSeconds)} · Status: ${person.activity} · Health ${Math.round(health * 100)}% · Energy ${Math.round(energy * 100)}%${person.isPregnant ? ' · Pregnant' : ''}`,
        },
        playMode
          ? livingSymbolRadialLift
          : livingSymbolRadialLift +
            0.0004,
        0.72 +
          health * 0.28,
      )
    }

    for (const wolf of visibleWolves) {
      const wolfEnergy =
        Math.max(
          0,
          Math.min(
            1,
            wolf.energyReserve,
          ),
        )

      const wolfHealth =
        Math.max(
          0,
          Math.min(
            1,
            wolf.health,
          ),
        )

      const wolfMetadata: LivingMarkerMetadata = {
        kind: 'wolves',
        title:
          `Wolf · ${wolf.sex ?? 'Unknown sex'}`,
        detail:
          `${formatLivingAge(wolf.birthTimeSeconds)} · Status: ${wolf.activity} · Health ${Math.round(wolfHealth * 100)}% · Energy ${Math.round(wolfEnergy * 100)}%${wolf.isPregnant ? ' · Pregnant' : ''}`,
      }

      const markerDiameter =
        wolf.activity === 'Attacking'
          ? 0.025
          : wolf.activity === 'Traveling'
            ? 0.022
            : 0.019

      createBillboardMarker(
        `wolf-${wolf.animalId}`,
        wolfMarkerMaterial,
        wolf.latitudeDegrees,
        wolf.longitudeDegrees,
        markerDiameter,
        wolfMetadata,
        livingSymbolRadialLift +
          0.001,
      )

      createBillboardMarker(
        `wolf-${wolf.animalId}-eye`,
        wolfEyeMarkerMaterial,
        wolf.latitudeDegrees,
        wolf.longitudeDegrees,
        0.006,
        wolfMetadata,
        livingSymbolRadialLift +
          0.0014,
      )
    }

    for (const flock of visibleBirdFlocks) {
      const scale =
        Math.min(
          0.042,
          0.022 +
            Math.log10(
              flock.memberCount + 1,
            ) *
              0.003,
        )

      createBillboardMarker(
        `bird-flock-${flock.flockId}`,
        birdMarkerMaterial,
        flock.latitudeDegrees,
        flock.longitudeDegrees,
        scale,
        {
          kind: 'birds',
          title: 'Bird flock',
          detail:
            `${flock.memberCount.toLocaleString()} birds`,
        },
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
          0.050,
          0.034 +
            Math.log10(
              cluster.memberCount + 1,
            ) *
              0.0018,
        )

      createBillboardMarker(
        `grazer-cluster-${cluster.key}`,
        grazerMarkerMaterial,
        cluster.latitudeDegrees,
        cluster.longitudeDegrees,
        scale,
        {
          kind: 'grazers',
          title: 'Grazer group',
          detail:
            `${cluster.memberCount.toLocaleString()} grazers · ${cluster.cohortCount.toLocaleString()} ${cluster.cohortCount === 1 ? 'cohort' : 'cohorts'}`,
        },
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
          0.030,
          0.015 +
            Math.log10(
              1 +
                cell.liveBiomassKilogramsPerSquareMeter *
                  1_000,
            ) *
              0.003,
        )

      createBillboardMarker(
        `invertebrates-${cell.surfaceCellId}`,
        invertebrateMarkerMaterial,
        surfaceCell.centerLatitudeDegrees,
        surfaceCell.centerLongitudeDegrees,
        scale,
        {
          kind: 'invertebrates',
          title: 'Invertebrates',
          detail:
            `Surface cell ${cell.surfaceCellId} · ${cell.liveBiomassKilogramsPerSquareMeter.toLocaleString(undefined, { maximumSignificantDigits: 3 })} kg/m² live biomass`,
        },
        0.009,
        0.42,
      )

      renderedInvertebrateCells +=
        1
    }

    if (
      faunaFocusRequested &&
      !faunaFocusApplied
    ) {
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

        faunaFocusApplied =
          true
      }
    }

    if (faunaStatus) {
      const simulatedDay =
        world.currentTimeSeconds /
        86_400

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
        `Day ${simulatedDay.toFixed(0)} · living world · ${visiblePopulation.length} humans · ${visibleWolves.length} wolves (${juvenileWolfCount} pups) · ${visibleBirdFlocks.length} bird flocks · ${grazerPresentationClusters.length} grazer markers representing ${formatLivingCount(totalGrazerMembers)} grazers / ${visibleGrazerCohorts.length} cohorts · ${activeVegetationCells.length} active vegetation cells · ${renderedInvertebrateCells} spatial insect samples / ${activeInvertebrateCells.length} active cells${faunaFocusRequested ? ' · FAUNA FOCUS' : ''}`
    }

  }

  const playGroundMaterial =
    new StandardMaterial(
      'play-ground-material',
      scene,
    )

  // Vertex colors provide the local terrain's presentation albedo.
  // Keep the material itself neutral so those colors remain visible
  // under directional lighting rather than being multiplied by another
  // strong green tint.
  playGroundMaterial.diffuseColor =
    new Color3(
      1,
      1,
      1,
    )

  // A very small ambient lift keeps shadowed terrain readable without
  // flattening the normals into the self-lit appearance of the earlier
  // temporary green material.
  playGroundMaterial.emissiveColor =
    Color3.Black()

  playGroundMaterial.specularColor =
    new Color3(
      0,
      0,
      0,
    )

  let playGroundMesh:
    Mesh | null =
      null

  let playTerrainAnchor:
    ManifestedEsterResponse | null =
      null

  let playTerrainAnchorElevationMeters =
    0

  const playTerrainSizeMeters =
    320

  const playTerrainSubdivisions =
    40

  const playTerrainReanchorDistanceMeters =
    32

  const playTerrainPresentationDetailAmplitudeMeters =
    22.85

  const playTerrainAppearanceIdentity =
    `${planet.planetId}:surface-appearance`

  const playTerrainTextureSize =
    256

  const playTerrainDetailTextureSize =
    256

  const playTerrainDetailTileMeters =
    8

  let playGroundAlbedoTexture:
    DynamicTexture | null =
      null

  let playGroundNormalTexture:
    DynamicTexture | null =
      null

  let playGroundDetailTexture:
    DynamicTexture | null =
      null

  let playGroundDetailAnchor:
    ManifestedEsterResponse | null =
      null

  let playGroundDetailUOffset =
    0

  let playGroundDetailVOffset =
    0

  // Grass streaming is intentionally independent from the larger
  // terrain reanchor. The buffer is wider than its visible fade radius,
  // and it rolls forward frequently enough that its hard edge remains
  // hidden outside the visible meadow.
  const playSurfaceScatterRadiusMeters =
    68

  const playSurfaceScatterReanchorDistanceMeters =
    8

  const playSurfaceScatterFadeStartMeters =
    28

  const playSurfaceScatterFadeEndMeters =
    55

  let playSurfaceScatterAnchor:
    ManifestedEsterResponse | null =
      null

  let playSurfaceScatterAnchorElevationMeters =
    0

  type PlaySurfaceScatterTransform = {
    eastMeters: number
    elevationMeters: number
    northMeters: number
    scaleX: number
    scaleY: number
    scaleZ: number
    yawRadians: number
  }

  let playGrassScatterMesh:
    Mesh | null =
      null

  let playGrassMaterial:
    ReturnType<
      typeof createAnimatedGrassMaterial
    > | null =
      null

  let playStoneScatterMesh:
    Mesh | null =
      null

  let playEsterMesh:
    Mesh | null =
      null

  const playHumanPresentations =
    new Map<
      string,
      HumanPresentation
    >()

  const playHumanLoadRequests =
    new Map<
      string,
      Promise<void>
    >()

  const playHumanLoadFailures =
    new Set<string>()

  const playWolfPresentations =
    new Map<
      string,
      WolfPresentation
    >()

  const playWolfLoadRequests =
    new Map<
      string,
      Promise<void>
    >()

  const playWolfLoadFailures =
    new Set<string>()

  const playGrazerPresentations =
    new Map<
      string,
      GrazerPresentation
    >()

  const playGrazerLoadRequests =
    new Map<
      string,
      Promise<void>
    >()

  const playGrazerLoadFailures =
    new Set<string>()

  const playAnimalMotionTracker =
    new LocalActorMotionTracker()

  const playGrazerMotionTracker =
    new LocalActorMotionTracker()

  interface PlayWolfPresentationTransition {
    snapshotTimeSeconds: number
    interpolation:
      LocalActorPresentationTransition
    state:
      WolfPresentationState
    isComplete: boolean
  }

  interface PlayGrazerPresentationTransition {
    snapshotTimeSeconds: number
    interpolation:
      LocalActorPresentationTransition
    state:
      GrazerPresentationState
    isComplete: boolean
  }

  const playWolfPresentationTransitions =
    new Map<
      string,
      PlayWolfPresentationTransition
    >()

  const playGrazerPresentationTransitions =
    new Map<
      string,
      PlayGrazerPresentationTransition
    >()

  const wolfPresentationTransitionMilliseconds =
    450

  const grazerPresentationTransitionMilliseconds =
    450

  let presentationEster:
    ManifestedEsterResponse | null =
      manifestedEster === null
        ? null
        : {
            ...manifestedEster,
          }

  const samplePlayTerrainElevationMeters = (
    latitudeDegrees: number,
    longitudeDegrees: number,
  ) =>
    heightField.sampleHeightMeters(
      latitudeDegrees,
      longitudeDegrees,
    ) +
    sampleTerrainPresentationDetailMeters(
      latitudeDegrees,
      longitudeDegrees,
      planet.meanRadiusMeters,
      planet.planetId,
    )

  const createScatterMatrixBuffer = (
    transforms:
      PlaySurfaceScatterTransform[],
  ) => {
    const buffer =
      new Float32Array(
        transforms.length *
        16,
      )

    for (
      let index = 0;
      index <
        transforms.length;
      index += 1
    ) {
      const transform =
        transforms[
          index
        ]

      const cosine =
        Math.cos(
          transform.yawRadians,
        )

      const sine =
        Math.sin(
          transform.yawRadians,
        )

      const offset =
        index *
        16

      buffer[
        offset
      ] =
        cosine *
        transform.scaleX

      buffer[
        offset + 1
      ] =
        0

      buffer[
        offset + 2
      ] =
        -sine *
        transform.scaleX

      buffer[
        offset + 3
      ] =
        0

      buffer[
        offset + 4
      ] =
        0

      buffer[
        offset + 5
      ] =
        transform.scaleY

      buffer[
        offset + 6
      ] =
        0

      buffer[
        offset + 7
      ] =
        0

      buffer[
        offset + 8
      ] =
        sine *
        transform.scaleZ

      buffer[
        offset + 9
      ] =
        0

      buffer[
        offset + 10
      ] =
        cosine *
        transform.scaleZ

      buffer[
        offset + 11
      ] =
        0

      buffer[
        offset + 12
      ] =
        transform.eastMeters

      buffer[
        offset + 13
      ] =
        transform.elevationMeters

      buffer[
        offset + 14
      ] =
        transform.northMeters

      buffer[
        offset + 15
      ] =
        1
    }

    return buffer
  }

  const ensurePlaySurfaceScatterMeshes = () => {
    if (
      playGrassScatterMesh === null
    ) {
      playGrassScatterMesh =
        new Mesh(
          'play-grass-scatter',
          scene,
        )

      const grassPositions:
        number[] =
          []

      const grassIndices:
        number[] =
          []

      const grassNormals:
        number[] =
          []

      const grassBladeData:
        number[] =
          []

      const bladeCount =
        24

      const segmentCount =
        4

      const goldenAngle =
        2.399963229728653

      for (
        let bladeIndex = 0;
        bladeIndex <
          bladeCount;
        bladeIndex += 1
      ) {
        const normalizedIndex =
          (
            bladeIndex +
            0.5
          ) /
          bladeCount

        const angle =
          bladeIndex *
          goldenAngle +
          (
            (
              bladeIndex *
              17
            ) %
            7
          ) *
          0.041

        const radialDistance =
          Math.sqrt(
            normalizedIndex,
          ) *
          0.40

        const directionX =
          Math.cos(
            angle,
          )

        const directionZ =
          Math.sin(
            angle,
          )

        const rightX =
          -directionZ

        const rightZ =
          directionX

        const baseCenterX =
          directionX *
          radialDistance

        const baseCenterZ =
          directionZ *
          radialDistance

        const randomA =
          (
            (
              bladeIndex *
              37 +
              11
            ) %
            101
          ) /
          100

        const randomB =
          (
            (
              bladeIndex *
              61 +
              23
            ) %
            97
          ) /
          96

        const randomC =
          (
            (
              bladeIndex *
              43 +
              47
            ) %
            89
          ) /
          88

        const randomD =
          (
            (
              bladeIndex *
              73 +
              31
            ) %
            83
          ) /
          82

        // Three overlapping meadow strata:
        //
        // - short basal grass closes visible ground gaps
        // - medium blades form the main canopy
        // - sparse tall stems break the skyline
        const bladeHeight =
          randomD <
          0.52
            ? 0.24 +
              randomB *
                0.22
            : randomD <
              0.87
              ? 0.46 +
                randomB *
                  0.30
              : 0.80 +
                randomB *
                  0.38

        // Short basal blades are a little broader so they form
        // continuous meadow body. Tall stems are narrower.
        const baseHalfWidth =
          randomD <
          0.52
            ? 0.015 +
              randomA *
                0.015
            : randomD <
              0.87
              ? 0.011 +
                randomA *
                  0.013
              : 0.007 +
                randomA *
                  0.010

        // Even without wind, wild grass leans and arcs in many
        // directions instead of forming upright comb teeth.
        const staticLean =
          randomD <
          0.52
            ? 0.035 +
              randomC *
                0.105
            : randomD <
              0.87
              ? 0.060 +
                randomC *
                  0.165
              : 0.085 +
                randomC *
                  0.205

        const bladeBaseIndex =
          grassPositions.length /
          3

        for (
          let segment = 0;
          segment <=
            segmentCount;
          segment += 1
        ) {
          const fraction =
            segment /
            segmentCount

          const height =
            fraction *
            bladeHeight

          const taper =
            Math.pow(
              1 -
              fraction,
              0.68,
            )

          const halfWidth =
            Math.max(
              0.0015,
              baseHalfWidth *
              taper,
            )

          const curve =
            staticLean *
            fraction *
            fraction

          const centerX =
            baseCenterX +
            directionX *
            curve

          const centerZ =
            baseCenterZ +
            directionZ *
            curve

          grassPositions.push(
            centerX -
              rightX *
              halfWidth,
            height,
            centerZ -
              rightZ *
              halfWidth,

            centerX +
              rightX *
              halfWidth,
            height,
            centerZ +
              rightZ *
              halfWidth,
          )

          for (
            let side = 0;
            side < 2;
            side += 1
          ) {
            grassBladeData.push(
              randomA,
              randomB,
              randomC,
              randomD,
            )
          }
        }

        for (
          let segment = 0;
          segment <
            segmentCount;
          segment += 1
        ) {
          const row =
            bladeBaseIndex +
            segment *
              2

          const next =
            row +
            2

          grassIndices.push(
            row,
            row + 1,
            next,

            row + 1,
            next + 1,
            next,
          )
        }
      }

      grassNormals.length =
        grassPositions.length

      grassNormals.fill(
        0,
      )

      VertexData.ComputeNormals(
        grassPositions,
        grassIndices,
        grassNormals,
      )

      const grassVertexData =
        new VertexData()

      grassVertexData.positions =
        grassPositions

      grassVertexData.indices =
        grassIndices

      grassVertexData.normals =
        grassNormals

      grassVertexData.applyToMesh(
        playGrassScatterMesh,
      )

      playGrassScatterMesh
        .setVerticesData(
          'bladeData',
          grassBladeData,
          false,
          4,
        )

      playGrassMaterial =
        createAnimatedGrassMaterial(
          scene,
        )

      playGrassMaterial.setFloat(
        'uFadeStartMeters',
        playSurfaceScatterFadeStartMeters,
      )

      playGrassMaterial.setFloat(
        'uFadeEndMeters',
        playSurfaceScatterFadeEndMeters,
      )

      playGrassScatterMesh.material =
        playGrassMaterial

      playGrassScatterMesh.isPickable =
        false

      playGrassScatterMesh
        .alwaysSelectAsActiveMesh =
          true

      const grassAnimationStart =
        performance.now()

      scene.onBeforeRenderObservable.add(
        () => {
          if (
            playGrassMaterial === null
          ) {
            return
          }

          playGrassMaterial.setFloat(
            'uTimeSeconds',
            (
              performance.now() -
              grassAnimationStart
            ) /
            1000,
          )
        },
      )
    }

    if (
      playStoneScatterMesh === null
    ) {
      playStoneScatterMesh =
        new Mesh(
          'play-stone-scatter',
          scene,
        )

      const stonePositions:
        number[] =
          []

      const stoneIndices:
        number[] =
          []

      const ringCount =
        10

      const lowerRadii = [
        1.00,
        0.88,
        1.05,
        0.82,
        0.96,
        0.85,
        1.02,
        0.89,
        0.94,
        0.84,
      ]

      const upperRadii = [
        0.68,
        0.59,
        0.72,
        0.61,
        0.66,
        0.57,
        0.70,
        0.62,
        0.65,
        0.58,
      ]

      for (
        let vertex = 0;
        vertex <
          ringCount;
        vertex += 1
      ) {
        const angle =
          vertex *
          (Math.PI * 2) /
          ringCount

        stonePositions.push(
          Math.cos(
            angle,
          ) *
          lowerRadii[
            vertex
          ],
          0,
          Math.sin(
            angle,
          ) *
          lowerRadii[
            vertex
          ],
        )
      }

      for (
        let vertex = 0;
        vertex <
          ringCount;
        vertex += 1
      ) {
        const angle =
          vertex *
          (Math.PI * 2) /
          ringCount +
          0.08

        stonePositions.push(
          0.05 +
          Math.cos(
            angle,
          ) *
          upperRadii[
            vertex
          ],
          0.34 +
          (
            vertex %
            3
          ) *
          0.025,
          -0.035 +
          Math.sin(
            angle,
          ) *
          upperRadii[
            vertex
          ],
        )
      }

      const stoneTopIndex =
        stonePositions.length /
        3

      stonePositions.push(
        0.02,
        0.53,
        -0.015,
      )

      for (
        let side = 0;
        side <
          ringCount;
        side += 1
      ) {
        const next =
          (
            side +
            1
          ) %
          ringCount

        const lower =
          side

        const lowerNext =
          next

        const upper =
          side +
          ringCount

        const upperNext =
          next +
          ringCount

        stoneIndices.push(
          lower,
          lowerNext,
          upperNext,

          lower,
          upperNext,
          upper,

          upper,
          upperNext,
          stoneTopIndex,
        )
      }

      const stoneNormals =
        new Array<number>(
          stonePositions.length,
        ).fill(
          0,
        )

      VertexData.ComputeNormals(
        stonePositions,
        stoneIndices,
        stoneNormals,
      )

      const stoneVertexData =
        new VertexData()

      stoneVertexData.positions =
        stonePositions

      stoneVertexData.indices =
        stoneIndices

      stoneVertexData.normals =
        stoneNormals

      stoneVertexData.applyToMesh(
        playStoneScatterMesh,
      )

      const stoneMaterial =
        new StandardMaterial(
          'play-stone-scatter-material',
          scene,
        )

      stoneMaterial.diffuseColor =
        new Color3(
          0.47,
          0.43,
          0.35,
        )

      stoneMaterial.emissiveColor =
        new Color3(
          0.010,
          0.009,
          0.007,
        )

      stoneMaterial.specularColor =
        Color3.Black()

      playStoneScatterMesh.material =
        stoneMaterial

      playStoneScatterMesh.isPickable =
        false

      playStoneScatterMesh
        .alwaysSelectAsActiveMesh =
          true
    }
  }

  const rebuildPlaySurfaceScatter = (
    scatterAnchor:
      ManifestedEsterResponse,
  ) => {
    ensurePlaySurfaceScatterMeshes()

    if (
      playGrassScatterMesh === null ||
      playStoneScatterMesh === null
    ) {
      return
    }

    playSurfaceScatterAnchor = {
      ...scatterAnchor,
    }

    playSurfaceScatterAnchorElevationMeters =
      samplePlayTerrainElevationMeters(
        scatterAnchor.latitudeDegrees,
        scatterAnchor.longitudeDegrees,
      )

    if (
      playGrassMaterial !== null
    ) {
      setAnimatedGrassAnchor(
        playGrassMaterial,
        scatterAnchor.latitudeDegrees,
        scatterAnchor.longitudeDegrees,
        planet.meanRadiusMeters,
      )
    }

    const samples =
      createTerrainSurfaceScatter(
        scatterAnchor.latitudeDegrees,
        scatterAnchor.longitudeDegrees,
        planet.meanRadiusMeters,
        planet.planetId,
        playSurfaceScatterRadiusMeters,
      )

    const grassTransforms:
      PlaySurfaceScatterTransform[] =
        []

    const grassInstanceData:
      number[] =
        []

    const stoneTransforms:
      PlaySurfaceScatterTransform[] =
        []

    for (
      const sample of samples
    ) {
      let grassScaleMultiplier =
        1

      let grassVariation =
        0

      let grassDryness =
        0

      let grassStiffness =
        0.5

      let grassSeedHead =
        0

      if (
        sample.kind ===
        'grass'
      ) {
        const appearance =
          sampleTerrainSurfaceAppearance(
            sample.latitudeDegrees,
            sample.longitudeDegrees,
            planet.meanRadiusMeters,
            planet.planetId,
          )

        if (
          appearance.earthiness >
          0.78
        ) {
          continue
        }

        const suitability =
          Math.max(
            0,
            Math.min(
              1,
              1 -
              appearance.earthiness,
            ),
          )

        grassScaleMultiplier =
          0.80 +
          suitability *
          0.32

        grassVariation =
          Math.max(
            0,
            Math.min(
              1,
              sample.variation +
              appearance
                .albedoVariation *
                0.10,
            ),
          )

        grassDryness =
          Math.max(
            0,
            Math.min(
              1,
              sample.dryness +
              appearance.earthiness *
                0.22,
            ),
          )

        grassStiffness =
          sample.stiffness

        grassSeedHead =
          sample.seedHead
      }

      const local =
        geographicToLocalMeters(
          sample,
          scatterAnchor,
          planet.meanRadiusMeters,
        )

      const elevationMeters =
        samplePlayTerrainElevationMeters(
          sample.latitudeDegrees,
          sample.longitudeDegrees,
        ) -
        playSurfaceScatterAnchorElevationMeters

            const transform:

        PlaySurfaceScatterTransform = {
          eastMeters:
            local.eastMeters,
          elevationMeters:
            elevationMeters -
            (
              sample.kind ===
              'stone'
                ? 0.008
                : 0
            ),
          northMeters:
            local.northMeters,
          scaleX:
            sample.scaleX,
          scaleY:
            sample.scaleY *
            grassScaleMultiplier,
          scaleZ:
            sample.scaleZ,
          yawRadians:
            sample.yawRadians,
        }

      if (
        sample.kind ===
        'grass'
      ) {
        grassTransforms.push(
          transform,
        )

        grassInstanceData.push(
          grassVariation,
          grassDryness,
          grassStiffness,
          grassSeedHead,
        )
      } else {
        stoneTransforms.push(
          transform,
        )
      }
    }

    playGrassScatterMesh
      .thinInstanceSetBuffer(
        'matrix',
        createScatterMatrixBuffer(
          grassTransforms,
        ),
        16,
        true,
      )

    playGrassScatterMesh
      .thinInstanceSetBuffer(
        'grassData',
        new Float32Array(
          grassInstanceData,
        ),
        4,
        true,
      )

    playStoneScatterMesh
      .thinInstanceSetBuffer(
        'matrix',
        createScatterMatrixBuffer(
          stoneTransforms,
        ),
        16,
        true,
      )

    playGrassScatterMesh.position.set(
      0,
      0,
      0,
    )

    playStoneScatterMesh.position.set(
      0,
      0,
      0,
    )
  }

  const ensurePlaySurfaceScatter = () => {
    if (
      !playMode ||
      presentationEster === null
    ) {
      return
    }

    let shouldReanchor =
      playSurfaceScatterAnchor === null

    if (
      !shouldReanchor &&
      playSurfaceScatterAnchor !== null
    ) {
      const currentOffset =
        geographicToLocalMeters(
          presentationEster,
          playSurfaceScatterAnchor,
          planet.meanRadiusMeters,
        )

      shouldReanchor =
        Math.hypot(
          currentOffset.eastMeters,
          currentOffset.northMeters,
        ) >=
        playSurfaceScatterReanchorDistanceMeters
    }

    if (!shouldReanchor) {
      return
    }

    rebuildPlaySurfaceScatter(
      presentationEster,
    )
  }


  const updatePlaySurfaceScatterPositions = () => {
    ensurePlaySurfaceScatter()

    if (
      presentationEster === null ||
      playSurfaceScatterAnchor === null
    ) {
      return
    }

    const esterElevationMeters =
      samplePlayTerrainElevationMeters(
        presentationEster
          .latitudeDegrees,
        presentationEster
          .longitudeDegrees,
      )

    const anchorOffset =
      geographicToLocalMeters(
        playSurfaceScatterAnchor,
        presentationEster,
        planet.meanRadiusMeters,
      )

    const elevationOffsetMeters =
      playSurfaceScatterAnchorElevationMeters -
      esterElevationMeters

    if (
      playGrassScatterMesh !== null
    ) {
      playGrassScatterMesh.position.set(
        anchorOffset.eastMeters,
        elevationOffsetMeters,
        anchorOffset.northMeters,
      )
    }

    if (
      playStoneScatterMesh !== null
    ) {
      playStoneScatterMesh.position.set(
        anchorOffset.eastMeters,
        elevationOffsetMeters,
        anchorOffset.northMeters,
      )
    }
  }


  const ensurePlayTerrainTextures = () => {
    if (
      playGroundAlbedoTexture !== null &&
      playGroundNormalTexture !== null &&
      playGroundDetailTexture !== null
    ) {
      return
    }

    playGroundAlbedoTexture =
      new DynamicTexture(
        'play-ground-albedo',
        {
          width:
            playTerrainTextureSize,
          height:
            playTerrainTextureSize,
        },
        scene,
        true,
      )

    playGroundNormalTexture =
      new DynamicTexture(
        'play-ground-normal',
        {
          width:
            playTerrainTextureSize,
          height:
            playTerrainTextureSize,
        },
        scene,
        true,
      )

    playGroundDetailTexture =
      new DynamicTexture(
        'play-ground-detail',
        {
          width:
            playTerrainDetailTextureSize,
          height:
            playTerrainDetailTextureSize,
        },
        scene,
        true,
      )

    const detailContext =
      playGroundDetailTexture
        .getContext() as unknown as
        CanvasRenderingContext2D

    const detailImage =
      detailContext.createImageData(
        playTerrainDetailTextureSize,
        playTerrainDetailTextureSize,
      )

    detailImage.data.set(
      createTerrainDetailMapPixels(
        playTerrainDetailTextureSize,
        playTerrainDetailTileMeters,
        planet.planetId,
      ),
    )

    detailContext.putImageData(
      detailImage,
      0,
      0,
    )

    playGroundDetailTexture.update(
      false,
    )

    playGroundAlbedoTexture
      .anisotropicFilteringLevel =
        8

    playGroundNormalTexture
      .anisotropicFilteringLevel =
        8

    playGroundDetailTexture
      .anisotropicFilteringLevel =
        8

    playGroundDetailTexture.wrapU =
      Texture.WRAP_ADDRESSMODE

    playGroundDetailTexture.wrapV =
      Texture.WRAP_ADDRESSMODE

    playGroundDetailTexture.uScale =
      playTerrainSizeMeters /
      playTerrainDetailTileMeters

    playGroundDetailTexture.vScale =
      playTerrainSizeMeters /
      playTerrainDetailTileMeters

    playGroundMaterial.diffuseTexture =
      playGroundAlbedoTexture

    playGroundMaterial.bumpTexture =
      playGroundNormalTexture

    playGroundNormalTexture.level =
      1.35

    playGroundMaterial.detailMap.texture =
      playGroundDetailTexture

    playGroundMaterial.detailMap
      .diffuseBlendLevel =
        0.82

    playGroundMaterial.detailMap
      .bumpLevel =
        1.65

    playGroundMaterial.detailMap
      .isEnabled =
        true
  }

  const updatePlayTerrainTextures = (
    terrainAnchor:
      ManifestedEsterResponse,
  ) => {
    ensurePlayTerrainTextures()

    if (
      playGroundAlbedoTexture === null ||
      playGroundNormalTexture === null ||
      playGroundDetailTexture === null
    ) {
      return
    }

    const wrapTextureOffset = (
      value: number,
    ) =>
      (
        (
          value %
          1
        ) +
        1
      ) %
      1

    if (
      playGroundDetailAnchor === null
    ) {
      const latitudeRadians =
        terrainAnchor
          .latitudeDegrees *
        Math.PI /
        180

      const longitudeRadians =
        terrainAnchor
          .longitudeDegrees *
        Math.PI /
        180

      const absoluteEastMeters =
        planet.meanRadiusMeters *
        longitudeRadians *
        Math.cos(
          latitudeRadians,
        )

      const absoluteNorthMeters =
        planet.meanRadiusMeters *
        latitudeRadians

      playGroundDetailUOffset =
        wrapTextureOffset(
          absoluteEastMeters /
            playTerrainDetailTileMeters,
        )

      playGroundDetailVOffset =
        wrapTextureOffset(
          -absoluteNorthMeters /
            playTerrainDetailTileMeters,
        )
    } else {
      const detailAnchorDelta =
        geographicToLocalMeters(
          terrainAnchor,
          playGroundDetailAnchor,
          planet.meanRadiusMeters,
        )

      playGroundDetailUOffset =
        wrapTextureOffset(
          playGroundDetailUOffset +
          detailAnchorDelta
            .eastMeters /
            playTerrainDetailTileMeters,
        )

      playGroundDetailVOffset =
        wrapTextureOffset(
          playGroundDetailVOffset -
          detailAnchorDelta
            .northMeters /
            playTerrainDetailTileMeters,
        )
    }

    playGroundDetailAnchor = {
      ...terrainAnchor,
    }

    playGroundDetailTexture.uOffset =
      playGroundDetailUOffset

    playGroundDetailTexture.vOffset =
      playGroundDetailVOffset

    const textureSize =
      playTerrainTextureSize

    const pixelSpacingMeters =
      playTerrainSizeMeters /
      (
        textureSize -
        1
      )

    const albedoContext =
      playGroundAlbedoTexture
        .getContext() as unknown as
        CanvasRenderingContext2D

    const normalContext =
      playGroundNormalTexture
        .getContext() as unknown as
        CanvasRenderingContext2D

    const albedoImage =
      albedoContext.createImageData(
        textureSize,
        textureSize,
      )

    const normalImage =
      normalContext.createImageData(
        textureSize,
        textureSize,
      )

    const microHeights =
      new Float32Array(
        textureSize *
        textureSize,
      )

    const albedoVariations =
      new Float32Array(
        textureSize *
        textureSize,
      )

    const earthinessValues =
      new Float32Array(
        textureSize *
        textureSize,
      )

    for (
      let y = 0;
      y < textureSize;
      y += 1
    ) {
      const northMeters =
        playTerrainSizeMeters *
        (
          0.5 -
          y /
            (
              textureSize -
              1
            )
        )

      for (
        let x = 0;
        x < textureSize;
        x += 1
      ) {
        const eastMeters =
          playTerrainSizeMeters *
          (
            x /
              (
                textureSize -
                1
              ) -
            0.5
          )

        const coordinate =
          moveSurfaceCoordinate(
            terrainAnchor,
            northMeters,
            eastMeters,
            planet.meanRadiusMeters,
          )

        const appearance =
          sampleTerrainSurfaceAppearance(
            coordinate.latitudeDegrees,
            coordinate.longitudeDegrees,
            planet.meanRadiusMeters,
            planet.planetId,
          )

        const index =
          y *
            textureSize +
          x

        microHeights[index] =
          appearance
            .microHeightMeters

        albedoVariations[index] =
          appearance
            .albedoVariation

        earthinessValues[index] =
          appearance
            .earthiness
      }
    }

    for (
      let y = 0;
      y < textureSize;
      y += 1
    ) {
      for (
        let x = 0;
        x < textureSize;
        x += 1
      ) {
        const index =
          y *
            textureSize +
          x

        const pixelIndex =
          index *
          4

        const variation =
          (
            albedoVariations[index] +
            1
          ) /
          2

        const earthBlend =
          Math.min(
            0.72,
            Math.max(
              0,
              (
                earthinessValues[
                  index
                ] -
                0.54
              ) /
                0.46 *
                0.72,
            ),
          )

        let red =
          0.25 +
          variation *
            0.18

        let green =
          0.36 +
          variation *
            0.17

        let blue =
          0.12 +
          variation *
            0.10

        const earthRed =
          0.46

        const earthGreen =
          0.36

        const earthBlue =
          0.21

        red +=
          (
            earthRed -
            red
          ) *
          earthBlend

        green +=
          (
            earthGreen -
            green
          ) *
          earthBlend

        blue +=
          (
            earthBlue -
            blue
          ) *
          earthBlend

        albedoImage.data[
          pixelIndex
        ] =
          Math.round(
            red *
            255,
          )

        albedoImage.data[
          pixelIndex + 1
        ] =
          Math.round(
            green *
            255,
          )

        albedoImage.data[
          pixelIndex + 2
        ] =
          Math.round(
            blue *
            255,
          )

        albedoImage.data[
          pixelIndex + 3
        ] =
          255

        const leftIndex =
          y *
            textureSize +
          Math.max(
            0,
            x - 1,
          )

        const rightIndex =
          y *
            textureSize +
          Math.min(
            textureSize - 1,
            x + 1,
          )

        const upIndex =
          Math.max(
            0,
            y - 1,
          ) *
            textureSize +
          x

        const downIndex =
          Math.min(
            textureSize - 1,
            y + 1,
          ) *
            textureSize +
          x

        const eastSlope =
          (
            microHeights[
              rightIndex
            ] -
            microHeights[
              leftIndex
            ]
          ) /
          (
            (
              rightIndex ===
              leftIndex
                ? 1
                : rightIndex -
                  leftIndex
            ) *
            pixelSpacingMeters
          )

        const northSlope =
          (
            microHeights[
              upIndex
            ] -
            microHeights[
              downIndex
            ]
          ) /
          (
            (
              upIndex ===
              downIndex
                ? 1
                : Math.abs(
                    upIndex -
                    downIndex,
                  ) /
                  textureSize
            ) *
            pixelSpacingMeters
          )

        let normalX =
          -eastSlope

        let normalY =
          northSlope

        let normalZ =
          1

        const normalLength =
          Math.hypot(
            normalX,
            normalY,
            normalZ,
          )

        normalX /=
          normalLength

        normalY /=
          normalLength

        normalZ /=
          normalLength

        normalImage.data[
          pixelIndex
        ] =
          Math.round(
            (
              normalX *
              0.5 +
              0.5
            ) *
            255,
          )

        normalImage.data[
          pixelIndex + 1
        ] =
          Math.round(
            (
              normalY *
              0.5 +
              0.5
            ) *
            255,
          )

        normalImage.data[
          pixelIndex + 2
        ] =
          Math.round(
            (
              normalZ *
              0.5 +
              0.5
            ) *
            255,
          )

        normalImage.data[
          pixelIndex + 3
        ] =
          255
      }
    }

    albedoContext.putImageData(
      albedoImage,
      0,
      0,
    )

    normalContext.putImageData(
      normalImage,
      0,
      0,
    )

    playGroundAlbedoTexture.update(
      false,
    )

    playGroundNormalTexture.update(
      false,
    )
  }

  const ensurePlayTerrain = () => {
    if (
      !playMode ||
      presentationEster === null
    ) {
      return
    }

    if (playGroundMesh === null) {
      playGroundMesh =
        CreateGround(
          'play-ground',
          {
            width:
              playTerrainSizeMeters,
            height:
              playTerrainSizeMeters,
            subdivisions:
              playTerrainSubdivisions,
            updatable: true,
          },
          scene,
        )

      playGroundMesh.material =
        playGroundMaterial

      playGroundMesh.useVertexColors =
        true

      const groundPositions =
        playGroundMesh.getVerticesData(
          VertexBuffer.PositionKind,
        )

      if (
        groundPositions === null
      ) {
        throw new Error(
          'Local play terrain requires position vertex data.',
        )
      }

      const groundUvs =
        new Float32Array(
          (
            groundPositions.length /
            3
          ) *
          2,
        )

      for (
        let positionIndex = 0,
          uvIndex = 0;
        positionIndex <
          groundPositions.length;
        positionIndex += 3,
          uvIndex += 2
      ) {
        groundUvs[
          uvIndex
        ] =
          groundPositions[
            positionIndex
          ] /
            playTerrainSizeMeters +
          0.5

        groundUvs[
          uvIndex + 1
        ] =
          0.5 -
          groundPositions[
            positionIndex + 2
          ] /
            playTerrainSizeMeters
      }

      playGroundMesh.setVerticesData(
        VertexBuffer.UVKind,
        groundUvs,
        true,
        2,
      )

      ensurePlayTerrainTextures()

      playGroundMesh.isPickable =
        false
    }

    let shouldReanchor =
      playTerrainAnchor === null

    if (
      playTerrainAnchor !== null
    ) {
      const anchorOffset =
        geographicToLocalMeters(
          presentationEster,
          playTerrainAnchor,
          planet.meanRadiusMeters,
        )

      shouldReanchor =
        Math.hypot(
          anchorOffset.eastMeters,
          anchorOffset.northMeters,
        ) >=
        playTerrainReanchorDistanceMeters
    }

    if (!shouldReanchor) {
      return
    }

    playTerrainAnchor = {
      ...presentationEster,
    }

    playTerrainAnchorElevationMeters =
      samplePlayTerrainElevationMeters(
        playTerrainAnchor.latitudeDegrees,
        playTerrainAnchor.longitudeDegrees,
      )

    const terrainAnchor =
      playTerrainAnchor

    playGroundMesh.updateMeshPositions(
      positions => {
        for (
          let index = 0;
          index < positions.length;
          index += 3
        ) {
          const eastMeters =
            positions[index]

          const northMeters =
            positions[index + 2]

          const coordinate =
            moveSurfaceCoordinate(
              terrainAnchor,
              northMeters,
              eastMeters,
              planet.meanRadiusMeters,
            )

          positions[index + 1] =
            samplePlayTerrainElevationMeters(
              coordinate.latitudeDegrees,
              coordinate.longitudeDegrees,
            ) -
            playTerrainAnchorElevationMeters
        }
      },
      true,
    )

    const terrainPositions =
      playGroundMesh.getVerticesData(
        VertexBuffer.PositionKind,
      )

    const terrainNormals =
      playGroundMesh.getVerticesData(
        VertexBuffer.NormalKind,
      )

    if (
      terrainPositions === null ||
      terrainNormals === null
    ) {
      throw new Error(
        'Local play terrain requires position and normal vertex data.',
      )
    }

    const terrainColors =
      new Float32Array(
        (
          terrainPositions.length /
          3
        ) *
        4,
      )

    for (
      let positionIndex = 0,
        colorIndex = 0;
      positionIndex <
        terrainPositions.length;
      positionIndex += 3,
        colorIndex += 4
    ) {
      const eastMeters =
        terrainPositions[
          positionIndex
        ]

      const northMeters =
        terrainPositions[
          positionIndex + 2
        ]

      const coordinate =
        moveSurfaceCoordinate(
          terrainAnchor,
          northMeters,
          eastMeters,
          planet.meanRadiusMeters,
        )

      const appearanceVariation =
        Math.max(
          -1,
          Math.min(
            1,
            sampleTerrainPresentationDetailMeters(
              coordinate.latitudeDegrees,
              coordinate.longitudeDegrees,
              planet.meanRadiusMeters,
              playTerrainAppearanceIdentity,
            ) /
              playTerrainPresentationDetailAmplitudeMeters,
          ),
        )

      const variationAmount =
        (
          appearanceVariation +
          1
        ) /
        2

      // Two restrained natural ground tones provide geographic,
      // deterministic variation without claiming a new simulation
      // vegetation or soil state.
      const darkGround = {
        red: 0.78,
        green: 0.84,
        blue: 0.72,
      }

      const lightGround = {
        red: 1.00,
        green: 1.00,
        blue: 0.92,
      }

      let red =
        darkGround.red +
        (
          lightGround.red -
          darkGround.red
        ) *
          variationAmount

      let green =
        darkGround.green +
        (
          lightGround.green -
          darkGround.green
        ) *
          variationAmount

      let blue =
        darkGround.blue +
        (
          lightGround.blue -
          darkGround.blue
        ) *
          variationAmount

      const normalY =
        Math.max(
          0,
          Math.min(
            1,
            terrainNormals[
              positionIndex + 1
            ],
          ),
        )

      // Steeper faces reveal a restrained earth/rock tone. This is a
      // presentation cue derived from the actual rendered geometry,
      // not authoritative geology or soil state.
      const exposedSurfaceAmount =
        Math.min(
          0.6,
          Math.max(
            0,
            (
              1 -
              normalY
            ) /
              0.03 *
              0.6,
          ),
        )

      const exposedRed =
        1.00

      const exposedGreen =
        0.88

      const exposedBlue =
        0.74

      red +=
        (
          exposedRed -
          red
        ) *
        exposedSurfaceAmount

      green +=
        (
          exposedGreen -
          green
        ) *
        exposedSurfaceAmount

      blue +=
        (
          exposedBlue -
          blue
        ) *
        exposedSurfaceAmount

      terrainColors[
        colorIndex
      ] =
        red

      terrainColors[
        colorIndex + 1
      ] =
        green

      terrainColors[
        colorIndex + 2
      ] =
        blue

      terrainColors[
        colorIndex + 3
      ] =
        1
    }

    playGroundMesh.setVerticesData(
      VertexBuffer.ColorKind,
      terrainColors,
      true,
      4,
    )

    updatePlayTerrainTextures(
      terrainAnchor,
    )

  }

  const updatePlaySpacePositions = () => {
    if (
      !playMode ||
      presentationEster === null
    ) {
      return
    }

    const latitudeRadians =
      presentationEster.latitudeDegrees *
      Math.PI /
      180

    const longitudeRadians =
      presentationEster.longitudeDegrees *
      Math.PI /
      180

    const cosLatitude =
      Math.cos(
        latitudeRadians,
      )

    const sinLatitude =
      Math.sin(
        latitudeRadians,
      )

    const cosLongitude =
      Math.cos(
        longitudeRadians,
      )

    const sinLongitude =
      Math.sin(
        longitudeRadians,
      )

    const east = {
      x: -sinLongitude,
      y: cosLongitude,
      z: 0,
    }

    const up = {
      x:
        cosLatitude *
        cosLongitude,
      y:
        cosLatitude *
        sinLongitude,
      z:
        sinLatitude,
    }

    const north = {
      x:
        -sinLatitude *
        cosLongitude,
      y:
        -sinLatitude *
        sinLongitude,
      z:
        cosLatitude,
    }

    const localSunlightDirection =
      new Vector3(
        sunlightRayDirection.x *
          east.x +
          sunlightRayDirection.y *
          east.y +
          sunlightRayDirection.z *
          east.z,
        sunlightRayDirection.x *
          up.x +
          sunlightRayDirection.y *
          up.y +
          sunlightRayDirection.z *
          up.z,
        sunlightRayDirection.x *
          north.x +
          sunlightRayDirection.y *
          north.y +
          sunlightRayDirection.z *
          north.z,
      )

    if (
      localSunlightDirection
        .lengthSquared() >
      1e-12
    ) {
      localSunlightDirection
        .normalize()

      sunlight.direction =
        localSunlightDirection

      if (
        playGrassMaterial !== null
      ) {
        playGrassMaterial.setVector3(
          'uSurfaceToLightDirection',
          localSunlightDirection
            .scale(
              -1,
            ),
        )
      }
    }

    // Keep the local world readable even when the current simulated
    // sun angle is shallow. Day/night presentation can later vary the
    // sky contribution explicitly rather than allowing terrain to
    // disappear into an unlit black field.
    sunlight.intensity =
      1.15

    ensurePlayTerrain()

    const esterElevationMeters =
      samplePlayTerrainElevationMeters(
        presentationEster.latitudeDegrees,
        presentationEster.longitudeDegrees,
      )

    if (
      playGroundMesh !== null &&
      playTerrainAnchor !== null
    ) {
      const terrainAnchorOffset =
        geographicToLocalMeters(
          playTerrainAnchor,
          presentationEster,
          planet.meanRadiusMeters,
        )

      playGroundMesh.position.set(
        terrainAnchorOffset.eastMeters,
        playTerrainAnchorElevationMeters -
          esterElevationMeters,
        terrainAnchorOffset.northMeters,
      )
    }

    if (playEsterMesh !== null) {
      playEsterMesh.position.set(
        0,
        0.9,
        0,
      )
    }

    const visibleRadiusMeters =
      120

    const preloadRadiusMeters =
      150

    const localIndividuals =
      projectLocalIndividualActors({
        planetId:
          planet.planetId,
        origin:
          presentationEster,
        planetRadiusMeters:
          planet.meanRadiusMeters,
        maximumDistanceMeters:
          preloadRadiusMeters,
        population:
          world.population,
        animals:
          world.animals,
      })

    for (
      const human of
      playHumanPresentations.values()
    ) {
      human.setEnabled(
        false,
      )
    }

    for (
      const actor of
      localIndividuals.people
    ) {
      const person =
        actor.source

      const human =
        playHumanPresentations.get(
          person.personId,
        )

      if (!human) {
        ensurePlayHumanPresentation(
          person.personId,
          person.sex,
        )

        continue
      }

      const visible =
        actor.distanceMeters <=
        visibleRadiusMeters

      human.setEnabled(
        visible,
      )

      if (!visible) {
        continue
      }

      const personElevationMeters =
        samplePlayTerrainElevationMeters(
          person.latitudeDegrees,
          person.longitudeDegrees,
        )

      human.root.position.set(
        actor.local.eastMeters,
        personElevationMeters -
          esterElevationMeters,
        actor.local.northMeters,
      )
    }

    const projectedWolfKeys =
      new Set<string>()

    for (
      const actor of
      localIndividuals.animals
    ) {
      if (
        actor.source.species !==
        'Wolf'
      ) {
        continue
      }

      projectedWolfKeys.add(
        actor.actorKey,
      )

      const wolf =
        playWolfPresentations.get(
          actor.actorKey,
        )

      if (!wolf) {
        ensurePlayWolfPresentation(
          actor.actorKey,
        )

        continue
      }

      const motion =
        playAnimalMotionTracker.observe({
          actorKey:
            actor.actorKey,
          coordinate:
            actor.source,
          planetRadiusMeters:
            planet.meanRadiusMeters,
          snapshotTimeSeconds:
            world.currentTimeSeconds,
        })

      wolf.setSimulationTimeSeconds(
        world.currentTimeSeconds,
      )

      if (
        motion.headingRadians !==
        null
      ) {
        wolf.setHeadingRadians(
          motion.headingRadians,
        )
      }

      const wolfState =
        resolveWolfPresentationState(
          actor.source.activity,
          motion.isMoving,
        )

      const visible =
        actor.distanceMeters <=
        visibleRadiusMeters

      wolf.setEnabled(
        visible,
      )

      if (!visible) {
        continue
      }

      const wolfElevationMeters =
        samplePlayTerrainElevationMeters(
          actor.source.latitudeDegrees,
          actor.source.longitudeDegrees,
        )

      const targetPosition:
        LocalActorPresentationPosition = {
          eastMeters:
            actor.local.eastMeters,
          verticalMeters:
            wolfElevationMeters -
            esterElevationMeters,
          northMeters:
            actor.local.northMeters,
        }

      const previousTransition =
        playWolfPresentationTransitions.get(
          actor.actorKey,
        )

      if (
        previousTransition?.snapshotTimeSeconds ===
        world.currentTimeSeconds
      ) {
        const targetDelta = {
          eastMeters:
            targetPosition.eastMeters -
            previousTransition
              .interpolation
              .target
              .eastMeters,
          verticalMeters:
            targetPosition.verticalMeters -
            previousTransition
              .interpolation
              .target
              .verticalMeters,
          northMeters:
            targetPosition.northMeters -
            previousTransition
              .interpolation
              .target
              .northMeters,
        }

        previousTransition.interpolation = {
          ...previousTransition.interpolation,
          start: {
            eastMeters:
              previousTransition
                .interpolation
                .start
                .eastMeters +
              targetDelta.eastMeters,
            verticalMeters:
              previousTransition
                .interpolation
                .start
                .verticalMeters +
              targetDelta.verticalMeters,
            northMeters:
              previousTransition
                .interpolation
                .start
                .northMeters +
              targetDelta.northMeters,
          },
          target: {
            ...targetPosition,
          },
        }

        if (
          previousTransition.isComplete
        ) {
          wolf.root.position.set(
            targetPosition.eastMeters,
            targetPosition.verticalMeters,
            targetPosition.northMeters,
          )
        } else {
          wolf.root.position.addInPlaceFromFloats(
            targetDelta.eastMeters,
            targetDelta.verticalMeters,
            targetDelta.northMeters,
          )
        }
      } else if (
        motion.isMoving &&
        previousTransition !== undefined
      ) {
        wolf.setState(
          wolfState,
        )

        wolf.setGaitPhase(
          0,
        )

        playWolfPresentationTransitions.set(
          actor.actorKey,
          {
            snapshotTimeSeconds:
              world.currentTimeSeconds,
            interpolation:
              createLocalActorPresentationTransition(
                {
                  eastMeters:
                    wolf.root.position.x,
                  verticalMeters:
                    wolf.root.position.y,
                  northMeters:
                    wolf.root.position.z,
                },
                targetPosition,
                performance.now(),
                wolfPresentationTransitionMilliseconds,
              ),
            state:
              wolfState,
            isComplete:
              false,
          },
        )
      } else {
        wolf.root.position.set(
          targetPosition.eastMeters,
          targetPosition.verticalMeters,
          targetPosition.northMeters,
        )

        const stationaryState:
          WolfPresentationState = {
            ...wolfState,
            locomotion:
              'stationary',
          }

        wolf.setState(
          stationaryState,
        )

        wolf.setGaitPhase(
          0,
        )

        playWolfPresentationTransitions.set(
          actor.actorKey,
          {
            snapshotTimeSeconds:
              world.currentTimeSeconds,
            interpolation:
              createLocalActorPresentationTransition(
                targetPosition,
                targetPosition,
                performance.now(),
                wolfPresentationTransitionMilliseconds,
              ),
            state:
              stationaryState,
            isComplete:
              true,
          },
        )
      }
    }

    for (
      const [
        actorKey,
        wolf,
      ] of
      playWolfPresentations
    ) {
      if (
        projectedWolfKeys.has(
          actorKey,
        )
      ) {
        continue
      }

      wolf.dispose()

      playWolfPresentations.delete(
        actorKey,
      )

      playAnimalMotionTracker.forget(
        actorKey,
      )

      playWolfPresentationTransitions.delete(
        actorKey,
      )
    }

    const localGrazers =
      projectLocalGrazers({
        planetId:
          planet.planetId,
        origin:
          presentationEster,
        planetRadiusMeters:
          planet.meanRadiusMeters,
        maximumDistanceMeters:
          preloadRadiusMeters,
        presentationSpreadRadiusMeters:
          40,
        maximumRepresentativesPerCohort:
          8,
        grazerCohorts,
        vegetation,
      })

    const projectedGrazerKeys =
      new Set<string>()

    for (
      const actor of
      localGrazers
    ) {
      projectedGrazerKeys.add(
        actor.actorKey,
      )

      const grazer =
        playGrazerPresentations.get(
          actor.actorKey,
        )

      if (!grazer) {
        ensurePlayGrazerPresentation(
          actor.actorKey,
        )

        continue
      }

      const motion =
        playGrazerMotionTracker.observe({
          actorKey:
            actor.actorKey,
          coordinate:
            actor.coordinate,
          planetRadiusMeters:
            planet.meanRadiusMeters,
          snapshotTimeSeconds:
            world.currentTimeSeconds,
        })

      grazer.setSimulationTimeSeconds(
        world.currentTimeSeconds,
      )

      if (
        motion.headingRadians !==
        null
      ) {
        grazer.setHeadingRadians(
          motion.headingRadians,
        )
      }

      const grazerState =
        resolveGrazerPresentationState(
          motion.isMoving,
        )

      const visible =
        actor.distanceMeters <=
        visibleRadiusMeters

      grazer.setEnabled(
        visible,
      )

      if (!visible) {
        continue
      }

      const grazerElevationMeters =
        samplePlayTerrainElevationMeters(
          actor.coordinate.latitudeDegrees,
          actor.coordinate.longitudeDegrees,
        )

      const targetPosition:
        LocalActorPresentationPosition = {
          eastMeters:
            actor.local.eastMeters,
          verticalMeters:
            grazerElevationMeters -
            esterElevationMeters,
          northMeters:
            actor.local.northMeters,
        }

      const previousTransition =
        playGrazerPresentationTransitions.get(
          actor.actorKey,
        )

      if (
        previousTransition?.snapshotTimeSeconds ===
        world.currentTimeSeconds
      ) {
        const targetDelta = {
          eastMeters:
            targetPosition.eastMeters -
            previousTransition
              .interpolation
              .target
              .eastMeters,
          verticalMeters:
            targetPosition.verticalMeters -
            previousTransition
              .interpolation
              .target
              .verticalMeters,
          northMeters:
            targetPosition.northMeters -
            previousTransition
              .interpolation
              .target
              .northMeters,
        }

        previousTransition.interpolation = {
          ...previousTransition.interpolation,
          start: {
            eastMeters:
              previousTransition
                .interpolation
                .start
                .eastMeters +
              targetDelta.eastMeters,
            verticalMeters:
              previousTransition
                .interpolation
                .start
                .verticalMeters +
              targetDelta.verticalMeters,
            northMeters:
              previousTransition
                .interpolation
                .start
                .northMeters +
              targetDelta.northMeters,
          },
          target: {
            ...targetPosition,
          },
        }

        if (
          previousTransition.isComplete
        ) {
          grazer.root.position.set(
            targetPosition.eastMeters,
            targetPosition.verticalMeters,
            targetPosition.northMeters,
          )
        } else {
          grazer.root.position.addInPlaceFromFloats(
            targetDelta.eastMeters,
            targetDelta.verticalMeters,
            targetDelta.northMeters,
          )
        }
      } else if (
        motion.isMoving &&
        previousTransition !== undefined
      ) {
        grazer.setState(
          grazerState,
        )

        grazer.setGaitPhase(
          0,
        )

        playGrazerPresentationTransitions.set(
          actor.actorKey,
          {
            snapshotTimeSeconds:
              world.currentTimeSeconds,
            interpolation:
              createLocalActorPresentationTransition(
                {
                  eastMeters:
                    grazer.root.position.x,
                  verticalMeters:
                    grazer.root.position.y,
                  northMeters:
                    grazer.root.position.z,
                },
                targetPosition,
                performance.now(),
                grazerPresentationTransitionMilliseconds,
              ),
            state:
              grazerState,
            isComplete:
              false,
          },
        )
      } else {
        grazer.root.position.set(
          targetPosition.eastMeters,
          targetPosition.verticalMeters,
          targetPosition.northMeters,
        )

        const stationaryState:
          GrazerPresentationState = {
            locomotion:
              'stationary',
          }

        grazer.setState(
          stationaryState,
        )

        grazer.setGaitPhase(
          0,
        )

        playGrazerPresentationTransitions.set(
          actor.actorKey,
          {
            snapshotTimeSeconds:
              world.currentTimeSeconds,
            interpolation:
              createLocalActorPresentationTransition(
                targetPosition,
                targetPosition,
                performance.now(),
                grazerPresentationTransitionMilliseconds,
              ),
            state:
              stationaryState,
            isComplete:
              true,
          },
        )
      }
    }

    for (
      const [
        actorKey,
        grazer,
      ] of
      playGrazerPresentations
    ) {
      if (
        projectedGrazerKeys.has(
          actorKey,
        )
      ) {
        continue
      }

      grazer.dispose()

      playGrazerPresentations.delete(
        actorKey,
      )

      playGrazerMotionTracker.forget(
        actorKey,
      )

      playGrazerPresentationTransitions.delete(
        actorKey,
      )
    }

    updatePlaySurfaceScatterPositions()
  }

  const ensurePlayWolfPresentation = (
    actorKey: string,
  ) => {
    if (
      playWolfPresentations.has(
        actorKey,
      ) ||
      playWolfLoadRequests.has(
        actorKey,
      ) ||
      playWolfLoadFailures.has(
        actorKey,
      )
    ) {
      return
    }

    const request =
      createAnimatedWolfPresentation(
        scene,
        actorKey,
      )
        .then(
          wolf => {
            playWolfPresentations.set(
              actorKey,
              wolf,
            )

            updatePlaySpacePositions()
          },
        )
        .catch(
          error => {
            playWolfLoadFailures.add(
              actorKey,
            )

            console.error(
              '[Est Babylon] Wolf presentation failed',
              {
                actorKey,
                error,
              },
            )
          },
        )
        .finally(
          () => {
            playWolfLoadRequests.delete(
              actorKey,
            )
          },
        )

    playWolfLoadRequests.set(
      actorKey,
      request,
    )
  }

  const ensurePlayGrazerPresentation = (
    actorKey: string,
  ) => {
    if (
      playGrazerPresentations.has(
        actorKey,
      ) ||
      playGrazerLoadRequests.has(
        actorKey,
      ) ||
      playGrazerLoadFailures.has(
        actorKey,
      )
    ) {
      return
    }

    const request =
      createAnimatedGrazerPresentation(
        scene,
        actorKey,
      )
        .then(
          grazer => {
            playGrazerPresentations.set(
              actorKey,
              grazer,
            )

            updatePlaySpacePositions()
          },
        )
        .catch(
          error => {
            playGrazerLoadFailures.add(
              actorKey,
            )

            console.error(
              '[Est Babylon] Grazer presentation failed',
              {
                actorKey,
                error,
              },
            )
          },
        )
        .finally(
          () => {
            playGrazerLoadRequests.delete(
              actorKey,
            )
          },
        )

    playGrazerLoadRequests.set(
      actorKey,
      request,
    )
  }

  const ensurePlayHumanPresentation = (
    personId: string,
    sex: string,
  ) => {
    if (
      playHumanPresentations.has(
        personId,
      ) ||
      playHumanLoadRequests.has(
        personId,
      ) ||
      playHumanLoadFailures.has(
        personId,
      )
    ) {
      return
    }

    const request =
      createHumanPresentation(
        scene,
        personId,
        sex,
      )
        .then(
          human => {
            playHumanPresentations.set(
              personId,
              human,
            )

            updatePlaySpacePositions()
          },
        )
        .catch(
          error => {
            playHumanLoadFailures.add(
              personId,
            )

            console.error(
              '[Est Babylon] Human presentation failed',
              {
                personId,
                sex,
                error,
              },
            )
          },
        )
        .finally(
          () => {
            playHumanLoadRequests.delete(
              personId,
            )
          },
        )

    playHumanLoadRequests.set(
      personId,
      request,
    )
  }

  const renderPlaySpace = () => {
    if (
      !playMode ||
      presentationEster === null
    ) {
      return
    }

    ensurePlayTerrain()

    if (playEsterMesh === null) {
      playEsterMesh =
        CreateCapsule(
          'play-ester',
          {
            height: 1.8,
            radius: 0.34,
            subdivisions: 4,
            tessellation: 32,
            capSubdivisions: 12,
          },
          scene,
        )

      playEsterMesh.material =
        createEsterCapsuleMaterial(
          scene,
          esterCapsuleSkin,
        )

      playEsterMesh.isPickable =
        false

      if (
        esterCapsuleSkin ===
        'tylenol'
      ) {
        // Temporary presentation-only winter hat for the Tylenol
        // capsule skin. The whole assembly is parented to Ester so it
        // follows the player without participating in simulation state.
        const hatRoot =
          new Mesh(
            'play-ester-snow-hat-root',
            scene,
          )

        hatRoot.parent =
          playEsterMesh

        hatRoot.position.set(
          0.02,
          0.91,
          0,
        )

        hatRoot.rotation.z =
          -0.22

        hatRoot.isPickable =
          false

        const hatRedMaterial =
          new StandardMaterial(
            'play-ester-snow-hat-red',
            scene,
          )

        hatRedMaterial.diffuseColor =
          Color3.FromHexString(
            '#c51622',
          )

        hatRedMaterial.specularColor =
          new Color3(
            0.18,
            0.18,
            0.18,
          )

        hatRedMaterial.specularPower =
          24

        const hatWhiteMaterial =
          new StandardMaterial(
            'play-ester-snow-hat-white',
            scene,
          )

        hatWhiteMaterial.diffuseColor =
          Color3.FromHexString(
            '#f7f7f2',
          )

        hatWhiteMaterial.specularColor =
          new Color3(
            0.10,
            0.10,
            0.10,
          )

        hatWhiteMaterial.specularPower =
          16

        const hatTrim =
          CreateCylinder(
            'play-ester-snow-hat-trim',
            {
              height: 0.075,
              diameter: 0.47,
              tessellation: 24,
            },
            scene,
          )

        hatTrim.parent =
          hatRoot

        hatTrim.position.set(
          0,
          0.025,
          0,
        )

        hatTrim.material =
          hatWhiteMaterial

        hatTrim.isPickable =
          false

        const hatBody =
          CreateCylinder(
            'play-ester-snow-hat-body',
            {
              height: 0.34,
              diameterBottom: 0.36,
              diameterTop: 0.08,
              tessellation: 24,
            },
            scene,
          )

        hatBody.parent =
          hatRoot

        hatBody.position.set(
          0.035,
          0.225,
          0,
        )

        hatBody.rotation.z =
          -0.10

        hatBody.material =
          hatRedMaterial

        hatBody.isPickable =
          false

        const hatPompom =
          CreateSphere(
            'play-ester-snow-hat-pompom',
            {
              diameter: 0.135,
              segments: 16,
            },
            scene,
          )

        hatPompom.parent =
          hatRoot

        hatPompom.position.set(
          0.095,
          0.405,
          0,
        )

        hatPompom.material =
          hatWhiteMaterial

        hatPompom.isPickable =
          false
      }
    }

    updatePlaySpacePositions()
  }


  if (playMode) {
    renderPlaySpace()
  } else {
    renderLivingWorld()
  }

  if (
    playMode &&
    manifestedEster !== null
  ) {
    camera.detachControl()

    camera.attachControl(
      canvas,
      true,
    )

    focusManifestedEster()

    const movementKeys =
      new Set<string>()

    let movementRequestInProgress =
      false

    let movementDirty =
      false

    let lastMovementFrameMilliseconds =
      performance.now()

    let lastMovementSyncMilliseconds =
      0

    const walkingMetersPerSecond =
      1.5

    const movementSyncMilliseconds =
      100

    const isMovementKey = (
      key: string,
    ) =>
      key === 'w' ||
      key === 'a' ||
      key === 's' ||
      key === 'd'

    window.addEventListener(
      'keydown',
      event => {
        const key =
          event.key.toLowerCase()

        if (!isMovementKey(key)) {
          return
        }

        if (
          event.target instanceof
            HTMLInputElement ||
          event.target instanceof
            HTMLTextAreaElement ||
          (
            event.target instanceof
              HTMLElement &&
            event.target.isContentEditable
          )
        ) {
          return
        }

        event.preventDefault()

        movementKeys.add(
          key,
        )
      },
    )

    window.addEventListener(
      'keyup',
      event => {
        const key =
          event.key.toLowerCase()

        if (!isMovementKey(key)) {
          return
        }

        event.preventDefault()

        movementKeys.delete(
          key,
        )
      },
    )

    window.addEventListener(
      'blur',
      () => {
        movementKeys.clear()
      },
    )

    const resolveMovementDirection =
      () => {
        const forwardInput =
          (
            movementKeys.has('w')
              ? 1
              : 0
          ) -
          (
            movementKeys.has('s')
              ? 1
              : 0
          )

        const rightInput =
          (
            movementKeys.has('d')
              ? 1
              : 0
          ) -
          (
            movementKeys.has('a')
              ? 1
              : 0
          )

        if (
          forwardInput === 0 &&
          rightInput === 0
        ) {
          return null
        }

        const cameraTarget =
          camera.getTarget()

        let forwardEast =
          cameraTarget.x -
          camera.position.x

        let forwardNorth =
          cameraTarget.z -
          camera.position.z

        const cameraForwardMagnitude =
          Math.hypot(
            forwardEast,
            forwardNorth,
          )

        if (
          cameraForwardMagnitude <
          0.000001
        ) {
          return null
        }

        forwardEast /=
          cameraForwardMagnitude

        forwardNorth /=
          cameraForwardMagnitude

        const rightEast =
          forwardNorth

        const rightNorth =
          -forwardEast

        let east =
          forwardEast *
            forwardInput +
          rightEast *
            rightInput

        let north =
          forwardNorth *
            forwardInput +
          rightNorth *
            rightInput

        const magnitude =
          Math.hypot(
            north,
            east,
          )

        if (
          magnitude <
          0.000001
        ) {
          return null
        }

        north /=
          magnitude

        east /=
          magnitude

        return {
          north,
          east,
        }
      }

    const synchronizeMovement = (
      nowMilliseconds: number,
    ) => {
      if (
        movementRequestInProgress ||
        !movementDirty ||
        presentationEster === null ||
        manifestedEster === null
      ) {
        return
      }

      const target = {
        ...presentationEster,
      }

      movementRequestInProgress =
        true

      movementDirty =
        false

      lastMovementSyncMilliseconds =
        nowMilliseconds

      void api
        .moveManifestedEster(
          sessionId,
          manifestedEster.esterId,
          target.latitudeDegrees,
          target.longitudeDegrees,
        )
        .then(
          moved => {
            manifestedEster =
              moved

            if (
              moved.encounters.length >
              0
            ) {
              const encounterMessages =
                moved.encounters.map(
                  formatPersonEncounterStatus,
                )

              const encounterStatus =
                encounterMessages.join(
                  ' · ',
                )

              showPanelVisibilityTip(
                encounterStatus,
              )

              console.log(
                `[Est Babylon] ${encounterStatus}`,
              )
            }

            if (
              !movementDirty &&
              movementKeys.size === 0
            ) {
              presentationEster = {
                ...moved,
              }

              updatePlaySpacePositions()
            }
          },
        )
        .catch(
          error => {
            console.error(
              '[Est Babylon] Ester movement failed',
              error,
            )

            if (
              manifestedEster !== null
            ) {
              presentationEster = {
                ...manifestedEster,
              }

              movementDirty =
                false

              updatePlaySpacePositions()
            }
          },
        )
        .finally(
          () => {
            movementRequestInProgress =
              false
          },
        )
    }

    const advanceWolfPresentationTransitions = (
      nowMilliseconds: number,
    ) => {
      for (
        const [
          actorKey,
          transition,
        ] of
        playWolfPresentationTransitions
      ) {
        if (transition.isComplete) {
          continue
        }

        const wolf =
          playWolfPresentations.get(
            actorKey,
          )

        if (!wolf) {
          playWolfPresentationTransitions.delete(
            actorKey,
          )

          continue
        }

        const sample =
          sampleLocalActorPresentationTransition(
            transition.interpolation,
            nowMilliseconds,
          )

        wolf.root.position.set(
          sample.position.eastMeters,
          sample.position.verticalMeters,
          sample.position.northMeters,
        )

        wolf.setGaitPhase(
          sample.progress *
          Math.PI *
          2,
        )

        if (!sample.isComplete) {
          continue
        }

        transition.isComplete =
          true

        const stationaryState:
          WolfPresentationState = {
            ...transition.state,
            locomotion:
              'stationary',
          }

        wolf.setState(
          stationaryState,
        )

        wolf.setGaitPhase(
          0,
        )
      }
    }

    const advanceGrazerPresentationTransitions = (
      nowMilliseconds: number,
    ) => {
      for (
        const [
          actorKey,
          transition,
        ] of
        playGrazerPresentationTransitions
      ) {
        if (transition.isComplete) {
          continue
        }

        const grazer =
          playGrazerPresentations.get(
            actorKey,
          )

        if (!grazer) {
          playGrazerPresentationTransitions.delete(
            actorKey,
          )

          continue
        }

        const sample =
          sampleLocalActorPresentationTransition(
            transition.interpolation,
            nowMilliseconds,
          )

        grazer.root.position.set(
          sample.position.eastMeters,
          sample.position.verticalMeters,
          sample.position.northMeters,
        )

        grazer.setGaitPhase(
          sample.progress *
          Math.PI *
          2,
        )

        if (!sample.isComplete) {
          continue
        }

        transition.isComplete =
          true

        const stationaryState:
          GrazerPresentationState = {
            locomotion:
              'stationary',
          }

        grazer.setState(
          stationaryState,
        )

        grazer.setGaitPhase(
          0,
        )
      }
    }

    const advancePresentationMovement = (
      nowMilliseconds: number,
    ) => {
      advanceWolfPresentationTransitions(
        nowMilliseconds,
      )

      advanceGrazerPresentationTransitions(
        nowMilliseconds,
      )

      const elapsedSeconds =
        Math.min(
          Math.max(
            (
              nowMilliseconds -
              lastMovementFrameMilliseconds
            ) /
              1_000,
            0,
          ),
          0.1,
        )

      lastMovementFrameMilliseconds =
        nowMilliseconds

      if (
        presentationEster !== null &&
        movementKeys.size > 0
      ) {
        const direction =
          resolveMovementDirection()

        if (direction !== null) {
          const distanceMeters =
            walkingMetersPerSecond *
            elapsedSeconds

          const next =
            moveSurfaceCoordinate(
              presentationEster,
              direction.north *
                distanceMeters,
              direction.east *
                distanceMeters,
              planet.meanRadiusMeters,
            )

          presentationEster = {
            ...presentationEster,
            latitudeDegrees:
              next.latitudeDegrees,
            longitudeDegrees:
              next.longitudeDegrees,
          }

          movementDirty =
            true

          updatePlaySpacePositions()
        }
      }

      if (
        movementDirty &&
        nowMilliseconds -
          lastMovementSyncMilliseconds >=
          movementSyncMilliseconds
      ) {
        synchronizeMovement(
          nowMilliseconds,
        )
      }

      window.requestAnimationFrame(
        advancePresentationMovement,
      )
    }

    window.requestAnimationFrame(
      advancePresentationMovement,
    )

  }

  const clearLivingMarkerHover = () => {
    livingMarkerTooltip.hidden =
      true

    canvas.style.cursor =
      ''

    if (sessionStatus) {
      delete sessionStatus.dataset
        .livingHover
    }
  }

  canvas.addEventListener(
    'pointermove',
    pointerEvent => {
      const pickInfo =
        scene.pick(
          scene.pointerX,
          scene.pointerY,
          mesh =>
            Boolean(
              mesh.metadata
                ?.livingMarker,
            ),
        )

      const pickedMesh =
        pickInfo?.hit
          ? pickInfo.pickedMesh
          : null

      const metadata =
        pickedMesh?.metadata
          ?.livingMarker as
            | LivingMarkerMetadata
            | undefined

      if (!metadata) {
        clearLivingMarkerHover()
        return
      }

      livingMarkerTooltipTitle.textContent =
        metadata.title

      livingMarkerTooltipDetail.textContent =
        metadata.detail

      livingMarkerTooltip.hidden =
        false

      const desiredLeft =
        pointerEvent.clientX + 14

      const desiredTop =
        pointerEvent.clientY + 14

      livingMarkerTooltip.style.left =
        `${Math.max(
          10,
          Math.min(
            desiredLeft,
            window.innerWidth -
              livingMarkerTooltip.offsetWidth -
              10,
          ),
        )}px`

      livingMarkerTooltip.style.top =
        `${Math.max(
          10,
          Math.min(
            desiredTop,
            window.innerHeight -
              livingMarkerTooltip.offsetHeight -
              10,
          ),
        )}px`

      canvas.style.cursor =
        'help'

      if (sessionStatus) {
        sessionStatus.dataset
          .livingHover =
            metadata.kind
      }
    },
  )

  canvas.addEventListener(
    'pointerleave',
    clearLivingMarkerHover,
  )

  const telemetrySection = (
    title: string,
    metrics: ReadonlyArray<
      readonly [string, string | number]
    >,
  ): string =>
    `
      <div class="simulation-telemetry-section">
        <div class="simulation-telemetry-section-title">
          ${title}
        </div>
        <div class="simulation-telemetry-grid">
          ${metrics.map(
            ([label, value]) =>
              `<div class="simulation-metric">
                <span class="simulation-metric-label">${label}</span>
                <strong>${value}</strong>
              </div>`,
          ).join('')}
        </div>
      </div>
    `

  const livingWorldRow = (
    kind: LivingMarkerKind,
    label: string,
    spriteUrls: readonly string[],
    value: string | number,
    detail: string,
  ): string =>
    `
      <div
        class="living-world-row"
        data-living-kind="${kind}"
      >
        <div class="living-world-identity">
          <div
            class="living-world-sprites"
            aria-hidden="true"
          >
            ${spriteUrls.map(
              url =>
                `<img
                  class="living-world-sprite"
                  src="${url}"
                  alt=""
                />`,
            ).join('')}
          </div>
          <div class="living-world-copy">
            <span>${label}</span>
            <small>${detail}</small>
          </div>
        </div>
        <strong>${value}</strong>
      </div>
    `

  const renderSimulationTelemetry = () => {
    if (!sessionStatus) {
      return
    }

    const population =
      world.population.filter(
        person =>
          person.planetId ===
          planet.planetId,
      )

    const wolves =
      world.animals.filter(
        animal =>
          animal.planetId ===
            planet.planetId &&
          animal.species === 'Wolf',
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

    const pregnantPeople =
      population.filter(
        person => person.isPregnant,
      ).length

    renderHumanMarkerLegend(
      activityCounts,
      pregnantPeople,
    )

    const pregnantWolves =
      wolves.filter(
        wolf => wolf.isPregnant,
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

    const birdMembers =
      birdFlocks?.flocks.reduce(
        (sum, flock) =>
          sum + flock.memberCount,
        0,
      ) ?? 0

    const grazerMembers =
      grazerCohorts?.cohorts.reduce(
        (sum, cohort) =>
          sum + cohort.memberCount,
        0,
      ) ?? 0

    const activeInvertebrateCellCount =
      invertebrates?.cells.filter(
        cell =>
          cell.liveBiomassKilogramsPerSquareMeter >
          0,
      ).length ?? 0

    const day =
      world.currentTimeSeconds /
      86_400

    sessionStatus.innerHTML =
      `
        <div class="simulation-telemetry-header">
          <div>
            <div class="simulation-telemetry-kicker">
              ${planet.name}
            </div>
            <div class="simulation-telemetry-population">
              ${population.length.toLocaleString()}
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
              Day ${day.toFixed(0)}
            </div>
          </div>
        </div>
      ` +
      telemetrySection(
        'Human activity',
        [
          ['Idle', activityCounts.Idle ?? 0],
          ['Foraging', activityCounts.Foraging ?? 0],
          ['Eating', activityCounts.Eating ?? 0],
          ['Traveling', activityCounts.Traveling ?? 0],
          ['Fleeing', activityCounts.Fleeing ?? 0],
          ['Seeking partner', activityCounts.SeekingPartner ?? 0],
          ['Mating', activityCounts.Mating ?? 0],
          ['Pregnant', pregnantPeople],
        ],
      ) +
      telemetrySection(
        'Condition',
        [
          ['Average energy', averageEnergy.toFixed(2)],
          ['Average health', averageHealth.toFixed(2)],
        ],
      ) +
      `
        <div class="simulation-telemetry-section">
          <div class="simulation-telemetry-section-title">
            Living world
          </div>
          <div class="living-world-list">
            ${livingWorldRow(
              'humans',
              'Humans',
              [
                humanManSpriteUrl,
                humanWomanSpriteUrl,
                humanBoySpriteUrl,
                humanGirlSpriteUrl,
              ],
              population.length.toLocaleString(),
              `${pregnantPeople} pregnant`,
            )}
            ${livingWorldRow(
              'wolves',
              'Wolves',
              [
                maleWolfSpriteUrl,
                femaleWolfSpriteUrl,
                wolfPupSpriteUrl,
              ],
              wolves.length.toLocaleString(),
              `${pregnantWolves} pregnant`,
            )}
            ${livingWorldRow(
              'birds',
              'Birds',
              [birdFlockSpriteUrl],
              birdMembers.toLocaleString(),
              `${birdFlocks?.flocks.length ?? 0} flocks`,
            )}
            ${livingWorldRow(
              'grazers',
              'Grazers',
              [grazerCohortSpriteUrl],
              grazerMembers.toLocaleString(),
              `${grazerCohorts?.cohorts.length ?? 0} cohorts`,
            )}
            ${livingWorldRow(
              'invertebrates',
              'Invertebrates',
              [invertebrateSpriteUrl],
              activeInvertebrateCellCount.toLocaleString(),
              'active surface cells',
            )}
          </div>
        </div>
      ` +
      telemetrySection(
        'Population change',
        [
          ['Births', `+${cumulativeMetrics.births}`],
          ['Natural deaths', `−${cumulativeMetrics.demographicDeaths}`],
          ['Starvation deaths', `−${cumulativeMetrics.starvationDeaths}`],
        ],
      ) +
      telemetrySection(
        'Movement & feeding',
        [
          ['Food-seeking steps', cumulativeMetrics.foodSeekingTravel],
          ['Continued travel', cumulativeMetrics.continuedFoodTravel],
          ['Feeding events', cumulativeMetrics.feedingEvents],
          ['Travel feedings', cumulativeMetrics.travelFeedingEvents],
          ['Random migrations', cumulativeMetrics.demographicMigrations],
          ['Scarcity migrations', cumulativeMetrics.scarcityMigrations],
          ['No viable food', cumulativeMetrics.noViableFoodFound],
        ],
      ) +
      telemetrySection(
        'Predation',
        [
          ['Wolf attacks', cumulativeMetrics.wolfAttacks],
          ['Failed attacks', cumulativeMetrics.failedAttacks],
          ['Human predation deaths', cumulativeMetrics.predationDeaths],
          ['Grazer kills', cumulativeMetrics.grazerKills],
          ['Wolf births', cumulativeMetrics.wolfBirths],
        ],
      )
  }

  const refreshTimelineMetrics = async () => {
    const timeline =
      await api.getTimeline(
        sessionId,
      )

    for (const event of timeline.events) {
      if (
        observedTimelineEvents.has(
          event.eventId,
        )
      ) {
        continue
      }

      observedTimelineEvents.add(
        event.eventId,
      )

      if (
        event.cause ===
        'population-dynamics'
      ) {
        cumulativeMetrics.demographicDeaths +=
          event.metrics.deaths ?? 0

        cumulativeMetrics.demographicMigrations +=
          event.metrics.migrations ?? 0
      }

      if (
        event.cause ===
        'reproduction'
      ) {
        cumulativeMetrics.births +=
          event.metrics.births ?? 0
      }

      if (
        event.cause ===
          'vegetation-foraging' ||
        event.cause ===
          'foraging'
      ) {
        cumulativeMetrics.starvationDeaths +=
          event.metrics.starvationDeaths ?? 0

        cumulativeMetrics.feedingEvents +=
          event.metrics.feedingEvents ?? 0

        cumulativeMetrics.travelFeedingEvents +=
          event.metrics.travelFeedingEvents ?? 0

        cumulativeMetrics.continuedFoodTravel +=
          event.metrics.continuedFoodTravel ?? 0

        cumulativeMetrics.foodSeekingTravel +=
          event.metrics.foodSeekingTravel ?? 0

        cumulativeMetrics.scarcityMigrations +=
          event.metrics.scarcityMigrations ?? 0

        cumulativeMetrics.noViableFoodFound +=
          event.metrics.noViableFoodFound ?? 0
      }

      if (
        event.cause ===
        'predation'
      ) {
        cumulativeMetrics.wolfAttacks +=
          event.metrics.wolfAttacks ?? 0

        cumulativeMetrics.failedAttacks +=
          event.metrics.failedAttacks ?? 0

        cumulativeMetrics.predationDeaths +=
          event.metrics.predationDeaths ?? 0

        cumulativeMetrics.grazerKills +=
          event.metrics.grazerKills ?? 0
      }

      if (
        event.cause ===
        'wolf-reproduction'
      ) {
        cumulativeMetrics.wolfBirths +=
          event.metrics.births ?? 0
      }
    }
  }

  await refreshTimelineMetrics()
  renderSimulationTelemetry()

  if (terrainStatus) {
    terrainStatus.textContent =
      `authoritative terrain · ${terrain.cells.length.toLocaleString()} cells · ${minimumElevationMeters.toFixed(0)} to ${maximumElevationMeters.toFixed(0)} m`
  }
  const simulationStepSeconds =
    86_400

  const simulationTickMilliseconds =
    500

  let simulationTickInProgress =
    false

  let simulationTickCount =
    0

  const updatePlanetVegetationCoverage = () => {
    const positions =
      planetMesh.getVerticesData(
        'position',
      )

    if (!positions) {
      throw new Error(
        'Planet position buffer is unavailable for vegetation refresh.',
      )
    }

    const coverage: number[] = []

    for (
      let index = 0;
      index < positions.length;
      index += 3
    ) {
      const x = positions[index]
      const y = positions[index + 1]
      const z = positions[index + 2]

      const length =
        Math.hypot(
          x,
          y,
          z,
        )

      if (
        !Number.isFinite(length) ||
        length <= 0
      ) {
        throw new Error(
          'Planet contains an invalid vertex direction during vegetation refresh.',
        )
      }

      const coordinate =
        sphereDirectionToGeographicDegrees({
          x: x / length,
          y: y / length,
          z: z / length,
        })

      const row =
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

      const normalizedLongitude =
        (
          (
            coordinate.longitudeDegrees +
            180
          ) %
            360 +
          360
        ) %
        360

      const column =
        Math.min(
          vegetationCoverageWidth - 1,
          Math.max(
            0,
            Math.floor(
              (
                normalizedLongitude /
                360
              ) *
                vegetationCoverageWidth,
            ),
          ),
        )

      coverage.push(
        vegetationCoverageData?.[
          row *
            vegetationCoverageWidth +
            column
        ] ?? 0,
      )
    }

    planetMesh.updateVerticesData(
      'vegetationCoverage',
      coverage,
      false,
      false,
    )
  }

  const refreshSimulation = async (
    stepSeconds =
      simulationStepSeconds,
  ) => {
    if (simulationTickInProgress) {
      return
    }

    simulationTickInProgress =
      true

    try {
      await api.advanceSession(
        sessionId,
        stepSeconds,
      )

      const [
        nextWorld,
        nextVegetation,
        nextInvertebrates,
        nextBirdFlocks,
        nextGrazerCohorts,
      ] =
        await Promise.all([
          api.getWorld(
            sessionId,
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

      world =
        nextWorld

      vegetation =
        nextVegetation

      invertebrates =
        nextInvertebrates

      birdFlocks =
        nextBirdFlocks

      grazerCohorts =
        nextGrazerCohorts

      simulationTickCount +=
        1

      if (
        simulationTickCount %
          10 ===
        0
      ) {
        await refreshTimelineMetrics()
      }

      updateVegetationCoverageState()
      updatePlanetVegetationCoverage()

      if (playMode) {
        updatePlaySpacePositions()
      } else {
        renderLivingWorld()
      }

      renderSimulationTelemetry()
    } catch (error) {
      console.error(
        '[Est Babylon] live simulation heartbeat failed',
        error,
      )

      if (faunaStatus) {
        faunaStatus.textContent =
          'LIVE UPDATE ERROR · see browser console'
      }
    } finally {
      simulationTickInProgress =
        false
    }
  }

  if (
    playMode &&
    faunaFocusRequested
  ) {
    faunaProofStepButton.addEventListener(
      'click',
      () => {
        if (simulationTickInProgress) {
          return
        }

        faunaProofStepButton.disabled =
          true

        const originalText =
          faunaProofStepButton.textContent

        faunaProofStepButton.textContent =
          'Stepping…'

        void refreshSimulation(
          1,
        ).finally(
          () => {
            faunaProofStepButton.disabled =
              false

            faunaProofStepButton.textContent =
              originalText
          },
        )
      },
    )
  }

  if (!playMode) {
    window.setInterval(
      () => {
        void refreshSimulation()
      },
      simulationTickMilliseconds,
    )
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
  true,
  1,
)

planetMesh.material =
  terrainSurfaceMaterial ??
  sharedSurface

planetMesh.isPickable = false

if (playMode) {
  for (const mesh of scene.meshes) {
    if (
      !mesh.name.startsWith(
        'play-',
      )
    ) {
      mesh.setEnabled(
        false,
      )
    }
  }
}

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
  if (playMode) {
    camera.minZ =
      0.05

    camera.maxZ =
      500

    return
  }

  const nearestPlanetDistance =
    Math.max(
      0,
      camera.radius -
        maximumPlanetRenderRadius,
    )

  // Keep the near plane comfortably in front of the planet while allowing it
  // to move outward as the camera retreats. This preserves depth precision
  // for shallow terrain/water separation at planetary viewing distances.
  const minimumNearPlane =
    playMode &&
    activePlanetMeanRadiusMeters
      ? 0.5 /
        activePlanetMeanRadiusMeters
      : 0.05

  camera.minZ =
    Math.max(
      minimumNearPlane,
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
