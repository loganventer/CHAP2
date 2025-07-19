# 🍓 CHAP2 Raspberry Pi Deployment Guide

This guide provides step-by-step instructions for deploying the CHAP2 Musical Chorus Management System on a Raspberry Pi using Docker.

## 📋 Prerequisites

### Hardware Requirements
- **Raspberry Pi 4** (recommended) with at least 2GB RAM
- **8GB+ microSD card** (Class 10 recommended)
- **Power supply** (5V/3A recommended)
- **Network connection** (WiFi or Ethernet)

### Software Requirements
- **Raspberry Pi OS** (64-bit recommended)
- **Docker** and **Docker Compose**
- **Git** (for cloning the repository)

## 🚀 Quick Start

### Step 1: Prepare Your Raspberry Pi

1. **Flash Raspberry Pi OS** to your microSD card
2. **Enable SSH** and **WiFi** (if using WiFi)
3. **Connect to your Raspberry Pi**:
   ```bash
   ssh pi@your-raspberry-pi-ip
   ```

### Step 2: Install Docker

Run the setup script on your Raspberry Pi:

```bash
# Download the setup script
wget https://raw.githubusercontent.com/your-repo/CHAP2/main/raspberry-pi-setup.sh

# Make it executable
chmod +x raspberry-pi-setup.sh

# Run the setup
./raspberry-pi-setup.sh
```

**Or install manually:**
```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Add user to docker group
sudo usermod -aG docker $USER

# Reboot
sudo reboot
```

### Step 3: Clone the Repository

```bash
# Clone the repository
git clone https://github.com/your-repo/CHAP2.git
cd CHAP2

# Make deployment script executable
chmod +x deploy-raspberry-pi.sh
```

### Step 4: Deploy the Application

```bash
# Deploy the application
./deploy-raspberry-pi.sh deploy
```

### Step 5: Access the Application

After deployment, you can access:
- **Web Portal**: `http://your-raspberry-pi-ip:5001`
- **API**: `http://your-raspberry-pi-ip:5000`
- **API Health**: `http://your-raspberry-pi-ip:5000/api/health/ping`

## 📋 Available Commands

### Deployment Script
```bash
./deploy-raspberry-pi.sh deploy    # Build and start services
./deploy-raspberry-pi.sh stop      # Stop all services
./deploy-raspberry-pi.sh logs      # Show container logs
./deploy-raspberry-pi.sh status    # Show service status
./deploy-raspberry-pi.sh help      # Show help
```

### Docker Compose Commands
```bash
# Build and start
docker-compose -f docker-compose.raspberry-pi.yml up --build -d

# View logs
docker-compose -f docker-compose.raspberry-pi.yml logs -f

# Stop services
docker-compose -f docker-compose.raspberry-pi.yml down

# Restart services
docker-compose -f docker-compose.raspberry-pi.yml restart
```

## 🏗️ Architecture

### Services
- **chap2-api-arm64**: CHAP2.Chorus.Api service (ARM64)
  - Port: 5000 (HTTP), 7000 (HTTPS)
  - Health check: `/api/health/ping`
  - Data volume: `./data/chorus`

- **chap2-web-arm64**: CHAP2.WebPortal service (ARM64)
  - Port: 5001 (HTTP), 7001 (HTTPS)
  - Depends on: chap2-api-arm64
  - Environment: `ApiBaseUrl=http://chap2-api`

### Network
- **chap2-network**: Bridge network for inter-service communication

### Volumes
- **chorus-data**: Persistent storage for chorus files

## 🔧 Configuration

### Environment Variables

#### API Service
```yaml
ASPNETCORE_ENVIRONMENT: Production
ASPNETCORE_URLS: http://+:80;https://+:443
```

#### Web Portal Service
```yaml
ASPNETCORE_ENVIRONMENT: Production
ASPNETCORE_URLS: http://+:80;https://+:443
ApiBaseUrl: http://chap2-api
```

### Port Mapping
- API: `5000:80` (HTTP), `7000:443` (HTTPS)
- Web Portal: `5001:80` (HTTP), `7001:443` (HTTPS)

### Data Persistence
- Chorus files are stored in `./data/chorus` directory
- This directory is mounted as a volume in the API container

## 🧪 Testing the Deployment

### 1. Health Check
```bash
curl http://your-raspberry-pi-ip:5000/api/health/ping
```

### 2. API Endpoints
```bash
# Get all choruses
curl http://your-raspberry-pi-ip:5000/api/choruses

# Search choruses
curl "http://your-raspberry-pi-ip:5000/api/choruses/search?q=grace"

# Create a new chorus
curl -X POST http://your-raspberry-pi-ip:5000/api/choruses \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Chorus","chorusText":"Test lyrics","key":1,"type":1,"timeSignature":1}'
```

