import {
  ShaderMaterial,
  ShaderStore,
  Vector3,
  type Scene,
} from '@babylonjs/core'

import '@babylonjs/core/Shaders/ShadersInclude/instancesDeclaration.js'
import '@babylonjs/core/Shaders/ShadersInclude/instancesVertex.js'

export interface AnimatedGrassAnchorBasis {
  readonly anchorMeters: {
    readonly x: number
    readonly y: number
    readonly z: number
  }

  readonly east: {
    readonly x: number
    readonly y: number
    readonly z: number
  }

  readonly north: {
    readonly x: number
    readonly y: number
    readonly z: number
  }
}

export const animatedGrassVertexShader = `
precision highp float;

attribute vec3 position;
attribute vec3 normal;

// Per geographic grass patch.
attribute vec4 grassData;

// Per blade inside the patch.
// x = phase
// y = stiffness variation
// z = dryness variation
// w = species / height variation
attribute vec4 bladeData;

#include<instancesDeclaration>

uniform mat4 viewProjection;

uniform float uTimeSeconds;
uniform float uWindStrength;

uniform float uFadeStartMeters;
uniform float uFadeEndMeters;

uniform vec3 uPlanetAnchorMeters;
uniform vec3 uEastBasis;
uniform vec3 uNorthBasis;

varying vec3 vNormalW;
varying float vBladeHeight;
varying float vVariation;
varying float vDryness;
varying float vSeedHead;
varying float vWindExposure;
varying float vGrassReveal;

float grassHash(vec3 p) {
  p =
    fract(
      p *
      0.1031
    );

  p +=
    dot(
      p,
      p.yzx +
      33.33
    );

  return fract(
    (
      p.x +
      p.y
    ) *
    p.z
  );
}

void main(void) {
  #include<instancesVertex>

  float bladeHeight =
    clamp(
      position.y,
      0.0,
      1.35
    ) /
    1.35;

  float bendWeight =
    bladeHeight *
    bladeHeight;

  // Recover this blade's ground-plane offset from the instance basis so
  // neighboring blades within one patch do not share an identical wind
  // sample.
  vec2 bladeOffsetMeters =
    vec2(
      world0.x *
        position.x +
      world2.x *
        position.z,

      world0.z *
        position.x +
      world2.z *
        position.z
    );

  vec2 sampleLocalMeters =
    vec2(
      world3.x,
      world3.z
    ) +
    bladeOffsetMeters;

  vec3 planetPositionMeters =
    uPlanetAnchorMeters +
    uEastBasis *
      sampleLocalMeters.x +
    uNorthBasis *
      sampleLocalMeters.y;

  // The grass scatter buffer extends beyond the visible meadow.
  // Reveal is evaluated continuously against Ester at local origin,
  // not against the scatter-cell boundary.
  vec4 grassRootWorldPosition =
    finalWorld *
    vec4(
      0.0,
      0.0,
      0.0,
      1.0
    );

  float grassDistanceMeters =
    length(
      grassRootWorldPosition.xz
    );

  // Break the distance boundary up geographically so the edge does not
  // describe a visible circle around Ester. The frequencies are broad
  // enough to create meadow fingers and recesses rather than noisy
  // individual-blade flicker.
  float edgeNoise =
    sin(
      dot(
        planetPositionMeters,
        vec3(
          0.071,
          -0.039,
          0.052
        )
      )
    ) *
      0.54 +
    sin(
      dot(
        planetPositionMeters,
        vec3(
          -0.031,
          0.063,
          0.044
        )
      ) +
      1.73
    ) *
      0.29 +
    sin(
      dot(
        planetPositionMeters,
        vec3(
          0.018,
          0.027,
          -0.076
        )
      ) +
      4.21
    ) *
      0.17;

  // Short basal grass extends farther into the transition than tall
  // meadow stems. This prevents the far edge from becoming a wall where
  // every vegetation stratum stops at the same distance.
  float bladeStratum =
    clamp(
      bladeData.w,
      0.0,
      1.0
    );

  float basalExtensionMeters =
    mix(
      9.0,
      0.0,
      smoothstep(
        0.48,
        0.92,
        bladeStratum
      )
    );

  float noisyFadeStartMeters =
    uFadeStartMeters +
    edgeNoise *
      4.5;

  float noisyFadeEndMeters =
    uFadeEndMeters +
    edgeNoise *
      6.5 +
    basalExtensionMeters;

  float distanceVisibility =
    1.0 -
    smoothstep(
      noisyFadeStartMeters,
      noisyFadeEndMeters,
      grassDistanceMeters
    );

  // Every blade receives a stable reveal threshold. This converts the
  // distance transition into progressive density rather than allowing
  // a complete patch to appear on one frame.
  float revealThreshold =
    fract(
      bladeData.x *
        0.754877666 +
      bladeData.w *
        0.438289 +
      grassData.x *
        0.569840
    );

  float grassReveal =
    smoothstep(
      revealThreshold -
        0.26,
      revealThreshold +
        0.16,
      distanceVisibility
    );

  float worldVariation =
    grassHash(
      planetPositionMeters *
      0.31
    );

  float bladePhase =
    bladeData.x *
    6.28318530718;

  // The prevailing wind changes direction very slowly.
  float windAngle =
    0.43 +
    sin(
      uTimeSeconds *
      0.061
    ) *
    0.27;

  vec2 windDirection =
    normalize(
      vec2(
        cos(
          windAngle
        ),
        sin(
          windAngle
        )
      )
    );

  vec2 crossWind =
    vec2(
      -windDirection.y,
      windDirection.x
    );

  // Broad travelling gust fronts.
  float waveA =
    sin(
      dot(
        planetPositionMeters,
        vec3(
          0.0037,
          -0.0015,
          0.0026
        )
      ) -
      uTimeSeconds *
      0.91
    );

  float waveB =
    sin(
      dot(
        planetPositionMeters,
        vec3(
          -0.0020,
          0.0030,
          0.0041
        )
      ) -
      uTimeSeconds *
      0.63 +
      waveA *
      0.78
    );

  float waveC =
    sin(
      dot(
        planetPositionMeters,
        vec3(
          0.0054,
          0.0012,
          -0.0023
        )
      ) -
      uTimeSeconds *
      1.17 +
      waveB *
      0.42
    );

  float pressure =
    waveA *
      0.48 +
    waveB *
      0.34 +
    waveC *
      0.18;

  float gust =
    smoothstep(
      -0.42,
      0.70,
      pressure
    );

  // Fine blade motion. Phase differs within every micro-patch.
  float flutter =
    sin(
      uTimeSeconds *
        (
          3.3 +
          bladeData.w *
          1.4 +
          worldVariation *
          0.7
        ) +
      bladePhase +
      dot(
        planetPositionMeters,
        vec3(
          0.064,
          0.031,
          -0.052
        )
      ) +
      bladeHeight *
        2.7
    );

  float stiffness =
    clamp(
      grassData.z *
        0.72 +
      bladeData.y *
        0.28,
      0.0,
      1.0
    );

  float flexibility =
    mix(
      1.30,
      0.50,
      stiffness
    );

  // Short undergrass stays comparatively restrained while the taller
  // canopy and wild stems travel farther in the same gust.
  flexibility *=
    mix(
      0.70,
      1.16,
      bladeData.w
    );

  float instanceHeightMeters =
    max(
      length(
        finalWorld[1].xyz
      ),
      0.01
    );

  float mainBendMeters =
    instanceHeightMeters *
    bendWeight *
    (
      0.040 +
      gust *
      0.40
    ) *
    flexibility *
    uWindStrength;

  float flutterMeters =
    instanceHeightMeters *
    bendWeight *
    flutter *
    0.040 *
    flexibility *
    uWindStrength;

  // Hidden blades collapse into the ground instead of being switched
  // on as complete geometry. Combined with the stable density threshold
  // this makes incoming meadow detail emerge progressively.
  vec3 positionUpdated =
    position;

  positionUpdated.y *=
    grassReveal;

  vec4 worldPosition =
    finalWorld *
    vec4(
      positionUpdated,
      1.0
    );

  worldPosition.xz +=
    windDirection *
      mainBendMeters +
    crossWind *
      flutterMeters;

  worldPosition.y -=
    instanceHeightMeters *
    bladeHeight *
    bladeHeight *
    bladeHeight *
    gust *
    0.035 *
    flexibility *
    uWindStrength;

  mat3 normalWorld =
    mat3(
      finalWorld
    );

  vec3 correctedNormal =
    normal /
    vec3(
      max(
        dot(
          normalWorld[0],
          normalWorld[0]
        ),
        0.00001
      ),
      max(
        dot(
          normalWorld[1],
          normalWorld[1]
        ),
        0.00001
      ),
      max(
        dot(
          normalWorld[2],
          normalWorld[2]
        ),
        0.00001
      )
    );

  vNormalW =
    normalize(
      normalWorld *
      correctedNormal
    );

  vBladeHeight =
    bladeHeight;

  vVariation =
    fract(
      grassData.x +
      bladeData.x *
      0.71 +
      bladeData.w *
      0.29
    );

  vDryness =
    clamp(
      grassData.y +
      (
        bladeData.z -
        0.5
      ) *
        0.34,
      0.0,
      1.0
    );

  vSeedHead =
    grassData.w *
    smoothstep(
      0.76,
      0.98,
      bladeData.w
    ) *
    smoothstep(
      0.68,
      0.88,
      bladeHeight
    );

  vWindExposure =
    clamp(
      gust *
        0.76 +
      abs(
        flutter
      ) *
        0.11,
      0.0,
      1.0
    );

  vGrassReveal =
    grassReveal;

  gl_Position =
    viewProjection *
    worldPosition;
}
`

