#!/bin/bash

# CHAP2 Docker Deployment Script
# This script builds and deploys the CHAP2 API and Web Portal using Docker

set -e  # Exit on any error

echo "🚀 CHAP2 Docker Deployment Script"
echo "=================================="

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

# Create data directory if it doesn't exist
create_data_directory() {
    print_status "Creating data directory..."
    mkdir -p ./data/chorus
    print_success "Data directory created"
}

# Stop existing containers
stop_containers() {
    print_status "Stopping existing containers..."
    docker-compose down --remove-orphans
    print_success "Existing containers stopped"
}

# Build and start containers
build_and_start() {
    print_status "Building and starting containers..."
    docker-compose up --build -d
    print_success "Containers built and started"
}

# Wait for services to be ready
wait_for_services() {
    print_status "Waiting for services to be ready..."
    
    # Wait for API
    print_status "Waiting for API service..."
    for i in {1..30}; do
        if curl -f http://localhost:5000/api/health/ping &> /dev/null; then
            print_success "API service is ready"
            break
        fi
        if [ $i -eq 30 ]; then
            print_error "API service failed to start within 30 seconds"
            exit 1
        fi
        sleep 1
    done
    
    # Wait for Web Portal
    print_status "Waiting for Web Portal service..."
    for i in {1..30}; do
        if curl -f http://localhost:5001 &> /dev/null; then
            print_success "Web Portal service is ready"
            break
        fi
        if [ $i -eq 30 ]; then
            print_error "Web Portal service failed to start within 30 seconds"
            exit 1
        fi
        sleep 1
    done
}

# Show service status
show_status() {
    print_status "Service Status:"
    echo ""
    echo "API Service:"
    echo "  - URL: http://localhost:5000"
    echo "  - Health: http://localhost:5000/api/health/ping"
    echo ""
    echo "Web Portal:"
    echo "  - URL: http://localhost:5001"
    echo "  - HTTPS: https://localhost:7001"
    echo ""
    echo "Data Directory: ./data/chorus"
    echo ""
}

# Show logs
show_logs() {
    print_status "Container logs:"
    docker-compose logs --tail=20
}

# Main deployment function
deploy() {
    print_status "Starting CHAP2 deployment..."
    
    check_docker
    check_docker_daemon
    create_data_directory
    stop_containers
    build_and_start
    wait_for_services
    show_status
    
    print_success "CHAP2 deployment completed successfully!"
    echo ""
    print_status "You can now access:"
    echo "  - API: http://localhost:5000"
    echo "  - Web Portal: http://localhost:5001"
    echo ""
    print_status "To view logs, run: ./deploy.sh logs"
    print_status "To stop services, run: ./deploy.sh stop"
}

# Stop services
stop_services() {
    print_status "Stopping CHAP2 services..."
    docker-compose down
    print_success "Services stopped"
}

# Show logs
logs() {
    print_status "Showing container logs..."
    docker-compose logs -f
}

# Show help
show_help() {
    echo "CHAP2 Docker Deployment Script"
    echo ""
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  deploy    Build and start all services (default)"
    echo "  stop      Stop all services"
    echo "  logs      Show container logs"
    echo "  status    Show service status"
    echo "  help      Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 deploy    # Deploy all services"
    echo "  $0 stop      # Stop all services"
    echo "  $0 logs      # Show logs"
}

# Main script logic
case "${1:-deploy}" in
    "deploy")
        deploy
        ;;
    "stop")
        stop_services
        ;;
    "logs")
        logs
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