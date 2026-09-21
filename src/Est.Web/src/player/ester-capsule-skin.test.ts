import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  esterCapsuleSkinDefinitions,
  resolveEsterCapsuleSkin,
} from './ester-capsule-skin'

describe(
  'Ester capsule presentation skin',
  () => {
    it('defaults to the Est-owned Ester capsule', () => {
      expect(
        resolveEsterCapsuleSkin(
          '?play=1',
        ),
      ).toBe(
        'ester',
      )
    })

    it('selects the Tylenol concept skin explicitly', () => {
      expect(
        resolveEsterCapsuleSkin(
          '?play=1&esterSkin=tylenol',
        ),
      ).toBe(
        'tylenol',
      )
    })

    it('falls back to Ester for unknown skins', () => {
      expect(
        resolveEsterCapsuleSkin(
          '?esterSkin=unknown',
        ),
      ).toBe(
        'ester',
      )
    })

    it('keeps Ester and Tylenol as distinct disposable presentations', () => {
      expect(
        esterCapsuleSkinDefinitions
          .ester
          .primaryText,
      ).toBe(
        'ESTER',
      )

      expect(
        esterCapsuleSkinDefinitions
          .tylenol
          .primaryText,
      ).toBe(
        'TYLENOL',
      )

      expect(
        esterCapsuleSkinDefinitions
          .ester
          .lowerColor,
      ).not.toBe(
        esterCapsuleSkinDefinitions
          .tylenol
          .lowerColor,
      )
    })
  },
)
