# Windows GPU Deployment

This folder contains the deployment configuration for Windows with GPU acceleration using NVIDIA Container Toolkit.

## Prerequisites

1. **Docker Desktop for Windows** with WSL2 backend
2. **NVIDIA Container Toolkit** installed and configured
3. **NVIDIA GPU drivers** installed
4. **Ollama** running locally on the host machine

## Installation

### 1. Install NVIDIA Container Toolkit

```powershell
# Install NVIDIA Container Toolkit
docker run --rm --gpus all nvidia/cuda:11.0-base nvidia-smi
```

### 2. Verify GPU Access

```powershell
# Test GPU access
nvidia-smi
docker run --rm --gpus all nvidia/cuda:11.0-base nvidia-smi
```

## Usage

### Start Deployment

```bash
./start-gpu-deployment.bat
```

### Force GPU Deployment (Skip Detection)

```bash
./start-force-gpu.bat
```

### Stop Deployment

```bash
./stop-deployment.bat
```

### View Logs

```bash
docker-compose logs -f
```

## Configuration

The deployment includes:

- **Qdrant Vector Database**: Port 6333
- **LangChain Service**: Port 8000 (with GPU acceleration)
- **CHAP2 API**: Port 5001
- **CHAP2 Web Portal**: Port 5000

## Access URLs

After deployment, access the services at:
- **Web Portal**: http://SERVER_IP:5000
- **API**: http://SERVER_IP:5001
- **LangChain**: http://SERVER_IP:8000
- **Qdrant**: http://SERVER_IP:6333

Replace `SERVER_IP` with your actual server IP address.

## Troubleshooting

### GPU Not Available

1. Check NVIDIA drivers are installed
2. Verify NVIDIA Container Toolkit is working:
   ```bash
   docker run --rm --gpus all nvidia/cuda:11.0-base nvidia-smi
   ```

### Port Access Issues

1. Check Windows Firewall settings
2. Ensure ports 5000, 5001, 8000, 6333 are open
3. Verify Docker Desktop is running

### Ollama Connection Issues

1. Ensure Ollama is running on the host machine
2. Check Ollama is accessible at http://localhost:11434
3. Verify the model is downloaded: `ollama list` 