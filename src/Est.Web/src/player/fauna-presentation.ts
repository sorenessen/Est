import {
  Color3,
  Scene,
  StandardMaterial,
  TransformNode,
} from '@babylonjs/core'

import {
  CreateCapsule,
} from '@babylonjs/core/Meshes/Builders/capsuleBuilder.js'

import {
  CreateCylinder,
} from '@babylonjs/core/Meshes/Builders/cylinderBuilder.js'

import {
  CreateSphere,
} from '@babylonjs/core/Meshes/Builders/sphereBuilder.js'


export interface FaunaPresentation {
  actorKey: string
  root: TransformNode
  setEnabled(enabled: boolean): void
  dispose(): void
}

interface FaunaMaterials {
  wolf: StandardMaterial
  grazer: StandardMaterial
}

const materialsByScene =
  new WeakMap<
    Scene,
    FaunaMaterials
  >()

function getFaunaMaterials(
  scene: Scene,
): FaunaMaterials {
  const existing =
    materialsByScene.get(
      scene,
    )

  if (existing) {
    return existing
  }

  const wolf =
    new StandardMaterial(
      'play-wolf-material',
      scene,
    )

  wolf.diffuseColor =
    new Color3(
      0.32,
      0.34,
      0.36,
    )

  wolf.specularColor =
    new Color3(
      0.08,
      0.08,
      0.08,
    )

  const grazer =
    new StandardMaterial(
      'play-grazer-material',
      scene,
    )

  grazer.diffuseColor =
    new Color3(
      0.48,
      0.31,
      0.16,
    )

  grazer.specularColor =
    new Color3(
      0.07,
      0.06,
      0.05,
    )

  const created = {
    wolf,
    grazer,
  }

  materialsByScene.set(
    scene,
    created,
  )

  return created
}

function createLeg(
  scene: Scene,
  name: string,
  root: TransformNode,
  material: StandardMaterial,
  x: number,
  z: number,
  height: number,
): void {
  const leg =
    CreateCylinder(
      name,
      {
        height,
        diameter:
          0.09,
        tessellation:
          10,
      },
      scene,
    )

  leg.parent =
    root

  leg.position.set(
    x,
    height / 2,
    z,
  )

  leg.material =
    material

  leg.isPickable =
    false
}

/**
 * Temporary asset-independent ground presentation for an individually
 * authoritative wolf.
 *
 * Identity and location remain owned by the projected AnimalId-backed actor.
 * Replacing this silhouette with a production rig must not affect authority.
 */
export function createWolfPresentation(
  scene: Scene,
  actorKey: string,
): FaunaPresentation {
  const root =
    new TransformNode(
      `play-${actorKey}`,
      scene,
    )

  const material =
    getFaunaMaterials(
      scene,
    ).wolf

  const body =
    CreateCapsule(
      `play-${actorKey}-body`,
      {
        height:
          1.15,
        radius:
          0.22,
        tessellation:
          16,
        subdivisions:
          2,
      },
      scene,
    )

  body.parent =
    root

  body.rotation.z =
    Math.PI /
    2

  body.position.y =
    0.46

  body.material =
    material

  body.isPickable =
    false

  const head =
    CreateSphere(
      `play-${actorKey}-head`,
      {
        diameter:
          0.38,
        segments:
          14,
      },
      scene,
    )

  head.parent =
    root

  head.position.set(
    0.56,
    0.54,
    0,
  )

  head.material =
    material

  head.isPickable =
    false

  createLeg(
    scene,
    `play-${actorKey}-leg-a`,
    root,
    material,
    -0.32,
    -0.13,
    0.43,
  )

  createLeg(
    scene,
    `play-${actorKey}-leg-b`,
    root,
    material,
    -0.32,
    0.13,
    0.43,
  )

  createLeg(
    scene,
    `play-${actorKey}-leg-c`,
    root,
    material,
    0.32,
    -0.13,
    0.43,
  )

  createLeg(
    scene,
    `play-${actorKey}-leg-d`,
    root,
    material,
    0.32,
    0.13,
    0.43,
  )

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

    dispose() {
      root.dispose(
        false,
        false,
      )
    },
  }
}

/**
 * Temporary asset-independent presentation for one deterministic local
 * representative of an aggregate grazer cohort.
 *
 * This mesh is presentation identity only. It is not an AnimalId-bearing
 * simulation entity.
 */
export function createGrazerPresentation(
  scene: Scene,
  actorKey: string,
): FaunaPresentation {
  const root =
    new TransformNode(
      `play-${actorKey}`,
      scene,
    )

  const material =
    getFaunaMaterials(
      scene,
    ).grazer

  const body =
    CreateCapsule(
      `play-${actorKey}-body`,
      {
        height:
          1.65,
        radius:
          0.34,
        tessellation:
          16,
        subdivisions:
          2,
      },
      scene,
    )

  body.parent =
    root

  body.rotation.z =
    Math.PI /
    2

  body.position.y =
    0.67

  body.material =
    material

  body.isPickable =
    false

  const head =
    CreateSphere(
      `play-${actorKey}-head`,
      {
        diameter:
          0.48,
        segments:
          14,
      },
      scene,
    )

  head.parent =
    root

  head.position.set(
    0.78,
    0.76,
    0,
  )

  head.material =
    material

  head.isPickable =
    false

  createLeg(
    scene,
    `play-${actorKey}-leg-a`,
    root,
    material,
    -0.48,
    -0.19,
    0.62,
  )

  createLeg(
    scene,
    `play-${actorKey}-leg-b`,
    root,
    material,
    -0.48,
    0.19,
    0.62,
  )

  createLeg(
    scene,
    `play-${actorKey}-leg-c`,
    root,
    material,
    0.48,
    -0.19,
    0.62,
  )

  createLeg(
    scene,
    `play-${actorKey}-leg-d`,
    root,
    material,
    0.48,
    0.19,
    0.62,
  )

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

    dispose() {
      root.dispose(
        false,
        false,
      )
    },
  }
}
