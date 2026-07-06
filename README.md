# SaszetApp

A mobile-first application (PWA) designed to analyze pet food ingredients via barcode scans, manual text search, or label photos.

## Requirements

- Docker and Docker Compose
- Node.js 20+ (for running E2E tests locally)

## Running the Application Locally

The application uses `docker-compose` to spin up the entire stack, which closely resembles the production environment. This includes:
- PostgreSQL databases (for Keycloak and the Application)
- Keycloak (Identity Provider)
- .NET 8 Backend API
- React/Vite Frontend (End-User App)
- React/Vite Frontend (Admin Gateway)

1. Build and start the stack:
   ```bash
   docker-compose up -d --build
   ```

2. Wait for the services to start (Keycloak may take a few moments).

3. Access the services:
   - **Frontend (Mobile App)**: [http://localhost:3010](http://localhost:3010)
   - **Frontend (Admin App)**: [http://localhost:3011](http://localhost:3011)
   - **Backend API**: [http://localhost:5000](http://localhost:5000) (Health check at `http://localhost:5000/health`)
   - **Keycloak**: [http://localhost:8180](http://localhost:8180)

## Running E2E Tests

We use Playwright for End-to-End testing.

1. Ensure the Docker Compose stack is running:
   ```bash
   docker-compose up -d
   ```

2. Navigate to the E2E tests directory:
   ```bash
   cd tests-e2e
   ```

3. Install dependencies and Playwright browsers (first time only):
   ```bash
   npm install
   npx playwright install chromium
   ```

4. Run the tests:
   ```bash
   npm run test:e2e
   ```
