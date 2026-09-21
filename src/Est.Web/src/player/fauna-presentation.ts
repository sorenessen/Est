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

import {
  resolveFaunaPresentationRotationY,
  type FaunaPresentation,
} from './fauna-presentation-contract'

export type {
  FaunaPresentation,
} from './fauna-presentation-contract'

interface FaunaMaterials {
  grazer: StandardMaterial
}

function setPresentationHeading(
  root: TransformNode,
  headingRadians: number,
): void {
  root.rotation.y =
    resolveFaunaPresentationRotationY(
      headingRadians,
    )
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
): ReturnType<
  typeof CreateCylinder
> {
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

  return leg
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

    setHeadingRadians(
      headingRadians: number,
    ) {
      setPresentationHeading(
        root,
        headingRadians,
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
