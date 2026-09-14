# Cesium historical evaluation archive

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

## Retirement policy

Once the Babylon renderer is fully established through terrain, lighting,
ocean, atmosphere, and subsequent runtime validation, review this archive
during R6 Cesium retirement.

At that point:

- preserve only evidence that still explains useful architectural lessons;
- rely on Git history for redundant implementation snapshots;
- delete obsolete Cesium runtime code, adapters, dependencies, and archived
  implementation files that no longer provide useful reference value.
