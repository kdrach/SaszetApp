#!/bin/bash
# bootstrap-vps.sh
# All-in-one bootstrap for SaszetApp production deployment on a fresh (or existing) VPS.
# Idempotent — safe to re-run on an already-running deployment.
#
# Usage:
#   ./infrastructure/scripts/bootstrap-vps.sh [OPTIONS]
#
# Options:
#   --skip-cleanup   Skip Step 2: cleanup old deployment (recommended for a truly fresh VPS)
#   --skip-build     Skip Step 4: Docker image build (config-only redeployment)
#   --skip-build     Skip Step 4: Docker image build (config-only redeployment)
#   --staging        (Deprecated) Caddy handles SSL automatically, flag ignored
#   --help           Print this help and exit

set -euo pipefail

# ══════════════════════════════════════════════════════════════════════════════
# GLOBALS & SETUP
# ══════════════════════════════════════════════════════════════════════════════

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
TIMESTAMP=$(date +%Y-%m-%d_%H%M%S)
LOG_DIR="$REPO_ROOT/logs"
LOG_FILE="$LOG_DIR/bootstrap-${TIMESTAMP}.log"

# Flags
SKIP_CLEANUP=0
SKIP_BUILD=0
STAGING=""

# ANSI color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

# ══════════════════════════════════════════════════════════════════════════════
# COLOR HELPER FUNCTIONS
# ══════════════════════════════════════════════════════════════════════════════

info()    { echo -e "${BLUE}[INFO]${NC}  $*"; }
success() { echo -e "${GREEN}[OK]${NC}    $*"; }
warn()    { echo -e "${YELLOW}[WARN]${NC}  $*"; }
error()   { echo -e "${RED}[ERROR]${NC} $*" >&2; exit 1; }
step()    { echo ""; echo -e "${BOLD}${CYAN}━━━ $* ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"; }

# ══════════════════════════════════════════════════════════════════════════════
# TRAP — catch unexpected exits
# ══════════════════════════════════════════════════════════════════════════════

trap 'on_error $? $LINENO' ERR

on_error() {
  local exit_code=$1
  local line=$2
  echo ""
  echo -e "${RED}══════════════════════════════════════════════════${NC}"
  echo -e "${RED}  ❌ Bootstrap FAILED at line $line (exit code: $exit_code)${NC}"
  echo -e "${RED}══════════════════════════════════════════════════${NC}"
  echo ""
  warn "Showing recent Docker container logs for diagnosis..."
  docker compose --env-file "$REPO_ROOT/.env.prod" \
    -f "$REPO_ROOT/docker-compose.yml" \
    -f "$REPO_ROOT/docker-compose.prod.yml" \
    ps --format "table {{.Name}}\t{{.Status}}" 2>/dev/null || true
  echo ""
  warn "To see logs for a specific service:"
  echo "  sudo docker compose --env-file .env.prod -f docker-compose.yml -f docker-compose.prod.yml logs <service>"
  echo ""
  warn "Full run log saved to: $LOG_FILE"
  echo ""
  info "Recovery options:"
  echo "  • Fix the issue, then re-run:  ./infrastructure/scripts/bootstrap-vps.sh --skip-cleanup --skip-build"
  echo "  • Start fresh:                 ./infrastructure/scripts/bootstrap-vps.sh"
  echo ""
  exit "$exit_code"
}

# ══════════════════════════════════════════════════════════════════════════════
# PARSE ARGUMENTS
# ══════════════════════════════════════════════════════════════════════════════

print_help() {
  cat <<EOF

${BOLD}SaszetApp VPS Bootstrap${NC}

  One-command deployment to a production VPS. Idempotent — safe to re-run.

${BOLD}USAGE${NC}
  ./infrastructure/scripts/bootstrap-vps.sh [OPTIONS]

${BOLD}OPTIONS${NC}
  --skip-cleanup   Skip Step 2: cleanup old deployment (use on a fresh VPS)
  --skip-build     Skip Step 4: Docker image build (config-only redeploy)
  --staging        (Deprecated) Caddy handles SSL automatically, flag ignored
  --help           Show this help and exit

${BOLD}EXAMPLES${NC}
  # First-time deployment on a fresh VPS
  ./infrastructure/scripts/bootstrap-vps.sh --skip-cleanup

  # Test SSL flow without hitting rate limits
  ./infrastructure/scripts/bootstrap-vps.sh --skip-cleanup --staging

  # Redeploy without rebuilding images
  ./infrastructure/scripts/bootstrap-vps.sh --skip-cleanup --skip-build

${BOLD}REQUIREMENTS${NC}
  - Docker Engine + Docker Compose v2
  - Git
  - .env.prod (copy from .env.prod.example and fill in values)
  - DNS A records pointing to this VPS for all 4 subdomains

EOF
}

