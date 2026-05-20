# ═══ LOCAL DEVELOPMENT ═══

# Start PostgreSQL & Redis (Docker)
docker compose up -d postgres redis

# Run API
cd src/Wms.Api
dotnet run
# → Swagger: http://localhost:5182/swagger
# → Health:  http://localhost:5182/health

# ═══ DOCKER PRODUCTION ═══

docker compose up -d --build

# Verify
curl http://localhost:8080/health
curl http://localhost:8080/swagger/v1/swagger.json

# Login test
curl -X POST http://localhost:8080/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@wms.vn","password":"Admin@123"}'

# API test (with token)
TOKEN="your-jwt-token-here"
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:8080/v1/inventory?page=1&limit=10

curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:8080/v1/reports/dashboard

curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:8080/v1/zones
