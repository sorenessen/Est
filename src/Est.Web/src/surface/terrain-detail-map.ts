function hashIdentity(
  identity: string,
): number {
  let hash =
    2166136261

  for (
    let index = 0;
    index < identity.length;
    index += 1
  ) {
    hash ^=
      identity.charCodeAt(
        index,
      )

    hash =
      Math.imul(
        hash,
        16777619,
      )
  }

  return hash >>> 0
}

function wrapIndex(
  value: number,
  period: number,
): number {
  return (
    (
      value %
      period
    ) +
    period
  ) %
    period
}

function latticeValue(
  x: number,
  y: number,
  period: number,
  seed: number,
): number {
  const wrappedX =
    wrapIndex(
      x,
      period,
    )

  const wrappedY =
    wrapIndex(
      y,
      period,
    )

  let hash =
    seed ^
    Math.imul(
      wrappedX,
      374761393,
    ) ^
    Math.imul(
      wrappedY,
      668265263,
    )

  hash =
    Math.imul(
      hash ^
        (
          hash >>>
          13
        ),
      1274126177,
    )

  hash ^=
    hash >>>
    16

  return (
    (
      hash >>>
      0
    ) /
      4294967295
  ) *
    2 -
    1
}

function smoothStep(
  value: number,
): number {
  return (
    value *
    value *
    (
      3 -
      2 *
        value
    )
  )
}

function periodicValueNoise(
  normalizedX: number,
  normalizedY: number,
  period: number,
  seed: number,
): number {
  const scaledX =
    normalizedX *
    period

  const scaledY =
    normalizedY *
    period

  const x0 =
    Math.floor(
      scaledX,
    )

  const y0 =
    Math.floor(
      scaledY,
    )

  const tx =
    smoothStep(
      scaledX -
        x0,
    )

  const ty =
    smoothStep(
      scaledY -
        y0,
    )

  const a =
    latticeValue(
      x0,
      y0,
      period,
      seed,
    )

  const b =
    latticeValue(
      x0 + 1,
      y0,
      period,
      seed,
    )

  const c =
    latticeValue(
      x0,
      y0 + 1,
      period,
      seed,
    )

  const d =
    latticeValue(
      x0 + 1,
      y0 + 1,
      period,
      seed,
    )

  const top =
    a +
    (
      b -
      a
    ) *
      tx

  const bottom =
    c +
    (
      d -
      c
    ) *
      tx

  return (
    top +
    (
      bottom -
      top
    ) *
      ty
  )
}

export function createTerrainDetailMapPixels(
  size: number,
  tileSizeMeters: number,
  identity: string,
): Uint8ClampedArray {
  if (
    !Number.isInteger(
      size,
    ) ||
    size < 8
  ) {
    throw new Error(
      'Terrain detail texture size must be an integer of at least 8.',
    )
  }

  if (
    !Number.isFinite(
      tileSizeMeters,
    ) ||
    tileSizeMeters <= 0
  ) {
    throw new Error(
      'Terrain detail tile size must be positive and finite.',
    )
  }

  if (
    identity.length === 0
  ) {
    throw new Error(
      'Terrain detail identity cannot be empty.',
    )
  }

  const seed =
    hashIdentity(
      identity,
    )

  const pixelSpacingMeters =
    tileSizeMeters /
    size

  const height =
    new Float32Array(
      size *
      size,
    )

  const albedo =
    new Float32Array(
      size *
      size,
    )

  for (
    let y = 0;
    y < size;
    y += 1
  ) {
    for (
      let x = 0;
      x < size;
      x += 1
    ) {
      const normalizedX =
        x /
        size

      const normalizedY =
        y /
        size

      const broad =
        periodicValueNoise(
          normalizedX,
          normalizedY,
          8,
          seed ^
            0x2a8317d1,
        )

      const medium =
        periodicValueNoise(
          normalizedX,
          normalizedY,
          16,
          seed ^
            0x5b19c3e7,
        )

      const small =
        periodicValueNoise(
          normalizedX,
          normalizedY,
          32,
          seed ^
            0x713b942d,
        )

      const fine =
        periodicValueNoise(
          normalizedX,
          normalizedY,
          64,
          seed ^
            0x19d74ab3,
        )

      const micro =
        periodicValueNoise(
          normalizedX,
          normalizedY,
          96,
          seed ^
            0x4dc28f61,
        )

      // Per-texel grain supplies the highest-frequency presentation
      // detail. At the current 8 m / 256 texel tile this is roughly
      // 3 cm structure, while mipmapping naturally removes it with
      // distance.
      const grain =
        latticeValue(
          x,
          y,
          size,
          seed ^
            0x6f31b8d5,
        )

      const index =
        y *
          size +
        x

      height[index] =
        broad *
          0.015 +
        medium *
          0.009 +
        small *
          0.006 +
        fine *
          0.003 +
        micro *
          0.002 +
        grain *
          0.001

      albedo[index] =
        broad *
          0.22 +
        medium *
          0.20 +
        small *
          0.20 +
        fine *
          0.18 +
        micro *
          0.12 +
        grain *
          0.08
    }
  }

  const pixels =
    new Uint8ClampedArray(
      size *
        size *
        4,
    )

  for (
    let y = 0;
    y < size;
    y += 1
  ) {
    for (
      let x = 0;
      x < size;
      x += 1
    ) {
      const index =
        y *
          size +
        x

      const leftIndex =
        y *
          size +
        wrapIndex(
          x - 1,
          size,
        )

      const rightIndex =
        y *
          size +
        wrapIndex(
          x + 1,
          size,
        )

      const upIndex =
        wrapIndex(
          y - 1,
          size,
        ) *
          size +
        x

      const downIndex =
        wrapIndex(
          y + 1,
          size,
        ) *
          size +
        x

      const slopeX =
        (
          height[
            rightIndex
          ] -
          height[
            leftIndex
          ]
        ) /
        (
          2 *
          pixelSpacingMeters
        )

      const slopeY =
        (
          height[
            downIndex
          ] -
          height[
            upIndex
          ]
        ) /
        (
          2 *
          pixelSpacingMeters
        )

      let normalX =
        -slopeX

      let normalY =
        -slopeY

      let normalZ =
        1

      const length =
        Math.hypot(
          normalX,
          normalY,
          normalZ,
        )

      normalX /=
        length

      normalY /=
        length

      normalZ /=
        length

      const detailAlbedo =
        Math.max(
          0.30,
          Math.min(
            0.70,
            0.5 +
              albedo[
                index
              ] *
                0.20,
          ),
        )

      const pixelIndex =
        index *
        4

      // Babylon detail-map packing:
      // R = diffuse multiplier
      // G = detail normal Y
      // B = roughness, neutral for StandardMaterial
      // A = detail normal X
      pixels[
        pixelIndex
      ] =
        Math.round(
          detailAlbedo *
          255,
        )

      pixels[
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

      pixels[
        pixelIndex + 2
      ] =
        128

      pixels[
        pixelIndex + 3
      ] =
        Math.round(
          (
            normalX *
              0.5 +
            0.5
          ) *
          255,
        )
    }
  }

  return pixels
}
