import type {
  PersonEncounterResponse,
} from '../api/est-api'

const shortPersonId = (
  personId: string,
): string =>
  personId.slice(
    0,
    8,
  )

export const formatPersonEncounterStatus = (
  encounter: PersonEncounterResponse,
): string => {
  const personLabel =
    `person ${shortPersonId(
      encounter.personId,
    )}`

  if (
    encounter.recognizedBeforeEncounter
  ) {
    return (
      `Recognized on return · ${personLabel}` +
      ` · encounter ${encounter.encounterCountAfter}`
    )
  }

  return (
    `First encounter · ${personLabel}`
  )
}
