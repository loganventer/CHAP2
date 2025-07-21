#!/bin/bash

# CHAP2 Oracle Cloud Auto-Startup Script
# This script runs on boot and ensures CHAP2 is always running with the latest version

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
REPO_URL="https://github.com/your-repo/CHAP2.git"
REPO_DIR="/home/opc/CHAP2"
LOG_FILE="/var/log/chap2-startup.log"
LOCK_FILE="/tmp/chap2-startup.lock"
MAX_RETRIES=3
RETRY_DELAY=30

# Function to print colored output
print_status() {
    echo -e "${BLUE}[$(date '+%Y-%m-%d %H:%M:%S')] [INFO]${NC} $1" | tee -a "$LOG_FILE"
}

print_success() {
    echo -e "${GREEN}[$(date '+%Y-%m-%d %H:%M:%S')] [SUCCESS]${NC} $1" | tee -a "$LOG_FILE"
}

print_warning() {
    echo -e "${YELLOW}[$(date '+%Y-%m-%d %H:%M:%S')] [WARNING]${NC} $1" | tee -a "$LOG_FILE"
}

print_error() {
    echo -e "${RED}[$(date '+%Y-%m-%d %H:%M:%S')] [ERROR]${NC} $1" | tee -a "$LOG_FILE"
}

# Function to check if script is already running
check_lock() {
    if [ -f "$LOCK_FILE" ]; then
        local pid=$(cat "$LOCK_FILE" 2>/dev/null)
        if ps -p "$pid" > /dev/null 2>&1; then
            print_warning "Startup script already running (PID: $pid)"
            exit 1
        else
            print_warning "Removing stale lock file"
            rm -f "$LOCK_FILE"
        fi
    fi
    echo $$ > "$LOCK_FILE"
}

# Function to cleanup lock file
cleanup() {
    rm -f "$LOCK_FILE"
    print_status "Startup script completed"
}

# Set trap to cleanup on exit
trap cleanup EXIT

# Function to check if Docker is running
check_docker() {
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed"
        return 1
    fi
    
    if ! docker info &> /dev/null; then
        print_error "Docker is not running"
        return 1
    fi
    
    print_success "Docker is running"
    return 0
}

# Function to check network connectivity
check_network() {
    local retries=0
    while [ $retries -lt 3 ]; do
        if ping -c 1 google.com &> /dev/null; then
            print_success "Network connectivity confirmed"
            return 0
        fi
        retries=$((retries + 1))
        print_warning "Network connectivity check failed (attempt $retries/3)"
        sleep 5
    done
    
    print_error "Network connectivity failed"
    return 1
}

# Function to clone or update repository
update_repository() {
    print_status "Checking repository..."
    
    if [ ! -d "$REPO_DIR" ]; then
        print_status "Cloning repository..."
        git clone "$REPO_URL" "$REPO_DIR" || {
            print_error "Failed to clone repository"
            return 1
        }
    else
        print_status "Updating repository..."
        cd "$REPO_DIR"
        git fetch origin || {
            print_warning "Failed to fetch updates"
            return 1
        }
        
        # Check if there are updates
        local current_commit=$(git rev-parse HEAD)
        local remote_commit=$(git rev-parse origin/main)
        
        if [ "$current_commit" != "$remote_commit" ]; then
            print_status "Updates found, pulling latest changes..."
            git pull origin main || {
                print_error "Failed to pull updates"
                return 1
            }
            print_success "Repository updated"
        else
            print_status "Repository is up to date"
        fi
    fi
    
    return 0
}

# Function to check if services are running
check_services() {
    print_status "Checking service status..."
    
    if ! docker-compose -f "$REPO_DIR/.deploy/docker/docker-compose.oracle-cloud.yml" ps | grep -q "Up"; then
        print_warning "Services are not running"
        return 1
    fi
    
    # Check API health
    local retries=0
    while [ $retries -lt 5 ]; do
        if curl -f http://localhost:5000/api/health/ping &> /dev/null; then
            print_success "API service is healthy"
            return 0
        fi
        retries=$((retries + 1))
        print_warning "API health check failed (attempt $retries/5)"
        sleep 10
    done
    
    print_error "API service is not responding"
    return 1
}

