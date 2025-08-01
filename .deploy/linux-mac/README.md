# Linux/Mac Deployment

This folder contains the deployment configuration for Linux and macOS using local Ollama on the network.

## Prerequisites

1. **Docker** installed and running
2. **Ollama** installed and running locally
3. **Docker Compose** available

## Installation

### 1. Install Docker

#### Ubuntu/Debian:
```bash
sudo apt update
sudo apt install docker.io docker-compose
sudo usermod -aG docker $USER
```

#### macOS:
```bash
# Install Docker Desktop from https://www.docker.com/products/docker-desktop
```

### 2. Install Ollama

```bash
# Download and install Ollama
curl -fsSL https://ollama.ai/install.sh | sh

# Start Ollama service
ollama serve

# Pull required models
ollama pull mistral
ollama pull nomic-embed-text
```

### 3. Verify Installation

```bash
# Test Docker
docker --version
docker-compose --version

# Test Ollama
ollama --version
ollama list
```

## Usage

### Start Deployment

```bash
chmod +x start-linux-mac-deployment.sh
./start-linux-mac-deployment.sh
```

### Stop Deployment

```bash
chmod +x stop-deployment.sh
./stop-deployment.sh
```

### View Logs

```bash
docker-compose logs -f
```

## Configuration

The deployment includes:

- **Qdrant Vector Database**: Port 6333
- **LangChain Service**: Port 8000
- **CHAP2 API**: Port 5001
- **CHAP2 Web Portal**: Port 8080 (changed from 5000 to avoid macOS AirPlay conflict)

## Access URLs

After deployment, access the services at:
- **Web Portal**: http://SERVER_IP:8080
- **API**: http://SERVER_IP:5001
- **LangChain**: http://SERVER_IP:8000
- **Qdrant**: http://SERVER_IP:6333

Replace `SERVER_IP` with your actual server IP address.

## Troubleshooting

### Ollama Connection Issues

1. Ensure Ollama is running:
   ```bash
   ollama serve
   ```

2. Check Ollama is accessible:
   ```bash
   curl http://localhost:11434/api/tags
   ```

3. Verify models are downloaded:
   ```bash
   ollama list
   ```

### Port Access Issues

1. Check if ports are in use:
   ```bash
   sudo netstat -tulpn | grep :5000
   sudo netstat -tulpn | grep :5001
   sudo netstat -tulpn | grep :8000
   sudo netstat -tulpn | grep :6333
   ```

2. Check firewall settings:
   ```bash
   # Ubuntu/Debian
   sudo ufw status
   
   # macOS
   sudo pfctl -s rules
   ```

### Docker Issues

1. Check Docker is running:
   ```bash
   docker ps
   ```

2. Check Docker Compose:
   ```bash
   docker-compose --version
   ```

3. Restart Docker if needed:
   ```bash
   # Ubuntu/Debian
   sudo systemctl restart docker
   
   # macOS
   # Restart Docker Desktop from the menu
   ``` 