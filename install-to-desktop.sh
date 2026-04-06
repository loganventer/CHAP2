#!/bin/bash

###############################################################################
# CHAP2 Desktop Installer
#
# This script will:
# 1. Detect the CHAP2 project directory location
# 2. Create a startup script with the correct paths
# 3. Copy it to the Desktop as a .command file
###############################################################################

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Print colored messages
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

###############################################################################
# Detect CHAP2 project directory
###############################################################################
detect_project_dir() {
    # Get the directory where this script is located
    SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

    # Check if we're in the CHAP2 directory by looking for key files
    if [ -d "$SCRIPT_DIR/.deploy/linux-mac" ] && [ -f "$SCRIPT_DIR/.deploy/linux-mac/docker-compose.yml" ]; then
        PROJECT_DIR="$SCRIPT_DIR"
        log_success "Found CHAP2 project at: $PROJECT_DIR"
        return 0
    fi

    log_error "Could not find CHAP2 project directory"
    log_error "Please run this script from the CHAP2 project root"
    exit 1
}

###############################################################################
# Create startup script with detected path
###############################################################################
create_startup_script() {
    local output_file="$1"

    log_info "Creating startup script with project path: $PROJECT_DIR"

    cat > "$output_file" << 'SCRIPT_EOF'
#!/bin/bash

###############################################################################
# CHAP2 Startup Script for macOS
#
# This script will:
# 1. Check if Docker is running, start it if not
# 2. Wait for Docker to be ready
# 3. Deploy all required containers using docker-compose
# 4. Wait for services to be healthy
# 5. Open the web UI in your default browser
###############################################################################

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
# Always use the CHAP2 project directory, regardless of where the script is located
PROJECT_DIR="__PROJECT_DIR_PLACEHOLDER__"
DOCKER_COMPOSE_DIR="$PROJECT_DIR/.deploy/linux-mac"
DOCKER_COMPOSE_FILE="$DOCKER_COMPOSE_DIR/docker-compose.yml"
WEB_UI_URL="http://localhost:8080"
MAX_WAIT_TIME=180  # Maximum wait time in seconds for services to start

# Print colored messages
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

###############################################################################
# Check if Docker is installed
###############################################################################
check_docker_installed() {
    log_info "Checking if Docker is installed..."
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed. Please install Docker Desktop for Mac first."
        log_info "Download from: https://www.docker.com/products/docker-desktop"
        exit 1
    fi
    log_success "Docker is installed"
}

###############################################################################
# Check if Docker is running
###############################################################################
is_docker_running() {
    docker info &> /dev/null
    return $?
}

###############################################################################
# Start Docker Desktop
###############################################################################
start_docker() {
    log_info "Starting Docker Desktop..."
    open -a Docker

    log_info "Waiting for Docker to start..."
    local count=0
    while ! is_docker_running; do
        sleep 2
        count=$((count + 2))
        if [ $count -gt 60 ]; then
            log_error "Docker failed to start within 60 seconds"
            exit 1
        fi
        echo -n "."
    done
    echo ""
    log_success "Docker is running"
}

###############################################################################
# Ensure Docker is running
###############################################################################
ensure_docker_running() {
    if is_docker_running; then
        log_success "Docker is already running"
    else
        start_docker
    fi
}

###############################################################################
# Check if containers already exist
###############################################################################
containers_exist() {
    cd "$DOCKER_COMPOSE_DIR"
    local count
    count=$(docker-compose ps -q 2>/dev/null | wc -l | tr -d ' ')
    [ "$count" -gt 0 ]
}

###############################################################################
# Check if all containers are running
###############################################################################
containers_running() {
    cd "$DOCKER_COMPOSE_DIR"
    local total stopped
    total=$(docker-compose ps -q 2>/dev/null | wc -l | tr -d ' ')
    stopped=$(docker-compose ps -q --filter "status=exited" 2>/dev/null | wc -l | tr -d ' ')
    [ "$total" -gt 0 ] && [ "$stopped" -eq 0 ]
}

###############################################################################
# Start existing containers (no rebuild)
###############################################################################
start_containers() {
    log_info "Starting existing CHAP2 containers..."
    cd "$DOCKER_COMPOSE_DIR"
    docker-compose start
    log_success "Containers started"
}

###############################################################################
# Stop and remove existing containers
###############################################################################
cleanup_containers() {
    log_info "Cleaning up existing containers..."
    cd "$DOCKER_COMPOSE_DIR"
    docker-compose down --remove-orphans 2>/dev/null || true
    log_success "Cleanup complete"
}

###############################################################################
# Build and deploy containers from scratch
###############################################################################
deploy_containers() {
    log_info "Deploying CHAP2 containers..."
    cd "$DOCKER_COMPOSE_DIR"

    # Pull latest images and build
    log_info "Building and starting services (this may take a few minutes)..."
    docker-compose up -d --build

    log_success "Containers deployed"
}

###############################################################################
# Smart start: reuse existing containers when possible
###############################################################################
smart_start() {
    cd "$DOCKER_COMPOSE_DIR"

    if containers_running; then
        log_success "All CHAP2 containers are already running"
        return 0
    fi

    if containers_exist; then
        log_info "Found existing CHAP2 containers, starting them..."
        start_containers
        return 0
    fi

    log_info "No existing containers found, building from scratch..."
    deploy_containers
}

###############################################################################
# Wait for a specific service to be ready
###############################################################################
wait_for_service() {
    local service_name=$1
    local url=$2
    local max_wait=$3

    log_info "Waiting for $service_name to be ready..."
    local count=0
    while [ $count -lt $max_wait ]; do
        if curl -s -f "$url" > /dev/null 2>&1; then
            log_success "$service_name is ready"
            return 0
        fi
        sleep 2
        count=$((count + 2))
        echo -n "."
    done
    echo ""
    log_warn "$service_name did not respond within ${max_wait}s, but continuing anyway..."
    return 1
}

###############################################################################
# Wait for all services to be ready
###############################################################################
wait_for_services() {
    log_info "Waiting for all services to start..."

    # Wait for CHAP2 API
    wait_for_service "CHAP2 API" "http://localhost:5001/api/health/ping" 90

    # Wait for Web Portal
    wait_for_service "CHAP2 Web Portal" "http://localhost:8080" 90

    log_success "All services are running"
}

###############################################################################
# Show container status
###############################################################################
show_status() {
    log_info "Container status:"
    cd "$DOCKER_COMPOSE_DIR"
    docker-compose ps
}

###############################################################################
# Open browser
###############################################################################
open_browser() {
    log_info "Opening web browser to $WEB_UI_URL..."
    sleep 2  # Give it a moment to fully initialize
    open "$WEB_UI_URL"
    log_success "Browser opened"
}

###############################################################################
# Detect local network IP address
###############################################################################
detect_local_ip() {
    # Try common interfaces in order of preference
    LOCAL_IP=$(ipconfig getifaddr en0 2>/dev/null || \
               ipconfig getifaddr en1 2>/dev/null || \
               ipconfig getifaddr en2 2>/dev/null || \
               echo "unknown")

    # Fallback: parse route table for default interface
    if [ "$LOCAL_IP" = "unknown" ]; then
        local iface
        iface=$(route -n get default 2>/dev/null | awk '/interface:/{print $2}')
        if [ -n "$iface" ]; then
            LOCAL_IP=$(ipconfig getifaddr "$iface" 2>/dev/null || echo "unknown")
        fi
    fi
}

###############################################################################
# Open firewall for mobile browsing
###############################################################################
open_firewall_port() {
    detect_local_ip

    if [ "$LOCAL_IP" != "unknown" ]; then
        log_success "Local IP address: $LOCAL_IP"
        echo ""
        log_info "========================================="
        log_info "  Mobile Sync URL:"
        log_info "  http://$LOCAL_IP:8080/sync"
        log_info "========================================="
        echo ""
        log_info "Open this URL on phones to sync with the chorus display"
    else
        log_warn "Could not detect local IP address. Mobile sync may not be available."
    fi

    # Check if firewall is enabled and configure exceptions
    FIREWALL_STATE=$(/usr/libexec/ApplicationFirewall/socketfilterfw --getglobalstate 2>/dev/null | grep -o "enabled\|disabled" || echo "unknown")

    if [ "$FIREWALL_STATE" = "enabled" ]; then
        log_info "macOS Firewall is enabled. Configuring exceptions for network access..."

        # Find and whitelist all Docker-related binaries
        # Docker Desktop uses multiple executables for networking
        local docker_binaries=(
            "/Applications/Docker.app/Contents/MacOS/Docker"
            "/Applications/Docker.app/Contents/MacOS/com.docker.backend"
            "/Applications/Docker.app/Contents/MacOS/com.docker.vmnetd"
            "/Applications/Docker.app/Contents/Resources/bin/vpnkit"
        )

        # Also dynamically find any other Docker binaries
        while IFS= read -r binary; do
            docker_binaries+=("$binary")
        done < <(find /Applications/Docker.app/Contents -type f -perm +111 -name "com.docker.*" 2>/dev/null)

        local added=0
        for binary in "${docker_binaries[@]}"; do
            if [ -f "$binary" ]; then
                sudo /usr/libexec/ApplicationFirewall/socketfilterfw --add "$binary" 2>/dev/null || true
                sudo /usr/libexec/ApplicationFirewall/socketfilterfw --unblockapp "$binary" 2>/dev/null || true
                added=$((added + 1))
            fi
        done

        # Also whitelist the docker CLI itself
        local docker_path
        docker_path=$(which docker 2>/dev/null)
        if [ -n "$docker_path" ] && [ -f "$docker_path" ]; then
            sudo /usr/libexec/ApplicationFirewall/socketfilterfw --add "$docker_path" 2>/dev/null || true
            sudo /usr/libexec/ApplicationFirewall/socketfilterfw --unblockapp "$docker_path" 2>/dev/null || true
            added=$((added + 1))
        fi

        log_success "$added Docker binaries added to firewall exceptions"
    else
        log_info "macOS Firewall is disabled - no configuration needed"
    fi

    # Verify the port is accessible
    if nc -z localhost 8080 2>/dev/null; then
        log_success "Port 8080 is open and accepting connections"
    else
        log_warn "Port 8080 does not appear to be listening yet - services may still be starting"
    fi

    # Check for iPhone hotspot (client isolation blocks device-to-device traffic)
    if [ "$LOCAL_IP" != "unknown" ]; then
        local subnet
        subnet=$(ifconfig en0 2>/dev/null | awk '/netmask/{print $4}')
        local router
        router=$(netstat -rn 2>/dev/null | awk '/default.*en0/{print $2; exit}')

        if [[ "$LOCAL_IP" == 172.20.10.* ]] || [[ "$router" == 172.20.10.* ]]; then
            echo ""
            log_warn "========================================="
            log_warn "  iPhone Hotspot Detected!"
            log_warn "========================================="
            log_warn "iPhone hotspots block device-to-device traffic."
            log_warn "Mobile phones cannot reach this Mac over a hotspot."
            log_warn ""
            log_warn "To enable mobile sync, connect all devices to"
            log_warn "the same Wi-Fi network (home/church router)."
            log_warn "========================================="
            echo ""
        fi
    fi
}

###############################################################################
# Main execution
###############################################################################
main() {
    echo ""
    log_info "========================================="
    log_info "CHAP2 Startup Script"
    log_info "========================================="
    echo ""

    # Step 1: Check Docker installation
    check_docker_installed

    # Step 2: Ensure Docker is running
    ensure_docker_running

    # Step 3: Start containers (reuse existing, or build if none found)
    smart_start

    # Step 4: Wait for services
    wait_for_services

    # Step 5: Configure network access
    open_firewall_port

    # Step 6: Show status
    show_status

    # Step 7: Open browser
    open_browser

    echo ""
    log_success "========================================="
    log_success "CHAP2 is now running!"
    log_success "========================================="
    echo ""
    log_info "Web Portal: http://localhost:8080"
    if [ "$LOCAL_IP" != "unknown" ] && [ -n "$LOCAL_IP" ]; then
        log_info "Network Access: http://$LOCAL_IP:8080"
    fi
    log_info "API: http://localhost:5001"
    echo ""
    log_info "To stop all services, run:"
    log_info "  cd $DOCKER_COMPOSE_DIR && docker-compose down"
    echo ""
}

# Run main function
main
SCRIPT_EOF

    # Replace the placeholder with the actual project directory
    sed -i '' "s|__PROJECT_DIR_PLACEHOLDER__|$PROJECT_DIR|g" "$output_file"

    # Make it executable
    chmod +x "$output_file"

    log_success "Startup script created at: $output_file"
}

