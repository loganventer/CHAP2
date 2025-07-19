# Multi-stage build for CHAP2.WebPortal
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["CHAP2.UI/CHAP2.WebPortal/CHAP2.Web.csproj", "CHAP2.UI/CHAP2.WebPortal/"]
COPY ["CHAP2.Application/CHAP2.Application.csproj", "CHAP2.Application/"]
COPY ["CHAP2.Domain/CHAP2.Domain.csproj", "CHAP2.Domain/"]
COPY ["CHAP2.Infrastructure/CHAP2.Infrastructure.csproj", "CHAP2.Infrastructure/"]
COPY ["CHAP2.Shared/CHAP2.Shared.csproj", "CHAP2.Shared/"]

# Restore dependencies
RUN dotnet restore "CHAP2.UI/CHAP2.WebPortal/CHAP2.Web.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/CHAP2.UI/CHAP2.WebPortal"
RUN dotnet build "CHAP2.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CHAP2.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80;https://+:443
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CHAP2.Web.dll"] 