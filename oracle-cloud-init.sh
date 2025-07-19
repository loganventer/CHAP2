#!/bin/bash

# CHAP2 Oracle Cloud Cloud-Init Script
# This script runs during instance creation and sets up everything automatically
# Include this script in the cloud-init configuration when creating your Oracle Cloud instance

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
REPO_URL="https://github.com/loganventer/CHAP2.git"
REPO_DIR="/home/opc/CHAP2"
LOG_FILE="/var/log/chap2-cloud-init.log"
STARTUP_SCRIPT="/home/opc/oracle-cloud-startup.sh"

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

# Function to update system packages
update_system() {
    print_status "Updating system packages..."
    
    # Update package lists
    dnf update -y || {
        print_error "Failed to update package lists"
        return 1
    }
    
    # Install essential packages
    dnf install -y \
        git \
        curl \
        wget \
        unzip \
        tar \
        gzip \
        bzip2 \
        vim \
        htop \
        net-tools \
        bind-utils \
        telnet \
        nc \
        jq \
        yum-utils \
        device-mapper-persistent-data \
        lvm2 || {
        print_error "Failed to install essential packages"
        return 1
    }
    
    print_success "System packages updated and essential packages installed"
}

# Function to install Docker
install_docker() {
    print_status "Installing Docker..."
    
    # Remove any existing Docker installations
    dnf remove -y docker docker-client docker-client-latest docker-common docker-latest docker-latest-logrotate docker-logrotate docker-engine docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin || true
    
    # Add Docker repository
    dnf config-manager --add-repo https://download.docker.com/linux/centos/docker-ce.repo || {
        print_error "Failed to add Docker repository"
        return 1
    }
    
    # Install Docker
    dnf install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin || {
        print_error "Failed to install Docker"
        return 1
    }
    
    # Start and enable Docker
    systemctl start docker || {
        print_error "Failed to start Docker service"
        return 1
    }
    
    systemctl enable docker || {
        print_error "Failed to enable Docker service"
        return 1
    }
    
    # Add opc user to docker group
    usermod -aG docker opc || {
        print_error "Failed to add opc user to docker group"
        return 1
    }
    
    # Verify Docker installation
    if ! docker --version &> /dev/null; then
        print_error "Docker installation verification failed"
        return 1
    fi
    
    print_success "Docker installed and configured successfully"
}

# Function to install Docker Compose
install_docker_compose() {
    print_status "Installing Docker Compose..."
    
    # Install Docker Compose using pip (more reliable on Oracle Linux)
    dnf install -y python3-pip || {
        print_error "Failed to install pip"
        return 1
    }
    
    pip3 install docker-compose || {
        print_error "Failed to install docker-compose"
        return 1
    }
    
    # Verify installation
    if ! docker-compose --version &> /dev/null; then
        print_error "Docker Compose installation verification failed"
        return 1
    fi
    
    print_success "Docker Compose installed successfully"
}

# Function to configure firewall
configure_firewall() {
    print_status "Configuring firewall..."
    
    # Install and enable firewalld
    dnf install -y firewalld || {
        print_error "Failed to install firewalld"
        return 1
    }
    
    systemctl start firewalld || {
        print_error "Failed to start firewalld"
        return 1
    }
    
    systemctl enable firewalld || {
        print_error "Failed to enable firewalld"
        return 1
    }
    
    # Configure firewall rules
    firewall-cmd --permanent --add-service=ssh || true
    firewall-cmd --permanent --add-port=80/tcp || true
    firewall-cmd --permanent --add-port=443/tcp || true
    firewall-cmd --permanent --add-port=5000/tcp || true
    firewall-cmd --permanent --add-port=5001/tcp || true
    
    # Reload firewall
    firewall-cmd --reload || {
        print_error "Failed to reload firewall"
        return 1
    }
    
    print_success "Firewall configured successfully"
}

# Function to clone repository
clone_repository() {
    print_status "Cloning CHAP2 repository..."
    
    # Remove existing directory if it exists
    rm -rf "$REPO_DIR" || true
    
    # Clone repository
    git clone "$REPO_URL" "$REPO_DIR" || {
        print_error "Failed to clone repository"
        return 1
    }
    
    # Set proper permissions
    chown -R opc:opc "$REPO_DIR" || {
        print_error "Failed to set repository permissions"
        return 1
    }
    
    print_success "Repository cloned successfully"
}

