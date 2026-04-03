# To my dearest Agent, with Love

## TERMINOLOGY POLICY (for all agents)

To ensure clarity, consistency, and predictable behavior across all agents, the following terminology is authoritative and must be used throughout this project.

| Term  | Definition                                                                                                             |
| ----- | ---------------------------------------------------------------------------------------------------------------------- |
| Turn  | A single conversational exchange consisting of one user message and one assistant message.                             |
| Task  | A discrete, actionable unit of work. Tasks are the primary planning unit for all agents.                               |
| Group | A collection of related tasks. Groups are organizational containers and do not imply sequencing.                       |
| Phase | A sequential stage of the project. Phases imply ordered progression and should be used only when explicitly required.  |
| Slice | Not used in this project. The term is ambiguous across AI systems and is replaced by the definitions above.            |

All agents must use these terms consistently in planning, reporting, and communication.

## Git Workflow Policy

### Branching

- Never commit to `main`.  
- Create a feature branch for each phase from the latest `origin/main`: `feature/<name>`.  
- Prefer traceable branch names like `feature/<area>-<intent>` or `feature/<issue>-<intent>`.  
- Use optional sub‑branches for risky or multi‑step work: `feature/<phase>/<step>`.  
- Keep branches focused and short‑lived.

### Committing

- Make small, atomic commits after each meaningful step or task.  
- Commit frequently during a slice, not only at the end.  
- Commit before applying risky patches or regenerating files.  
- Do not mix unrelated changes in a single commit.
- Stage only files relevant to the current task; leave unrelated local changes untouched.
- Create checkpoint commits before large refactors or uncertain edits.

### Commit Messages (Conventional Commits)

Use standard types: `feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:`  
Format:  
`<type>(scope): short summary`

Examples:

- `feat(fontawesome): add curated icon runtime allowlist`
- `fix(layout): load curated subset css when available`
- `docs(strategy): document font awesome level roadmap`

### Pull Requests

- All feature branches merge through PRs.  
- Keep PRs small and descriptive (what changed, why, risks).  
- Do not merge if validation or tests fail.
- Before opening PR, sync with latest `main` (rebase or merge) and rerun validation.

### Safety

- Before starting a new feature branch, fetch latest remote state and branch from updated `main`.  
- Do not pull/rebase into a dirty working tree.  
- Create a commit checkpoint before large or uncertain changes.  
- If a patch misbehaves, restore the file or revert the commit.  
- Never run destructive commands (`git reset --hard`, `git checkout --`, `git clean -fd`) without explicit user approval.
- If a branch becomes unstable, prefer non-destructive recovery first (revert/cherry-pick/new branch).

### Signed Merge Policy

- Agents may commit unsigned on feature branches.  
- All merges into `main` must be performed by a human maintainer using a **GPG‑signed merge commit**.  
- GitHub is configured to require signed commits on the `main` branch, ensuring that integration points are authenticated without blocking agent workflows.  
- Agents must not attempt to sign commits or tags.

## Third-Party/Vendor Assets

- Handle vendor assets (for example Font Awesome) with an explicit strategy per feature:
  1. Preferred: Use a reproducible package/restore process with lockfiles/scripts.
  2. Only commit vendor files when licensing and distribution terms explicitly allow it.
- For this repository specifically, licensed Font Awesome payloads must not be committed to source control or included in the NuGet package.
- Do not leave vendor dependency state ambiguous or partially tracked.
