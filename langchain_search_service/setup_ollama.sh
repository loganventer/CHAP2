#!/bin/bash

# Ollama Setup Script for LangChain Search Service
# This script sets up Ollama locally with persistent model storage

set -e

echo "🤖 Setting up Ollama for LangChain Search Service..."

# Check if Ollama is installed
if ! command -v ollama &> /dev/null; then
    echo "❌ Ollama is not installed. Please install Ollama first."
    echo "Visit: https://ollama.ai/download"
    exit 1
fi

# Create models directory if it doesn't exist
MODELS_DIR="$HOME/.ollama/models"
echo "📁 Using models directory: $MODELS_DIR"

# Check if models are already downloaded
echo "🔍 Checking for required models..."

# Check for nomic-embed-text model
if ! ollama list | grep -q "nomic-embed-text"; then
    echo "📥 Pulling nomic-embed-text model for embeddings..."
    ollama pull nomic-embed-text
else
    echo "✅ nomic-embed-text model found"
fi

# Check for mistral model
if ! ollama list | grep -q "mistral"; then
    echo "📥 Pulling mistral model for LLM operations..."
    ollama pull mistral
else
    echo "✅ mistral model found"
fi

echo ""
echo "🎉 Ollama setup complete!"
echo ""
echo "📊 Model information:"
ollama list
echo ""
echo "🚀 Next steps:"
echo "   1. Start the LangChain service: docker-compose up -d"
echo "   2. Run data migration: python migrate_data.py"
echo "   3. Test the service: curl http://localhost:8000/health"
echo ""
echo "💡 Tips:"
echo "   - Models are stored in: $MODELS_DIR"
echo "   - Restarting containers won't re-download models"
echo "   - To update models: ollama pull <model-name>" 