# Function to deploy services
deploy_services() {
    print_status "Deploying services..."
    
    cd "$REPO_DIR"
    
    # Make deployment script executable
    chmod +x deploy-oracle-cloud.sh
    
    # Stop existing services
    print_status "Stopping existing services..."
    ./deploy-oracle-cloud.sh stop || true
    
    # Wait a moment for services to stop
    sleep 5
    
    # Deploy services
    print_status "Starting services..."
    ./deploy-oracle-cloud.sh deploy || {
        print_error "Failed to deploy services"
        return 1
    }
    
    # Wait for services to be ready
    print_status "Waiting for services to be ready..."
    local retries=0
    while [ $retries -lt 30 ]; do
        if curl -f http://localhost:5000/api/health/ping &> /dev/null; then
            print_success "Services are ready"
            return 0
        fi
        retries=$((retries + 1))
        print_status "Waiting for services... (attempt $retries/30)"
        sleep 10
    done
    
    print_error "Services failed to start"
    return 1
}

# Function to restart services
restart_services() {
    print_status "Restarting services..."
    
    cd "$REPO_DIR"
    
    # Stop services
    ./deploy-oracle-cloud.sh stop || true
    
    # Wait a moment
    sleep 5
    
    # Start services
    ./deploy-oracle-cloud.sh deploy || {
        print_error "Failed to restart services"
        return 1
    }
    
    return 0
}

# Function to monitor services
monitor_services() {
    print_status "Starting service monitoring..."
    
    while true; do
        # Check if services are running
        if ! check_services; then
            print_warning "Services are not healthy, attempting restart..."
            restart_services
        fi
        
        # Check disk space
        local disk_usage=$(df / | awk 'NR==2 {print $5}' | sed 's/%//')
        if [ "$disk_usage" -gt 80 ]; then
            print_warning "Disk usage is high: ${disk_usage}%"
            docker system prune -f
        fi
        
        # Check memory usage
        local mem_usage=$(free | awk 'NR==2{printf "%.0f", $3*100/$2}')
        if [ "$mem_usage" -gt 80 ]; then
            print_warning "Memory usage is high: ${mem_usage}%"
        fi
        
        # Sleep for 5 minutes
        sleep 300
    done
}

# Function to send notification (optional)
send_notification() {
    local message="$1"
    # You can add notification logic here (email, Slack, etc.)
    # Example: curl -X POST -H 'Content-type: application/json' --data "{\"text\":\"$message\"}" $SLACK_WEBHOOK_URL
    print_status "Notification: $message"
}

# Main startup logic
main() {
    print_status "Starting CHAP2 Oracle Cloud startup script..."
    
    # Check lock file
    check_lock
    
    # Wait for system to be fully booted
    print_status "Waiting for system to be ready..."
    sleep 30
    
    # Check Docker
    if ! check_docker; then
        print_error "Docker is not available, exiting"
        exit 1
    fi
    
    # Check network
    if ! check_network; then
        print_error "Network is not available, exiting"
        exit 1
    fi
    
    # Update repository
    if ! update_repository; then
        print_error "Failed to update repository"
        exit 1
    fi
    
    # Check if services are already running
    if check_services; then
        print_success "Services are already running and healthy"
        send_notification "CHAP2 services are running and healthy"
    else
        print_status "Services need to be deployed or restarted"
        
        # Deploy services
        if deploy_services; then
            print_success "Services deployed successfully"
            send_notification "CHAP2 services deployed successfully"
        else
            print_error "Failed to deploy services"
            send_notification "CHAP2 services deployment failed"
            exit 1
        fi
    fi
    
    # Show service status
    print_status "Service URLs:"
    local public_ip=$(curl -s ifconfig.me)
    echo "  Web Portal: http://$public_ip:5001" | tee -a "$LOG_FILE"
    echo "  API: http://$public_ip:5000" | tee -a "$LOG_FILE"
    echo "  API Health: http://$public_ip:5000/api/health/ping" | tee -a "$LOG_FILE"
    
    # Start monitoring in background
    print_status "Starting background monitoring..."
    monitor_services &
    
    print_success "Startup script completed successfully"
}

# Run main function
main "$@" 