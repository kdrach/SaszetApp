#!/bin/bash
# backup-db.sh
# Creates a compressed PostgreSQL dump of the SaszetApp application database.
# Keeps the last 7 backups, deleting older ones automatically.
#
# Usage:
#   ./infrastructure/scripts/backup-db.sh
#
# Requires: POSTGRES_USER env var (from .env.prod) or pass it as first argument.

set -euo pipefail

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Read POSTGRES_USER from environment or .env.prod
if [ -f .env.prod ]; then
  # shellcheck source=/dev/null
  source <(grep -E '^POSTGRES_USER=' .env.prod)
fi

POSTGRES_USER="${POSTGRES_USER:-saszetapp}"
BACKUP_DIR="./backups"
mkdir -p "$BACKUP_DIR"

TIMESTAMP=$(date +%Y-%m-%d_%H%M%S)
FILENAME="app-db-${TIMESTAMP}.sql.gz"

echo -e "${GREEN}💾 SaszetApp — Database Backup${NC}"
echo "================================"
echo ""
echo -e "${YELLOW}▶ Backing up app-db to ${BACKUP_DIR}/${FILENAME}...${NC}"

docker exec app-db pg_dumpall -U "$POSTGRES_USER" | gzip > "${BACKUP_DIR}/${FILENAME}"

echo -e "${GREEN}  ✅ Backup saved to ${BACKUP_DIR}/${FILENAME}${NC}"

# Show backup size
BACKUP_SIZE=$(du -sh "${BACKUP_DIR}/${FILENAME}" | cut -f1)
echo "  📦 Size: ${BACKUP_SIZE}"

# ──────────────────────────────────────────────
# Keep only last 7 backups
# ──────────────────────────────────────────────
echo ""
echo -e "${YELLOW}▶ Cleaning up old backups (keeping last 7)...${NC}"
DELETED=$(ls -t "${BACKUP_DIR}"/app-db-*.sql.gz 2>/dev/null | tail -n +8 | wc -l)
ls -t "${BACKUP_DIR}"/app-db-*.sql.gz 2>/dev/null | tail -n +8 | xargs rm -f 2>/dev/null || true

if [ "$DELETED" -gt 0 ]; then
  echo -e "${GREEN}  🧹 Removed $DELETED old backup(s).${NC}"
else
  echo "  ℹ️  No old backups to remove."
fi

echo ""
echo -e "${GREEN}✅ Backup complete: ${BACKUP_DIR}/${FILENAME}${NC}"
echo ""
echo "  Existing backups:"
ls -lh "${BACKUP_DIR}"/app-db-*.sql.gz 2>/dev/null | awk '{print "    " $5 "  " $9}' || echo "    (none)"
