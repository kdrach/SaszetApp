# SaszetApp — Deployment Guide

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
