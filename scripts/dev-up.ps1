param(
    [switch]$WithObservability
)

$compose = @("docker", "compose", "-f", "infra/docker-compose.yml")
if ($WithObservability) {
    $compose += @("-f", "infra/docker-compose.observability.yml")
}
$compose += @("up", "--build", "-d")
& $compose[0] $compose[1..($compose.Length-1)]

Write-Host "Web      http://localhost:8080"
Write-Host "Gateway  http://localhost:5000/api/health"
Write-Host "API      http://localhost:5080/api/health"
if ($WithObservability) {
    Write-Host "Seq      http://localhost:5341"
    Write-Host "Jaeger   http://localhost:16686"
    Write-Host "Mailpit  http://localhost:8025"
}
