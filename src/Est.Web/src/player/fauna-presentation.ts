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

import type {
  WolfPresentation,
} from './wolf-presentation'

import type {
  WolfPresentationState,
} from './wolf-presentation-state'

export type {
  FaunaPresentation,
} from './fauna-presentation-contract'

export type {
  WolfPresentation,
} from './wolf-presentation'


interface FaunaMaterials {
  wolf: StandardMaterial
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
 * Temporary asset-independent ground presentation for an individually
 * authoritative wolf.
 *
 * Identity and location remain owned by the projected AnimalId-backed actor.
 * Replacing this silhouette with a production rig must not affect authority.
 */
export function createWolfPresentation(
  scene: Scene,
  actorKey: string,
): WolfPresentation {
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

  const legA =
    createLeg(
      scene,
      `play-${actorKey}-leg-a`,
    root,
    material,
    -0.32,
    -0.13,
    0.43,
  )

  const legB =
    createLeg(
      scene,
      `play-${actorKey}-leg-b`,
    root,
    material,
    -0.32,
    0.13,
    0.43,
  )

  const legC =
    createLeg(
      scene,
      `play-${actorKey}-leg-c`,
    root,
    material,
    0.32,
    -0.13,
    0.43,
  )

  const legD =
    createLeg(
      scene,
      `play-${actorKey}-leg-d`,
    root,
    material,
    0.32,
    0.13,
    0.43,
  )

  let currentState:
    WolfPresentationState = {
      activity:
        'idle',
      locomotion:
        'stationary',
    }

  const applyState = (
    state: WolfPresentationState,
    gaitPhaseRadians = 0,
  ) => {
    body.position.y =
      0.46

    head.position.set(
      0.56,
      0.54,
      0,
    )

    legA.rotation.z =
      0

    legB.rotation.z =
      0

    legC.rotation.z =
      0

    legD.rotation.z =
      0

    if (
      state.locomotion ===
      'moving'
    ) {
      const strideAmplitude =
        state.activity ===
          'attacking'
          ? 0.75
          : 0.55

      const strideRadians =
        Math.sin(
          gaitPhaseRadians,
        ) *
        strideAmplitude

      legA.rotation.z =
        strideRadians

      legD.rotation.z =
        strideRadians

      legB.rotation.z =
        -strideRadians

      legC.rotation.z =
        -strideRadians

      body.position.y +=
        Math.abs(
          Math.sin(
            gaitPhaseRadians *
            2,
          ),
        ) *
        0.025
    }

    switch (state.activity) {
      case 'idle':
        break

      case 'hunting':
        body.position.y =
          0.42

        head.position.set(
          0.61,
          0.47,
          0,
        )
        break

      case 'traveling':
        body.position.y =
          0.45
        break

      case 'attacking':
        body.position.y =
          0.40

        head.position.set(
          0.70,
          0.44,
          0,
        )
        break

      case 'eating':
        body.position.y =
          0.43

        head.position.set(
          0.62,
          0.27,
          0,
        )
        break
    }
  }

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

    setState(
      state: WolfPresentationState,
    ) {
      currentState =
        state

      applyState(
        currentState,
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

      applyState(
        currentState,
        phaseRadians,
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
