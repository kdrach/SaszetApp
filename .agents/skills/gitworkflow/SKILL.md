---
name: gitworkflow
description: Standard git workflow, branching strategy, and execution order for the project.
---

# Git Workflow & Execution Rules

## Anti-Patterns

- ❌ Branching from other branches (always branch from `main`).
- ❌ PR targeting other branches directly (always target `main`).
- ❌ Non-conforming branch names (must be `feature/{number}-{slug}`).
- ❌ Committing directly to `main` (always use PRs).
- ❌ Switching branches in the main clone while worktrees are active (use worktrees instead).
- ❌ Using worktrees for cross-repo work (use separate clones).
- ❌ Leaving stale worktrees after PR merge (clean up immediately).

## Promotion Pipeline

*(Currently undefined. Add deployment/promotion steps here if needed)*

## Strict Execution Order

Every single change MUST follow this exact sequence:

0. **Check Build Validation**: Check the GitHub "Build Validation" pipeline status first. If it is `failed`, prioritize fixing the build failure immediately before doing any other feature work. Fix the build, write tests, pull request, get comments/reviews, merge, and delete the temporary branch.
1. **Prepare Tests First**: Write automated tests that define the change before writing the actual implementation code.
2. **Implement Code**: Write the code to make the tests pass.
3. **Document for AI Context**: After implementation and tests, ensure an AI-readable README (`README_AI.md`) exists **within each modified service/module subdirectory** (e.g., `services/admin/README_AI.md`, `services/routing/README_AI.md`) rather than at the root of the repository. This keeps the documentation as close to the code as possible. If a service-level `README_AI.md` doesn't exist, create it. If it does, update it so that future AI agents understand that specific service's architecture, patterns, and navigation.
   - **Required Structure for AI README**:
     - `## Architecture & Navigation`: Directory layout, key components, and where to find specific layers inside this service (e.g., controllers, services, database models).
     - `## Core Patterns & Rules`: Service-specific conventions, non-obvious constraints, and what to avoid.
     - `## Recent Feature Context`: A brief summary of newly implemented features in this service to give context for follow-up work.
4. **Create Pull Request**: Push to a branch (`feature/{issue-number}-{slug}`) and open a Pull Request targeting `main`.
5. **Code Review**: Run a Code Review (using the CodeReviewer subagent).
6. **Implement Fixes**: Deploy any requested improvements or bug fixes.
7. **Merge & Delete**: Merge the PR into `main` and delete the feature branch (both locally and remotely).
