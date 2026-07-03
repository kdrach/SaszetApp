# Infrastructure (Docker & CI/CD)

## Architecture & Navigation
- `docker-compose.yml`: Defines the core backing services. We enforce a strict database isolation pattern.
- `infrastructure/keycloak/`: Contains the Keycloak `realm-export.json` which is automatically imported on startup to provide zero-touch identity provider provisioning.
- `.github/workflows/`: Contains GitHub Actions pipelines for Docker validation, .NET/React testing, and AI Code Reviews.

## Core Patterns & Rules
- **Strict Separation of Concerns**: `app-db` is EXCLUSIVELY for the backend API. `keycloak-db` is EXCLUSIVELY for Keycloak. They run on separate ports externally (5432 and 5433) but use standard 5432 internally.
- **Stateless Configuration**: Keycloak imports `realm-export.json` on startup. Any changes to roles, clients, or users should be persisted in this file rather than done manually in the UI, ensuring reproducible environments.
- **Zero-Touch Provisioning**: Containers must start without requiring manual intervention.

## Recent Feature Context
- Configured base `docker-compose.yml` for Epic 1.
- Initialized `petfood-realm` with `admin` role and `saszetapp-admin`/`saszetapp-pwa` clients.
- Configured CI/CD workflows for testing and AI PR Reviewer.
