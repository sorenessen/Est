import type {
  TransformNode,
} from '@babylonjs/core'


export interface FaunaPresentation {
  actorKey: string
  root: TransformNode
  setEnabled(
    enabled: boolean,
  ): void
  setHeadingRadians(
    headingRadians: number,
  ): void
  dispose(): void
}

/**
 * Local actor heading is measured from +east toward +north.
 * Est fauna presentations face local +X/east, while Babylon Y rotation
 * uses the opposite sign for this axis convention.
 */
export function resolveFaunaPresentationRotationY(
  headingRadians: number,
): number {
  if (
    !Number.isFinite(
      headingRadians,
    )
  ) {
    throw new RangeError(
      'Fauna presentation heading must be finite.',
    )
  }

  return -headingRadians
}