export const animatedGrassFragmentShader = `
precision highp float;

varying vec3 vNormalW;
varying float vBladeHeight;
varying float vVariation;
varying float vDryness;
varying float vSeedHead;
varying float vWindExposure;
varying float vGrassReveal;

uniform vec3 uSurfaceToLightDirection;

void main(void) {
  if (
    vGrassReveal <=
    0.015
  ) {
    discard;
  }

  vec3 normal =
    normalize(
      vNormalW
    );

  if (!gl_FrontFacing) {
    normal =
      -normal;
  }

  vec3 lightDirection =
    normalize(
      uSurfaceToLightDirection
    );

  float facingLight =
    abs(
      dot(
        normal,
        lightDirection
      )
    );

  float frontLight =
    max(
      dot(
        normal,
        lightDirection
      ),
      0.0
    );

  float backLight =
    max(
      dot(
        -normal,
        lightDirection
      ),
      0.0
    );

  vec3 lushRoot =
    vec3(
      0.180,
      0.315,
      0.060
    );

  vec3 lushTip =
    vec3(
      0.410,
      0.585,
      0.120
    );

  vec3 dryRoot =
    vec3(
      0.295,
      0.265,
      0.105
    );

  vec3 dryTip =
    vec3(
      0.640,
      0.555,
      0.235
    );

  float heightBlend =
    pow(
      clamp(
        vBladeHeight,
        0.0,
        1.0
      ),
      0.78
    );

  vec3 lushColor =
    mix(
      lushRoot,
      lushTip,
      heightBlend
    );

  vec3 dryColor =
    mix(
      dryRoot,
      dryTip,
      heightBlend
    );

  vec3 grassColor =
    mix(
      lushColor,
      dryColor,
      vDryness
    );

  grassColor *=
    mix(
      0.88,
      1.12,
      vVariation
    );

  vec3 seedColor =
    vec3(
      0.62,
      0.52,
      0.22
    );

  grassColor =
    mix(
      grassColor,
      seedColor,
      clamp(
        vSeedHead *
        (
          0.62 +
          vDryness *
          0.38
        ),
        0.0,
        0.82
      )
    );

  // Grass scatters substantial daylight. Avoid the black-wire look
  // produced by ordinary opaque Lambert shading on narrow vertical
  // ribbons.
  float wrappedLight =
    0.61 +
    facingLight *
      0.20 +
    frontLight *
      0.075 +
    backLight *
      0.115;

  float rootOcclusion =
    mix(
      0.88,
      1.0,
      smoothstep(
        0.0,
        0.30,
        vBladeHeight
      )
    );

  vec3 transmission =
    grassColor *
    backLight *
    (
      0.08 +
      vBladeHeight *
        0.12
    );

  float gustHighlight =
    1.0 +
    vWindExposure *
      0.025;

  gl_FragColor =
    vec4(
      (
        grassColor *
        wrappedLight *
        rootOcclusion +
        transmission
      ) *
      gustHighlight,
      1.0
    );
}
`

