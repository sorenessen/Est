export interface SurfaceCoordinate {
  latitudeDegrees: number
  longitudeDegrees: number
}

function wrapLongitude(
  longitudeDegrees: number,
): number {
  let wrapped =
    longitudeDegrees

  while (wrapped > 180) {
    wrapped -= 360
  }

  while (wrapped < -180) {
    wrapped += 360
  }

  return wrapped
}

export function moveSurfaceCoordinate(
  coordinate: SurfaceCoordinate,
  northMeters: number,
  eastMeters: number,
  planetRadiusMeters: number,
): SurfaceCoordinate {
  if (
    !Number.isFinite(
      planetRadiusMeters,
    ) ||
    planetRadiusMeters <= 0
  ) {
    throw new Error(
      'Planet radius must be finite and positive.',
    )
  }

  const radiansToDegrees =
    180 / Math.PI

  const latitudeDeltaDegrees =
    northMeters /
    planetRadiusMeters *
    radiansToDegrees

  const latitudeDegrees =
    Math.max(
      -89.999999,
      Math.min(
        89.999999,
        coordinate.latitudeDegrees +
          latitudeDeltaDegrees,
      ),
    )

  const latitudeRadians =
    latitudeDegrees *
    Math.PI /
    180

  const longitudeScale =
    Math.max(
      0.000001,
      Math.cos(
        latitudeRadians,
      ),
    )

  const longitudeDeltaDegrees =
    eastMeters /
    (
      planetRadiusMeters *
      longitudeScale
    ) *
    radiansToDegrees

  return {
    latitudeDegrees,
    longitudeDegrees:
      wrapLongitude(
        coordinate.longitudeDegrees +
          longitudeDeltaDegrees,
      ),
  }
}
