import {
  Color3,
  DynamicTexture,
  StandardMaterial,
  Texture,
  type Scene,
} from '@babylonjs/core'

export type EsterCapsuleSkin =
  | 'ester'
  | 'tylenol'

export interface EsterCapsuleSkinDefinition {
  readonly upperColor: string
  readonly lowerColor: string
  readonly seamColor: string
  readonly primaryText: string
  readonly primaryTextColor: string
  readonly secondaryText: string
  readonly secondaryTextColor: string
}

export const esterCapsuleSkinDefinitions:
  Readonly<
    Record<
      EsterCapsuleSkin,
      EsterCapsuleSkinDefinition
    >
  > = {
    ester: {
      upperColor:
        '#f5f7f4',
      lowerColor:
        '#163a45',
      seamColor:
        '#a9b7b7',
      primaryText:
        'ESTER',
      primaryTextColor:
        '#ffffff',
      secondaryText:
        'EST',
      secondaryTextColor:
        '#163a45',
    },

    tylenol: {
      upperColor:
        '#d71920',
      lowerColor:
        '#f8f8f4',
      seamColor:
        '#9d1117',
      primaryText:
        'TYLENOL',
      primaryTextColor:
        '#ffffff',
      secondaryText:
        'EST',
      secondaryTextColor:
        '#b5121b',
    },
  }

export function resolveEsterCapsuleSkin(
  search: string,
): EsterCapsuleSkin {
  const requested =
    new URLSearchParams(
      search,
    )
      .get(
        'esterSkin',
      )
      ?.trim()
      .toLowerCase()

  return requested ===
    'tylenol'
    ? 'tylenol'
    : 'ester'
}

function drawRepeatedImprint(
  context:
    ReturnType<
      DynamicTexture['getContext']
    >,
  textureSize: number,
  text: string,
  y: number,
  color: string,
  fontSize: number,
): void {
  context.save()

  context.fillStyle =
    color

  context.font =
    `900 ${fontSize}px Arial, Helvetica, sans-serif`

  // Babylon exposes a portable canvas abstraction that does not include
  // textAlign/textBaseline in its public type. Center the imprint
  // explicitly so this remains compatible with DynamicTexture.
  const estimatedTextWidth =
    text.length *
    fontSize *
    0.61

  const baselineY =
    y +
    fontSize *
    0.34

  // Opposite sides of the capsule. One marking should remain readable
  // from most ordinary third-person viewing angles.
  context.fillText(
    text,
    textureSize *
      0.25 -
      estimatedTextWidth /
        2,
    baselineY,
  )

  context.fillText(
    text,
    textureSize *
      0.75 -
      estimatedTextWidth /
        2,
    baselineY,
  )

  context.restore()
}

function createEsterCapsuleTexture(
  scene: Scene,
  skin: EsterCapsuleSkin,
): DynamicTexture {
  const textureSize =
    1024

  const definition =
    esterCapsuleSkinDefinitions[
      skin
    ]

  const texture =
    new DynamicTexture(
      `play-ester-${skin}-texture`,
      {
        width:
          textureSize,
        height:
          textureSize,
      },
      scene,
      true,
    )

  const context =
    texture.getContext()

  const middle =
    textureSize /
    2

  context.clearRect(
    0,
    0,
    textureSize,
    textureSize,
  )

  context.fillStyle =
    definition.upperColor

  context.fillRect(
    0,
    0,
    textureSize,
    middle,
  )

  context.fillStyle =
    definition.lowerColor

  context.fillRect(
    0,
    middle,
    textureSize,
    middle,
  )

  // A small manufactured-looking overlap/seam makes the object read
  // as a two-piece capsule instead of a single recolored primitive.
  const seamHeight =
    18

  context.fillStyle =
    definition.seamColor

  context.fillRect(
    0,
    middle -
      seamHeight /
        2,
    textureSize,
    seamHeight,
  )

  context.globalAlpha =
    0.28

  context.fillStyle =
    '#ffffff'

  context.fillRect(
    0,
    middle -
      seamHeight /
        2,
    textureSize,
    3,
  )

  context.globalAlpha =
    1

  if (
    skin ===
    'tylenol'
  ) {
    drawRepeatedImprint(
      context,
      textureSize,
      definition.primaryText,
      textureSize *
        0.27,
      definition.primaryTextColor,
      82,
    )

    drawRepeatedImprint(
      context,
      textureSize,
      definition.secondaryText,
      textureSize *
        0.72,
      definition.secondaryTextColor,
      62,
    )
  } else {
    drawRepeatedImprint(
      context,
      textureSize,
      definition.secondaryText,
      textureSize *
        0.27,
      definition.secondaryTextColor,
      66,
    )

    drawRepeatedImprint(
      context,
      textureSize,
      definition.primaryText,
      textureSize *
        0.72,
      definition.primaryTextColor,
      82,
    )
  }

  texture.wrapU =
    Texture.WRAP_ADDRESSMODE

  texture.wrapV =
    Texture.CLAMP_ADDRESSMODE

  // Babylon's capsule UVs wrap around the mesh in the opposite
  // horizontal direction from the DynamicTexture canvas. Flip U so
  // printed capsule markings read normally rather than mirrored.
  texture.uScale =
    -1

  texture.uOffset =
    1

  texture.anisotropicFilteringLevel =
    8

  texture.update()

  return texture
}

export function createEsterCapsuleMaterial(
  scene: Scene,
  skin: EsterCapsuleSkin,
): StandardMaterial {
  const material =
    new StandardMaterial(
      `play-ester-${skin}-material`,
      scene,
    )

  material.diffuseTexture =
    createEsterCapsuleTexture(
      scene,
      skin,
    )

  material.diffuseColor =
    Color3.White()

  material.specularColor =
    new Color3(
      0.92,
      0.92,
      0.92,
    )

  material.specularPower =
    128

  material.emissiveColor =
    new Color3(
      0.018,
      0.018,
      0.018,
    )

  material.backFaceCulling =
    true

  return material
}