# Function to setup startup script
setup_startup_script() {
    print_status "Setting up startup script..."
    
    # Copy startup script to home directory
    if [ -f "$REPO_DIR/oracle-cloud-startup.sh" ]; then
        cp "$REPO_DIR/oracle-cloud-startup.sh" "$STARTUP_SCRIPT" || {
            print_error "Failed to copy startup script"
            return 1
        }
        
        # Make startup script executable
        chmod +x "$STARTUP_SCRIPT" || {
            print_error "Failed to make startup script executable"
            return 1
        }
        
        # Set ownership
        chown opc:opc "$STARTUP_SCRIPT" || {
            print_error "Failed to set startup script ownership"
            return 1
        }
        
        print_success "Startup script configured"
    else
        print_warning "Startup script not found in repository"
    fi
}

# Function to setup systemd service
setup_systemd_service() {
    print_status "Setting up systemd service..."
    
    # Create systemd service file
    cat > /etc/systemd/system/chap2-startup.service << 'EOF'
[Unit]
Description=CHAP2 Auto Startup Service
After=network.target docker.service
Wants=docker.service

[Service]
Type=simple
User=opc
Group=opc
WorkingDirectory=/home/opc
ExecStart=/home/opc/oracle-cloud-startup.sh
Restart=always
RestartSec=30
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
EOF
    
    # Reload systemd
    systemctl daemon-reload || {
        print_error "Failed to reload systemd"
        return 1
    }
    
    # Enable service
    systemctl enable chap2-startup.service || {
        print_error "Failed to enable chap2-startup service"
        return 1
    }
    
    print_success "Systemd service configured"
}

# Function to setup log rotation
setup_log_rotation() {
    print_status "Setting up log rotation..."
    
    # Create logrotate configuration
    cat > /etc/logrotate.d/chap2 << 'EOF'
/var/log/chap2-startup.log {
    daily
    missingok
    rotate 7
    compress
    delaycompress
    notifempty
    create 644 opc opc
    postrotate
        systemctl reload chap2-startup.service > /dev/null 2>&1 || true
    endscript
}

/var/log/chap2-cloud-init.log {
    daily
    missingok
    rotate 7
    compress
    delaycompress
    notifempty
    create 644 opc opc
}
EOF
    
    print_success "Log rotation configured"
}

# Function to setup monitoring
setup_monitoring() {
    print_status "Setting up basic monitoring..."
    
    # Create monitoring script
    cat > /home/opc/monitor-chap2.sh << 'EOF'
#!/bin/bash

# Simple monitoring script for CHAP2
LOG_FILE="/var/log/chap2-monitor.log"

echo "$(date): Checking CHAP2 services..." >> "$LOG_FILE"

# Check if Docker is running
if ! docker info &> /dev/null; then
    echo "$(date): ERROR - Docker is not running" >> "$LOG_FILE"
    systemctl restart docker
fi

# Check if services are responding
if ! curl -f http://localhost:5000/api/health/ping &> /dev/null; then
    echo "$(date): WARNING - API service is not responding" >> "$LOG_FILE"
fi

# Check disk space
DISK_USAGE=$(df / | awk 'NR==2 {print $5}' | sed 's/%//')
if [ "$DISK_USAGE" -gt 80 ]; then
    echo "$(date): WARNING - Disk usage is high: ${DISK_USAGE}%" >> "$LOG_FILE"
    docker system prune -f
fi

# Check memory usage
MEM_USAGE=$(free | awk 'NR==2{printf "%.0f", $3*100/$2}')
if [ "$MEM_USAGE" -gt 80 ]; then
    echo "$(date): WARNING - Memory usage is high: ${MEM_USAGE}%" >> "$LOG_FILE"
fi
EOF
    
    # Make monitoring script executable
    chmod +x /home/opc/monitor-chap2.sh
    
    # Set ownership
    chown opc:opc /home/opc/monitor-chap2.sh
    
    # Add to crontab
    (crontab -u opc -l 2>/dev/null; echo "*/5 * * * * /home/opc/monitor-chap2.sh") | crontab -u opc -
    
    print_success "Monitoring configured"
}

