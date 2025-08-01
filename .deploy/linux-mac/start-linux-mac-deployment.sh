#!/bin/bash

echo "========================================"
echo "CHAP2 Linux/Mac Deployment"
echo "========================================"
echo

echo "Checking Docker and Ollama..."
docker --version
if command -v ollama &> /dev/null; then
    echo "Ollama found: $(ollama --version)"
    echo "Available models:"
    ollama list
else
    echo "Warning: Ollama not found. Please install Ollama first."
    echo "Visit: https://ollama.ai/download"
fi
echo

echo "Stopping any existing containers..."
docker-compose down
echo

echo "Building and starting services..."
docker-compose up -d --build
echo

echo "Waiting 30 seconds for services to start..."
sleep 30
echo

echo "Checking container status:"
docker ps
echo

echo "Getting server IP address:"
if command -v ip &> /dev/null; then
    # Linux
    ip route get 1.1.1.1 | awk '{print $7; exit}'
elif command -v ifconfig &> /dev/null; then
    # macOS
    ifconfig | grep "inet " | grep -v 127.0.0.1 | awk '{print $2}' | head -1
else
    echo "Could not determine IP address automatically"
fi
echo

echo "Testing connections:"
echo

echo "Testing Web Portal (port 5000):"
curl -s -I http://localhost:5000
echo

echo "Testing API (port 5001):"
curl -s -I http://localhost:5001
echo

echo "Testing LangChain Service (port 8000):"
curl -s -I http://localhost:8000/health
echo

echo "Testing Qdrant (port 6333):"
curl -s -I http://localhost:6333
echo

echo
echo "Access URLs (replace SERVER_IP with your actual IP):"
echo "- Web Portal: http://SERVER_IP:5000"
echo "- API: http://SERVER_IP:5001"
echo "- LangChain: http://SERVER_IP:8000"
echo "- Qdrant: http://SERVER_IP:6333"
echo

echo "To view logs:"
echo "docker-compose logs -f"
echo

echo "To stop services:"
echo "docker-compose down"
echo

echo "Press any key to continue..."
read -n 1 