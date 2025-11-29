#!/bin/bash

###############################################################################
# CHAP2 Rebuild Script for macOS
#
# This script forces a complete rebuild of all Docker images.
# Use this when:
# - You've made significant code changes
# - Dependencies have been updated
# - You want to ensure everything is fresh
#
# For normal development, use start-chap2.sh instead (much faster!)
###############################################################################

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROJECT_DIR="/Users/logan/Documents/dev/CHAP2"
DOCKER_COMPOSE_DIR="$PROJECT_DIR/.deploy/linux-mac"

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

echo ""
log_info "========================================="
log_info "CHAP2 Rebuild Script"
log_info "========================================="
echo ""
log_warn "This will force rebuild all Docker images"
log_warn "This may take 10-15 minutes"
echo ""
read -p "Continue? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]
then
    log_info "Rebuild cancelled"
    exit 0
fi

cd "$DOCKER_COMPOSE_DIR"

log_info "Stopping existing containers..."
docker-compose down --remove-orphans

log_info "Rebuilding all images (this will take a while)..."
docker-compose build --no-cache --parallel

log_info "Starting services..."
docker-compose up -d

log_success "Rebuild complete!"
log_info "Use start-chap2.sh for future deployments (much faster)"
