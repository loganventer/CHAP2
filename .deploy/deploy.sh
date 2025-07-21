#!/bin/bash

# CHAP2 Docker Deployment Script
# This script builds and deploys the CHAP2 API and Web Portal using Docker

set -e

# Source shared utilities
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/deploy-utils.sh"

echo "🚀 CHAP2 Docker Deployment Script"
echo "=================================="

# Configuration
COMPOSE_FILE="docker/docker-compose.yml"
API_PORT="5000"
WEB_PORT="5001"
API_URL="http://localhost:$API_PORT/api/health/ping"

# Main deployment function
deploy_production() {
    print_status "Starting production deployment..."
    
    # Check prerequisites
    check_docker
    check_docker_daemon
    check_curl
    
    # Setup data directory
    setup_data_directory
    
    # Stop existing containers
    stop_containers "$COMPOSE_FILE"
    
    # Build and start containers
    build_and_start "$COMPOSE_FILE"
    
    # Wait for services to be ready
    wait_for_services "$API_URL"
    
    # Display service information
    display_service_info "$API_PORT" "$WEB_PORT"
}

# Handle script arguments
case "${1:-}" in
    --help|-h)
        echo "Usage: $0 [OPTIONS]"
        echo ""
        echo "Options:"
        echo "  --help, -h     Show this help message"
        echo "  --stop         Stop the deployment"
        echo "  --logs         Show container logs"
        echo "  --restart      Restart the deployment"
        echo ""
        echo "Default: Deploy CHAP2 for production"
        exit 0
        ;;
    --stop)
        print_status "Stopping deployment..."
        docker-compose -f "$COMPOSE_FILE" down
        print_success "Deployment stopped"
        exit 0
        ;;
    --logs)
        print_status "Showing container logs..."
        docker-compose -f "$COMPOSE_FILE" logs -f
        exit 0
        ;;
    --restart)
        print_status "Restarting deployment..."
        docker-compose -f "$COMPOSE_FILE" restart
        print_success "Deployment restarted"
        exit 0
        ;;
    "")
        deploy_production
        ;;
    *)
        print_error "Unknown option: $1"
        echo "Use --help for usage information"
        exit 1
        ;;
esac 