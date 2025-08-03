from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import List, Optional, Dict, Any
import asyncio
import json
import logging
import os
from contextlib import asynccontextmanager
from sse_starlette.sse import EventSourceResponse

from langchain_community.vectorstores import Qdrant
from langchain_ollama import OllamaEmbeddings
from langchain_ollama import OllamaLLM as Ollama
from qdrant_client import QdrantClient
from langchain.schema import Document
from langchain.chains import RetrievalQA
from langchain.prompts import PromptTemplate

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Global variables for services
vector_store = None
llm = None
embeddings = None
qa_chain = None
qdrant_client = None

# Simple in-memory cache (for demo; use Redis for production)
search_cache = {}
cache_timestamp = 0  # Add timestamp for cache invalidation

class SearchRequest(BaseModel):
    query: str
    k: int = 5

class IntelligentSearchRequest(BaseModel):
    query: str
    k: int = 5
    include_analysis: bool = True

class SearchResult(BaseModel):
    id: str  # GUID
    name: str  # Chorus name
    chorusText: str  # Chorus lyrics
    key: int  # Musical key
    type: int  # Chorus type
    timeSignature: int  # Time signature
    createdAt: Optional[str] = None  # Creation date
    updatedAt: Optional[str] = None  # Update date
    metadata: Optional[Dict[str, Any]] = None  # Additional metadata
    domainEvents: Optional[List[Any]] = None  # Domain events
    score: float  # Search relevance score
    explanation: Optional[str] = None  # AI explanation

class IntelligentSearchResult(BaseModel):
    search_results: List[SearchResult]
    ai_analysis: Optional[str] = None
    query_understanding: Optional[str] = None

@asynccontextmanager
async def lifespan(app: FastAPI):
    global vector_store, llm, embeddings, qa_chain, qdrant_client
    logger.info("Initializing LangChain services...")

    # Get Ollama URL from environment variable
    ollama_url = os.getenv("OLLAMA_URL", "http://localhost:11434")
    
    # Initialize Ollama embeddings
    embeddings = OllamaEmbeddings(
        model="nomic-embed-text",  # Use original model which generates 768-dimensional embeddings
        base_url=ollama_url
    )
    # Initialize Ollama LLM with GPU acceleration and optimized settings
    logger.info(f"Initializing Ollama LLM with URL: {ollama_url}")
    try:
        llm = Ollama(
            model="mistral",
            base_url=ollama_url,
            timeout=600,  # 10 minutes timeout
            temperature=0.7,
            num_gpu=1,  # Use GPU acceleration
            num_thread=4,  # Limit CPU threads to reduce CPU usage
            num_ctx=2048,  # Limit context window for faster processing
            repeat_penalty=1.1,  # Reduce repetition for faster generation
            top_k=40,  # Limit top-k for faster generation
            top_p=0.9  # Use nucleus sampling for faster generation
        )
        # Test the connection
        logger.info("Testing Ollama connection...")
        test_response = llm.invoke("Hello")
        logger.info(f"Ollama connection test successful: {test_response[:50]}...")
    except Exception as e:
        logger.error(f"Failed to initialize Ollama LLM: {type(e).__name__}: {e}")
        logger.error(f"Ollama URL being used: {ollama_url}")
        logger.error("Please ensure Ollama is running on the host machine")
        raise
    # Initialize Qdrant client with retry
    qdrant_url = os.getenv("QDRANT_URL", "http://localhost:6333")
    logger.info(f"Connecting to Qdrant at: {qdrant_url}")
    
    # Retry connection to Qdrant
    max_retries = 5
    client = None
    logger.info("Starting Qdrant connection attempts...")
    for attempt in range(max_retries):
        try:
            logger.info(f"Attempting to connect to Qdrant (attempt {attempt + 1}/{max_retries})...")
            logger.info(f"Creating QdrantClient with URL: {qdrant_url}")
            client = QdrantClient(qdrant_url)
            logger.info("QdrantClient created successfully, testing connection...")
            # Test the connection
            collections = client.get_collections()
            logger.info(f"Qdrant client initialized successfully. Found {len(collections.collections)} collections.")
            break
        except Exception as e:
            logger.error(f"Exception during Qdrant connection (attempt {attempt + 1}/{max_retries}): {type(e).__name__}: {e}")
            if attempt < max_retries - 1:
                logger.warning(f"Failed to connect to Qdrant (attempt {attempt + 1}/{max_retries}): {e}")
                import time
                time.sleep(2)
            else:
                logger.error(f"Failed to connect to Qdrant after {max_retries} attempts: {e}")
                raise
    # Ensure collection exists
    try:
        logger.info("Checking if collection 'chorus-vectors' exists...")
        client.get_collection("chorus-vectors")
        logger.info("Collection 'chorus-vectors' already exists")
    except Exception as e:
        logger.info(f"Collection 'chorus-vectors' does not exist, creating it... Error: {e}")
        client.create_collection(
            collection_name="chorus-vectors",
            vectors_config={
                "size": 768,  # nomic-embed-text embedding size
                "distance": "Cosine"
            }
        )
        logger.info("Collection 'chorus-vectors' created successfully")
    
    qdrant_client = client
    # Initialize vector store
    vector_store = Qdrant(
        client=client,
        collection_name="chorus-vectors",
        embeddings=embeddings,
    )
    # System prompt template for RAG
    system_prompt = PromptTemplate(
        input_variables=["context", "question"],
        template="""
You are a helpful assistant for religious chorus search. Your task is to analyze the provided choruses and give meaningful insights about them.

Context (choruses found):
{context}

Question: {question}

Please provide a thoughtful analysis that includes:
1. A summary of the choruses that match the query
2. Key themes or messages found in these choruses
3. How these choruses relate to the user's search query
4. Any notable patterns or insights about the music or lyrics

Keep your analysis concise but insightful. Focus on providing value to someone searching for religious choruses.
"""
    )
    # Build the RAG chain
    qa_chain = RetrievalQA.from_chain_type(
        llm=llm,
        retriever=vector_store.as_retriever(search_kwargs={"k": 12}),  # Increased from 8 to 12 for better context
        chain_type_kwargs={"prompt": system_prompt}
    )
    logger.info("LangChain services initialized successfully")
    yield
    logger.info("Shutting down LangChain services...")

