#!/bin/bash

echo "=== CHAP2 RAG-Optimized Vector Database Rebuild ==="
echo "This script will rebuild the vector database with RAG-optimized embeddings"
echo ""

# Check if Qdrant is running
echo "Checking if Qdrant is running..."
if ! curl -s http://localhost:6333/collections > /dev/null 2>&1; then
    echo "❌ Qdrant is not running. Please start it with: docker-compose up -d"
    echo "   Then run this script again."
    exit 1
fi
echo "✅ Qdrant is running"

# Navigate to the vectorization console app directory
cd "$(dirname "$0")"

# Build the project
echo ""
echo "Building RAG-optimized vectorization console app..."
dotnet build --configuration Release

if [ $? -ne 0 ]; then
    echo "❌ Build failed. Please fix any compilation errors and try again."
    exit 1
fi
echo "✅ Build successful"

# Clear existing collection (optional - uncomment if you want to start fresh)
echo ""
echo "Clearing existing vector collection..."
curl -X DELETE http://localhost:6333/collections/chorus-vectors 2>/dev/null || true
echo "✅ Collection cleared (or didn't exist)"

# Run the vectorization process
echo ""
echo "Starting RAG-optimized vectorization process..."
echo "This may take a few minutes depending on the number of choruses..."

dotnet run --configuration Release --project . --no-build

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ RAG-optimized vectorization completed successfully!"
    echo ""
    echo "The vector database has been rebuilt with:"
    echo "  - Enhanced semantic weighting for religious/spiritual terms"
    echo "  - RAG-optimized features for better question answering"
    echo "  - Improved language detection (English/Afrikaans)"
    echo "  - Question word and context indicators"
    echo "  - Consistent embedding generation between console and web portal"
    echo ""
    echo "You can now use the web portal's RAG features with improved accuracy."
else
    echo ""
    echo "❌ Vectorization failed. Please check the logs above for errors."
    exit 1
fi 