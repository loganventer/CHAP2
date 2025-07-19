# CHAP2 Docker Deployment

This document provides instructions for deploying the CHAP2 Musical Chorus Management System using Docker and Docker Compose.

## 🐳 Prerequisites

- Docker Desktop (or Docker Engine + Docker Compose)
- At least 2GB of available RAM
- At least 1GB of available disk space

## 🚀 Quick Start

### 1. Clone the Repository
```bash
git clone <repository-url>
cd CHAP2
```

### 2. Make the Deployment Script Executable
```bash
chmod +x deploy.sh
```

### 3. Deploy the Application
```bash
./deploy.sh deploy
```

### 4. Access the Application
- **API**: http://localhost:5000
- **Web Portal**: http://localhost:5001
- **API Health Check**: http://localhost:5000/api/health/ping

## 📋 Available Commands

### Deployment Script Commands
```bash
./deploy.sh deploy    # Build and start all services (default)
./deploy.sh stop      # Stop all services
./deploy.sh logs      # Show container logs
./deploy.sh status    # Show service status
./deploy.sh help      # Show help message
```

### Docker Compose Commands
```bash
# Build and start services
docker-compose up --build -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down

# Restart services
docker-compose restart

# View service status
docker-compose ps
```

## 🏗️ Architecture

### Services
- **chap2-api**: CHAP2.Chorus.Api service
  - Port: 5000 (HTTP), 7000 (HTTPS)
  - Health check: `/api/health/ping`
  - Data volume: `./data/chorus`

- **chap2-web**: CHAP2.WebPortal service
  - Port: 5001 (HTTP), 7001 (HTTPS)
  - Depends on: chap2-api
  - Environment: `ApiBaseUrl=http://chap2-api`

### Network
- **chap2-network**: Bridge network for inter-service communication

### Volumes
- **chorus-data**: Persistent storage for chorus files

## 🔧 Configuration

### Environment Variables

#### API Service (chap2-api)
```yaml
ASPNETCORE_ENVIRONMENT: Production
ASPNETCORE_URLS: http://+:80;https://+:443
```

#### Web Portal Service (chap2-web)
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
curl http://localhost:5000/api/health/ping
```

### 2. API Endpoints
```bash
# Get all choruses
curl http://localhost:5000/api/choruses

# Search choruses
curl "http://localhost:5000/api/choruses/search?q=grace"

# Create a new chorus
curl -X POST http://localhost:5000/api/choruses \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Chorus","chorusText":"Test lyrics","key":1,"type":1,"timeSignature":1}'
```

### 3. Web Portal
- Open http://localhost:5001 in your browser
- Test search functionality
- Test chorus creation and editing

## 🔍 Troubleshooting

### Common Issues

#### 1. Port Already in Use
```bash
# Check what's using the ports
lsof -i :5000
lsof -i :5001

# Stop conflicting services
sudo lsof -ti:5000 | xargs kill -9
sudo lsof -ti:5001 | xargs kill -9
```

#### 2. Container Build Failures
```bash
# Clean Docker cache
docker system prune -a

# Rebuild without cache
docker-compose build --no-cache
```

#### 3. Service Not Starting
```bash
# Check container logs
docker-compose logs chap2-api
docker-compose logs chap2-web

# Check container status
docker-compose ps
```

#### 4. API Connection Issues
```bash
# Test API connectivity
curl -v http://localhost:5000/api/health/ping

# Check if containers are on the same network
docker network ls
docker network inspect chap2_chap2-network
```

### Debug Commands

#### View Container Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f chap2-api
docker-compose logs -f chap2-web
```

#### Access Container Shell
```bash
# API container
docker exec -it chap2-api /bin/bash

# Web Portal container
docker exec -it chap2-web /bin/bash
```

#### Check Container Resources
```bash
# Resource usage
docker stats

# Container details
docker inspect chap2-api
docker inspect chap2-web
```

## 🔒 Security Considerations

### Production Deployment
1. **Use HTTPS**: Configure SSL certificates for production
2. **Environment Variables**: Use secrets management for sensitive data
3. **Network Security**: Configure firewall rules appropriately
4. **Data Backup**: Implement regular backups of the `./data/chorus` directory

### Security Best Practices
```bash
# Use Docker secrets for sensitive data
echo "my-secret-api-key" | docker secret create api-key -

# Update docker-compose.yml to use secrets
secrets:
  api-key:
    external: true
```

## 📊 Monitoring

### Health Checks
Both services include health checks:
- API: `curl -f http://localhost/api/health/ping`
- Web Portal: `curl -f http://localhost/`

### Logging
- Application logs are available via `docker-compose logs`
- Logs are also written to stdout/stderr for Docker logging

### Metrics
Consider adding monitoring solutions like:
- Prometheus + Grafana
- ELK Stack (Elasticsearch, Logstash, Kibana)
- Application Insights

## 🔄 Updates and Maintenance

### Updating the Application
```bash
# Pull latest changes
git pull

# Rebuild and restart
./deploy.sh deploy
```

### Backup and Restore
```bash
# Backup chorus data
tar -czf chorus-backup-$(date +%Y%m%d).tar.gz ./data/chorus/

# Restore chorus data
tar -xzf chorus-backup-20240101.tar.gz
```

### Cleanup
```bash
# Remove unused containers and images
docker system prune -a

# Remove volumes (WARNING: This will delete all data)
docker volume prune
```

## 📝 Development with Docker

### Development Mode
For development, you can override the production settings:

```bash
# Use development configuration
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up
```

### Hot Reload
For development with hot reload:
```bash
# Mount source code for hot reload
docker-compose -f docker-compose.dev.yml up
```

## 🤝 Contributing

When contributing to the Docker deployment:

1. Test changes locally first
2. Update documentation
3. Ensure backward compatibility
4. Test with different environments

## 📄 License

This Docker deployment is part of the CHAP2 system. 