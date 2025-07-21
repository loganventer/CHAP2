#!/bin/bash

# CHAP2 Oracle Cloud Auto-Startup Installation Script
# This script installs the auto-startup system on Oracle Cloud

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

# Function to check if running as root
check_root() {
    if [ "$EUID" -ne 0 ]; then
        print_error "This script must be run as root (use sudo)"
        exit 1
    fi
}

# Function to install required packages
install_packages() {
    print_status "Installing required packages..."
    
    # Update package list
    dnf update -y
    
    # Install required packages
    dnf install -y \
        git \
        curl \
        wget \
        jq \
        cronie \
        logrotate
    
    print_success "Required packages installed"
}

# Function to setup log rotation
setup_log_rotation() {
    print_status "Setting up log rotation..."
    
    # Create logrotate configuration
    cat > /etc/logrotate.d/chap2-startup << 'EOF'
/var/log/chap2-startup.log {
    daily
    missingok
    rotate 7
    compress
    delaycompress
    notifempty
    create 644 opc opc
    postrotate
        systemctl reload rsyslog >/dev/null 2>&1 || true
    endscript
}
EOF
    
    print_success "Log rotation configured"
}

# Function to setup monitoring script
setup_monitoring() {
    print_status "Setting up monitoring script..."
    
    # Create monitoring script
    cat > /home/opc/chap2-monitor.sh << 'EOF'
#!/bin/bash

# CHAP2 Service Monitoring Script
# This script monitors CHAP2 services and restarts them if needed

LOG_FILE="/var/log/chap2-monitor.log"
REPO_DIR="/home/opc/CHAP2"

# Function to log messages
log_message() {
    echo "$(date '+%Y-%m-%d %H:%M:%S') - $1" >> "$LOG_FILE"
}

# Check if services are running
if ! docker-compose -f "$REPO_DIR/.deploy/docker/docker-compose.oracle-cloud.yml" ps | grep -q "Up"; then
    log_message "Services are not running, attempting restart"
    cd "$REPO_DIR"
    ./deploy-oracle-cloud.sh deploy
    log_message "Service restart completed"
fi

# Check API health
if ! curl -f http://localhost:5000/api/health/ping &> /dev/null; then
    log_message "API health check failed, restarting services"
    cd "$REPO_DIR"
    ./deploy-oracle-cloud.sh stop
    sleep 10
    ./deploy-oracle-cloud.sh deploy
    log_message "Service restart completed"
fi

# Check disk space
DISK_USAGE=$(df / | awk 'NR==2 {print $5}' | sed 's/%//')
if [ "$DISK_USAGE" -gt 80 ]; then
    log_message "Disk usage is high: ${DISK_USAGE}%, cleaning Docker"
    docker system prune -f
    log_message "Docker cleanup completed"
fi

# Check memory usage
MEM_USAGE=$(free | awk 'NR==2{printf "%.0f", $3*100/$2}')
if [ "$MEM_USAGE" -gt 80 ]; then
    log_message "Memory usage is high: ${MEM_USAGE}%"
fi
EOF
    
    # Make monitoring script executable
    chmod +x /home/opc/chap2-monitor.sh
    
    print_success "Monitoring script created"
}

# Function to setup cron job for monitoring
setup_cron() {
    print_status "Setting up cron job for monitoring..."
    
    # Add cron job to run monitoring every 5 minutes
    (crontab -u opc -l 2>/dev/null; echo "*/5 * * * * /home/opc/chap2-monitor.sh") | crontab -u opc -
    
    print_success "Cron job configured"
}

# Function to setup systemd service
setup_systemd() {
    print_status "Setting up systemd service..."
    
    # Copy service file
    cp chap2-startup.service /etc/systemd/system/
    
    # Reload systemd
    systemctl daemon-reload
    
    # Enable service
    systemctl enable chap2-startup.service
    
    print_success "Systemd service configured"
}

# Function to setup backup script
setup_backup() {
    print_status "Setting up backup script..."
    
    # Create backup script
    cat > /home/opc/chap2-backup.sh << 'EOF'
#!/bin/bash

# CHAP2 Backup Script
# This script creates backups of CHAP2 data

BACKUP_DIR="/home/opc/backups"
REPO_DIR="/home/opc/CHAP2"
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="chap2-backup-$DATE.tar.gz"

# Create backup directory
mkdir -p "$BACKUP_DIR"

# Create backup
cd "$REPO_DIR"
tar -czf "$BACKUP_DIR/$BACKUP_FILE" data/

# Keep only last 7 backups
cd "$BACKUP_DIR"
ls -t chap2-backup-*.tar.gz | tail -n +8 | xargs -r rm

echo "Backup created: $BACKUP_FILE"
EOF
    
    # Make backup script executable
    chmod +x /home/opc/chap2-backup.sh
    
    # Add daily backup cron job
    (crontab -u opc -l 2>/dev/null; echo "0 2 * * * /home/opc/chap2-backup.sh") | crontab -u opc -
    
    print_success "Backup script configured"
}

