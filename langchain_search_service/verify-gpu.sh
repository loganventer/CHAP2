#!/bin/bash

echo "🔍 Verifying GPU usage and configuration..."

# Check if containers are running
echo "📦 Checking container status..."
docker ps --filter "name=langchain_search_service" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

# Check GPU usage in Ollama container
echo ""
echo "🎮 Checking GPU usage in Ollama container..."
docker exec -it langchain_search_service-ollama-1 nvidia-smi 2>/dev/null || echo "⚠️  GPU not detected or nvidia-smi not available"

# Check Ollama models and their GPU usage
echo ""
echo "🤖 Checking Ollama models..."
docker exec -it langchain_search_service-ollama-1 ollama list

# Check if models are using GPU
echo ""
echo "🔧 Checking model GPU usage..."
docker exec -it langchain_search_service-ollama-1 ollama show mistral | grep -i gpu || echo "No GPU info found for mistral model"

# Test GPU inference
echo ""
echo "🧪 Testing GPU inference..."
docker exec -it langchain_search_service-ollama-1 ollama run mistral "Hello, this is a GPU test. Please respond with 'GPU test successful' if you can see this." 2>/dev/null || echo "⚠️  Could not test GPU inference"

# Check LangChain service logs for GPU usage
echo ""
echo "📋 Checking LangChain service logs for GPU usage..."
docker logs langchain_search_service-langchain-service-1 --tail 20 | grep -i gpu || echo "No GPU-related logs found"

# Check Ollama container logs
echo ""
echo "📋 Checking Ollama container logs..."
docker logs langchain_search_service-ollama-1 --tail 10 | grep -i gpu || echo "No GPU-related logs found"

echo ""
echo "✅ GPU verification complete!"
echo ""
echo "💡 If GPU is not being used:"
echo "   1. Restart the containers: docker-compose -f docker-compose.gpu.yml restart"
echo "   2. Pull models again: docker exec -it langchain_search_service-ollama-1 ollama pull mistral"
echo "   3. Check GPU drivers: nvidia-smi"
echo "   4. Verify Docker GPU runtime: docker run --rm --gpus all nvidia/cuda:11.0-base nvidia-smi" 