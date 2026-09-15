# Est Development Guide

Canonical operational reference for local development and repository recovery. Procedures described as verified have been exercised in the documented development environment.

## Current planetary renderer — 2026-09-15

The active production browser renderer at `/` uses one immutable Babylon
icosphere with authoritative terrain baked radially into fixed geometry.

Current invariant:

`camera movement changes the view, never the planet`

The current live planet does not use cube-sphere geometry, terrain patches, a
renderer quadtree, or camera-driven planetary LOD.

Cesium evaluation pages and the historical R1/R2/R3/R4 work are intentionally
preserved as engineering evidence for future features and enhancements. Do not
delete them simply because they are not the current production rendering path.

Some launcher instructions below still describe the earlier Cesium-first
workflow. Until those scripts are deliberately realigned, validate the current
production renderer at:

`http://localhost:5173/?session=<session-id>`

## Local Environment

Primary environment: macOS Apple Silicon. Repository: `~/Projects/Est`. Solution: `Est.slnx`. Target framework: `net10.0`. Recorded SDK: `10.0.301`.

## Build and Test

From the repository root:

```bash
dotnet test Est.slnx
```

Latest verified result: September 8, 2026, `dotnet test Est.slnx`: 222 passed, 0 failed, 0 skipped. The web production build also passed with `npm run build`.

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
The Cesium evaluation reads authoritative API session/world state. The API
provides explicit world/session operations and server-owned archive storage.
The archive directory defaults to the API content root's `archives` directory
and can be configured through `Est:ArchiveDirectory`.

### Normal startup

The macOS launcher was validated on September 8, 2026, including a true
cold start from Sparrow and subsequent reuse of both running services.

From Sparrow, open the Est workspace and select **Play Est**. Alternatively,
run the same launcher from a regular development terminal:

```bash
cd ~/Projects/Est
./scripts/dev/play.sh
```

Play starts or reuses Est.Api and Est.Web, waits for their health checks,
creates a fresh Earth session through `POST /sessions`, and opens Cesium
with the returned session ID. No manual API -> Web -> Play sequence is
required.

The Sparrow workspace configuration is `sparrow.toml`:

- **Play Est**: normal startup and fresh Earth session.
- **Start API**: start or reuse the API independently.
- **Start Web**: start or reuse the web renderer independently.

The launcher is currently macOS-specific and uses iTerm to open independent
service windows. It requires the .NET SDK, Node.js/npm, Python 3, curl,
lsof, zsh, and iTerm. The launcher preserves the invoking shell's PATH
when starting service windows. The browser requires the existing local
Cesium configuration, including the ion token where applicable.

The API listens on port 5026 and Vite on port 5173. The API health contract
is `GET /health`, returning HTTP 200 with service `Est.Api` and status
`healthy`. The web health check requests the Cesium HTML page using
`localhost`, which supports the observed IPv6-only Vite listener.

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
cd ~/Projects/Est
dotnet run --project src/Est.Api/Est.Api.csproj --launch-profile http
```

Run the web development host from a separate terminal:

```bash
cd ~/Projects/Est/src/Est.Web
npm run dev
```

The verified API endpoint is `http://localhost:5026`. The verified browser
endpoint is `http://localhost:5173`. Vite proxies browser requests under
`/api` to Est.Api on port 5026.

Create a simulation session through `POST /sessions`, retain the returned
`sessionId`, and open:

```text
http://localhost:5173/cesium.html?session=<session-id>
```

A successful smoke test renders the Cesium globe and shows authoritative
session data, for example `Earth · 288.15 K · t=0s`.

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
