import {
  ShaderMaterial,
  ShaderStore,
  Vector3,
  type Scene,
} from '@babylonjs/core'

import {
  defaultPlanetaryLighting,
  type PlanetaryLightingState,
} from '../planet/planetary-lighting'

export interface TerrainMaterialOptions {
  readonly meanRadiusMeters: number
  readonly minimumElevationMeters: number
  readonly maximumElevationMeters: number
  readonly lighting?:
    PlanetaryLightingState
}

export const terrainVertexShader = `
precision highp float;

attribute vec3 position;
attribute vec3 normal;
attribute float vegetationCoverage;

uniform mat4 worldViewProjection;

varying vec3 vPlanetPosition;
varying vec3 vTerrainNormal;
varying float vVegetationCoverage;

void main(void) {
  vPlanetPosition = position;
  vTerrainNormal = normal;
  vVegetationCoverage =
    vegetationCoverage;

  gl_Position =
    worldViewProjection *
    vec4(position, 1.0);
}
`

export const terrainFragmentShader = `
precision highp float;

varying vec3 vPlanetPosition;
varying vec3 vTerrainNormal;
varying float vVegetationCoverage;

uniform float uMeanRadiusMeters;
uniform float uMinimumElevationMeters;
uniform float uMaximumElevationMeters;
uniform vec3 uSurfaceToLightDirection;
uniform float uAmbientIntensity;
uniform float uDiffuseIntensity;

float terrainHash(vec3 p) {
  p = fract(
    p * 0.1031
  );

  p += dot(
    p,
    p.yzx + 33.33
  );

  return fract(
    (p.x + p.y) * p.z
  );
}

float terrainNoise(vec3 p) {
  vec3 cell =
    floor(p);

  vec3 local =
    fract(p);

  local =
    local *
    local *
    (
      3.0 -
      2.0 * local
    );

  float n000 =
    terrainHash(
      cell +
      vec3(0.0, 0.0, 0.0)
    );

  float n100 =
    terrainHash(
      cell +
      vec3(1.0, 0.0, 0.0)
    );

  float n010 =
    terrainHash(
      cell +
      vec3(0.0, 1.0, 0.0)
    );

  float n110 =
    terrainHash(
      cell +
      vec3(1.0, 1.0, 0.0)
    );

  float n001 =
    terrainHash(
      cell +
      vec3(0.0, 0.0, 1.0)
    );

  float n101 =
    terrainHash(
      cell +
      vec3(1.0, 0.0, 1.0)
    );

  float n011 =
    terrainHash(
      cell +
      vec3(0.0, 1.0, 1.0)
    );

  float n111 =
    terrainHash(
      cell +
      vec3(1.0, 1.0, 1.0)
    );

  float nx00 =
    mix(
      n000,
      n100,
      local.x
    );

  float nx10 =
    mix(
      n010,
      n110,
      local.x
    );

  float nx01 =
    mix(
      n001,
      n101,
      local.x
    );

  float nx11 =
    mix(
      n011,
      n111,
      local.x
    );

  float nxy0 =
    mix(
      nx00,
      nx10,
      local.y
    );

  float nxy1 =
    mix(
      nx01,
      nx11,
      local.y
    );

  return mix(
    nxy0,
    nxy1,
    local.z
  );
}

void main(void) {
  vec3 direction =
    normalize(
      vPlanetPosition
    );

  vec3 terrainNormal =
    normalize(
      vTerrainNormal
    );

  float elevationMeters =
    (
      length(
        vPlanetPosition
      ) -
      1.0
    ) *
    uMeanRadiusMeters;

  float elevationMagnitude =
    max(
      max(
        abs(
          uMinimumElevationMeters
        ),
        abs(
          uMaximumElevationMeters
        )
      ),
      1.0
    );

  float elevationSignal =
    clamp(
      elevationMeters /
      elevationMagnitude,
      -1.0,
      1.0
    );

  // Seamless planet-space procedural detail.
  //
  // A low-frequency domain warp breaks up the obvious isotropic
  // noise pattern. Ridged and fractal terms then provide correlated
  // geological-looking structure without becoming simulation state.
  vec3 warp =
    vec3(
      terrainNoise(
        direction * 7.0 +
        vec3(11.0, 3.0, 17.0)
      ),
      terrainNoise(
        direction * 7.0 +
        vec3(29.0, 41.0, 5.0)
      ),
      terrainNoise(
        direction * 7.0 +
        vec3(7.0, 23.0, 37.0)
      )
    ) -
    vec3(0.5);

  vec3 warpedDirection =
    normalize(
      direction +
      warp * 0.075
    );

  float broad =
    terrainNoise(
      warpedDirection * 18.0
    );

  float regional =
    terrainNoise(
      warpedDirection * 46.0 +
      vec3(13.0, 31.0, 19.0)
    );

  float ridgeNoise =
    terrainNoise(
      warpedDirection * 115.0 +
      vec3(47.0, 9.0, 27.0)
    );

  float ridges =
    1.0 -
    abs(
      ridgeNoise * 2.0 -
      1.0
    );

  ridges *= ridges;

  float detail =
    terrainNoise(
      warpedDirection * 310.0 +
      vec3(5.0, 53.0, 71.0)
    );

  float grain =
    terrainNoise(
      direction * 960.0
    );

  float materialVariation =
    (
      broad - 0.5
    ) * 0.18 +
    (
      regional - 0.5
    ) * 0.12 +
    (
      ridges - 0.35
    ) * 0.10 +
    (
      detail - 0.5
    ) * 0.065 +
    (
      grain - 0.5
    ) * 0.025;

  vec3 lowTerrain =
    vec3(
      0.255,
      0.225,
      0.170
    );

  vec3 highTerrain =
    vec3(
      0.455,
      0.415,
      0.335
    );

  float elevationBlend =
    clamp(
      0.5 +
      elevationSignal * 0.28,
      0.0,
      1.0
    );

  vec3 terrainColor =
    mix(
      lowTerrain,
      highTerrain,
      elevationBlend
    );

  terrainColor *=
    1.0 +
    materialVariation;

  float radialAgreement =
    clamp(
      dot(
        terrainNormal,
        direction
      ),
      0.0,
      1.0
    );

  float slope =
    smoothstep(
      0.00025,
      0.012,
      1.0 -
      radialAgreement
    );

  vec3 exposedRock =
    vec3(
      0.405,
      0.395,
      0.365
    );

  float rockBreakup =
    clamp(
      0.82 +
      ridges * 0.20 +
      detail * 0.10,
      0.0,
      1.15
    );

  terrainColor =
    mix(
      terrainColor,
      exposedRock *
        rockBreakup,
      slope * 0.62
    );

  float vegetationCoverage =
    clamp(
      vVegetationCoverage,
      0.0,
      1.0
    );

  float vegetationBreakup =
    clamp(
      0.78 +
        detail * 0.16 +
        grain * 0.12 +
        ridges * 0.08,
      0.62,
      1.0
    );

  float vegetationPresence =
    vegetationCoverage *
    vegetationBreakup *
    (
      1.0 -
      slope * 0.52
    );

  vec3 sparseVegetation =
    vec3(
      0.115,
      0.205,
      0.075
    );

  vec3 denseVegetation =
    vec3(
      0.245,
      0.405,
      0.135
    );

  vec3 vegetationColor =
    mix(
      sparseVegetation,
      denseVegetation,
      clamp(
        vegetationCoverage *
          1.15 +
          detail * 0.10,
        0.0,
        1.0
      )
    );

  terrainColor =
    mix(
      terrainColor,
      vegetationColor,
      clamp(
        vegetationPresence * 0.88,
        0.0,
        0.88
      )
    );

  float diffuse =
    max(
      dot(
        terrainNormal,
        normalize(
          uSurfaceToLightDirection
        )
      ),
      0.0
    );

  float illumination =
    uAmbientIntensity +
    diffuse *
      uDiffuseIntensity;

  gl_FragColor =
    vec4(
      terrainColor *
      illumination,
      1.0
    );
}
`

