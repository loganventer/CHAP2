#!/usr/bin/env python3
"""
Vectorize chorus data and populate Qdrant database
"""

import json
import os
import logging
from pathlib import Path
from qdrant_client import QdrantClient
from langchain_ollama import OllamaEmbeddings
from langchain.schema import Document

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

def load_chorus_data(data_dir):
    """Load all chorus JSON files from the data directory"""
    chorus_data = []
    data_path = Path(data_dir)
    
    if not data_path.exists():
        logger.error(f"Data directory {data_dir} does not exist")
        return []
    
    json_files = list(data_path.glob("*.json"))
    logger.info(f"Found {len(json_files)} JSON files")
    
    for json_file in json_files:
        try:
            with open(json_file, 'r', encoding='utf-8') as f:
                data = json.load(f)
                chorus_data.append({
                    'id': json_file.stem,  # Use filename as ID
                    'data': data
                })
        except Exception as e:
            logger.error(f"Error loading {json_file}: {e}")
    
    logger.info(f"Successfully loaded {len(chorus_data)} chorus records")
    return chorus_data

def create_documents(chorus_data):
    """Convert chorus data to LangChain documents"""
    documents = []
    
    for chorus in chorus_data:
        try:
            data = chorus['data']
            
            # Create text content for vectorization
            text_parts = []
            
            # Add title if available
            if 'title' in data:
                text_parts.append(f"Title: {data['title']}")
            
            # Add lyrics if available
            if 'lyrics' in data:
                text_parts.append(f"Lyrics: {data['lyrics']}")
            
            # Add composer if available
            if 'composer' in data:
                text_parts.append(f"Composer: {data['composer']}")
            
            # Add key if available
            if 'key' in data:
                text_parts.append(f"Key: {data['key']}")
            
            # Add time signature if available
            if 'timeSignature' in data:
                text_parts.append(f"Time Signature: {data['timeSignature']}")
            
            # Add chorus type if available
            if 'chorusType' in data:
                text_parts.append(f"Type: {data['chorusType']}")
            
            # Combine all text
            text = " ".join(text_parts)
            
            # Create metadata
            metadata = {
                'id': chorus['id'],
                'title': data.get('title', ''),
                'composer': data.get('composer', ''),
                'key': data.get('key', ''),
                'timeSignature': data.get('timeSignature', ''),
                'chorusType': data.get('chorusType', ''),
                'source': 'json_file'
            }
            
            # Create LangChain document
            doc = Document(
                page_content=text,
                metadata=metadata
            )
            
            documents.append(doc)
            
        except Exception as e:
            logger.error(f"Error processing chorus {chorus['id']}: {e}")
    
    logger.info(f"Created {len(documents)} documents for vectorization")
    return documents

def vectorize_and_store(documents, qdrant_url="http://qdrant:6333"):
    """Vectorize documents and store in Qdrant"""
    try:
        # Initialize Qdrant client
        logger.info(f"Connecting to Qdrant at {qdrant_url}")
        client = QdrantClient(qdrant_url)
        
        # Test connection
        collections = client.get_collections()
        logger.info(f"Connected to Qdrant. Found {len(collections.collections)} collections")
        
        # Initialize embeddings
        logger.info("Initializing Ollama embeddings...")
        embeddings = OllamaEmbeddings(
            model="nomic-embed-text",
            base_url="http://host.docker.internal:11434"
        )
        
        # Test embeddings
        test_embedding = embeddings.embed_query("test")
        logger.info(f"Embeddings initialized. Vector size: {len(test_embedding)}")
        
        # Check if collection exists
        collection_name = "chorus-vectors"
        try:
            client.get_collection(collection_name)
            logger.info(f"Collection '{collection_name}' already exists")
        except Exception:
            logger.info(f"Creating collection '{collection_name}'...")
            client.create_collection(
                collection_name=collection_name,
                vectors_config={
                    "size": 768,  # nomic-embed-text embedding size
                    "distance": "Cosine"
                }
            )
            logger.info(f"Collection '{collection_name}' created successfully")
        
        # Vectorize and store documents
        logger.info("Starting vectorization and storage...")
        
        batch_size = 10
        total_docs = len(documents)
        
        for i in range(0, total_docs, batch_size):
            batch = documents[i:i + batch_size]
            logger.info(f"Processing batch {i//batch_size + 1}/{(total_docs + batch_size - 1)//batch_size} ({len(batch)} documents)")
            
            # Get embeddings for batch
            texts = [doc.page_content for doc in batch]
            embeddings_list = embeddings.embed_documents(texts)
            
            # Prepare points for Qdrant
            points = []
            for j, (doc, embedding) in enumerate(zip(batch, embeddings_list)):
                point = {
                    "id": f"{doc.metadata['id']}_{j}",
                    "vector": embedding,
                    "payload": {
                        "text": doc.page_content,
                        "metadata": doc.metadata
                    }
                }
                points.append(point)
            
            # Upload to Qdrant
            client.upsert(
                collection_name=collection_name,
                points=points
            )
            
            logger.info(f"Uploaded batch {i//batch_size + 1} to Qdrant")
        
        # Verify upload
        collection_info = client.get_collection(collection_name)
        vector_count = collection_info.vectors_count
        logger.info(f"Vectorization complete! Total vectors in collection: {vector_count}")
        
        return True
        
    except Exception as e:
        logger.error(f"Error during vectorization: {e}")
        return False

def main():
    """Main function"""
    logger.info("Starting chorus data vectorization...")
    
    # Load chorus data
    data_dir = "/app/data"
    chorus_data = load_chorus_data(data_dir)
    
    if not chorus_data:
        logger.error("No chorus data found. Exiting.")
        return False
    
    # Create documents
    documents = create_documents(chorus_data)
    
    if not documents:
        logger.error("No documents created. Exiting.")
        return False
    
    # Vectorize and store
    success = vectorize_and_store(documents)
    
    if success:
        logger.info("Vectorization completed successfully!")
        return True
    else:
        logger.error("Vectorization failed!")
        return False

if __name__ == "__main__":
    main() 