while [[ $# -gt 0 ]]; do
  case $1 in
    --skip-cleanup) SKIP_CLEANUP=1; shift ;;
    --skip-build)   SKIP_BUILD=1;   shift ;;
    --staging)      STAGING="--staging"; shift ;;
    --help|-h)      print_help; exit 0 ;;
    *)
      echo -e "${RED}Unknown option: $1${NC}"
      echo "Run with --help for usage."
      exit 1
      ;;
  esac
done

# ══════════════════════════════════════════════════════════════════════════════
# SETUP LOGGING — tee all output to timestamped log file
# ══════════════════════════════════════════════════════════════════════════════

mkdir -p "$LOG_DIR"
exec > >(tee -a "$LOG_FILE") 2>&1

echo ""
echo -e "${BOLD}${CYAN}╔══════════════════════════════════════════════════════╗${NC}"
echo -e "${BOLD}${CYAN}║     SaszetApp — VPS Bootstrap                        ║${NC}"
echo -e "${BOLD}${CYAN}║     $(date '+%Y-%m-%d %H:%M:%S')                              ║${NC}"
echo -e "${BOLD}${CYAN}╚══════════════════════════════════════════════════════╝${NC}"
echo ""
info "Full log: $LOG_FILE"

# ══════════════════════════════════════════════════════════════════════════════
# STEP 1 — PRE-FLIGHT CHECKS
# ══════════════════════════════════════════════════════════════════════════════

step "STEP 1 — PRE-FLIGHT CHECKS"

preflight_checks() {
  local FAILED=0

  # Docker installed?
  if ! command -v docker &>/dev/null; then
    echo -e "${RED}  ❌ Docker is not installed.${NC}"
    echo "     Fix: curl -fsSL https://get.docker.com | sh"
    FAILED=1
  else
    success "Docker found: $(docker --version)"
  fi

  # Docker daemon running?
  if ! docker info &>/dev/null 2>&1; then
    echo -e "${RED}  ❌ Docker daemon is not running.${NC}"
    echo "     Fix: sudo systemctl start docker"
    FAILED=1
  else
    success "Docker daemon is running."
  fi

  # Docker Compose v2 available?
  if ! docker compose version &>/dev/null 2>&1; then
    echo -e "${RED}  ❌ Docker Compose v2 is not available.${NC}"
    echo "     Fix: Upgrade Docker Engine to 20.10+ which includes compose v2"
    FAILED=1
  else
    success "Docker Compose v2 found: $(docker compose version --short)"
  fi

  # Git installed?
  if ! command -v git &>/dev/null; then
    echo -e "${RED}  ❌ Git is not installed.${NC}"
    echo "     Fix: apt-get install -y git"
    FAILED=1
  else
    success "Git found: $(git --version)"
  fi

  # .env.prod exists?
  if [ ! -f "$REPO_ROOT/.env.prod" ]; then
    warn ".env.prod not found at $REPO_ROOT/.env.prod"
    echo ""
    # Offer to auto-generate
    if [ -t 0 ]; then
      # Interactive terminal — ask the user
      read -r -p "  Would you like to auto-generate .env.prod with secure random secrets? [Y/n]: " GENERATE_ANSWER
      GENERATE_ANSWER="${GENERATE_ANSWER:-Y}"
      if [[ "$GENERATE_ANSWER" =~ ^[Yy] ]]; then
        info "Delegating to generate-env.sh..."
        chmod +x "$SCRIPT_DIR/generate-env.sh"
        "$SCRIPT_DIR/generate-env.sh"
        success ".env.prod auto-generated."
      else
        echo -e "${RED}  ❌ .env.prod is required.${NC}"
        echo "     Fix: ./infrastructure/scripts/generate-env.sh"
        echo "     Or:  cp .env.prod.example .env.prod && nano .env.prod"
        FAILED=1
      fi
    else
      # Non-interactive (piped/CI) — fail with instructions
      echo -e "${RED}  ❌ .env.prod not found and running non-interactively.${NC}"
      echo "     Fix: ./infrastructure/scripts/generate-env.sh --non-interactive --domain yourdomain.com --email you@email.com"
      echo "     Or:  cp .env.prod.example .env.prod && nano .env.prod"
      FAILED=1
    fi
  else
    success ".env.prod found."

    # Validate required vars are non-empty
    local REQUIRED_VARS=(
      POSTGRES_USER POSTGRES_PASSWORD POSTGRES_DB
      KEYCLOAK_USER KEYCLOAK_PASSWORD KEYCLOAK_DB
      KEYCLOAK_ADMIN KEYCLOAK_ADMIN_PASSWORD
      ENCRYPTION_KEY CERTBOT_EMAIL DOMAIN
    )
    # shellcheck source=/dev/null
    source <(grep -E '^[A-Z_]+=.' "$REPO_ROOT/.env.prod" | sed 's/^/export /')

    for var in "${REQUIRED_VARS[@]}"; do
      val="${!var:-}"
      if [ -z "$val" ]; then
        echo -e "${RED}  ❌ Required variable '$var' is missing or empty in .env.prod${NC}"
        FAILED=1
      fi
    done

    if [ "$FAILED" -eq 0 ]; then
      success "All required .env.prod variables are set."
    fi
  fi

  # Internet connectivity?
  if curl -s --max-time 5 https://hub.docker.com &>/dev/null; then
    success "Internet connectivity verified."
  else
    warn "Cannot reach hub.docker.com — internet may be unavailable."
    warn "Docker image pulls may fail. Continuing anyway..."
  fi

  if [ "$FAILED" -ne 0 ]; then
    echo ""
    error "Pre-flight checks failed. Fix the issues above and re-run."
  fi

  success "All pre-flight checks passed."
}

