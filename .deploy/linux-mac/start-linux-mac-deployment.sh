#!/bin/bash

set -e

REMOTE_URL="https://chap2-web.onrender.com"
LOCAL_URL="http://localhost:8080"
LOCAL_SERVICE="chap2-webportal"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
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

# Bring the local stack up (optionally rebuilding), wait for it, and open the browser.
run_local() {
    local build_flag="$1"
    start_containers "$build_flag"
    echo "Waiting for local web portal..."
    if wait_for_local_ready; then
        open_url "$LOCAL_URL"
    else
        echo "Local web portal did not respond in time. Check: $COMPOSE logs -f"
        exit 1
    fi
}

# (Re)create the desktop launcher that runs this script when double-clicked.
# macOS: a .command shell file (optionally with a custom icon via `fileicon`).
# Linux: a .desktop entry pointing at this script with the kerk-logo icon.
install_desktop_shortcut() {
    local desktop_dir="$HOME/Desktop"
    [ -d "$desktop_dir" ] || return 0

    local icon_source="$REPO_ROOT/CHAP2.UI/CHAP2.WebPortal/wwwroot/img/kerk-logo.png"
    local script_path="$SCRIPT_DIR/$(basename "${BASH_SOURCE[0]}")"

    case "$(uname -s)" in
        Darwin)
            local shortcut="$desktop_dir/Evangelie Kerk.command"
            cat > "$shortcut" <<SH
#!/bin/bash
cd "$SCRIPT_DIR"
./$(basename "${BASH_SOURCE[0]}") "\$@"
SH
            chmod +x "$shortcut"
            # Custom icon: fileicon (Homebrew) is the simplest non-destructive
            # way to attach an icon to a file. Gracefully skip if missing.
            if command -v fileicon >/dev/null 2>&1 && [ -f "$icon_source" ]; then
                fileicon set "$shortcut" "$icon_source" >/dev/null 2>&1 || true
            fi
            echo "Desktop shortcut refreshed: $shortcut"
            ;;
        Linux)
            local shortcut="$desktop_dir/evangelie-kerk.desktop"
            cat > "$shortcut" <<EOF
[Desktop Entry]
Type=Application
Name=Evangelie Kerk
Comment=Launch Evangelie Kerk chorus search
Exec="$script_path"
Icon=$icon_source
Terminal=false
Categories=Application;
EOF
            chmod +x "$shortcut"
            echo "Desktop shortcut refreshed: $shortcut"
            ;;
    esac
}

# --- Main ------------------------------------------------------------------

echo "========================================"
echo "CHAP2 Linux/Mac Deployment"
echo "========================================"

install_desktop_shortcut

if [ "$REDEPLOY" = "true" ]; then
    echo "Mode: redeploy (full rebuild, local)"
    run_local "--build"
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
    run_local ""
    exit 0
fi

echo "No local images found. Building and starting the local stack..."
run_local "--build"
exit 0
