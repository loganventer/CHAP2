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

class SearchRequest(BaseModel):
    query: str
    k: int = 5

class IntelligentSearchRequest(BaseModel):
    query: str
    k: int = 5
    include_analysis: bool = True

class SearchResult(BaseModel):
    id: str
    text: str
    score: float
    explanation: Optional[str] = None
    metadata: Dict[str, Any]

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
        model="nomic-embed-text",
        base_url=ollama_url
    )
    # Initialize Ollama LLM
    llm = Ollama(
        model="mistral",
        base_url=ollama_url
    )
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
        logger.info("Checking if collection 'choruses' exists...")
        client.get_collection("choruses")
        logger.info("Collection 'choruses' already exists")
    except Exception as e:
        logger.info(f"Collection 'choruses' does not exist, creating it... Error: {e}")
        client.create_collection(
            collection_name="choruses",
            vectors_config={
                "size": 768,  # nomic-embed-text embedding size
                "distance": "Cosine"
            }
        )
        logger.info("Collection 'choruses' created successfully")
    
    qdrant_client = client
    # Initialize vector store
    vector_store = Qdrant(
        client=client,
        collection_name="choruses",
        embeddings=embeddings,
    )
    # System prompt template for RAG
    system_prompt = PromptTemplate(
        input_variables=["context", "question"],
        template="""
You are a helpful assistant for religious chorus search. Use only the provided context to answer the user's question. If the answer is not in the context, say you don't know.

Context:
{context}

Question:
{question}

Answer (in the same language as the context):
"""
    )
    # Build the RAG chain
    qa_chain = RetrievalQA.from_chain_type(
        llm=llm,
        retriever=vector_store.as_retriever(search_kwargs={"k": 8}),
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
    for doc, score in docs:
        result = SearchResult(
            id=doc.metadata.get("id", ""),
            text=doc.page_content,
            score=float(score),
            metadata=doc.metadata
        )
        results.append(result)
    search_cache[cache_key] = results
    return results

@app.post("/search_intelligent", response_model=IntelligentSearchResult)
async def search_intelligent(request: IntelligentSearchRequest):
    cache_key = f"rag|{request.query.lower()}|{request.k}"
    if cache_key in search_cache:
        logger.info(f"Cache hit for RAG query: {request.query}")
        return search_cache[cache_key]
    logger.info(f"Cache miss for RAG query: {request.query}")
    # Use RAG chain (retrieval + LLM)
    answer = qa_chain.run(request.query)
    # Also return the top docs for transparency
    docs = vector_store.similarity_search_with_score(request.query, k=request.k)
    search_results = []
    for doc, score in docs:
        search_results.append(SearchResult(
            id=doc.metadata.get("id", ""),
            text=doc.page_content,
            score=float(score),
            metadata=doc.metadata
        ))
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
            # Step 1: Send query understanding
            yield f"data: {json.dumps({'type': 'queryUnderstanding', 'queryUnderstanding': request.query})}\n\n"
            # Step 2: Perform RAG search
            logger.info("Performing RAG search...")
            docs = vector_store.similarity_search_with_score(request.query, k=request.k)
            search_results = []
            for doc, score in docs:
                result = SearchResult(
                    id=doc.metadata.get("id", ""),
                    text=doc.page_content,
                    score=float(score),
                    metadata=doc.metadata
                )
                search_results.append(result)
            yield f"data: {json.dumps({'type': 'searchResults', 'searchResults': [r.dict() for r in search_results]})}\n\n"
            # Step 3: Generate AI analysis
            logger.info("Generating AI analysis...")
            answer = qa_chain.run(request.query)
            yield f"data: {json.dumps({'type': 'aiAnalysis', 'analysis': answer})}\n\n"
            # Step 4: Send completion
            yield f"data: {json.dumps({'type': 'complete', 'status': 'completed'})}\n\n"
        except Exception as e:
            logger.error(f"Error in streaming search: {e}")
            yield f"data: {json.dumps({'type': 'error', 'error': str(e)})}\n\n"
    return EventSourceResponse(generate_stream())

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