# Function to setup notification (optional)
setup_notification() {
    print_status "Setting up notification system..."
    
    # Create notification script template
    cat > /home/opc/chap2-notify.sh << 'EOF'
#!/bin/bash

# CHAP2 Notification Script
# Configure this script to send notifications (email, Slack, etc.)

MESSAGE="$1"
WEBHOOK_URL="YOUR_WEBHOOK_URL_HERE"

# Example: Send to Slack
# curl -X POST -H 'Content-type: application/json' \
#   --data "{\"text\":\"CHAP2: $MESSAGE\"}" \
#   "$WEBHOOK_URL"

# Example: Send email
# echo "$MESSAGE" | mail -s "CHAP2 Alert" your-email@example.com

echo "Notification: $MESSAGE"
EOF
    
    # Make notification script executable
    chmod +x /home/opc/chap2-notify.sh
    
    print_success "Notification script created (configure manually)"
}

# Function to create startup script
create_startup_script() {
    print_status "Creating startup script..."
    
    # Download startup script
    wget -O /home/opc/oracle-cloud-startup.sh https://raw.githubusercontent.com/your-repo/CHAP2/main/oracle-cloud-startup.sh
    
    # Make startup script executable
    chmod +x /home/opc/oracle-cloud-startup.sh
    
    print_success "Startup script created"
}

# Function to setup permissions
setup_permissions() {
    print_status "Setting up permissions..."
    
    # Set ownership
    chown -R opc:opc /home/opc
    
    # Set permissions
    chmod 755 /home/opc
    chmod 644 /home/opc/*.sh
    
    print_success "Permissions configured"
}

# Function to show status
show_status() {
    print_status "Auto-startup system status:"
    echo ""
    echo "✅ Systemd Service:"
    systemctl status chap2-startup.service --no-pager -l
    echo ""
    echo "✅ Cron Jobs:"
    crontab -u opc -l
    echo ""
    echo "✅ Log Files:"
    echo "  - Startup Log: /var/log/chap2-startup.log"
    echo "  - Monitor Log: /var/log/chap2-monitor.log"
    echo ""
    echo "✅ Scripts:"
    echo "  - Startup: /home/opc/oracle-cloud-startup.sh"
    echo "  - Monitor: /home/opc/chap2-monitor.sh"
    echo "  - Backup: /home/opc/chap2-backup.sh"
    echo "  - Notify: /home/opc/chap2-notify.sh"
    echo ""
    echo "✅ Service URLs:"
    local public_ip=$(curl -s ifconfig.me)
    echo "  - Web Portal: http://$public_ip:5001"
    echo "  - API: http://$public_ip:5000"
    echo "  - API Health: http://$public_ip:5000/api/health/ping"
}

# Function to show help
show_help() {
    echo "CHAP2 Oracle Cloud Auto-Startup Installation"
    echo ""
    echo "Usage: $0 [COMMAND]"
    echo ""
    echo "Commands:"
    echo "  install    Install the auto-startup system (default)"
    echo "  status     Show system status"
    echo "  start      Start the startup service"
    echo "  stop       Stop the startup service"
    echo "  restart    Restart the startup service"
    echo "  logs       Show startup logs"
    echo "  help       Show this help message"
    echo ""
    echo "Examples:"
    echo "  sudo $0 install    # Install auto-startup system"
    echo "  sudo $0 status     # Show system status"
    echo "  sudo $0 logs       # Show logs"
}

# Main installation logic
install_system() {
    print_status "Installing CHAP2 auto-startup system..."
    
    # Check if running as root
    check_root
    
    # Install packages
    install_packages
    
    # Setup components
    setup_log_rotation
    setup_monitoring
    setup_cron
    setup_backup
    setup_notification
    create_startup_script
    setup_systemd
    setup_permissions
    
    print_success "Auto-startup system installed successfully!"
    echo ""
    echo "🎉 CHAP2 auto-startup system is now installed!"
    echo "📝 The system will:"
    echo "  - Start automatically on boot"
    echo "  - Deploy the latest version from GitHub"
    echo "  - Monitor services and restart if needed"
    echo "  - Create daily backups"
    echo "  - Send notifications (configure manually)"
    echo ""
    echo "📋 Next steps:"
    echo "  1. Configure notifications in /home/opc/chap2-notify.sh"
    echo "  2. Test the system: sudo $0 status"
    echo "  3. Reboot to test auto-startup: sudo reboot"
}

# Main script logic
case "${1:-install}" in
    "install")
        install_system
        ;;
    "status")
        show_status
        ;;
    "start")
        systemctl start chap2-startup.service
        print_success "Startup service started"
        ;;
    "stop")
        systemctl stop chap2-startup.service
        print_success "Startup service stopped"
        ;;
    "restart")
        systemctl restart chap2-startup.service
        print_success "Startup service restarted"
        ;;
    "logs")
        journalctl -u chap2-startup.service -f
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