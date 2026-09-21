import {
  describe,
  expect,
  it,
} from 'vitest'

import type {
  AnimalResponse,
  PopulationPersonResponse,
} from '../api/est-api'

import {
  projectLocalIndividualActors,
} from './local-actor-projection'

import {
  moveSurfaceCoordinate,
} from './surface-movement'

const earthRadiusMeters =
  6_371_000

const planetId =
  'planet-a'

function createPerson(
  personId: string,
  latitudeDegrees: number,
  longitudeDegrees: number,
  personPlanetId = planetId,
): PopulationPersonResponse {
  return {
    personId,
    planetId:
      personPlanetId,
    sex:
      'Female',
    birthTimeSeconds:
      0,
    latitudeDegrees,
    longitudeDegrees,
    parentId:
      null,
    isPregnant:
      false,
    pregnancyConceptionTimeSeconds:
      null,
    pregnancyFatherId:
      null,
    activity:
      'Idle',
    energyReserve:
      1,
    health:
      1,
  }
}

function createAnimal(
  animalId: string,
  latitudeDegrees: number,
  longitudeDegrees: number,
  animalPlanetId = planetId,
): AnimalResponse {
  return {
    animalId,
    planetId:
      animalPlanetId,
    species:
      'Wolf',
    birthTimeSeconds:
      0,
    parentId:
      null,
    sex:
      'Female',
    isPregnant:
      false,
    pregnancyConceptionTimeSeconds:
      null,
    pregnancyFatherId:
      null,
    latitudeDegrees,
    longitudeDegrees,
    energyReserve:
      1,
    health:
      1,
    activity:
      'Idle',
    material: {
      liveBiomassKilograms:
        40,
      liveNitrogenKilograms:
        1,
    },
  }
}

