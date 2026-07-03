# GitHub Actions Workflows

## Architecture & Navigation
- `docker-build-test.yml`: Validates that `docker-compose up` completes successfully.
- `test.yml`: Runs .NET and frontend tests.
- `ai-pr-reviewer.yml`: Uses an LLM to review pull requests.

## Core Patterns & Rules
- All workflows must trigger on Pull Requests to `main`.
- Secrets should be properly injected via GitHub Repository Secrets (e.g., `OPENAI_API_KEY`).

## Recent Feature Context
- Base workflows established during Epic 1 to ensure build stability.
