import {
  Scene,
  TransformNode,
} from '@babylonjs/core'

import type {
  AssetContainer,
} from '@babylonjs/core/assetContainer.js'

import {
  LoadAssetContainerAsync,
} from '@babylonjs/core/Loading/sceneLoader.js'

import {
  GLTFLoaderAnimationStartMode,
} from '@babylonjs/loaders/glTF/index.js'

import {
  resolveFaunaPresentationRotationY,
  type FaunaPresentation,
} from './fauna-presentation-contract'

import type {
  GrazerPresentationState,
} from './grazer-presentation-state'


export interface GrazerPresentation
  extends FaunaPresentation {
  setSimulationTimeSeconds(
    simulationTimeSeconds: number,
  ): void
  setState(
    state: GrazerPresentationState,
  ): void
  setGaitPhase(
    phaseRadians: number,
  ): void
}

export const grazerPresentationAssetPath =
  '/assets/animals/quaternius/ultimate-animated-animals/Deer.gltf'

/**
 * The Quaternius deer is authored facing +Z. Est fauna presentations face
 * local +X/east, so the asset-specific child rotates +Z onto +X.
 *
 * This correction belongs below the Est-owned presentation root and therefore
 * does not redefine cohort position, presentation identity, or heading.
 */
export const grazerPresentationAssetYawCorrectionRadians =
  Math.PI / 2

/**
 * The source mesh is approximately 4.40 units long and 4.27 units tall.
 * At 0.30 presentation scale it is approximately 1.32 m long and
 * 1.28 m tall in Est's local metre-space.
 */
export const grazerPresentationAssetScale =
  0.30

export const grazerPresentationAnimationNames = {
  idle: 'Idle',
  walk: 'Walk',
} as const

export type GrazerPresentationAnimationName =
  typeof grazerPresentationAnimationNames[
    keyof typeof grazerPresentationAnimationNames
  ]

export type GrazerPresentationAnimationPlayback =
  | 'simulation-time'
  | 'gait-phase'

export interface GrazerPresentationAnimationSelection {
  name: GrazerPresentationAnimationName
  playback: GrazerPresentationAnimationPlayback
}

/**
 * Grazer representatives have presentation locomotion only. Est does not
 * currently expose per-representative authoritative activity such as eating,
 * attacking, or dying.
 */
export function resolveGrazerPresentationAnimation(
  state: GrazerPresentationState,
): GrazerPresentationAnimationSelection {
  if (
    state.locomotion ===
    'moving'
  ) {
    return {
      name:
        grazerPresentationAnimationNames.walk,
      playback:
        'gait-phase',
    }
  }

  return {
    name:
      grazerPresentationAnimationNames.idle,
    playback:
      'simulation-time',
  }
}

export function resolveGrazerGaitFrame(
  phaseRadians: number,
  fromFrame: number,
  toFrame: number,
): number {
  if (
    !Number.isFinite(
      phaseRadians,
    )
  ) {
    throw new RangeError(
      'Grazer gait phase must be finite.',
    )
  }

  if (
    !Number.isFinite(
      fromFrame,
    ) ||
    !Number.isFinite(
      toFrame,
    ) ||
    toFrame <
      fromFrame
  ) {
    throw new RangeError(
      'Grazer animation frame range must be finite and ordered.',
    )
  }

  const cycleRadians =
    Math.PI *
    2

  const wrappedPhase =
    (
      (
        phaseRadians %
        cycleRadians
      ) +
      cycleRadians
    ) %
    cycleRadians

  const progress =
    wrappedPhase /
    cycleRadians

  return (
    fromFrame +
    (
      toFrame -
      fromFrame
    ) *
      progress
  )
}

export function resolveGrazerSimulationFrame(
  simulationTimeSeconds: number,
  fromFrame: number,
  toFrame: number,
  durationSeconds: number,
): number {
  if (
    !Number.isFinite(
      simulationTimeSeconds,
    )
  ) {
    throw new RangeError(
      'Grazer simulation time must be finite.',
    )
  }

  if (
    !Number.isFinite(
      fromFrame,
    ) ||
    !Number.isFinite(
      toFrame,
    ) ||
    toFrame <
      fromFrame ||
    !Number.isFinite(
      durationSeconds,
    ) ||
    durationSeconds <=
      0
  ) {
    throw new RangeError(
      'Grazer animation frame range and duration must be finite, ordered, and positive.',
    )
  }

  const wrappedSeconds =
    (
      (
        simulationTimeSeconds %
        durationSeconds
      ) +
      durationSeconds
    ) %
    durationSeconds

  const progress =
    wrappedSeconds /
    durationSeconds

  return (
    fromFrame +
    (
      toFrame -
      fromFrame
    ) *
      progress
  )
}

