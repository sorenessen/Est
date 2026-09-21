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

export interface HumanPresentation {
  personId: string
  bodyVariant: HumanBodyVariant
  root: TransformNode
  setEnabled(enabled: boolean): void
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

  for (
    const sourceRoot of
    instance.rootNodes
  ) {
    sourceRoot.parent =
      root
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

  if (!idleTemplate) {
    instance.dispose()
    root.dispose()

    throw new Error(
      `Human animation '${humanPresentationIdleAnimationName}' was not found.`,
    )
  }

  const missingTargetNames =
    idleTemplate
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
            return '<unnamed>'
          }

          return personNodesBySourceName.has(
            targetName,
          )
            ? null
            : targetName
        },
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
        `Human animation '${humanPresentationIdleAnimationName}'`,
        `could not map ${missingTargetNames.length} targets`,
        `for person ${personId}:`,
        ...missingTargetNames,
      ].join(
        ' ',
      ),
    )
  }

  const idleAnimation =
    idleTemplate.clone(
      `${instanceNamePrefix}${humanPresentationIdleAnimationName}`,
      sourceTarget => {
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
      },
      false,
    )

  // Animation controls only the cloned humanoid rig beneath this root.
  // Est continues to position the outer root from authoritative
  // geographic simulation state.
  idleAnimation.start(
    true,
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

    dispose() {
      idleAnimation.dispose()
      instance.dispose()
      root.dispose()
    },
  }
}
