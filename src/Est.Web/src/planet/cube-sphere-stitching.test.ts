import { describe, expect, it } from 'vitest'
import {
  createCubeSpherePatchGeometry,
} from './cube-sphere'

const segments = 8
const stride = segments + 1

const singleEdgeCases = [
  {
    name: 'left',
    stitchEdges: {
      left: true,
    },
    boundary: Array.from(
      { length: stride },
      (_, row) =>
        row * stride,
    ),
  },
  {
    name: 'right',
    stitchEdges: {
      right: true,
    },
    boundary: Array.from(
      { length: stride },
      (_, row) =>
        row * stride +
        segments,
    ),
  },
  {
    name: 'bottom',
    stitchEdges: {
      bottom: true,
    },
    boundary: Array.from(
      { length: stride },
      (_, column) =>
        column,
    ),
  },
  {
    name: 'top',
    stitchEdges: {
      top: true,
    },
    boundary: Array.from(
      { length: stride },
      (_, column) =>
        segments * stride +
        column,
    ),
  },
] as const

describe('cube-sphere mixed-LOD stitching', () => {
  for (const sample of singleEdgeCases) {
    it(`removes odd fine vertices from the ${sample.name} edge`, () => {
      const geometry =
        createCubeSpherePatchGeometry(
          'positiveZ',
          segments,
          -1,
          0,
          -1,
          0,
          sample.stitchEdges,
        )

      const used =
        new Set(
          geometry.indices,
        )

      sample.boundary.forEach(
        (
          vertex,
          index,
        ) => {
          expect(
            used.has(vertex),
          ).toBe(
            index % 2 === 0,
          )
        },
      )

      expect(
        geometry.indices.length /
          3,
      ).toBe(124)
    })
  }

  it('supports adjacent stitched edges', () => {
    const geometry =
      createCubeSpherePatchGeometry(
        'positiveZ',
        segments,
        -1,
        0,
        -1,
        0,
        {
          left: true,
          bottom: true,
        },
      )

    const used =
      new Set(
        geometry.indices,
      )

    for (
      let index = 1;
      index < segments;
      index += 2
    ) {
      expect(
        used.has(index),
      ).toBe(false)

      expect(
        used.has(
          index * stride,
        ),
      ).toBe(false)
    }

    expect(
      used.has(0),
    ).toBe(true)

    expect(
      geometry.indices.length /
        3,
    ).toBe(120)
  })

  it('matches coarse vertices across a cube-face transition', () => {
    const fine =
      createCubeSpherePatchGeometry(
        'positiveZ',
        segments,
        0.5,
        1,
        -1,
        -0.5,
        {
          right: true,
        },
      )

    const coarse =
      createCubeSpherePatchGeometry(
        'positiveX',
        segments,
        -1,
        0,
        -1,
        0,
      )

    for (
      let coarseRow = 0;
      coarseRow <=
        segments / 2;
      coarseRow += 1
    ) {
      const fineRow =
        coarseRow * 2

      const fineVertex =
        fineRow * stride +
        segments

      const coarseVertex =
        coarseRow * stride

      for (
        let component = 0;
        component < 3;
        component += 1
      ) {
        expect(
          fine.positions[
            fineVertex * 3 +
              component
          ],
        ).toBeCloseTo(
          coarse.positions[
            coarseVertex * 3 +
              component
          ],
          12,
        )
      }
    }
  })
})
