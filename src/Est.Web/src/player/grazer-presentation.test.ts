import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  grazerPresentationAnimationNames,
  grazerPresentationAssetPath,
  grazerPresentationAssetScale,
  grazerPresentationAssetYawCorrectionRadians,
  resolveGrazerGaitFrame,
  resolveGrazerPresentationAnimation,
  resolveGrazerSimulationFrame,
} from './grazer-presentation'


describe(
  'grazer presentation asset policy',
  () => {
    it(
      'uses the Est-hosted Quaternius deer asset',
      () => {
        expect(
          grazerPresentationAssetPath,
        ).toBe(
          '/assets/animals/quaternius/ultimate-animated-animals/Deer.gltf',
        )
      },
    )

    it(
      'keeps source orientation and scale below the Est presentation root',
      () => {
        expect(
          grazerPresentationAssetYawCorrectionRadians,
        ).toBe(
          Math.PI / 2,
        )

        expect(
          grazerPresentationAssetScale,
        ).toBe(
          0.30,
        )
      },
    )

    it(
      'maps stationary representatives to deterministic idle presentation',
      () => {
        expect(
          resolveGrazerPresentationAnimation({
            locomotion:
              'stationary',
          }),
        ).toEqual({
          name:
            grazerPresentationAnimationNames.idle,
          playback:
            'simulation-time',
        })
      },
    )

    it(
      'maps moving representatives to in-place gait presentation',
      () => {
        expect(
          resolveGrazerPresentationAnimation({
            locomotion:
              'moving',
          }),
        ).toEqual({
          name:
            grazerPresentationAnimationNames.walk,
          playback:
            'gait-phase',
        })
      },
    )

    it(
      'maps gait phase deterministically across the clip frame range',
      () => {
        expect(
          resolveGrazerGaitFrame(
            0,
            10,
            40,
          ),
        ).toBe(
          10,
        )

        expect(
          resolveGrazerGaitFrame(
            Math.PI,
            10,
            40,
          ),
        ).toBe(
          25,
        )

        expect(
          resolveGrazerGaitFrame(
            Math.PI * 2,
            10,
            40,
          ),
        ).toBe(
          10,
        )

        expect(
          resolveGrazerGaitFrame(
            -Math.PI,
            10,
            40,
          ),
        ).toBe(
          25,
        )
      },
    )

    it(
      'maps simulation time deterministically across the idle clip',
      () => {
        expect(
          resolveGrazerSimulationFrame(
            0,
            10,
            40,
            3,
          ),
        ).toBe(
          10,
        )

        expect(
          resolveGrazerSimulationFrame(
            1.5,
            10,
            40,
            3,
          ),
        ).toBe(
          25,
        )

        expect(
          resolveGrazerSimulationFrame(
            3,
            10,
            40,
            3,
          ),
        ).toBe(
          10,
        )

        expect(
          resolveGrazerSimulationFrame(
            -1.5,
            10,
            40,
            3,
          ),
        ).toBe(
          25,
        )
      },
    )

    it(
      'rejects invalid gait inputs',
      () => {
        expect(
          () =>
            resolveGrazerGaitFrame(
              Number.NaN,
              0,
              10,
            ),
        ).toThrow(
          'Grazer gait phase must be finite.',
        )

        expect(
          () =>
            resolveGrazerGaitFrame(
              0,
              10,
              5,
            ),
        ).toThrow(
          'Grazer animation frame range must be finite and ordered.',
        )
      },
    )

    it(
      'rejects invalid simulation-time inputs',
      () => {
        expect(
          () =>
            resolveGrazerSimulationFrame(
              Number.NaN,
              0,
              10,
              1,
            ),
        ).toThrow(
          'Grazer simulation time must be finite.',
        )

        expect(
          () =>
            resolveGrazerSimulationFrame(
              0,
              0,
              10,
              0,
            ),
        ).toThrow(
          'Grazer animation frame range and duration must be finite, ordered, and positive.',
        )
      },
    )
  },
)
