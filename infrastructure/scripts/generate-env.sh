#!/bin/bash
# generate-env.sh
# Generates a production .env.prod file with auto-generated secrets.
# Safe to run on a fresh clone — will NOT overwrite an existing .env.prod.
#
# Auto-generated (random secrets):
#   POSTGRES_PASSWORD, KEYCLOAK_PASSWORD, KEYCLOAK_ADMIN_PASSWORD, ENCRYPTION_KEY
#
# Prompted from user (external/integration — cannot be guessed):
#   DOMAIN, CERTBOT_EMAIL
#
# Auto-computed:
#   KC_HOSTNAME (derived from DOMAIN)
#
# Defaults preserved:
#   POSTGRES_USER, POSTGRES_DB, KEYCLOAK_USER, KEYCLOAK_DB, KEYCLOAK_ADMIN
#
# NOT generated (user must configure manually after):
#   SMTP_HOST, SMTP_PORT, SMTP_USER, SMTP_PASSWORD, SMTP_FROM
#
# Usage:
#   ./infrastructure/scripts/generate-env.sh
#   ./infrastructure/scripts/generate-env.sh --non-interactive --domain saszet.app --email admin@saszet.app

set -euo pipefail

# ──────────────────────────────────────────────
# Globals
# ──────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
ENV_EXAMPLE="$REPO_ROOT/.env.prod.example"
ENV_PROD="$REPO_ROOT/.env.prod"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

# Flags
NON_INTERACTIVE=0
ARG_DOMAIN=""
ARG_EMAIL=""

# ──────────────────────────────────────────────
# Parse Arguments
# ──────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case $1 in
    --non-interactive) NON_INTERACTIVE=1; shift ;;
    --domain)          ARG_DOMAIN="$2"; shift 2 ;;
    --email)           ARG_EMAIL="$2"; shift 2 ;;
    --help|-h)
      cat <<EOF

${BOLD}generate-env.sh${NC} — Auto-generate .env.prod with secure startup secrets

${BOLD}USAGE${NC}
  ./infrastructure/scripts/generate-env.sh [OPTIONS]

${BOLD}OPTIONS${NC}
  --non-interactive    Don't prompt; use --domain and --email flags or defaults
  --domain <domain>    Production domain (default: saszet.app)
  --email  <email>     Certbot email for Let's Encrypt (default: admin@saszet.app)
  --help               Show this help and exit

${BOLD}EXAMPLES${NC}
  # Interactive mode (prompts for domain and email)
  ./infrastructure/scripts/generate-env.sh

  # Non-interactive (CI/scripted usage)
  ./infrastructure/scripts/generate-env.sh --non-interactive --domain saszet.app --email admin@saszet.app

EOF
      exit 0
      ;;
    *)
      echo -e "${RED}Unknown option: $1${NC}"
      echo "Run with --help for usage."
      exit 1
      ;;
  esac
done

# ──────────────────────────────────────────────
# Pre-checks
# ──────────────────────────────────────────────
echo ""
echo -e "${BOLD}${CYAN}🔐 SaszetApp — Production Secrets Generator${NC}"
echo "============================================"
echo ""

# Check .env.prod.example exists
if [ ! -f "$ENV_EXAMPLE" ]; then
  echo -e "${RED}❌ .env.prod.example not found at $ENV_EXAMPLE${NC}"
  echo "   Are you running this from the repository root?"
  exit 1
fi

# Check .env.prod does NOT exist (idempotency guard)
if [ -f "$ENV_PROD" ]; then
  echo -e "${YELLOW}⚠️  .env.prod already exists at $ENV_PROD${NC}"
  echo ""
  echo "   This script will NOT overwrite existing secrets."
  echo "   If you want to regenerate, delete .env.prod first:"
  echo "     rm .env.prod"
  echo "     ./infrastructure/scripts/generate-env.sh"
  echo ""
  exit 0
fi

# Check openssl is available
if ! command -v openssl &>/dev/null; then
  echo -e "${RED}❌ openssl is not installed (required for secret generation)${NC}"
  echo "   Fix: apt-get install -y openssl"
  exit 1
fi

# ──────────────────────────────────────────────
# Step 1 — Collect user input (domain + email)
# ──────────────────────────────────────────────
DEFAULT_DOMAIN="saszet.app"
DEFAULT_EMAIL="admin@saszet.app"

if [ "$NON_INTERACTIVE" -eq 1 ]; then
  DOMAIN="${ARG_DOMAIN:-$DEFAULT_DOMAIN}"
  CERTBOT_EMAIL="${ARG_EMAIL:-$DEFAULT_EMAIL}"
else
  echo -e "${BLUE}▶ Configure external settings${NC}"
  echo ""

  # Prompt for DOMAIN
  read -r -p "  Production domain [$DEFAULT_DOMAIN]: " DOMAIN
  DOMAIN="${DOMAIN:-$DEFAULT_DOMAIN}"

  # Prompt for CERTBOT_EMAIL
  read -r -p "  Certbot email for Let's Encrypt [$DEFAULT_EMAIL]: " CERTBOT_EMAIL
  CERTBOT_EMAIL="${CERTBOT_EMAIL:-$DEFAULT_EMAIL}"

  echo ""
