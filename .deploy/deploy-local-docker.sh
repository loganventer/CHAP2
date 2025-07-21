#!/bin/bash

# CHAP2 Local Docker Deployment Script
# This script builds and deploys the CHAP2 API and Web Portal using Docker for local development
# 
# What this does:
# - Builds and runs CHAP2 in Docker containers for local development
# - Uses Development environment settings
# - Runs on ports 8080 (API) and 8081 (Web Portal)
# - Includes hot-reload capabilities for development
# - Uses local data directory for persistence

set -e

# Source shared utilities
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/deploy-utils.sh"

echo "🚀 CHAP2 Local Docker Deployment"
echo "================================"
echo ""
echo "This will deploy CHAP2 using Docker for local development with:"
echo "  📡 API Service:     http://localhost:8080"
echo "  🌐 Web Portal:      http://localhost:8081"
echo "  🔧 Environment:     Development"
echo "  📁 Data Directory:  ./data/chorus"
echo "  🐳 Container Mode:  Docker"
echo ""

# Configuration
COMPOSE_FILE="docker/docker-compose.local.yml"
API_PORT="8080"
WEB_PORT="8081"
API_URL="http://localhost:$API_PORT/api/health/ping"

# Main deployment function
deploy_local_docker() {
    print_status "Starting local Docker deployment..."
    
    # Check prerequisites
    check_docker
    check_docker_daemon
    check_curl
    
    # Setup data directory
    setup_data_directory
    
    # Stop existing containers
    stop_containers "$COMPOSE_FILE"
    
    # Build and start containers (with cache for faster local development)
    build_and_start "$COMPOSE_FILE" ""
    
    # Wait for services to be ready
    wait_for_services "$API_URL"
    
    # Display service information
    display_service_info "$API_PORT" "$WEB_PORT"
    
    echo ""
    print_status "💡 Development Tips:"
    echo "  • Use --logs to view real-time logs during development"
    echo "  • Use --restart to restart services after code changes"
    echo "  • Data is persisted in ./data/chorus directory"
    echo "  • Hot-reload is enabled for faster development cycles"
    echo ""
}

# Handle script arguments
case "${1:-}" in
    --help|-h)
        echo "Usage: $0 [OPTIONS]"
        echo ""
        echo "Description:"
        echo "  Deploy CHAP2 using Docker containers for local development."
        echo "  This is ideal for development, testing, and local experimentation."
        echo ""
        echo "What gets deployed:"
        echo "  📡 API Service:     http://localhost:8080"
        echo "  🌐 Web Portal:      http://localhost:8081"
        echo "  🔧 Environment:     Development (with hot-reload)"
        echo "  📁 Data Directory:  ./data/chorus"
        echo "  🐳 Container Mode:  Docker containers"
        echo ""
        echo "Options:"
        echo "  --help, -h     Show this help message"
        echo "  --stop         Stop the local deployment"
        echo "  --logs         Show container logs"
        echo "  --restart      Restart the deployment"
        echo ""
        echo "Examples:"
        echo "  $0              # Deploy for local development"
        echo "  $0 --stop       # Stop local deployment"
        echo "  $0 --logs       # View container logs"
        echo ""
        echo "Default: Deploy CHAP2 using Docker for local development"
        exit 0
        ;;
    --stop)
        print_status "Stopping local deployment..."
        docker-compose -f "$COMPOSE_FILE" down
        print_success "Local deployment stopped"
        exit 0
        ;;
    --logs)
        print_status "Showing container logs..."
        docker-compose -f "$COMPOSE_FILE" logs -f
        exit 0
        ;;
    --restart)
        print_status "Restarting local deployment..."
        docker-compose -f "$COMPOSE_FILE" restart
        print_success "Local deployment restarted"
        exit 0
        ;;
    "")
        deploy_local_docker
        ;;
    *)
        print_error "Unknown option: $1"
        echo "Use --help for usage information"
        exit 1
        ;;
esac 