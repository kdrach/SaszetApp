#!/bin/bash
# deploy.sh
# Deploys SaszetApp to production using Docker Compose.
# Run from the root of the cloned SaszetApp repository on the VPS.
#
# Usage:
#   ./infrastructure/scripts/deploy.sh

set -euo pipefail

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}🚀 SaszetApp — Production Deployment${NC}"
echo "======================================"
echo ""

# ──────────────────────────────────────────────
# 1. Validate .env.prod exists
# ──────────────────────────────────────────────
if [ ! -f .env.prod ]; then
  echo -e "${RED}❌ Error: .env.prod not found!${NC}"
  echo ""
  echo "  Please copy .env.prod.example and fill in your production values:"
  echo "    cp .env.prod.example .env.prod"
  echo "    nano .env.prod"
  echo ""
  exit 1
fi
echo -e "${GREEN}  ✅ .env.prod found.${NC}"

# ──────────────────────────────────────────────
# 2. Pull latest code from main
# ──────────────────────────────────────────────
echo ""
echo -e "${YELLOW}▶ Pulling latest code from main...${NC}"
git pull origin main
echo -e "${GREEN}  ✅ Code is up to date.${NC}"

# ──────────────────────────────────────────────
# 3. Build Docker images
# ──────────────────────────────────────────────
echo ""
echo -e "${YELLOW}▶ Building Docker images...${NC}"
docker compose --env-file .env.prod -f docker-compose.yml -f docker-compose.prod.yml build
echo -e "${GREEN}  ✅ Images built.${NC}"

# ──────────────────────────────────────────────
# 4. Start all services
# ──────────────────────────────────────────────
echo ""
echo -e "${YELLOW}▶ Starting all services...${NC}"
docker compose --env-file .env.prod -f docker-compose.yml -f docker-compose.prod.yml up -d
echo -e "${GREEN}  ✅ Services started.${NC}"

# ──────────────────────────────────────────────
# 5. Wait for containers to become healthy
# ──────────────────────────────────────────────
echo ""
echo -e "${YELLOW}⏳ Waiting 20s for containers to become healthy...${NC}"
sleep 20

# ──────────────────────────────────────────────
# 6. Show service status
# ──────────────────────────────────────────────
echo ""
echo -e "${YELLOW}▶ Container status:${NC}"
docker compose --env-file .env.prod -f docker-compose.yml -f docker-compose.prod.yml ps

echo ""
echo "======================================"
echo -e "${GREEN}✅ Deployment complete!${NC}"
echo ""
echo "  Next steps:"
echo "  • Run smoke test: ./infrastructure/scripts/smoke-test.sh"
echo "  • View logs:      docker compose -f docker-compose.yml -f docker-compose.prod.yml logs -f"