fi

# Auto-compute KC_HOSTNAME
KC_HOSTNAME="auth.${DOMAIN}"

echo -e "${GREEN}  ✅ Domain:     ${DOMAIN}${NC}"
echo -e "${GREEN}  ✅ Email:      ${CERTBOT_EMAIL}${NC}"
echo -e "${GREEN}  ✅ KC_HOSTNAME: ${KC_HOSTNAME}${NC}"
echo ""

# ──────────────────────────────────────────────
# Step 2 — Generate random secrets
# ──────────────────────────────────────────────
echo -e "${BLUE}▶ Generating cryptographically strong secrets...${NC}"
echo ""

POSTGRES_PASSWORD=$(openssl rand -base64 32)
KEYCLOAK_PASSWORD=$(openssl rand -base64 32)
KEYCLOAK_ADMIN_PASSWORD=$(openssl rand -base64 32)
ENCRYPTION_KEY=$(openssl rand -base64 32)

echo -e "${GREEN}  ✅ POSTGRES_PASSWORD       generated (44 chars)${NC}"
echo -e "${GREEN}  ✅ KEYCLOAK_PASSWORD        generated (44 chars)${NC}"
echo -e "${GREEN}  ✅ KEYCLOAK_ADMIN_PASSWORD  generated (44 chars)${NC}"
echo -e "${GREEN}  ✅ ENCRYPTION_KEY           generated (AES-256 compatible)${NC}"
echo ""

# ──────────────────────────────────────────────
# Step 3 — Create .env.prod from template
# ──────────────────────────────────────────────
echo -e "${BLUE}▶ Creating .env.prod from template...${NC}"

cp "$ENV_EXAMPLE" "$ENV_PROD"

# Replace placeholder values with generated/configured ones using sed
# Use | as sed delimiter to avoid conflicts with base64 characters (/, +, =)
sed -i "s|POSTGRES_PASSWORD=.*|POSTGRES_PASSWORD=${POSTGRES_PASSWORD}|" "$ENV_PROD"
sed -i "s|KEYCLOAK_PASSWORD=.*|KEYCLOAK_PASSWORD=${KEYCLOAK_PASSWORD}|" "$ENV_PROD"
sed -i "s|KEYCLOAK_ADMIN_PASSWORD=.*|KEYCLOAK_ADMIN_PASSWORD=${KEYCLOAK_ADMIN_PASSWORD}|" "$ENV_PROD"
sed -i "s|ENCRYPTION_KEY=.*|ENCRYPTION_KEY=${ENCRYPTION_KEY}|" "$ENV_PROD"
sed -i "s|DOMAIN=.*|DOMAIN=${DOMAIN}|" "$ENV_PROD"
sed -i "s|CERTBOT_EMAIL=.*|CERTBOT_EMAIL=${CERTBOT_EMAIL}|" "$ENV_PROD"
sed -i "s|KC_HOSTNAME=.*|KC_HOSTNAME=${KC_HOSTNAME}|" "$ENV_PROD"

echo -e "${GREEN}  ✅ .env.prod created at: $ENV_PROD${NC}"
echo ""

# ──────────────────────────────────────────────
# Step 4 — Summary
# ──────────────────────────────────────────────
echo "============================================"
echo -e "${BOLD}${GREEN}✅ Production secrets generated successfully!${NC}"
echo ""
echo -e "${BOLD}Auto-generated secrets:${NC}"
echo "  • POSTGRES_PASSWORD       — random (unique to this deployment)"
echo "  • KEYCLOAK_PASSWORD       — random (unique to this deployment)"
echo "  • KEYCLOAK_ADMIN_PASSWORD — random (unique to this deployment)"
echo "  • ENCRYPTION_KEY          — random AES-256 key"
echo ""
echo -e "${BOLD}Configured values:${NC}"
echo "  • DOMAIN           = ${DOMAIN}"
echo "  • CERTBOT_EMAIL    = ${CERTBOT_EMAIL}"
echo "  • KC_HOSTNAME      = ${KC_HOSTNAME}"
echo ""
echo -e "${BOLD}Defaults kept (editable in .env.prod):${NC}"
echo "  • POSTGRES_USER    = saszetapp"
echo "  • POSTGRES_DB      = saszetapp_db"
echo "  • KEYCLOAK_USER    = keycloak"
echo "  • KEYCLOAK_DB      = keycloak_db"
echo "  • KEYCLOAK_ADMIN   = admin"
echo ""
echo -e "${YELLOW}⚠️  Manual configuration still needed:${NC}"
echo "  • SMTP_HOST, SMTP_PORT, SMTP_USER, SMTP_PASSWORD, SMTP_FROM"
echo "  Edit .env.prod to configure email sending:"
echo "    nano .env.prod"
echo ""
echo -e "${BOLD}Next step:${NC}"
echo "  ./infrastructure/scripts/bootstrap-vps.sh --skip-cleanup"
echo ""