const grazerTemplatesByScene =
  new WeakMap<
    Scene,
    Promise<AssetContainer>
  >()

function loadGrazerTemplate(
  scene: Scene,
): Promise<AssetContainer> {
  const existing =
    grazerTemplatesByScene.get(
      scene,
    )

  if (existing) {
    return existing
  }

  const loading =
    LoadAssetContainerAsync(
      grazerPresentationAssetPath,
      scene,
      {
        // Babylon's glTF default is FIRST, which would start the source
        // Attack_Headbutt clip as soon as the template loads. Est owns
        // animation selection, so template playback must begin completely
        // stopped.
        pluginOptions: {
          gltf: {
            animationStartMode:
              GLTFLoaderAnimationStartMode.NONE,
          },
        },
      },
    ).catch(
      error => {
        grazerTemplatesByScene.delete(
          scene,
        )

        throw error
      },
    )

  grazerTemplatesByScene.set(
    scene,
    loading,
  )

  return loading
}

function resolveSourceAnimationName(
  instanceAnimationName: string,
  instanceNamePrefix: string,
): string {
  return instanceAnimationName.startsWith(
    instanceNamePrefix,
  )
    ? instanceAnimationName.slice(
        instanceNamePrefix.length,
      )
    : instanceAnimationName
}

/**
 * Creates a Quaternius-backed presentation for one deterministic local
 * representative of an authoritative aggregate grazer cohort.
 *
 * The outer root remains Est-owned presentation state:
 * - local grazer projection supplies the representative position;
 * - observed representative displacement may supply presentation heading;
 * - bounded presentation interpolation may supply gait phase;
 * - the imported rig supplies presentation pose only.
 *
 * The representative remains cohort-backed presentation identity. This
 * factory does not create an AnimalId or independently simulated grazer.
 *
 * This factory is intentionally not wired into main.ts yet. It can be
 * validated independently before replacing the temporary primitive grazer.
 */
