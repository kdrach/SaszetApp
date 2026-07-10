#!/bin/bash
# auto-deploy.sh
# Skrypt do automatycznego sprawdzania zmian w repo i uruchamiania deployu
# Uruchamiany z poziomu crona dla użytkownika 'deploy'

# Zmienna przechowująca ścieżkę do projektu (zmień jeśli jest inna)
PROJECT_DIR="/opt/SaszetApp" 
LOG_FILE="/var/log/saszetapp-auto-deploy.log"

cd "$PROJECT_DIR" || { echo "Nie znaleziono katalogu $PROJECT_DIR"; exit 1; }

# Zapisanie daty uruchomienia skryptu
echo "[$(date)] Sprawdzanie zmian w repozytorium..."

# Pobranie informacji z remote bez zmiany plików lokalnych
git fetch origin main

# Sprawdzenie czy lokalna gałąź jest w tyle za zaktualizowanym origin/main
LOCAL=$(git rev-parse HEAD)
REMOTE=$(git rev-parse origin/main)

if [ "$LOCAL" != "$REMOTE" ]; then
    echo "[$(date)] Wykryto zmiany. Uruchamianie aktualizacji..."
    git pull origin main
    
    # Uruchomienie skryptu bootstrap jako root
    if sudo ./infrastructure/scripts/bootstrap-vps.sh --skip-cleanup; then
        echo "[$(date)] Aktualizacja zakończona pomyślnie."
    else
        echo "[$(date)] Błąd podczas uruchamiania bootstrap-vps.sh!" >&2
        exit 1
    fi
else
    echo "[$(date)] Brak zmian. Wszystko aktualne."
fi
