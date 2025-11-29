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
PROJECT_DIR="/Users/logan/Documents/dev/CHAP2"
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
# Stop and remove existing containers
###############################################################################
cleanup_containers() {
    log_info "Cleaning up existing containers..."
    cd "$DOCKER_COMPOSE_DIR"
    docker-compose down --remove-orphans 2>/dev/null || true
    log_success "Cleanup complete"
}

###############################################################################
# Check if rebuild is needed
###############################################################################
needs_rebuild() {
    local service=$1
    local dockerfile=$2
    local context=$3

    # Check if image exists
    local image_name="linux-mac-${service}"
    if ! docker images -q "$image_name" 2>/dev/null | grep -q .; then
        log_info "$service: Image not found, will build"
        return 0  # True - needs rebuild
    fi

    # Check if Dockerfile changed
    local dockerfile_path="$PROJECT_DIR/$dockerfile"
    local image_created=$(docker inspect -f '{{ .Created }}' "$image_name" 2>/dev/null)
    local dockerfile_modified=$(stat -f %m "$dockerfile_path" 2>/dev/null || echo 0)

    if [ "$dockerfile_modified" -gt "$(date -j -f "%Y-%m-%dT%H:%M:%S" "${image_created%.*}" +%s 2>/dev/null || echo 0)" ]; then
        log_info "$service: Dockerfile changed, will rebuild"
        return 0  # True - needs rebuild
    fi

    log_info "$service: Image up to date, will reuse"
    return 1  # False - no rebuild needed
}

###############################################################################
# Deploy containers
###############################################################################
deploy_containers() {
    log_info "Deploying CHAP2 containers..."
    cd "$DOCKER_COMPOSE_DIR"

    # Check each service for changes
    log_info "Checking if services need rebuilding..."

    local build_needed=false

    # Check if any images are missing
    if ! docker images -q linux-mac-chap2-api 2>/dev/null | grep -q . || \
       ! docker images -q linux-mac-chap2-webportal 2>/dev/null | grep -q . || \
       ! docker images -q linux-mac-langchain-service 2>/dev/null | grep -q .; then
        log_warn "One or more images missing, will build"
        build_needed=true
    fi

    # Start services (will auto-build if needed thanks to docker-compose)
    if [ "$build_needed" = true ]; then
        log_info "Building and starting services (this may take a few minutes)..."
        docker-compose up -d --build
    else
        log_info "Starting services (reusing existing images)..."
        # Docker Compose will automatically detect if rebuild is needed
        docker-compose up -d
    fi

    log_success "Containers deployed"
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

    # Wait for Qdrant
    wait_for_service "Qdrant" "http://localhost:6333/healthz" 60

    # Wait for LangChain service
    wait_for_service "LangChain Service" "http://localhost:8000/health" 90

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

    # Step 3: Cleanup old containers
    cleanup_containers

    # Step 4: Deploy containers
    deploy_containers

    # Step 5: Wait for services
    wait_for_services

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
    log_info "API: http://localhost:5001"
    log_info "LangChain Service: http://localhost:8000"
    log_info "Qdrant: http://localhost:6333"
    echo ""
    log_info "To stop all services, run:"
    log_info "  cd $DOCKER_COMPOSE_DIR && docker-compose down"
    echo ""
}

# Run main function
main