# SaszetApp — Deployment Guide

## Quick Start — Automated Bootstrap (recommended)

The fastest way to go from a clean VPS to a fully running production deployment:

```bash
# 1. SSH into your VPS
ssh user@<your-vps-ip>

# 2. Clone the repository
git clone https://github.com/kdrach/SaszetApp.git
cd SaszetApp

# 3. Configure secrets (auto-generated)
./infrastructure/scripts/generate-env.sh
# Or manually: cp .env.prod.example .env.prod && nano .env.prod

# 4. Make scripts executable
chmod +x infrastructure/scripts/*.sh

# 5. Run the bootstrap
./infrastructure/scripts/bootstrap-vps.sh --skip-cleanup

# 6. Done! Visit https://saszet.app
```

> **Tip — First time?** Test the SSL flow without hitting Let's Encrypt rate limits:
> ```bash
> ./infrastructure/scripts/bootstrap-vps.sh --skip-cleanup --staging
> ```
> Staging certificates are not trusted by browsers but validate the whole flow. Run without `--staging` afterwards for real certs.

### Bootstrap flags

| Flag | Description |
|------|-------------|
| `--skip-cleanup` | Skip cleanup step (use on a fresh VPS with nothing to wipe) |
| `--skip-build` | Skip image build (config-only redeployment) |
| `--skip-ssl` | Skip SSL initialization (certs already valid) |
| `--staging` | Use Let's Encrypt staging (no rate limits during testing) |
| `--help` | Print full usage and exit |

> The bootstrap is **idempotent** — safe to re-run on an already-running deployment without breaking it.

---

## Overview


This guide describes how to deploy SaszetApp to a VPS using Docker Compose.  
The application consists of 7 containers: `app-db`, `keycloak-db`, `keycloak`, `backend-api`, `frontend-mobile`, `frontend-admin`, and `nginx-proxy` (with `certbot` for SSL).

All 4 public subdomains are served through a single Nginx reverse proxy:

| Subdomain | Service |
|-----------|---------|
| `saszet.app` | Mobile PWA (end-user app) |
| `admin.saszet.app` | Admin Gateway (desktop PWA) |
| `auth.saszet.app` | Keycloak (identity provider) |
| `api.saszet.app` | Backend REST API |

---

## Prerequisites

- VPS running Ubuntu 22.04+ (or Debian 12+)
- Docker Engine installed (`curl -fsSL https://get.docker.com | sh`)
- Docker Compose v2+ installed (included with Docker Engine)
- Domain `saszet.app` registered, with DNS A records pointing to your VPS IP
- SSH access to the VPS
- Git installed on the VPS

---

## Cleaning Up an Old Deployment

If the VPS has a **previous version of SaszetApp** already deployed (e.g., with an expired SSL certificate):

### 1. SSH into your VPS

```bash
ssh user@<your-vps-ip>
```

### 2. Navigate to the old repo directory (if it exists)

```bash
cd /path/to/old/SaszetApp
```

### 3. Run the cleanup script

```bash
chmod +x infrastructure/scripts/cleanup-old-deployment.sh
./infrastructure/scripts/cleanup-old-deployment.sh
```

The script will:
- Stop and remove all running Docker containers
- Prune unused images and networks
- **Ask for confirmation** before deleting Docker volumes (database data)
- **Ask for confirmation** before removing old SSL certificates
- **Ask for confirmation** before removing old host-based Nginx configs

### 4. Verify a clean state


```bash
docker ps          # Should show no containers
docker images      # Should show no images (or minimal base images)
docker volume ls   # Should show no volumes (if you confirmed deletion)
docker network ls  # Should show only default Docker networks
```

---

## DNS Configuration

Set the following **A records** in your domain registrar (e.g., where `saszet.app` was purchased):

| Type | Name    | Value              | TTL  |
|------|---------|--------------------|------|
| A    | @       | `<YOUR_VPS_IP>`    | 3600 |
| A    | admin   | `<YOUR_VPS_IP>`    | 3600 |
| A    | auth    | `<YOUR_VPS_IP>`    | 3600 |
| A    | api     | `<YOUR_VPS_IP>`    | 3600 |

Replace `<YOUR_VPS_IP>` with your VPS's public IP address.

> **Note:** DNS propagation may take up to 48 hours, but usually completes within minutes.

Verify DNS propagation:
```bash
dig saszet.app +short
dig admin.saszet.app +short
dig auth.saszet.app +short
dig api.saszet.app +short
```

---

## First-Time Setup

Follow these steps the **very first time** you deploy SaszetApp on a fresh VPS.

### 1. SSH into VPS

```bash
ssh user@<your-vps-ip>
```

### 2. Run cleanup (if old deployment exists)