function registerAnimatedGrassShaders(): void {
  ShaderStore.ShadersStore[
    'estAnimatedGrassVertexShader'
  ] =
    animatedGrassVertexShader

  ShaderStore.ShadersStore[
    'estAnimatedGrassFragmentShader'
  ] =
    animatedGrassFragmentShader
}

export function calculateAnimatedGrassAnchorBasis(
  latitudeDegrees: number,
  longitudeDegrees: number,
  planetRadiusMeters: number,
): AnimatedGrassAnchorBasis {
  if (
    !Number.isFinite(
      latitudeDegrees,
    ) ||
    latitudeDegrees < -90 ||
    latitudeDegrees > 90
  ) {
    throw new Error(
      'Animated grass latitude must be finite and between -90 and 90.',
    )
  }

  if (
    !Number.isFinite(
      longitudeDegrees,
    )
  ) {
    throw new Error(
      'Animated grass longitude must be finite.',
    )
  }

  if (
    !Number.isFinite(
      planetRadiusMeters,
    ) ||
    planetRadiusMeters <= 0
  ) {
    throw new Error(
      'Animated grass planet radius must be positive and finite.',
    )
  }

  const latitude =
    latitudeDegrees *
    Math.PI /
    180

  const longitude =
    longitudeDegrees *
    Math.PI /
    180

  const cosineLatitude =
    Math.cos(
      latitude,
    )

  const sineLatitude =
    Math.sin(
      latitude,
    )

  const cosineLongitude =
    Math.cos(
      longitude,
    )

  const sineLongitude =
    Math.sin(
      longitude,
    )

  return {
    anchorMeters: {
      x:
        cosineLatitude *
        cosineLongitude *
        planetRadiusMeters,
      y:
        sineLatitude *
        planetRadiusMeters,
      z:
        cosineLatitude *
        sineLongitude *
        planetRadiusMeters,
    },

    east: {
      x:
        -sineLongitude,
      y:
        0,
      z:
        cosineLongitude,
    },

    north: {
      x:
        -sineLatitude *
        cosineLongitude,
      y:
        cosineLatitude,
      z:
        -sineLatitude *
        sineLongitude,
    },
  }
}