preflight_checks

# Load .env.prod for use throughout the script
# shellcheck source=/dev/null
set -o allexport
source "$REPO_ROOT/.env.prod"
set +o allexport

# ══════════════════════════════════════════════════════════════════════════════
# STEP 2 — CLEANUP OLD DEPLOYMENT
# ══════════════════════════════════════════════════════════════════════════════

step "STEP 2 — CLEANUP OLD DEPLOYMENT"

if [ "$SKIP_CLEANUP" -eq 1 ]; then
  warn "Skipping cleanup (--skip-cleanup flag set)."
else
  info "Delegating to cleanup-old-deployment.sh..."
  chmod +x "$SCRIPT_DIR/cleanup-old-deployment.sh"
  # Run non-interactively: auto-answer 'no' to volume deletion (preserve data)
  echo "no" | bash "$SCRIPT_DIR/cleanup-old-deployment.sh" || true
  success "Cleanup complete."
fi

# ══════════════════════════════════════════════════════════════════════════════
# STEP 3 — PULL LATEST CODE
# ══════════════════════════════════════════════════════════════════════════════

step "STEP 3 — PULL LATEST CODE"

cd "$REPO_ROOT"
info "Pulling latest code from main..."
git pull origin main
success "Code is up to date."

# ══════════════════════════════════════════════════════════════════════════════
# STEP 4 — BUILD DOCKER IMAGES
# ══════════════════════════════════════════════════════════════════════════════

step "STEP 4 — BUILD DOCKER IMAGES"

if [ "$SKIP_BUILD" -eq 1 ]; then
  warn "Skipping build (--skip-build flag set)."
else
  info "Building Docker images (--no-cache)..."
  docker compose --env-file "$REPO_ROOT/.env.prod" \
    -f "$REPO_ROOT/docker-compose.yml" \
    -f "$REPO_ROOT/docker-compose.prod.yml" \
    build --no-cache
  success "Docker images built."
fi


# ══════════════════════════════════════════════════════════════════════════════
# STEP 6 — START ALL CONTAINERS
# ══════════════════════════════════════════════════════════════════════════════

step "STEP 6 — START ALL CONTAINERS"

info "Starting all services..."
docker compose --env-file "$REPO_ROOT/.env.prod" \
  -f "$REPO_ROOT/docker-compose.yml" \
  -f "$REPO_ROOT/docker-compose.prod.yml" \
  up -d
success "All services started."

# ══════════════════════════════════════════════════════════════════════════════
# STEP 7 — WAIT FOR HEALTH
# ══════════════════════════════════════════════════════════════════════════════

step "STEP 7 — WAITING FOR CONTAINER HEALTH"

