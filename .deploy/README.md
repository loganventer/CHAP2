# CHAP2 Deployment Configurations

This folder contains organized deployment configurations for different environments.

## Folder Structure

### Windows GPU Deployment
- **Location**: `.deploy/windows-gpu/`
- **Description**: Windows deployment with CUDA container toolkit for GPU acceleration
- **Features**: 
  - GPU acceleration via CUDA container toolkit
  - Host networking for external access
  - Windows-specific optimizations

### Linux/Mac Deployment  
- **Location**: `.deploy/linux-mac/`
- **Description**: Linux/Mac deployment using local Ollama on network
- **Features**:
  - Local Ollama service on network
  - Cross-platform compatibility
  - Standard Docker networking

## Usage

### Windows GPU Deployment
```bash
cd .deploy/windows-gpu/
./start-gpu-deployment.bat
```

### Linux/Mac Deployment
```bash
cd .deploy/linux-mac/
./start-linux-mac-deployment.sh
```

## Access URLs

After deployment, access the services at:
- **Web Portal**: http://SERVER_IP:5000
- **API**: http://SERVER_IP:5001  
- **LangChain**: http://SERVER_IP:8000
- **Qdrant**: http://SERVER_IP:6333

Replace `SERVER_IP` with your actual server IP address. 