export function createAnimatedGrassMaterial(
  scene: Scene,
): ShaderMaterial {
  registerAnimatedGrassShaders()

  const material =
    new ShaderMaterial(
      'est-animated-grass',
      scene,
      'estAnimatedGrass',
      {
        attributes: [
          'position',
          'normal',
          'bladeData',
          'grassData',
        ],
        uniforms: [
          'world',
          'viewProjection',
          'uTimeSeconds',
          'uWindStrength',
          'uFadeStartMeters',
          'uFadeEndMeters',
          'uPlanetAnchorMeters',
          'uEastBasis',
          'uNorthBasis',
          'uSurfaceToLightDirection',
        ],
      },
    )

  material.backFaceCulling =
    false

  material.setFloat(
    'uTimeSeconds',
    0,
  )

  material.setFloat(
    'uWindStrength',
    1,
  )

  material.setFloat(
    'uFadeStartMeters',
    30,
  )

  material.setFloat(
    'uFadeEndMeters',
    42,
  )

  material.setVector3(
    'uPlanetAnchorMeters',
    Vector3.Zero(),
  )

  material.setVector3(
    'uEastBasis',
    new Vector3(
      1,
      0,
      0,
    ),
  )

  material.setVector3(
    'uNorthBasis',
    new Vector3(
      0,
      0,
      1,
    ),
  )

  material.setVector3(
    'uSurfaceToLightDirection',
    new Vector3(
      0.42,
      0.82,
      0.38,
    ).normalize(),
  )

  return material
}

export function setAnimatedGrassAnchor(
  material: ShaderMaterial,
  latitudeDegrees: number,
  longitudeDegrees: number,
  planetRadiusMeters: number,
): void {
  const basis =
    calculateAnimatedGrassAnchorBasis(
      latitudeDegrees,
      longitudeDegrees,
      planetRadiusMeters,
    )

  material.setVector3(
    'uPlanetAnchorMeters',
    new Vector3(
      basis.anchorMeters.x,
      basis.anchorMeters.y,
      basis.anchorMeters.z,
    ),
  )

  material.setVector3(
    'uEastBasis',
    new Vector3(
      basis.east.x,
      basis.east.y,
      basis.east.z,
    ),
  )

  material.setVector3(
    'uNorthBasis',
    new Vector3(
      basis.north.x,
      basis.north.y,
      basis.north.z,
    ),
  )
}
