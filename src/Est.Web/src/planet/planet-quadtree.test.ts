import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  balancePlanetPatches,
  filterPlanetPatchesByFrustum,
  findPlanetPatchNeighbors,
  findPlanetPatchStitchEdges,
  patchBoundingSphere,
  patchBounds,
  patchCenterDirection,
  patchKey,
  patchesAtLevel,
  selectBalancedPlanetPatches,
  selectPlanetPatches,
  sphereDirectionToCubeFaceCoordinates,
  subdividePatch,
  type PlanetPatch,
} from './planet-quadtree'

import {
  CUBE_FACES,
  createCubeSpherePatchGeometry,
} from './cube-sphere'

describe('planet quadtree', () => {
  it('maps the root patch to the complete cube face', () => {
    expect(
      patchBounds({
        face: 'positiveZ',
        level: 0,
        x: 0,
        y: 0,
      }),
    ).toEqual({
      uMin: -1,
      uMax: 1,
      vMin: -1,
      vMax: 1,
    })
  })

  it('subdivides a patch into four exact children', () => {
    const root: PlanetPatch = {
      face: 'positiveZ',
      level: 0,
      x: 0,
      y: 0,
    }

    const children =
      subdividePatch(root)

    expect(children).toEqual([
      {
        face: 'positiveZ',
        level: 1,
        x: 0,
        y: 0,
      },
      {
        face: 'positiveZ',
        level: 1,
        x: 1,
        y: 0,
      },
      {
        face: 'positiveZ',
        level: 1,
        x: 0,
        y: 1,
      },
      {
        face: 'positiveZ',
        level: 1,
        x: 1,
        y: 1,
      },
    ])

    expect(
      children.map(patchBounds),
    ).toEqual([
      {
        uMin: -1,
        uMax: 0,
        vMin: -1,
        vMax: 0,
      },
      {
        uMin: 0,
        uMax: 1,
        vMin: -1,
        vMax: 0,
      },
      {
        uMin: -1,
        uMax: 0,
        vMin: 0,
        vMax: 1,
      },
      {
        uMin: 0,
        uMax: 1,
        vMin: 0,
        vMax: 1,
      },
    ])
  })

  it('produces the expected number of patches per level', () => {
    expect(
      patchesAtLevel(
        'negativeX',
        0,
      ),
    ).toHaveLength(1)

    expect(
      patchesAtLevel(
        'negativeX',
        1,
      ),
    ).toHaveLength(4)

    expect(
      patchesAtLevel(
        'negativeX',
        2,
      ),
    ).toHaveLength(16)

    expect(
      patchesAtLevel(
        'negativeX',
        3,
      ),
    ).toHaveLength(64)
  })

  it('tiles a face continuously without gaps or overlap', () => {
    const patches =
      patchesAtLevel(
        'positiveY',
        3,
      )

    const count = 8

    for (
      let y = 0;
      y < count;
      y += 1
    ) {
      for (
        let x = 0;
        x < count;
        x += 1
      ) {
        const patch =
          patches[
            y * count + x
          ]

        const bounds =
          patchBounds(patch)

        if (x < count - 1) {
          const right =
            patchBounds(
              patches[
                y * count +
                x +
                1
              ],
            )

          expect(
            bounds.uMax,
          ).toBeCloseTo(
            right.uMin,
            12,
          )
        }

        if (y < count - 1) {
          const above =
            patchBounds(
              patches[
                (y + 1) *
                  count +
                x
              ],
            )

          expect(
            bounds.vMax,
          ).toBeCloseTo(
            above.vMin,
            12,
          )
        }
      }
    }
  })
})

describe('planet LOD selection', () => {
  const options = {
    minimumLevel: 1,
    maximumLevel: 5,
    splitThreshold: 0.28,
  } as const

  it('refines the planet more deeply as the camera approaches', () => {
    const far =
      selectPlanetPatches(
        {
          x: 0,
          y: 0,
          z: 8,
        },
        options,
      )

    const near =
      selectPlanetPatches(
        {
          x: 0,
          y: 0,
          z: 1.2,
        },
        options,
      )

    expect(
      near.length,
    ).toBeGreaterThan(
      far.length,
    )

    expect(
      Math.max(
        ...near.map(
          (patch) =>
            patch.level,
        ),
      ),
    ).toBeGreaterThan(
      Math.max(
        ...far.map(
          (patch) =>
            patch.level,
        ),
      ),
    )
  })

  it('never selects beyond the configured maximum level', () => {
    const selected =
      selectPlanetPatches(
        {
          x: 0,
          y: 0,
          z: 1.01,
        },
        options,
      )

    expect(
      Math.max(
        ...selected.map(
          (patch) =>
            patch.level,
        ),
      ),
    ).toBeLessThanOrEqual(
      options.maximumLevel,
    )
  })

  it('preserves complete cube-face coverage at mixed levels', () => {
    const selected =
      selectPlanetPatches(
        {
          x: 1.4,
          y: 0.35,
          z: 1.2,
        },
        options,
      )

    for (const face of CUBE_FACES) {
      const normalizedArea =
        selected
          .filter(
            (patch) =>
              patch.face === face,
          )
          .reduce(
            (
              total,
              patch,
            ) =>
              total +
              4 /
                4 **
                  patch.level,
            0,
          )

      expect(
        normalizedArea,
      ).toBeCloseTo(
        4,
        12,
      )
    }
  })

  it('produces mixed levels for an ordinary oblique view', () => {
    const selected =
      selectPlanetPatches(
        {
          x: 2.1,
          y: 1.3,
          z: 2.4,
        },
        options,
      )

    const levels =
      new Set(
        selected.map(
          (patch) =>
            patch.level,
        ),
      )

    expect(
      levels.size,
    ).toBeGreaterThan(1)
  })
})

