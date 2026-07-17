---
name: minver-version-bump
description: Use when cutting a release or bumping the DacPac Viewer app version, since the version is computed by MinVer from git tags rather than set in a csproj or version file.
---

# MinVer Version Bump

## Overview
The app version comes from [MinVer](https://github.com/adamralph/minver) reading git tags — there is no `<Version>` to edit anywhere. **Bumping the version means tagging and pushing**, nothing else.

## How it works
- `source/Directory.Build.props` sets `MinVerTagPrefix` to `v`, so release tags look like `v1.2.3`.
- With no tag reachable from HEAD, MinVer falls back to `0.0.0-alpha.0.<height>+<sha>`.
- `.github/workflows/release.yml` triggers automatically on any pushed tag matching `v*` (or manual `workflow_dispatch`). It computes the version via the `minver-cli` global tool (`minver -t v`), builds installers, and publishes a GitHub release.
- Both the `version` and `build` CI jobs use `fetch-depth: 0` — MinVer needs full tag history; a shallow clone breaks it.

## Bumping the version
1. Check the current state:
   ```
   git tag --sort=-v:refname | head   # existing tags
   minver -t v                        # current computed version (needs: dotnet tool install -g minver-cli)
   ```
2. Tag the commit to release and push the tag — pushing is what triggers the release build:
   ```
   git tag v1.2.3
   git push origin v1.2.3
   ```
3. For a pre-release, this repo's convention is a dash-separated suffix (not dot), matching existing tags like `v0.0.2-beta`, `v0.0.2-beta-3` … `v0.0.2-beta-5`. Check existing tags for that version first (`git tag --list "vX.Y.Z-beta*"`) and increment N from the highest found (start at `-beta-1` if none exist):
   ```
   git tag v1.2.3-beta-1
   git push origin v1.2.3-beta-1
   ```

## Common mistakes
- Adding `<VersionPrefix>`/`<VersionSuffix>`/`<Version>` to `DacPac.UI.csproj` or `DacPac.Core.csproj` — MinVer derives the version itself; don't set these.
- Tagging locally without pushing — `release.yml` only fires on the pushed tag.
- Tagging the wrong commit — a pushed `v*` tag immediately kicks off a real release build, so confirm `git log -1` / branch first.

## Quick reference
| Task | Command |
|---|---|
| See current computed version | `minver -t v` |
| List existing tags | `git tag --sort=-v:refname` |
| Cut a release | `git tag vX.Y.Z && git push origin vX.Y.Z` |
| Cut a pre-release | `git tag vX.Y.Z-beta-N && git push origin vX.Y.Z-beta-N` |
