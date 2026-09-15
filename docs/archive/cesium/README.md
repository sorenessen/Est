# Cesium historical evaluation archive

## Preservation intent

This archive is intentionally retained as future-facing engineering evidence,
not merely as dead code.

Cesium demonstrated capabilities and integration seams that may become useful
again for future Est enhancements, including global-to-local navigation,
streamed terrain and imagery, regional surface presentation, local geographic
geometry, scale/significance behavior, and renderer-neutral preparation
boundaries.

Preserving this work does not make Cesium the current production renderer.
It ensures Est does not discard useful evidence that may accelerate later
features.

Cesium is no longer the production planetary-renderer direction for Est.

This directory preserves unique implementation evidence from the Cesium
evaluation period while Babylon proves the replacement renderer through the
remaining First Light rendering milestones.

## authoritative-session-viewer.ts

This was an experimental Cesium viewer for an authoritative Est simulation
session. It loaded Est-owned world, surface, terrain, and simulation state and
rendered the resulting planet through Cesium.

It is retained only as historical/reference material. It is not part of the
active application path and should not receive new production development.

## Preservation policy

Cesium is not the current production planetary renderer, but this archive is
not scheduled for wholesale deletion.

Preserve working examples, failed experiments and their failure reasons,
renderer-neutral seams, regional-surface and local-geometry studies, runtime
observations, screenshots, and lessons about terrain, imagery, streaming,
scale, and LOD.

Future cleanup may remove obsolete Cesium code from active production
dependencies or move additional material into archival locations. Cleanup must
distinguish "not currently production" from "not useful."

If future Est features require continuous extreme-scale navigation, mature
terrain streaming, regional/local transitions, GIS-oriented presentation, or
similar capabilities, review this preserved evidence first.
