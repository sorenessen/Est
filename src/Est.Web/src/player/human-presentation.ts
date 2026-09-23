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

import '@babylonjs/loaders/glTF/index.js'


export type HumanBodyVariant =
  | 'female'
  | 'male'

export const humanPresentationAssetPaths:
  Record<
    HumanBodyVariant,
    string
  > = {
    female:
      '/assets/characters/quaternius/universal-base-characters/Superhero_Female_FullBody.gltf',
    male:
      '/assets/characters/quaternius/universal-base-characters/Superhero_Male_FullBody.gltf',
  }

export const humanPresentationAnimationAssetPath =
  '/assets/characters/quaternius/universal-animation-library/UAL1_Standard.glb'

export const humanPresentationIdleAnimationName =
  'Idle_Loop'

export const humanPresentationWalkAnimationName =
  'Walk_Loop'

export const humanPresentationAssetYawCorrectionRadians =
  Math.PI / 2

export interface HumanPresentation {
  personId: string
  bodyVariant: HumanBodyVariant
  root: TransformNode
  setEnabled(enabled: boolean): void
  setHeadingRadians(
    headingRadians: number,
  ): void
  setWalkBlendWeight(
    weight: number,
  ): void
  setGaitPhase(
    phaseRadians: number,
  ): void
  dispose(): void
}

const templatesByScene =
  new WeakMap<
    Scene,
    Map<
      HumanBodyVariant,
      Promise<AssetContainer>
    >
  >()

const animationLibrariesByScene =
  new WeakMap<
    Scene,
    Promise<AssetContainer>
  >()

export function resolveHumanBodyVariant(
  sex: string,
): HumanBodyVariant | null {
  switch (
    sex
      .trim()
      .toLowerCase()
  ) {
    case 'female':
      return 'female'

    case 'male':
      return 'male'

    default:
      return null
  }
}


export function resolveHumanPresentationRotationY(
  headingRadians: number,
): number {
  if (
    !Number.isFinite(
      headingRadians,
    )
  ) {
    throw new RangeError(
      'Human presentation heading must be finite.',
    )
  }

  return -headingRadians
}

