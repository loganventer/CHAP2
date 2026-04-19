#!/bin/bash

set -e

REMOTE_URL="https://chap2-web.onrender.com"
LOCAL_URL="http://localhost:8080"
LOCAL_SERVICE="chap2-webportal"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

REDEPLOY="false"
for arg in "$@"; do
    if [ "$arg" = "redeploy" ]; then
        REDEPLOY="true"
    fi
done

# --- Helpers ---------------------------------------------------------------

open_url() {
    local url="$1"
    echo "Opening $url"
    if command -v open >/dev/null 2>&1; then
        open "$url"
    elif command -v xdg-open >/dev/null 2>&1; then
        xdg-open "$url" >/dev/null 2>&1 &
    else
        echo "(No browser opener found. Visit $url manually.)"
    fi
}

# Pick the docker compose command form that exists on this system.
if docker compose version >/dev/null 2>&1; then
    COMPOSE="docker compose"
elif command -v docker-compose >/dev/null 2>&1; then
    COMPOSE="docker-compose"
else
    echo "Error: neither 'docker compose' nor 'docker-compose' is available."
    exit 1
fi

remote_is_reachable() {
    curl -fsS --max-time 5 -o /dev/null "$REMOTE_URL" >/dev/null 2>&1
}

local_images_exist() {
    # docker compose images prints nothing for services with no image.
    local output
    output="$($COMPOSE images -q "$LOCAL_SERVICE" 2>/dev/null || true)"
    [ -n "$output" ]
}

wait_for_local_ready() {
    local tries=30
    while [ $tries -gt 0 ]; do
        if curl -fsS --max-time 2 -o /dev/null "$LOCAL_URL"; then
            return 0
        fi
        sleep 1
        tries=$((tries - 1))
    done
    return 1
}

start_containers() {
    local build_flag="$1"
    echo "Stopping any existing containers..."
    $COMPOSE down >/dev/null 2>&1 || true
    echo "Starting services${build_flag:+ ($build_flag)}..."
    $COMPOSE up -d $build_flag
}

# --- Main ------------------------------------------------------------------

echo "========================================"
echo "CHAP2 Linux/Mac Deployment"
echo "========================================"

if [ "$REDEPLOY" = "true" ]; then
    echo "Mode: redeploy (full rebuild, local)"
    start_containers "--build"
    echo "Waiting for local web portal..."
    if wait_for_local_ready; then
        open_url "$LOCAL_URL"
    else
        echo "Local web portal did not respond in time. Check: $COMPOSE logs -f"
        exit 1
    fi
    exit 0
fi

echo "Checking remote ($REMOTE_URL)..."
if remote_is_reachable; then
    echo "Remote is up. Using hosted deployment."
    open_url "$REMOTE_URL"
    exit 0
fi

echo "Remote unreachable. Checking for local images..."
if local_images_exist; then
    echo "Local images found. Starting without rebuild."
    start_containers ""
    if wait_for_local_ready; then
        open_url "$LOCAL_URL"
    else
        echo "Local web portal did not respond in time. Check: $COMPOSE logs -f"
        exit 1
    fi
    exit 0
fi

echo "No local images found. Run again with: $(basename "$0") redeploy"
echo "That will build the images and start the local stack."
exit 1
