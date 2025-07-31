@echo off
echo ========================================
echo Testing Container-to-Container Connectivity
echo ========================================
echo.

echo Testing if Web Portal can reach LangChain service by IP:
docker exec langchain_search_service-chap2-webportal-1 curl -s http://172.18.0.1:8000/health
echo.

echo Testing if Web Portal can reach API service by IP:
docker exec langchain_search_service-chap2-webportal-1 curl -s http://172.18.0.1:5001/health
echo.

echo Testing if Web Portal can reach Qdrant by IP:
docker exec langchain_search_service-chap2-webportal-1 curl -s http://172.18.0.1:6333/collections
echo.

echo ========================================
echo Testing DNS Resolution
echo ========================================
echo.

echo Testing if Web Portal can resolve hostnames:
docker exec langchain_search_service-chap2-webportal-1 nslookup langchain-service
echo.

docker exec langchain_search_service-chap2-webportal-1 nslookup chap2-api
echo.

docker exec langchain_search_service-chap2-webportal-1 nslookup qdrant
echo.

echo ========================================
echo Testing Network Configuration
echo ========================================
echo.

echo Web Portal container network info:
docker exec langchain_search_service-chap2-webportal-1 ip addr
echo.

echo Web Portal container routing:
docker exec langchain_search_service-chap2-webportal-1 ip route
echo.

echo ========================================
echo Testing Direct Connection
echo ========================================
echo.

echo Testing if Web Portal can reach LangChain directly:
docker exec langchain_search_service-chap2-webportal-1 ping -c 3 langchain-service
echo.

echo Testing if Web Portal can reach API directly:
docker exec langchain_search_service-chap2-webportal-1 ping -c 3 chap2-api
echo.

pause 