export function validateTerrainMaterialOptions(
  options: TerrainMaterialOptions,
): void {
  if (
    !Number.isFinite(
      options.meanRadiusMeters,
    ) ||
    options.meanRadiusMeters <= 0
  ) {
    throw new Error(
      'Terrain material requires a positive finite mean radius.',
    )
  }

  if (
    !Number.isFinite(
      options.minimumElevationMeters,
    ) ||
    !Number.isFinite(
      options.maximumElevationMeters,
    ) ||
    options.minimumElevationMeters >
      options.maximumElevationMeters
  ) {
    throw new Error(
      'Terrain material elevation range must be finite and ordered.',
    )
  }
}

function registerTerrainShaders(): void {
  ShaderStore.ShadersStore[
    'estTerrainVertexShader'
  ] =
    terrainVertexShader

  ShaderStore.ShadersStore[
    'estTerrainFragmentShader'
  ] =
    terrainFragmentShader
}

export function createTerrainShaderMaterial(
  scene: Scene,
  options: TerrainMaterialOptions,
): ShaderMaterial {
  validateTerrainMaterialOptions(
    options,
  )

  registerTerrainShaders()

  const material =
    new ShaderMaterial(
      'est-terrain-surface',
      scene,
      'estTerrain',
      {
        attributes: [
          'position',
          'normal',
          'vegetationCoverage',
        ],
        uniforms: [
          'worldViewProjection',
          'uMeanRadiusMeters',
          'uMinimumElevationMeters',
          'uMaximumElevationMeters',
          'uSurfaceToLightDirection',
          'uAmbientIntensity',
          'uDiffuseIntensity',
        ],
      },
    )

  material.setFloat(
    'uMeanRadiusMeters',
    options.meanRadiusMeters,
  )

  material.setFloat(
    'uMinimumElevationMeters',
    options.minimumElevationMeters,
  )

  material.setFloat(
    'uMaximumElevationMeters',
    options.maximumElevationMeters,
  )

  const lighting =
    options.lighting ??
    defaultPlanetaryLighting

  material.setVector3(
    'uSurfaceToLightDirection',
    new Vector3(
      lighting
        .surfaceToLightDirection.x,
      lighting
        .surfaceToLightDirection.y,
      lighting
        .surfaceToLightDirection.z,
    ),
  )

  material.setFloat(
    'uAmbientIntensity',
    lighting.ambientIntensity,
  )

  material.setFloat(
    'uDiffuseIntensity',
    lighting.diffuseIntensity,
  )

  material.backFaceCulling = true

  return material
}
