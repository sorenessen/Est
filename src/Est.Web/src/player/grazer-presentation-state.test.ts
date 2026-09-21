import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  resolveGrazerPresentationState,
} from './grazer-presentation-state'

describe(
  'resolveGrazerPresentationState',
  () => {
    it(
      'keeps an unchanged representative stationary',
      () => {
        expect(
          resolveGrazerPresentationState(
            false,
          ),
        ).toEqual({
          locomotion:
            'stationary',
        })
      },
    )

    it(
      'treats observed representative displacement as presentation locomotion',
      () => {
        expect(
          resolveGrazerPresentationState(
            true,
          ),
        ).toEqual({
          locomotion:
            'moving',
        })
      },
    )
  },
)
