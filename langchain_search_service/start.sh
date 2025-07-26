#!/bin/bash

# LangChain Search Service Startup Script
# This script sets up and starts the LangChain search service with all dependencies

set -e

echo "🚀 Starting LangChain Search Service Setup..."

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker first."
    exit 1
fi

# Check if Docker Compose is installed
if ! command -v docker-compose &> /dev/null; then
    echo "❌ Docker Compose is not installed. Please install Docker Compose first."
    exit 1
fi

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker first."
    exit 1
fi

echo "✅ Prerequisites check passed"

# Create data directory if it doesn't exist
mkdir -p data

# Copy chorus data if it exists in the parent directory
if [ -d "../../data" ]; then
    echo "📁 Copying chorus data..."
    cp -r ../../data/* ./data/ 2>/dev/null || true
    echo "✅ Data copied"
else
    echo "⚠️  No data directory found at ../../data"
    echo "   You can add your chorus data to the ./data/ directory manually"
fi

# Start Qdrant and Ollama
echo "🐳 Starting Qdrant and Ollama..."
docker-compose up qdrant ollama -d

# Wait for services to be ready
echo "⏳ Waiting for services to be ready..."
sleep 10

# Check if Ollama models are available
echo "🤖 Checking Ollama models..."

# Check for nomic-embed-text model
if ! docker exec langchain_search_service-ollama-1 ollama list | grep -q "nomic-embed-text"; then
    echo "📥 Pulling nomic-embed-text model..."
    docker exec langchain_search_service-ollama-1 ollama pull nomic-embed-text
else
    echo "✅ nomic-embed-text model found"
fi

# Check for mistral model
if ! docker exec langchain_search_service-ollama-1 ollama list | grep -q "mistral"; then
    echo "📥 Pulling mistral model..."
    docker exec langchain_search_service-ollama-1 ollama pull mistral
else
    echo "✅ mistral model found"
fi

# Build and start the LangChain service
echo "🔨 Building LangChain service..."
docker-compose build langchain-service

echo "🚀 Starting LangChain service..."
docker-compose up langchain-service -d

# Wait for the service to be ready
echo "⏳ Waiting for LangChain service to be ready..."
sleep 15

# Test the service
echo "🧪 Testing the service..."
if curl -f http://localhost:8000/health > /dev/null 2>&1; then
    echo "✅ LangChain service is running!"
    echo ""
    echo "🎉 Setup complete! Your services are running:"
    echo "   - Qdrant: http://localhost:6333"
    echo "   - Ollama: http://localhost:11434"
    echo "   - LangChain Service: http://localhost:8000"
    echo ""
    echo "📊 To view logs:"
    echo "   docker-compose logs -f langchain-service"
    echo ""
    echo "🔄 To restart services:"
    echo "   docker-compose restart"
    echo ""
    echo "🛑 To stop all services:"
    echo "   docker-compose down"
    echo ""
    echo "📝 Next steps:"
    echo "   1. Run the data migration: python migrate_data.py"
    echo "   2. Test the search API: curl -X POST http://localhost:8000/search -H 'Content-Type: application/json' -d '{\"query\": \"Jesus\", \"k\": 3}'"
    echo "   3. Update your .NET app configuration to use the LangChain service"
else
    echo "❌ LangChain service is not responding"
    echo "Check logs: docker-compose logs langchain-service"
    exit 1
fi 