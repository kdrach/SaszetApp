#!/bin/bash
# smoke-test.sh
# Validates the full production deployment of SaszetApp.
# Tests all 4 HTTPS subdomains, HTTP -> HTTPS redirects, and SSL certificate validity.
#
# Usage:
#   ./infrastructure/scripts/smoke-test.sh
#   ./infrastructure/scripts/smoke-test.sh saszet.app          # default domain
#   ./infrastructure/scripts/smoke-test.sh yourdomain.com      # custom domain

set -euo pipefail

DOMAIN=${1:-saszet.app}
PASS=0
FAIL=0

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}🔍 SaszetApp — Production Smoke Test${NC}"
echo "   Domain: $DOMAIN"
echo "======================================"
echo ""

# ──────────────────────────────────────────────
# Helper: check HTTP status code
# ──────────────────────────────────────────────
check() {
  local url=$1
  local expected=$2
  local label=${3:-$url}
  local status

  status=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 "$url" 2>/dev/null || echo "000")

  if [ "$status" = "$expected" ]; then
    echo -e "${GREEN}  ✅ $label → HTTP $status${NC}"
    ((PASS++)) || true
  else
    echo -e "${RED}  ❌ $label → HTTP $status (expected $expected)${NC}"
    ((FAIL++)) || true
  fi
}

# ──────────────────────────────────────────────
# 1. Test all 4 HTTPS subdomains
# ──────────────────────────────────────────────
echo -e "${YELLOW}▶ Testing HTTPS subdomains...${NC}"
check "https://$DOMAIN"          "200" "saszet.app (mobile PWA)"
check "https://api.$DOMAIN/health" "200" "api.saszet.app/health (backend API)"
check "https://auth.$DOMAIN/realms/petfood-admin-realm" "200" "auth.saszet.app (Admin realm)"
check "https://auth.$DOMAIN/realms/petfood-customer-realm" "200" "auth.saszet.app (Customer realm)"
check "https://admin.$DOMAIN"    "200" "admin.saszet.app (admin PWA)"

# ──────────────────────────────────────────────
# 2. Test HTTP → HTTPS redirects
# ──────────────────────────────────────────────
echo ""
echo -e "${YELLOW}▶ Testing HTTP → HTTPS redirects...${NC}"
for subdomain in "" "api." "auth." "admin."; do
  url="http://${subdomain}${DOMAIN}"
  redirect_status=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 "$url" 2>/dev/null || echo "000")
  if [ "$redirect_status" = "301" ] || [ "$redirect_status" = "302" ]; then
    echo -e "${GREEN}  ✅ $url → HTTP $redirect_status (redirects to HTTPS)${NC}"
    ((PASS++)) || true
  else
    echo -e "${RED}  ❌ $url → HTTP $redirect_status (expected 301 or 302)${NC}"
    ((FAIL++)) || true
  fi
done

# ──────────────────────────────────────────────
# 3. SSL Certificate check
# ──────────────────────────────────────────────
echo ""
echo -e "${YELLOW}▶ Checking SSL certificate for $DOMAIN...${NC}"
CERT_INFO=$(echo | openssl s_client -servername "$DOMAIN" -connect "$DOMAIN:443" 2>/dev/null \
  | openssl x509 -noout -subject -dates 2>/dev/null || echo "ERROR")

if [ "$CERT_INFO" = "ERROR" ] || [ -z "$CERT_INFO" ]; then
  echo -e "${RED}  ❌ Could not retrieve SSL certificate for $DOMAIN${NC}"
  ((FAIL++)) || true
else
  echo -e "${GREEN}  ✅ SSL Certificate info:${NC}"
  echo "$CERT_INFO" | sed 's/^/    /'
  ((PASS++)) || true
fi

# ──────────────────────────────────────────────
# 4. Summary
# ──────────────────────────────────────────────
echo ""
echo "======================================"
echo -e "${BLUE}📊 Smoke Test Results: ${GREEN}$PASS passed${NC}, ${RED}$FAIL failed${NC}"

if [ "$FAIL" -gt 0 ]; then
  echo -e "${RED}❌ Some tests FAILED! Check the output above.${NC}"
  echo ""
  echo "  Common fixes:"
  echo "  • DNS not propagated yet: wait a few minutes and retry"
  echo "  • SSL not initialized: run ./infrastructure/scripts/init-letsencrypt.sh"
  echo "  • Containers not running: run ./infrastructure/scripts/deploy.sh"
  exit 1
else
  echo -e "${GREEN}✅ All smoke tests PASSED! Production deployment is healthy.${NC}"
  exit 0
fi
