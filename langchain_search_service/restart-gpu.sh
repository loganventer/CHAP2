#!/bin/bash

echo "🚀 Restarting services with GPU support..."

# Stop existing containers
echo "📦 Stopping existing containers..."
docker-compose down

# Start with GPU support
echo "🔧 Starting containers with GPU support..."
docker-compose up -d

# Wait for services to be ready
echo "⏳ Waiting for services to be ready..."
sleep 30

# Check GPU usage
echo "📊 Checking GPU usage..."
docker exec -it langchain_search_service-ollama-1 nvidia-smi 2>/dev/null || echo "⚠️  GPU not detected or nvidia-smi not available"

echo "✅ Services restarted with GPU support!"
echo ""
echo "🌐 Service URLs:"
echo "- Web Portal: http://localhost:5002"
echo "- API: http://localhost:5001"
echo "- LangChain: http://localhost:8000"
echo "- Qdrant: http://localhost:6333"
echo "- Ollama: http://localhost:11434"
echo ""
echo "🔍 To monitor GPU usage:"
echo "   docker exec -it langchain_search_service-ollama-1 nvidia-smi" 