import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  createTerrainDetailMapPixels,
} from './terrain-detail-map'

describe(
  'createTerrainDetailMapPixels',
  () => {
    it('is deterministic', () => {
      const first =
        createTerrainDetailMapPixels(
          32,
          16,
          'planet-a',
        )

      const second =
        createTerrainDetailMapPixels(
          32,
          16,
          'planet-a',
        )

      expect(
        Array.from(
          second,
        ),
      ).toEqual(
        Array.from(
          first,
        ),
      )
    })

    it('varies by identity', () => {
      const first =
        createTerrainDetailMapPixels(
          32,
          16,
          'planet-a',
        )

      const second =
        createTerrainDetailMapPixels(
          32,
          16,
          'planet-b',
        )

      expect(
        Array.from(
          second,
        ),
      ).not.toEqual(
        Array.from(
          first,
        ),
      )
    })

    it('contains diffuse and normal variation at close-range density', () => {
      const pixels =
        createTerrainDetailMapPixels(
          128,
          8,
          'planet-a',
        )

      const red =
        new Set<number>()

      const green =
        new Set<number>()

      const alpha =
        new Set<number>()

      const blue =
        new Set<number>()

      for (
        let index = 0;
        index < pixels.length;
        index += 4
      ) {
        red.add(
          pixels[
            index
          ],
        )

        green.add(
          pixels[
            index + 1
          ],
        )

        blue.add(
          pixels[
            index + 2
          ],
        )

        alpha.add(
          pixels[
            index + 3
          ],
        )
      }

      expect(
        red.size,
      ).toBeGreaterThan(
        8,
      )

      expect(
        green.size,
      ).toBeGreaterThan(
        8,
      )

      expect(
        alpha.size,
      ).toBeGreaterThan(
        8,
      )

      expect(
        blue,
      ).toEqual(
        new Set([
          128,
        ]),
      )
    })

    it('rejects invalid input', () => {
      expect(
        () =>
          createTerrainDetailMapPixels(
            4,
            16,
            'planet',
          ),
      ).toThrow()

      expect(
        () =>
          createTerrainDetailMapPixels(
            32,
            0,
            'planet',
          ),
      ).toThrow()

      expect(
        () =>
          createTerrainDetailMapPixels(
            32,
            16,
            '',
          ),
      ).toThrow()
    })
  },
)