```bash
cd /old/SaszetApp  # if applicable
chmod +x infrastructure/scripts/cleanup-old-deployment.sh
./infrastructure/scripts/cleanup-old-deployment.sh
```

### 3. Clone the repository

```bash
git clone https://github.com/kdrach/SaszetApp.git
cd SaszetApp
```

### 4. Create production environment file

```bash
cp .env.prod.example .env.prod
nano .env.prod  # Fill in all required values
```

**Required values in `.env.prod`:**
- `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB` — application database credentials
- `KEYCLOAK_USER`, `KEYCLOAK_PASSWORD`, `KEYCLOAK_DB` — Keycloak database credentials
- `KEYCLOAK_ADMIN`, `KEYCLOAK_ADMIN_PASSWORD` — Keycloak admin console credentials
- `ENCRYPTION_KEY` — 32-byte AES-256 key (generate with `openssl rand -base64 32`)
- `DOMAIN` — your domain (`saszet.app`)
- `CERTBOT_EMAIL` — your email for Let's Encrypt notifications
- `KC_HOSTNAME` — Keycloak hostname (`auth.saszet.app`)

### 5. Make scripts executable

```bash
chmod +x infrastructure/scripts/*.sh
```

### 6. Generate SSL certificates (first-time only)

```bash
# Test with Let's Encrypt staging first (no rate limits)
./infrastructure/scripts/init-letsencrypt.sh --email your@email.com --staging

# If staging succeeds, run for real
./infrastructure/scripts/init-letsencrypt.sh --email your@email.com
```

### 7. Deploy the application

```bash
./infrastructure/scripts/deploy.sh
```

### 8. Run smoke test

```bash
./infrastructure/scripts/smoke-test.sh
```

---

## Regular Deployment (Updates)

When deploying a new version of the application:

```bash
# 1. SSH into VPS
ssh user@<your-vps-ip>
cd SaszetApp

# 2. Backup the database first
./infrastructure/scripts/backup-db.sh

# 3. Deploy
./infrastructure/scripts/deploy.sh

# 4. Verify
./infrastructure/scripts/smoke-test.sh
```

---

## Rollback

If a deployment causes issues, roll back to a previous commit:

```bash
# Find the commit to roll back to
git log --oneline -10

# Roll back
git checkout <commit-hash>

# Redeploy
./infrastructure/scripts/deploy.sh
```

To return to the latest version:
```bash
git checkout main
git pull origin main
./infrastructure/scripts/deploy.sh
```

---

## Database Backup & Restore

### Create a backup

```bash
./infrastructure/scripts/backup-db.sh
# Saves to ./backups/app-db-<timestamp>.sql.gz
# Automatically keeps only the last 7 backups
```

### Restore from backup

```bash
# Decompress and restore
gunzip -c ./backups/app-db-<timestamp>.sql.gz | docker exec -i app-db psql -U "$POSTGRES_USER"
```

---

## Troubleshooting

### Container not starting

```bash
# View logs for a specific container
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs backend-api
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs keycloak
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs nginx-proxy
```

### SSL certificate issues

```bash
# Re-run SSL initialization
./infrastructure/scripts/init-letsencrypt.sh --email your@email.com

# Check certificate expiry
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec certbot certbot certificates
```

### Database connection issues

```bash
# Verify .env.prod credentials
cat .env.prod | grep POSTGRES

# Check app-db health
docker exec app-db pg_isready -U $POSTGRES_USER
```

### Nginx configuration issues

```bash
# Test nginx config syntax
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec nginx-proxy nginx -t

# Reload nginx
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec nginx-proxy nginx -s reload
```

### Keycloak not accessible

```bash
# Check Keycloak logs
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs keycloak

# Verify KC_HOSTNAME in .env.prod
grep KC_HOSTNAME .env.prod
```

---

## First Deployment Checklist

Use this checklist when doing a first-time production deployment:

- [ ] DNS A records configured for all 4 subdomains (`@`, `admin`, `auth`, `api`)
- [ ] DNS propagation verified (`dig saszet.app`, `dig admin.saszet.app`, etc.)
- [ ] VPS cleaned (ran `cleanup-old-deployment.sh`) if old deployment existed
- [ ] Repository cloned on VPS
- [ ] `.env.prod` created with all production secrets
- [ ] All scripts made executable (`chmod +x infrastructure/scripts/*.sh`)
- [ ] SSL certificates generated (`init-letsencrypt.sh --email your@email.com`)
- [ ] Deployment run successfully (`deploy.sh`)
- [ ] Smoke test passed (`smoke-test.sh`)
- [ ] Keycloak admin console accessible at `https://auth.saszet.app`
- [ ] Admin panel login works at `https://admin.saszet.app`
- [ ] Mobile app loads at `https://saszet.app`
- [ ] API health check passes at `https://api.saszet.app/health`
