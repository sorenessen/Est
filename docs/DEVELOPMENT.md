# Est Development Guide

Canonical operational reference for local development and repository recovery. Procedures described as verified have been exercised in the documented development environment.

## Current planetary renderer — 2026-09-15

The active production browser renderer at `/` uses one immutable Babylon
icosphere with authoritative terrain baked radially into fixed geometry.

Current invariant:

`camera movement changes the view, never the planet`

The current live planet does not use cube-sphere geometry, terrain patches, a
renderer quadtree, or camera-driven planetary LOD.

Babylon.js is the only current planetary renderer.

Cesium evaluation pages and historical Cesium/R-series work are preserved only
as engineering evidence. They are not an alternate current renderer, development
path, smoke-test target, or runtime-validation path.

**Do not use `/cesium.html` for current development, launch, smoke testing,
runtime validation, or simulation visualization.**

Use the Babylon root route with an explicit presentation view:

- Observatory: `http://127.0.0.1:5173/?session=<session-id>&view=observatory`
- Embodied Living World: `http://127.0.0.1:5173/?session=<session-id>&view=embodied`

Both views present the same authoritative simulation session. Switching views
must preserve the session ID rather than creating or copying world state.

### Living-surface runtime checkpoint — 2026-09-17

The current production globe presents authoritative live vegetation through
per-vertex coverage on the immutable Babylon terrain sphere. Broad vegetation
is terrain-surface state, not a globe-scale sprite layer, and the production
terrain shader does not depend on a separate vegetation `RawTexture` sampler.

Fresh-session validation against the rebuilt API also verified terrestrial
founder placement against authoritative standing water:

- humans: 0 flooded / 48 total;
- wolves: 0 flooded / 8 total.

This validates the generated-world dry-habitat placement path when terrain and
hydrology are available.

### Playable Ester runtime checkpoint — 2026-09-20

The production Babylon client now supports a manifested Ester entering the
authoritative world in a ground-level play mode.

Current authority boundary:

- the Ester has a stable `EsterId` independent of one session;
- authoritative manifested state owns planet identity and geographic
  latitude/longitude;
- movement is submitted through the Est application/API path and does not grant
  authoritative position ownership to the browser;
- the browser may predict and smooth movement for presentation quality, then
  converges to authoritative state;
- the local Babylon scene is a metre-space render frame centered around the
  manifested Ester, not a second simulation coordinate system;
- nearby authoritative people are transformed into that local frame using their
  stable `PersonId` and geographic position.

The local surface presentation now proves a second important separation:

`authoritative surface state != required close-range render density`

The play-space terrain samples Est's authoritative continuous terrain field and
may add deterministic geographically anchored presentation detail. That added
relief, color variation, micro-detail, grass, stones, wind animation, and
streaming behavior are visual representation only. They do not modify
authoritative terrain, vegetation, ecology, or person state.

Grass/scatter streaming is independently re-anchored around the manifested
Ester with hidden preload and progressive geographic fade, preventing the prior
whole-field regeneration pop during ordinary walking.

The Ester capsule remains temporary presentation scaffolding. Nearby simulated
people now use an animated human presentation keyed by authoritative `PersonId`.
Neither visual representation owns identity or simulation state.

Checkpoint commits:

- `4001097` — playable Ester embodiment proof;
- `10e0639` — playable environment presentation;
- `13ab399` — playable Ester manifestation documentation;
- `e1e4540` — animated human presentation;
- `d89048b` — authoritative Ester encounter recognition.

Latest verified web gate on September 21, 2026:

- 21 test files passed;
- 137 / 137 tests passed;
- `npx tsc --noEmit` passed;
- `git diff --check` passed.

The physical recognition proof is now complete. A manifested Ester can approach
a stable simulated person, cross an encounter boundary derived from
authoritative geographic positions, create the social encounter through the
simulation authority path, leave that boundary, and return. The movement API
reports whether the person recognized the Ester before the new encounter was
recorded, allowing presentation to distinguish a first encounter from
recognition on return without owning social truth.

## Local Environment

Primary environment: macOS Apple Silicon. Repository: `~/Projects/Est`. Solution: `Est.slnx`. Target framework: `net10.0`. Recorded SDK: `10.0.301`.

## Build and Test

From the repository root:

```bash
dotnet test Est.slnx
```

Latest verified full .NET solution result: September 17, 2026: 745 passed, 0 failed, 0 skipped. Latest verified web result: September 20, 2026: 129 / 129 tests passed with TypeScript and diff checks clean.

## Repository Inspection

```bash
git status --short
git branch --show-current
git log -1 --oneline
```

Do not assume the worktree is clean or that all current work is pushed.

## Recovery ZIP

From the repository root, run:

```bash
python3 scripts/create-recovery-zip.py
```

The script captures tracked and untracked, non-ignored files, including uncommitted source and documentation, and writes `Est-recovery.zip` beside the repository. It includes `Est/RECOVERY_STATUS.txt` with branch, commit, and worktree status. It excludes Git history, ignored build output, dependency caches, IDE state, and known ignored secrets. It skips symlinks rather than following them.

