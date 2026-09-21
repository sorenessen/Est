import type {
  GrazerCohortResponse,
  GrazerCohortsResponse,
  VegetationResponse,
} from '../api/est-api'

import {
  geographicToLocalMeters,
  type GeographicCoordinate,
  type LocalPlayCoordinate,
} from './local-play-space'

import {
  moveSurfaceCoordinate,
} from './surface-movement'

export interface LocalGrazerRepresentativeProjection {
  kind: 'grazer'
  actorKey: string
  cohortId: string
  representativeIndex: number
  source: GrazerCohortResponse
  coordinate: GeographicCoordinate
  local: LocalPlayCoordinate
  distanceMeters: number
  cohortCellLiveBiomassKilogramsPerSquareMeter:
    number | null
}

export interface LocalGrazerProjectionInput {
  planetId: string
  origin: GeographicCoordinate
  planetRadiusMeters: number
  maximumDistanceMeters: number
  presentationSpreadRadiusMeters: number
  maximumRepresentativesPerCohort: number
  grazerCohorts: GrazerCohortsResponse | null
  vegetation: VegetationResponse | null
}

function validateNonNegativeFinite(
  value: number,
  name: string,
): void {
  if (
    !Number.isFinite(
      value,
    ) ||
    value < 0
  ) {
    throw new RangeError(
      `${name} must be finite and non-negative.`,
    )
  }
}

function validateMaximumRepresentatives(
  value: number,
): void {
  if (
    !Number.isInteger(
      value,
    ) ||
    value < 0
  ) {
    throw new RangeError(
      'Maximum grazer representatives per cohort must be a non-negative integer.',
    )
  }
}

function hashIdentity(
  identity: string,
): number {
  let hash =
    0x811c9dc5

  for (
    let index = 0;
    index < identity.length;
    index += 1
  ) {
    hash ^=
      identity.charCodeAt(
        index,
      )

    hash =
      Math.imul(
        hash,
        0x01000193,
      )
  }

  return hash >>> 0
}

function unitValue(
  identity: string,
): number {
  return (
    hashIdentity(
      identity,
    ) /
    0xffffffff
  )
}

function createRepresentativeCoordinate(
  cohort: GrazerCohortResponse,
  representativeIndex: number,
  presentationSpreadRadiusMeters: number,
  planetRadiusMeters: number,
): GeographicCoordinate {
  if (
    presentationSpreadRadiusMeters ===
    0
  ) {
    return {
      latitudeDegrees:
        cohort.latitudeDegrees,
      longitudeDegrees:
        cohort.longitudeDegrees,
    }
  }

  const angleRadians =
    unitValue(
      `${cohort.cohortId}:${representativeIndex}:angle`,
    ) *
    Math.PI *
    2

  const radialFraction =
    Math.sqrt(
      unitValue(
        `${cohort.cohortId}:${representativeIndex}:radius`,
      ),
    )

  const radiusMeters =
    presentationSpreadRadiusMeters *
    radialFraction

  return moveSurfaceCoordinate(
    cohort,
    Math.cos(
      angleRadians,
    ) *
      radiusMeters,
    Math.sin(
      angleRadians,
    ) *
      radiusMeters,
    planetRadiusMeters,
  )
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
 * Refines authoritative aggregate grazer cohorts into a bounded local
 * presentation sample.
 *
 * Representatives are not authoritative AnimalId-bearing individuals.
 * Their keys are presentation identity derived from cohort identity and a
 * deterministic slot. The cohort coordinate remains macro authority; the
 * bounded spread only prevents the aggregate from being rendered as every
 * member occupying one exact walking-scale point.
 *
 * Current vegetation data is attached from the cohort's authoritative
 * surface cell. It is not yet used as a sub-cell placement field because the
 * current simulation does not expose sub-cell ecological distribution.
 */
export function projectLocalGrazers(
  input: LocalGrazerProjectionInput,
): LocalGrazerRepresentativeProjection[] {
  validateNonNegativeFinite(
    input.maximumDistanceMeters,
    'Local grazer maximum distance',
  )

  validateNonNegativeFinite(
    input.presentationSpreadRadiusMeters,
    'Local grazer presentation spread radius',
  )

  validateMaximumRepresentatives(
    input.maximumRepresentativesPerCohort,
  )

  if (
    input.grazerCohorts === null ||
    input.maximumRepresentativesPerCohort ===
      0
  ) {
    return []
  }

  if (
    input.grazerCohorts.planetId !==
    input.planetId
  ) {
    throw new Error(
      'Grazer cohorts do not match the local projection planet.',
    )
  }

  if (
    input.vegetation !== null &&
    input.vegetation.planetId !==
      input.planetId
  ) {
    throw new Error(
      'Vegetation does not match the local projection planet.',
    )
  }

  const vegetationByCellId =
    new Map(
      input.vegetation
        ?.cells
        .map(
          cell => [
            cell.surfaceCellId,
            cell.liveBiomassKilogramsPerSquareMeter,
          ] as const,
        ) ??
        [],
    )

  const projections:
    LocalGrazerRepresentativeProjection[] =
      []

  for (
    const cohort of
    input.grazerCohorts.cohorts
  ) {
    const representativeCount =
      Math.min(
        Math.max(
          0,
          cohort.memberCount,
        ),
        input.maximumRepresentativesPerCohort,
      )

    const cohortCellLiveBiomass =
      vegetationByCellId.get(
        cohort.surfaceCellId,
      )

    for (
      let representativeIndex = 0;
      representativeIndex <
      representativeCount;
      representativeIndex += 1
    ) {
      const coordinate =
        createRepresentativeCoordinate(
          cohort,
          representativeIndex,
          input.presentationSpreadRadiusMeters,
          input.planetRadiusMeters,
        )

      const local =
        geographicToLocalMeters(
          coordinate,
          input.origin,
          input.planetRadiusMeters,
        )

      const distanceMeters =
        distanceFromOrigin(
          local,
        )

      if (
        distanceMeters >
        input.maximumDistanceMeters
      ) {
        continue
      }

      projections.push({
        kind:
          'grazer',
        actorKey:
          `grazer:${cohort.cohortId}:${representativeIndex}`,
        cohortId:
          cohort.cohortId,
        representativeIndex,
        source:
          cohort,
        coordinate,
        local,
        distanceMeters,
        cohortCellLiveBiomassKilogramsPerSquareMeter:
          cohortCellLiveBiomass ??
          null,
      })
    }
  }

  return projections.sort(
    (left, right) =>
      left.actorKey.localeCompare(
        right.actorKey,
      ),
  )
}
