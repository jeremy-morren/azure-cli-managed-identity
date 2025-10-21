
docker compose -p msi down
docker compose -p msi -f (Join-Path $PSScriptRoot "docker-compose.yaml") up -d --build