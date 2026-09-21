# Quaternius Universal Base Characters

Source:

- Quaternius Universal Base Characters, Standard/free edition
- https://quaternius.com/packs/universalbasecharacters.html

License:

- CC0 1.0 Universal / Public Domain Dedication
- Original license text is preserved in `LICENSE-CC0.txt`.

Imported assets:

- `Superhero_Female_FullBody.gltf`
- `Superhero_Male_FullBody.gltf`
- their binary buffers and referenced textures

The source archive contains two inconsistent glTF texture URIs:

- `T_Eye_Normal_png.png`
- `T_Hair_1_Normal_png.png`

The archive actually supplies:

- `T_Eye_Normal.png`
- `T_Hair_1_Normal.png`

Est normalizes those two imported glTF URI references to the filenames
actually present in the source archive rather than storing duplicate texture
files.

These assets are presentation resources only. They do not define authoritative
person identity, sex, state, location, or behavior.