### 3. Web Portal
- Open `http://your-raspberry-pi-ip:5001` in your browser
- Test search functionality
- Test chorus creation and editing

## 🔍 Troubleshooting

### Common Issues

#### 1. Build Failures
```bash
# Check available memory
free -h

# Check disk space
df -h

# Clean Docker cache
docker system prune -a

# Rebuild without cache
docker-compose -f docker-compose.raspberry-pi.yml build --no-cache
```

#### 2. Out of Memory
```bash
# Check memory usage
docker stats

# Increase swap space
sudo dphys-swapfile swapoff
sudo nano /etc/dphys-swapfile
# Set CONF_SWAPSIZE=2048
sudo dphys-swapfile setup
sudo dphys-swapfile swapon
```

#### 3. Slow Build Times
```bash
# Use a faster microSD card (Class 10+)
# Ensure adequate power supply (5V/3A)
# Close unnecessary applications
# Consider using an external SSD
```

#### 4. Network Issues
```bash
# Check network connectivity
ping google.com

# Check if ports are open
netstat -tulpn | grep :5000
netstat -tulpn | grep :5001

# Check firewall
sudo ufw status
```

### Debug Commands

#### View Container Logs
```bash
# All services
docker-compose -f docker-compose.raspberry-pi.yml logs -f

# Specific service
docker-compose -f docker-compose.raspberry-pi.yml logs -f chap2-api-arm64
docker-compose -f docker-compose.raspberry-pi.yml logs -f chap2-web-arm64
```

#### Access Container Shell
```bash
# API container
docker exec -it chap2-api-arm64 /bin/bash

# Web Portal container
docker exec -it chap2-web-arm64 /bin/bash
```

#### Check Container Resources
```bash
# Resource usage
docker stats

# Container details
docker inspect chap2-api-arm64
docker inspect chap2-web-arm64
```

## 🔒 Security Considerations

### Production Deployment
1. **Change default passwords** for Raspberry Pi
2. **Use HTTPS** in production
3. **Configure firewall** rules
4. **Regular updates** for Raspberry Pi OS
5. **Backup data** regularly

### Security Best Practices
```bash
# Update Raspberry Pi OS
sudo apt update && sudo apt upgrade

# Configure firewall
sudo ufw enable
sudo ufw allow ssh
sudo ufw allow 5000
sudo ufw allow 5001

# Change default password
passwd
```

## 📊 Monitoring

### System Monitoring
```bash
# Check CPU usage
htop

# Check memory usage
free -h

# Check disk usage
df -h

# Check temperature
vcgencmd measure_temp
```

### Application Monitoring
```bash
# Check container status
docker-compose -f docker-compose.raspberry-pi.yml ps

# Check application logs
docker-compose -f docker-compose.raspberry-pi.yml logs -f

# Monitor resource usage
docker stats
```

## 🔄 Updates and Maintenance

### Updating the Application
```bash
# Pull latest changes
git pull

# Rebuild and restart
./deploy-raspberry-pi.sh deploy
```

### Backup and Restore
```bash
# Backup chorus data
tar -czf chorus-backup-$(date +%Y%m%d).tar.gz ./data/chorus/

# Restore chorus data
tar -xzf chorus-backup-20240101.tar.gz
```

### System Maintenance
```bash
# Update system packages
sudo apt update && sudo apt upgrade

# Clean Docker
docker system prune -a

# Check disk space
df -h
```

## 📱 Mobile Access

### Network Access
- **Local Network**: Access from any device on your network
- **Port Forwarding**: Configure router for internet access
- **Dynamic DNS**: Use services like No-IP for remote access

### Mobile Optimization
- The web portal is responsive and works on mobile devices
- Touch-friendly interface
- Optimized for small screens

## 🎯 Performance Tips

### Hardware Optimization
1. **Use a fast microSD card** (Class 10 or better)
2. **Ensure adequate power supply** (5V/3A)
3. **Add cooling** if running continuously
4. **Consider external SSD** for better performance

### Software Optimization
1. **Close unnecessary services** on Raspberry Pi
2. **Use production builds** (Release mode)
3. **Monitor resource usage** regularly
4. **Restart services** periodically

## 🤝 Contributing

When contributing to Raspberry Pi deployment:

1. Test on actual Raspberry Pi hardware
2. Consider ARM64 architecture limitations
3. Optimize for resource constraints
4. Document performance characteristics

## 📄 License

This Raspberry Pi deployment is part of the CHAP2 system. 