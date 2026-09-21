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
  WolfPresentationState,
} from './wolf-presentation-state'


export interface WolfPresentation
  extends FaunaPresentation {
  setSimulationTimeSeconds(
    simulationTimeSeconds: number,
  ): void
  setState(
    state: WolfPresentationState,
  ): void
  setGaitPhase(
    phaseRadians: number,
  ): void
}

export const wolfPresentationAssetPath =
  '/assets/animals/quaternius/ultimate-animated-animals/Wolf.gltf'

/**
 * The Quaternius mesh is authored facing +Z. Est fauna presentations
 * face local +X/east, so the asset-specific child rotates +Z onto +X.
 *
 * This correction lives below the Est-owned authoritative root and
 * therefore does not redefine Est heading semantics.
 */
export const wolfPresentationAssetYawCorrectionRadians =
  Math.PI / 2

/**
 * The source mesh is approximately 5.55 units long and 2.68 units tall.
 * At 0.30 presentation scale it is approximately 1.67 m long and
 * 0.80 m tall in Est's local metre-space.
 */
export const wolfPresentationAssetScale =
  0.30

export const wolfPresentationAnimationNames = {
  attack: 'Attack',
  eating: 'Eating',
  huntingIdle: 'Idle_2_HeadLow',
  idle: 'Idle',
  walk: 'Walk',
} as const

export type WolfPresentationAnimationName =
  typeof wolfPresentationAnimationNames[
    keyof typeof wolfPresentationAnimationNames
  ]

export type WolfPresentationAnimationPlayback =
  | 'simulation-time'
  | 'gait-phase'

export interface WolfPresentationAnimationSelection {
  name: WolfPresentationAnimationName
  playback: WolfPresentationAnimationPlayback
}

/**
 * Maps renderer-independent wolf presentation state onto the current
 * Quaternius asset.
 *
 * Authoritative displacement takes precedence for the single non-additive
 * rig: any moving wolf uses the in-place Walk clip. The authoritative
 * activity itself is not changed. Once displacement stops, the stationary
 * presentation returns to the activity-specific clip.
 */
export function resolveWolfPresentationAnimation(
  state: WolfPresentationState,
): WolfPresentationAnimationSelection {
  if (
    state.locomotion ===
    'moving'
  ) {
    return {
      name:
        wolfPresentationAnimationNames.walk,
      playback:
        'gait-phase',
    }
  }

  switch (state.activity) {
    case 'idle':
    case 'traveling':
      return {
        name:
          wolfPresentationAnimationNames.idle,
        playback:
          'simulation-time',
      }

    case 'hunting':
      return {
        name:
          wolfPresentationAnimationNames
            .huntingIdle,
        playback:
          'simulation-time',
      }

    case 'attacking':
      return {
        name:
          wolfPresentationAnimationNames.attack,
        playback:
          'simulation-time',
      }

    case 'eating':
      return {
        name:
          wolfPresentationAnimationNames.eating,
        playback:
          'simulation-time',
      }
  }
}

/**
 * Converts Est's periodic gait phase into a Babylon animation frame.
 *
 * A complete 2π cycle wraps back to the source frame. This keeps the
 * imported in-place gait subordinate to Est's bounded authoritative
 * interpolation rather than allowing animation time to advance movement.
 */
