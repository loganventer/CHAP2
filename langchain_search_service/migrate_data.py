#!/usr/bin/env python3
"""
Data migration script to transfer chorus data from .NET application to LangChain service.
This script reads the existing JSON files and uploads them to the LangChain vector store.
"""

import json
import os
import asyncio
import aiohttp
from typing import List, Dict, Any
import logging

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class DataMigrator:
    def __init__(self, data_dir: str, langchain_service_url: str = "http://localhost:8000"):
        self.data_dir = data_dir
        self.langchain_service_url = langchain_service_url
        
    def load_chorus_data(self) -> List[Dict[str, Any]]:
        """Load chorus data from JSON files in the data directory"""
        documents = []
        
        if not os.path.exists(self.data_dir):
            logger.error(f"Data directory does not exist: {self.data_dir}")
            return documents
            
        # Look for chorus data in various possible locations
        possible_paths = [
            os.path.join(self.data_dir, "chorus"),
            os.path.join(self.data_dir, "data", "chorus"),
            self.data_dir
        ]
        
        chorus_dir = None
        for path in possible_paths:
            if os.path.exists(path):
                chorus_dir = path
                break
                
        if not chorus_dir:
            logger.error(f"Could not find chorus data directory in: {possible_paths}")
            return documents
            
        logger.info(f"Loading chorus data from: {chorus_dir}")
        
        # Load all JSON files
        for filename in os.listdir(chorus_dir):
            if filename.endswith('.json'):
                file_path = os.path.join(chorus_dir, filename)
                try:
                    with open(file_path, 'r', encoding='utf-8') as f:
                        chorus_data = json.load(f)
                        
                    # Extract chorus ID from filename (remove .json extension)
                    chorus_id = filename.replace('.json', '')
                    
                    # Create document for LangChain
                    document = {
                        "id": chorus_id,
                        "text": chorus_data.get("chorusText", ""),
                        "name": chorus_data.get("name", ""),
                        "key": chorus_data.get("key", 0),
                        "type": chorus_data.get("type", 0),
                        "timeSignature": chorus_data.get("timeSignature", 0),
                        "metadata": {
                            "createdAt": chorus_data.get("createdAt"),
                            "updatedAt": chorus_data.get("updatedAt"),
                            "source": "migration"
                        }
                    }
                    
                    documents.append(document)
                    logger.debug(f"Loaded chorus: {chorus_id} - {document['name']}")
                    
                except Exception as e:
                    logger.error(f"Error loading {filename}: {str(e)}")
                    continue
                    
        logger.info(f"Loaded {len(documents)} chorus documents")
        return documents
        
    async def upload_to_langchain(self, documents: List[Dict[str, Any]]) -> bool:
        """Upload documents to LangChain service"""
        if not documents:
            logger.warning("No documents to upload")
            return True
            
        try:
            async with aiohttp.ClientSession() as session:
                url = f"{self.langchain_service_url}/add_documents"
                
                # Upload in batches to avoid memory issues
                batch_size = 100
                for i in range(0, len(documents), batch_size):
                    batch = documents[i:i + batch_size]
                    
                    logger.info(f"Uploading batch {i//batch_size + 1}/{(len(documents) + batch_size - 1)//batch_size} ({len(batch)} documents)")
                    
                    async with session.post(url, json=batch) as response:
                        if response.status == 200:
                            result = await response.json()
                            logger.info(f"Batch uploaded successfully: {result.get('message', '')}")
                        else:
                            error_text = await response.text()
                            logger.error(f"Failed to upload batch: {response.status} - {error_text}")
                            return False
                            
            logger.info("All documents uploaded successfully")
            return True
            
        except Exception as e:
            logger.error(f"Error uploading documents: {str(e)}")
            return False
            
    async def verify_upload(self) -> bool:
        """Verify that documents were uploaded correctly"""
        try:
            async with aiohttp.ClientSession() as session:
                # Test search to verify data is available
                url = f"{self.langchain_service_url}/search"
                test_query = {"query": "Jesus", "k": 1}
                
                async with session.post(url, json=test_query) as response:
                    if response.status == 200:
                        results = await response.json()
                        logger.info(f"Verification successful - found {len(results)} results for test query")
                        return True
                    else:
                        error_text = await response.text()
                        logger.error(f"Verification failed: {response.status} - {error_text}")
                        return False
                        
        except Exception as e:
            logger.error(f"Error during verification: {str(e)}")
            return False

async def main():
    """Main migration function"""
    # Configuration
    data_dir = "./data"  # Use local data directory
    langchain_service_url = "http://localhost:8000"
    
    # Create migrator
    migrator = DataMigrator(data_dir, langchain_service_url)
    
    # Step 1: Load data
    logger.info("Step 1: Loading chorus data...")
    documents = migrator.load_chorus_data()
    
    if not documents:
        logger.error("No documents found. Migration failed.")
        return
        
    # Step 2: Upload to LangChain service
    logger.info("Step 2: Uploading to LangChain service...")
    success = await migrator.upload_to_langchain(documents)
    
    if not success:
        logger.error("Upload failed. Migration failed.")
        return
        
    # Step 3: Verify upload
    logger.info("Step 3: Verifying upload...")
    verification_success = await migrator.verify_upload()
    
    if verification_success:
        logger.info("Migration completed successfully!")
    else:
        logger.error("Verification failed. Please check the LangChain service.")

if __name__ == "__main__":
    asyncio.run(main()) 