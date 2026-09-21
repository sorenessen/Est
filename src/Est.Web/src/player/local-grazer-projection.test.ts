import {
  describe,
  expect,
  it,
} from 'vitest'

import type {
  GrazerCohortResponse,
  GrazerCohortsResponse,
  VegetationResponse,
} from '../api/est-api'

import {
  projectLocalGrazers,
} from './local-grazer-projection'

const earthRadiusMeters =
  6_371_000

const planetId =
  'planet-a'

const origin = {
  latitudeDegrees:
    47,
  longitudeDegrees:
    -122,
}

function createCohort(
  cohortId: string,
  memberCount: number,
  surfaceCellId = 'cell-a',
): GrazerCohortResponse {
  return {
    cohortId,
    memberCount,
    surfaceCellId,
    latitudeDegrees:
      origin.latitudeDegrees,
    longitudeDegrees:
      origin.longitudeDegrees,
    material: {
      liveBiomassKilograms:
        memberCount *
        250,
      liveNitrogenKilograms:
        memberCount *
        6.25,
    },
  }
}

function createGrazerResponse(
  cohorts: GrazerCohortResponse[],
  responsePlanetId = planetId,
): GrazerCohortsResponse {
  return {
    planetId:
      responsePlanetId,
    cohorts,
  }
}

function createVegetation(
  responsePlanetId = planetId,
): VegetationResponse {
  return {
    planetId:
      responsePlanetId,
    grid: {
      kind:
        'LatitudeLongitude',
      identityVersion:
        1,
      latitudeBandCount:
        72,
      longitudeBandCount:
        144,
    },
    cells: [
      {
        surfaceCellId:
          'cell-a',
        liveBiomassKilogramsPerSquareMeter:
          1.75,
      },
      {
        surfaceCellId:
          'cell-b',
        liveBiomassKilogramsPerSquareMeter:
          0.25,
      },
    ],
  }
}

function project(
  grazerCohorts: GrazerCohortsResponse,
  overrides: Partial<{
    maximumDistanceMeters: number
    presentationSpreadRadiusMeters: number
    maximumRepresentativesPerCohort: number
    vegetation: VegetationResponse | null
  }> = {},
) {
  return projectLocalGrazers({
    planetId,
    origin,
    planetRadiusMeters:
      earthRadiusMeters,
    maximumDistanceMeters:
      overrides.maximumDistanceMeters ??
      500,
    presentationSpreadRadiusMeters:
      overrides.presentationSpreadRadiusMeters ??
      40,
    maximumRepresentativesPerCohort:
      overrides.maximumRepresentativesPerCohort ??
      8,
    grazerCohorts,
    vegetation:
      overrides.vegetation ===
      undefined
        ? createVegetation()
        : overrides.vegetation,
  })
}