export async function createAnimatedGrazerPresentation(
  scene: Scene,
  actorKey: string,
): Promise<GrazerPresentation> {
  const template =
    await loadGrazerTemplate(
      scene,
    )

  const instanceNamePrefix =
    `play-grazer-${actorKey}-`

  const instance =
    template.instantiateModelsToScene(
      sourceName =>
        `${instanceNamePrefix}${sourceName}`,
      false,
      {
        // Each local presentation representative receives an independent
        // skeleton and animation groups. Geometry and materials remain reusable.
        doNotInstantiate: true,
      },
    )

  const root =
    new TransformNode(
      `play-grazer-${actorKey}`,
      scene,
    )

  const assetRoot =
    new TransformNode(
      `play-grazer-${actorKey}-asset`,
      scene,
    )

  assetRoot.parent =
    root

  assetRoot.rotation.y =
    grazerPresentationAssetYawCorrectionRadians

  assetRoot.scaling.setAll(
    grazerPresentationAssetScale,
  )

  for (
    const sourceRoot of
    instance.rootNodes
  ) {
    sourceRoot.parent =
      assetRoot
  }

  for (
    const mesh of
    root.getChildMeshes()
  ) {
    mesh.isPickable =
      false
  }

  const animationGroupsBySourceName =
    new Map<
      string,
      typeof instance.animationGroups[number]
    >()

  for (
    const animationGroup of
    instance.animationGroups
  ) {
    const sourceName =
      resolveSourceAnimationName(
        animationGroup.name,
        instanceNamePrefix,
      )

    if (
      animationGroupsBySourceName.has(
        sourceName,
      )
    ) {
      instance.dispose()
      root.dispose()

      throw new Error(
        `Grazer animation '${sourceName}' was instantiated more than once for ${actorKey}.`,
      )
    }

    animationGroupsBySourceName.set(
      sourceName,
      animationGroup,
    )
  }

  const requiredAnimationNames =
    Object.values(
      grazerPresentationAnimationNames,
    )

  const missingAnimationNames =
    requiredAnimationNames.filter(
      animationName =>
        !animationGroupsBySourceName.has(
          animationName,
        ),
    )

  if (
    missingAnimationNames.length >
    0
  ) {
    instance.dispose()
    root.dispose()

    throw new Error(
      [
        `Grazer asset is missing required animations for ${actorKey}:`,
        ...missingAnimationNames,
      ].join(
        ' ',
      ),
    )
  }

  function getAnimationGroup(
    animationName:
      GrazerPresentationAnimationName,
  ) {
    const animationGroup =
      animationGroupsBySourceName.get(
        animationName,
      )

    if (!animationGroup) {
      throw new Error(
        `Grazer animation '${animationName}' was not available for ${actorKey}.`,
      )
    }

    return animationGroup
  }

  let currentAnimationName:
    GrazerPresentationAnimationName | null =
      null

  let currentPlayback:
    GrazerPresentationAnimationPlayback | null =
      null

  let currentGaitPhase =
    0

  let currentSimulationTimeSeconds =
    0

  function stopCurrentAnimation(): void {
    if (
      currentAnimationName ===
      null
    ) {
      return
    }

    const animationGroup =
      getAnimationGroup(
        currentAnimationName,
      )

    if (
      animationGroup.isStarted
    ) {
      animationGroup.stop(
        true,
      )
    }
  }

  function applyAnimationSelection(
    selection:
      GrazerPresentationAnimationSelection,
  ): void {
    if (
      currentAnimationName ===
        selection.name &&
      currentPlayback ===
        selection.playback
    ) {
      return
    }

    stopCurrentAnimation()

    currentAnimationName =
      selection.name

    currentPlayback =
      selection.playback

    const animationGroup =
      getAnimationGroup(
        selection.name,
      )

    // Imported animation groups are started only to create Babylon's
    // animatables, then immediately paused. Babylon wall-clock time never
    // advances grazer presentation state.
    animationGroup.start(
      false,
    )

    animationGroup.pause()

    if (
      selection.playback ===
      'simulation-time'
    ) {
      animationGroup.goToFrame(
        resolveGrazerSimulationFrame(
          currentSimulationTimeSeconds,
          animationGroup.from,
          animationGroup.to,
          animationGroup.getLength(),
        ),
      )

      return
    }

    // Walk is in-place and follows only Est's bounded locomotion phase.
    animationGroup.goToFrame(
      resolveGrazerGaitFrame(
        currentGaitPhase,
        animationGroup.from,
        animationGroup.to,
      ),
    )
  }

  // The presentation remains hidden until cohort-backed local projection
  // has placed it. No source animation has been started at this point.
  root.setEnabled(
    false,
  )

  return {
    actorKey,
    root,

    setEnabled(
      enabled: boolean,
    ) {
      root.setEnabled(
        enabled,
      )
    },

    setHeadingRadians(
      headingRadians: number,
    ) {
      root.rotation.y =
        resolveFaunaPresentationRotationY(
          headingRadians,
        )
    },

    setSimulationTimeSeconds(
      simulationTimeSeconds: number,
    ) {
      if (
        !Number.isFinite(
          simulationTimeSeconds,
        )
      ) {
        throw new RangeError(
          'Grazer simulation time must be finite.',
        )
      }

      currentSimulationTimeSeconds =
        simulationTimeSeconds

      if (
        currentPlayback !==
          'simulation-time' ||
        currentAnimationName ===
          null
      ) {
        return
      }

      const animationGroup =
        getAnimationGroup(
          currentAnimationName,
        )

      animationGroup.goToFrame(
        resolveGrazerSimulationFrame(
          currentSimulationTimeSeconds,
          animationGroup.from,
          animationGroup.to,
          animationGroup.getLength(),
        ),
      )
    },

    setState(
      state: GrazerPresentationState,
    ) {
      applyAnimationSelection(
        resolveGrazerPresentationAnimation(
          state,
        ),
      )
    },

    setGaitPhase(
      phaseRadians: number,
    ) {
      if (
        !Number.isFinite(
          phaseRadians,
        )
      ) {
        throw new RangeError(
          'Grazer gait phase must be finite.',
        )
      }

      currentGaitPhase =
        phaseRadians

      if (
        currentPlayback !==
          'gait-phase' ||
        currentAnimationName ===
          null
      ) {
        return
      }

      const animationGroup =
        getAnimationGroup(
          currentAnimationName,
        )

      animationGroup.goToFrame(
        resolveGrazerGaitFrame(
          currentGaitPhase,
          animationGroup.from,
          animationGroup.to,
        ),
      )
    },

    dispose() {
      instance.dispose()
      root.dispose()
    },
  }
}
