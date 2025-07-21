# Deployment Scripts Organization

This directory contains all deployment-related scripts and configuration files organized by deployment target and purpose.

## Directory Structure

### `/docker/`
Docker-related configuration files and documentation
- Docker Compose files for different environments
- Docker documentation
- Docker ignore file

### `/oracle-cloud/`
Oracle Cloud Infrastructure deployment scripts and configuration
- Cloud initialization scripts
- Oracle Cloud setup documentation
- Oracle Linux setup scripts

### `/raspberry-pi/`
Raspberry Pi deployment scripts and documentation
- Pi-specific setup scripts
- Raspberry Pi deployment documentation

### `/auto-startup/`
Auto-startup service configuration for systemd
- Systemd service file
- Auto-startup installation script
- Auto-startup documentation

### `/nginx/`
Nginx configuration files
- Nginx configuration for reverse proxy

### Root Level
General deployment scripts and documentation
- Main deployment scripts (`deploy.sh`, `deploy-oracle-cloud.sh`, `deploy-raspberry-pi.sh`, `deploy-local-docker.sh`)
- Shared utilities (`deploy-utils.sh`)
- Main documentation (`README.md`)

## Quick Start

### Local Docker Deployment
```bash
./deploy/deploy-local-docker.sh
```

**What this does:**
- Deploys CHAP2 using Docker containers for local development
- Uses Development environment with hot-reload capabilities
- Runs on ports 8080 (API) and 8081 (Web Portal)
- Persists data in `./data/chorus` directory
- Ideal for development, testing, and local experimentation

### Production Deployment
```bash
./deploy/deploy.sh
```

### Oracle Cloud Deployment
```bash
./deploy/deploy-oracle-cloud.sh
```

### Raspberry Pi Deployment
```bash
./deploy/deploy-raspberry-pi.sh
```

## Deployment Types

### Local Docker Deployment (`deploy-local-docker.sh`)
- **Purpose**: Local development and testing using Docker
- **Environment**: Development (with hot-reload)
- **Ports**: 8080 (API), 8081 (Web Portal)
- **Data**: Local `./data/chorus` directory
- **Use case**: Daily development, debugging, testing

### Production (`deploy.sh`)
- **Purpose**: Production deployment
- **Environment**: Production
- **Ports**: 5000 (API), 5001 (Web Portal)
- **Data**: Docker volumes
- **Use case**: Production servers, staging environments

### Oracle Cloud (`deploy-oracle-cloud.sh`)
- **Purpose**: Oracle Cloud Infrastructure deployment
- **Environment**: Production
- **Ports**: 5000 (API), 5001 (Web Portal)
- **Data**: Docker volumes
- **Use case**: Cloud deployment on Oracle Linux

### Raspberry Pi (`deploy-raspberry-pi.sh`)
- **Purpose**: Raspberry Pi deployment
- **Environment**: Production
- **Ports**: 5000 (API), 5001 (Web Portal)
- **Data**: Docker volumes
- **Use case**: Home servers, IoT deployments

## Script Options

All deployment scripts support the following options:
- `--help, -h`: Show help message
- `--stop`: Stop the deployment
- `--logs`: Show container logs
- `--restart`: Restart the deployment

## Documentation

Each subdirectory contains specific deployment configurations for different targets:

- **`auto-startup/AUTO_STARTUP_README.md`** - Comprehensive auto-startup system guide
- **`docker/DOCKER_README.md`** - Complete Docker deployment documentation
- **`oracle-cloud/ORACLE_CLOUD_README.md`** - Oracle Cloud deployment guide
- **`oracle-cloud/oracle-cloud-setup.md`** - Detailed Oracle Cloud setup instructions
- **`raspberry-pi/RASPBERRY_PI_README.md`** - Raspberry Pi deployment guide 