describe(
  'projectLocalGrazers',
  () => {
    it(
      'creates a bounded deterministic sample without inventing authoritative animal identity',
      () => {
        const cohort =
          createCohort(
            'cohort-a',
            30,
          )

        const first =
          project(
            createGrazerResponse([
              cohort,
            ]),
          )

        const second =
          project(
            createGrazerResponse([
              cohort,
            ]),
          )

        expect(
          first,
        ).toHaveLength(
          8,
        )

        expect(
          second,
        ).toEqual(
          first,
        )

        expect(
          first.map(
            representative =>
              representative.actorKey,
          ),
        ).toEqual([
          'grazer:cohort-a:0',
          'grazer:cohort-a:1',
          'grazer:cohort-a:2',
          'grazer:cohort-a:3',
          'grazer:cohort-a:4',
          'grazer:cohort-a:5',
          'grazer:cohort-a:6',
          'grazer:cohort-a:7',
        ])

        expect(
          new Set(
            first.map(
              representative =>
                [
                  representative.coordinate
                    .latitudeDegrees
                    .toFixed(
                      9,
                    ),
                  representative.coordinate
                    .longitudeDegrees
                    .toFixed(
                      9,
                    ),
                ].join(
                  ':',
                ),
            ),
          ).size,
        ).toBeGreaterThan(
          1,
        )

        expect(
          first.every(
            representative =>
              Math.hypot(
                representative.local.eastMeters,
                representative.local.northMeters,
              ) <=
              40.1,
          ),
        ).toBe(true)
      },
    )

    it(
      'never realizes more representatives than authoritative cohort members',
      () => {
        const projected =
          project(
            createGrazerResponse([
              createCohort(
                'cohort-small',
                3,
              ),
            ]),
            {
              maximumRepresentativesPerCohort:
                20,
            },
          )

        expect(
          projected,
        ).toHaveLength(
          3,
        )
      },
    )

    it(
      'keeps existing representative slots stable when authoritative member count changes',
      () => {
        const before =
          project(
            createGrazerResponse([
              createCohort(
                'cohort-a',
                3,
              ),
            ]),
            {
              maximumRepresentativesPerCohort:
                10,
            },
          )

        const after =
          project(
            createGrazerResponse([
              createCohort(
                'cohort-a',
                6,
              ),
            ]),
            {
              maximumRepresentativesPerCohort:
                10,
            },
          )

        expect(
          after
            .slice(
              0,
              3,
            )
            .map(
              representative => ({
                actorKey:
                  representative.actorKey,
                coordinate:
                  representative.coordinate,
              }),
            ),
        ).toEqual(
          before.map(
            representative => ({
              actorKey:
                representative.actorKey,
              coordinate:
                representative.coordinate,
            }),
          ),
        )
      },
    )

    it(
      'filters realized representatives by the requested local projection radius',
      () => {
        const projected =
          project(
            createGrazerResponse([
              createCohort(
                'cohort-a',
                30,
              ),
            ]),
            {
              maximumDistanceMeters:
                5,
              presentationSpreadRadiusMeters:
                40,
              maximumRepresentativesPerCohort:
                30,
            },
          )

        expect(
          projected.length,
        ).toBeLessThan(
          30,
        )

        expect(
          projected.every(
            representative =>
              representative.distanceMeters <=
              5,
          ),
        ).toBe(true)
      },
    )

    it(
      'carries habitat support from the authoritative current surface cell',
      () => {
        const projected =
          project(
            createGrazerResponse([
              createCohort(
                'cohort-a',
                2,
                'cell-b',
              ),
            ]),
          )

        expect(
          projected.map(
            representative =>
              representative
                .cohortCellLiveBiomassKilogramsPerSquareMeter,
          ),
        ).toEqual([
          0.25,
          0.25,
        ])
      },
    )

    it(
      'rejects vegetation that omits the cohort authoritative surface cell',
      () => {
        const vegetation = {
          ...createVegetation(),
          cells:
            createVegetation()
              .cells
              .filter(
                cell =>
                  cell.surfaceCellId ===
                  'cell-a',
              ),
        }

        expect(
          () =>
            project(
              createGrazerResponse([
                createCohort(
                  'cohort-a',
                  2,
                  'cell-b',
                ),
              ]),
              {
                vegetation,
              },
            ),
        ).toThrow(
          'Vegetation does not contain the grazer cohort surface cell.',
        )
      },
    )

    it(
      'returns stable ordering independent of cohort collection order',
      () => {
        const first =
          project(
            createGrazerResponse([
              createCohort(
                'cohort-b',
                2,
              ),
              createCohort(
                'cohort-a',
                2,
              ),
            ]),
          )

        const second =
          project(
            createGrazerResponse([
              createCohort(
                'cohort-a',
                2,
              ),
              createCohort(
                'cohort-b',
                2,
              ),
            ]),
          )

        expect(
          first.map(
            representative =>
              representative.actorKey,
          ),
        ).toEqual(
          second.map(
            representative =>
              representative.actorKey,
          ),
        )

        expect(
          first.map(
            representative =>
              representative.actorKey,
          ),
        ).toEqual([
          'grazer:cohort-a:0',
          'grazer:cohort-a:1',
          'grazer:cohort-b:0',
          'grazer:cohort-b:1',
        ])
      },
    )

    it(
      'rejects planetary response mismatches and invalid bounds',
      () => {
        expect(
          () =>
            projectLocalGrazers({
              planetId,
              origin,
              planetRadiusMeters:
                earthRadiusMeters,
              maximumDistanceMeters:
                100,
              presentationSpreadRadiusMeters:
                20,
              maximumRepresentativesPerCohort:
                4,
              grazerCohorts:
                createGrazerResponse(
                  [],
                  'planet-b',
                ),
              vegetation:
                createVegetation(),
            }),
        ).toThrow(
          'Grazer cohorts do not match the local projection planet.',
        )

        expect(
          () =>
            projectLocalGrazers({
              planetId,
              origin,
              planetRadiusMeters:
                earthRadiusMeters,
              maximumDistanceMeters:
                100,
              presentationSpreadRadiusMeters:
                20,
              maximumRepresentativesPerCohort:
                4,
              grazerCohorts:
                createGrazerResponse(
                  [],
                ),
              vegetation:
                createVegetation(
                  'planet-b',
                ),
            }),
        ).toThrow(
          'Vegetation does not match the local projection planet.',
        )

        expect(
          () =>
            projectLocalGrazers({
              planetId,
              origin,
              planetRadiusMeters:
                earthRadiusMeters,
              maximumDistanceMeters:
                -1,
              presentationSpreadRadiusMeters:
                20,
              maximumRepresentativesPerCohort:
                4,
              grazerCohorts:
                createGrazerResponse(
                  [],
                ),
              vegetation:
                createVegetation(),
            }),
        ).toThrow(
          'Local grazer maximum distance must be finite and non-negative.',
        )

        expect(
          () =>
            projectLocalGrazers({
              planetId,
              origin,
              planetRadiusMeters:
                earthRadiusMeters,
              maximumDistanceMeters:
                100,
              presentationSpreadRadiusMeters:
                20,
              maximumRepresentativesPerCohort:
                1.5,
              grazerCohorts:
                createGrazerResponse(
                  [],
                ),
              vegetation:
                createVegetation(),
            }),
        ).toThrow(
          'Maximum grazer representatives per cohort must be a non-negative integer.',
        )
      },
    )
  },
)