describe('planet quadtree frustum culling', () => {
  it('bounds generated patch vertices conservatively', () => {
    for (const face of CUBE_FACES) {
      const patch: PlanetPatch = {
        face,
        level: 2,
        x: 1,
        y: 2,
      }

      const bounds =
        patchBounds(patch)

      const geometry =
        createCubeSpherePatchGeometry(
          face,
          8,
          bounds.uMin,
          bounds.uMax,
          bounds.vMin,
          bounds.vMax,
        )

      const sphere =
        patchBoundingSphere(patch)

      for (
        let index = 0;
        index <
        geometry.positions.length;
        index += 3
      ) {
        const distance =
          Math.hypot(
            geometry.positions[index] -
              sphere.center.x,
            geometry.positions[
              index + 1
            ] -
              sphere.center.y,
            geometry.positions[
              index + 2
            ] -
              sphere.center.z,
          )

        expect(
          distance,
        ).toBeLessThanOrEqual(
          sphere.radius +
            1e-12,
        )
      }
    }
  })

  it('rejects a patch wholly outside a frustum plane', () => {
    const patch: PlanetPatch = {
      face: 'positiveZ',
      level: 3,
      x: 3,
      y: 3,
    }

    const center =
      patchCenterDirection(patch)

    const visible =
      filterPlanetPatchesByFrustum(
        [patch],
        [
          {
            normal: {
              x: -center.x,
              y: -center.y,
              z: -center.z,
            },
            d: 0,
          },
        ],
      )

    expect(visible).toEqual([])
  })

  it('keeps a patch that intersects a frustum plane', () => {
    const patch: PlanetPatch = {
      face: 'positiveZ',
      level: 3,
      x: 3,
      y: 3,
    }

    const sphere =
      patchBoundingSphere(patch)

    const visible =
      filterPlanetPatchesByFrustum(
        [patch],
        [
          {
            normal: {
              x: -sphere.center.x,
              y: -sphere.center.y,
              z: -sphere.center.z,
            },
            d:
              1 -
              sphere.radius / 2,
          },
        ],
      )

    expect(visible).toEqual([
      patch,
    ])
  })

  it('filters opposite planetary patches by view half-space', () => {
    const front: PlanetPatch = {
      face: 'positiveZ',
      level: 3,
      x: 3,
      y: 3,
    }

    const back: PlanetPatch = {
      face: 'negativeZ',
      level: 3,
      x: 3,
      y: 3,
    }

    const center =
      patchCenterDirection(front)

    const visible =
      filterPlanetPatchesByFrustum(
        [
          front,
          back,
        ],
        [
          {
            normal: center,
            d: 0,
          },
        ],
      )

    expect(visible).toEqual([
      front,
    ])
  })
})

