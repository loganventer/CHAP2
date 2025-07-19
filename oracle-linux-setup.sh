#!/bin/bash

# Oracle Linux Docker Setup Script
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

print_status "🚀 Setting up Docker on Oracle Linux..."

# Update system
print_status "Updating system packages..."
sudo dnf update -y
sudo dnf upgrade -y

# Install required packages
print_status "Installing required packages..."
sudo dnf install -y \
    yum-utils \
    device-mapper-persistent-data \
    lvm2 \
    curl \
    wget \
    git

# Add Docker repository
print_status "Adding Docker repository..."
sudo dnf config-manager --add-repo https://download.docker.com/linux/centos/docker-ce.repo

# Install Docker Engine
print_status "Installing Docker Engine..."
sudo dnf install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

# Start and enable Docker
print_status "Starting Docker service..."
sudo systemctl start docker
sudo systemctl enable docker

# Add user to docker group
print_status "Adding user to docker group..."
sudo usermod -aG docker $USER

# Install Docker Compose (if not already installed)
if ! command -v docker-compose &> /dev/null; then
    print_status "Installing Docker Compose..."
    sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    sudo chmod +x /usr/local/bin/docker-compose
fi

# Create symbolic link for docker-compose
if [ ! -f /usr/bin/docker-compose ]; then
    sudo ln -s /usr/local/bin/docker-compose /usr/bin/docker-compose
fi

print_success "✅ Docker installed successfully!"

# Show Docker version
print_status "Docker version:"
docker --version
docker-compose --version

print_warning "🔄 Please log out and back in for group changes to take effect"
print_warning "📝 Or run: newgrp docker"

print_status "🧪 Testing Docker installation..."
sudo docker run hello-world

print_success "🎉 Docker setup completed successfully!"
print_status "📝 Next steps:"
print_status "   1. Log out and back in: exit && ssh opc@your-instance-ip"
print_status "   2. Clone the CHAP2 repository"
print_status "   3. Run the deployment script" 