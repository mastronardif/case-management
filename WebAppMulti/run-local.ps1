# Stop any previous containers
docker-compose -f .\docker-compose.yml down

# Build app images (optional if you already built)
docker build -t webappmulti ./WebAppMulti
docker build -t webappmulti2 ./WebAppMulti2

# Create network if not exists
docker network inspect appnet > $null 2>&1
if ($LASTEXITCODE -ne 0) {
    docker network create appnet
}

# Start containers
docker-compose -f .\docker-compose.yml up -d

Write-Host "Containers started..."
Write-Host "App1 (PathBase=/app1) -> http://localhost:8090/app1/swagger/index.html"
Write-Host "App2 (PathBase=/app2) -> http://localhost:8090/app2/swagger/index.html"

# Tail Nginx logs
Write-Host "`nPress Ctrl+C to stop. Tailing Nginx logs..."
docker logs -f nginx-proxy
