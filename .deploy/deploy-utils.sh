#!/bin/bash

# CHAP2 Deployment Utilities
# Shared functions for all deployment scripts

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if Docker is installed
check_docker() {
    print_status "Checking Docker installation..."
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed. Please install Docker first."
        exit 1
    fi
    
    if ! command -v docker-compose &> /dev/null; then
        print_error "Docker Compose is not installed. Please install Docker Compose first."
        exit 1
    fi
    
    print_success "Docker and Docker Compose are installed"
}

# Check if Docker daemon is running
check_docker_daemon() {
    print_status "Checking Docker daemon..."
    if ! docker info &> /dev/null; then
        print_error "Docker daemon is not running. Please start Docker first."
        exit 1
    fi
    print_success "Docker daemon is running"
}

# Create data directory and copy sample data if empty
setup_data_directory() {
    print_status "Setting up data directory..."
    mkdir -p ./data/chorus
    
    # Check if data directory is empty
    if [ ! "$(ls -A ./data/chorus)" ]; then
        print_status "Data directory is empty, copying sample data..."
        if [ -d "./CHAP2.Chorus.Api/data/chorus" ]; then
            cp -r ./CHAP2.Chorus.Api/data/chorus/* ./data/chorus/
            print_success "Sample data copied to data directory"
        else
            print_warning "Sample data directory not found, starting with empty database"
        fi
    else
        print_success "Data directory already contains data"
    fi
}

# Stop existing containers
stop_containers() {
    local compose_file=$1
    print_status "Stopping existing containers..."
    docker-compose -f "$compose_file" down --remove-orphans 2>/dev/null || true
    print_success "Existing containers stopped"
}

# Build and start containers
build_and_start() {
    local compose_file=$1
    local build_cache=${2:-"--no-cache"}
    
    print_status "Building and starting containers..."
    
    # Build images
    print_status "Building images..."
    docker-compose -f "$compose_file" build $build_cache
    
    # Start services
    print_status "Starting services..."
    docker-compose -f "$compose_file" up -d
    
    print_success "Containers built and started"
}

# Wait for services to be ready
wait_for_services() {
    local api_url=$1
    local max_attempts=${2:-30}
    local attempt=1
    
    print_status "Waiting for services to be ready..."
    
    while [ $attempt -le $max_attempts ]; do
        if curl -f "$api_url" &> /dev/null; then
            print_success "API service is ready"
            break
        fi
        
        print_status "Waiting for API service... (attempt $attempt/$max_attempts)"
        sleep 10
        ((attempt++))
    done
    
    if [ $attempt -gt $max_attempts ]; then
        print_error "API service failed to start within the expected time"
        exit 1
    fi
}

# Display service information
display_service_info() {
    local api_port=$1
    local web_port=$2
    
    echo ""
    print_success "🎉 CHAP2 Deployment Complete!"
    echo ""
    echo "Services are now running:"
    echo "  📡 API Service:     http://localhost:$api_port"
    echo "  🌐 Web Portal:      http://localhost:$web_port"
    echo "  📊 Health Check:    http://localhost:$api_port/api/health/ping"
    echo ""
    echo "To view logs:"
    echo "  docker-compose -f <compose-file> logs -f"
    echo ""
    echo "To stop services:"
    echo "  docker-compose -f <compose-file> down"
    echo ""
}

# Check if curl is available
check_curl() {
    if ! command -v curl &> /dev/null; then
        print_warning "curl is not installed. Health checks will be skipped."
        return 1
    fi
    return 0
} 