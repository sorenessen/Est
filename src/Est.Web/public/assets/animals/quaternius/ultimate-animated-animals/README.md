# Quaternius Ultimate Animated Animals

Source:

- Quaternius Ultimate Animated Animals, July 2021
- https://quaternius.com/packs/ultimateanimatedanimals.html

License:

- CC0 1.0 Universal / Public Domain Dedication
- Original license text is preserved in `LICENSE-CC0.txt`.

Imported asset:

- `Wolf.gltf`

The imported wolf is a self-contained glTF 2.0 asset with:

- one skinned wolf mesh;
- one 51-joint armature;
- embedded geometry and animation data;
- no external textures;
- twelve animation clips:
  - `Attack`
  - `Death`
  - `Eating`
  - `Gallop`
  - `Gallop_Jump`
  - `Idle`
  - `Idle_2`
  - `Idle_2_HeadLow`
  - `Idle_HitReact1`
  - `Idle_HitReact2`
  - `Jump_ToIdle`
  - `Walk`

The source wolf faces +Z. Est's local actor presentation convention derives
heading from authoritative geographic displacement with visual forward aligned
to the presentation root's +X direction. Any source-asset orientation correction
must therefore remain below the Est-owned actor root and must not redefine
authoritative heading.

The animation clips animate the rig beneath the source `AnimalArmature`.
The source `AnimalArmature` itself is not animation-driven. Locomotion clips
such as `Walk` are in-place with respect to the actor root.

This asset is a presentation resource only. It does not define authoritative
animal identity, activity, location, heading, velocity, or movement. Est owns
those concerns through simulation state and local actor presentation policy.
