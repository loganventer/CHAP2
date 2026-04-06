#!/bin/bash

###############################################################################
# CHAP2 Fly.io Deployment
#
# Prerequisites:
#   brew install flyctl
#   fly auth login
#
# This script deploys both the API and WebPortal to Fly.io.
# Run from the project root: .deploy/fly/deploy-fly.sh
###############################################################################

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info()    { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[OK]${NC} $1"; }
log_error()   { echo -e "${RED}[ERROR]${NC} $1"; }

# Check flyctl is installed
if ! command -v fly &> /dev/null; then
    log_error "flyctl not installed. Run: brew install flyctl"
    exit 1
fi

# Check logged in
if ! fly auth whoami &> /dev/null; then
    log_error "Not logged in. Run: fly auth login"
    exit 1
fi

log_info "Deploying from: $PROJECT_ROOT"

###############################################################################
# Step 1: Deploy the API
###############################################################################
log_info "========================================="
log_info "Deploying CHAP2 API..."
log_info "========================================="

cd "$PROJECT_ROOT"

# Create the app if it doesn't exist
fly apps create chap2-api --org personal 2>/dev/null || true

# Create volume for chorus data if it doesn't exist
fly volumes list -a chap2-api 2>/dev/null | grep -q "chorus_data" || \
    fly volumes create chorus_data --region jnb --size 1 -a chap2-api

# Deploy
fly deploy --config "$SCRIPT_DIR/fly-api.toml" --remote-only

log_success "API deployed"

###############################################################################
# Step 2: Upload chorus data (first time only)
###############################################################################
if fly ssh console -a chap2-api -C "ls /app/data/chorus/*.json 2>/dev/null | head -1" 2>/dev/null | grep -q ".json"; then
    log_info "Chorus data already exists on volume, skipping upload"
else
    log_info "Uploading chorus data..."
    # Create the directory
    fly ssh console -a chap2-api -C "mkdir -p /app/data/chorus"

    # Upload each file via sftp
    cd "$PROJECT_ROOT/CHAP2.Chorus.Api/data"
    tar czf /tmp/chorus-data.tar.gz chorus/
    fly ssh sftp shell -a chap2-api <<SFTP
put /tmp/chorus-data.tar.gz /app/data/chorus-data.tar.gz
SFTP
    fly ssh console -a chap2-api -C "cd /app/data && tar xzf chorus-data.tar.gz && rm chorus-data.tar.gz"
    rm /tmp/chorus-data.tar.gz
    log_success "Chorus data uploaded"
fi

###############################################################################
# Step 3: Deploy the WebPortal
###############################################################################
log_info "========================================="
log_info "Deploying CHAP2 WebPortal..."
log_info "========================================="

cd "$PROJECT_ROOT"

# Create the app if it doesn't exist
fly apps create chap2-web --org personal 2>/dev/null || true

# Deploy
fly deploy --config "$SCRIPT_DIR/fly-web.toml" --remote-only

log_success "WebPortal deployed"

###############################################################################
# Done
###############################################################################
WEB_URL=$(fly status -a chap2-web 2>/dev/null | grep "Hostname" | awk '{print $NF}')

echo ""
log_success "========================================="
log_success "Deployment complete!"
log_success "========================================="
echo ""
log_info "Web Portal:  https://${WEB_URL:-chap2-web.fly.dev}"
log_info "Mobile Sync: https://${WEB_URL:-chap2-web.fly.dev}/sync"
log_info "API:         https://chap2-api.fly.dev"
echo ""