###############################################################################
# Nuke all Docker state (containers, images, volumes, networks, build cache)
###############################################################################
nuke_docker() {
    log_info "Nuking all Docker containers, images, volumes, and cache..."

    # Stop all running containers
    local containers
    containers=$(docker ps -aq 2>/dev/null)
    if [ -n "$containers" ]; then
        docker stop $containers 2>/dev/null || true
        docker rm -f $containers 2>/dev/null || true
        log_success "All containers removed"
    else
        log_info "No containers to remove"
    fi

    # Remove all images
    local images
    images=$(docker images -q 2>/dev/null)
    if [ -n "$images" ]; then
        docker rmi -f $images 2>/dev/null || true
        log_success "All images removed"
    else
        log_info "No images to remove"
    fi

    # Remove all volumes, networks, and build cache
    docker volume prune -f 2>/dev/null || true
    docker network prune -f 2>/dev/null || true
    docker system prune -af --volumes 2>/dev/null || true

    log_success "Docker nuked clean"
}

###############################################################################
# Copy to Desktop
###############################################################################
copy_to_desktop() {
    local desktop_file="$HOME/Desktop/start-chap2.command"

    # Force remove any existing script (ignore errors)
    rm -f "$desktop_file" 2>/dev/null || true
    log_info "Creating fresh startup script on Desktop..."

    # Create the script with the detected path
    local temp_file
    temp_file=$(mktemp)
    create_startup_script "$temp_file"

    # Overwrite desktop file
    cp -f "$temp_file" "$desktop_file"
    chmod +x "$desktop_file"
    rm -f "$temp_file"

    # Verify it was written
    if [ -f "$desktop_file" ]; then
        log_success "Installed to: $desktop_file"
    else
        log_error "Failed to write desktop script!"
        exit 1
    fi
}

###############################################################################
# Main execution
###############################################################################
main() {
    echo ""
    log_info "========================================="
    log_info "CHAP2 Desktop Installer"
    log_info "========================================="
    echo ""

    # Detect project directory
    detect_project_dir

    # Nuke Docker before creating fresh desktop script
    if command -v docker &> /dev/null && docker info &> /dev/null 2>&1; then
        nuke_docker
    else
        log_info "Docker not running, skipping cleanup"
    fi

    # Copy to Desktop
    copy_to_desktop

    echo ""
    log_success "========================================="
    log_success "Installation complete!"
    log_success "========================================="
    echo ""
    log_info "You can now double-click 'start-chap2.command' on your Desktop"
    log_info "to start CHAP2 with all required services."
    echo ""
}

# Run main function
main
