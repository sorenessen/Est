import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  animatedGrassFragmentShader,
  animatedGrassVertexShader,
  calculateAnimatedGrassAnchorBasis,
} from './animated-grass-material'

describe(
  'animated grass material',
  () => {
    it('uses Babylon thin-instance transforms and per-blade data', () => {
      expect(
        animatedGrassVertexShader,
      ).toContain(
        '#include<instancesDeclaration>',
      )

      expect(
        animatedGrassVertexShader,
      ).toContain(
        '#include<instancesVertex>',
      )

      expect(
        animatedGrassVertexShader,
      ).toContain(
        'attribute vec4 grassData',
      )

      expect(
        animatedGrassVertexShader,
      ).toContain(
        'finalWorld',
      )
    })

    it('combines broad pressure fronts with subordinate flutter', () => {
      expect(
        animatedGrassVertexShader,
      ).toContain(
        'waveA',
      )

      expect(
        animatedGrassVertexShader,
      ).toContain(
        'waveB',
      )

      expect(
        animatedGrassVertexShader,
      ).toContain(
        'waveC',
      )

      expect(
        animatedGrassVertexShader,
      ).toContain(
        'flutter',
      )

      expect(
        animatedGrassVertexShader,
      ).toContain(
        'stiffness',
      )
    })

    it('keeps the blade root pinned while the tip bends most', () => {
      expect(
        animatedGrassVertexShader,
      ).toContain(
        'bladeHeight *\n    bladeHeight',
      )
    })

    it('supports wild-grass color, dryness and seed heads', () => {
      expect(
        animatedGrassFragmentShader,
      ).toContain(
        'lushRoot',
      )

      expect(
        animatedGrassFragmentShader,
      ).toContain(
        'dryTip',
      )

      expect(
        animatedGrassFragmentShader,
      ).toContain(
        'seedColor',
      )

      expect(
        animatedGrassFragmentShader,
      ).toContain(
        'backLight',
      )
    })

    it('uses a Y-up geographic tangent basis', () => {
      const basis =
        calculateAnimatedGrassAnchorBasis(
          0,
          0,
          10,
        )

      expect(
        basis.anchorMeters.x,
      ).toBeCloseTo(
        10,
      )

      expect(
        basis.anchorMeters.y,
      ).toBeCloseTo(
        0,
      )

      expect(
        basis.anchorMeters.z,
      ).toBeCloseTo(
        0,
      )

      expect(
        basis.east.z,
      ).toBeCloseTo(
        1,
      )

      expect(
        basis.north.y,
      ).toBeCloseTo(
        1,
      )
    })
  },
)
