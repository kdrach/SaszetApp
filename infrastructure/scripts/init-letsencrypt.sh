#!/bin/bash
# init-letsencrypt.sh
# First-time SSL certificate setup using Let's Encrypt for all 4 SaszetApp subdomains.
# Run this ONCE before starting production for the first time.
#
# Usage:
#   ./infrastructure/scripts/init-letsencrypt.sh --email your@email.com
#   ./infrastructure/scripts/init-letsencrypt.sh --email your@email.com --staging   # test against LE staging
#   ./infrastructure/scripts/init-letsencrypt.sh --email your@email.com --domain yourdomain.com

set -euo pipefail

# ──────────────────────────────────────────────
# Defaults
# ──────────────────────────────────────────────
DOMAIN="saszet.app"
EMAIL=""
STAGING=0

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# ──────────────────────────────────────────────
# Parse arguments
# ──────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case $1 in
    --email)
      EMAIL="$2"
      shift 2
      ;;
    --domain)
      DOMAIN="$2"
      shift 2
      ;;
    --staging)
      STAGING=1
      shift
      ;;
    *)
      echo "Unknown argument: $1"
      echo "Usage: $0 --email <email> [--domain <domain>] [--staging]"
      exit 1
      ;;
  esac
done

if [ -z "$EMAIL" ]; then
  echo -e "${RED}❌ Error: --email is required.${NC}"
  echo "Usage: $0 --email your@email.com [--domain saszet.app] [--staging]"
  exit 1
fi

# All 4 subdomains
DOMAINS=("$DOMAIN" "admin.$DOMAIN" "auth.$DOMAIN" "api.$DOMAIN")

# Staging flag for certbot
STAGING_FLAG=""
if [ "$STAGING" -eq 1 ]; then
  STAGING_FLAG="--staging"
  echo -e "${YELLOW}⚠️  Running in STAGING mode (Let's Encrypt staging servers — no rate limits).${NC}"
  echo -e "${YELLOW}   Staging certs are NOT trusted by browsers. Remove --staging for production.${NC}"
fi

echo ""
echo -e "${GREEN}🔒 Initializing Let's Encrypt SSL certificates${NC}"
echo "   Domain: $DOMAIN"
echo "   Email:  $EMAIL"
echo "   Subdomains: ${DOMAINS[*]}"
echo ""

# ──────────────────────────────────────────────
# 1. Download recommended TLS parameters
# ──────────────────────────────────────────────
echo -e "${YELLOW}▶ Downloading recommended TLS parameters...${NC}"
LETSENCRYPT_DIR="./certbot-conf"
mkdir -p "$LETSENCRYPT_DIR"

if [ ! -e "$LETSENCRYPT_DIR/options-ssl-nginx.conf" ] || [ ! -e "$LETSENCRYPT_DIR/ssl-dhparams.pem" ]; then
  curl -s https://raw.githubusercontent.com/certbot/certbot/master/certbot-nginx/certbot_nginx/_internal/tls_configs/options-ssl-nginx.conf \
    > "$LETSENCRYPT_DIR/options-ssl-nginx.conf"
  curl -s https://raw.githubusercontent.com/certbot/certbot/master/certbot/certbot/ssl-dhparams.pem \
    > "$LETSENCRYPT_DIR/ssl-dhparams.pem"
  echo -e "${GREEN}  ✅ TLS parameters downloaded.${NC}"
else
  echo "  ℹ️  TLS parameters already exist, skipping."
fi

# ──────────────────────────────────────────────
# 2. Create dummy self-signed certificates so Nginx can start
# ──────────────────────────────────────────────
echo ""
echo -e "${YELLOW}▶ Creating temporary self-signed certificates...${NC}"
for domain in "${DOMAINS[@]}"; do
  CERT_PATH="$LETSENCRYPT_DIR/live/$domain"
  if [ -d "$CERT_PATH" ]; then
    echo "  ℹ️  Certificate for $domain already exists, skipping dummy creation."
    continue
  fi
  mkdir -p "$CERT_PATH"
  openssl req -x509 -nodes -newkey rsa:4096 -days 1 \
    -keyout "$CERT_PATH/privkey.pem" \
    -out "$CERT_PATH/fullchain.pem" \
    -subj "/CN=$domain" 2>/dev/null
  echo -e "${GREEN}  ✅ Dummy cert created for $domain${NC}"
done

# ──────────────────────────────────────────────
# 3. Start Nginx with dummy certificates
# ──────────────────────────────────────────────
echo ""
echo -e "${YELLOW}▶ Starting nginx-proxy container...${NC}"
docker compose --env-file .env.prod -f docker-compose.yml -f docker-compose.prod.yml up --force-recreate -d nginx-proxy
echo "  ⏳ Waiting 5s for Nginx to become ready..."
sleep 5
echo -e "${GREEN}  ✅ Nginx started.${NC}"

# ──────────────────────────────────────────────
# 4. Delete the dummy certificates
# ──────────────────────────────────────────────
echo ""
echo -e "${YELLOW}▶ Removing temporary self-signed certificates...${NC}"
for domain in "${DOMAINS[@]}"; do
  rm -rf "$LETSENCRYPT_DIR/live/$domain"
  echo -e "${GREEN}  ✅ Dummy cert for $domain removed.${NC}"
done

# ──────────────────────────────────────────────
# 5. Request real certificates from Let's Encrypt
# ──────────────────────────────────────────────
echo ""
echo -e "${YELLOW}▶ Requesting real Let's Encrypt certificates...${NC}"
for domain in "${DOMAINS[@]}"; do
  echo "  📜 Requesting cert for: $domain"
  docker compose --env-file .env.prod -f docker-compose.yml -f docker-compose.prod.yml run --rm certbot certonly \
    --webroot \
    --webroot-path=/var/www/certbot \
    $STAGING_FLAG \
    --email "$EMAIL" \
    --agree-tos \
    --no-eff-email \
    --non-interactive \
    --keep-until-expiring \
    -d "$domain" \
    2>&1 | sed 's/^/    /'
  echo -e "${GREEN}  ✅ Certificate issued for $domain${NC}"
done

# ──────────────────────────────────────────────
# 6. Reload Nginx to pick up real certificates
# ──────────────────────────────────────────────
echo ""
echo -e "${YELLOW}▶ Reloading Nginx to activate real certificates...${NC}"
docker compose --env-file .env.prod -f docker-compose.yml -f docker-compose.prod.yml exec nginx-proxy nginx -s reload
echo -e "${GREEN}  ✅ Nginx reloaded.${NC}"

# ──────────────────────────────────────────────
# Summary
# ──────────────────────────────────────────────
echo ""
echo "=============================================="
echo -e "${GREEN}🎉 SSL certificates successfully initialized!${NC}"
echo ""
echo "  Certificates issued for:"
for domain in "${DOMAINS[@]}"; do
  echo "    • https://$domain"
done
echo ""
if [ "$STAGING" -eq 1 ]; then
  echo -e "${YELLOW}  ⚠️  STAGING mode was used. Run again without --staging for real production certs.${NC}"
fi
echo ""
echo "  Auto-renewal: Certbot renews every 12h (configured in docker-compose.prod.yml)"
echo "  Nginx reload: Every 6h to pick up renewed certs"
echo ""
echo "  Next step: Run ./infrastructure/scripts/deploy.sh to start all services."
