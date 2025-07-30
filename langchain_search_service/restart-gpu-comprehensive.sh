#!/bin/bash

echo "🚀 Restarting comprehensive deployment with GPU fixes..."

# Stop existing containers
echo "📦 Stopping existing containers..."
docker-compose -f docker-compose.gpu.yml down

# Remove any existing Ollama models to force re-download with GPU
echo "🗑️  Removing existing Ollama models to force GPU re-download..."
docker volume rm langchain_search_service_ollama_models 2>/dev/null || echo "No existing models to remove"

# Start with GPU support
echo "🔧 Starting containers with comprehensive GPU support..."
docker-compose -f docker-compose.gpu.yml up -d

# Wait for services to be ready
echo "⏳ Waiting for services to be ready..."
sleep 30

# Pull models with GPU support
echo "📦 Pulling models with GPU support..."
docker exec -it langchain_search_service-ollama-1 ollama pull mistral
docker exec -it langchain_search_service-ollama-1 ollama pull nomic-embed-text

# Verify GPU usage
echo "🔍 Verifying GPU usage..."
docker exec -it langchain_search_service-ollama-1 nvidia-smi 2>/dev/null || echo "⚠️  GPU not detected"

# Test GPU inference
echo "🧪 Testing GPU inference..."
docker exec -it langchain_search_service-ollama-1 ollama run mistral "Test GPU inference" 2>/dev/null || echo "⚠️  Could not test GPU inference"

echo "✅ Comprehensive GPU deployment complete!"
echo ""
echo "🌐 Service URLs:"
echo "- Web Portal: http://localhost:5000"
echo "- API: http://localhost:5001"
echo "- LangChain: http://localhost:8000"
echo "- Qdrant: http://localhost:6333"
echo "- Ollama: http://localhost:11434"
echo ""
echo "🔍 To monitor GPU usage:"
echo "   docker exec -it langchain_search_service-ollama-1 nvidia-smi"
echo ""
echo "📊 To check if GPU is being used during AI search:"
echo "   docker exec -it langchain_search_service-ollama-1 nvidia-smi -l 1" 