export function resolveWolfGaitFrame(
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
      'Wolf gait phase must be finite.',
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
      'Wolf animation frame range must be finite and ordered.',
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

/**
 * Maps authoritative Est simulation seconds onto a looping source clip.
 *
 * Babylon's animation clock never owns this phase. If Est simulation time
 * does not advance, the sampled rig pose does not advance either.
 */
export function resolveWolfSimulationFrame(
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
      'Wolf simulation time must be finite.',
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
      'Wolf animation frame range and duration must be finite, ordered, and positive.',
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


const wolfTemplatesByScene =
  new WeakMap<
    Scene,
    Promise<AssetContainer>
  >()

function loadWolfTemplate(
  scene: Scene,
): Promise<AssetContainer> {
  const existing =
    wolfTemplatesByScene.get(
      scene,
    )

  if (existing) {
    return existing
  }

  const loading =
    LoadAssetContainerAsync(
      wolfPresentationAssetPath,
      scene,
      {
        // Babylon's glTF default is FIRST, which would start the source
        // Attack clip as soon as the template loads. Est owns animation
        // selection, so template playback must begin completely stopped.
        pluginOptions: {
          gltf: {
            animationStartMode:
              GLTFLoaderAnimationStartMode.NONE,
          },
        },
      },
    ).catch(
      error => {
        wolfTemplatesByScene.delete(
          scene,
        )

        throw error
      },
    )

  wolfTemplatesByScene.set(
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
 * Creates the Quaternius-backed wolf presentation without changing
 * authoritative animal state.
 *
 * The outer root remains Est-owned:
 * - main.ts supplies authoritative local position;
 * - authoritative displacement supplies heading;
 * - bounded Est interpolation supplies gait phase;
 * - the imported rig supplies presentation pose only.
 *
 * This factory is intentionally not wired into main.ts yet. It can be
 * validated independently before replacing the temporary primitive wolf.
 */
export async function createAnimatedWolfPresentation(
  scene: Scene,
  actorKey: string,
): Promise<WolfPresentation> {
  const template =
    await loadWolfTemplate(
      scene,
    )

  const instanceNamePrefix =
    `play-wolf-${actorKey}-`

  const instance =
    template.instantiateModelsToScene(
      sourceName =>
        `${instanceNamePrefix}${sourceName}`,
      false,
      {
        // Each authoritative wolf receives an independent skeleton and
        // animation groups. Geometry and materials remain reusable.
        doNotInstantiate: true,
      },
    )

  const root =
    new TransformNode(
      `play-wolf-${actorKey}`,
      scene,
    )

  const assetRoot =
    new TransformNode(
      `play-wolf-${actorKey}-asset`,
      scene,
    )

  assetRoot.parent =
    root

  assetRoot.rotation.y =
    wolfPresentationAssetYawCorrectionRadians

  assetRoot.scaling.setAll(
    wolfPresentationAssetScale,
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
        `Wolf animation '${sourceName}' was instantiated more than once for ${actorKey}.`,
      )
    }

    animationGroupsBySourceName.set(
      sourceName,
      animationGroup,
    )
  }

  const requiredAnimationNames =
    Object.values(
      wolfPresentationAnimationNames,
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
        `Wolf asset is missing required animations for ${actorKey}:`,
        ...missingAnimationNames,
      ].join(
        ' ',
      ),
    )
  }

  function getAnimationGroup(
    animationName:
      WolfPresentationAnimationName,
  ) {
    const animationGroup =
      animationGroupsBySourceName.get(
        animationName,
      )

    if (!animationGroup) {
      throw new Error(
        `Wolf animation '${animationName}' was not available for ${actorKey}.`,
      )
    }

    return animationGroup
  }

  let currentAnimationName:
    WolfPresentationAnimationName | null =
      null

  let currentPlayback:
    WolfPresentationAnimationPlayback | null =
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
      WolfPresentationAnimationSelection,
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

    // All imported animation groups are started only to create Babylon's
    // animatables, then immediately paused. Babylon wall-clock time never
    // advances wolf presentation state.
    animationGroup.start(
      false,
    )

    animationGroup.pause()

    if (
      selection.playback ===
      'simulation-time'
    ) {
      animationGroup.goToFrame(
        resolveWolfSimulationFrame(
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
      resolveWolfGaitFrame(
        currentGaitPhase,
        animationGroup.from,
        animationGroup.to,
      ),
    )
  }

  // The presentation remains hidden until authoritative geography has
  // placed it. No source animation has been started at this point.
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
          'Wolf simulation time must be finite.',
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
        resolveWolfSimulationFrame(
          currentSimulationTimeSeconds,
          animationGroup.from,
          animationGroup.to,
          animationGroup.getLength(),
        ),
      )
    },

    setState(
      state: WolfPresentationState,
    ) {
      applyAnimationSelection(
        resolveWolfPresentationAnimation(
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
          'Wolf gait phase must be finite.',
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
        resolveWolfGaitFrame(
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
