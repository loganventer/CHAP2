@echo off
echo ========================================
echo Finding Correct Host IP for Windows Docker Desktop
echo ========================================
echo.

echo Testing different host IP addresses for Ollama connection:
echo.

echo Testing 172.17.0.1 (default Docker bridge):
docker run --rm alpine sh -c "apk add --no-cache curl && curl -s http://172.17.0.1:11434/api/tags"
echo.

echo Testing 10.0.2.2 (Docker Desktop default):
docker run --rm alpine sh -c "apk add --no-cache curl && curl -s http://10.0.2.2:11434/api/tags"
echo.

echo Testing host.docker.internal:
docker run --rm alpine sh -c "apk add --no-cache curl && curl -s http://host.docker.internal:11434/api/tags"
echo.

echo Testing 192.168.65.1 (Docker Desktop for Windows):
docker run --rm alpine sh -c "apk add --no-cache curl && curl -s http://192.168.65.1:11434/api/tags"
echo.

echo Testing 192.168.1.1 (common router IP):
docker run --rm alpine sh -c "apk add --no-cache curl && curl -s http://192.168.1.1:11434/api/tags"
echo.

echo ========================================
echo Getting Docker Network Info
echo ========================================
echo.

echo Docker networks:
docker network ls
echo.

echo Bridge network details:
docker network inspect bridge
echo.

echo ========================================
echo Testing from LangChain Container
echo ========================================
echo.

echo Testing Ollama connection from LangChain container:
docker exec langchain_search_service-langchain-service-1 curl -s http://172.17.0.1:11434/api/tags
echo.

docker exec langchain_search_service-langchain-service-1 curl -s http://10.0.2.2:11434/api/tags
echo.

docker exec langchain_search_service-langchain-service-1 curl -s http://host.docker.internal:11434/api/tags
echo.

docker exec langchain_search_service-langchain-service-1 curl -s http://192.168.65.1:11434/api/tags
echo.

pause 