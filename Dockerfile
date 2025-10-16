# ========================================
# Multi-Stage Dockerfile for .NET 9 API
# ========================================

# ========================================
# Stage 1: Build
# ========================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["DevPioneers.sln", "./"]
COPY ["src/DevPioneers.Api/DevPioneers.Api.csproj", "src/DevPioneers.Api/"]
COPY ["src/DevPioneers.Application/DevPioneers.Application.csproj", "src/DevPioneers.Application/"]
COPY ["src/DevPioneers.Domain/DevPioneers.Domain.csproj", "src/DevPioneers.Domain/"]
COPY ["src/DevPioneers.Infrastructure/DevPioneers.Infrastructure.csproj", "src/DevPioneers.Infrastructure/"]
COPY ["src/DevPioneers.Persistence/DevPioneers.Persistence.csproj", "src/DevPioneers.Persistence/"]

# Restore dependencies
RUN dotnet restore "DevPioneers.sln"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/src/DevPioneers.Api"
RUN dotnet build "DevPioneers.Api.csproj" -c Release -o /app/build

# ========================================
# Stage 2: Publish
# ========================================
FROM build AS publish
RUN dotnet publish "DevPioneers.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ========================================
# Stage 3: Runtime
# ========================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Copy published files from publish stage
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs && chmod 755 /app/logs

# Create non-root user for security
RUN addgroup --system --gid 1000 appuser \
    && adduser --system --uid 1000 --ingroup appuser --shell /bin/sh appuser

# Change ownership of app directory
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Entry point
ENTRYPOINT ["dotnet", "DevPioneers.Api.dll"]