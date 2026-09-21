import type {
  AnimalResponse,
  PopulationPersonResponse,
} from '../api/est-api'

import {
  geographicToLocalMeters,
  type GeographicCoordinate,
  type LocalPlayCoordinate,
} from './local-play-space'

export interface LocalPersonActorProjection {
  kind: 'person'
  actorKey: string
  personId: string
  source: PopulationPersonResponse
  local: LocalPlayCoordinate
  distanceMeters: number
}

export interface LocalAnimalActorProjection {
  kind: 'animal'
  actorKey: string
  animalId: string
  source: AnimalResponse
  local: LocalPlayCoordinate
  distanceMeters: number
}

export interface LocalIndividualActorProjection {
  people: LocalPersonActorProjection[]
  animals: LocalAnimalActorProjection[]
}

export interface LocalIndividualActorProjectionInput {
  planetId: string
  origin: GeographicCoordinate
  planetRadiusMeters: number
  maximumDistanceMeters: number
  population: readonly PopulationPersonResponse[]
  animals: readonly AnimalResponse[]
}

function validateMaximumDistance(
  maximumDistanceMeters: number,
): void {
  if (
    !Number.isFinite(
      maximumDistanceMeters,
    ) ||
    maximumDistanceMeters < 0
  ) {
    throw new RangeError(
      'Local actor maximum distance must be finite and non-negative.',
    )
  }
}

function distanceFromOrigin(
  local: LocalPlayCoordinate,
): number {
  return Math.hypot(
    local.eastMeters,
    local.northMeters,
  )
}

/**
 * Projects individually authoritative actors into Ester's local metre-space.
 *
 * This function does not create simulation identities, infer authority from
 * presentation, or decide how an actor is rendered. It only selects nearby
 * authoritative individuals and supplies their renderer-neutral local
 * coordinates.
 */
export function projectLocalIndividualActors(
  input: LocalIndividualActorProjectionInput,
): LocalIndividualActorProjection {
  validateMaximumDistance(
    input.maximumDistanceMeters,
  )

  const people =
    input.population
      .filter(
        person =>
          person.planetId ===
          input.planetId,
      )
      .map(
        person => {
          const local =
            geographicToLocalMeters(
              person,
              input.origin,
              input.planetRadiusMeters,
            )

          return {
            kind: 'person' as const,
            actorKey:
              `person:${person.personId}`,
            personId:
              person.personId,
            source:
              person,
            local,
            distanceMeters:
              distanceFromOrigin(
                local,
              ),
          }
        },
      )
      .filter(
        projection =>
          projection.distanceMeters <=
          input.maximumDistanceMeters,
      )
      .sort(
        (left, right) =>
          left.actorKey.localeCompare(
            right.actorKey,
          ),
      )

  const animals =
    input.animals
      .filter(
        animal =>
          animal.planetId ===
          input.planetId,
      )
      .map(
        animal => {
          const local =
            geographicToLocalMeters(
              animal,
              input.origin,
              input.planetRadiusMeters,
            )

          return {
            kind: 'animal' as const,
            actorKey:
              `animal:${animal.animalId}`,
            animalId:
              animal.animalId,
            source:
              animal,
            local,
            distanceMeters:
              distanceFromOrigin(
                local,
              ),
          }
        },
      )
      .filter(
        projection =>
          projection.distanceMeters <=
          input.maximumDistanceMeters,
      )
      .sort(
        (left, right) =>
          left.actorKey.localeCompare(
            right.actorKey,
          ),
      )

  return {
    people,
    animals,
  }
}