export function resolveHumanGaitFrame(
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
      'Human gait phase must be finite.',
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
      'Human animation frame range must be finite and ordered.',
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

function getTemplateMap(
  scene: Scene,
) {
  const existing =
    templatesByScene.get(
      scene,
    )

  if (existing) {
    return existing
  }

  const created =
    new Map<
      HumanBodyVariant,
      Promise<AssetContainer>
    >()

  templatesByScene.set(
    scene,
    created,
  )

  return created
}

function loadHumanTemplate(
  scene: Scene,
  bodyVariant:
    HumanBodyVariant,
): Promise<AssetContainer> {
  const templates =
    getTemplateMap(
      scene,
    )

  const existing =
    templates.get(
      bodyVariant,
    )

  if (existing) {
    return existing
  }

  const loading =
    LoadAssetContainerAsync(
      humanPresentationAssetPaths[
        bodyVariant
      ],
      scene,
    ).catch(
      error => {
        templates.delete(
          bodyVariant,
        )

        throw error
      },
    )

  templates.set(
    bodyVariant,
    loading,
  )

  return loading
}

function loadHumanAnimationLibrary(
  scene: Scene,
): Promise<AssetContainer> {
  const existing =
    animationLibrariesByScene.get(
      scene,
    )

  if (existing) {
    return existing
  }

  const loading =
    LoadAssetContainerAsync(
      humanPresentationAnimationAssetPath,
      scene,
    ).catch(
      error => {
        animationLibrariesByScene.delete(
          scene,
        )

        throw error
      },
    )

  animationLibrariesByScene.set(
    scene,
    loading,
  )

  return loading
}

export async function createHumanPresentation(
  scene: Scene,
  personId: string,
  sex: string,
): Promise<HumanPresentation> {
  const bodyVariant =
    resolveHumanBodyVariant(
      sex,
    )

  if (bodyVariant === null) {
    throw new Error(
      `Unsupported person sex for ${personId}: ${sex}`,
    )
  }

  const [
    template,
    animationLibrary,
  ] =
    await Promise.all([
      loadHumanTemplate(
        scene,
        bodyVariant,
      ),
      loadHumanAnimationLibrary(
        scene,
      ),
    ])

  const instanceNamePrefix =
    `play-human-${personId}-`

  const instance =
    template.instantiateModelsToScene(
      sourceName =>
        `${instanceNamePrefix}${sourceName}`,
      false,
      {
        // Every simulated person receives an independent rig so
        // presentation animation cannot mutate another person's pose.
        // Geometry/material resources remain reusable.
        doNotInstantiate: true,
      },
    )

  const root =
    new TransformNode(
      `play-human-${personId}`,
      scene,
    )

  const assetRoot =
    new TransformNode(
      `play-human-${personId}-asset`,
      scene,
    )

  assetRoot.parent =
    root

  assetRoot.rotation.y =
    humanPresentationAssetYawCorrectionRadians

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

  const personNodesBySourceName =
    new Map<
      string,
      ReturnType<
        TransformNode['getDescendants']
      >[number]
    >()

  for (
    const sourceRoot of
    instance.rootNodes
  ) {
    const nodes = [
      sourceRoot,
      ...sourceRoot.getDescendants(),
    ]

    for (
      const node of nodes
    ) {
      const sourceName =
        node.name.startsWith(
          instanceNamePrefix,
        )
          ? node.name.slice(
              instanceNamePrefix.length,
            )
          : node.name

      if (
        !personNodesBySourceName.has(
          sourceName,
        )
      ) {
        personNodesBySourceName.set(
          sourceName,
          node,
        )
      }
    }
  }

  const idleTemplate =
    animationLibrary
      .animationGroups
      .find(
        animationGroup =>
          animationGroup.name ===
          humanPresentationIdleAnimationName,
      )

  const walkTemplate =
    animationLibrary
      .animationGroups
      .find(
        animationGroup =>
          animationGroup.name ===
          humanPresentationWalkAnimationName,
      )

  const missingAnimationNames = [
    idleTemplate
      ? null
      : humanPresentationIdleAnimationName,
    walkTemplate
      ? null
      : humanPresentationWalkAnimationName,
  ].filter(
    (
      animationName,
    ): animationName is string =>
      animationName !== null,
  )

  if (
    missingAnimationNames.length >
    0
  ) {
    instance.dispose()
    root.dispose()

    throw new Error(
      [
        `Human animation library is missing required animations for ${personId}:`,
        ...missingAnimationNames,
      ].join(
        ' ',
      ),
    )
  }

  if (
    !idleTemplate ||
    !walkTemplate
  ) {
    throw new Error(
      `Human animation validation failed for ${personId}.`,
    )
  }

  const requiredTemplates = [
    idleTemplate,
    walkTemplate,
  ]

  const missingTargetNames =
    requiredTemplates
      .flatMap(
        animationTemplate =>
          animationTemplate
            .targetedAnimations
            .map(
              targetedAnimation => {
                const targetName =
                  targetedAnimation
                    .target
                    ?.name

                if (
                  typeof targetName !==
                  'string'
                ) {
                  return (
                    `${animationTemplate.name}:<unnamed>`
                  )
                }

                return personNodesBySourceName.has(
                  targetName,
                )
                  ? null
                  : (
                      `${animationTemplate.name}:${targetName}`
                    )
              },
            ),
      )
      .filter(
        (
          targetName,
        ): targetName is string =>
          targetName !== null,
      )

  if (
    missingTargetNames.length >
    0
  ) {
    instance.dispose()
    root.dispose()

    throw new Error(
      [
        'Human animations could not map required targets',
        `for person ${personId}:`,
        ...missingTargetNames,
      ].join(
        ' ',
      ),
    )
  }

  const mapAnimationTarget = (
    sourceTarget:
      typeof idleTemplate
        .targetedAnimations[number]['target'],
  ) => {
    const targetName =
      sourceTarget?.name

    if (
      typeof targetName !==
      'string'
    ) {
      throw new Error(
        `Human animation target has no name for person ${personId}.`,
      )
    }

    const mapped =
      personNodesBySourceName.get(
        targetName,
      )

    if (!mapped) {
      throw new Error(
        `Human animation target '${targetName}' was not mapped for person ${personId}.`,
      )
    }

    return mapped
  }

  const idleAnimation =
    idleTemplate.clone(
      `${instanceNamePrefix}${humanPresentationIdleAnimationName}`,
      mapAnimationTarget,
      false,
    )

  const walkAnimation =
    walkTemplate.clone(
      `${instanceNamePrefix}${humanPresentationWalkAnimationName}`,
      mapAnimationTarget,
      false,
    )

  let currentGaitPhase =
    0

  let walkAnimationStarted =
    false

  idleAnimation.start(
    true,
  )

  idleAnimation.setWeightForAllAnimatables(
    1,
  )

  // The presentation remains hidden until main.ts has placed it from
  // authoritative person geography.
  root.setEnabled(
    false,
  )

  return {
    personId,
    bodyVariant,
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
        resolveHumanPresentationRotationY(
          headingRadians,
        )
    },

    setWalkBlendWeight(
      weight: number,
    ) {
      if (
        !Number.isFinite(
          weight,
        ) ||
        weight < 0 ||
        weight > 1
      ) {
        throw new RangeError(
          'Human walk blend weight must be finite and between zero and one.',
        )
      }

      idleAnimation.setWeightForAllAnimatables(
        1 - weight,
      )

      if (
        weight <= 0 &&
        !walkAnimationStarted
      ) {
        return
      }

      if (!walkAnimationStarted) {
        walkAnimation.start(
          false,
        )

        walkAnimation.pause()

        walkAnimationStarted =
          true
      }

      walkAnimation.setWeightForAllAnimatables(
        weight,
      )

      walkAnimation.goToFrame(
        resolveHumanGaitFrame(
          currentGaitPhase,
          walkAnimation.from,
          walkAnimation.to,
        ),
        true,
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
          'Human gait phase must be finite.',
        )
      }

      currentGaitPhase =
        phaseRadians

      if (!walkAnimationStarted) {
        return
      }

      walkAnimation.goToFrame(
        resolveHumanGaitFrame(
          currentGaitPhase,
          walkAnimation.from,
          walkAnimation.to,
        ),
        true,
      )
    },

    dispose() {
      idleAnimation.dispose()
      walkAnimation.dispose()
      instance.dispose()
      root.dispose()
    },
  }
}
