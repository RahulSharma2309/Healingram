Write-Host "Starting Postgres + Seq + Jaeger + Mailpit in Docker."
Write-Host "Run API and Gateway with: dotnet run --project backend/src/Healingram.Api and backend/src/Healingram.Gateway"
Write-Host "Run web with: npm run dev"

docker compose -f infra/docker-compose.yml -f infra/docker-compose.observability.yml up -d postgres seq jaeger mailpit
