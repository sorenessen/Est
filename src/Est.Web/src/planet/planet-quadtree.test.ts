import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  patchBounds,
  patchesAtLevel,
  subdividePatch,
  type PlanetPatch,
} from './planet-quadtree'

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