# Function to perform initial deployment
initial_deployment() {
    print_status "Performing initial deployment..."
    
    # Change to repository directory
    cd "$REPO_DIR" || {
        print_error "Failed to change to repository directory"
        return 1
    }
    
    # Make deployment script executable
    chmod +x deploy-oracle-cloud.sh || {
        print_error "Failed to make deployment script executable"
        return 1
    }
    
    # Run initial deployment
    ./deploy-oracle-cloud.sh deploy || {
        print_error "Initial deployment failed"
        return 1
    }
    
    print_success "Initial deployment completed"
}

# Function to create welcome message
create_welcome_message() {
    print_status "Creating welcome message..."
    
    # Create welcome script
    cat > /home/opc/welcome-chap2.sh << 'EOF'
#!/bin/bash

echo ""
echo "🎉 CHAP2 Oracle Cloud Setup Complete!"
echo "======================================"
echo ""
echo "📱 Your CHAP2 application is now running!"
echo ""
echo "🌐 Public URLs:"
echo "  Web Portal: http://$(curl -s ifconfig.me):5001"
echo "  API: http://$(curl -s ifconfig.me):5000"
echo "  API Health: http://$(curl -s ifconfig.me):5000/api/health/ping"
echo ""
echo "🔧 Management Commands:"
echo "  View logs: docker-compose -f /home/opc/CHAP2/docker-compose.oracle-cloud.yml logs"
echo "  Restart: /home/opc/CHAP2/deploy-oracle-cloud.sh deploy"
echo "  Stop: /home/opc/CHAP2/deploy-oracle-cloud.sh stop"
echo "  Status: /home/opc/CHAP2/deploy-oracle-cloud.sh status"
echo ""
echo "📊 Monitoring:"
echo "  Check logs: tail -f /var/log/chap2-startup.log"
echo "  Monitor script: /home/opc/monitor-chap2.sh"
echo ""
echo "🚀 The application will automatically restart on system reboot."
echo ""
EOF
    
    # Make welcome script executable
    chmod +x /home/opc/welcome-chap2.sh
    
    # Set ownership
    chown opc:opc /home/opc/welcome-chap2.sh
    
    print_success "Welcome message created"
}

# Function to finalize setup
finalize_setup() {
    print_status "Finalizing setup..."
    
    # Create a completion marker
    echo "$(date): CHAP2 cloud-init setup completed successfully" > /home/opc/chap2-setup-complete.txt
    
    # Set proper permissions for opc user
    chown -R opc:opc /home/opc/CHAP2 || true
    
    # Display completion message
    print_success "CHAP2 Oracle Cloud setup completed successfully!"
    echo ""
    echo "🎉 Setup Summary:"
    echo "  ✅ System packages updated"
    echo "  ✅ Docker installed and configured"
    echo "  ✅ Docker Compose installed"
    echo "  ✅ Firewall configured"
    echo "  ✅ Repository cloned"
    echo "  ✅ Startup script configured"
    echo "  ✅ Systemd service configured"
    echo "  ✅ Log rotation configured"
    echo "  ✅ Monitoring configured"
    echo "  ✅ Initial deployment completed"
    echo ""
    echo "📱 Your CHAP2 application is ready!"
    echo "🌐 Access it at: http://$(curl -s ifconfig.me):5001"
    echo ""
}

# Main setup function
main() {
    print_status "Starting CHAP2 Oracle Cloud cloud-init setup..."
    
    # Create log file
    touch "$LOG_FILE"
    chown opc:opc "$LOG_FILE"
    
    # Update system
    update_system
    
    # Install Docker
    install_docker
    
    # Install Docker Compose
    install_docker_compose
    
    # Configure firewall
    configure_firewall
    
    # Clone repository
    clone_repository
    
    # Setup startup script
    setup_startup_script
    
    # Setup systemd service
    setup_systemd_service
    
    # Setup log rotation
    setup_log_rotation
    
    # Setup monitoring
    setup_monitoring
    
    # Perform initial deployment
    initial_deployment
    
    # Create welcome message
    create_welcome_message
    
    # Finalize setup
    finalize_setup
    
    print_success "Cloud-init setup completed successfully!"
}

# Run main function
main "$@" 