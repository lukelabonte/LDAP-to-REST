---
name: create-release
description: This skill should be used when the user asks to "create a release", "tag a release", "publish a new version", "draft release notes", or "cut a release". Walks through version selection, release notes curation, changelog update, and tag creation.
user-invocable: true
---

# Create Release

An interactive workflow for creating releases of the LDAP-to-REST project.

## Step 1: Authorization Check

1. Run `gh auth status` and parse the logged-in username
2. Check against the authorized users list: `lukelabonte`
3. If not authorized: print a message explaining only authorized maintainers can create releases, then **stop**
4. If `gh` is not authenticated: print a message to run `gh auth login` first, then **stop**

## Step 2: Pre-flight Checks

Run these checks and report any failures:

1. **Clean working tree:** `git status --porcelain` must return empty
2. **On main branch:** `git branch --show-current` must return `main`
3. **Up-to-date with remote:** `git fetch origin main && git rev-list HEAD..origin/main --count` must return `0`

If any check fails, use AskUserQuestion explaining what's wrong with options:
- **Fix it** — Provide instructions for the specific issue
- **Abort** — Cancel the release

## Step 3: Gather Changes Since Last Release

1. Get last tag: `git tag -l 'v*' --sort=-v:refname | head -1`
2. If no tags exist (first release): use `git log --oneline` for all commits
3. Otherwise: `git log --oneline <last_tag>..HEAD`
4. Also get the full diff summary: `git diff --stat <last_tag>..HEAD` (or from root if first release)
5. Count and categorize commits — display a summary to the user

## Step 4: Suggest Version Number

Analyze commit messages for patterns:
- "fix", "bug", "patch" → suggest **patch** bump
- "feat", "add", "new" → suggest **minor** bump
- "breaking", "remove", "BREAKING CHANGE" → suggest **major** bump
- If first release: suggest `v1.0.0`

Present via AskUserQuestion with the suggestion as recommended and alternatives available.
Show the reasoning: "Found X features, Y fixes, Z breaking changes → suggesting vX.Y.Z"

## Step 5: Curate Release Notes (Interactive)

Group commits into categories: **Features**, **Fixes**, **Changes**, **Documentation**, **Internal**

For each commit, decide if it's:
- **Clearly public-facing** (new endpoints, bug fixes, config changes): Auto-include, show user for confirmation
- **Clearly internal** (refactoring, CI, tests, CLAUDE.md changes): Auto-exclude, skip silently
- **Borderline** (dependency updates, security fixes, performance): Ask via AskUserQuestion with:
  - The commit message and what changed
  - Why it might be worth including (user impact)
  - Why it might be worth excluding (implementation detail)
  - Recommended choice

Show progress throughout: "Release note X of Y"

After each included item, let user refine the wording if they want.

## Step 6: Review Final Release Notes

Present the complete categorized release notes.

Use AskUserQuestion:
- **Looks good, proceed** (Recommended)
- **Edit an entry** — Ask which entry and what the new text should be
- **Add something manually** — Let user add a custom entry
- **Start over** — Redo from Step 5

Loop until user approves.

## Step 7: Update CHANGELOG.md

1. Read current `CHANGELOG.md`
2. Under the `## [Unreleased]` heading, insert a new version entry:

```
## [vX.Y.Z] - YYYY-MM-DD

### Added
- ...

### Fixed
- ...

### Changed
- ...
```

3. Clear the `[Unreleased]` section content (leave the heading)
4. Show the user the CHANGELOG diff for confirmation

Refer to `references/changelog-format.md` for formatting rules.

## Step 8: Commit Changelog

Stage and commit the changelog update:

```bash
git add CHANGELOG.md
```

Then commit with message: `Release vX.Y.Z`

Use the HEREDOC format for the commit message.

## Step 9: Create and Push Tag

Use AskUserQuestion:
- **Create tag and push (triggers release)** (Recommended) — This will push the changelog commit and tag, triggering the release workflow
- **Abort — I changed my mind** — Stop here without pushing

If proceeding:
```bash
git push origin main && git tag vX.Y.Z && git push origin vX.Y.Z
```

Tell the user: "Tag pushed. The release workflow is now running — it will build the Docker image, push to ghcr.io, and create the GitHub Release."

## Step 10: Update GitHub Release with Curated Notes

1. Wait a moment for the release to be created by the workflow
2. Build the release body from the curated notes + a link to the CHANGELOG section
3. Update the release: `gh release edit vX.Y.Z --notes "..."` (using HEREDOC)
4. Confirm: "Release vX.Y.Z is live at https://github.com/lukelabonte/LDAP-to-REST/releases/tag/vX.Y.Z"

## Important Notes

- The `.NET 8 SDK` path must be set before any dotnet commands: `PATH="/opt/homebrew/opt/dotnet@8/bin:$PATH"`
- The release workflow (`.github/workflows/release.yml`) triggers on `v*` tag push
- The workflow builds, tests, pushes a Docker image to ghcr.io, and creates a GitHub Release with a placeholder body
- This skill replaces the placeholder body with curated release notes