describe(
  'projectLocalIndividualActors',
  () => {
    it(
      'projects nearby authoritative people and animals into local metres',
      () => {
        const origin = {
          latitudeDegrees:
            47,
          longitudeDegrees:
            -122,
        }

        const personCoordinate =
          moveSurfaceCoordinate(
            origin,
            10,
            0,
            earthRadiusMeters,
          )

        const animalCoordinate =
          moveSurfaceCoordinate(
            origin,
            0,
            15,
            earthRadiusMeters,
          )

        const person =
          createPerson(
            'person-a',
            personCoordinate.latitudeDegrees,
            personCoordinate.longitudeDegrees,
          )

        const animal =
          createAnimal(
            'animal-a',
            animalCoordinate.latitudeDegrees,
            animalCoordinate.longitudeDegrees,
          )

        const projection =
          projectLocalIndividualActors({
            planetId,
            origin,
            planetRadiusMeters:
              earthRadiusMeters,
            maximumDistanceMeters:
              20,
            population: [
              person,
            ],
            animals: [
              animal,
            ],
          })

        expect(
          projection.people,
        ).toHaveLength(
          1,
        )

        expect(
          projection.animals,
        ).toHaveLength(
          1,
        )

        expect(
          projection.people[0]
            .actorKey,
        ).toBe(
          'person:person-a',
        )

        expect(
          projection.people[0]
            .source,
        ).toBe(
          person,
        )

        expect(
          projection.people[0]
            .local.northMeters,
        ).toBeCloseTo(
          10,
          3,
        )

        expect(
          projection.people[0]
            .local.eastMeters,
        ).toBeCloseTo(
          0,
          3,
        )

        expect(
          projection.animals[0]
            .actorKey,
        ).toBe(
          'animal:animal-a',
        )

        expect(
          projection.animals[0]
            .source,
        ).toBe(
          animal,
        )

        expect(
          projection.animals[0]
            .local.eastMeters,
        ).toBeCloseTo(
          15,
          3,
        )

        expect(
          projection.animals[0]
            .local.northMeters,
        ).toBeCloseTo(
          0,
          3,
        )
      },
    )

    it(
      'excludes authoritative individuals on another planet',
      () => {
        const origin = {
          latitudeDegrees:
            47,
          longitudeDegrees:
            -122,
        }

        const projection =
          projectLocalIndividualActors({
            planetId,
            origin,
            planetRadiusMeters:
              earthRadiusMeters,
            maximumDistanceMeters:
              100,
            population: [
              createPerson(
                'person-a',
                47,
                -122,
                'planet-b',
              ),
            ],
            animals: [
              createAnimal(
                'animal-a',
                47,
                -122,
                'planet-b',
              ),
            ],
          })

        expect(
          projection,
        ).toEqual({
          people: [],
          animals: [],
        })
      },
    )

    it(
      'excludes individuals outside the requested projection radius',
      () => {
        const origin = {
          latitudeDegrees:
            47,
          longitudeDegrees:
            -122,
        }

        const near =
          moveSurfaceCoordinate(
            origin,
            25,
            0,
            earthRadiusMeters,
          )

        const far =
          moveSurfaceCoordinate(
            origin,
            125,
            0,
            earthRadiusMeters,
          )

        const projection =
          projectLocalIndividualActors({
            planetId,
            origin,
            planetRadiusMeters:
              earthRadiusMeters,
            maximumDistanceMeters:
              100,
            population: [
              createPerson(
                'near',
                near.latitudeDegrees,
                near.longitudeDegrees,
              ),
              createPerson(
                'far',
                far.latitudeDegrees,
                far.longitudeDegrees,
              ),
            ],
            animals: [],
          })

        expect(
          projection.people.map(
            person =>
              person.personId,
          ),
        ).toEqual([
          'near',
        ])
      },
    )

    it(
      'returns stable actor ordering independent of authoritative collection order',
      () => {
        const origin = {
          latitudeDegrees:
            47,
          longitudeDegrees:
            -122,
        }

        const first =
          projectLocalIndividualActors({
            planetId,
            origin,
            planetRadiusMeters:
              earthRadiusMeters,
            maximumDistanceMeters:
              100,
            population: [
              createPerson(
                'person-b',
                47,
                -122,
              ),
              createPerson(
                'person-a',
                47,
                -122,
              ),
            ],
            animals: [
              createAnimal(
                'animal-b',
                47,
                -122,
              ),
              createAnimal(
                'animal-a',
                47,
                -122,
              ),
            ],
          })

        const second =
          projectLocalIndividualActors({
            planetId,
            origin,
            planetRadiusMeters:
              earthRadiusMeters,
            maximumDistanceMeters:
              100,
            population: [
              createPerson(
                'person-a',
                47,
                -122,
              ),
              createPerson(
                'person-b',
                47,
                -122,
              ),
            ],
            animals: [
              createAnimal(
                'animal-a',
                47,
                -122,
              ),
              createAnimal(
                'animal-b',
                47,
                -122,
              ),
            ],
          })

        expect(
          first.people.map(
            person =>
              person.actorKey,
          ),
        ).toEqual([
          'person:person-a',
          'person:person-b',
        ])

        expect(
          first.animals.map(
            animal =>
              animal.actorKey,
          ),
        ).toEqual([
          'animal:animal-a',
          'animal:animal-b',
        ])

        expect(
          first.people.map(
            person =>
              person.actorKey,
          ),
        ).toEqual(
          second.people.map(
            person =>
              person.actorKey,
          ),
        )

        expect(
          first.animals.map(
            animal =>
              animal.actorKey,
          ),
        ).toEqual(
          second.animals.map(
            animal =>
              animal.actorKey,
          ),
        )
      },
    )

    it(
      'rejects an invalid maximum projection distance',
      () => {
        expect(
          () =>
            projectLocalIndividualActors({
              planetId,
              origin: {
                latitudeDegrees:
                  47,
                longitudeDegrees:
                  -122,
              },
              planetRadiusMeters:
                earthRadiusMeters,
              maximumDistanceMeters:
                -1,
              population: [],
              animals: [],
            }),
        ).toThrow(
          'Local actor maximum distance must be finite and non-negative.',
        )
      },
    )
  },
)
