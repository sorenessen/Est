import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  CUBE_FACES,
  createCubeSphereFaceGeometry,
  cubeFacePoint,
  projectCubePointToUnitSphere,
} from './cube-sphere'

function positionKey(
  x: number,
  y: number,
  z: number,
): string {
  return [
    x.toFixed(12),
    y.toFixed(12),
    z.toFixed(12),
  ].join(',')
}

describe('cube sphere', () => {
  it('projects every cube-face sample onto the unit sphere', () => {
    for (const face of CUBE_FACES) {
      for (const u of [-1, -0.4, 0, 0.6, 1]) {
        for (const v of [-1, -0.3, 0, 0.8, 1]) {
          const projected =
            projectCubePointToUnitSphere(
              cubeFacePoint(face, u, v),
            )

          expect(
            Math.hypot(
              projected.x,
              projected.y,
              projected.z,
            ),
          ).toBeCloseTo(1, 12)
        }
      }
    }
  })

  it('places the six face centers on the six cardinal axes', () => {
    const expected = {
      positiveX: [1, 0, 0],
      negativeX: [-1, 0, 0],
      positiveY: [0, 1, 0],
      negativeY: [0, -1, 0],
      positiveZ: [0, 0, 1],
      negativeZ: [0, 0, -1],
    } as const

    for (const face of CUBE_FACES) {
      const point =
        projectCubePointToUnitSphere(
          cubeFacePoint(face, 0, 0),
        )

      expect(point.x).toBeCloseTo(expected[face][0], 12)
      expect(point.y).toBeCloseTo(expected[face][1], 12)
      expect(point.z).toBeCloseTo(expected[face][2], 12)
    }
  })

  it('creates the expected regular patch topology', () => {
    const segments = 8

    const geometry =
      createCubeSphereFaceGeometry(
        'positiveZ',
        segments,
      )

    expect(
      geometry.positions.length,
    ).toBe(
      (segments + 1) *
      (segments + 1) *
      3,
    )

    expect(
      geometry.normals.length,
    ).toBe(
      geometry.positions.length,
    )

    expect(
      geometry.indices.length,
    ).toBe(
      segments *
      segments *
      6,
    )
  })

  it('makes all neighboring face boundaries geometrically identical', () => {
    const segments = 8
    const boundaryCounts =
      new Map<string, number>()

    for (const face of CUBE_FACES) {
      const geometry =
        createCubeSphereFaceGeometry(
          face,
          segments,
        )

      const stride = segments + 1

      for (
        let row = 0;
        row <= segments;
        row += 1
      ) {
        for (
          let column = 0;
          column <= segments;
          column += 1
        ) {
          if (
            row !== 0 &&
            row !== segments &&
            column !== 0 &&
            column !== segments
          ) {
            continue
          }

          const index =
            row * stride + column

          const offset = index * 3

          const key = positionKey(
            geometry.positions[offset],
            geometry.positions[offset + 1],
            geometry.positions[offset + 2],
          )

          boundaryCounts.set(
            key,
            (boundaryCounts.get(key) ?? 0) + 1,
          )
        }
      }
    }

    for (const count of boundaryCounts.values()) {
      expect(
        count === 2 || count === 3,
      ).toBe(true)
    }

    const cornerCount = [
      ...boundaryCounts.values(),
    ].filter(
      (count) => count === 3,
    ).length

    expect(cornerCount).toBe(8)
  })

  it('winds every render triangle outward', () => {
    for (const face of CUBE_FACES) {
      const geometry =
        createCubeSphereFaceGeometry(
          face,
          6,
        )

      for (
        let index = 0;
        index < geometry.indices.length;
        index += 3
      ) {
        const a = geometry.indices[index]
        const b = geometry.indices[index + 1]
        const c = geometry.indices[index + 2]

        const ax = geometry.positions[a * 3]
        const ay = geometry.positions[a * 3 + 1]
        const az = geometry.positions[a * 3 + 2]

        const abx =
          geometry.positions[b * 3] - ax
        const aby =
          geometry.positions[b * 3 + 1] - ay
        const abz =
          geometry.positions[b * 3 + 2] - az

        const acx =
          geometry.positions[c * 3] - ax
        const acy =
          geometry.positions[c * 3 + 1] - ay
        const acz =
          geometry.positions[c * 3 + 2] - az

        const nx =
          aby * acz - abz * acy
        const ny =
          abz * acx - abx * acz
        const nz =
          abx * acy - aby * acx

        const cx =
          (
            geometry.positions[a * 3] +
            geometry.positions[b * 3] +
            geometry.positions[c * 3]
          ) / 3

        const cy =
          (
            geometry.positions[a * 3 + 1] +
            geometry.positions[b * 3 + 1] +
            geometry.positions[c * 3 + 1]
          ) / 3

        const cz =
          (
            geometry.positions[a * 3 + 2] +
            geometry.positions[b * 3 + 2] +
            geometry.positions[c * 3 + 2]
          ) / 3

        expect(
          nx * cx +
          ny * cy +
          nz * cz,
        ).toBeGreaterThan(0)
      }
    }
  })
})