Review the ZIP before sharing it. Ignore rules cannot guarantee that every untracked file is safe. The ZIP is a source snapshot, not a Git backup. Preserve the original repository. This recovery procedure was validated successfully on macOS Apple Silicon and produced an integrity-tested recovery ZIP.

## Run and Debug

Est has a headless API project and a browser client in `src/Est.Web`.
The current Babylon browser renderer reads authoritative API session/world state.
The API provides explicit world/session operations and server-owned archive storage.

Cesium remains historical/evaluation-only and must not be used as the current
simulation viewer.
The archive directory defaults to the API content root's `archives` directory
and can be configured through `Est:ArchiveDirectory`.

### Normal startup

The macOS launcher was validated on September 8, 2026, including a true
cold start from Sparrow and subsequent reuse of both running services.

Est has two current presentation views over one authoritative simulation:

- **Observatory** presents the planetary/global simulation for inspection.
- **Embodied** presents the local Living World around manifested Ester.

For normal embodied development:

```bash
cd ~/Projects/Est
./scripts/dev/play.sh
```

For the global Observatory:

```bash
cd ~/Projects/Est
./scripts/dev/observatory.sh
```

For a focused local fauna presentation proof:

```bash
cd ~/Projects/Est
./scripts/dev/play.sh --focus fauna
```

`--focus fauna` is a development hint, not a separate Est application mode. It
places a newly manifested Ester near authoritative fauna when available so
walking-scale fauna presentation can be exercised without changing fauna
authority or normal movement semantics.

In this focused embodied view, **Step Fauna +1s** explicitly advances the
authoritative simulation by one second and refreshes the local world snapshot.
It exists to prove local actor motion and facing against authoritative state.

The step control does not establish the final embodied simulation-time policy
and does not enable an automatic Play-mode heartbeat. Wolf facing is derived
from successive authoritative geographic positions for the same `AnimalId`;
camera movement and Ester movement do not count as wolf motion.

Both launchers start or reuse Est.Api and Est.Web, wait for their health
checks, create a fresh Earth session through `POST /sessions`, and open the
Babylon root route with an explicit `view` parameter. No manual
API -> Web -> Play sequence is required.

Once a session is open, the **Enter World** / **Observatory** control switches
between presentation views while preserving that same session ID.

Useful Sparrow tasks may therefore map directly to:

- **Play Est**: `./scripts/dev/play.sh`
- **Play Est - Fauna**: `./scripts/dev/play.sh --focus fauna`
- **Est Observatory**: `./scripts/dev/observatory.sh`
- **Start API**: start or reuse the API independently.
- **Start Web**: start or reuse the web renderer independently.

The Sparrow workspace configuration is `sparrow.toml`.

The launcher is currently macOS-specific and uses iTerm to open independent
service windows. It requires the .NET SDK, Node.js/npm, Python 3, curl,
lsof, zsh, and iTerm. The launcher preserves the invoking shell's PATH
when starting service windows. Current Babylon development does not require
Cesium configuration or a Cesium ion token.

The API listens on port 5026 and Vite on IPv4 loopback port 5173. The API health contract
is `GET /health`, returning HTTP 200 with service `Est.Api` and status
`healthy`. Web startup validation must target the current Est.Web application,
not `/cesium.html`.

The scripts inspect existing listeners and verify project ownership before
reuse. They do not automatically kill or restart occupied ports. An
unrecognized or unhealthy listener produces a diagnostic and requires
manual investigation. The API script also preserves a recognized older
Est.Api host that predates `/health`, rather than discarding its sessions.

### Manual development

The individual scripts can be run from the repository root:

```bash
./scripts/dev/api.sh
./scripts/dev/web.sh
```

For direct debugging without the launcher, run Est.Api in a dedicated
terminal:

```bash
cd ~/Projects/Est/src/Est.Api
dotnet run --launch-profile http
```

Run the web development host from a separate terminal:

```bash
cd ~/Projects/Est/src/Est.Web
npm run dev
```

The verified API endpoint is `http://localhost:5026`. The verified browser
endpoint is `http://127.0.0.1:5173`. Vite proxies browser requests under
`/api` to Est.Api on port 5026.

Create a simulation session through `POST /sessions`, retain the returned
`sessionId`, and open either presentation view:

```text
http://127.0.0.1:5173/?session=<session-id>&view=observatory
http://127.0.0.1:5173/?session=<session-id>&view=embodied
```

A successful smoke test renders the Babylon planet and shows authoritative
session state. `/cesium.html` is historical/evaluation-only and is not a valid
current smoke-test target.

### Process and session safety

Simulation sessions are currently in-memory. Restarting Est.Api discards
existing sessions, so browser URLs containing old session IDs need a newly
created session after the restart. Play intentionally creates a fresh
session each time; it does not resume an existing session.

Keep API and Vite in separate long-running terminals. Stop a service
deliberately with Ctrl+C in its own window when necessary. Do not kill
unrelated listeners or restart the API merely to change browser rendering.
A listening port alone does not prove that a service is healthy.

## Release Procedures

Create `docs/RELEASE.md` when a real packaging and distribution workflow exists. Preserve validated versioning, packaging, signing, distribution, and release-validation procedures there.
