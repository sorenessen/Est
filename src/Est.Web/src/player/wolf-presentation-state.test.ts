import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  resolveWolfPresentationState,
} from './wolf-presentation-state'


describe(
  'resolveWolfPresentationState',
  () => {
    it.each([
      [
        'Idle',
        'idle',
      ],
      [
        'Hunting',
        'hunting',
      ],
      [
        'Traveling',
        'traveling',
      ],
      [
        'Attacking',
        'attacking',
      ],
      [
        'Eating',
        'eating',
      ],
    ])(
      'preserves authoritative %s activity as presentation %s',
      (
        authoritativeActivity,
        expectedActivity,
      ) => {
        expect(
          resolveWolfPresentationState(
            authoritativeActivity,
            false,
          ),
        ).toEqual({
          activity:
            expectedActivity,
          locomotion:
            'stationary',
        })
      },
    )

    it(
      'keeps locomotion separate from authoritative Hunting activity',
      () => {
        expect(
          resolveWolfPresentationState(
            'Hunting',
            false,
          ),
        ).toEqual({
          activity:
            'hunting',
          locomotion:
            'stationary',
        })

        expect(
          resolveWolfPresentationState(
            'Hunting',
            true,
          ),
        ).toEqual({
          activity:
            'hunting',
          locomotion:
            'moving',
        })
      },
    )

    it(
      'marks observed authoritative displacement as moving without changing activity',
      () => {
        expect(
          resolveWolfPresentationState(
            'Traveling',
            true,
          ),
        ).toEqual({
          activity:
            'traveling',
          locomotion:
            'moving',
        })

        expect(
          resolveWolfPresentationState(
            'Eating',
            true,
          ),
        ).toEqual({
          activity:
            'eating',
          locomotion:
            'moving',
        })
      },
    )

    it(
      'accepts activity casing from transport boundaries without changing semantics',
      () => {
        expect(
          resolveWolfPresentationState(
            '  ATTACKING  ',
            false,
          ),
        ).toEqual({
          activity:
            'attacking',
          locomotion:
            'stationary',
        })
      },
    )

    it(
      'rejects an activity that is not present in authoritative wolf state',
      () => {
        expect(
          () =>
            resolveWolfPresentationState(
              'Sleeping',
              false,
            ),
        ).toThrow(
          'Unsupported authoritative wolf activity: Sleeping',
        )
      },
    )
  },
)
