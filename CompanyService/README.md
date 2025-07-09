
# CompanyService

## Overview
CompanyService is a .NET 8 Web API for managing company data. It uses SQL Server 2022 as the primary database and Redis for caching. The API implements a cache-aside strategy: data is read from Redis if available (cache hit) or fetched from SQL Server and stored in Redis (cache miss). Real-time updates are delivered via WebSocket.

## Features
- SQL Server 2022 backend
- Redis caching (cache-aside pattern)
  - Key format: `company:{id}`
  - Default TTL: 30 minutes (configurable)
  - Retry and timeout configurations implemented
- API Key-based authentication (required on all endpoints)
- RESTful CRUD API
- WebSocket endpoint for real-time notifications on company create/update/delete

## Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) and Docker Compose
- (Optional) Local SQL Server and Redis if not using Docker

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=db;Database=CompanyDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;",
    "Redis": "redis:6379"
  },
  "CacheSettings": {
    "DefaultTTLMinutes": 30
  },
  "ApiKey": "SuperSecretApiKey123",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Environment Variables
- `ASPNETCORE_ENVIRONMENT`: `Development` or `Production`
- `ApiKey`: API key for securing endpoints
- `ConnectionStrings__DefaultConnection`: SQL Server connection string override
- `ConnectionStrings__Redis`: Redis connection string override

## Docker Compose

The project includes a `docker-compose.yml` file that sets up:
- API container
- SQL Server container
- Redis container

Run the entire stack:
```bash
docker-compose up --build
```

API will be available at `http://localhost:5000`  
WebSocket will be available at `ws://localhost:5001/ws/companies/updates`

## API Endpoints

### Authentication
All endpoints require an `X-API-KEY` header.

### Company Endpoints
- `GET /api/companies`
  - Returns all companies.
- `GET /api/companies/{id}`
  - Returns company by ID.
  - Caching:
    - Cache hit: returns data from Redis.
    - Cache miss: queries SQL Server and stores result in Redis.
- `POST /api/companies`
  - Creates a new company.
  - Invalidates relevant Redis keys.
- `PUT /api/companies/{id}`
  - Updates an existing company.
  - Invalidates relevant Redis keys.
- `DELETE /api/companies/{id}`
  - Deletes a company.
  - Removes related Redis cache key.

### External API
- `GET api/externalcompanies/random`
  - Fetches random company data from Random User API.

### Health Check
- `GET /health`
  - Returns API health status.

### WebSocket
- `ws://0.0.0.0:5001/ws/companies/updates`
  - Sends messages on company create, update, or delete.

## Redis Configuration
- **Key Pattern**: `company:{id}`
- **Default TTL**: 30 minutes (set in `CacheSettings`)
- **Connection Options**:
  - `AbortOnConnectFail`: false
  - `ConnectRetry`: 3
  - `ConnectTimeout`: 5000ms
  - `KeepAlive`: 180s

## Query Types
- **Read Queries** (SQL):
  - `SELECT TOP(1) ... WHERE [FIRMA_ID] = @id`
- **Write Queries** (SQL):
  - `INSERT INTO FIRMA_BILGILERI ...`
  - `UPDATE FIRMA_BILGILERI SET ... WHERE [FIRMA_ID] = @id`
  - `DELETE FROM FIRMA_BILGILERI WHERE [FIRMA_ID] = @id`
- **Cache Queries** (Redis):
  - `GET company:{id}`
  - `SETEX company:{id} <TTL> <value>`
  - `DEL company:{id}`

## Build and Run

### Local Development
```bash
dotnet restore
dotnet build
dotnet run
```

### Docker
```bash
docker-compose up --build
```

Stop containers:
```bash
docker-compose down
```

## Logging
The application logs:
- Cache hits/misses
- Database queries
- Redis connection retries and restoration

Example logs:
```
✅ Cache hit: company:1
❌ Cache miss: company:1
[Retry 1/3] Failed to connect to Redis: ...
[Info] Redis connection restored.
```

## Notes
- Prometheus is **not implemented**.
- This project uses basic API Key authentication for simplicity.
- All caching is handled with a cache-aside pattern.
