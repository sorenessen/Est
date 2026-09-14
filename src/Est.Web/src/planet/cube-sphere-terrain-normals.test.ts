import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  CUBE_FACES,
  createCubeSpherePatchGeometry,
  type CubeSphereRadialOffset,
  type CubeSphereSurfaceNormal,
} from './cube-sphere'

function positionKey(
  x: number,
  y: number,
  z: number,
): string {
  return [
    x.toFixed(10),
    y.toFixed(10),
    z.toFixed(10),
  ].join(':')
}

describe(
  'cube-sphere terrain normals',
  () => {
    const radialOffset:
      CubeSphereRadialOffset =
      direction =>
        0.003 *
        (
          0.7 * direction.x -
          0.4 * direction.y +
          0.2 * direction.z
        )

    const surfaceNormal:
      CubeSphereSurfaceNormal =
      direction => ({
        x:
          direction.x - 0.02,
        y:
          direction.y + 0.01,
        z:
          direction.z,
      })

    it(
      'uses a supplied displaced-surface normal',
      () => {
        const geometry =
          createCubeSpherePatchGeometry(
            'positiveZ',
            4,
            -1,
            1,
            -1,
            1,
            {},
            radialOffset,
            surfaceNormal,
          )

        const center =
          (2 * 5 + 2) * 3

        expect(
          geometry.normals[center],
        ).toBeLessThan(0)

        expect(
          geometry.normals[
            center + 1
          ],
        ).toBeGreaterThan(0)

        expect(
          geometry.normals[
            center + 2
          ],
        ).toBeGreaterThan(0.99)
      },
    )

    it(
      'produces identical supplied normals across cube-face boundaries',
      () => {
        const normalsByPosition =
          new Map<
            string,
            Array<
              readonly [
                number,
                number,
                number,
              ]
            >
          >()

        const segments = 8
        const stride =
          segments + 1

        for (const face of CUBE_FACES) {
          const geometry =
            createCubeSpherePatchGeometry(
              face,
              segments,
              -1,
              1,
              -1,
              1,
              {},
              radialOffset,
              surfaceNormal,
            )

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

              const offset =
                (
                  row * stride +
                  column
                ) * 3

              const key =
                positionKey(
                  geometry.positions[
                    offset
                  ],
                  geometry.positions[
                    offset + 1
                  ],
                  geometry.positions[
                    offset + 2
                  ],
                )

              const normals =
                normalsByPosition.get(
                  key,
                ) ?? []

              normals.push([
                geometry.normals[
                  offset
                ],
                geometry.normals[
                  offset + 1
                ],
                geometry.normals[
                  offset + 2
                ],
              ])

              normalsByPosition.set(
                key,
                normals,
              )
            }
          }
        }

        let shared = 0

        for (
          const normals of
            normalsByPosition.values()
        ) {
          if (normals.length < 2) {
            continue
          }

          shared += 1

          for (
            const normal of
              normals.slice(1)
          ) {
            expect(normal[0])
              .toBeCloseTo(
                normals[0][0],
                10,
              )

            expect(normal[1])
              .toBeCloseTo(
                normals[0][1],
                10,
              )

            expect(normal[2])
              .toBeCloseTo(
                normals[0][2],
                10,
              )
          }
        }

        expect(shared)
          .toBeGreaterThan(0)
      },
    )

    it(
      'keeps supplied normals independent of patch resolution',
      () => {
        const coarse =
          createCubeSpherePatchGeometry(
            'positiveZ',
            4,
            -1,
            1,
            -1,
            1,
            {},
            radialOffset,
            surfaceNormal,
          )

        const fine =
          createCubeSpherePatchGeometry(
            'positiveZ',
            8,
            -1,
            1,
            -1,
            1,
            {},
            radialOffset,
            surfaceNormal,
          )

        const coarseCenter =
          (2 * 5 + 2) * 3

        const fineCenter =
          (4 * 9 + 4) * 3

        for (
          let component = 0;
          component < 3;
          component += 1
        ) {
          expect(
            coarse.normals[
              coarseCenter +
              component
            ],
          ).toBeCloseTo(
            fine.normals[
              fineCenter +
              component
            ],
            12,
          )
        }
      },
    )
  },
)
