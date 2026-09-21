import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  formatPersonEncounterStatus,
} from './person-encounter-presentation'

describe(
  'formatPersonEncounterStatus',
  () => {
    it(
      'describes a first encounter without claiming prior recognition',
      () => {
        expect(
          formatPersonEncounterStatus({
            personId:
              '12345678-aaaa-bbbb-cccc-123456789abc',
            recognizedBeforeEncounter:
              false,
            encounterCountBefore:
              0,
            encounterCountAfter:
              1,
          }),
        ).toBe(
          'First encounter · person 12345678',
        )
      },
    )

    it(
      'describes authoritative recognition on return',
      () => {
        expect(
          formatPersonEncounterStatus({
            personId:
              '87654321-aaaa-bbbb-cccc-123456789abc',
            recognizedBeforeEncounter:
              true,
            encounterCountBefore:
              1,
            encounterCountAfter:
              2,
          }),
        ).toBe(
          'Recognized on return · person 87654321 · encounter 2',
        )
      },
    )
  },
)
