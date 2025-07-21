# CHAP2 Local Deployment

This guide explains how to deploy CHAP2 locally on your machine using Docker.

## Prerequisites

- Docker Desktop installed and running
- Git (to clone the repository)

## Quick Start

1. **Clone the repository:**
   ```bash
   git clone https://github.com/loganventer/CHAP2.git
   cd CHAP2
   ```

2. **Make the deployment script executable:**
   ```bash
   chmod +x deploy.sh
   ```

3. **Deploy the application:**
   ```bash
   ./deploy.sh
   ```

That's it! The script will automatically:
- Check Docker installation
- Copy sample data if the database is empty
- Build and start the containers
- Wait for services to be ready

## Access Your Application

Once deployed, you can access:

- **Web Portal**: http://localhost:8081
- **API**: http://localhost:8080
- **API Health Check**: http://localhost:8080/api/health/ping

## Sample Data

The deployment automatically includes sample chorus data. You can search for terms like:
- "heer" (Lord)
- "Jesus"
- "God"
- Musical keys like "C", "F", "G"

## Useful Commands

```bash
# View logs
./deploy.sh logs

# Stop services
./deploy.sh stop

# Check status
./deploy.sh status

# Get help
./deploy.sh help
```

## Troubleshooting

### Port Conflicts
If you get port conflicts, the script uses ports 8080 and 8081. Make sure these ports are available.

### Docker Not Running
Make sure Docker Desktop is running before executing the deployment script.

### Permission Issues
If you get permission errors, make sure the deployment script is executable:
```bash
chmod +x deploy.sh
```

## Configuration

The local deployment uses:
- `docker-compose.local.yml` - HTTP-only configuration
- Port 8080 for API
- Port 8081 for Web Portal
- Sample data from `CHAP2.Chorus.Api/data/chorus/`

## Development

For development, you can modify the configuration files:
- `docker-compose.local.yml` - Container configuration
- `CHAP2.UI/CHAP2.WebPortal/appsettings.json` - Web portal settings
- `data/chorus/` - Chorus data directory 