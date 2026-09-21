import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  resolveFaunaPresentationRotationY,
} from './fauna-presentation-contract'


describe(
  'fauna presentation contract',
  () => {
    it(
      'maps authoritative local heading onto Babylon Y rotation',
      () => {
        expect(
          resolveFaunaPresentationRotationY(
            0,
          ),
        ).toBe(
          -0,
        )

        expect(
          resolveFaunaPresentationRotationY(
            Math.PI / 2,
          ),
        ).toBe(
          -Math.PI / 2,
        )

        expect(
          resolveFaunaPresentationRotationY(
            -Math.PI / 2,
          ),
        ).toBe(
          Math.PI / 2,
        )
      },
    )

    it(
      'rejects non-finite heading',
      () => {
        expect(
          () =>
            resolveFaunaPresentationRotationY(
              Number.NaN,
            ),
        ).toThrow(
          'Fauna presentation heading must be finite.',
        )
      },
    )
  },
)
