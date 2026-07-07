#!/bin/bash
# cleanup-old-deployment.sh
# Safely removes old SaszetApp deployment from the VPS.
# WARNING: Does NOT delete PostgreSQL data volumes without explicit confirmation.

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}🧹 SaszetApp — Old Deployment Cleanup Script${NC}"
echo "=============================================="
echo ""

# ──────────────────────────────────────────────
# 1. Stop and remove all running Docker containers
# ──────────────────────────────────────────────
echo -e "${YELLOW}▶ Stopping and removing ALL Docker containers...${NC}"
CONTAINERS=$(docker ps -aq 2>/dev/null || true)
if [ -n "$CONTAINERS" ]; then
  docker stop $CONTAINERS 2>/dev/null || true
  docker rm $CONTAINERS 2>/dev/null || true
  echo -e "${GREEN}  ✅ Containers removed.${NC}"
else
  echo "  ℹ️  No containers running."
fi

# ──────────────────────────────────────────────
# 2. Remove old Docker images
# ──────────────────────────────────────────────
echo -e "${YELLOW}▶ Pruning unused Docker images...${NC}"
docker image prune -af 2>/dev/null || true
echo -e "${GREEN}  ✅ Images pruned.${NC}"

# ──────────────────────────────────────────────
# 3. Remove unused Docker networks
# ──────────────────────────────────────────────
echo -e "${YELLOW}▶ Pruning unused Docker networks...${NC}"
docker network prune -f 2>/dev/null || true
echo -e "${GREEN}  ✅ Networks pruned.${NC}"

# ──────────────────────────────────────────────
# 4. Handle Docker volumes — ASK before deleting
# ──────────────────────────────────────────────
echo ""
echo -e "${YELLOW}▶ Listing existing Docker volumes:${NC}"
VOLUMES=$(docker volume ls -q 2>/dev/null || true)
if [ -n "$VOLUMES" ]; then
  docker volume ls
  echo ""
  echo -e "${RED}⚠️  WARNING: The volumes above may contain PostgreSQL database data!${NC}"
  echo -e "${RED}   Deleting them is IRREVERSIBLE. Old data might be valuable.${NC}"
  echo ""
  read -r -p "Do you want to DELETE ALL Docker volumes? (type 'yes' to confirm): " CONFIRM_VOLUMES
  if [ "$CONFIRM_VOLUMES" = "yes" ]; then
    docker volume prune -f 2>/dev/null || true
    echo -e "${GREEN}  ✅ All volumes deleted.${NC}"
  else
    echo "  ℹ️  Volumes were PRESERVED (no data was deleted)."
  fi
else
  echo "  ℹ️  No Docker volumes found."
fi

# ──────────────────────────────────────────────
# 5. Remove old Let's Encrypt / SSL certificates
# ──────────────────────────────────────────────
echo ""
echo -e "${YELLOW}▶ Checking for old SSL certificates...${NC}"
if [ -d "/etc/letsencrypt" ]; then
  echo -e "${RED}⚠️  Found /etc/letsencrypt on the host filesystem.${NC}"
  read -r -p "Do you want to DELETE old SSL certificates in /etc/letsencrypt? (type 'yes' to confirm): " CONFIRM_CERTS
  if [ "$CONFIRM_CERTS" = "yes" ]; then
    rm -rf /etc/letsencrypt
    echo -e "${GREEN}  ✅ Old SSL certificates removed from /etc/letsencrypt.${NC}"
  else
    echo "  ℹ️  Certificates in /etc/letsencrypt were preserved."
  fi
else
  echo "  ℹ️  No /etc/letsencrypt directory found (certificates are managed in Docker volumes)."
fi

# ──────────────────────────────────────────────
# 6. Remove old host-mounted Nginx configs
# ──────────────────────────────────────────────
echo ""
echo -e "${YELLOW}▶ Checking for old host-mounted Nginx configs...${NC}"
OLD_NGINX_DIRS=("/etc/nginx/conf.d" "/etc/nginx/sites-enabled" "/etc/nginx/sites-available")
FOUND_NGINX=0
for dir in "${OLD_NGINX_DIRS[@]}"; do
  if [ -d "$dir" ] && [ "$(ls -A "$dir" 2>/dev/null)" ]; then
    echo "  Found config files in: $dir"
    FOUND_NGINX=1
  fi
done

if [ $FOUND_NGINX -eq 1 ]; then
  read -r -p "Do you want to remove old host-based Nginx configs? (type 'yes' to confirm): " CONFIRM_NGINX
  if [ "$CONFIRM_NGINX" = "yes" ]; then
    for dir in "${OLD_NGINX_DIRS[@]}"; do
      rm -rf "$dir"/*.conf 2>/dev/null || true
    done
    echo -e "${GREEN}  ✅ Old Nginx configs removed.${NC}"
  else
    echo "  ℹ️  Nginx configs preserved."
  fi
else
  echo "  ℹ️  No host-based Nginx config files found."
fi

# ──────────────────────────────────────────────
# 7. Summary
# ──────────────────────────────────────────────
echo ""
echo "=============================================="
echo -e "${GREEN}📊 Cleanup Summary${NC}"
echo "  ✅ Containers:   Stopped and removed"
echo "  ✅ Images:       Pruned"
echo "  ✅ Networks:     Pruned"
echo "  ℹ️  Volumes:     Handled with confirmation"
echo "  ℹ️  SSL Certs:   Handled with confirmation"
echo "  ℹ️  Nginx:       Handled with confirmation"
echo ""
echo -e "${GREEN}🎉 VPS is ready for a fresh SaszetApp deployment!${NC}"
echo ""
echo "Verify clean state with:"
echo "  docker ps"
echo "  docker images"
echo "  docker volume ls"
echo "  docker network ls"