wait_for_health() {
  local MAX_WAIT=120
  local POLL_INTERVAL=5
  local ELAPSED=0

  info "Polling container health every ${POLL_INTERVAL}s (timeout: ${MAX_WAIT}s)..."

  while [ "$ELAPSED" -lt "$MAX_WAIT" ]; do
    # Check for any unhealthy containers
    UNHEALTHY=$(docker compose --env-file "$REPO_ROOT/.env.prod" \
      -f "$REPO_ROOT/docker-compose.yml" \
      -f "$REPO_ROOT/docker-compose.prod.yml" \
      ps --format json 2>/dev/null \
      | grep -c -i '"Health":"unhealthy"' || true)

    STARTING=$(docker compose --env-file "$REPO_ROOT/.env.prod" \
      -f "$REPO_ROOT/docker-compose.yml" \
      -f "$REPO_ROOT/docker-compose.prod.yml" \
      ps --format json 2>/dev/null \
      | grep -c -iE '"Health":"(starting|unknown)"' || true)

    if [ "$UNHEALTHY" -gt 0 ]; then
      echo ""
      error "One or more containers are UNHEALTHY after ${ELAPSED}s. See logs above."
    fi

    if [ "$STARTING" -eq 0 ]; then
      echo ""
      success "All containers are healthy!"
      return 0
    fi

    printf "\r  ⏳ Waiting... %ds elapsed (%d container(s) still starting)" "$ELAPSED" "$STARTING"
    sleep "$POLL_INTERVAL"
    ELAPSED=$((ELAPSED + POLL_INTERVAL))
  done

  echo ""
  warn "Health check timed out after ${MAX_WAIT}s. Printing container status..."
  docker compose --env-file "$REPO_ROOT/.env.prod" \
    -f "$REPO_ROOT/docker-compose.yml" \
    -f "$REPO_ROOT/docker-compose.prod.yml" \
    ps

  echo ""
  warn "Printing logs of all services for diagnosis:"
  docker compose --env-file "$REPO_ROOT/.env.prod" \
    -f "$REPO_ROOT/docker-compose.yml" \
    -f "$REPO_ROOT/docker-compose.prod.yml" \
    logs --tail=30

  error "Containers did not become healthy within ${MAX_WAIT}s."
}

wait_for_health

# ══════════════════════════════════════════════════════════════════════════════
# STEP 8 — SMOKE TESTS
# ══════════════════════════════════════════════════════════════════════════════

step "STEP 8 — SMOKE TESTS"

info "Running smoke tests against https://$DOMAIN ..."
chmod +x "$SCRIPT_DIR/smoke-test.sh"

if "$SCRIPT_DIR/smoke-test.sh" "$DOMAIN"; then
  success "All smoke tests passed."
else
  warn "Some smoke tests failed. Printing Docker container status:"
  docker compose --env-file "$REPO_ROOT/.env.prod" \
    -f "$REPO_ROOT/docker-compose.yml" \
    -f "$REPO_ROOT/docker-compose.prod.yml" \
    ps
  error "Smoke tests failed. Check the output above for details."
fi

# ══════════════════════════════════════════════════════════════════════════════
# STEP 9 — FINAL REPORT
# ══════════════════════════════════════════════════════════════════════════════

step "STEP 9 — FINAL REPORT"

# Container status table
echo ""
echo -e "${BOLD}Container Status:${NC}"
docker compose --env-file "$REPO_ROOT/.env.prod" \
  -f "$REPO_ROOT/docker-compose.yml" \
  -f "$REPO_ROOT/docker-compose.prod.yml" \
  ps

# Success banner
echo ""
echo -e "${BOLD}${GREEN}╔══════════════════════════════════════════════════════╗${NC}"
echo -e "${BOLD}${GREEN}║  ✅  SaszetApp is LIVE!                              ║${NC}"
echo -e "${BOLD}${GREEN}╠══════════════════════════════════════════════════════╣${NC}"
echo -e "${BOLD}${GREEN}║  🌐  https://${DOMAIN}                              ║${NC}"
echo -e "${BOLD}${GREEN}║  🔧  https://admin.${DOMAIN}                        ║${NC}"
echo -e "${BOLD}${GREEN}║  🔑  https://auth.${DOMAIN}                         ║${NC}"
echo -e "${BOLD}${GREEN}║  🚀  https://api.${DOMAIN}/health                   ║${NC}"
echo -e "${BOLD}${GREEN}╚══════════════════════════════════════════════════════╝${NC}"
echo ""

# Next-steps cheat sheet
echo -e "${BOLD}Next Steps Cheat Sheet:${NC}"
echo ""
echo -e "  ${CYAN}Update deployment:${NC}"
echo "    ./infrastructure/scripts/deploy.sh"
echo ""
echo -e "  ${CYAN}Backup database:${NC}"
echo "    ./infrastructure/scripts/backup-db.sh"
echo ""
echo -e "  ${CYAN}Run smoke tests:${NC}"
echo "    ./infrastructure/scripts/smoke-test.sh"
echo ""
echo -e "  ${CYAN}View logs:${NC}"
echo "    sudo docker compose --env-file .env.prod -f docker-compose.yml -f docker-compose.prod.yml logs -f"
echo ""
echo -e "  ${CYAN}Rollback:${NC}"
echo "    git log --oneline -5          # find previous commit"
echo "    git checkout <commit-hash>    # roll back"
echo "    ./infrastructure/scripts/deploy.sh"
echo ""

info "Full run log saved to: $LOG_FILE"
echo ""
