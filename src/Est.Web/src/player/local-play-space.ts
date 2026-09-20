export interface GeographicCoordinate {
  latitudeDegrees: number
  longitudeDegrees: number
}

export interface LocalPlayCoordinate {
  eastMeters: number
  northMeters: number
}

function wrappedLongitudeDeltaDegrees(
  longitudeDegrees: number,
  originLongitudeDegrees: number,
): number {
  let delta =
    longitudeDegrees -
    originLongitudeDegrees

  while (delta > 180) {
    delta -= 360
  }

  while (delta < -180) {
    delta += 360
  }

  return delta
}

export function geographicToLocalMeters(
  coordinate: GeographicCoordinate,
  origin: GeographicCoordinate,
  planetRadiusMeters: number,
): LocalPlayCoordinate {
  if (
    !Number.isFinite(planetRadiusMeters) ||
    planetRadiusMeters <= 0
  ) {
    throw new Error(
      'Planet radius must be a positive finite number.',
    )
  }

  const degreesToRadians =
    Math.PI / 180

  const latitudeDeltaRadians =
    (
      coordinate.latitudeDegrees -
      origin.latitudeDegrees
    ) *
    degreesToRadians

  const longitudeDeltaRadians =
    wrappedLongitudeDeltaDegrees(
      coordinate.longitudeDegrees,
      origin.longitudeDegrees,
    ) *
    degreesToRadians

  const originLatitudeRadians =
    origin.latitudeDegrees *
    degreesToRadians

  return {
    eastMeters:
      longitudeDeltaRadians *
      Math.cos(originLatitudeRadians) *
      planetRadiusMeters,
    northMeters:
      latitudeDeltaRadians *
      planetRadiusMeters,
  }
}