describe('planet quadtree balancing', () => {
  it('inverts all six cube-face center directions', () => {
    const centers = [
      {
        point: {
          x: 1,
          y: 0,
          z: 0,
        },
        face: 'positiveX',
      },
      {
        point: {
          x: -1,
          y: 0,
          z: 0,
        },
        face: 'negativeX',
      },
      {
        point: {
          x: 0,
          y: 1,
          z: 0,
        },
        face: 'positiveY',
      },
      {
        point: {
          x: 0,
          y: -1,
          z: 0,
        },
        face: 'negativeY',
      },
      {
        point: {
          x: 0,
          y: 0,
          z: 1,
        },
        face: 'positiveZ',
      },
      {
        point: {
          x: 0,
          y: 0,
          z: -1,
        },
        face: 'negativeZ',
      },
    ] as const

    for (const sample of centers) {
      const location =
        sphereDirectionToCubeFaceCoordinates(
          sample.point,
        )

      expect(
        location.face,
      ).toBe(sample.face)

      expect(
        location.u,
      ).toBeCloseTo(0, 12)

      expect(
        location.v,
      ).toBeCloseTo(0, 12)
    }
  })

  it('balances an intentionally extreme mixed-level face', () => {
    const coarse =
      patchesAtLevel(
        'positiveZ',
        1,
      ).filter(
        (patch) =>
          !(
            patch.x === 0 &&
            patch.y === 0
          ),
      )

    const deep =
      patchesAtLevel(
        'positiveZ',
        4,
      ).filter(
        (patch) =>
          patch.x < 8 &&
          patch.y < 8,
      )

    const balanced =
      balancePlanetPatches(
        [
          ...coarse,
          ...deep,
        ],
        4,
      )

    const neighbors =
      findPlanetPatchNeighbors(
        balanced,
        4,
      )

    for (const pair of neighbors) {
      expect(
        Math.abs(
          pair.first.level -
            pair.second.level,
        ),
      ).toBeLessThanOrEqual(1)
    }

    const normalizedArea =
      balanced.reduce(
        (
          total,
          patch,
        ) =>
          total +
          4 /
            4 **
              patch.level,
        0,
      )

    expect(
      normalizedArea,
    ).toBeCloseTo(4, 12)
  })

  it('keeps camera-selected neighboring patches within one level', () => {
    const options = {
      minimumLevel: 1,
      maximumLevel: 5,
      splitThreshold: 0.28,
    } as const

    const selected =
      selectBalancedPlanetPatches(
        {
          x: 1.31,
          y: 0.47,
          z: 1.18,
        },
        options,
      )

    const neighbors =
      findPlanetPatchNeighbors(
        selected,
        options.maximumLevel,
      )

    expect(
      neighbors.length,
    ).toBeGreaterThan(0)

    for (const pair of neighbors) {
      expect(
        Math.abs(
          pair.first.level -
            pair.second.level,
        ),
      ).toBeLessThanOrEqual(1)
    }
  })

  it('balances neighbors across cube-face boundaries too', () => {
    const options = {
      minimumLevel: 1,
      maximumLevel: 5,
      splitThreshold: 0.24,
    } as const

    const selected =
      selectBalancedPlanetPatches(
        {
          x: 1.18,
          y: 0.11,
          z: 1.18,
        },
        options,
      )

    const crossFaceNeighbors =
      findPlanetPatchNeighbors(
        selected,
        options.maximumLevel,
      ).filter(
        (pair) =>
          pair.first.face !==
          pair.second.face,
      )

    expect(
      crossFaceNeighbors.length,
    ).toBeGreaterThan(0)

    for (
      const pair of
        crossFaceNeighbors
    ) {
      expect(
        Math.abs(
          pair.first.level -
            pair.second.level,
        ),
      ).toBeLessThanOrEqual(1)
    }
  })
  it('marks the fine side of a same-face coarse-to-fine boundary for stitching', () => {
    const coarse: PlanetPatch = {
      face: 'positiveZ',
      level: 1,
      x: 0,
      y: 0,
    }

    const fineLower: PlanetPatch = {
      face: 'positiveZ',
      level: 2,
      x: 2,
      y: 0,
    }

    const fineUpper: PlanetPatch = {
      face: 'positiveZ',
      level: 2,
      x: 2,
      y: 1,
    }

    const stitches =
      findPlanetPatchStitchEdges(
        [
          coarse,
          fineLower,
          fineUpper,
        ],
        2,
      )

    expect(
      stitches.get(
        patchKey(fineLower),
      ),
    ).toEqual({
      left: true,
      right: false,
      bottom: false,
      top: false,
    })

    expect(
      stitches.get(
        patchKey(fineUpper),
      ),
    ).toEqual({
      left: true,
      right: false,
      bottom: false,
      top: false,
    })

    expect(
      stitches.get(
        patchKey(coarse),
      ),
    ).toEqual({
      left: false,
      right: false,
      bottom: false,
      top: false,
    })
  })

  it('marks the fine side across a cube-face boundary for stitching', () => {
    const coarse: PlanetPatch = {
      face: 'positiveX',
      level: 1,
      x: 0,
      y: 0,
    }

    const fine: PlanetPatch = {
      face: 'positiveZ',
      level: 2,
      x: 3,
      y: 0,
    }

    const stitches =
      findPlanetPatchStitchEdges(
        [
          coarse,
          fine,
        ],
        2,
      )

    expect(
      stitches.get(
        patchKey(fine),
      ),
    ).toEqual({
      left: false,
      right: true,
      bottom: false,
      top: false,
    })

    expect(
      stitches.get(
        patchKey(coarse),
      ),
    ).toEqual({
      left: false,
      right: false,
      bottom: false,
      top: false,
    })
  })

})