app = FastAPI(
    title="LangChain Search Service",
    description="Search service using LangChain with Ollama and Qdrant (RAG, cache, chaining)",
    version="1.1.0",
    lifespan=lifespan
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.get("/health")
async def health_check():
    return {"status": "healthy", "services": {
        "vector_store": vector_store is not None,
        "llm": llm is not None,
        "embeddings": embeddings is not None,
        "qa_chain": qa_chain is not None
    }}

@app.post("/search", response_model=List[SearchResult])
async def search(request: SearchRequest):
    cache_key = f"search|{request.query.lower()}|{request.k}"
    if cache_key in search_cache:
        logger.info(f"Cache hit for query: {request.query}")
        return search_cache[cache_key]
    logger.info(f"Cache miss for query: {request.query}")
    # Retrieve from Qdrant
    docs = vector_store.similarity_search_with_score(request.query, k=request.k)
    results = []
    for i, (doc, score) in enumerate(docs):
        try:
            chorus_id = doc.metadata.get("id", "")
            # Handle empty IDs
            if not chorus_id:
                chorus_id = f"unknown_{i}"
                logger.warning(f"Found document with empty ID in regular search, using generated ID: {chorus_id}")
            
            result = SearchResult(
                id=doc.metadata.get('Id', chorus_id),
                name=doc.metadata.get('Name', ''),
                chorusText=doc.metadata.get('ChorusText', ''),
                key=doc.metadata.get('Key', 0),
                type=doc.metadata.get('Type', 0),
                timeSignature=doc.metadata.get('TimeSignature', 0),
                createdAt=doc.metadata.get('CreatedAt', ''),
                updatedAt=doc.metadata.get('UpdatedAt', ''),
                metadata=doc.metadata.get('Metadata', {}),
                domainEvents=doc.metadata.get('DomainEvents', []),
                score=float(score),
                explanation=None
            )
            results.append(result)
            logger.debug(f"Added search result with ID: {chorus_id}")
        except Exception as e:
            logger.error(f"Error creating search result for document {i}: {e}")
            # Create a fallback result
            fallback_id = f"error_{i}"
            result = SearchResult(
                id=fallback_id,
                text=doc.page_content if hasattr(doc, 'page_content') else "Error loading content",
                score=float(score) if score is not None else 0.0,
                metadata=doc.metadata if hasattr(doc, 'metadata') else {}
            )
            results.append(result)
            logger.warning(f"Created fallback search result with ID: {fallback_id}")
    search_cache[cache_key] = results
    return results

@app.post("/search_intelligent", response_model=IntelligentSearchResult)
async def search_intelligent(request: IntelligentSearchRequest):
    # Use timestamp-based cache key to prevent stale cache
    cache_key = f"rag|{request.query.lower()}|{request.k}|{cache_timestamp}"
    if cache_key in search_cache:
        logger.info(f"Cache hit for RAG query: {request.query}")
        return search_cache[cache_key]
    logger.info(f"Cache miss for RAG query: {request.query}")
    
    # Get more documents for better context (k=12 instead of 8)
    docs = vector_store.similarity_search_with_score(request.query, k=12)
    
    # Deduplicate results based on chorus ID before analysis with better error handling
    unique_docs = []
    seen_ids = set()
    for i, (doc, score) in enumerate(docs):
        try:
            chorus_id = doc.metadata.get('id', '')
            # Handle empty or None IDs
            if not chorus_id:
                chorus_id = f"unknown_{i}"
                logger.warning(f"Found document with empty ID in non-streaming search, using generated ID: {chorus_id}")
            
            if chorus_id not in seen_ids:
                unique_docs.append((doc, score))
                seen_ids.add(chorus_id)
                logger.debug(f"Added unique document with ID: {chorus_id}")
            else:
                logger.debug(f"Skipping duplicate document with ID: {chorus_id}")
        except Exception as e:
            logger.error(f"Error processing document during non-streaming deduplication: {e}")
            # Add with a generated ID to avoid crashes
            generated_id = f"error_{i}"
            unique_docs.append((doc, score))
            seen_ids.add(generated_id)
            logger.warning(f"Added document with generated ID due to error: {generated_id}")
    
    logger.info(f"Found {len(docs)} total results, {len(unique_docs)} unique choruses")
    
    # Create a more detailed context for analysis using unique results
    context_parts = []
    for i, (doc, score) in enumerate(unique_docs[:8]):  # Use top 8 unique results for analysis
        context_parts.append(f"Chorus {i+1} (Score: {score:.3f}):\nTitle: {doc.metadata.get('name', 'Unknown')}\nText: {doc.page_content}\n")
    
    context = "\n".join(context_parts)
    
    # Create an enhanced prompt for better analysis
    analysis_prompt = f"""
You are an expert musicologist and religious scholar helping someone find meaningful choruses. The user searched for: "{request.query}"

Here are the most relevant choruses found:

{context}

IMPORTANT: Provide a detailed, insightful analysis that includes ALL of the following sections:

1. **Summary**: What specific choruses were found and why they match this query? Mention specific titles and key themes.

2. **Musical & Spiritual Insights**: What musical elements (key, tempo, style) and spiritual themes are prominent in these choruses?

3. **Relevance to Query**: How do these choruses specifically address what the user is looking for? Be specific about lyrics, themes, or musical characteristics.

4. **Practical Value**: What makes these choruses particularly suitable for someone searching with this query? Consider worship context, emotional impact, or theological depth.

5. **Notable Patterns**: Are there recurring musical patterns, lyrical themes, or spiritual messages across these choruses?

DO NOT give generic responses like "This chorus was selected based on relevance to your search query." Instead, provide concrete, actionable insights that help the user understand why these choruses are relevant and valuable for their search. Be specific about musical details, lyrical content, and spiritual significance.

Your response should be comprehensive and detailed, covering all the sections above.
"""
    
    # Use LLM directly with enhanced prompt for better analysis
    answer = llm.invoke(analysis_prompt)
    
    # Return the deduplicated search results with better error handling
    search_results = []
    for i, (doc, score) in enumerate(unique_docs[:request.k]):  # Limit to requested k
        try:
            chorus_id = doc.metadata.get("id", "")
            # Handle empty IDs
            if not chorus_id:
                chorus_id = f"unknown_{i}"
                logger.warning(f"Found document with empty ID in non-streaming search results, using generated ID: {chorus_id}")
            
            search_results.append(SearchResult(
                id=chorus_id,
                text=doc.page_content,
                score=float(score),
                metadata=doc.metadata
            ))
            logger.debug(f"Added search result with ID: {chorus_id}")
        except Exception as e:
            logger.error(f"Error creating search result for document {i}: {e}")
            # Create a fallback result
            fallback_id = f"error_{i}"
            search_results.append(SearchResult(
                id=fallback_id,
                text=doc.page_content if hasattr(doc, 'page_content') else "Error loading content",
                score=float(score) if score is not None else 0.0,
                metadata=doc.metadata if hasattr(doc, 'metadata') else {}
            ))
            logger.warning(f"Created fallback search result with ID: {fallback_id}")
    
    result = IntelligentSearchResult(
        search_results=search_results,
        ai_analysis=answer,
        query_understanding=request.query
    )
    search_cache[cache_key] = result
    return result

@app.post("/search_intelligent_stream")
async def search_intelligent_stream(request: IntelligentSearchRequest):
    async def generate_stream():
        try:
            logger.info(f"Starting streaming intelligent search for query: {request.query}")
            
            # Step 1: Generate search terms from user's query using Ollama
            logger.info("Step 1: Generating search terms from user query...")
            search_terms_prompt = f"""Query: "{request.query}"

Generate 3-5 search terms for finding relevant choruses. Return only comma-separated terms.
Example: "love, Jesus, worship, praise, salvation"

Terms:"""
            
            try:
                logger.info(f"Sending prompt to Ollama: {search_terms_prompt[:100]}...")
                search_terms_response = llm.invoke(search_terms_prompt)
                search_terms = search_terms_response.strip()
                logger.info(f"Generated search terms: {search_terms}")
            except Exception as e:
                logger.error(f"Error generating search terms: {type(e).__name__}: {e}")
                logger.error(f"Ollama URL: {os.getenv('OLLAMA_URL', 'http://localhost:11434')}")
                error_message = f"Failed to generate search terms: {str(e)}. Please ensure Ollama is running and accessible."
                yield f"data: {json.dumps({'type': 'error', 'error': error_message})}\n\n"
                return
            
            # Clean up the response to ensure it's just the search terms
            if search_terms.startswith('"') and search_terms.endswith('"'):
                search_terms = search_terms[1:-1]
            
            logger.info(f"Generated search terms: {search_terms}")
            
            # Step 2: Send query understanding (the generated search terms)
            logger.info("Step 2: Sending query understanding")
            yield f"data: {json.dumps({'type': 'queryUnderstanding', 'queryUnderstanding': search_terms})}\n\n"
            
            # Step 3: Use the generated search terms to search the vector database
            logger.info("Step 3: Performing search with generated terms...")
            try:
                docs = vector_store.similarity_search_with_score(search_terms, k=request.k)
                logger.info(f"Vector search returned {len(docs)} documents")
            except Exception as e:
                logger.error(f"Error during vector search: {e}")
                logger.error(f"Error type: {type(e).__name__}")
                logger.error(f"Error details: {str(e)}")
                # Check if it's a Qdrant-specific error
                if "duplicate" in str(e).lower() or "key" in str(e).lower():
                    yield f"data: {json.dumps({'type': 'error', 'error': 'Database contains duplicate entries. Please contact support.'})}\n\n"
                else:
                    yield f"data: {json.dumps({'type': 'error', 'error': 'Vector search failed. Please try again.'})}\n\n"
                return
            
            # Deduplicate results with better error handling
            unique_docs = []
            seen_ids = set()
            logger.info(f"Processing {len(docs)} documents for deduplication")
            
            for i, (doc, score) in enumerate(docs):
                try:
                    chorus_id = doc.metadata.get('id', '')
                    logger.debug(f"Processing document {i+1}/{len(docs)}, ID: {chorus_id}")
                    
                    # Handle empty or None IDs
                    if not chorus_id:
                        chorus_id = f"unknown_{i}"
                        logger.warning(f"Found document with empty ID, using generated ID: {chorus_id}")
                    
                    if chorus_id not in seen_ids:
                        unique_docs.append((doc, score))
                        seen_ids.add(chorus_id)
                        logger.debug(f"Added unique document with ID: {chorus_id}")
                    else:
                        logger.debug(f"Skipping duplicate document with ID: {chorus_id}")
                except Exception as e:
                    logger.error(f"Error processing document {i+1} during deduplication: {e}")
                    logger.error(f"Document metadata: {doc.metadata if hasattr(doc, 'metadata') else 'No metadata'}")
                    # Add with a generated ID to avoid crashes
                    generated_id = f"error_{i}"
                    unique_docs.append((doc, score))
                    seen_ids.add(generated_id)
                    logger.warning(f"Added document with generated ID due to error: {generated_id}")
            
            logger.info(f"Deduplication complete: {len(unique_docs)} unique documents from {len(docs)} total")
            
            search_results = []
            for i, (doc, score) in enumerate(unique_docs):
                try:
                    chorus_id = doc.metadata.get("id", "")
                    # Handle empty IDs
                    if not chorus_id:
                        chorus_id = f"unknown_{i}"
                        logger.warning(f"Found document with empty ID in search results, using generated ID: {chorus_id}")
                    
                    search_results.append({
                        "id": doc.metadata.get('Id', chorus_id),
                        "name": doc.metadata.get('Name', ''),
                        "chorusText": doc.metadata.get('ChorusText', ''),
                        "key": doc.metadata.get('Key', 0),
                        "type": doc.metadata.get('Type', 0),
                        "timeSignature": doc.metadata.get('TimeSignature', 0),
                        "createdAt": doc.metadata.get('CreatedAt', ''),
                        "updatedAt": doc.metadata.get('UpdatedAt', ''),
                        "metadata": doc.metadata.get('Metadata', {}),
                        "domainEvents": doc.metadata.get('DomainEvents', []),
                        "score": float(score)
                    })
                    logger.debug(f"Added search result with ID: {chorus_id}")
                except Exception as e:
                    logger.error(f"Error creating search result for document {i}: {e}")
                    # Create a fallback result
                    fallback_id = f"error_{i}"
                    search_results.append({
                        "id": fallback_id,
                        "text": doc.page_content if hasattr(doc, 'page_content') else "Error loading content",
                        "score": float(score) if score is not None else 0.0,
                        "metadata": doc.metadata if hasattr(doc, 'metadata') else {}
                    })
                    logger.warning(f"Created fallback search result with ID: {fallback_id}")
            
            logger.info(f"Step 3: Found {len(search_results)} unique results")
            
            # Send individual search results as they're processed
            for i, result in enumerate(search_results):
                logger.info(f"Sending individual search result {i+1}/{len(search_results)}: {result['name']}")
                yield f"data: {json.dumps({'type': 'searchResult', 'index': i, 'searchResult': result})}\n\n"
            
            # Also send the complete results array for compatibility
            yield f"data: {json.dumps({'type': 'searchResults', 'searchResults': search_results})}\n\n"
            
            # Step 4: Skipping individual reasons generation for performance
            logger.info("Step 4: Skipping individual reasons generation for performance")
            
            # Step 5: Skipping overall analysis generation for performance
            logger.info("Step 5: Skipping overall analysis generation for performance")
            
            # Step 6: Send completion
            yield f"data: {json.dumps({'type': 'complete', 'status': 'completed'})}\n\n"
            
        except Exception as e:
            logger.error(f"Error in streaming search: {e}")
            yield f"data: {json.dumps({'type': 'error', 'error': str(e)})}\n\n"
    
    return EventSourceResponse(generate_stream())

@app.post("/clear_cache")
async def clear_cache():
    global search_cache, cache_timestamp
    search_cache.clear()
    cache_timestamp += 1
    logger.info("Cache cleared and timestamp incremented")
    return {"message": "Cache cleared successfully"}

@app.post("/add_documents")
async def add_documents(documents: List[Dict[str, Any]]):
    if not vector_store:
        raise HTTPException(status_code=503, detail="Vector store not initialized")
    # Debug: test Qdrant connection before proceeding
    try:
        logger.info(f"Testing Qdrant connection in /add_documents: {qdrant_client}")
        collections = qdrant_client.get_collections()
        logger.info(f"Qdrant connection OK in /add_documents. Found {len(collections.collections)} collections.")
    except Exception as e:
        logger.error(f"Qdrant connection failed in /add_documents: {e}")
        raise HTTPException(status_code=500, detail=f"Qdrant connection failed: {e}")
    docs = []
    for doc in documents:
        langchain_doc = Document(
            page_content=doc.get("text", ""),
            metadata={
                "id": doc.get("id", ""),
                "name": doc.get("name", ""),
                "key": doc.get("key", ""),
                "type": doc.get("type", ""),
                "word_positions": doc.get("word_positions", {})
            }
        )
        docs.append(langchain_doc)
    try:
        vector_store.add_documents(docs)
    except Exception as e:
        logger.error(f"Error during add_documents: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail=f"add_documents failed: {e}")
    return {"message": f"Added {len(docs)} documents to vector store"}

@app.post("/test_qdrant")
async def test_qdrant():
    info = {}
    try:
        info['client_type'] = str(type(qdrant_client))
        info['client_url'] = getattr(qdrant_client, '_rest_uri', 'unknown')
        collections = qdrant_client.get_collections()
        info['collections'] = [c.name for c in collections.collections]
        # Try a minimal upsert
        import uuid
        test_id = str(uuid.uuid4())
        test_vector = [0.0] * 768
        upsert_result = qdrant_client.upsert(
            collection_name="choruses",
            points=[
                {
                    "id": test_id,
                    "vector": test_vector,
                    "payload": {"test": True}
                }
            ]
        )
        info['upsert_result'] = str(upsert_result)
        return {"success": True, "info": info}
    except Exception as e:
        import traceback
        info['error'] = str(e)
        info['traceback'] = traceback.format_exc()
        return {"success": False, "info": info}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000) 