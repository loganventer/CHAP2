#!/bin/bash

# CHAP2 Oracle Cloud Deployment Script
# Run this on your Oracle Linux instance

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

# Function to check if Docker is installed
check_docker() {
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed. Please run oracle-linux-setup.sh first."
        exit 1
    fi
    
    if ! docker info &> /dev/null; then
        print_error "Docker is not running. Please start Docker or log out and back in."
        exit 1
    fi
    
    print_success "Docker is installed and running"
}

# Function to check Docker daemon
check_docker_daemon() {
    if ! docker info &> /dev/null; then
        print_error "Docker daemon is not running. Please start Docker."
        exit 1
    fi
    print_success "Docker daemon is running"
}

# Function to create data directories
create_data_directories() {
    print_status "Creating data directories..."
    mkdir -p ./data/chorus
    print_success "Data directories created"
}

# Function to stop existing containers
stop_containers() {
    print_status "Stopping existing containers..."
    docker-compose -f docker-compose.oracle-cloud.yml down --remove-orphans 2>/dev/null || true
    print_success "Existing containers stopped"
}

# Function to build and start containers
build_and_start() {
    print_status "Building and starting containers..."
    
    # Build images
    print_status "Building images for Oracle Cloud..."
    docker-compose -f docker-compose.oracle-cloud.yml build --no-cache
    
    # Start services
    print_status "Starting services..."
    docker-compose -f docker-compose.oracle-cloud.yml up -d
    
    print_success "Containers built and started"
}

# Function to wait for services
wait_for_services() {
    print_status "Waiting for services to be ready..."
    
    local max_attempts=30
    local attempt=1
    
    while [ $attempt -le $max_attempts ]; do
        if curl -f http://localhost:5000/api/health/ping &> /dev/null; then
            print_success "API service is ready"
            break
        fi
        
        print_status "Waiting for API service... (attempt $attempt/$max_attempts)"
        sleep 10
        ((attempt++))
    done
    
    if [ $attempt -gt $max_attempts ]; then
        print_warning "API service may not be ready yet. Check logs with: docker-compose -f docker-compose.oracle-cloud.yml logs"
    fi
}

# Function to show status
show_status() {
    print_status "Container status:"
    docker-compose -f docker-compose.oracle-cloud.yml ps
    
    echo ""
    print_status "Service URLs:"
    echo "  API: http://$(curl -s ifconfig.me):5000"
    echo "  Web Portal: http://$(curl -s ifconfig.me):5001"
    echo "  API Health: http://$(curl -s ifconfig.me):5000/api/health/ping"
    
    echo ""
    print_status "Local URLs:"
    echo "  API: http://localhost:5000"
    echo "  Web Portal: http://localhost:5001"
}

# Function to show logs
show_logs() {
    print_status "Showing container logs..."
    docker-compose -f docker-compose.oracle-cloud.yml logs -f
}

# Function to stop services
stop_services() {
    print_status "Stopping services..."
    docker-compose -f docker-compose.oracle-cloud.yml down
    print_success "Services stopped"
}

# Function to show help
show_help() {
    echo "CHAP2 Oracle Cloud Deployment Script"
    echo ""
    echo "Usage: $0 [COMMAND]"
    echo ""
    echo "Commands:"
    echo "  deploy    Build and start all services (default)"
    echo "  stop      Stop all services"
    echo "  logs      Show container logs"
    echo "  status    Show service status"
    echo "  help      Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 deploy    # Deploy the application"
    echo "  $0 stop      # Stop the application"
    echo "  $0 logs      # View logs"
}

# Main script logic
case "${1:-deploy}" in
    "deploy")
        print_status "Starting CHAP2 deployment on Oracle Cloud..."
        check_docker
        check_docker_daemon
        create_data_directories
        stop_containers
        build_and_start
        wait_for_services
        show_status
        print_success "Deployment completed!"
        echo ""
        echo "🎉 CHAP2 is now running on Oracle Cloud!"
        echo "🌐 Public URLs:"
        echo "  Web Portal: http://$(curl -s ifconfig.me):5001"
        echo "  API: http://$(curl -s ifconfig.me):5000"
        echo ""
        echo "📱 You can now access the application from anywhere!"
        ;;
    "stop")
        stop_services
        ;;
    "logs")
        show_logs
        ;;
    "status")
        show_status
        ;;
    "help"|"-h"|"--help")
        show_help
        ;;
    *)
        print_error "Unknown command: $1"
        show_help
        exit 1
        ;;
esac 